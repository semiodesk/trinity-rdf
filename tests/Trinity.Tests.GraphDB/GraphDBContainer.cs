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

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.GraphDB
{
    /// <summary>
    /// Starts a throwaway Ontotext GraphDB server in a Docker container for the whole
    /// integration-test assembly, creates the <c>trinity-rdf</c> repository the tests expect, and
    /// tears it all down afterwards (ADR-0036). Testcontainers maps GraphDB's HTTP port 7200 to a
    /// random free host port, so it never collides with a GraphDB already running locally. The
    /// connection string (with the mapped port) is published on
    /// <see cref="GraphDBContainer.ConnectionString"/> before any fixture runs and consumed by
    /// <see cref="GraphDBTestSetup"/>.
    ///
    /// Requires a running Docker daemon.
    /// </summary>
    [SetUpFixture]
    public class GraphDBContainer
    {
        private const string RepositoryId = "trinity-rdf";

        private IContainer _container;

        private static string Host;

        private static int Port;

        /// <summary>Trinity connection string for the running container (random host port).</summary>
        public static string ConnectionString =>
            $"provider=graphdb;host=http://{Host}:{Port};uid=trinity;pw=test;repository={RepositoryId}";

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            _container = new ContainerBuilder()
                .WithImage("ontotext/graphdb:10.8.0")
                // assignRandomHostPort: true -> a free ephemeral host port, so we never clash
                // with a local GraphDB on 7200.
                .WithPortBinding(7200, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPort(7200).ForPath("/rest/repositories")))
                .Build();

            await _container.StartAsync();

            Host = _container.Hostname;
            Port = _container.GetMappedPublicPort(7200);

            await CreateRepositoryAsync();
        }

        /// <summary>
        /// Creates the repository the tests target by POSTing a GraphDB repository configuration
        /// (Turtle) to the REST API. GraphDB Free ships with security disabled, so the credentials
        /// in the connection string are accepted but not enforced.
        /// </summary>
        private async Task CreateRepositoryAsync()
        {
            const string config = @"@prefix rep: <http://www.openrdf.org/config/repository#> .
@prefix sr: <http://www.openrdf.org/config/repository/sail#> .
@prefix sail: <http://www.openrdf.org/config/sail#> .
@prefix graphdb: <http://www.ontotext.com/config/graphdb#> .
[] a rep:Repository ;
   rep:repositoryID ""trinity-rdf"" ;
   rep:repositoryImpl [
      rep:repositoryType ""graphdb:SailRepository"" ;
      sr:sailImpl [ sail:sailType ""graphdb:Sail"" ; graphdb:ruleset ""rdfsplus-optimized"" ] ] .";

            using (var client = new HttpClient())
            using (var form = new MultipartFormDataContent())
            {
                form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(config)), "config", "config.ttl");

                var response = await client.PostAsync($"http://{Host}:{Port}/rest/repositories", form);

                response.EnsureSuccessStatusCode();
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
