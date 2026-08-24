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
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015-2020

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Fuseki
{
    /// <summary>
    /// Starts a throwaway Apache Jena Fuseki server in a Docker container for the whole
    /// integration-test assembly and tears it down afterwards (ADR-0036). Testcontainers maps
    /// the container's port 3030 to a random free host port, so the server never collides with a
    /// Fuseki instance already running locally. The resulting connection string (with the mapped
    /// port) is published on <see cref="ConnectionString"/> before any fixture runs.
    ///
    /// Requires a running Docker daemon. As a <c>[SetUpFixture]</c> in this namespace it runs once
    /// before, and once after, every fixture under <c>Semiodesk.Trinity.Tests.Fuseki</c>.
    /// </summary>
    [SetUpFixture]
    public class FusekiContainer
    {
        /// <summary>
        /// Name of the dataset created on the throwaway server. Jena's conventional short name; the
        /// connection string below binds the store to it.
        /// </summary>
        private const string Dataset = "ds";

        /// <summary>
        /// Credentials the image is started with, and which the admin protocol requires for writes.
        /// </summary>
        private const string User = "admin";

        private const string Password = "test";

        /// <summary>
        /// Connection string for the throwaway server, complete with the mapped host port. Set
        /// before any fixture runs and read by <see cref="FusekiTestSetup"/>.
        /// </summary>
        public static string ConnectionString { get; private set; }

        private IContainer _container;

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            _container = new ContainerBuilder()
                // stain/jena-fuseki is used rather than secoresearch/fuseki because it serves the
                // dataset query endpoint at /<dataset>/query, which is what dotNetRDF's
                // FusekiConnector derives from the /<dataset>/data URL it is given.
                //
                // 5.1.0 rather than 4.0.0: Jena 4.0.0 answers HTTP 500 "Not a valid UUID string"
                // to any query mentioning a urn:uuid: (or uuid:) IRI, because it hands the whole
                // IRI to a UUID parser instead of just the UUID part. Since Model.CreateResource()
                // mints urn:uuid: identifiers by default, that made a resource created without an
                // explicit URI writable but permanently unreadable. Fixed upstream by 5.1.0.
                .WithImage("stain/jena-fuseki:5.1.0")
                .WithEnvironment("ADMIN_PASSWORD", Password)
                // assignRandomHostPort: true -> a free ephemeral host port, so we never clash
                // with a local Fuseki on 3030.
                .WithPortBinding(3030, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPort(3030).ForPath("/$/ping")))
                .Build();

            await _container.StartAsync();

            var host = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(3030)}";

            // The image ships with no dataset at all, and it honours no environment variable to
            // create one: FUSEKI_DATASET_1 belongs to a different image (secoresearch/fuseki) and is
            // silently ignored here. Without this call the server answers /$/ping with 200 while
            // every path under /<dataset>/* 404s -- query, update and Graph Store Protocol alike --
            // which is what made the whole suite fail at 4/86 and read as an upstream connector bug.
            await CreateDatasetAsync(host);

            // ...and because /$/ping cannot see that, prove the dataset actually answers a query
            // before any fixture runs. A readiness probe has to exercise the thing under test.
            await VerifyDatasetIsQueryableAsync(host);

            ConnectionString =
                $"provider=fuseki;host={host};uid={User};pw={Password};dataset={Dataset}";
        }

        /// <summary>
        /// Creates the in-memory dataset over Fuseki's admin protocol.
        /// </summary>
        /// <param name="host">Base URI of the running server, without a trailing slash.</param>
        private static async Task CreateDatasetAsync(string host)
        {
            using (var client = CreateClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dbType", "mem"),
                    new KeyValuePair<string, string>("dbName", Dataset)
                });

                var response = await client.PostAsync($"{host}/$/datasets", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Could not create the Fuseki dataset '{Dataset}' at {host}: " +
                        $"POST /$/datasets returned {(int)response.StatusCode} {response.ReasonPhrase}. " +
                        "Every query in this assembly would fail with a 404 without it.");
                }
            }
        }

        /// <summary>
        /// Asserts that the dataset created above really serves SPARQL, by asking it the cheapest
        /// possible question. This is the check that <c>/$/ping</c> does not perform: a Fuseki server
        /// with no datasets is "up" as far as ping is concerned, so a provisioning failure would
        /// otherwise surface as 80-odd unexplained 404s in the middle of the run instead of one clear
        /// error here.
        /// </summary>
        /// <param name="host">Base URI of the running server, without a trailing slash.</param>
        private static async Task VerifyDatasetIsQueryableAsync(string host)
        {
            var endpoint = $"{host}/{Dataset}/query?query=" + Uri.EscapeDataString("ASK { ?s ?p ?o }");

            using (var client = CreateClient())
            {
                var response = await client.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"The Fuseki dataset '{Dataset}' at {host} does not answer SPARQL: " +
                        $"GET {endpoint} returned {(int)response.StatusCode} {response.ReasonPhrase}. " +
                        "The container is running but the dataset was not provisioned.");
                }
            }
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{User}:{Password}")));

            return client;
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
