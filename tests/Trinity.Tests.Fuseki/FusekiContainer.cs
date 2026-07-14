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
    /// port) is published on <see cref="SetupClass.ConnectionString"/> before any fixture runs.
    ///
    /// Requires a running Docker daemon. As a <c>[SetUpFixture]</c> in this namespace it runs once
    /// before, and once after, every fixture under <c>Semiodesk.Trinity.Tests.Fuseki</c>.
    /// </summary>
    [SetUpFixture]
    public class FusekiContainer
    {
        private IContainer _container;

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            _container = new ContainerBuilder()
                // stain/jena-fuseki serves the dataset query endpoint at /ds/query, which is what
                // dotNetRDF's FusekiConnector targets (secoresearch/fuseki only exposes /ds/sparql).
                .WithImage("stain/jena-fuseki:4.0.0")
                .WithEnvironment("ADMIN_PASSWORD", "test")
                .WithEnvironment("FUSEKI_DATASET_1", "ds")
                // assignRandomHostPort: true -> a free ephemeral host port, so we never clash
                // with a local Fuseki on 3030.
                .WithPortBinding(3030, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPort(3030).ForPath("/$/ping")))
                .Build();

            await _container.StartAsync();

            SetupClass.ConnectionString =
                $"provider=fuseki;host=http://{_container.Hostname}:{_container.GetMappedPublicPort(3030)};uid=admin;pw=test;dataset=ds";
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
