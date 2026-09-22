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

using System.Text;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Creating and committing resources: the mapped path against the SPARQL it stands in for.
    /// </summary>
    /// <remarks>
    /// The two benchmarks write the same triples by the two routes a caller has. The raw one is the
    /// baseline, so BenchmarkDotNet's ratio column reads directly as what the mapping costs on top
    /// of the update the store would have received anyway. Comparing backends is the obvious use of
    /// this table; comparing the two rows within one backend is the more useful one.
    /// </remarks>
    public class WriteBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Resources written per invocation.
        /// </summary>
        [Params(100, 1000)]
        public int Count { get; set; }

        /// <summary>
        /// Two triples per resource: rdf:type and foaf:firstName.
        /// </summary>
        private int ExpectedTriples => Count * 2;

        [IterationSetup]
        public void IterationSetup()
        {
            // Outside the measurement. Each invocation must start from the same empty model, or the
            // later iterations measure a growing store rather than the operation.
            Model.Clear();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            AssertWrote(ExpectedTriples);
        }

        /// <summary>
        /// The mapped path: create a resource, set a property, commit.
        /// </summary>
        [Benchmark(Description = "Commit() (mapped)")]
        public void CommitMapped()
        {
            for (var i = 0; i < Count; i++)
            {
                var person = Model.CreateResource<BenchmarkPerson>(BaseUri.GetUriRef($"person-{i}"));

                person.FirstName = $"Person {i}";
                person.Commit();
            }
        }

        /// <summary>
        /// The same triples as <c>Count</c> separate hand-written updates: one request per resource,
        /// exactly as the mapped path issues them.
        /// </summary>
        /// <remarks>
        /// This is the row that isolates what Trinity costs. It does the same number of round trips
        /// as <see cref="CommitMapped"/>, so the difference between the two is mapping, change
        /// tracking and serialization -- and nothing else. Against the batched baseline below, the
        /// difference is round trips.
        ///
        /// Without this row the table invites a wrong conclusion: the mapped-to-batched ratio looks
        /// like library overhead when most of it is <c>Count</c> requests against one.
        /// </remarks>
        [Benchmark(Description = "INSERT DATA x N (raw, same round trips)")]
        public void InsertRawPerResource()
        {
            for (var i = 0; i < Count; i++)
            {
                var subject = BaseUri.GetUriRef($"person-{i}");

                Store.ExecuteNonQuery(new SparqlUpdate(
                    $"INSERT DATA {{ GRAPH <{Model.Uri}> {{ <{subject}> a <{Vocabulary.PersonClass}>; "
                    + $"<{Vocabulary.FirstNameProperty}> \"Person {i}\" . }} }}"));
            }
        }

        /// <summary>
        /// The same triples as one hand-written update. The baseline.
        /// </summary>
        /// <remarks>
        /// One request rather than <c>Count</c> of them: what a caller writing SPARQL by hand would
        /// actually do, so the ratio column answers "what does using the mapper cost me against the
        /// best I could do by hand". Read it together with the per-resource row above, which splits
        /// that cost into mapping and round trips.
        /// </remarks>
        [Benchmark(Description = "INSERT DATA (raw, one batch)", Baseline = true)]
        public void InsertRaw()
        {
            var triples = new StringBuilder();

            for (var i = 0; i < Count; i++)
            {
                triples
                    .Append('<').Append(BaseUri.GetUriRef($"person-{i}")).Append("> a <")
                    .Append(Vocabulary.PersonClass).Append(">; <")
                    .Append(Vocabulary.FirstNameProperty).Append("> \"Person ").Append(i).Append("\" . ");
            }

            Store.ExecuteNonQuery(new SparqlUpdate(
                $"INSERT DATA {{ GRAPH <{Model.Uri}> {{ {triples} }} }}"));
        }
    }
}
