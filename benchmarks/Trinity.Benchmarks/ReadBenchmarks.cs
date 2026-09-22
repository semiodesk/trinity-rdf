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
    /// Reading resources back: the mapped and LINQ paths against the SPARQL they stand in for.
    /// </summary>
    /// <remarks>
    /// The model is populated once in <c>[GlobalSetup]</c> and never written to, so every benchmark
    /// here is a pure read and no iteration needs to reset anything. The raw bindings query is the
    /// baseline: it is the same question, asked the way a caller would ask it without the mapper, so
    /// the ratio column is what materialization and translation cost.
    /// </remarks>
    public class ReadBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Resources in the model. Both are read in full by every benchmark below.
        /// </summary>
        [Params(100, 1000)]
        public int Count { get; set; }

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            for (var i = 0; i < Count; i++)
            {
                var person = Model.CreateResource<BenchmarkPerson>(BaseUri.GetUriRef($"person-{i}"));

                person.FirstName = $"Person {i}";
                person.Commit();
            }

            // Proving the fixture landed before measuring reads of it, for the same reason the write
            // benchmarks verify: a read of an empty model is fast and says nothing.
            var actual = CountTriples();

            if (actual != Count * 2)
            {
                throw new InvalidOperationException(
                    $"{Backend}: seeding wrote {actual} triples, expected {Count * 2}. Every read "
                    + "benchmark below would be measuring the wrong amount of data.");
            }
        }

        /// <summary>
        /// Materializing every resource through the mapper.
        /// </summary>
        [Benchmark(Description = "GetResources<T>() (mapped)")]
        public int GetResourcesMapped()
        {
            return Model.GetResources<BenchmarkPerson>().Count();
        }

        /// <summary>
        /// The same set through the LINQ provider, which translates before it reads.
        /// </summary>
        [Benchmark(Description = "AsQueryable<T>().Where() (LINQ)")]
        public int LinqWhere()
        {
            return Model.AsQueryable<BenchmarkPerson>()
                .Where(p => p.FirstName != null)
                .ToList()
                .Count;
        }

        /// <summary>
        /// The same question as bindings, with no mapping and no translation. The baseline.
        /// </summary>
        [Benchmark(Description = "SELECT bindings (raw SPARQL)", Baseline = true)]
        public int SelectRaw()
        {
            var query = new SparqlQuery(
                $"SELECT ?s ?n FROM <{Model.Uri}> WHERE {{ ?s <{Vocabulary.FirstNameProperty}> ?n }}",
                declarePrefixes: false);

            return Store.ExecuteQuery(query).GetBindings().Count();
        }
    }
}
