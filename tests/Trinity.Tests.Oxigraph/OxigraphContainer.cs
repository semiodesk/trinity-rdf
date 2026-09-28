// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Oxigraph
{
    /// <summary>
    /// Starts a throwaway Oxigraph server in a Docker container for the whole integration-test
    /// assembly and tears it down afterwards (ADR-0036). Testcontainers maps the container's port
    /// 7878 to a random free host port, so the server never collides with an Oxigraph instance
    /// already running locally. The resulting connection string (with the mapped port) is published
    /// on <see cref="ConnectionString"/> before any fixture runs.
    ///
    /// Requires a running Docker daemon. As a <c>[SetUpFixture]</c> in this namespace it runs once
    /// before, and once after, every fixture under <c>Semiodesk.Trinity.Tests.Oxigraph</c>.
    /// </summary>
    [SetUpFixture]
    public class OxigraphContainer
    {
        /// <summary>
        /// Port the server listens on inside the container.
        /// </summary>
        private const int ServicePort = 7878;

        /// <summary>
        /// Connection string for the throwaway server, complete with the mapped host port. Set
        /// before any fixture runs and read by <see cref="OxigraphTestSetup"/>.
        /// </summary>
        public static string ConnectionString { get; private set; }

        private IContainer _container;

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            _container = new ContainerBuilder()
                // Pinned, never :latest -- an upstream push should not be able to redden a branch
                // that changed nothing.
                .WithImage("ghcr.io/oxigraph/oxigraph:0.5.5")
                // --bind 0.0.0.0 is not optional. Oxigraph defaults to binding localhost, which
                // inside a container means the container's own loopback: the port maps, the
                // container reports healthy, and every connection from the host is refused.
                // --location gives it a store directory; without one it refuses to serve.
                .WithCommand("serve", "--location", "/data", "--bind", $"0.0.0.0:{ServicePort}")
                // assignRandomHostPort: true -> a free ephemeral host port, so we never clash with
                // a local Oxigraph on 7878.
                .WithPortBinding(ServicePort, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPort(ServicePort).ForPath("/")))
                .Build();

            await _container.StartAsync();

            var host = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(ServicePort)}";

            // The wait strategy above proves the HTML UI answers, which is not the thing under test.
            // A readiness probe has to exercise the thing under test, or a misrouted server surfaces
            // as 300 unexplained failures in the middle of the run instead of one clear error here
            // (ADR-0043).
            await VerifyQueryEndpointAnswersAsync(host);

            ConnectionString = $"provider=oxigraph;host={host}";
        }

        /// <summary>
        /// Asserts that the server really answers SPARQL at the endpoint the store will use, by
        /// asking it the cheapest possible question.
        /// </summary>
        /// <param name="host">Base URI of the running server, without a trailing slash.</param>
        private static async Task VerifyQueryEndpointAnswersAsync(string host)
        {
            var endpoint = $"{host}/query?query=" + Uri.EscapeDataString("ASK { ?s ?p ?o }");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"The Oxigraph server at {host} does not answer SPARQL: " +
                        $"GET {endpoint} returned {(int)response.StatusCode} {response.ReasonPhrase}. " +
                        "The container is running but the query endpoint is not where the store expects it.");
                }
            }
        }

        [OneTimeTearDown]
        public async Task StopAsync()
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
            }
        }
    }
}
