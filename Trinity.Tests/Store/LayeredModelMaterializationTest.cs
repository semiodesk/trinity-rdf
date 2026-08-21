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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// A materialized layered view: the effective triples kept in a fourth graph, so queries run
    /// natively against an ordinary graph.
    /// </summary>
    /// <remarks>
    /// The central assertion is <b>equivalence</b>: a materialized view must answer exactly as a
    /// rewriting view over the same three layers. That makes the rewriting mode - already covered by a
    /// corpus and a differential oracle - the oracle for this one, so no expectations have to be
    /// authored twice and the two modes cannot drift.
    /// </remarks>
    [TestFixture]
    public abstract class LayeredModelMaterializationTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members

        private const string EX = "http://example.org/mat/";

        protected IModel Baseline;
        protected IModel Additions;
        protected IModel Removals;
        protected IModel Effective;

        /// <summary>The same three layers, read through the overlay.</summary>
        protected ILayeredModel Rewriting;

        /// <summary>The same three layers, read through the materialized graph.</summary>
        protected ILayeredModel Materialized;

        #endregion

        #region Setup

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            OntologyDiscovery.AddNamespace("mat", new Uri(EX));

            Baseline = Store.GetModel(BaseUri.GetUriRef("mat-baseline"));
            Additions = Store.GetModel(BaseUri.GetUriRef("mat-additions"));
            Removals = Store.GetModel(BaseUri.GetUriRef("mat-removals"));
            Effective = Store.GetModel(BaseUri.GetUriRef("mat-effective"));

            foreach (IModel model in new[] { Baseline, Additions, Removals, Effective })
            {
                if (!model.IsEmpty) model.Clear();
            }

            // keep  : a Thing, survives untouched
            // gone  : its type is staged for removal, so it stops being a Thing
            // both  : its label is replaced
            // added : exists only as a staged addition
            // A chain a -> b -> c spans the layers, which is what a transitive path needs.
            Baseline.ExecuteUpdate(new SparqlUpdate(
                "INSERT DATA { GRAPH @g { " +
                $"<{EX}keep>  a <{EX}Thing> ; <{EX}label> 'keep' ; <{EX}rank> 1 . " +
                $"<{EX}gone>  a <{EX}Thing> ; <{EX}label> 'gone' ; <{EX}rank> 2 . " +
                $"<{EX}both>  a <{EX}Thing> ; <{EX}label> 'old'  ; <{EX}rank> 3 . " +
                $"<{EX}a> <{EX}next> <{EX}b> . " +
                "} }").Bind("@g", Baseline));

            Removals.ExecuteUpdate(new SparqlUpdate(
                "INSERT { GRAPH @removals { ?s ?p ?o } } WHERE { GRAPH @baseline { ?s ?p ?o . " +
                $"FILTER ((?s = <{EX}gone> && ?p = <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>) " +
                $"|| (?s = <{EX}both> && ?p = <{EX}label>)) }} }}")
                .Bind("@removals", Removals).Bind("@baseline", Baseline));

            Additions.ExecuteUpdate(new SparqlUpdate(
                "INSERT DATA { GRAPH @g { " +
                $"<{EX}both>  <{EX}label> 'new' . " +
                $"<{EX}added> a <{EX}Thing> ; <{EX}label> 'added' ; <{EX}rank> 4 . " +
                $"<{EX}b> <{EX}next> <{EX}c> . " +
                "} }").Bind("@g", Additions));

            Rewriting = Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri);
            Materialized = Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri, Effective.Uri);
        }

        [TearDown]
        public void TearDownMaterialization()
        {
            foreach (IModel model in new[] { Baseline, Additions, Removals, Effective })
            {
                model.Clear();
            }
        }

        private static IEnumerable<string> Rows(IModel model, string sparql, bool ordered = false)
        {
            var rows = model.GetBindings(new SparqlQuery(sparql, declarePrefixes: false))
                .Select(b => string.Join(",", b.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                                              .Select(kv => kv.Key + "=" + kv.Value)))
                .Select(r => r.Replace(EX, ""))
                .ToList();

            if (!ordered) rows.Sort(StringComparer.Ordinal);

            return rows;
        }

        #endregion

        #region Equivalence with the rewriting mode

        /// <summary>
        /// Shapes the rewriting mode supports, which must give identical answers materialized.
        /// </summary>
        public static IEnumerable<TestCaseData> EquivalentShapes()
        {
            string thing = $"<{EX}Thing>", label = $"<{EX}label>", rank = $"<{EX}rank>";

            var shapes = new (string, string)[]
            {
                ("type constraint",        $"SELECT ?s WHERE {{ ?s a {thing} }}"),
                ("unconstrained",          $"SELECT ?s WHERE {{ ?s {rank} ?r }}"),
                ("numeric FILTER",         $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(?r > 2) }}"),
                ("replaced value, new",    $"SELECT ?s WHERE {{ ?s {label} ?l . FILTER(str(?l) = 'new') }}"),
                ("replaced value, old",    $"SELECT ?s WHERE {{ ?s {label} ?l . FILTER(str(?l) = 'old') }}"),
                ("OPTIONAL",               $"SELECT ?s ?t WHERE {{ ?s {rank} ?r . OPTIONAL {{ ?s a ?t }} }}"),
                ("UNION",                  $"SELECT ?s WHERE {{ {{ ?s a {thing} }} UNION {{ ?s {label} ?l }} }}"),
                ("MINUS",                  $"SELECT ?s WHERE {{ ?s {rank} ?r . MINUS {{ ?s a {thing} }} }}"),
                ("sub-SELECT",             $"SELECT ?s WHERE {{ ?s {rank} ?r . {{ SELECT ?s WHERE {{ ?s a {thing} }} }} }}"),
                ("aggregate",              $"SELECT (COUNT(DISTINCT ?s) AS ?n) WHERE {{ ?s a {thing} }}"),
                ("GROUP BY",               $"SELECT ?s WHERE {{ ?s {rank} ?r }} GROUP BY ?s"),
                ("negated filter",         $"SELECT ?s WHERE {{ ?s {rank} ?r . FILTER(!(?r < 3)) }}"),
                ("SELECT *",               $"SELECT * WHERE {{ ?s {rank} ?r }}"),
            };

            return shapes.Select(x => new TestCaseData(x.Item1, x.Item2));
        }

        [TestCaseSource(nameof(EquivalentShapes))]
        public virtual void MaterializedAnswersAsTheRewritingViewDoes(string label, string sparql)
        {
            Assert.AreEqual(
                string.Join(" | ", Rows(Rewriting, sparql)),
                string.Join(" | ", Rows(Materialized, sparql)),
                $"{label}: the two modes must agree\n  query: {sparql}");
        }

        [Test]
        public virtual void MappedReadsAgreeBetweenTheModes()
        {
            foreach (string local in new[] { "keep", "gone", "both", "added" })
            {
                var uri = new Uri(EX + local);

                Assert.AreEqual(Rewriting.ContainsResource(uri), Materialized.ContainsResource(uri),
                    $"ContainsResource disagreed for {local}");

                if (!Rewriting.ContainsResource(uri)) continue;

                Assert.AreEqual(
                    string.Join(",", Rewriting.GetResource(uri).ListValues(new Property(new Uri(EX + "label")))
                        .Select(v => v.ToString()).OrderBy(v => v)),
                    string.Join(",", Materialized.GetResource(uri).ListValues(new Property(new Uri(EX + "label")))
                        .Select(v => v.ToString()).OrderBy(v => v)),
                    $"label values disagreed for {local}");
            }

            Assert.AreEqual(Rewriting.IsEmpty, Materialized.IsEmpty);
        }

        #endregion

        #region What materializing unlocks

        /// <summary>
        /// A transitive path whose hops straddle the layers — the one thing no rewrite can do.
        /// </summary>
        /// <remarks>
        /// <c>a next b</c> is in the baseline and <c>b next c</c> in the additions, so the chain exists
        /// in the effective graph but in neither layer alone. Transitive closure does not distribute
        /// over the union of the layers, which is why the rewriting mode refuses the query outright;
        /// materialized, it is an ordinary graph and the path just works.
        /// </remarks>
        [Test]
        public virtual void TransitivePathAcrossTheLayersWorksMaterialized()
        {
            string query = $"SELECT ?o WHERE {{ <{EX}a> <{EX}next>+ ?o }}";

            Assert.Throws<NotSupportedException>(
                () => Rewriting.GetBindings(new SparqlQuery(query, declarePrefixes: false)).ToList(),
                "the rewriting mode cannot express an unbounded path over the layers");

            Assert.AreEqual("o=b | o=c", string.Join(" | ", Rows(Materialized, query)),
                "materialized, the chain resolves across what were separate layers");
        }

        /// <summary>
        /// Query forms the rewriter has to refuse work materialized, because there is no overlay to
        /// apply and therefore nothing to refuse.
        /// </summary>
        /// <remarks>
        /// Note what is <i>not</i> asserted: an explicit <c>GRAPH &lt;effective&gt;</c> block. A
        /// materialized view scopes queries with a plain <c>FROM</c>, which places the graph in the
        /// default graph rather than among the named graphs, so addressing it by name matches nothing.
        /// That is correct SPARQL rather than a gap - a caller should query the view as the default
        /// graph and need not know the graph's name at all.
        /// </remarks>
        [Test]
        public virtual void FormsTheRewriterRefusesWorkMaterialized()
        {
            var refusedByRewriter = new (string Label, string Sparql)[]
            {
                ("sequence path",   $"SELECT ?o WHERE {{ <{EX}a> <{EX}next>/<{EX}next> ?o }}"),
                ("inverse path",    $"SELECT ?s WHERE {{ ?s ^<{EX}next> <{EX}b> }}"),
                ("alternative path", $"SELECT ?o WHERE {{ <{EX}a> <{EX}next>|<{EX}label> ?o }}"),
            };

            foreach (var (label, sparql) in refusedByRewriter)
            {
                Assert.Throws<NotSupportedException>(
                    () => Rewriting.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(),
                    $"{label} must be refused by the rewriting mode");

                Assert.DoesNotThrow(
                    () => Materialized.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(),
                    $"{label} must work against a materialized view");
            }

            // The sequence path resolves across what were separate layers.
            Assert.AreEqual("o=c",
                string.Join(" | ", Rows(Materialized, $"SELECT ?o WHERE {{ <{EX}a> <{EX}next>/<{EX}next> ?o }}")));

            // CONSTRUCT and DESCRIBE are refused by the rewriter for a structural reason - a template
            // describes triples to build rather than to match - and are ordinary queries here.
            Assert.Throws<NotSupportedException>(() => Rewriting.ExecuteQuery(new SparqlQuery(
                $"CONSTRUCT {{ ?s <{EX}copy> ?l }} WHERE {{ ?s <{EX}label> ?l }}", declarePrefixes: false)));

            Assert.DoesNotThrow(() => Materialized.ExecuteQuery(new SparqlQuery(
                $"CONSTRUCT {{ ?s <{EX}copy> ?l }} WHERE {{ ?s <{EX}label> ?l }}", declarePrefixes: false)));
        }

        #endregion

        #region Maintenance

        /// <summary>
        /// Staging through the view keeps the materialized graph in step, without a rebuild.
        /// </summary>
        [Test]
        public virtual void StagingKeepsTheMaterializedGraphInStep()
        {
            var resource = Materialized.GetResource(new Uri(EX + "keep"));
            resource.RemoveProperty(new Property(new Uri(EX + "label")), "keep");
            resource.AddProperty(new Property(new Uri(EX + "label")), "restaged");
            resource.Commit();

            Assert.AreEqual("o=restaged",
                string.Join(" | ", Rows(Materialized, $"SELECT ?o WHERE {{ <{EX}keep> <{EX}label> ?o }}")),
                "the materialized graph must reflect the staged change immediately");

            Assert.AreEqual(
                string.Join(" | ", Rows(Rewriting, $"SELECT ?o WHERE {{ <{EX}keep> <{EX}label> ?o }}")),
                string.Join(" | ", Rows(Materialized, $"SELECT ?o WHERE {{ <{EX}keep> <{EX}label> ?o }}")),
                "and still agree with the rewriting view");
        }

        [Test]
        public virtual void DiscardRebuildsTheMaterializedGraph()
        {
            Materialized.Discard();

            Assert.AreEqual(
                string.Join(" | ", Rows(Baseline, $"SELECT ?s ?p ?o FROM <{Baseline.Uri}> WHERE {{ ?s ?p ?o }}")),
                string.Join(" | ", Rows(Materialized, "SELECT ?s ?p ?o WHERE { ?s ?p ?o }")),
                "after discard the effective graph is the baseline again");
        }

        [Test]
        public virtual void AcceptNeedsNoRebuildBecauseTheBaselineBecomesWhatWasMaterialized()
        {
            string before = string.Join(" | ", Rows(Materialized, "SELECT ?s ?p ?o WHERE { ?s ?p ?o }"));

            Materialized.Accept();

            Assert.AreEqual(before,
                string.Join(" | ", Rows(Materialized, "SELECT ?s ?p ?o WHERE { ?s ?p ?o }")),
                "accept makes the baseline equal what was materialized, so the graph is already right");
            Assert.IsTrue(Additions.IsEmpty);
            Assert.IsTrue(Removals.IsEmpty);
        }

        /// <summary>
        /// A write straight to a layer, bypassing the view, leaves the materialized graph stale — and
        /// nothing detects it.
        /// </summary>
        /// <remarks>
        /// Pinned rather than implied. There is no cheap reliable staleness check: triple counts are
        /// unsound, since swapping one triple for another leaves the count unchanged, and there is no
        /// change notification. So the contract is that the view maintains what it changes and
        /// <c>Refresh()</c> exists for when that contract is deliberately broken — the same assumption
        /// the baseline already carries.
        /// </remarks>
        [Test]
        public virtual void AnOutOfBandWriteGoesStaleUntilRefreshed()
        {
            Additions.ExecuteUpdate(new SparqlUpdate(
                $"INSERT DATA {{ GRAPH @g {{ <{EX}sneaky> <{EX}label> 'behind the view' }} }}")
                .Bind("@g", Additions));

            string query = $"SELECT ?o WHERE {{ <{EX}sneaky> <{EX}label> ?o }}";

            Assert.AreEqual("o=behind the view", string.Join(" | ", Rows(Rewriting, query)),
                "the rewriting view sees it at once, because it computes the overlay per query");
            Assert.IsEmpty(Rows(Materialized, query),
                "the materialized view does not, and cannot tell that it is stale");

            Materialized.Refresh();

            Assert.AreEqual("o=behind the view", string.Join(" | ", Rows(Materialized, query)),
                "Refresh() is how the contract is repaired");
        }

        [Test]
        public virtual void RefreshOnANonMaterializedViewThrows()
        {
            Assert.IsFalse(Rewriting.IsMaterialized);
            Assert.IsTrue(Materialized.IsMaterialized);
            Assert.IsNull(Rewriting.Materialized);

            Assert.Throws<NotSupportedException>(() => Rewriting.Refresh());
        }

        [Test]
        public virtual void TheMaterializedGraphMustBeDistinctFromTheLayers()
        {
            Assert.Throws<ArgumentException>(
                () => Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri, Baseline.Uri));
            Assert.Throws<ArgumentException>(
                () => Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri, Additions.Uri));
        }

        #endregion
    }
}
