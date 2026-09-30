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

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Reading through a layered view against reading the baseline alone (ADR-0041).
    /// </summary>
    /// <remarks>
    /// Reproduces ADR-0041's read table, which was measured through the production path at 1,000,000
    /// baseline triples with ~100 staged additions and ~100 staged removals:
    ///
    /// <code>
    /// | Read                            | in-memory | Virtuoso | GraphDB |
    /// | GetResource (subject-bound)     | 1.31x     | 0.94x    | 1.29x   |
    /// | ContainsResource (ASK)          | 3.45x     | 1.18x    | 1.04x   |
    /// | caller query, unselective scan  | 2.24x     | 0.97x    | 0.94x   |
    /// | caller query, selective         | 4.23x     | 1.47x    | 2.33x   |
    /// </code>
    ///
    /// Each of those is a category here, and its baseline row is the same read on the plain baseline
    /// model, so the Ratio column <b>is</b> that table. The 1M row is <see cref="LayeredReadLargeBenchmarks"/>,
    /// behind <c>--large</c>.
    ///
    /// Each category has a third row: the same read through a <see cref="IModelGroup"/> of the baseline
    /// and the additions. A group is a union with no subtraction, so the view-to-group gap is what the
    /// removals cost, and the group-to-baseline gap is what a second graph costs -- the split
    /// <see cref="ModelGroupBenchmarks"/> measures in isolation. The group reads a different answer (it
    /// does not subtract), which is the point of comparing it, not a defect of the row.
    ///
    /// <see cref="Materialized"/> doubles the table. A materialized view reads one graph natively, so
    /// its rows should sit near 1x; how near is what they are for.
    /// </remarks>
    public class LayeredReadBenchmarks : LayeredBenchmarkBase
    {
        /// <summary>
        /// Lookups per invocation for the point reads, each against a different subject.
        /// </summary>
        private const int Lookups = 20;

        protected override IEnumerable<int> BaselineSizes => new[] { 10_000, 100_000 };

        /// <summary>
        /// Whether the view is materialized.
        /// </summary>
        [Params(false, true)]
        public bool Materialized { get; set; }

        protected override bool UseMaterialized => Materialized;

        protected override string GraphPrefix => "layered-read";

        private IModelGroup _group;

        private int _next;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            _group = Store.CreateModelGroup(Baseline.Uri, Additions.Uri);
        }

        private int NextIndex()
        {
            _next = (_next + 7919) % People;

            return _next;
        }

        // --- GetResource ------------------------------------------------------------------------

        [Benchmark(Description = "GetResource<T> (view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceView() => Lookup(m => m.GetResource<BenchmarkPerson>(PersonUri(NextIndex())) != null, View);

        [Benchmark(Description = "GetResource<T> (baseline model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceBaseline() => Lookup(m => m.GetResource<BenchmarkPerson>(PersonUri(NextIndex())) != null, Baseline);

        [Benchmark(Description = "GetResource<T> (group: baseline + additions)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceGroup() => Lookup(m => m.GetResource<BenchmarkPerson>(PersonUri(NextIndex())) != null, _group);

        // --- ContainsResource -------------------------------------------------------------------

        [Benchmark(Description = "ContainsResource (view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsView() => Lookup(m => m.ContainsResource(PersonUri(NextIndex())), View);

        [Benchmark(Description = "ContainsResource (baseline model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsBaseline() => Lookup(m => m.ContainsResource(PersonUri(NextIndex())), Baseline);

        [Benchmark(Description = "ContainsResource (group: baseline + additions)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsGroup() => Lookup(m => m.ContainsResource(PersonUri(NextIndex())), _group);

        // --- A caller's selective query: one subject -------------------------------------------

        [Benchmark(Description = "caller query, selective (view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("CallerSelective")]
        public int SelectiveView() => Lookup(m => Bindings(m, SelectiveQuery()) >= 0, View);

        [Benchmark(Description = "caller query, selective (baseline model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("CallerSelective")]
        public int SelectiveBaseline() => Lookup(m => Bindings(m, SelectiveQuery()) >= 0, Baseline);

        [Benchmark(Description = "caller query, selective (group: baseline + additions)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("CallerSelective")]
        public int SelectiveGroup() => Lookup(m => Bindings(m, SelectiveQuery()) >= 0, _group);

        // --- A caller's unselective query: every name ------------------------------------------

        [Benchmark(Description = "caller query, unselective scan (view)")]
        [BenchmarkCategory("CallerScan")]
        public int ScanView() => Bindings(View, ScanQuery());

        [Benchmark(Description = "caller query, unselective scan (baseline model)", Baseline = true)]
        [BenchmarkCategory("CallerScan")]
        public int ScanBaseline() => Bindings(Baseline, ScanQuery());

        [Benchmark(Description = "caller query, unselective scan (group: baseline + additions)")]
        [BenchmarkCategory("CallerScan")]
        public int ScanGroup() => Bindings(_group, ScanQuery());

        /// <summary>
        /// One subject's name. Written without a dataset clause: the model it runs through supplies
        /// one, and a view refuses a caller <c>FROM</c> (ADR-0041).
        /// </summary>
        private SparqlQuery SelectiveQuery()
        {
            return new SparqlQuery(
                $"SELECT ?n WHERE {{ <{PersonUri(NextIndex())}> <{Vocabulary.FirstNameProperty}> ?n }}",
                declarePrefixes: false);
        }

        private static SparqlQuery ScanQuery()
        {
            return new SparqlQuery(
                $"SELECT ?s ?n WHERE {{ ?s <{Vocabulary.FirstNameProperty}> ?n }}", declarePrefixes: false);
        }

        private static int Bindings(IModel model, SparqlQuery query)
        {
            return model.ExecuteQuery(query).GetBindings().Count();
        }

        private static int Lookup(System.Func<IModel, bool> read, IModel model)
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += read(model) ? 1 : 0;
            }

            return found;
        }
    }

    /// <summary>
    /// <see cref="LayeredReadBenchmarks"/> at the 1,000,000-triple baseline ADR-0041 measured at.
    /// Opt-in with <c>--large</c>.
    /// </summary>
    /// <remarks>
    /// Virtuoso's materialized cells fail in setup: it cannot materialize a baseline this size in one
    /// request, and <c>Refresh()</c> refuses to serve the empty graph that results (ADR-0042). That is
    /// the verification working. The non-materialized cells are unaffected.
    /// </remarks>
    [BenchmarkCategory(Program.LargeCategory)]
    public class LayeredReadLargeBenchmarks : LayeredReadBenchmarks
    {
        protected override IEnumerable<int> BaselineSizes => new[] { 1_000_000 };
    }
}
