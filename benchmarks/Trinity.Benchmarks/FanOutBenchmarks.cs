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
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// One resource with many links: the bulk subject binding of ADR-0046.
    /// </summary>
    /// <remarks>
    /// Traversing a mapped collection loads every member in one query that binds them all with
    /// <c>VALUES</c>, chunked at <c>SparqlSerializer.SubjectBindingBatchSize</c> (1000). ADR-0046
    /// replaced an equality chain that Virtuoso could not compile past 1024 terms, and measured the
    /// replacement on Virtuoso at 13 ms for 157 subjects, 28 ms at 300, 83 ms at 1000 and 170 ms at 2000.
    /// The <see cref="Links"/> values reproduce that table: 158 is the size a consumer hit (SP031
    /// before the fix), 1000 is one full chunk, 2000 is two.
    ///
    /// <see cref="LazyLoadBenchmarks"/> is the other axis -- many resources with few links each, the
    /// N+1 shape -- and holds its fan-out fixed so the two do not blur.
    ///
    /// The baseline is the same two questions asked by hand: the hub's triples, then one VALUES query
    /// over the members. So the Ratio is what the mapper adds to the query shape it issues. The join
    /// row asks for everything in one query, which is what the shape costs against not being split.
    /// </remarks>
    public class FanOutBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Members of the hub's <c>Knows</c>.
        /// </summary>
        [Params(10, 158, 1000, 2000)]
        public int Links { get; set; }

        private UriRef _hub;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            _hub = BaseUri.GetUriRef("hub");

            var written = BenchmarkData.Seed(Store, Model.Uri, Links + 1, (buffer, i) =>
            {
                if (i == Links)
                {
                    var hub = BenchmarkData.AppendPerson(buffer, _hub, "Hub");

                    for (var k = 0; k < Links; k++)
                    {
                        hub += BenchmarkData.AppendKnows(buffer, _hub, PersonUri(k));
                    }

                    return hub;
                }

                return BenchmarkData.AppendPerson(buffer, PersonUri(i), $"Person {i}");
            });

            AssertSeeded(written);
        }

        [Benchmark(Description = "GetResource + traverse Knows (mapped, VALUES)")]
        public int TraverseMapped()
        {
            var hub = Model.GetResource<BenchmarkPerson>(_hub);

            // FirstName, not just Count: a member that came back unresolved would still be counted.
            return hub.Knows.Count(p => p.FirstName != null);
        }

        [Benchmark(Description = "hub SELECT + VALUES SELECT (raw, same shape)", Baseline = true)]
        public int TraverseRaw()
        {
            var members = Store.ExecuteQuery(new SparqlQuery(
                    $"SELECT ?o FROM <{Model.Uri}> WHERE {{ <{_hub}> <{Vocabulary.KnowsProperty}> ?o }}",
                    declarePrefixes: false))
                .GetBindings()
                .Select(b => ((System.Uri)b["o"]).OriginalString)
                .ToList();

            var total = 0;

            // Chunked exactly as Trinity chunks it, so the two rows issue the same requests.
            for (var offset = 0; offset < members.Count; offset += 1000)
            {
                var values = new StringBuilder();

                foreach (var member in members.Skip(offset).Take(1000))
                {
                    values.Append('<').Append(member).Append("> ");
                }

                total += Store.ExecuteQuery(new SparqlQuery(
                        $"SELECT ?s ?p ?o FROM <{Model.Uri}> WHERE {{ VALUES ?s {{ {values}}} ?s ?p ?o }}",
                        declarePrefixes: false))
                    .GetBindings()
                    .Count();
            }

            return total;
        }

        [Benchmark(Description = "one join SELECT (raw)")]
        public int TraverseJoin()
        {
            return Store.ExecuteQuery(new SparqlQuery(
                    $"SELECT ?o ?p ?v FROM <{Model.Uri}> WHERE {{ <{_hub}> <{Vocabulary.KnowsProperty}> ?o . ?o ?p ?v }}",
                    declarePrefixes: false))
                .GetBindings()
                .Count();
        }
    }
}
