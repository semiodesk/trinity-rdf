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

namespace Semiodesk.Trinity.Tests.Virtuoso
{
    /// <summary>
    /// Starts a throwaway OpenLink Virtuoso server in a Docker container for the whole
    /// integration-test assembly and tears it down afterwards (ADR-0036). Testcontainers maps
    /// Virtuoso's native protocol port 1111 to a random free host port, so it never collides with
    /// a Virtuoso already running locally. The connection string (with the mapped port) is
    /// published on <see cref="VirtuosoContainer.ConnectionString"/> before any fixture runs and
    /// consumed by <see cref="VirtuosoTestSetup"/>.
    ///
    /// Requires a running Docker daemon.
    /// </summary>
    [SetUpFixture]
    public class VirtuosoContainer
    {
        private IContainer _container;

        /// <summary>Trinity connection string for the running container (random host port).</summary>
        public static string ConnectionString { get; private set; }

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            _container = new ContainerBuilder()
                .WithImage("openlink/virtuoso-opensource-7:latest")
                .WithEnvironment("DBA_PASSWORD", "dba")
                // assignRandomHostPort: true -> a free ephemeral host port, so we never clash
                // with a local Virtuoso on 1111.
                .WithPortBinding(1111, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server online at 1111"))
                .Build();

            await _container.StartAsync();

            ConnectionString =
                $"provider=virtuoso;host={_container.Hostname};port={_container.GetMappedPublicPort(1111)};uid=dba;pw=dba;rule=urn:semiodesk/test/ruleset";
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
