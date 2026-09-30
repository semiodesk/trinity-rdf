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
    /// Each category has four rows: the view in rewriting mode, the view materialized, the baseline, and
    /// the same read through a <see cref="IModelGroup"/> of the baseline and the additions. A group is a
    /// union with no subtraction, so the view-to-group gap is what the removals cost, and the
    /// group-to-baseline gap is what a second graph costs -- the split <see cref="ModelGroupBenchmarks"/>
    /// measures in isolation. The group reads a different answer (it does not subtract), which is the
    /// point of comparing it; each row checks its own answer.
    ///
    /// Materialization is a pair of rows rather than a parameter. As a parameter it doubled every cell,
    /// including the baseline and group rows that never touch the view -- the costliest cells in the
    /// class, an in-memory group query being O(data). Only the materialized rows build the fourth graph,
    /// through their own <c>[GlobalSetup]</c>.
    /// </remarks>
    public class LayeredReadBenchmarks : LayeredBenchmarkBase
    {
        /// <summary>
        /// Lookups per invocation for the point reads, each against a different subject.
        /// </summary>
        private const int Lookups = 20;

        protected override IEnumerable<int> BaselineSizes => new[] { 10_000, 100_000 };

        private bool _materialized;

        protected override bool UseMaterialized => _materialized;

        protected override string GraphPrefix => "layered-read";

        private IModelGroup _group;

        private int _next;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            _group = Store.CreateModelGroup(Baseline.Uri, Additions.Uri);
        }

        /// <summary>
        /// The setup for the materialized rows: the same fixture, with the fourth graph built.
        /// </summary>
        [GlobalSetup(Targets = new[]
        {
            nameof(GetResourceMaterialized), nameof(ContainsMaterialized),
            nameof(SelectiveMaterialized), nameof(ScanMaterialized)
        })]
        public void GlobalSetupMaterialized()
        {
            _materialized = true;

            GlobalSetup();
        }

        /// <summary>
        /// The subjects one invocation looks up, each different.
        /// </summary>
        private int[] NextIndices()
        {
            var indices = new int[Lookups];

            for (var k = 0; k < Lookups; k++)
            {
                _next = (_next + 7919) % People;
                indices[k] = _next;
            }

            return indices;
        }

        private bool IsRemovedName(int i)
        {
            var stride = People / RemovedNames;

            return i % stride == 0 && i / stride < RemovedNames;
        }

        // --- GetResource ------------------------------------------------------------------------
        //
        // A resource whose name is staged for removal still exists through the view (its type and links
        // remain), so every lookup finds one.

        [Benchmark(Description = "GetResource<T> (view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceView() => Found(View);

        [Benchmark(Description = "GetResource<T> (materialized view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceMaterialized() => Found(View);

        [Benchmark(Description = "GetResource<T> (baseline model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceBaseline() => Found(Baseline);

        [Benchmark(Description = "GetResource<T> (group: baseline + additions)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceGroup() => Found(_group);

        private int Found(IModel model)
        {
            var found = 0;

            foreach (var i in NextIndices())
            {
                found += model.GetResource<BenchmarkPerson>(PersonUri(i)) != null ? 1 : 0;
            }

            return Expect(found, Lookups, "GetResource<T>");
        }

        // --- ContainsResource -------------------------------------------------------------------

        [Benchmark(Description = "ContainsResource (view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsView() => Contains(View);

        [Benchmark(Description = "ContainsResource (materialized view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsMaterialized() => Contains(View);

        [Benchmark(Description = "ContainsResource (baseline model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsBaseline() => Contains(Baseline);

        [Benchmark(Description = "ContainsResource (group: baseline + additions)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsGroup() => Contains(_group);

        private int Contains(IModel model)
        {
            var found = 0;

            foreach (var i in NextIndices())
            {
                found += model.ContainsResource(PersonUri(i)) ? 1 : 0;
            }

            return Expect(found, Lookups, "ContainsResource");
        }

        // --- A caller's selective query: one subject -------------------------------------------
        //
        // Through the view, a subject whose name is staged for removal has no name, so the expected
        // count depends on which subjects this invocation drew.

        [Benchmark(Description = "caller query, selective (view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("CallerSelective")]
        public int SelectiveView() => Selective(View, subtracts: true);

        [Benchmark(Description = "caller query, selective (materialized view)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("CallerSelective")]
        public int SelectiveMaterialized() => Selective(View, subtracts: true);

        [Benchmark(Description = "caller query, selective (baseline model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("CallerSelective")]
        public int SelectiveBaseline() => Selective(Baseline, subtracts: false);

        [Benchmark(Description = "caller query, selective (group: baseline + additions)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("CallerSelective")]
        public int SelectiveGroup() => Selective(_group, subtracts: false);

        /// <summary>
        /// One subject's name per lookup. Written without a dataset clause: the model it runs through
        /// supplies one, and a view refuses a caller <c>FROM</c> (ADR-0041).
        /// </summary>
        private int Selective(IModel model, bool subtracts)
        {
            var total = 0;
            var expected = 0;

            foreach (var i in NextIndices())
            {
                var query = new SparqlQuery(
                    $"SELECT ?n WHERE {{ <{PersonUri(i)}> <{Vocabulary.FirstNameProperty}> ?n }}",
                    declarePrefixes: false);

                total += model.ExecuteQuery(query).GetBindings().Count();
                expected += subtracts && IsRemovedName(i) ? 0 : 1;
            }

            return Expect(total, expected, "selective caller query");
        }

        // --- A caller's unselective query: every name ------------------------------------------

        [Benchmark(Description = "caller query, unselective scan (view)")]
        [BenchmarkCategory("CallerScan")]
        public int ScanView() => Scan(View, People - RemovedNames + AddedPeople);

        [Benchmark(Description = "caller query, unselective scan (materialized view)")]
        [BenchmarkCategory("CallerScan")]
        public int ScanMaterialized() => Scan(View, People - RemovedNames + AddedPeople);

        [Benchmark(Description = "caller query, unselective scan (baseline model)", Baseline = true)]
        [BenchmarkCategory("CallerScan")]
        public int ScanBaseline() => Scan(Baseline, People);

        [Benchmark(Description = "caller query, unselective scan (group: baseline + additions)")]
        [BenchmarkCategory("CallerScan")]
        public int ScanGroup() => Scan(_group, People + AddedPeople);

        private int Scan(IModel model, int expected)
        {
            var query = new SparqlQuery(
                $"SELECT ?s ?n WHERE {{ ?s <{Vocabulary.FirstNameProperty}> ?n }}", declarePrefixes: false);

            return Expect(model.ExecuteQuery(query).GetBindings().Count(), expected, "unselective caller query");
        }
    }

    /// <summary>
    /// <see cref="LayeredReadBenchmarks"/> at the 1,000,000-triple baseline ADR-0041 measured at.
    /// Opt-in with <c>--large</c>.
    /// </summary>
    /// <remarks>
    /// Virtuoso's materialized rows fail in setup, as they do at 10k and 100k: past 10,000 effective
    /// triples a view cannot be materialized there (#70). The rewriting rows are unaffected.
    /// </remarks>
    [BenchmarkCategory(Program.LargeCategory)]
    public class LayeredReadLargeBenchmarks : LayeredReadBenchmarks
    {
        protected override IEnumerable<int> BaselineSizes => new[] { 1_000_000 };
    }
}
