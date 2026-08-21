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
using System.Reflection;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests.Linq;

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

            // Mapped resources too, so the LINQ path can be held to the same equivalence as SPARQL:
            // one agent in the baseline, one staged as an addition, and one whose name is staged for
            // removal - a resource LINQ must stop selecting.
            var kept = Baseline.CreateResource<Agent>(BaseUri.GetUriRef("mat-agent-kept"));
            kept.FirstName = "findme";
            kept.Commit();

            var dropped = Baseline.CreateResource<Agent>(BaseUri.GetUriRef("mat-agent-dropped"));
            dropped.FirstName = "findme";
            dropped.Commit();

            var staged = Additions.CreateResource<Agent>(BaseUri.GetUriRef("mat-agent-staged"));
            staged.FirstName = "findme";
            staged.Commit();

            Removals.ExecuteUpdate(new SparqlUpdate(
                "INSERT { GRAPH @removals { ?s ?p ?o } } WHERE { GRAPH @baseline { ?s ?p ?o . " +
                $"FILTER (?s = <{BaseUri.GetUriRef("mat-agent-dropped")}> && ?p = <{FOAF.firstName}>) }} }}")
                .Bind("@removals", Removals).Bind("@baseline", Baseline));

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

        #region The LINQ path

        /// <summary>
        /// LINQ has to answer alike in both modes, which is what the fixture's stated central assertion
        /// means and what it did not previously cover.
        /// </summary>
        /// <remarks>
        /// The absence of this test is how a total LINQ regression reached review: the graph-selection
        /// guard could not tell the view's own injected dataset clause from a caller's, and the LINQ
        /// provider assigns <c>Model</c> at construction, so every LINQ query on a materialized view was
        /// refused outright.
        /// </remarks>
        [Test]
        public virtual void LinqCountAgreesBetweenTheModes()
        {
            Assert.AreEqual(
                Rewriting.AsQueryable<Agent>().Count(a => a.FirstName == "findme"),
                Materialized.AsQueryable<Agent>().Count(a => a.FirstName == "findme"),
                "the staged addition counts, the staged removal does not");
        }

        [Test]
        public virtual void LinqWhereAgreesBetweenTheModes()
        {
            Assert.AreEqual(
                string.Join(",", Rewriting.AsQueryable<Agent>()
                    .Where(a => a.FirstName == "findme").ToList().Select(a => a.Uri.ToString()).OrderBy(u => u)),
                string.Join(",", Materialized.AsQueryable<Agent>()
                    .Where(a => a.FirstName == "findme").ToList().Select(a => a.Uri.ToString()).OrderBy(u => u)));
        }

        [Test]
        public virtual void LinqEnumerationAgreesBetweenTheModes()
        {
            Assert.AreEqual(
                string.Join(",", Rewriting.AsQueryable<Agent>().ToList().Select(a => a.Uri.ToString()).OrderBy(u => u)),
                string.Join(",", Materialized.AsQueryable<Agent>().ToList().Select(a => a.Uri.ToString()).OrderBy(u => u)));
        }

        /// <summary>
        /// Executing a query object twice must give the same answer twice.
        /// </summary>
        /// <remarks>
        /// Execution <i>mutates</i> the query - assigning <c>Model</c> injects the view's dataset clause
        /// - so a guard that refuses any dataset clause is not idempotent: the first call succeeded and
        /// the second threw.
        /// </remarks>
        [Test]
        public virtual void AQueryObjectCanBeExecutedTwice()
        {
            var query = new SparqlQuery($"SELECT ?s WHERE {{ ?s a <{EX}Thing> }}", declarePrefixes: false);

            int first = Materialized.GetBindings(query).Count();
            int second = Materialized.GetBindings(query).Count();

            Assert.AreEqual(3, first, "keep, both and added are Things; gone had its type staged for removal");
            Assert.AreEqual(first, second, "the guard has to accept the view's own injected clause, so it is idempotent");
        }

        /// <summary>
        /// A caller may assign <see cref="ISparqlQuery.Model"/> themselves - the setter is public.
        /// </summary>
        [Test]
        public virtual void ACallerMayAssignTheModelItself()
        {
            var query = new SparqlQuery($"SELECT ?s WHERE {{ ?s a <{EX}Thing> }}", declarePrefixes: false)
            {
                Model = Materialized
            };

            Assert.AreEqual(3, Materialized.GetBindings(query).Count());
        }

        #endregion

        #region Review regressions

        /// <summary>
        /// The stale-changeset guard has to keep reading the layers, not the effective graph.
        /// </summary>
        /// <remarks>
        /// It once composed its query from the view's own dataset clause, which in materialized mode is
        /// a bare <c>FROM</c> and so leaves the named-graph set empty. Both of its <c>GRAPH</c> blocks
        /// then matched nothing and it answered "no divergence" for every input, disabling the ADR-0042
        /// precondition exactly when a caller had opted into the faster mode.
        /// </remarks>
        [Test]
        public virtual void HasDivergedReadsTheLayersNotTheEffectiveGraph()
        {
            Assert.IsFalse(Rewriting.HasDiverged(), "the baseline still holds everything staged for removal");
            Assert.IsFalse(Materialized.HasDiverged(), "and the two modes must agree on that");

            // Someone else removes, in the baseline, a triple this changeset also stages for removal.
            Baseline.ExecuteUpdate(new SparqlUpdate(
                "DELETE WHERE { GRAPH @g { " +
                $"<{EX}gone> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{EX}Thing> }} }}")
                .Bind("@g", Baseline));

            Assert.IsTrue(Rewriting.HasDiverged(), "the removal's precondition no longer holds");
            Assert.IsTrue(Materialized.HasDiverged(),
                "the materialized view must detect divergence too - it is a question about the layers");
        }

        /// <summary>
        /// Accept() must still refuse a stale changeset when the view is materialized.
        /// </summary>
        [Test]
        public virtual void AcceptStillRefusesAStaleChangesetWhenMaterialized()
        {
            Baseline.ExecuteUpdate(new SparqlUpdate(
                "DELETE WHERE { GRAPH @g { " +
                $"<{EX}gone> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{EX}Thing> }} }}")
                .Bind("@g", Baseline));

            Assert.Throws<InvalidOperationException>(() => Materialized.Accept());

            Assert.DoesNotThrow(() => Materialized.Accept(force: true), "force is the documented override");
        }

        /// <summary>
        /// A delete through the view is a change the view owns, so it owes the effective graph the same
        /// maintenance it gives a staged update.
        /// </summary>
        /// <remarks>
        /// <c>DeleteResource</c> built its own staging update and called the store directly, skipping
        /// the synchronization every other write path performs. The resource stayed readable through the
        /// very view that deleted it, with no signal to the caller.
        /// </remarks>
        [Test]
        public virtual void DeleteThroughAMaterializedViewIsVisibleThroughIt()
        {
            var keep = new Uri(EX + "keep");

            Assert.IsTrue(Materialized.ContainsResource(keep));

            Materialized.DeleteResource(keep);

            Assert.IsFalse(Materialized.ContainsResource(keep),
                "the view that performed the delete must not still show the resource");
            Assert.IsFalse(Rewriting.ContainsResource(keep), "and the layers agree");

            Assert.AreEqual(
                string.Join(" | ", Rows(Rewriting, "SELECT ?s ?p ?o WHERE { ?s ?p ?o }")),
                string.Join(" | ", Rows(Materialized, "SELECT ?s ?p ?o WHERE { ?s ?p ?o }")),
                "the effective graph is in step for every triple the delete touched");
        }

        /// <summary>
        /// Deleting a resource that other triples point at removes those too (ADR-0030), and the
        /// materialized graph has to follow on the object side as well.
        /// </summary>
        [Test]
        public virtual void DeleteThroughAMaterializedViewFollowsTheObjectSide()
        {
            Materialized.DeleteResource(new Uri(EX + "b"));

            Assert.AreEqual(
                string.Join(" | ", Rows(Rewriting, $"SELECT ?s ?o WHERE {{ ?s <{EX}next> ?o }}")),
                string.Join(" | ", Rows(Materialized, $"SELECT ?s ?o WHERE {{ ?s <{EX}next> ?o }}")),
                "a -> b came from the baseline and b -> c from the additions; both mention b");
        }

        /// <summary>
        /// Graph selection stays refused in both modes.
        /// </summary>
        /// <remarks>
        /// Materialization lifts the refusals that exist for want of a faithful rewrite. A caller
        /// dataset clause is not one of them: the view names the effective graph itself and the
        /// preprocessor merely appends the caller's beside it, so the query reads the union of the two -
        /// which served triples staged for removal out of a subtractive view.
        /// </remarks>
        public static IEnumerable<TestCaseData> GraphSelectingQueries()
        {
            yield return new TestCaseData("FROM", $"SELECT ?s FROM <$g> WHERE {{ ?s a <{EX}Thing> }}");
            yield return new TestCaseData("FROM NAMED",
                $"SELECT ?s FROM NAMED <$g> WHERE {{ GRAPH ?g {{ ?s a <{EX}Thing> }} }}");
            yield return new TestCaseData("GRAPH block", $"SELECT ?s WHERE {{ GRAPH <$g> {{ ?s a <{EX}Thing> }} }}");
            yield return new TestCaseData("GRAPH in a sub-SELECT",
                $"SELECT ?s WHERE {{ {{ SELECT ?s WHERE {{ GRAPH <$g> {{ ?s a <{EX}Thing> }} }} }} }}");
        }

        [TestCaseSource(nameof(GraphSelectingQueries))]
        public virtual void GraphSelectionIsRefusedInBothModes(string label, string template)
        {
            string sparql = template.Replace("$g", Baseline.Uri.ToString());

            Assert.Throws<NotSupportedException>(
                () => Rewriting.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(),
                $"{label}: refused when rewriting");

            Assert.Throws<NotSupportedException>(
                () => Materialized.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(),
                $"{label}: must be refused when materialized too - it reads past the view, which is not a " +
                "rewriting limitation\n  query: " + sparql);
        }

        /// <summary>
        /// A materialization known to be short must fail every later read, not just the rebuild.
        /// </summary>
        /// <remarks>
        /// <c>VerifyMaterialized</c> throws after the truncated write is already committed, and there is
        /// nothing to roll back - so without a latch the view threw once and then answered every
        /// subsequent read from a graph it knew was incomplete. The failure is provoked here by setting
        /// the flag directly: the real trigger is a store limit (Virtuoso's transaction log, somewhere
        /// between 500,000 and 1,000,000 rows) that no test can reach in reasonable time.
        /// </remarks>
        [Test]
        public virtual void AShortMaterializationRefusesEveryLaterRead()
        {
            FieldInfo latch = Materialized.GetType().GetField(
                "_materializationFailed", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(latch, "the latch this test pins has been renamed or removed");

            latch.SetValue(Materialized, true);

            Assert.Throws<InvalidOperationException>(
                () => Materialized.ContainsResource(new Uri(EX + "keep")),
                "a read from a graph known to be short must fail loudly");
            Assert.Throws<InvalidOperationException>(
                () => Rows(Materialized, "SELECT ?s WHERE { ?s ?p ?o }").ToList(),
                "caller SPARQL too");

            Materialized.Refresh();

            Assert.IsFalse((bool)latch.GetValue(Materialized), "a successful rebuild is the recovery path");
            Assert.IsTrue(Materialized.ContainsResource(new Uri(EX + "keep")));
        }

        /// <summary>
        /// A model that does not name a single graph cannot hold the effective triples, and asking for
        /// one must fail rather than quietly hand back a rewriting view.
        /// </summary>
        [Test]
        public virtual void AMaterializedModelMustNameOneGraph()
        {
            IModelGroup group = Store.CreateModelGroup(Baseline.Uri, Additions.Uri);

            Assert.IsNull(group.Uri, "a ModelGroup spans several graphs, so it has no Uri of its own");

            var thrown = Assert.Throws<ArgumentException>(
                () => Store.CreateLayeredModel(Baseline, Additions, Removals, group),
                "this used to return a rewriting view, silently ignoring the mode the caller asked for");

            Assert.AreEqual("materialized", thrown.ParamName);
        }

        /// <summary>
        /// A query with no <c>WHERE</c> clause must be handled, not crash.
        /// </summary>
        /// <remarks>
        /// <c>RootGraphPattern</c> is null for a bare <c>DESCRIBE &lt;iri&gt;</c>, and the guard walked
        /// straight into it. The form materialization exists to enable was the form that threw a
        /// <see cref="NullReferenceException"/> - a worse diagnostic than the rewriting mode it improves
        /// on.
        /// </remarks>
        public static IEnumerable<TestCaseData> PatternlessQueries()
        {
            yield return new TestCaseData("bare DESCRIBE", $"DESCRIBE <{EX}keep>");
            yield return new TestCaseData("DESCRIBE of a staged addition", $"DESCRIBE <{EX}added>");
            yield return new TestCaseData("empty WHERE", "SELECT ?s WHERE { }");
            yield return new TestCaseData("bare ASK", "ASK { }");
        }

        [TestCaseSource(nameof(PatternlessQueries))]
        public virtual void AQueryWithoutAPatternDoesNotCrash(string label, string sparql)
        {
            Assert.DoesNotThrow(
                () => Materialized.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)),
                $"{label}: no pattern means no graph selection to find\n  query: {sparql}");
        }

        /// <summary>
        /// A bare <c>DESCRIBE</c> is one of the forms materialization advertises as newly working.
        /// </summary>
        [Test]
        public virtual void ABareDescribeReturnsTheEffectiveTriples()
        {
            var described = Materialized.GetResources(
                new SparqlQuery($"DESCRIBE <{EX}gone>", declarePrefixes: false)).ToList();

            Assert.AreEqual(1, described.Count);
            Assert.IsFalse(
                described[0].ListValues(new Property(
                    new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"))).Any(),
                "gone's type is staged for removal, so DESCRIBE must not report it");
        }

        /// <summary>
        /// A <c>GRAPH</c> block nested in a <c>FILTER EXISTS</c> is graph selection too.
        /// </summary>
        /// <remarks>
        /// dotNetRDF keeps an <c>EXISTS</c> pattern in the filter's expression tree rather than in
        /// <c>ChildGraphPatterns</c>, so the walk reached neither. Unchecked, it was a <i>silent wrong
        /// answer</i> rather than a refusal: the dataset clause is a bare <c>FROM</c>, so the named-graph
        /// set is empty and the nested block matched nothing - the caller asked about a named graph and
        /// was told it was empty.
        /// </remarks>
        public static IEnumerable<TestCaseData> GraphInsideAFilter()
        {
            yield return new TestCaseData("FILTER EXISTS",
                $"SELECT ?s WHERE {{ ?s a <{EX}Thing> . FILTER EXISTS {{ GRAPH <$g> {{ ?s ?p ?o }} }} }}");
            yield return new TestCaseData("FILTER NOT EXISTS",
                $"SELECT ?s WHERE {{ ?s a <{EX}Thing> . FILTER NOT EXISTS {{ GRAPH <$g> {{ ?s ?p ?o }} }} }}");
            yield return new TestCaseData("GRAPH ?g inside a filter",
                $"SELECT ?s WHERE {{ ?s a <{EX}Thing> . FILTER EXISTS {{ GRAPH ?g {{ ?s ?p ?o }} }} }}");
            yield return new TestCaseData("nested one level deeper",
                $"SELECT ?s WHERE {{ ?s a <{EX}Thing> . FILTER (!EXISTS {{ GRAPH <$g> {{ ?s ?p ?o }} }}) }}");
            yield return new TestCaseData("BIND of an EXISTS",
                $"SELECT ?s WHERE {{ ?s a <{EX}Thing> . BIND(EXISTS {{ GRAPH <$g> {{ ?s ?p ?o }} }} AS ?x) }}");
        }

        [TestCaseSource(nameof(GraphInsideAFilter))]
        public virtual void GraphSelectionInsideAFilterIsRefused(string label, string template)
        {
            string sparql = template.Replace("$g", Additions.Uri.ToString());

            Assert.Throws<NotSupportedException>(
                () => Materialized.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(),
                $"{label}: a GRAPH block in a filter expression is graph selection too, and answering it " +
                "from an empty named-graph set is a silent wrong answer\n  query: " + sparql);
        }

        /// <summary>
        /// An <c>EXISTS</c> that names no graph is fine on a materialized view - it is an ordinary
        /// graph, so there is nothing to weave an overlay into.
        /// </summary>
        [Test]
        public virtual void AFilterExistsWithoutAGraphBlockIsAllowedWhenMaterialized()
        {
            string sparql = $"SELECT ?s WHERE {{ ?s a <{EX}Thing> . FILTER EXISTS {{ ?s <{EX}rank> ?r }} }}";

            Assert.AreEqual("s=added | s=both | s=keep",
                string.Join(" | ", Rows(Materialized, sparql)),
                "materialization lifts the EXISTS refusal, which is a rewrite-shape restriction");

            Assert.Throws<NotSupportedException>(
                () => Rewriting.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(),
                "and rewriting mode still refuses it, which is the difference between the modes");
        }

        #endregion
    }
}
