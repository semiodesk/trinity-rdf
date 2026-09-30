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

using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// The LINQ provider on query shapes beyond a plain filter (ADR-0037).
    /// </summary>
    /// <remarks>
    /// <see cref="ReadBenchmarks"/> and <see cref="PointReadBenchmarks"/> cover a filter and an
    /// equality lookup. These are the other shapes an application leans on: paging, counting,
    /// existence, and a substring filter. Each is a category paired with the SPARQL it should
    /// translate to, so the Ratio is translation, execution of whatever was emitted, and
    /// materialization. The InMemory row has no network to hide behind, so it is the one to read for
    /// the provider's own cost; a server row whose Ratio is far above InMemory's means the emitted
    /// query is harder for that engine than the hand-written one, which is a translation finding.
    /// </remarks>
    public class LinqShapeBenchmarks : StoreBenchmarkBase
    {
        private const int People = 1000;

        /// <summary>
        /// Names in the fixture containing "99", which the substring rows must find.
        /// </summary>
        private static readonly int Contains99 = Enumerable.Range(0, People).Count(i => i.ToString().Contains("99"));

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            BenchmarkData.Seed(Store, Model.Uri, People,
                (buffer, i) => BenchmarkData.AppendPerson(buffer, PersonUri(i), $"Person {i}"));

            AssertSeeded(People * 2);
        }

        // --- Paging -----------------------------------------------------------------------------

        [Benchmark(Description = "OrderBy.Skip.Take (LINQ)")]
        [BenchmarkCategory("Paging")]
        public int PagingLinq()
        {
            return Expect(Model.AsQueryable<BenchmarkPerson>().OrderBy(p => p.FirstName).Skip(100).Take(50).ToList().Count, 50, "paged LINQ");
        }

        [Benchmark(Description = "ORDER BY OFFSET LIMIT subquery (raw)", Baseline = true)]
        [BenchmarkCategory("Paging")]
        public int PagingRaw()
        {
            return Expect(Raw($"SELECT ?s ?p ?o FROM <{Model.Uri}> WHERE {{ {{ SELECT ?s WHERE {{ ?s a <{Vocabulary.PersonClass}> ; "
                + $"<{Vocabulary.FirstNameProperty}> ?n }} ORDER BY ?n OFFSET 100 LIMIT 50 }} ?s ?p ?o }}"), 50 * 2, "ORDER BY OFFSET LIMIT");
        }

        // --- Count ------------------------------------------------------------------------------

        [Benchmark(Description = "Count() (LINQ)")]
        [BenchmarkCategory("Count")]
        public int CountLinq()
        {
            return Expect(Model.AsQueryable<BenchmarkPerson>().Count(), People, "LINQ Count()");
        }

        [Benchmark(Description = "SELECT COUNT (raw)", Baseline = true)]
        [BenchmarkCategory("Count")]
        public int CountRaw()
        {
            return Expect(CountWhere(Model.Uri, $"?s a <{Vocabulary.PersonClass}>"), People, "SELECT COUNT");
        }

        // --- Any --------------------------------------------------------------------------------

        [Benchmark(Description = "Any(==) (LINQ)")]
        [BenchmarkCategory("Any")]
        public bool AnyLinq()
        {
            return Expect(Model.AsQueryable<BenchmarkPerson>().Any(p => p.FirstName == "Person 500"), true, "LINQ Any()");
        }

        [Benchmark(Description = "ASK (raw)", Baseline = true)]
        [BenchmarkCategory("Any")]
        public bool AnyRaw()
        {
            return Expect(Store.ExecuteQuery(new SparqlQuery(
                    $"ASK FROM <{Model.Uri}> {{ ?s a <{Vocabulary.PersonClass}> ; <{Vocabulary.FirstNameProperty}> \"Person 500\" }}",
                    declarePrefixes: false))
                .GetAnwser(), true, "ASK");
        }

        // --- Substring filter -------------------------------------------------------------------

        [Benchmark(Description = "Where(Contains) (LINQ)")]
        [BenchmarkCategory("Contains")]
        public int ContainsLinq()
        {
            return Expect(Model.AsQueryable<BenchmarkPerson>().Where(p => p.FirstName.Contains("99")).ToList().Count, Contains99, "LINQ Contains");
        }

        [Benchmark(Description = "FILTER CONTAINS (raw)", Baseline = true)]
        [BenchmarkCategory("Contains")]
        public int ContainsRaw()
        {
            return Expect(Raw($"SELECT ?s ?p ?o FROM <{Model.Uri}> WHERE {{ ?s a <{Vocabulary.PersonClass}> ; "
                + $"<{Vocabulary.FirstNameProperty}> ?n . FILTER (CONTAINS(?n, \"99\")) ?s ?p ?o }}"), Contains99 * 2, "FILTER CONTAINS");
        }

        private int Raw(string sparql)
        {
            return Store.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)).GetBindings().Count();
        }
    }
}
