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
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// The fixture every layered-view workload shares (ADR-0041/0042): a baseline of
    /// <see cref="BaselineTriples"/>, a small staged changeset over it, and the view.
    /// </summary>
    /// <remarks>
    /// Shaped after the setup ADR-0041 measured its read ratios on: a large baseline and "~100 staged
    /// additions and ~100 staged removals". Here that is <see cref="AddedPeople"/> new resources (two
    /// triples each) in the additions graph, and the <c>foaf:firstName</c> of
    /// <see cref="RemovedNames"/> baseline resources in the removals graph.
    ///
    /// The baseline is a ring (<see cref="BenchmarkData.SeedRing"/>), five triples per resource.
    ///
    /// <b>The baseline is kept between benchmark cases.</b> BenchmarkDotNet runs <c>[GlobalSetup]</c>
    /// once per case -- per method, per parameter combination -- and reseeding a million triples for
    /// each would dominate a run. So the baseline graph is named after its size, left in place by
    /// <see cref="GlobalCleanup"/>, and reseeded only when its triple count is not what it should be.
    /// A workload that changes the baseline (<c>Accept</c>) must clear it afterwards so the next case
    /// reseeds. The in-memory store is created afresh for every case, so it reseeds regardless. The
    /// layer graphs are rebuilt for every case, since they are small and the staging workloads change
    /// them.
    /// </remarks>
    public abstract class LayeredBenchmarkBase : StoreBenchmarkBase
    {
        /// <summary>
        /// Outgoing links per baseline resource, so each is five triples.
        /// </summary>
        protected const int Links = 3;

        /// <summary>
        /// Resources staged as additions, two triples each.
        /// </summary>
        protected const int AddedPeople = 50;

        /// <summary>
        /// Baseline <c>foaf:firstName</c> triples staged for removal.
        /// </summary>
        protected const int RemovedNames = 100;

        /// <summary>
        /// The <c>ParamsSource</c> for <see cref="BaselineTriples"/>.
        /// </summary>
        /// <remarks>
        /// Not virtual, deliberately: BenchmarkDotNet resolves a source by name across the whole type
        /// hierarchy and throws when it finds more than one, which an override is. Subclasses vary the
        /// sizes through <see cref="BaselineSizes"/> instead.
        /// </remarks>
        public IEnumerable<int> Sizes => BaselineSizes;

        /// <summary>
        /// The baseline sizes this class runs at. The <c>Large</c> subclasses override it.
        /// </summary>
        protected abstract IEnumerable<int> BaselineSizes { get; }

        /// <summary>
        /// Triples in the baseline graph.
        /// </summary>
        [ParamsSource(nameof(Sizes))]
        public int BaselineTriples { get; set; }

        /// <summary>
        /// Resources in the baseline.
        /// </summary>
        protected int People => BaselineTriples / (2 + Links);

        /// <summary>
        /// What the view reads: the baseline, less the removals, plus the additions.
        /// </summary>
        protected int EffectiveTriples => BaselineTriples - RemovedNames + AddedPeople * 2;

        /// <summary>
        /// Whether the view keeps its effective triples in a fourth graph (ADR-0042).
        /// </summary>
        protected abstract bool UseMaterialized { get; }

        /// <summary>
        /// Distinguishes the graphs of one workload from another's, so a workload that changes its
        /// baseline cannot leave a stale one behind for a workload that only reads.
        /// </summary>
        protected abstract string GraphPrefix { get; }

        protected IModel Baseline;

        protected IModel Additions;

        protected IModel Removals;

        protected IModel MaterializedGraph;

        protected ILayeredModel View;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            Baseline = Store.GetModel(BaseUri.GetUriRef($"{GraphPrefix}-baseline-{BaselineTriples}"));
            Additions = Store.GetModel(BaseUri.GetUriRef($"{GraphPrefix}-additions"));
            Removals = Store.GetModel(BaseUri.GetUriRef($"{GraphPrefix}-removals"));
            MaterializedGraph = Store.GetModel(BaseUri.GetUriRef($"{GraphPrefix}-materialized"));

            EnsureBaseline();

            Additions.Clear();
            Removals.Clear();
            MaterializedGraph.Clear();

            StageChangeset();

            // Materialization builds the fourth graph here, before the first iteration, so no read row
            // pays for it. Virtuoso at 1M throws from this call, by design: see ADR-0042 and the README.
            View = Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri,
                UseMaterialized ? MaterializedGraph.Uri : null);

            if (UseMaterialized)
            {
                AssertSeeded(EffectiveTriples, MaterializedGraph.Uri);
            }
        }

        public override void GlobalCleanup()
        {
            // The baseline stays, for the next case of the same size; see the class remarks.
            Additions?.Clear();
            Removals?.Clear();
            MaterializedGraph?.Clear();

            base.GlobalCleanup();
        }

        /// <summary>
        /// The index of the <paramref name="j"/>th baseline resource whose name is staged for removal:
        /// spread through the ring rather than adjacent.
        /// </summary>
        protected int RemovedIndex(int j)
        {
            return j * (People / RemovedNames);
        }

        /// <summary>
        /// The URI of the <paramref name="j"/>th staged addition.
        /// </summary>
        protected UriRef AddedUri(int j)
        {
            return BaseUri.GetUriRef($"added-{j}");
        }

        private void EnsureBaseline()
        {
            if (CountTriples(Baseline.Uri) == BaselineTriples)
            {
                return;
            }

            Baseline.Clear();

            BenchmarkData.SeedRing(Store, Baseline.Uri, People, Links, PersonUri);

            AssertSeeded(BaselineTriples, Baseline.Uri);
        }

        /// <summary>
        /// Writes the changeset straight into the layer graphs, not through the view: it is the state
        /// being read, not an operation being measured, and the staging workloads measure the other.
        /// </summary>
        private void StageChangeset()
        {
            BenchmarkData.Seed(Store, Additions.Uri, AddedPeople,
                (buffer, j) => BenchmarkData.AppendPerson(buffer, AddedUri(j), $"Added {j}"));

            BenchmarkData.Seed(Store, Removals.Uri, RemovedNames, (buffer, j) =>
            {
                var i = RemovedIndex(j);

                buffer.Append('<').Append(PersonUri(i).OriginalString).Append("> <")
                    .Append(Vocabulary.FirstNameProperty).Append("> \"Person ").Append(i).Append("\" .\n");

                return 1;
            });

            AssertSeeded(AddedPeople * 2, Additions.Uri);
            AssertSeeded(RemovedNames, Removals.Uri);
        }
    }
}
