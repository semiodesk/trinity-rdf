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
using System.Collections.Generic;
using System.Threading.Tasks;
using Semiodesk.Trinity.Store.Fuseki;
using Semiodesk.Trinity.Store.GraphDB;
using Semiodesk.Trinity.Store.Oxigraph;
using Semiodesk.Trinity.Store.Virtuoso;
using Semiodesk.Trinity.Tests;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Provisions a backend and hands back a seeded store, for use from a BenchmarkDotNet
    /// <c>[GlobalSetup]</c>.
    /// </summary>
    /// <remarks>
    /// The containers are the very ones the store test suites use. Their fixture classes are public
    /// and their <c>StartAsync</c> works outside NUnit, so provisioning lives in one place rather
    /// than being copied here -- which is the failure mode that left GraphDB unable to parse TriG
    /// for as long as it did (ADR-0047).
    ///
    /// Containers are started once per process and shared by every benchmark, because starting one
    /// costs seconds to tens of seconds and that has nothing to do with what is being measured. They
    /// are stopped when the process exits; Testcontainers' reaper is the backstop if it is killed.
    ///
    /// A backend that fails to start is not retried. BenchmarkDotNet runs a <c>[GlobalSetup]</c> per
    /// case, so a retry would start a fresh container -- several GB, for GraphDB -- for each of up to
    /// 232 cases, every one of them failing the same way. The first failure is kept and rethrown.
    /// </remarks>
    public static class BenchmarkStore
    {
        private static readonly Dictionary<StoreBackend, string> ConnectionStrings =
            new Dictionary<StoreBackend, string>();

        private static readonly Dictionary<StoreBackend, Exception> StartFailures =
            new Dictionary<StoreBackend, Exception>();

        private static readonly List<Func<Task>> Stops = new List<Func<Task>>();

        private static readonly object Gate = new object();

        private static bool _discoveryRegistered;

        /// <summary>
        /// Starts the backend if it is not already running and returns a store bound to it, with the
        /// test ontologies seeded.
        /// </summary>
        /// <param name="backend">The backend to provision.</param>
        public static IStore Create(StoreBackend backend)
        {
            RegisterDiscoveryOnce();

            var connectionString = ConnectionStringFor(backend);
            var store = StoreFactory.CreateStore(connectionString);

            TestOntologies.LoadInto(store);

            return store;
        }

        /// <summary>
        /// Registers the mapping and ontology assemblies. Global static state (ADR-0020), so it is
        /// done once per process rather than once per store.
        /// </summary>
        /// <remarks>
        /// The test assembly is registered for its <b>ontologies</b> only, which the seeded graphs and
        /// SPARQL prefixes need. Its <b>mappings</b> are deliberately not registered: its
        /// <c>Linq.Person</c> also maps <c>foaf:Person</c>, and an untyped read would then build that
        /// wider test class instead of <see cref="BenchmarkPerson"/> -- so editing a test would move a
        /// benchmark number, which owning the model exists to prevent (see BenchmarkModel.cs).
        /// </remarks>
        private static void RegisterDiscoveryOnce()
        {
            lock (Gate)
            {
                if (_discoveryRegistered)
                {
                    return;
                }

                OntologyDiscovery.AddAssembly(typeof(TestOntologies).Assembly);
                OntologyDiscovery.AddAssembly(typeof(Resource).Assembly);
                MappingDiscovery.RegisterAssembly(typeof(Resource).Assembly);

                // The benchmarks own their mapped model (see BenchmarkModel.cs).
                MappingDiscovery.RegisterAssembly(typeof(BenchmarkPerson).Assembly);

                StoreFactory.LoadProvider<OxigraphStoreProvider>();
                StoreFactory.LoadProvider<FusekiStoreProvider>();
                StoreFactory.LoadProvider<GraphDBStoreProvider>();
                StoreFactory.LoadProvider<VirtuosoStoreProvider>();

                AppDomain.CurrentDomain.ProcessExit += (sender, args) => StopAll();

                _discoveryRegistered = true;
            }
        }

        private static string ConnectionStringFor(StoreBackend backend)
        {
            lock (Gate)
            {
                if (ConnectionStrings.TryGetValue(backend, out var cached))
                {
                    return cached;
                }

                if (StartFailures.TryGetValue(backend, out var failure))
                {
                    throw new InvalidOperationException(
                        $"{backend} failed to start earlier in this run and is not retried; see the inner "
                        + "exception for the original failure.", failure);
                }

                try
                {
                    var connectionString = Start(backend);

                    ConnectionStrings[backend] = connectionString;

                    return connectionString;
                }
                catch (Exception e)
                {
                    StartFailures[backend] = e;

                    throw;
                }
            }
        }

        private static string Start(StoreBackend backend)
        {
            switch (backend)
            {
                case StoreBackend.InMemory:
                    return "provider=dotnetrdf";

                case StoreBackend.Oxigraph:
                {
                    var fixture = new Tests.Oxigraph.OxigraphContainer();

                    return StartContainer(fixture.StartAsync, fixture.StopAsync,
                        () => Tests.Oxigraph.OxigraphContainer.ConnectionString);
                }

                case StoreBackend.Fuseki:
                {
                    var fixture = new Tests.Fuseki.FusekiContainer();

                    return StartContainer(fixture.StartAsync, fixture.StopAsync,
                        () => Tests.Fuseki.FusekiContainer.ConnectionString);
                }

                case StoreBackend.GraphDB:
                {
                    var fixture = new Tests.GraphDB.GraphDBContainer();

                    return StartContainer(fixture.StartAsync, fixture.StopAsync,
                        () => Tests.GraphDB.GraphDBContainer.ConnectionString);
                }

                case StoreBackend.Virtuoso:
                {
                    var fixture = new Tests.Virtuoso.VirtuosoContainer();

                    return StartContainer(fixture.StartAsync, fixture.StopAsync,
                        () => Tests.Virtuoso.VirtuosoContainer.ConnectionString);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unknown backend.");
            }
        }

        /// <summary>
        /// Runs a test-suite container fixture and reads back the connection string it publishes.
        /// </summary>
        /// <remarks>
        /// Called with the fixture's own methods, typed, so a rename in a test project fails the build
        /// here rather than at run time. A start that throws partway can leave a container running, so
        /// the fixture is stopped before the failure propagates; every fixture's <c>StopAsync</c> is
        /// safe to call on one that never finished starting.
        /// </remarks>
        private static string StartContainer(Func<Task> start, Func<Task> stop, Func<string> connectionString)
        {
            try
            {
                start().GetAwaiter().GetResult();
            }
            catch
            {
                try
                {
                    stop().GetAwaiter().GetResult();
                }
                catch
                {
                    // The start failure is the one worth reporting; the reaper removes what is left.
                }

                throw;
            }

            Stops.Add(stop);

            return connectionString();
        }

        private static void StopAll()
        {
            lock (Gate)
            {
                foreach (var stop in Stops)
                {
                    try
                    {
                        stop().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // Best effort at exit; Testcontainers' reaper removes anything left behind.
                    }
                }

                Stops.Clear();
            }
        }
    }
}
