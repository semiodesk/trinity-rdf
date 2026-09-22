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
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// How many resources to put in one request.
    /// </summary>
    /// <remarks>
    /// The total written is held constant and only the batch size varies, so a row answers "what
    /// does this batch size cost me for the same work". It is a real axis rather than a synthetic
    /// one: <c>StoreBase.UpdateResources</c> genuinely batches -- it accumulates every resource into
    /// at most two SPARQL updates regardless of how many there are -- so the mapped path has a
    /// batching API, and per-resource <c>Commit()</c> is the pessimal use of it.
    ///
    /// Resources are built directly rather than through <c>Model.CreateResource</c>, which issues an
    /// ASK per resource to refuse a duplicate. That is a round trip per resource in what is only
    /// fixture setup: at 1000 resources across the whole matrix it is some 440,000 requests, dwarfing
    /// the writes being measured and taking hours. <c>SetModel</c> and <c>IsNew</c> are public, so a
    /// resource can be handed to the store without asking it anything first.
    /// </remarks>
    public class BatchWriteBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Total resources written per invocation, the same for every batch size.
        /// </summary>
        private const int Total = 1000;

        /// <summary>
        /// Resources per request.
        /// </summary>
        [Params(1, 10, 100, 1000)]
        public int BatchSize { get; set; }

        private List<BenchmarkPerson[]> _batches;

        [IterationSetup]
        public void IterationSetup()
        {
            Model.Clear();

            var people = new List<BenchmarkPerson>(Total);

            for (var i = 0; i < Total; i++)
            {
                // No CreateResource: its existence check is a round trip per resource, and this is
                // setup. IsNew = true is what routes the write down the wholesale branch, as a
                // freshly created resource would.
                var person = new BenchmarkPerson(BaseUri.GetUriRef($"person-{i}"));

                person.SetModel(Model);
                person.IsNew = true;
                person.FirstName = $"Person {i}";

                people.Add(person);
            }

            _batches = people
                .Select((person, index) => (person, index))
                .GroupBy(x => x.index / BatchSize)
                .Select(g => g.Select(x => x.person).ToArray())
                .ToList();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            AssertWrote(Total * 2);
        }

        /// <summary>
        /// The mapped batch API: at most two updates per batch, whatever its size.
        /// </summary>
        [Benchmark(Description = "UpdateResources (mapped batch)")]
        public void UpdateResourcesMapped()
        {
            foreach (var batch in _batches)
            {
                Store.UpdateResources(batch, Model.Uri);
            }
        }

        /// <summary>
        /// The same batching by hand. The baseline.
        /// </summary>
        [Benchmark(Description = "INSERT DATA (raw batch)", Baseline = true)]
        public void InsertRawBatched()
        {
            foreach (var batch in _batches)
            {
                var triples = new StringBuilder();

                foreach (var person in batch)
                {
                    triples
                        .Append('<').Append(person.Uri).Append("> a <")
                        .Append(Vocabulary.PersonClass).Append(">; <")
                        .Append(Vocabulary.FirstNameProperty).Append("> \"")
                        .Append(person.FirstName).Append("\" . ");
                }

                Store.ExecuteNonQuery(new SparqlUpdate(
                    $"INSERT DATA {{ GRAPH <{Model.Uri}> {{ {triples} }} }}"));
            }
        }
    }
}
