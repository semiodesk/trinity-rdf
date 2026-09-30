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
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Reading through a <see cref="IModelGroup"/>: what the union of several graphs costs (ADR-0019).
    /// </summary>
    /// <remarks>
    /// A group is a read-only <see cref="IModel"/> over several named graphs; its queries carry one
    /// <c>FROM</c> per member, which <c>SparqlQuery.Model</c> injects through the preprocessor. It is the
    /// more basic of the two multi-graph structures -- a layered view (ADR-0041) adds subtraction on
    /// top of a union -- so measuring it first splits the layered numbers into their parts: plain model,
    /// then the union, then the overlay. <c>LayeredModel</c> is not built on <c>ModelGroup</c> in code;
    /// the two share the multi-graph dataset and the VALUES batching, which is what is measured here.
    ///
    /// The same 1000 resources are written twice: spread round-robin across <see cref="Graphs"/> member
    /// graphs, and all together into the one "union" model. Links cross members, so traversal has to
    /// reach into another graph. Every category then has three rows:
    /// <list type="bullet">
    /// <item>the operation on the group;</item>
    /// <item>the same mapped operation on the union model -- the <b>baseline</b>, so the Ratio column is
    /// the cost of the group and nothing else;</item>
    /// <item>hand-written SPARQL with one <c>FROM</c> per member, which is what the group asks the store
    /// for. The gap from the group row to this one is Trinity's group plumbing; the gap from the
    /// baseline to it is the store's own multi-graph dataset cost.</item>
    /// </list>
    ///
    /// Groups are read-only -- <c>UpdateResource</c> and <c>DeleteResource</c> throw
    /// <see cref="NotSupportedException"/> -- so there is no write category. Resources read through one
    /// come back <c>IsReadOnly</c>. The parameterless <c>GetResources&lt;T&gt;()</c> throws
    /// <see cref="NotImplementedException"/> on a group, so the materialization category goes through
    /// the query overload instead.
    ///
    /// The group is created from graph URIs. <c>CreateModelGroup(params IModel[])</c> on the in-memory
    /// store builds the list and then discards it, returning an empty group (ADR-0041 notes the bug);
    /// an empty group reads nothing, fast. The seeding guard counts through the group for that reason.
    /// </remarks>
    public class ModelGroupBenchmarks : StoreBenchmarkBase
    {
        private const int People = 1000;

        /// <summary>
        /// Outgoing links per resource, each landing in a different member when there is more than one.
        /// </summary>
        private const int Links = 3;

        /// <summary>
        /// Point lookups per invocation, and resources traversed per invocation.
        /// </summary>
        private const int Lookups = 50;

        /// <summary>
        /// Member graphs the resources are spread across.
        /// </summary>
        [Params(1, 4, 16)]
        public int Graphs { get; set; }

        private IModelGroup _group;

        private UriRef[] _members;

        private string _from;

        private int _next;

        private BenchmarkPerson[] _sample;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            _members = Enumerable.Range(0, Graphs).Select(g => BaseUri.GetUriRef($"member-{g}")).ToArray();
            _from = string.Join(" ", _members.Select(m => $"FROM <{m}>"));

            // The union: every triple in the one model the baseline rows read.
            var total = BenchmarkData.SeedRing(Store, Model.Uri, People, Links, PersonUri);

            AssertSeeded(total);

            for (var g = 0; g < Graphs; g++)
            {
                var member = Store.GetModel(_members[g]);

                member.Clear();

                // Only this member's share of the ring: person i lives in member i % Graphs, links and all.
                var share = Enumerable.Range(0, People).Where(i => i % Graphs == g).ToArray();

                BenchmarkData.Seed(Store, member.Uri, share.Length, (buffer, j) =>
                {
                    var i = share[j];
                    var written = BenchmarkData.AppendPerson(buffer, PersonUri(i), $"Person {i}");

                    for (var k = 1; k <= Links; k++)
                    {
                        written += BenchmarkData.AppendKnows(buffer, PersonUri(i), PersonUri((i + k) % People));
                    }

                    return written;
                });
            }

            _group = Store.CreateModelGroup(_members.Cast<Uri>().ToArray());

            var throughGroup = Convert.ToInt32(_group
                .ExecuteQuery(new SparqlQuery("SELECT (COUNT(*) AS ?count) WHERE { ?s ?p ?o }", declarePrefixes: false))
                .GetBindings().Single()["count"]);

            if (throughGroup != total)
            {
                throw new InvalidOperationException(
                    $"{Backend}: the group over {Graphs} member(s) reads {throughGroup} triples, expected {total}. "
                    + "An empty or partial group reads fast and says nothing -- see the CreateModelGroup(IModel[]) "
                    + "bug in the class remarks.");
            }
        }

        public override void GlobalCleanup()
        {
            foreach (var member in _members ?? Array.Empty<UriRef>())
            {
                Store?.GetModel(member).Clear();
            }

            base.GlobalCleanup();
        }

        private int NextIndex()
        {
            _next = (_next + 7919) % People;

            return _next;
        }

        // --- GetResources: materializing the whole set --------------------------------------------
        //
        // Through the query overload on both sides, because the parameterless GetResources<T>() throws
        // NotImplementedException on a ModelGroup. The query is the typed pattern that overload issues
        // on a Model, so the two rows still materialize the same set.

        [Benchmark(Description = "GetResources<T>(query) (group)")]
        [BenchmarkCategory("GetResources")]
        public int GetResourcesGroup()
        {
            return _group.GetResources<BenchmarkPerson>(TypedQuery()).Count();
        }

        [Benchmark(Description = "GetResources<T>(query) (union model)", Baseline = true)]
        [BenchmarkCategory("GetResources")]
        public int GetResourcesUnion()
        {
            return Model.GetResources<BenchmarkPerson>(TypedQuery()).Count();
        }

        [Benchmark(Description = "SELECT typed ?s ?p ?o, FROM per member (raw)")]
        [BenchmarkCategory("GetResources")]
        public int GetResourcesRaw()
        {
            return Raw($"SELECT ?s ?p ?o {_from} WHERE {{ ?s ?p ?o . ?s a <{Vocabulary.PersonClass}> . }}");
        }

        private static SparqlQuery TypedQuery()
        {
            // ?s ?p ?o first and spelled out: GetResources<T>(query) accepts a SELECT only when the
            // preprocessor sees exactly ?s ?p ?o in scope, and it does not follow the ';' shorthand.
            return new SparqlQuery(
                $"SELECT ?s ?p ?o WHERE {{ ?s ?p ?o . ?s a <{Vocabulary.PersonClass}> . }}", declarePrefixes: false);
        }

        // --- GetResource: one resource, fixed cost ------------------------------------------------

        [Benchmark(Description = "GetResource<T>(uri) (group)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceGroup()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += _group.GetResource<BenchmarkPerson>(PersonUri(NextIndex())) != null ? 1 : 0;
            }

            return found;
        }

        [Benchmark(Description = "GetResource<T>(uri) (union model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceUnion()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += Model.GetResource<BenchmarkPerson>(PersonUri(NextIndex())) != null ? 1 : 0;
            }

            return found;
        }

        [Benchmark(Description = "SELECT bound subject, FROM per member (raw)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("GetResource")]
        public int GetResourceRaw()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += Raw($"SELECT ?p ?o {_from} WHERE {{ <{PersonUri(NextIndex())}> ?p ?o }}");
            }

            return found;
        }

        // --- ContainsResource -------------------------------------------------------------------

        [Benchmark(Description = "ContainsResource(uri) (group)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsGroup()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += _group.ContainsResource(PersonUri(NextIndex())) ? 1 : 0;
            }

            return found;
        }

        [Benchmark(Description = "ContainsResource(uri) (union model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsUnion()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                found += Model.ContainsResource(PersonUri(NextIndex())) ? 1 : 0;
            }

            return found;
        }

        [Benchmark(Description = "ASK bound subject, FROM per member (raw)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("ContainsResource")]
        public int ContainsRaw()
        {
            var found = 0;

            for (var i = 0; i < Lookups; i++)
            {
                var query = new SparqlQuery(
                    $"ASK {_from} {{ <{PersonUri(NextIndex())}> ?p ?o }}", declarePrefixes: false);

                found += Store.ExecuteQuery(query).GetAnwser() ? 1 : 0;
            }

            return found;
        }

        // --- LINQ ---------------------------------------------------------------------------------

        [Benchmark(Description = "AsQueryable<T>().Where() (group)")]
        [BenchmarkCategory("Linq")]
        public int LinqGroup()
        {
            return _group.AsQueryable<BenchmarkPerson>().Where(p => p.FirstName != null).ToList().Count;
        }

        [Benchmark(Description = "AsQueryable<T>().Where() (union model)", Baseline = true)]
        [BenchmarkCategory("Linq")]
        public int LinqUnion()
        {
            return Model.AsQueryable<BenchmarkPerson>().Where(p => p.FirstName != null).ToList().Count;
        }

        [Benchmark(Description = "SELECT ?s ?n, FROM per member (raw)")]
        [BenchmarkCategory("Linq")]
        public int LinqRaw()
        {
            return Raw($"SELECT ?s ?n {_from} WHERE {{ ?s <{Vocabulary.FirstNameProperty}> ?n }}");
        }

        // --- A caller's own SPARQL, scoped by the group -------------------------------------------

        /// <summary>
        /// A query with no dataset clause of its own, run through the group: assigning
        /// <c>SparqlQuery.Model</c> makes the preprocessor inject one <c>FROM</c> per member (ADR-0043).
        /// </summary>
        [Benchmark(Description = "ExecuteQuery, FROM injected (group)")]
        [BenchmarkCategory("CallerQuery")]
        public int CallerQueryGroup()
        {
            return _group.ExecuteQuery(CallerQuery()).GetBindings().Count();
        }

        [Benchmark(Description = "ExecuteQuery, FROM injected (union model)", Baseline = true)]
        [BenchmarkCategory("CallerQuery")]
        public int CallerQueryUnion()
        {
            return Model.ExecuteQuery(CallerQuery()).GetBindings().Count();
        }

        [Benchmark(Description = "ExecuteQuery, FROM per member written in (raw)")]
        [BenchmarkCategory("CallerQuery")]
        public int CallerQueryRaw()
        {
            return Raw($"SELECT ?s ?n {_from} WHERE {{ ?s <{Vocabulary.FirstNameProperty}> ?n }}");
        }

        private static SparqlQuery CallerQuery()
        {
            return new SparqlQuery(
                $"SELECT ?s ?n WHERE {{ ?s <{Vocabulary.FirstNameProperty}> ?n }}", declarePrefixes: false);
        }

        // --- Traversal across members -------------------------------------------------------------

        /// <summary>
        /// Reads the resources the traversal rows start from, outside the timed region, fresh for every
        /// iteration: a lazy-loaded collection is cached on its resource once touched, so reusing them
        /// would time the cache.
        /// </summary>
        [IterationSetup(Target = nameof(TraverseGroup))]
        public void ReadSampleFromGroup()
        {
            _sample = Enumerable.Range(0, Lookups)
                .Select(i => _group.GetResource<BenchmarkPerson>(PersonUri(i * (People / Lookups))))
                .ToArray();
        }

        [IterationSetup(Target = nameof(TraverseUnion))]
        public void ReadSampleFromUnion()
        {
            _sample = Enumerable.Range(0, Lookups)
                .Select(i => Model.GetResource<BenchmarkPerson>(PersonUri(i * (People / Lookups))))
                .ToArray();
        }

        /// <summary>
        /// Touches each sampled resource's links, which lazy-loads members held in other graphs through
        /// the group's bulk <c>GetResources(IEnumerable&lt;Uri&gt;, …)</c> (ADR-0046).
        /// </summary>
        [Benchmark(Description = "traverse Knows across members (group)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("Traverse")]
        public int TraverseGroup()
        {
            return Traverse();
        }

        [Benchmark(Description = "traverse Knows (union model)", Baseline = true, OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("Traverse")]
        public int TraverseUnion()
        {
            return Traverse();
        }

        [Benchmark(Description = "VALUES SELECT, FROM per member (raw)", OperationsPerInvoke = Lookups)]
        [BenchmarkCategory("Traverse")]
        public int TraverseRaw()
        {
            var total = 0;

            for (var s = 0; s < Lookups; s++)
            {
                var i = s * (People / Lookups);
                var values = new StringBuilder();

                for (var k = 1; k <= Links; k++)
                {
                    values.Append('<').Append(PersonUri((i + k) % People)).Append("> ");
                }

                total += Raw($"SELECT ?s ?p ?o {_from} WHERE {{ VALUES ?s {{ {values}}} ?s ?p ?o }}");
            }

            return total;
        }

        private int Traverse()
        {
            var links = 0;

            foreach (var person in _sample)
            {
                // FirstName, not just Count: a member the load could not resolve would still be counted.
                links += person.Knows.Count(p => p.FirstName != null);
            }

            if (links != Lookups * Links)
            {
                throw new InvalidOperationException(
                    $"{Backend}: traversal resolved {links} of {Lookups * Links} links. A link into another "
                    + "member that did not load would make this row fast for the wrong reason.");
            }

            return links;
        }

        private int Raw(string sparql)
        {
            return Store.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)).GetBindings().Count();
        }
    }
}
