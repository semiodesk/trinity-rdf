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
    /// Changing resources that already exist: the per-value delta write of ADR-0039.
    /// </summary>
    /// <remarks>
    /// <see cref="WriteBenchmarks"/> only ever creates, which takes the wholesale insert branch of
    /// <c>StoreBase.UpdateResource</c>. An application mostly edits, and an edit takes the other
    /// branch: <c>Commit()</c> diffs the resource against the snapshot taken when it was read and
    /// writes only what changed. That diff, and the update it produces, is what this class measures.
    ///
    /// Two edits, each its own category with its own raw baseline:
    /// <list type="bullet">
    /// <item><b>Literal</b>: replace one literal. The raw equivalent is the <c>DELETE DATA</c> +
    /// <c>INSERT DATA</c> pair a caller would write.</item>
    /// <item><b>Link</b>: add one resource to <c>Knows</c>. This is a read-modify-write: touching the
    /// collection lazy-loads its current members first (ADR-0023, through the VALUES query of
    /// ADR-0046), so the row carries a read the raw <c>INSERT DATA</c> does not. That is the point --
    /// it is what adding a link costs a caller of the mapped API.</item>
    /// </list>
    ///
    /// The resources are read in <c>[IterationSetup]</c>, outside the timed region, so the rows are
    /// the edit and the commit and not the load that precedes them. The model is reseeded every
    /// iteration, so each one edits the same starting state.
    /// </remarks>
    public class UpdateBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Resources edited per invocation; the reported time is per resource.
        /// </summary>
        private const int People = 100;

        /// <summary>
        /// Existing <c>foaf:knows</c> links per resource, which a link edit loads before it adds one.
        /// </summary>
        private const int Links = 5;

        private const int Seeded = People * (2 + Links);

        private BenchmarkPerson[] _people;

        [IterationSetup]
        public void IterationSetup()
        {
            Model.Clear();

            BenchmarkData.SeedRing(Store, Model.Uri, People, Links, PersonUri);

            var byUri = Model.GetResources<BenchmarkPerson>().ToDictionary(p => p.Uri.OriginalString);

            _people = Enumerable.Range(0, People).Select(i => byUri[PersonUri(i).OriginalString]).ToArray();
        }

        [IterationCleanup(Targets = new[] { nameof(CommitLiteral), nameof(RawLiteral) })]
        public void VerifyLiteral()
        {
            AssertWrote(Seeded);

            var updated = CountWhere(Model.Uri,
                $"?s <{Vocabulary.FirstNameProperty}> ?n . FILTER (STRSTARTS(STR(?n), \"Updated\"))");

            if (updated != People)
            {
                throw new InvalidOperationException(
                    $"{Backend}: {updated} of {People} literals were updated. A commit that wrote nothing "
                    + "would be timed as a fast one.");
            }
        }

        [IterationCleanup(Targets = new[] { nameof(CommitLink), nameof(RawLink) })]
        public void VerifyLink()
        {
            AssertWrote(Seeded + People);
        }

        [Benchmark(Description = "set literal + Commit() (mapped delta)", OperationsPerInvoke = People)]
        [BenchmarkCategory("Literal")]
        public void CommitLiteral()
        {
            for (var i = 0; i < People; i++)
            {
                _people[i].FirstName = $"Updated {i}";
                _people[i].Commit();
            }
        }

        [Benchmark(Description = "DELETE DATA + INSERT DATA (raw)", Baseline = true, OperationsPerInvoke = People)]
        [BenchmarkCategory("Literal")]
        public void RawLiteral()
        {
            for (var i = 0; i < People; i++)
            {
                var subject = PersonUri(i);

                Store.ExecuteNonQuery(new SparqlUpdate(
                    $"DELETE DATA {{ GRAPH <{Model.Uri}> {{ <{subject}> <{Vocabulary.FirstNameProperty}> \"Person {i}\" }} }}; "
                    + $"INSERT DATA {{ GRAPH <{Model.Uri}> {{ <{subject}> <{Vocabulary.FirstNameProperty}> \"Updated {i}\" }} }}"));
            }
        }

        [Benchmark(Description = "Knows.Add + Commit() (mapped, lazy-loads first)", OperationsPerInvoke = People)]
        [BenchmarkCategory("Link")]
        public void CommitLink()
        {
            for (var i = 0; i < People; i++)
            {
                // One past the ring's existing links, so the new link is never a duplicate.
                _people[i].Knows.Add(_people[(i + Links + 1) % People]);
                _people[i].Commit();
            }
        }

        [Benchmark(Description = "INSERT DATA one link (raw)", Baseline = true, OperationsPerInvoke = People)]
        [BenchmarkCategory("Link")]
        public void RawLink()
        {
            for (var i = 0; i < People; i++)
            {
                Store.ExecuteNonQuery(new SparqlUpdate(
                    $"INSERT DATA {{ GRAPH <{Model.Uri}> {{ <{PersonUri(i)}> <{Vocabulary.KnowsProperty}> "
                    + $"<{PersonUri((i + Links + 1) % People)}> }} }}"));
            }
        }
    }
}
