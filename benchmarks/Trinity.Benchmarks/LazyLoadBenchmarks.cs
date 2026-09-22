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
    /// What always-on lazy loading of linked resources costs (ADR-0023).
    /// </summary>
    /// <remarks>
    /// Lazy loading is not disablable, so its cost is paid by every read that touches a resource
    /// reference, whether the caller wanted it or not. The three benchmarks isolate it:
    ///
    /// <list type="bullet">
    /// <item><see cref="MaterializeOnly"/> reads the resources and does not touch the links.</item>
    /// <item><see cref="MaterializeAndTraverse"/> reads them and touches every link.</item>
    /// <item><see cref="FetchEverythingRaw"/> asks one query for the whole shape. The baseline.</item>
    /// </list>
    ///
    /// The difference between the first two <b>is</b> the lazy-loading cost, with the base read
    /// subtracted out. The gap to the third is what it costs against fetching the graph in one go.
    ///
    /// The shape is the N+1 one, and deliberately so: <c>ResourceCache.LoadCachedValues</c> batches
    /// the URIs of <i>one</i> mapping on <i>one</i> resource into a single
    /// <c>Model.GetResources</c> call, so a wide fan-out is cheap -- but N resources each with links
    /// means N of those calls. Scaling <see cref="People"/> is what makes that visible.
    /// </remarks>
    public class LazyLoadBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Resources read. Each one traversed costs a round trip, so this is the N in N+1.
        /// </summary>
        [Params(50, 200)]
        public int People { get; set; }

        /// <summary>
        /// Links per resource. Held fixed: they ride on one request per resource, so widening the
        /// fan-out grows the payload rather than the number of round trips.
        /// </summary>
        private const int Links = 5;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            var people = new BenchmarkPerson[People];

            for (var i = 0; i < People; i++)
            {
                people[i] = Model.CreateResource<BenchmarkPerson>(BaseUri.GetUriRef($"person-{i}"));
                people[i].FirstName = $"Person {i}";
            }

            for (var i = 0; i < People; i++)
            {
                for (var k = 1; k <= Links; k++)
                {
                    people[i].Knows.Add(people[(i + k) % People]);
                }
            }

            // One batched write rather than People commits: this is fixture setup, and it is not
            // what the benchmarks below measure.
            Store.UpdateResources(people, Model.Uri);

            var expected = People * (2 + Links);
            var actual = CountTriples();

            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"{Backend}: seeding wrote {actual} triples, expected {expected}. Every benchmark "
                    + "below would be traversing the wrong shape.");
            }
        }

        /// <summary>
        /// Read the resources without touching their links: the cost lazy loading is added to.
        /// </summary>
        [Benchmark(Description = "GetResources, links untouched")]
        public int MaterializeOnly()
        {
            return Model.GetResources<BenchmarkPerson>().Count();
        }

        /// <summary>
        /// Read them and touch every link, which is what makes the loading happen.
        /// </summary>
        [Benchmark(Description = "GetResources + traverse (lazy)")]
        public int MaterializeAndTraverse()
        {
            var links = 0;

            foreach (var person in Model.GetResources<BenchmarkPerson>())
            {
                links += person.Knows.Count;
            }

            return links;
        }

        /// <summary>
        /// The whole shape in one query, with no mapping and no traversal. The baseline.
        /// </summary>
        [Benchmark(Description = "one SELECT for the whole shape (raw)", Baseline = true)]
        public int FetchEverythingRaw()
        {
            var query = new SparqlQuery(
                $"SELECT ?s ?o FROM <{Model.Uri}> WHERE {{ ?s <{Vocabulary.KnowsProperty}> ?o }}",
                declarePrefixes: false);

            return Store.ExecuteQuery(query).GetBindings().Count();
        }
    }
}
