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
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Shared provisioning for a cross-store benchmark.
    /// </summary>
    /// <remarks>
    /// <see cref="RunStrategy.Monitoring"/> with an invocation count of one, rather than
    /// BenchmarkDotNet's default pilot. These operations are network- and disk-bound and take
    /// milliseconds to seconds; the default strategy would run a multi-second operation thousands of
    /// times to find a stable nanosecond figure that does not exist here.
    ///
    /// <see cref="MemoryDiagnoserAttribute"/> is on because allocation is the half of the cost that
    /// is unambiguously ours: the store's work happens in another process, so what is allocated here
    /// is what Trinity built to ask for it.
    /// </remarks>
    ///
    /// The job is built in <see cref="Program"/> rather than declared with attributes here.
    /// Stacking <c>[InProcess]</c> on <c>[SimpleJob]</c> does not configure one job, it declares
    /// <b>two</b> -- an in-process one at BenchmarkDotNet's default iteration counts, and a
    /// Monitoring one that spawns a process per case and so restarts every container. That doubles
    /// the matrix and makes most of it meaningless, while still printing a plausible table.
    public abstract class StoreBenchmarkBase
    {
        /// <summary>
        /// The backend under test. Every benchmark runs once per value.
        /// </summary>
        [ParamsAllValues]
        public StoreBackend Backend { get; set; }

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
        /// Counts the triples in the benchmark model.
        /// </summary>
        /// <remarks>
        /// Used to prove a write benchmark did the work, outside the timed region. This is not
        /// belt-and-braces: ADR-0042 records that Virtuoso <b>silently writes zero</b> when an
        /// <c>INSERT ... WHERE</c> exceeds its transaction-log limit. A scaling benchmark that
        /// crosses that limit would report an excellent time for having done nothing, and the number
        /// would look like a result rather than a failure.
        /// </remarks>
        protected int CountTriples()
        {
            var query = new SparqlQuery(
                $"SELECT ?s ?p ?o FROM <{Model.Uri}> WHERE {{ ?s ?p ?o }}", declarePrefixes: false);

            return Store.ExecuteQuery(query).GetBindings().Count();
        }

        /// <summary>
        /// Throws unless the model holds exactly <paramref name="expected"/> triples.
        /// </summary>
        protected void AssertWrote(int expected)
        {
            var actual = CountTriples();

            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"{Backend}: expected {expected} triples in <{Model.Uri}> after the benchmark but "
                    + $"found {actual}. The measurement is of an operation that did not do the work, "
                    + "so the timing is meaningless -- see ADR-0042 on Virtuoso writing zero above its "
                    + "transaction-log limit.");
            }
        }
    }
}
