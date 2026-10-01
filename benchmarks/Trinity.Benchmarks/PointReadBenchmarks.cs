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
    /// Single-resource operations: the per-call cost of reading or probing one resource.
    /// </summary>
    /// <remarks>
    /// Where <see cref="ReadBenchmarks"/> measures throughput, this measures fixed cost -- query
    /// building, parameter binding, parsing the result, materializing one object -- which is what
    /// dominates an application that reads resources one at a time, and what an optimization of the
    /// per-call path should move.
    ///
    /// Each invocation does <see cref="Lookups"/> operations over distinct subjects and reports the
    /// per-operation time (<c>OperationsPerInvoke</c>): one sub-millisecond call per invocation is below
    /// what the Monitoring strategy can resolve, and rotating the subject keeps a server-side result
    /// cache from answering the same question every time.
    ///
    /// <see cref="Size"/> is the model the lookups are made against. A point read should not care how
    /// big the model is; if a row grows with it, the query shape is scanning rather than using an
    /// index -- the O(baseline) versus O(changes) distinction ADR-0042 measured at 1000x. The two
    /// <c>GetResource</c> overloads issue different shapes, which is why they sit side by side: the
    /// untyped one is a <c>SELECT</c> that binds its subject with <c>FILTER (?s = @subject)</c> in
    /// <c>Model</c>, the typed one the bare <c>DESCRIBE &lt;s&gt; FROM &lt;g&gt;</c> that
    /// <c>StoreBase.GetDescribeQuery</c> builds for every store. Which of them scales is an engine
    /// question, so it is measured here rather than asserted.
    ///
    /// Each operation is a category with its own raw baseline, so every Ratio is against the query
    /// that operation stands in for.
    /// </remarks>
    public class PointReadBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Operations per invocation, each against a different subject.
        /// </summary>
        private const int Lookups = 100;

        /// <summary>
        /// Resources in the model the lookups are made against.
        /// </summary>
        [Params(1000, 100_000)]
        public int Size { get; set; }

        private int _next;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            BenchmarkData.Seed(Store, Model.Uri, Size,
                (buffer, i) => BenchmarkData.AppendPerson(buffer, PersonUri(i), $"Person {i}"));

            AssertSeeded(Size * 2);
        }

        /// <summary>
        /// The next subject to look up. Strided so consecutive lookups do not touch neighbouring
        /// subjects, and wraps so any number of iterations stays inside the model.
        /// </summary>
        private int NextIndex()
        {
            _next = (_next + 7919) % Size;

            return _next;
        }

        [Benchmark(Description = "GetResource<T>(uri) (mapped, DESCRIBE)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceTyped()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += Model.GetResource<BenchmarkPerson>(PersonUri(NextIndex())).FirstName != null ? 1 : 0;
            }

            return Expect(found, Lookups, "GetResource<T>");
        }

        [Benchmark(Description = "GetResource(uri) (untyped, FILTER)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceUntyped()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += Model.GetResource(PersonUri(NextIndex())) is BenchmarkPerson ? 1 : 0;
            }

            return Expect(found, Lookups, "GetResource(uri) as BenchmarkPerson");
        }

        [Benchmark(Description = "SELECT bound subject (raw)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceRaw()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                var query = new SparqlQuery(
                    $"SELECT ?p ?o FROM <{Model.Uri}> WHERE {{ <{PersonUri(NextIndex())}> ?p ?o }}",
                    declarePrefixes: false);

                found += Store.ExecuteQuery(query).GetBindings().Count();
            }

            return Expect(found, Lookups * 2, "subject-bound SELECT");
        }

        [Benchmark(Description = "ContainsResource(uri) (mapped)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsMapped()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += Model.ContainsResource(PersonUri(NextIndex())) ? 1 : 0;
            }

            return Expect(found, Lookups, "ContainsResource");
        }

        [Benchmark(Description = "ASK bound subject (raw)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsRaw()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                var query = new SparqlQuery(
                    $"ASK FROM <{Model.Uri}> {{ <{PersonUri(NextIndex())}> ?p ?o }}", declarePrefixes: false);

                found += Store.ExecuteQuery(query).GetAnwser() ? 1 : 0;
            }

            return Expect(found, Lookups, "ASK");
        }

        /// <summary>
        /// A point lookup by value through the LINQ provider, which translates before it reads.
        /// </summary>
        [Benchmark(Description = "AsQueryable<T>().Where(==) (LINQ)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("LinqPoint")]
        public int LinqPoint()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                var name = $"Person {NextIndex()}";

                found += Model.AsQueryable<BenchmarkPerson>().Where(p => p.FirstName == name).ToList().Count;
            }

            return Expect(found, Lookups, "LINQ equality lookup");
        }

        /// <summary>
        /// The same lookup by hand: find the subject by its value and fetch its triples in one query.
        /// </summary>
        [Benchmark(Description = "SELECT by value (raw)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("LinqPoint")]
        public int LinqPointRaw()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                var query = new SparqlQuery(
                    $"SELECT ?s ?p ?o FROM <{Model.Uri}> WHERE {{ ?s <{Vocabulary.FirstNameProperty}> "
                    + $"\"Person {NextIndex()}\" . ?s ?p ?o }}",
                    declarePrefixes: false);

                found += Store.ExecuteQuery(query).GetBindings().Count();
            }

            return Expect(found, Lookups * 2, "SELECT by value");
        }
    }
}
