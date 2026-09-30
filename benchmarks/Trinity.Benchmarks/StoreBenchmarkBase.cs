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
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Shared provisioning for a cross-store benchmark: the backend parameter, a store, a clean
    /// model, and the guards that prove the work happened.
    /// </summary>
    /// <remarks>
    /// The job every benchmark runs under -- in-process, Monitoring, memory diagnoser -- is built in
    /// <see cref="Program"/>, not declared here with attributes. Stacking <c>[InProcess]</c> on
    /// <c>[SimpleJob]</c> does not configure one job, it declares <b>two</b>: an in-process one at
    /// BenchmarkDotNet's default iteration counts, and a Monitoring one that spawns a process per case
    /// and so restarts every container. That doubles the matrix and makes most of it meaningless,
    /// while still printing a plausible table.
    /// </remarks>
    public abstract class StoreBenchmarkBase
    {
        /// <summary>
        /// The backend under test. Every benchmark runs once per selected value.
        /// </summary>
        /// <remarks>
        /// Selected with <c>TRINITY_BENCH_BACKENDS</c> rather than on the command line, because
        /// BenchmarkDotNet has no parameter filter (<c>-p</c> is its profiler switch). An unselected
        /// backend is never provisioned: containers start lazily in <see cref="BenchmarkStore"/>.
        /// </remarks>
        [ParamsSource(nameof(Backends))]
        public StoreBackend Backend { get; set; }

        /// <summary>
        /// The values <see cref="Backend"/> takes. Public and instance-level because BenchmarkDotNet
        /// resolves a <c>ParamsSource</c> by name on the benchmark type, and an inherited static
        /// member is not found that way.
        /// </summary>
        public IEnumerable<StoreBackend> Backends => BenchmarkBackends.Selected;

        protected IStore Store;

        protected IModel Model;

        protected UriRef BaseUri;

        [GlobalSetup]
        public virtual void GlobalSetup()
        {
            // A fixed identifier; it names graphs rather than addressing the server, so it does not
            // track the container's mapped port.
            BaseUri = new UriRef("http://localhost/benchmark/");

            Store = BenchmarkStore.Create(Backend);
            Model = Store.GetModel(BaseUri.GetUriRef("model"));

            Model.Clear();
        }

        [GlobalCleanup]
        public virtual void GlobalCleanup()
        {
            Model?.Clear();
            Store?.Dispose();
        }

        /// <summary>
        /// The URI of the <paramref name="i"/>th person, shared by every workload so seeding and the
        /// raw baselines agree on the subjects.
        /// </summary>
        protected UriRef PersonUri(int i)
        {
            return BaseUri.GetUriRef($"person-{i}");
        }

        /// <summary>
        /// Counts the triples in the benchmark model.
        /// </summary>
        /// <remarks>
        /// Used to prove a write benchmark did the work, outside the timed region. This is not
        /// belt-and-braces: Virtuoso <b>silently writes zero</b> when one statement exceeds 10,000
        /// entries, because the error it raises is swallowed (#50, #70). A scaling benchmark that
        /// crosses that limit would report an excellent time for having done nothing, and the number
        /// would look like a result rather than a failure.
        /// </remarks>
        protected int CountTriples()
        {
            return CountTriples(Model.Uri);
        }

        /// <summary>
        /// Counts the triples in one graph.
        /// </summary>
        /// <remarks>
        /// An aggregate rather than counting bindings client-side, because the layered workloads verify
        /// graphs of a million triples, and transferring a million rows to count them would make the
        /// guard cost more than the benchmark. The value comes back as whatever integer box the store
        /// chooses (ADR-0040), so it is converted rather than cast.
        /// </remarks>
        protected int CountTriples(Uri graph)
        {
            return CountWhere(graph, "?s ?p ?o");
        }

        /// <summary>
        /// Counts the solutions of <paramref name="pattern"/> in one graph.
        /// </summary>
        /// <param name="graph">The graph to count in, as the query's only <c>FROM</c>.</param>
        /// <param name="pattern">A group graph pattern body, without the braces.</param>
        protected int CountWhere(Uri graph, string pattern)
        {
            var query = new SparqlQuery(
                $"SELECT (COUNT(*) AS ?count) FROM <{graph}> WHERE {{ {pattern} }}", declarePrefixes: false);

            var binding = Store.ExecuteQuery(query).GetBindings().Single();

            return Convert.ToInt32(binding["count"]);
        }

        /// <summary>
        /// Throws unless the model holds exactly <paramref name="expected"/> triples.
        /// </summary>
        protected void AssertWrote(int expected)
        {
            AssertCount(Model.Uri, expected,
                "after the benchmark. The measurement is of an operation that did not do the work, so the "
                + "timing is meaningless -- see #50 and #70 on Virtuoso reporting success for a write "
                + "it refused.");
        }

        /// <summary>
        /// Throws unless <paramref name="graph"/> holds exactly <paramref name="expected"/> triples
        /// once a fixture is seeded.
        /// </summary>
        /// <remarks>
        /// A read of an empty or half-seeded model is fast and says nothing, so every read workload
        /// proves its fixture before measuring it.
        /// </remarks>
        protected void AssertSeeded(int expected, Uri graph = null)
        {
            AssertCount(graph ?? Model.Uri, expected,
                "after seeding. Every benchmark in this class would be measuring the wrong amount of data.");
        }

        /// <summary>
        /// Throws unless a read benchmark's answer is the one its fixture implies, and returns it.
        /// </summary>
        /// <remarks>
        /// A read that returns less than it should is faster than one that returns everything: a bulk
        /// load that resolved none of its members would be recorded as a large improvement. Checking
        /// the count costs one comparison, so it stays inside the timed region, where the answer is.
        /// </remarks>
        protected int Expect(int actual, int expected, string what)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"{Backend}: {what} returned {actual}, expected {expected}. A read that returns the "
                    + "wrong answer is timed as a different, usually faster, operation.");
            }

            return actual;
        }

        /// <inheritdoc cref="Expect(int, int, string)"/>
        protected bool Expect(bool actual, bool expected, string what)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException($"{Backend}: {what} returned {actual}, expected {expected}.");
            }

            return actual;
        }

        private void AssertCount(Uri graph, int expected, string consequence)
        {
            var actual = CountTriples(graph);

            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"{Backend}: expected {expected} triples in <{graph}> but found {actual} {consequence}");
            }
        }
    }

    /// <summary>
    /// Thrown by a fixture for a parameter value it cannot build, such as a layered size that is not a
    /// multiple of 5.
    /// </summary>
    /// <remarks>
    /// Its own type, so the <c>profile</c> runner can report it as a usage error without doing the same
    /// to the <see cref="ArgumentException"/>s Trinity throws on real failures, which need their stack
    /// trace and the fixture's cleanup.
    /// </remarks>
    public sealed class BenchmarkParameterException : Exception
    {
        public BenchmarkParameterException(string message) : base(message) { }
    }
}
