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
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// A corpus of caller-supplied SPARQL run through an <see cref="ILayeredModel"/>, each case with
    /// its expected result, plus the forms that must be refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every case is chosen so that the answer differs depending on whether the overlay was applied —
    /// a query whose result is the same with and without subtraction proves nothing. The seed data is
    /// laid out so that one resource survives untouched, one loses a triple, one has a value replaced,
    /// and one exists only as a staged addition.
    /// </para>
    /// <para>
    /// This runs on every backend with a fixture, because the union half of the overlay is where
    /// backends differ. Note the deliberate avoidance of plain-literal equality in the queries:
    /// Virtuoso 7 stores a plain literal as <c>xsd:string</c> and does not equate the two, so
    /// comparisons go through <c>str()</c> and the structural cases key on IRIs and integers.
    /// </para>
    /// </remarks>
    [TestFixture]
    public abstract class LayeredModelQueryCorpusTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members

        private const string EX = "http://example.org/corpus/";

        protected IModel Baseline;
        protected IModel Additions;
        protected IModel Removals;
        protected ILayeredModel View;

        private static string S(string local) => $"<{EX}{local}>";

        #endregion

        #region Setup

        /// <summary>
        /// Seeds the three graphs.
        /// </summary>
        /// <remarks>
        /// Effective view after staging:
        /// <list type="bullet">
        /// <item><c>keep</c>  — a Thing, rank 1, peer of <c>other</c>, label "keep" (untouched)</item>
        /// <item><c>gone</c>  — rank 2, label "gone", but <b>no longer a Thing</b> (type staged for removal)</item>
        /// <item><c>both</c>  — a Thing, rank 3, label "new" (old label removed, new one added)</item>
        /// <item><c>other</c> — an Other, label "other"</item>
        /// <item><c>added</c> — a Thing, rank 4, label "added" (exists only in additions)</item>
        /// </list>
        /// </remarks>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            OntologyDiscovery.AddNamespace("cex", new Uri(EX));

            Baseline = Store.GetModel(BaseUri.GetUriRef("corpus-baseline"));
            Additions = Store.GetModel(BaseUri.GetUriRef("corpus-additions"));
            Removals = Store.GetModel(BaseUri.GetUriRef("corpus-removals"));

            foreach (IModel model in new[] { Baseline, Additions, Removals })
            {
                if (!model.IsEmpty) model.Clear();
            }

            View = Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri);

            Baseline.ExecuteUpdate(new SparqlUpdate(
                "INSERT DATA { GRAPH @g { " +
                $"{S("keep")}  a {S("Thing")} ; {S("rank")} 1 ; {S("label")} 'keep'  ; {S("peer")} {S("other")} . " +
                $"{S("gone")}  a {S("Thing")} ; {S("rank")} 2 ; {S("label")} 'gone'  . " +
                $"{S("both")}  a {S("Thing")} ; {S("rank")} 3 ; {S("label")} 'old'   . " +
                $"{S("other")} a {S("Other")} ; {S("label")} 'other' . " +
                "} }").Bind("@g", Baseline));

            // Copy the triples to be removed out of the baseline, so they are term-identical.
            Removals.ExecuteUpdate(new SparqlUpdate(
                "INSERT { GRAPH @removals { ?s ?p ?o } } WHERE { GRAPH @baseline { ?s ?p ?o . " +
                $"FILTER ((?s = {S("gone")} && ?p = <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>) " +
                $"|| (?s = {S("both")} && ?p = {S("label")})) }} }}")
                .Bind("@removals", Removals).Bind("@baseline", Baseline));

            Additions.ExecuteUpdate(new SparqlUpdate(
                "INSERT DATA { GRAPH @g { " +
                $"{S("both")}  {S("label")} 'new' . " +
                $"{S("added")} a {S("Thing")} ; {S("rank")} 4 ; {S("label")} 'added' . " +
                "} }").Bind("@g", Additions));
        }

        [TearDown]
        public void TearDownCorpus()
        {
            Baseline.Clear();
            Additions.Clear();
            Removals.Clear();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Runs a query through the view and returns the <c>?s</c> bindings as sorted local names.
        /// </summary>
        private string Subjects(string sparql)
        {
            var seen = View.GetBindings(new SparqlQuery(sparql, declarePrefixes: false))
                .Select(b => b.ContainsKey("s") ? b["s"]?.ToString() : null)
                .Where(v => v != null)
                .Select(v => v.Replace(EX, ""))
                .Distinct()
                .OrderBy(v => v, StringComparer.Ordinal);

            return string.Join(",", seen);
        }

        private string Scalar(string sparql, string variable)
        {
            BindingSet first = View.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).FirstOrDefault();

            return first != null && first.ContainsKey(variable) ? first[variable]?.ToString() : null;
        }

        private bool Ask(string sparql)
        {
            return View.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)).GetAnwser();
        }

        #endregion

        #region Corpus: queries that must be rewritten and answered

        /// <summary>
        /// Each case is (label, query, expected sorted <c>?s</c> local names). The expected value is
        /// what the overlay makes true, and differs from the un-subtracted answer in every case.
        /// </summary>
        public static IEnumerable<TestCaseData> SelectCorpus()
        {
            string thing = $"<{EX}Thing>", other = $"<{EX}Other>";
            string rank = $"<{EX}rank>", label = $"<{EX}label>", peer = $"<{EX}peer>";
            string a = "<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>";

            // 'gone' is no longer a Thing; 'added' is one only via additions.
            yield return new TestCaseData("type constraint",
                $"SELECT ?s WHERE {{ ?s a {thing} }}", "added,both,keep");

            yield return new TestCaseData("type constraint via 'a' keyword",
                $"SELECT ?s WHERE {{ ?s {a} {thing} }}", "added,both,keep");

            // rank is untouched, so every resource that has one shows up - including 'gone'.
            yield return new TestCaseData("unconstrained predicate",
                $"SELECT ?s WHERE {{ ?s {rank} ?r }}", "added,both,gone,keep");

            yield return new TestCaseData("numeric FILTER",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r > 2) }}", "added,both");

            yield return new TestCaseData("two patterns joined",
                $"SELECT ?s WHERE {{ ?s a {thing} . ?s {rank} ?r }}", "added,both,keep");

            yield return new TestCaseData("predicate-object list",
                $"SELECT ?s WHERE {{ ?s a {thing} ; {rank} ?r }}", "added,both,keep");

            yield return new TestCaseData("object list",
                $"SELECT ?s WHERE {{ ?s a {thing}, {thing} }}", "added,both,keep");

            yield return new TestCaseData("IRI object",
                $"SELECT ?s WHERE {{ ?s {peer} <{EX}other> }}", "keep");

            // 'both' had its label replaced: the old value must be gone, the new one present.
            yield return new TestCaseData("replaced value, new",
                $"SELECT ?s WHERE {{ ?s {label} ?l . FILTER(str(?l) = 'new') }}", "both");

            yield return new TestCaseData("replaced value, old is subtracted",
                $"SELECT ?s WHERE {{ ?s {label} ?l . FILTER(str(?l) = 'old') }}", "");

            yield return new TestCaseData("OPTIONAL keeps unmatched rows",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . OPTIONAL {{ ?s a {thing} }} }}", "added,both,gone,keep");

            yield return new TestCaseData("UNION over two types",
                $"SELECT ?s WHERE {{ {{ ?s a {thing} }} UNION {{ ?s a {other} }} }}", "added,both,keep,other");

            yield return new TestCaseData("MINUS finds the de-typed resource",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . MINUS {{ ?s a {thing} }} }}", "gone");

            yield return new TestCaseData("sub-SELECT",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . {{ SELECT ?s WHERE {{ ?s a {thing} }} }} }}", "added,both,keep");

            yield return new TestCaseData("VALUES restricts subjects",
                $"SELECT ?s WHERE {{ VALUES ?s {{ <{EX}keep> <{EX}gone> }} ?s a {thing} }}", "keep");

            yield return new TestCaseData("BIND alongside a pattern",
                $"SELECT ?s WHERE {{ ?s a {thing} . BIND(1 AS ?one) }}", "added,both,keep");

            yield return new TestCaseData("DISTINCT",
                $"SELECT DISTINCT ?s WHERE {{ ?s a {thing} ; {rank} ?r }}", "added,both,keep");

            yield return new TestCaseData("ORDER BY with LIMIT",
                $"SELECT ?s WHERE {{ ?s a {thing} ; {rank} ?r }} ORDER BY ?r LIMIT 2", "both,keep");

            yield return new TestCaseData("ORDER BY DESC with LIMIT",
                $"SELECT ?s WHERE {{ ?s a {thing} ; {rank} ?r }} ORDER BY DESC(?r) LIMIT 1", "added");

            yield return new TestCaseData("OFFSET",
                $"SELECT ?s WHERE {{ ?s a {thing} ; {rank} ?r }} ORDER BY ?r OFFSET 2", "added");

            yield return new TestCaseData("blank node property list",
                $"SELECT ?s WHERE {{ ?s a {thing} ; {rank} ?r . FILTER(?r = 1) }}", "keep");

            // dotNetRDF serializes !(?r < 3) as !?r < 3, i.e. (!?r) < 3, and it still parses.
            // Trinity writes filter expressions with its own serializer so these stay faithful.
            yield return new TestCaseData("negation around a comparison",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(!(?r < 3)) }}", "added,both");

            yield return new TestCaseData("negation around a disjunction",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(!(?r = 1 || ?r = 2)) }}", "added,both");

            yield return new TestCaseData("negation around a conjunction",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(!(?r > 1 && ?r < 4)) }}", "added,keep");

            yield return new TestCaseData("doubly nested negation",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(!(!(?r < 3))) }}", "gone,keep");

            yield return new TestCaseData("equivalent comparison",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r >= 3) }}", "added,both");

            yield return new TestCaseData("IN",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r IN (1, 4)) }}", "added,keep");

            yield return new TestCaseData("NOT IN",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r NOT IN (1, 4)) }}", "both,gone");

            yield return new TestCaseData("nested function calls",
                $"SELECT ?s WHERE {{ ?s {label} ?l . FILTER(strlen(str(?l)) = 4) }}", "gone,keep");

            yield return new TestCaseData("arithmetic precedence",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER((?r + 1) * 2 > 8) }}", "added");

            yield return new TestCaseData("BIND with a negated comparison",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . BIND(!(?r < 3) AS ?high) . FILTER(?high) }}", "added,both");

            yield return new TestCaseData("negated equality",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r != 1) }}", "added,both,gone");

            // Regression: '<' is also the less-than operator. A scanner that mistakes it for the
            // start of an IRI swallows the rest of the query, closing braces included.
            yield return new TestCaseData("less-than in a filter",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r < 3) }}", "gone,keep");

            yield return new TestCaseData("both comparison operators",
                $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r > 1 && ?r < 4) }}", "both,gone");

            yield return new TestCaseData("angle brackets inside a literal",
                "SELECT ?s WHERE { ?s " + label + " ?l . FILTER(str(?l) != '<a> } b') }", "added,both,gone,keep,other");

            yield return new TestCaseData("closing brace inside a literal",
                "SELECT ?s WHERE { ?s " + label + " ?l . FILTER(str(?l) != '}') }", "added,both,gone,keep,other");

            yield return new TestCaseData("SELECT *",
                $"SELECT * WHERE {{ ?s {peer} ?o }}", "keep");
        }

        [TestCaseSource(nameof(SelectCorpus))]
        public virtual void SelectCorpusCase(string label, string sparql, string expected)
        {
            Assert.AreEqual(expected, Subjects(sparql), $"{label}\n  query: {sparql}");
        }

        [Test]
        public virtual void AggregateCountsOnlyEffectiveTriples()
        {
            // 4 Things in the baseline+additions union, 3 through the view: 'gone' lost its type.
            Assert.AreEqual("3", Scalar(
                $"SELECT (COUNT(DISTINCT ?s) AS ?n) WHERE {{ ?s a <{EX}Thing> }}", "n"));
        }

        [Test]
        public virtual void AggregateWithGroupByAndHaving()
        {
            // Each Thing has exactly one rank, so grouping by ?s and requiring a count of 1 keeps all
            // three effective Things.
            var rows = View.GetBindings(new SparqlQuery(
                $"SELECT ?s (COUNT(?r) AS ?n) WHERE {{ ?s a <{EX}Thing> ; <{EX}rank> ?r }} " +
                "GROUP BY ?s HAVING(COUNT(?r) = 1)", declarePrefixes: false)).ToList();

            Assert.AreEqual(3, rows.Count);
        }

        [Test]
        public virtual void AskIsTrueForAnEffectiveTriple()
        {
            Assert.IsTrue(Ask($"ASK {{ <{EX}keep> a <{EX}Thing> }}"));
            Assert.IsTrue(Ask($"ASK {{ <{EX}added> a <{EX}Thing> }}"), "a staged addition must be visible");
            Assert.IsTrue(Ask($"ASK {{ <{EX}gone> <{EX}rank> 2 }}"), "an untouched triple must remain visible");
        }

        [Test]
        public virtual void AskIsFalseForARemovedTriple()
        {
            Assert.IsFalse(Ask($"ASK {{ <{EX}gone> a <{EX}Thing> }}"),
                "the staged removal must apply - this is a fully ground pattern, which needs FILTER NOT EXISTS");

            Assert.IsTrue(Baseline.ExecuteQuery(new SparqlQuery(
                $"ASK FROM <{Baseline.Uri}> {{ <{EX}gone> a <{EX}Thing> }}", declarePrefixes: false)).GetAnwser(),
                "while the baseline still holds it");
        }

        [Test]
        public virtual void StatementQueryStillMaterializesResources()
        {
            // A caller SELECT ?s ?p ?o must survive the rewrite as a statement-providing query,
            // otherwise resource materialization refuses it outright.
            var resources = View.GetResources(new SparqlQuery(
                $"SELECT ?s ?p ?o WHERE {{ ?s ?p ?o . ?s a <{EX}Thing> }}", declarePrefixes: false)).ToList();

            var names = resources.Select(r => r.Uri.OriginalString.Replace(EX, "")).OrderBy(n => n).ToList();

            Assert.AreEqual(new[] { "added", "both", "keep" }, names);

            var both = resources.Single(r => r.Uri.OriginalString == EX + "both");
            var labels = both.ListValues(new Property(new Uri(EX + "label"))).Select(v => v.ToString()).ToList();

            Assert.AreEqual(new[] { "new" }, labels, "the replaced label must read as the staged value only");
        }

        #endregion

        #region Corpus: forms that must be refused

        /// <summary>
        /// Each case is (label, query, a distinctive fragment of the expected reason). These must throw
        /// rather than answer: every one of them would otherwise read past the overlay and return
        /// triples staged for removal, or return nothing at all, with no signal either way.
        /// </summary>
        public static IEnumerable<TestCaseData> RefusedCorpus()
        {
            string thing = $"<{EX}Thing>", peer = $"<{EX}peer>", label = $"<{EX}label>", rank = $"<{EX}rank>";

            yield return new TestCaseData("property path, sequence",
                $"SELECT ?s WHERE {{ ?s {peer}/{label} ?l }}", "property path");

            yield return new TestCaseData("property path, transitive",
                $"SELECT ?s WHERE {{ ?s {peer}+ ?o }}", "property path");

            yield return new TestCaseData("explicit GRAPH block",
                $"SELECT ?s WHERE {{ GRAPH <{EX}g> {{ ?s a {thing} }} }}", "GRAPH");

            yield return new TestCaseData("GRAPH with a variable",
                $"SELECT ?s WHERE {{ GRAPH ?g {{ ?s a {thing} }} }}", "GRAPH");

            yield return new TestCaseData("SERVICE",
                $"SELECT ?s WHERE {{ SERVICE <http://example.org/sparql> {{ ?s a {thing} }} }}", "SERVICE");

            yield return new TestCaseData("CONSTRUCT",
                $"CONSTRUCT {{ ?s a {thing} }} WHERE {{ ?s a {thing} }}", "CONSTRUCT");

            yield return new TestCaseData("DESCRIBE",
                $"DESCRIBE <{EX}keep>", "DESCRIBE");

            yield return new TestCaseData("FILTER NOT EXISTS",
                $"SELECT ?s WHERE {{ ?s a {thing} . FILTER NOT EXISTS {{ ?s {label} ?l }} }}", "EXISTS");

            yield return new TestCaseData("FILTER EXISTS",
                $"SELECT ?s WHERE {{ ?s a {thing} . FILTER EXISTS {{ ?s {label} ?l }} }}", "EXISTS");

            // A negation inside HAVING or a projected expression is mangled by dotNetRDF exactly as
            // it is inside a FILTER, but those two are reused from its serialization of the query head
            // and solution modifiers rather than re-emitted, so they can only be detected. Refused
            // with a suggested rephrasing rather than answered differently than asked.
            yield return new TestCaseData("negation inside HAVING",
                $"SELECT ?s WHERE {{ ?s a {thing} ; {rank} ?r }} GROUP BY ?s HAVING(!(COUNT(?r) < 1))", "HAVING");

            yield return new TestCaseData("negation inside a projected expression",
                $"SELECT (!(?r < 3) AS ?high) WHERE {{ ?s {rank} ?r }}", "projected expression");

            yield return new TestCaseData("query with its own dataset clause",
                $"SELECT ?s FROM <{EX}g> WHERE {{ ?s a {thing} }}", "dataset clause");
        }

        [TestCaseSource(nameof(RefusedCorpus))]
        public virtual void RefusedCorpusCase(string label, string sparql, string reasonFragment)
        {
            var ex = Assert.Throws<NotSupportedException>(
                () => View.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(),
                $"{label} must be refused, not answered\n  query: {sparql}");

            Assert.That(ex.Message, Does.Contain(reasonFragment),
                $"{label}: the refusal must say why\n  message: {ex.Message}");
        }

        /// <summary>
        /// A refused form must stay refused however it is reached, not just through GetBindings.
        /// </summary>
        [Test]
        public virtual void RefusalAppliesToEveryQueryEntryPoint()
        {
            string path = $"SELECT ?s WHERE {{ ?s <{EX}peer>/<{EX}label> ?l }}";

            Assert.Throws<NotSupportedException>(() => View.ExecuteQuery(new SparqlQuery(path, declarePrefixes: false)));
            Assert.Throws<NotSupportedException>(() => View.GetBindings(new SparqlQuery(path, declarePrefixes: false)).ToList());
            Assert.Throws<NotSupportedException>(() => View.GetResources(new SparqlQuery(path, declarePrefixes: false)).ToList());
        }

        #endregion
    }
}
