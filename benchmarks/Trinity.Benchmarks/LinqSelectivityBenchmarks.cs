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
    /// LINQ equality over a value half the resources hold: the case the equality lookup makes slower
    /// (ADR-0051).
    /// </summary>
    /// <remarks>
    /// The lookup is a sub-select of every resource holding the value, evaluated before it is joined. For a
    /// unique value that is one row and the lookup is a large win, which <see cref="PointReadBenchmarks"/>
    /// measures. For a common one it is half the model: <c>Any</c> and <c>First</c> can no longer stop at
    /// the first match, and <c>Count</c> pays to join the whole set. ADR-0051 records the trade-off and why
    /// it was taken; these rows keep it measured. Each pairs the LINQ call with the SPARQL a hand-written
    /// query would use, which can stop early.
    /// </remarks>
    public class LinqSelectivityBenchmarks : StoreBenchmarkBase
    {
        private const string Common = "common";

        /// <summary>
        /// Resources in the model; every second one is named <see cref="Common"/>.
        /// </summary>
        [Params(1000, 100_000)]
        public int Size { get; set; }

        private string Holders => $"?s a <{Vocabulary.PersonClass}> ; <{Vocabulary.FirstNameProperty}> \"{Common}\" .";

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            BenchmarkData.Seed(Store, Model.Uri, Size,
                (buffer, i) => BenchmarkData.AppendPerson(buffer, PersonUri(i), i % 2 == 0 ? Common : $"Person {i}"));

            AssertSeeded(Size * 2);
        }

        // --- Any --------------------------------------------------------------------------------

        [Benchmark(Description = "Any(== common) (LINQ)")]
        [BenchmarkCategory("Any")]
        public bool AnyLinq()
        {
            return Expect(Model.AsQueryable<BenchmarkPerson>().Any(p => p.FirstName == Common), true, "LINQ Any()");
        }

        [Benchmark(Description = "ASK (raw)", Baseline = true)]
        [BenchmarkCategory("Any")]
        public bool AnyRaw()
        {
            return Expect(Store.ExecuteQuery(new SparqlQuery($"ASK FROM <{Model.Uri}> {{ {Holders} }}", declarePrefixes: false))
                .GetAnwser(), true, "ASK");
        }

        // --- First ------------------------------------------------------------------------------

        [Benchmark(Description = "First(== common) (LINQ)")]
        [BenchmarkCategory("First")]
        public bool FirstLinq()
        {
            // FirstName, not just non-null: a lookup that returned some other resource would still be one.
            return Expect(Model.AsQueryable<BenchmarkPerson>().First(p => p.FirstName == Common).FirstName == Common,
                true, "LINQ First()");
        }

        [Benchmark(Description = "LIMIT 1 subquery (raw)", Baseline = true)]
        [BenchmarkCategory("First")]
        public int FirstRaw()
        {
            return Expect(Store.ExecuteQuery(new SparqlQuery(
                    $"SELECT ?s ?p ?o FROM <{Model.Uri}> WHERE {{ {{ SELECT ?s WHERE {{ {Holders} }} LIMIT 1 }} ?s ?p ?o }}",
                    declarePrefixes: false))
                .GetBindings()
                .Count(), 2, "LIMIT 1 subquery");
        }

        // --- Count ------------------------------------------------------------------------------

        [Benchmark(Description = "Count(== common) (LINQ)")]
        [BenchmarkCategory("Count")]
        public int CountLinq()
        {
            return Expect(Model.AsQueryable<BenchmarkPerson>().Count(p => p.FirstName == Common), Size / 2, "LINQ Count()");
        }

        [Benchmark(Description = "SELECT COUNT (raw)", Baseline = true)]
        [BenchmarkCategory("Count")]
        public int CountRaw()
        {
            return Expect(CountWhere(Model.Uri, Holders), Size / 2, "SELECT COUNT");
        }
    }
}
