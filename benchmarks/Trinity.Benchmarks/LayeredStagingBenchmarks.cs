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
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Writing through a layered view: staging, deleting, accepting and discarding (ADR-0042).
    /// </summary>
    /// <remarks>
    /// Reproduces the maintenance figures ADR-0042 measured on a 1,000,000-triple in-memory baseline:
    /// <b>0.7 ms</b> to stage a change and keep a materialized graph in step, and <c>DeleteResource</c>
    /// through a materialized view at <b>3 ms</b> -- down from 12.5 s before the object side became its
    /// own bound pattern. Both are claims that staging is O(changes), not O(baseline), so
    /// <see cref="LayeredBenchmarkBase.BaselineTriples"/> is the axis to read them along: a row that
    /// grows with the baseline has regressed to a scan. The 1M row is behind <c>--large</c>.
    ///
    /// <b>Stage</b> and <b>Delete</b> each pair the mapped call with its raw baseline: the minimal writes
    /// the operation must make by hand -- the old value into the removals graph, the new one into the
    /// additions, and for a materialized view the same change applied to the fourth graph. So the Ratio
    /// is what Trinity's staging costs over the writes it cannot avoid.
    ///
    /// <b>Accept</b> and <b>Discard</b> have no raw equivalent worth the name and stand alone. A clean
    /// <c>Accept()</c> needs no rebuild; <c>Discard()</c> of a materialized view does, since the
    /// effective graph reverts to the baseline.
    ///
    /// Stage and Delete never revisit a resource within a run, so no iteration needs to undo the one
    /// before it -- which would mean a rebuild per iteration at the sizes that matter. Each iteration
    /// tags its values, and the cleanup counts that tag to prove the writes landed.
    /// </remarks>
    public class LayeredStagingBenchmarks : LayeredBenchmarkBase
    {
        /// <summary>
        /// Resources staged per invocation, for Stage and Delete.
        /// </summary>
        private const int Batch = 10;

        /// <summary>
        /// Changes staged before each Accept and Discard.
        /// </summary>
        private const int Changeset = 100;

        protected override IEnumerable<int> BaselineSizes => new[] { 10_000, 100_000 };

        /// <summary>
        /// Whether the view keeps a materialized graph in step.
        /// </summary>
        [Params(false, true)]
        public bool Materialized { get; set; }

        protected override bool UseMaterialized => Materialized;

        protected override string GraphPrefix => "layered-staging";

        private int _iteration;

        private int _cursor;

        private int[] _batch;

        private BenchmarkPerson[] _resources;

        private int _accepts;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            _iteration = 0;
            _accepts = 0;

            // Past every resource the base changeset touches, so a staged value never lands on a
            // resource whose name is already staged for removal.
            _cursor = 1;
        }

        /// <summary>
        /// The next <paramref name="count"/> resources no earlier iteration of this case has touched.
        /// </summary>
        private int[] NextBatch(int count)
        {
            var batch = new int[count];

            for (var k = 0; k < count; k++)
            {
                if (_cursor % (People / RemovedNames) == 0)
                {
                    _cursor++;
                }

                if (_cursor >= People)
                {
                    throw new InvalidOperationException(
                        $"{Backend}: ran out of untouched resources after {_iteration} iterations; raise the "
                        + "baseline size or lower the iteration count.");
                }

                batch[k] = _cursor++;
            }

            return batch;
        }

        private string Tag => $"#{_iteration}";

        // --- Stage ------------------------------------------------------------------------------

        [IterationSetup(Target = nameof(StageMapped))]
        public void ReadForStaging()
        {
            _iteration++;
            _batch = NextBatch(Batch);
            _resources = _batch.Select(i => View.GetResource<BenchmarkPerson>(PersonUri(i))).ToArray();
        }

        [IterationSetup(Target = nameof(StageRaw))]
        public void NextRawBatch()
        {
            _iteration++;
            _batch = NextBatch(Batch);
        }

        [IterationCleanup(Targets = new[] { nameof(StageMapped), nameof(StageRaw) })]
        public void VerifyStaged()
        {
            var pattern = $"?s <{Vocabulary.FirstNameProperty}> ?n . FILTER (STRENDS(STR(?n), \"{Tag}\"))";

            RequireCount(Additions.Uri, pattern, Batch, "additions");

            // The old value has to reach the removals graph too. If it did not, the view would read both
            // names for a single-valued property -- the ADR-0042 hazard a mapped read then hides by
            // picking one -- and a count of the additions alone would still pass.
            var replaced = ReplacedNamesPattern();

            RequireCount(Removals.Uri, replaced, Batch, "removals (the replaced names)");

            if (Materialized)
            {
                RequireCount(MaterializedGraph.Uri, pattern, Batch, "materialized graph");
                RequireCount(MaterializedGraph.Uri, replaced, 0, "materialized graph (the replaced names)");
            }
        }

        /// <summary>
        /// The original name triples of the resources in the current batch.
        /// </summary>
        private string ReplacedNamesPattern()
        {
            var rows = string.Join(" ", _batch.Select(i => $"(<{PersonUri(i).OriginalString}> \"Person {i}\")"));

            return $"VALUES (?s ?n) {{ {rows} }} ?s <{Vocabulary.FirstNameProperty}> ?n";
        }

        /// <summary>
        /// Change one literal on each resource and commit it: through a view, <c>Commit()</c> stages.
        /// </summary>
        [Benchmark(Description = "set literal + Commit() (view stages)", OperationsPerInvoke = Batch)]
        [BenchmarkCategory("Stage")]
        public void StageMapped()
        {
            for (var k = 0; k < Batch; k++)
            {
                _resources[k].FirstName = $"Staged {_batch[k]} {Tag}";
                _resources[k].Commit();
            }
        }

        [Benchmark(Description = "INSERT/DELETE DATA into the layers (raw)", Baseline = true, OperationsPerInvoke = Batch)]
        [BenchmarkCategory("Stage")]
        public void StageRaw()
        {
            foreach (var i in _batch)
            {
                var old = $"<{PersonUri(i)}> <{Vocabulary.FirstNameProperty}> \"Person {i}\"";
                var @new = $"<{PersonUri(i)}> <{Vocabulary.FirstNameProperty}> \"Staged {i} {Tag}\"";

                var update = new StringBuilder()
                    .Append($"INSERT DATA {{ GRAPH <{Removals.Uri}> {{ {old} }} GRAPH <{Additions.Uri}> {{ {@new} }} }}");

                if (Materialized)
                {
                    update.Append($"; DELETE DATA {{ GRAPH <{MaterializedGraph.Uri}> {{ {old} }} }}")
                        .Append($"; INSERT DATA {{ GRAPH <{MaterializedGraph.Uri}> {{ {@new} }} }}");
                }

                Store.ExecuteNonQuery(new SparqlUpdate(update.ToString()));
            }
        }

        // --- Delete -----------------------------------------------------------------------------

        [IterationSetup(Targets = new[] { nameof(DeleteMapped), nameof(DeleteRaw) })]
        public void NextVictims()
        {
            _iteration++;
            _batch = NextBatch(Batch);
        }

        [IterationCleanup(Targets = new[] { nameof(DeleteMapped), nameof(DeleteRaw) })]
        public void VerifyDeleted()
        {
            foreach (var i in _batch)
            {
                if (View.ContainsResource(PersonUri(i)))
                {
                    throw new InvalidOperationException(
                        $"{Backend}: <{PersonUri(i)}> is still readable through the view after its delete was "
                        + "staged. A delete that did nothing is timed as a fast one.");
                }

                // Both sides (ADR-0030). ContainsResource only asks about the victim as a subject, and the
                // object side -- links pointing at it -- is exactly what ADR-0042's two-bound-patterns
                // fix changed. Every baseline triple mentioning the victim must be staged for removal,
                // and, for a materialized view, gone from the fourth graph.
                var victim = $"<{PersonUri(i).OriginalString}>";

                RequireCount(Removals.Uri, $"{victim} ?p ?o", CountWhere(Baseline.Uri, $"{victim} ?p ?o"),
                    "removals (subject side of a deleted resource)");
                RequireCount(Removals.Uri, $"?s ?p {victim}", CountWhere(Baseline.Uri, $"?s ?p {victim}"),
                    "removals (object side of a deleted resource)");

                if (Materialized)
                {
                    RequireCount(MaterializedGraph.Uri, $"{victim} ?p ?o", 0, "materialized graph (subject side)");
                    RequireCount(MaterializedGraph.Uri, $"?s ?p {victim}", 0, "materialized graph (object side)");
                }
            }
        }

        /// <summary>
        /// Delete each resource through the view, which stages every triple mentioning it, on either
        /// side, for removal -- and for a materialized view removes them from the fourth graph too.
        /// </summary>
        [Benchmark(Description = "DeleteResource (view stages)", OperationsPerInvoke = Batch)]
        [BenchmarkCategory("Delete")]
        public void DeleteMapped()
        {
            foreach (var i in _batch)
            {
                View.DeleteResource(PersonUri(i));
            }
        }

        /// <summary>
        /// The same staging by hand, as the two bound patterns ADR-0042 found an index can answer.
        /// </summary>
        [Benchmark(Description = "INSERT ... WHERE into removals, two bound patterns (raw)", Baseline = true, OperationsPerInvoke = Batch)]
        [BenchmarkCategory("Delete")]
        public void DeleteRaw()
        {
            foreach (var i in _batch)
            {
                var r = $"<{PersonUri(i)}>";

                var update = new StringBuilder()
                    .Append($"INSERT {{ GRAPH <{Removals.Uri}> {{ {r} ?p ?o }} }} WHERE {{ GRAPH <{Baseline.Uri}> {{ {r} ?p ?o }} }}; ")
                    .Append($"INSERT {{ GRAPH <{Removals.Uri}> {{ ?s ?p {r} }} }} WHERE {{ GRAPH <{Baseline.Uri}> {{ ?s ?p {r} }} }}");

                if (Materialized)
                {
                    update.Append($"; DELETE WHERE {{ GRAPH <{MaterializedGraph.Uri}> {{ {r} ?p ?o }} }}")
                        .Append($"; DELETE WHERE {{ GRAPH <{MaterializedGraph.Uri}> {{ ?s ?p {r} }} }}");
                }

                Store.ExecuteNonQuery(new SparqlUpdate(update.ToString()));
            }
        }

        // --- Accept -----------------------------------------------------------------------------

        /// <summary>
        /// Stages a changeset for <see cref="Accept"/> to apply, outside the timed region.
        /// </summary>
        /// <remarks>
        /// The same resources every time, alternating between a new value and the original one, so the
        /// baseline oscillates rather than drifting and no iteration finds its precondition broken.
        /// The first Accept also applies the base changeset, which is why the cleanup clears the
        /// baseline for the next case to reseed.
        /// </remarks>
        [IterationSetup(Target = nameof(Accept))]
        public void StageForAccept()
        {
            StageChangeset(i => _accepts % 2 == 0 ? $"Accepted {i}" : $"Person {i}");
        }

        [IterationCleanup(Target = nameof(Accept))]
        public void VerifyAccepted()
        {
            RequireCount(Additions.Uri, "?s ?p ?o", 0, "additions after Accept");
            RequireCount(Removals.Uri, "?s ?p ?o", 0, "removals after Accept");

            var expected = _accepts % 2 == 0 ? "Accepted" : "Person";

            RequireCount(Baseline.Uri,
                $"?s <{Vocabulary.FirstNameProperty}> ?n . FILTER (STRSTARTS(STR(?n), \"{expected} \"))",
                expected == "Accepted" ? Changeset : People - RemovedNames, "accepted values in the baseline");

            _accepts++;
        }

        [GlobalCleanup(Target = nameof(Accept))]
        public void CleanupAfterAccept()
        {
            // Accept changed the baseline, so the next case must not mistake it for a fresh one.
            Baseline?.Clear();

            GlobalCleanup();
        }

        [Benchmark(Description = "Accept() (clean)")]
        [BenchmarkCategory("Accept")]
        public void Accept()
        {
            View.Accept();
        }

        // --- Discard ----------------------------------------------------------------------------

        [IterationSetup(Target = nameof(Discard))]
        public void StageForDiscard()
        {
            StageChangeset(i => $"Discarded {i}");
        }

        [IterationCleanup(Target = nameof(Discard))]
        public void VerifyDiscarded()
        {
            RequireCount(Additions.Uri, "?s ?p ?o", 0, "additions after Discard");
            RequireCount(Removals.Uri, "?s ?p ?o", 0, "removals after Discard");

            if (Materialized)
            {
                // By content: a rebuild that skipped the removals would have the baseline's names and
                // the wrong count, one that skipped the additions the right count only by accident.
                AssertBaselineOnly(MaterializedGraph.Uri, "the materialized graph after Discard");
            }
        }

        /// <summary>
        /// Abandon the changeset. For a materialized view this is a full rebuild.
        /// </summary>
        [Benchmark(Description = "Discard()")]
        [BenchmarkCategory("Discard")]
        public void Discard()
        {
            View.Discard();
        }

        /// <summary>
        /// Stages a new name on <see cref="Changeset"/> resources through the view.
        /// </summary>
        private void StageChangeset(Func<int, string> name)
        {
            for (var j = 0; j < Changeset; j++)
            {
                // Offset by one from the base changeset's removed names, so these resources have a name
                // to change and the counts in the cleanups are exact.
                var i = RemovedIndex(j) + 1;
                var person = View.GetResource<BenchmarkPerson>(PersonUri(i));

                person.FirstName = name(i);
                person.Commit();
            }
        }

        private void RequireCount(Uri graph, string pattern, int expected, string what)
        {
            var actual = CountWhere(graph, pattern);

            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"{Backend}: expected {expected} in the {what} <{graph}> but found {actual}. The operation "
                    + "being timed did not do the work it claims.");
            }
        }
    }

    /// <summary>
    /// <see cref="LayeredStagingBenchmarks"/> at the 1,000,000-triple baseline ADR-0042 measured at.
    /// Opt-in with <c>--large</c>.
    /// </summary>
    /// <remarks>
    /// Virtuoso's materialized cells fail in setup, as at 10k and 100k (#70).
    /// A materialized <c>Discard()</c> rebuilds the whole graph per iteration here, so expect minutes.
    /// </remarks>
    [BenchmarkCategory(Program.LargeCategory)]
    public class LayeredStagingLargeBenchmarks : LayeredStagingBenchmarks
    {
        protected override IEnumerable<int> BaselineSizes => new[] { 1_000_000 };
    }

    /// <summary>
    /// Building a materialized view from scratch: <c>Refresh()</c> (ADR-0042).
    /// </summary>
    /// <remarks>
    /// ADR-0042 measured the full build on a 1M in-memory baseline at <b>31.5 s</b> including the
    /// verification that caught Virtuoso writing zero, and 19.1 s without it. The raw baseline is the
    /// cheapest build conceivable -- copy the baseline graph into the fourth one, ignoring the layers --
    /// so the Ratio is what the overlay and the verification cost over a plain copy. ADR-0041 measured
    /// that copy on the server stores at 3.9-6.0 s for 1M.
    ///
    /// The raw copy is verified too. On Virtuoso every case in this class fails in setup: past 10,000
    /// effective triples, Virtuoso refuses the materializing <c>INSERT … WHERE</c>, the error is
    /// swallowed (#50), and <c>Refresh()</c> finds nothing written (#70). The cells are left failing
    /// so the defect stays visible.
    /// </remarks>
    public class LayeredMaterializeBenchmarks : LayeredBenchmarkBase
    {
        protected override IEnumerable<int> BaselineSizes => new[] { 10_000, 100_000 };

        protected override bool UseMaterialized => true;

        protected override string GraphPrefix => "layered-materialize";

        [IterationCleanup(Target = nameof(Refresh))]
        public void VerifyRefresh()
        {
            AssertEffective(MaterializedGraph.Uri, "the materialized graph after Refresh()");
        }

        [IterationCleanup(Target = nameof(CopyRaw))]
        public void VerifyCopy()
        {
            AssertBaselineOnly(MaterializedGraph.Uri, "the copied graph");
        }

        /// <summary>
        /// Rebuild the effective graph from the three layers, then verify it.
        /// </summary>
        [Benchmark(Description = "Refresh() (overlay + verification)")]
        public void Refresh()
        {
            View.Refresh();
        }

        [Benchmark(Description = "copy baseline into the graph (raw)", Baseline = true)]
        public void CopyRaw()
        {
            Store.ExecuteNonQuery(new SparqlUpdate(
                $"DELETE WHERE {{ GRAPH <{MaterializedGraph.Uri}> {{ ?s ?p ?o }} }}; "
                + $"INSERT {{ GRAPH <{MaterializedGraph.Uri}> {{ ?s ?p ?o }} }} WHERE {{ GRAPH <{Baseline.Uri}> {{ ?s ?p ?o }} }}"));
        }
    }

    /// <summary>
    /// <see cref="LayeredMaterializeBenchmarks"/> at 1,000,000 triples: ADR-0042's 31.5 s. Opt-in with
    /// <c>--large</c>. Virtuoso fails in setup, as at every size here (#70).
    /// </summary>
    [BenchmarkCategory(Program.LargeCategory)]
    public class LayeredMaterializeLargeBenchmarks : LayeredMaterializeBenchmarks
    {
        protected override IEnumerable<int> BaselineSizes => new[] { 1_000_000 };
    }
}
