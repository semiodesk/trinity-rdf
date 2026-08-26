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
// Copyright (c) Semiodesk GmbH 2023

using System;
using System.Linq;
using NUnit.Framework;
using VDS.RDF.Query;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.DotNetRDF
{
    /// <summary>
    /// The in-memory store's RDFS entailment, from the angles the shared inferencing tests do not
    /// cover: that entailments stay <b>out</b> of non-inferred queries, that they survive a write, and
    /// that the graphs holding them stay invisible.
    /// </summary>
    [TestFixture]
    public class DotNetRDFInferenceTest : StoreTest<DotNetRDFTestSetup>
    {
        /// <summary>
        /// The whole point of materializing into a side graph. If entailments ever leak into the model
        /// graph, this is what catches it -- and every other inferencing test would still pass.
        /// </summary>
        [Test]
        public void InferredTriplesAreInvisibleWithoutTheFlag()
        {
            var uri = BaseUri.GetUriRef("inference-leak");
            var resource = Model1.CreateResource(uri);
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery("ASK WHERE { @s a @t . }")
                .Bind("@s", uri)
                .Bind("@t", nco.Contact);

            Assert.IsFalse(Model1.ExecuteQuery(query).GetAnwser(),
                "nco:Contact is entailed, not stated -- it must not be visible without inference");

            Assert.IsTrue(Model1.ExecuteQuery(query, true).GetAnwser(),
                "...and it must be visible with inference");
        }

        /// <summary>
        /// Entailments are cached, and every write throws the cache away. A stale cache would answer
        /// the second query from the first query's entailments and miss the new resource.
        /// </summary>
        [Test]
        public void EntailmentsAreRecomputedAfterAWrite()
        {
            var first = Model1.CreateResource(BaseUri.GetUriRef("inference-first"));
            first.AddProperty(rdf.type, nco.PersonContact);
            first.Commit();

            Assert.AreEqual(1, Model1.GetResources<Contact>(true).Count(),
                "the first resource is entailed to be a Contact");

            var second = Model1.CreateResource(BaseUri.GetUriRef("inference-second"));
            second.AddProperty(rdf.type, nco.PersonContact);
            second.Commit();

            Assert.AreEqual(2, Model1.GetResources<Contact>(true).Count(),
                "a write after the entailments were materialized must invalidate them");
        }

        /// <summary>
        /// Deleting the data has to withdraw the entailment too -- the inverse staleness bug.
        /// </summary>
        [Test]
        public void EntailmentsAreWithdrawnWhenTheDataGoes()
        {
            var uri = BaseUri.GetUriRef("inference-withdrawn");
            var resource = Model1.CreateResource(uri);
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            Assert.AreEqual(1, Model1.GetResources<Contact>(true).Count());

            Model1.DeleteResource(uri);

            Assert.AreEqual(0, Model1.GetResources<Contact>(true).Count(),
                "the entailment must go when the triple it was derived from does");
        }

        /// <summary>
        /// Entailments must not be visible to a query that did not ask for them — including one that
        /// enumerates graphs.
        /// </summary>
        /// <remarks>
        /// An earlier design cached entailment graphs inside the store. <c>ListModels</c> and
        /// <c>ContainsModel</c> filtered them out, but a raw <c>GRAPH ?g</c> query did not, so internal
        /// bookkeeping and its entailed triples became visible to callers who had switched inference
        /// off. Entailments now live in a per-query dataset and never enter the store at all.
        /// </remarks>
        [Test]
        public void EntailmentsAreNeverVisibleToAQueryThatDidNotAskForThem()
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-hidden"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            // Force entailments to be computed at least once.
            Assert.IsNotEmpty(Model1.GetResources<Contact>(true).ToList());

            var graphs = ((Semiodesk.Trinity.Store.dotNetRDFStore)Store)
                .ExecuteQuery("SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } }") as SparqlResultSet;

            var names = graphs.Select(r => r["g"]?.ToString() ?? string.Empty).ToList();

            CollectionAssert.IsEmpty(
                names.Where(n => n.StartsWith("urn:semiodesk:trinity:", StringComparison.Ordinal)).ToList(),
                "no internal graph may be visible to a caller:\n" + string.Join("\n", names));

            CollectionAssert.IsEmpty(Store.ListModels()
                .Select(m => m.Uri.ToString())
                .Where(n => n.StartsWith("urn:semiodesk:trinity:", StringComparison.Ordinal)).ToList(),
                "nor listed as a model");
        }

        /// <summary>
        /// A query whose dataset clause uses <c>BASE</c> must not break when inference is switched on.
        /// </summary>
        /// <remarks>
        /// The first implementation read Trinity's own record of the <c>FROM</c> operands, which is the
        /// raw token text: with a <c>BASE</c> declaration that text is relative, and constructing a URI
        /// from it threw. Enabling inference turned a working query into a crash. The parsed query's
        /// resolved graph names are used instead.
        /// </remarks>
        [Test]
        public void RelativeDatasetClauseSurvivesTheFlag()
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-relative"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery(
                $"BASE <{Model1.Uri}> SELECT ?s WHERE {{ ?s a <{nco.PersonContact.Uri}> }}");

            var withoutInference = Model1.ExecuteQuery(query).GetBindings().Count();

            Assert.AreEqual(1, withoutInference);

            Assert.DoesNotThrow(() => Model1.ExecuteQuery(query, true).GetBindings().ToList(),
                "a relative dataset clause must not crash when inference is enabled");
        }

        /// <summary>
        /// A query that reaches a graph by name is refused rather than answered without inference.
        /// </summary>
        /// <remarks>
        /// Entailments are added to the query's default graph, so a pattern inside <c>GRAPH &lt;g&gt;</c>
        /// reads only asserted triples. Answering it would report success while silently returning a
        /// non-inferred result — the failure this whole design exists to remove — so it throws instead
        /// (the choice a layered view makes for a query it cannot rewrite faithfully, ADR-0041).
        /// </remarks>
        [Test]
        public void QueryingANamedGraphWithInferenceIsRefusedRatherThanAnsweredUninferred()
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-named"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var graph = new SparqlQuery(
                $"SELECT ?s WHERE {{ GRAPH <{Model1.Uri}> {{ ?s a <{nco.Contact.Uri}> }} }}");

            Assert.Throws<NotSupportedException>(
                () => Model1.ExecuteQuery(graph, true).GetBindings().ToList(),
                "GRAPH with inference must refuse, not answer uninferred");

            // ...and the same query without the flag is untouched.
            Assert.DoesNotThrow(() => Model1.ExecuteQuery(graph).GetBindings().ToList());
        }

        /// <summary>
        /// A <c>GRAPH</c> is refused wherever it hides, not only at the top level.
        /// </summary>
        /// <remarks>
        /// The first walker recursed only through child graph patterns. That reaches <c>OPTIONAL</c>,
        /// <c>MINUS</c> and <c>UNION</c>, but a subquery's pattern hangs off a <c>SubQueryPattern</c>
        /// in <c>TriplePatterns</c> and a <c>FILTER EXISTS</c> pattern hangs off the filter
        /// expression — so both were accepted and answered non-inferred. The subquery case is the
        /// dangerous one: it returned 0 rows where the equivalent default-graph query returned 1.
        /// </remarks>
        [TestCase("{{ SELECT ?s WHERE {{ GRAPH <{0}> {{ ?s a <{1}> }} }} }}", TestName = "subquery")]
        [TestCase("?s ?p ?o . FILTER EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }}", TestName = "FILTER EXISTS")]
        [TestCase("?s ?p ?o . FILTER NOT EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }}", TestName = "FILTER NOT EXISTS")]
        [TestCase("?s ?p ?o . OPTIONAL {{ GRAPH <{0}> {{ ?s a <{1}> }} }}", TestName = "OPTIONAL")]
        [TestCase("?s ?p ?o . MINUS {{ GRAPH <{0}> {{ ?s a <{1}> }} }}", TestName = "MINUS")]
        [TestCase("{{ ?s a <{1}> }} UNION {{ GRAPH <{0}> {{ ?s a <{1}> }} }}", TestName = "UNION")]
        [TestCase("?s ?p ?o . FILTER(EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }} || true)", TestName = "EXISTS nested in an expression")]
        [TestCase("?s a <{1}> . BIND(EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }} AS ?v)", TestName = "BIND")]
        public void NamedGraphAccessIsRefusedWhereverItHides(string where)
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-hidden-graph"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery(
                "SELECT ?s WHERE { " + string.Format(where, Model1.Uri, nco.Contact.Uri) + " }");

            Assert.Throws<NotSupportedException>(
                () => Model1.ExecuteQuery(query, true).GetBindings().ToList(),
                "a GRAPH anywhere in the query must be refused, not answered uninferred");
        }

        /// <summary>
        /// A <c>GRAPH</c> hiding in a projection, <c>HAVING</c> or <c>ORDER BY</c> expression is
        /// refused too — the walker visits every expression a query carries, not only its filters.
        /// </summary>
        /// <remarks>
        /// These four slots were unvisited while the pattern tree was fully walked, and the result was
        /// demonstrably wrong rather than merely unchecked: a single query could prove <c>?s a C2</c>
        /// in its <c>WHERE</c> clause and deny it in a <c>BIND</c> on the next line, because the
        /// <c>EXISTS</c> read the named graph where entailments never land.
        ///
        /// This is the half of the ADR-0041 parallel that did not carry over the first time:
        /// <c>OverlayQueryRewriter</c> refuses <c>HAVING</c>, projections, <c>GROUP BY</c> and
        /// <c>ORDER BY</c> it cannot verify for exactly this reason.
        /// </remarks>
        [TestCase("SELECT ?s (EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }} AS ?v) WHERE {{ ?s a <{1}> }}", TestName = "projection")]
        [TestCase("SELECT ?s WHERE {{ ?s a <{1}> }} GROUP BY ?s HAVING(EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }})", TestName = "HAVING")]
        [TestCase("SELECT ?s WHERE {{ ?s a <{1}> }} ORDER BY (EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }})", TestName = "ORDER BY")]
        // Grouping by an expression means the grouped variable cannot also be projected, so this one
        // projects the aggregate instead -- the naive form is rejected by the SPARQL parser, not by us.
        [TestCase("SELECT (COUNT(*) AS ?n) WHERE {{ ?s a <{1}> }} GROUP BY (EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }})", TestName = "GROUP BY")]
        [TestCase("SELECT (SUM(IF(EXISTS {{ GRAPH <{0}> {{ ?s a <{1}> }} }}, 1, 0)) AS ?n) WHERE {{ ?s a <{1}> }}", TestName = "inside an aggregate")]
        public void NamedGraphAccessIsRefusedInEveryExpressionSlot(string queryText)
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-expr-slot"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery(string.Format(queryText, Model1.Uri, nco.Contact.Uri));

            Assert.Throws<NotSupportedException>(
                () => Model1.ExecuteQuery(query, true).GetBindings().ToList(),
                "a GRAPH in any expression the query carries must be refused");
        }

        /// <summary>
        /// The refusal must not swallow ordinary queries: the walker refuses what it does not
        /// recognise, so a whitelist that is too narrow would reject perfectly good SPARQL.
        /// </summary>
        [TestCase("?s a <{1}>", TestName = "plain pattern")]
        [TestCase("?s a <{1}> . FILTER(BOUND(?s))", TestName = "FILTER")]
        [TestCase("?s a <{1}> . OPTIONAL {{ ?s <http://example.org/p> ?o }}", TestName = "OPTIONAL")]
        [TestCase("{{ ?s a <{1}> }} UNION {{ ?s a <{1}> }}", TestName = "UNION")]
        [TestCase("{{ SELECT ?s WHERE {{ ?s a <{1}> }} }}", TestName = "subquery")]
        [TestCase("?s a <{1}> . BIND(1 AS ?n)", TestName = "BIND")]
        [TestCase("VALUES ?s {{ <http://example.org/nobody> }} ?s a <{1}>", TestName = "VALUES")]
        [TestCase("?s <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>/<http://www.w3.org/2000/01/rdf-schema#subClassOf>* <{1}>", TestName = "property path")]
        [TestCase("?s ?p ?o . FILTER NOT EXISTS {{ ?s a <http://example.org/None> }}", TestName = "NOT EXISTS without GRAPH")]
        [TestCase("?s a <{1}> . BIND(EXISTS {{ ?s a <{1}> }} AS ?v)", TestName = "BIND with EXISTS, no GRAPH")]
        public void OrdinaryQueriesAreStillAnswered(string where)
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-ordinary"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery(
                "SELECT ?s WHERE { " + string.Format(where, Model1.Uri, nco.Contact.Uri) + " }");

            Assert.DoesNotThrow(() => Model1.ExecuteQuery(query, true).GetBindings().ToList(),
                "the whitelist must not reject ordinary SPARQL");
        }

        /// <summary>
        /// The newly walked slots must still accept ordinary projections, grouping and ordering.
        /// </summary>
        /// <remarks>
        /// Walking more of the query means more chances to refuse something legitimate. These are the
        /// forms the walker now visits that it did not before.
        /// </remarks>
        [TestCase("SELECT ?s (COUNT(?s) AS ?n) WHERE {{ ?s a <{1}> }} GROUP BY ?s", TestName = "aggregate projection")]
        [TestCase("SELECT ?s WHERE {{ ?s a <{1}> }} GROUP BY ?s HAVING(COUNT(?s) > 0)", TestName = "HAVING on an aggregate")]
        [TestCase("SELECT ?s WHERE {{ ?s a <{1}> }} ORDER BY ?s", TestName = "ORDER BY a variable")]
        [TestCase("SELECT ?s WHERE {{ ?s a <{1}> }} ORDER BY DESC(STR(?s)) LIMIT 10", TestName = "ORDER BY an expression, with LIMIT")]
        [TestCase("SELECT (STR(?s) AS ?t) WHERE {{ ?s a <{1}> }}", TestName = "projection expression")]
        public void OrdinaryProjectionGroupingAndOrderingAreStillAnswered(string queryText)
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-ordinary-slot"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery(string.Format(queryText, Model1.Uri, nco.Contact.Uri));

            Assert.DoesNotThrow(() => Model1.ExecuteQuery(query, true).GetBindings().ToList(),
                "walking more of the query must not mean refusing more of it");
        }

        /// <summary>
        /// A query naming no graph is answered the same way with the flag and without it.
        /// </summary>
        /// <remarks>
        /// Executed through the store rather than through a model: <c>Model.ExecuteQuery</c> injects a
        /// <c>FROM</c> for the model, so a test written against it never has the empty dataset it
        /// claims to exercise — the earlier version of this test asserted only <c>DoesNotThrow</c> and
        /// would have passed either way. Widening such a query would <i>narrow</i> it from the whole
        /// store to one graph, so inference deliberately does nothing.
        /// </remarks>
        [Test]
        public void QueryNamingNoGraphIsAnsweredIdenticallyWithAndWithoutTheFlag()
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-nodataset"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var text = $"SELECT ?s WHERE {{ ?s a <{nco.Contact.Uri}> }}";

            var plain = Store.ExecuteQuery(new SparqlQuery(text)).GetBindings().Count();

            var inferred = Store.ExecuteQuery(
                new SparqlQuery(text) { IsInferenceEnabled = true }).GetBindings().Count();

            // Both read the store's default graph, which the models do not write to -- so the count
            // itself is not the point. That it does not *change* is: widening a query that named no
            // graph would restrict it to one, and switching inference on must never do that.
            Assert.AreEqual(plain, inferred,
                "with no graph named there is nothing to widen, so the answer must not change");
        }

    }
}
