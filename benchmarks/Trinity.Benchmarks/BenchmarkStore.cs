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
using System.Reflection;
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
    /// for as long as it did (ADR-0046).
    ///
    /// Containers are started once per process and shared by every benchmark, because starting one
    /// costs seconds to tens of seconds and that has nothing to do with what is being measured.
    /// </remarks>
    public static class BenchmarkStore
    {
        private static readonly Dictionary<StoreBackend, string> ConnectionStrings =
            new Dictionary<StoreBackend, string>();

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
        private static void RegisterDiscoveryOnce()
        {
            lock (Gate)
            {
                if (_discoveryRegistered)
                {
                    return;
                }

                OntologyDiscovery.AddAssembly(typeof(TestOntologies).Assembly);
                MappingDiscovery.RegisterAssembly(typeof(TestOntologies).Assembly);
                OntologyDiscovery.AddAssembly(typeof(Resource).Assembly);
                MappingDiscovery.RegisterAssembly(typeof(Resource).Assembly);

                // The benchmarks own their mapped model (see BenchmarkModel.cs).
                MappingDiscovery.RegisterAssembly(typeof(BenchmarkPerson).Assembly);

                StoreFactory.LoadProvider<OxigraphStoreProvider>();
                StoreFactory.LoadProvider<FusekiStoreProvider>();
                StoreFactory.LoadProvider<GraphDBStoreProvider>();
                StoreFactory.LoadProvider<VirtuosoStoreProvider>();

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

                var connectionString = Start(backend);

                ConnectionStrings[backend] = connectionString;

                return connectionString;
            }
        }

        private static string Start(StoreBackend backend)
        {
            switch (backend)
            {
                case StoreBackend.InMemory:
                    return "provider=dotnetrdf";

                case StoreBackend.Oxigraph:
                    return StartContainer<Tests.Oxigraph.OxigraphContainer>(
                        () => Tests.Oxigraph.OxigraphContainer.ConnectionString);

                case StoreBackend.Fuseki:
                    return StartContainer<Tests.Fuseki.FusekiContainer>(
                        () => Tests.Fuseki.FusekiContainer.ConnectionString);

                case StoreBackend.GraphDB:
                    return StartContainer<Tests.GraphDB.GraphDBContainer>(
                        () => Tests.GraphDB.GraphDBContainer.ConnectionString);

                case StoreBackend.Virtuoso:
                    return StartContainer<Tests.Virtuoso.VirtuosoContainer>(
                        () => Tests.Virtuoso.VirtuosoContainer.ConnectionString);

                default:
                    throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unknown backend.");
            }
        }

        /// <summary>
        /// Runs a test-suite container fixture's <c>StartAsync</c> and reads back the connection
        /// string it publishes.
        /// </summary>
        /// <remarks>
        /// Invoked by reflection on the method name rather than through an interface, because the
        /// fixtures are NUnit types that share a shape but no base type. Adding one for the sake of
        /// this would change four test projects to suit a benchmark.
        /// </remarks>
        private static string StartContainer<T>(Func<string> connectionString) where T : new()
        {
            var fixture = new T();
            var start = typeof(T).GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance);

            if (start == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} has no public StartAsync; the container fixtures are expected to "
                    + "expose one so the benchmarks can reuse them rather than copy their provisioning.");
            }

            ((Task)start.Invoke(fixture, null)).GetAwaiter().GetResult();

            var result = connectionString();

            if (string.IsNullOrEmpty(result))
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name}.StartAsync completed but published no connection string.");
            }

            return result;
        }
    }
}
