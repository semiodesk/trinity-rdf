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
    /// Deleting resources, which removes every triple mentioning them on either side (ADR-0030).
    /// </summary>
    /// <remarks>
    /// The fixture is a ring in which every resource is both a subject and the object of other
    /// resources' links, so a delete has both halves of its work to do.
    ///
    /// The raw baseline is the shape ADR-0042 measured as the fast one: the subject side and the object
    /// side as <b>separate bound patterns</b>, which an index can answer. The alternative,
    /// <c>?s ?p ?o</c> with <c>FILTER (?s = &lt;r&gt; || ?o = &lt;r&gt;)</c>, enumerates the whole
    /// graph -- ADR-0042 took <c>DeleteResource</c> on a 1M baseline from 12.5 s to 3 ms by leaving
    /// it. <see cref="People"/> varies the model around the deleted resources for that reason: a
    /// delete should cost the same in a big model as in a small one, and a row that grows with it is
    /// scanning.
    ///
    /// Reseeded every iteration, since each one deletes.
    /// </remarks>
    public class DeleteBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Resources deleted per invocation; the reported time is per resource.
        /// </summary>
        private const int Deletes = 50;

        /// <summary>
        /// Outgoing links per resource, so each deleted one is also the object of this many.
        /// </summary>
        private const int Links = 5;

        /// <summary>
        /// Resources in the model the deletes are made from.
        /// </summary>
        [Params(1000, 10_000)]
        public int People { get; set; }

        private int[] _victims;

        private int _expected;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            // Spread through the ring rather than adjacent, so no deleted resource links to another
            // and the expected count below is not an accident of which ones were picked.
            var stride = People / Deletes;

            _victims = Enumerable.Range(0, Deletes).Select(i => i * stride).ToArray();

            var victims = new HashSet<int>(_victims);
            var removed = 0;

            for (var i = 0; i < People; i++)
            {
                if (victims.Contains(i))
                {
                    // Its own triples, links included.
                    removed += 2 + Links;
                    continue;
                }

                for (var k = 1; k <= Links; k++)
                {
                    // A surviving resource's link to a deleted one.
                    removed += victims.Contains((i + k) % People) ? 1 : 0;
                }
            }

            _expected = People * (2 + Links) - removed;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            Model.Clear();

            BenchmarkData.SeedRing(Store, Model.Uri, People, Links, PersonUri);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            AssertWrote(_expected);
        }

        [Benchmark(Description = "DeleteResource(uri) (mapped)", OperationsPerInvoke = Deletes)]
        public void DeleteMapped()
        {
            foreach (var i in _victims)
            {
                Model.DeleteResource(PersonUri(i));
            }
        }

        [Benchmark(Description = "DELETE WHERE, two bound patterns (raw)", Baseline = true, OperationsPerInvoke = Deletes)]
        public void DeleteRaw()
        {
            foreach (var i in _victims)
            {
                var subject = PersonUri(i);

                Store.ExecuteNonQuery(new SparqlUpdate(
                    $"DELETE WHERE {{ GRAPH <{Model.Uri}> {{ <{subject}> ?p ?o }} }}; "
                    + $"DELETE WHERE {{ GRAPH <{Model.Uri}> {{ ?s ?p <{subject}> }} }}"));
            }
        }
    }
}
