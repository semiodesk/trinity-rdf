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
using System.Linq;
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
        /// <summary>
        /// Name of the inference rule set the connection string selects, and which the tests that
        /// pass <c>inferenceEnabled: true</c> depend on.
        /// </summary>
        private const string RuleSet = "urn:semiodesk/test/ruleset";

        /// <summary>
        /// Schema graphs the rule set draws its axioms from. These are the graphs
        /// <see cref="TestOntologies"/> seeds, and the same set the retired
        /// <c>ontologies.config</c> declared for this rule set.
        /// </summary>
        private static readonly string[] RuleSetGraphs =
        {
            "http://www.w3.org/1999/02/22-rdf-syntax-ns#",
            "http://www.w3.org/2000/01/rdf-schema#",
            "http://www.w3.org/2002/07/owl#",
            "http://xmlns.com/foaf/0.1/",
            "http://www.semanticdesktop.org/ontologies/2007/03/22/nco#"
        };

        private static IContainer _instance;

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

            _instance = _container;

            // Virtuoso has no rule set out of the box, and the connection string below selects one
            // by name. It used to be created from ontologies.config by the configuration subsystem
            // ADR-0011 retired -- that file is still in the repo but nothing reads it -- so every
            // inferenceEnabled query failed with "SP031: 'define input:inference refers to undefined
            // inference rule set". Create it here, the way GraphDBContainer creates its repository.
            //
            // This first call only makes the name resolve, so a query naming it compiles. It cannot
            // yet infer anything: rdfs_rule_set builds the rule set from a *snapshot* of the graphs
            // taken when it runs, and nothing is seeded yet. RefreshRuleSet below rebuilds it once
            // TestOntologies has loaded the axioms -- measured: created-before-data infers nothing,
            // re-registering the same name after the data returns the entailed row.
            await CreateRuleSetAsync();

            ConnectionString =
                $"provider=virtuoso;host={_container.Hostname};port={_container.GetMappedPublicPort(1111)};uid=dba;pw=dba;rule={RuleSet}";
        }

        /// <summary>
        /// Rebuilds <see cref="RuleSet"/> against the current contents of the schema graphs. Called
        /// from <c>VirtuosoTestSetup.AfterSeed</c>, once the axioms are actually in the store.
        /// </summary>
        /// <remarks>
        /// <c>rdfs_rule_set</c> is idempotent by name: re-registering the same rule set re-reads the
        /// graphs rather than duplicating anything.
        /// </remarks>
        public static void RefreshRuleSet()
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    "The Virtuoso container has not been started; RefreshRuleSet must run after "
                    + nameof(StartAsync) + ".");
            }

            CreateRuleSetAsync(_instance).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Registers <see cref="RuleSet"/> over each schema graph via Virtuoso's <c>rdfs_rule_set</c>.
        /// </summary>
        private Task CreateRuleSetAsync() => CreateRuleSetAsync(_container);

        private static async Task CreateRuleSetAsync(IContainer container)
        {
            var statements = string.Concat(
                RuleSetGraphs.Select(g => $"rdfs_rule_set('{RuleSet}', '{g}');"));

            var result = await container.ExecAsync(
                new[] { "isql", "1111", "dba", "dba", $"exec={statements}" });

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Could not create the Virtuoso inference rule set '{RuleSet}': isql exited " +
                    $"{result.ExitCode}. Every inferenceEnabled query in this assembly would fail " +
                    $"to compile without it.{Environment.NewLine}{result.Stderr}{result.Stdout}");
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
