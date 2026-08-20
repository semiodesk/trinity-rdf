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
using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Store-independent behaviour of <see cref="ILayeredModel"/>: a read view over
    /// <c>baseline</c> + <c>additions</c> - <c>removals</c>.
    /// </summary>
    /// <remarks>
    /// These run against every backend that has a fixture, because the union half of the overlay is
    /// exactly where backends differ — an in-memory-only test would prove very little here. Fuseki
    /// has no generic fixture (its suite is a hand-written copy and the backend is 4/86 on an
    /// upstream connector bug), so it is not covered; see ADR-0041.
    /// </remarks>
    [TestFixture]
    public abstract class LayeredModelTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members

        protected IModel Baseline;

        protected IModel Additions;

        protected IModel Removals;

        protected ILayeredModel View;

        protected UriRef R1;

        protected UriRef R2;

        #endregion

        #region Setup

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            OntologyDiscovery.AddNamespace("ex", new Uri("http://example.org/"));

            Baseline = Store.GetModel(BaseUri.GetUriRef("layer-baseline"));
            Additions = Store.GetModel(BaseUri.GetUriRef("layer-additions"));
            Removals = Store.GetModel(BaseUri.GetUriRef("layer-removals"));

            if (!Baseline.IsEmpty) Baseline.Clear();
            if (!Additions.IsEmpty) Additions.Clear();
            if (!Removals.IsEmpty) Removals.Clear();

            View = Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri);

            R1 = BaseUri.GetUriRef("lr1");
            R2 = BaseUri.GetUriRef("lr2");
        }

        // Named differently from the base TearDown, which is not virtual. NUnit runs both.
        [TearDown]
        public void TearDownLayers()
        {
            Baseline.Clear();
            Additions.Clear();
            Removals.Clear();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Stages a removal by copying the matching triples out of the baseline into the removals
        /// graph.
        /// </summary>
        /// <remarks>
        /// Copying rather than re-serializing the value is deliberate: the staged triple has to be
        /// term-identical to the baseline's, and stores do not agree on literal normalization —
        /// Virtuoso 7 turns a plain literal into <c>xsd:string</c> and does not equate the two. A
        /// server-side copy is immune to that, and is also how a real staging layer would record a
        /// removal.
        /// </remarks>
        protected void StageRemoval(Uri subject, Property predicate)
        {
            var update = new SparqlUpdate(
                "INSERT { GRAPH @removals { ?s ?p ?o } } " +
                "WHERE { GRAPH @baseline { ?s ?p ?o . FILTER (?s = @subject && ?p = @predicate) } }")
                .Bind("@removals", Removals)
                .Bind("@baseline", Baseline)
                .Bind("@subject", subject)
                .Bind("@predicate", predicate.Uri);

            Removals.ExecuteUpdate(update);
        }

        /// <summary>
        /// Stages every triple of a subject for removal.
        /// </summary>
        protected void StageRemovalOfEverything(Uri subject)
        {
            var update = new SparqlUpdate(
                "INSERT { GRAPH @removals { ?s ?p ?o } } " +
                "WHERE { GRAPH @baseline { ?s ?p ?o . FILTER (?s = @subject) } }")
                .Bind("@removals", Removals)
                .Bind("@baseline", Baseline)
                .Bind("@subject", subject);

            Removals.ExecuteUpdate(update);
        }

        /// <summary>
        /// Asserts whether a triple is visible through the view, without going through the mapped
        /// API — so a failure points at the overlay rather than at resource materialization.
        /// </summary>
        protected bool VisibleThroughView(Uri subject, Property predicate)
        {
            return View.GetResource(subject).ListValues(predicate).Any();
        }

        protected bool PresentIn(IModel model, Uri subject, Property predicate)
        {
            var query = new SparqlQuery("ASK FROM @graph { @subject @predicate ?o . }")
                .Bind("@graph", model)
                .Bind("@subject", subject)
                .Bind("@predicate", predicate.Uri);

            return model.ExecuteQuery(query).GetAnwser();
        }

        /// <summary>
        /// Creates a typed resource with a single string value in the baseline.
        /// </summary>
        protected MappingTestClass GivenBaselineResource(UriRef uri, string value)
        {
            var resource = Baseline.CreateResource<MappingTestClass>(uri);
            resource.uniqueStringTest = value;
            resource.Commit();

            return resource;
        }

        /// <summary>
        /// Creates an attribute-mapped resource in the baseline. The LINQ provider resolves
        /// predicates from <c>[RdfProperty]</c>, which the <c>PropertyMapping</c>-style test classes
        /// do not carry, so LINQ assertions use this object model instead.
        /// </summary>
        protected Agent GivenBaselineAgent(UriRef uri, string firstName)
        {
            var agent = Baseline.CreateResource<Agent>(uri);
            agent.FirstName = firstName;
            agent.Commit();

            return agent;
        }

        #endregion

        #region Acceptance criteria

        [Test]
        public virtual void RemovedTripleIsInvisibleThroughViewButRemainsInBaseline()
        {
            GivenBaselineResource(R1, "baseline value");

            Assert.IsTrue(VisibleThroughView(R1, to.uniqueStringTest), "precondition: visible before staging");

            StageRemoval(R1, to.uniqueStringTest);

            Assert.IsFalse(VisibleThroughView(R1, to.uniqueStringTest), "removed triple must be invisible through the view");
            Assert.IsTrue(PresentIn(Baseline, R1, to.uniqueStringTest), "the baseline must be untouched");
        }

        [Test]
        public virtual void AddedTripleIsVisibleThroughViewButAbsentFromBaseline()
        {
            var resource = Additions.CreateResource<MappingTestClass>(R1);
            resource.uniqueStringTest = "staged value";
            resource.Commit();

            Assert.IsTrue(VisibleThroughView(R1, to.uniqueStringTest), "added triple must be visible through the view");
            Assert.IsFalse(PresentIn(Baseline, R1, to.uniqueStringTest), "the baseline must not have gained the triple");
        }

        [Test]
        public virtual void AdditionWinsOverRemovalForTheSameTriple()
        {
            GivenBaselineResource(R1, "baseline value");

            // Stage the exact baseline triple for removal, then stage it right back as an addition.
            StageRemoval(R1, to.uniqueStringTest);

            var update = new SparqlUpdate(
                "INSERT { GRAPH @additions { ?s ?p ?o } } " +
                "WHERE { GRAPH @removals { ?s ?p ?o } }")
                .Bind("@additions", Additions)
                .Bind("@removals", Removals);

            Additions.ExecuteUpdate(update);

            Assert.IsTrue(VisibleThroughView(R1, to.uniqueStringTest),
                "a triple in both additions and removals must be visible: additions win");
        }

        /// <summary>
        /// Re-adding a triple that is already in the baseline must not double it.
        /// </summary>
        /// <remarks>
        /// SPARQL <c>UNION</c> is a bag union, so an overlay whose branches overlap yields two
        /// solutions for one triple: <c>COUNT</c> inflates and rows duplicate, with nothing to signal
        /// it. The view promises the <i>set</i> <c>(baseline - removals) union additions</c>, and an
        /// idempotent re-add is exactly what "additions win" invites a caller to do - so this is the
        /// ordinary case rather than an exotic one.
        /// </remarks>
        [Test]
        public virtual void AnIdenticalReAddDoesNotDuplicateSolutions()
        {
            GivenBaselineResource(R1, "same value");

            // Stage the identical triple as an addition.
            Additions.ExecuteUpdate(new SparqlUpdate(
                "INSERT { GRAPH @additions { ?s ?p ?o } } WHERE { GRAPH @baseline { ?s ?p ?o } }")
                .Bind("@additions", Additions)
                .Bind("@baseline", Baseline));

            Assert.AreEqual(1, View.GetResource(R1).ListValues(to.uniqueStringTest).Count(),
                "a triple in both the baseline and the additions must yield one value, not two");

            var count = View.GetBindings(new SparqlQuery(
                $"SELECT (COUNT(*) AS ?n) WHERE {{ ?s <{to.uniqueStringTest.Uri}> ?o }}", declarePrefixes: false))
                .First();

            Assert.AreEqual("1", count["n"].ToString(), "COUNT must not be inflated by the overlay");
        }

        /// <summary>
        /// The same triple in all three graphs is still visible exactly once.
        /// </summary>
        [Test]
        public virtual void ATripleInAllThreeLayersIsVisibleOnce()
        {
            GivenBaselineResource(R1, "same value");

            foreach (IModel target in new[] { Removals, Additions })
            {
                target.ExecuteUpdate(new SparqlUpdate(
                    "INSERT { GRAPH @target { ?s ?p ?o } } WHERE { GRAPH @baseline { ?s ?p ?o } }")
                    .Bind("@target", target)
                    .Bind("@baseline", Baseline));
            }

            Assert.AreEqual(1, View.GetResource(R1).ListValues(to.uniqueStringTest).Count(),
                "additions win over removals, but only once");
        }

        /// <summary>
        /// A union reached as an alternative of another union keeps its disjunction.
        /// </summary>
        /// <remarks>
        /// <c>A UNION B UNION C</c> parses left-nested, so this is the ordinary three-way case rather
        /// than an unusual one. Emitting the inner union as a group would turn it into a join and
        /// quietly drop solutions.
        /// </remarks>
        [Test]
        public virtual void NestedUnionKeepsItsDisjunction()
        {
            GivenBaselineResource(R1, "one");
            GivenBaselineResource(R2, "two");

            string p = to.uniqueStringTest.Uri.OriginalString;

            var seen = View.GetBindings(new SparqlQuery(
                $"SELECT ?s WHERE {{ {{ {{ ?s <{p}> 'one' }} UNION {{ ?s <{p}> 'two' }} }} UNION {{ ?s <{p}> 'three' }} }}",
                declarePrefixes: false))
                .Select(b => b["s"].ToString())
                .OrderBy(x => x)
                .ToList();

            Assert.AreEqual(2, seen.Count, "both alternatives of the inner union must survive");
        }

        #endregion

        #region Mapped-object reads

        [Test]
        public virtual void GetResourceReflectsAStagedValueChange()
        {
            GivenBaselineResource(R1, "old");

            StageRemoval(R1, to.uniqueStringTest);

            var staged = Additions.CreateResource<MappingTestClass>(R1);
            staged.uniqueStringTest = "new";
            staged.Commit();

            var seen = View.GetResource<MappingTestClass>(R1);

            Assert.AreEqual("new", seen.uniqueStringTest, "the view must show the staged value, not the baseline's");
            Assert.AreEqual("old", Baseline.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "the baseline must still hold the original value");
        }

        [Test]
        public virtual void GetResourceReadsARemovedValueAsUnset()
        {
            GivenBaselineResource(R1, "baseline value");

            StageRemoval(R1, to.uniqueStringTest);

            var seen = View.GetResource<MappingTestClass>(R1);

            Assert.IsNull(seen.uniqueStringTest, "a mapped property whose only value is staged for removal must read as unset");
        }

        [Test]
        public virtual void ContainsResourceHonoursTheOverlay()
        {
            Assert.IsFalse(View.ContainsResource(R1));

            GivenBaselineResource(R1, "baseline value");

            Assert.IsTrue(View.ContainsResource(R1));

            StageRemovalOfEverything(R1);

            Assert.IsFalse(View.ContainsResource(R1), "a fully removed resource must not be contained in the view");
            Assert.IsTrue(Baseline.ContainsResource(R1), "the baseline must still contain it");
        }

        [Test]
        public virtual void IsEmptyHonoursTheOverlay()
        {
            Assert.IsTrue(View.IsEmpty);

            GivenBaselineResource(R1, "baseline value");

            Assert.IsFalse(View.IsEmpty);

            StageRemovalOfEverything(R1);

            Assert.IsTrue(View.IsEmpty, "a baseline whose every triple is staged for removal must read as empty");
            Assert.IsFalse(Baseline.IsEmpty, "the baseline itself is not empty");
        }

        [Test]
        public virtual void GetResourcesOfTypeHonoursTheOverlay()
        {
            GivenBaselineResource(R1, "one");
            GivenBaselineResource(R2, "two");

            Assert.AreEqual(2, View.GetResources<MappingTestClass>().Count());

            StageRemovalOfEverything(R1);

            var seen = View.GetResources<MappingTestClass>().ToList();

            Assert.AreEqual(1, seen.Count, "a removed resource must not be enumerated");
            Assert.AreEqual(R2, seen[0].Uri);
        }

        [Test]
        public virtual void GetResourcesByUriHonoursTheOverlay()
        {
            GivenBaselineResource(R1, "one");
            GivenBaselineResource(R2, "two");

            StageRemoval(R1, to.uniqueStringTest);

            var seen = View.GetResources(new Uri[] { R1, R2 }, typeof(MappingTestClass))
                .Cast<MappingTestClass>()
                .ToDictionary(r => r.Uri);

            Assert.IsNull(seen[R1].uniqueStringTest, "the removed value must not come back");
            Assert.AreEqual("two", seen[R2].uniqueStringTest);
        }

        #endregion

        #region LINQ

        [Test]
        public virtual void LinqWhereDoesNotMatchARemovedValue()
        {
            GivenBaselineAgent(R1, "findme");

            Assert.AreEqual(1, View.AsQueryable<Agent>().Count(a => a.FirstName == "findme"),
                "precondition: matches before staging");

            StageRemoval(R1, foaf.firstName);

            // The selection-vs-materialization case: the value appears only in the WHERE clause, so
            // it is the overlay on the *selection* pattern that has to subtract it. Without that,
            // the resource would still be selected and only its triples would come back subtracted.
            Assert.AreEqual(0, View.AsQueryable<Agent>().Count(a => a.FirstName == "findme"),
                "a resource whose matching value is staged for removal must not be selected");
        }

        [Test]
        public virtual void LinqWhereMatchesAStagedAddition()
        {
            var staged = Additions.CreateResource<Agent>(R1);
            staged.FirstName = "findme";
            staged.Commit();

            var found = View.AsQueryable<Agent>().Where(a => a.FirstName == "findme").ToList();

            Assert.AreEqual(1, found.Count, "a staged addition must be selectable");
            Assert.AreEqual(R1, found[0].Uri);
        }

        #endregion

        #region Refusals

        [Test]
        public virtual void CallerSuppliedSparqlIsRewrittenToHonourTheOverlay()
        {
            GivenBaselineResource(R1, "baseline value");
            StageRemoval(R1, to.uniqueStringTest);

            // A caller query is rewritten rather than refused, so the removal applies to it too.
            var query = new SparqlQuery(
                $"SELECT ?s WHERE {{ ?s <{to.uniqueStringTest.Uri}> ?o }}", declarePrefixes: false);

            Assert.IsEmpty(View.GetBindings(query).ToList(),
                "the staged removal must apply to a caller-supplied query as well");
        }

        [Test]
        public virtual void CallerSuppliedSparqlThatCannotBeRewrittenThrows()
        {
            // A form the rewriter cannot handle must throw rather than answer without the overlay -
            // silently returning triples staged for removal is the one failure this design excludes.
            var path = new SparqlQuery(
                $"SELECT ?s WHERE {{ ?s <{to.resourceTest.Uri}>/<{to.uniqueStringTest.Uri}> ?o }}",
                declarePrefixes: false);

            Assert.Throws<NotSupportedException>(() => View.ExecuteQuery(path));

            var graphBlock = new SparqlQuery(
                $"SELECT ?s WHERE {{ GRAPH <{Baseline.Uri}> {{ ?s ?p ?o }} }}", declarePrefixes: false);

            Assert.Throws<NotSupportedException>(() => View.GetBindings(graphBlock).ToList());
        }

        /// <summary>
        /// A blank node in a triple pattern is refused, and says why.
        /// </summary>
        /// <remarks>
        /// The overlay repeats each pattern across three basic graph patterns, and SPARQL forbids a
        /// blank-node label appearing in more than one BGP - so the rewrite could not be legal SPARQL.
        /// The <c>[ ... ]</c> property-list form is the same thing spelled differently.
        /// </remarks>
        [Test]
        public virtual void BlankNodePatternsAreRefusedWithAReason()
        {
            foreach (string sparql in new[]
            {
                $"SELECT ?o WHERE {{ _:b <{to.uniqueStringTest.Uri}> ?o }}",
                $"SELECT ?o WHERE {{ ?s <{to.resourceTest.Uri}> [ <{to.uniqueStringTest.Uri}> ?o ] }}",
            })
            {
                var ex = Assert.Throws<NotSupportedException>(
                    () => View.GetBindings(new SparqlQuery(sparql, declarePrefixes: false)).ToList(), sparql);

                Assert.That(ex.Message, Does.Contain("blank node"),
                    $"the refusal must name the cause, not report a rewriter defect:\n{ex.Message}");
            }
        }

        /// <summary>
        /// A negation inside GROUP BY or ORDER BY is refused rather than answered differently.
        /// </summary>
        /// <remarks>
        /// Those clauses reach the output through dotNetRDF's own serialization of the query head,
        /// which mangles a negation wrapped around a comparison, so they can only be detected - the
        /// same treatment HAVING and projected expressions get.
        /// </remarks>
        [Test]
        public virtual void NegationInGroupByOrOrderByIsRefused()
        {
            GivenBaselineResource(R1, "one");

            string p = to.uniqueStringTest.Uri.OriginalString;

            Assert.Throws<NotSupportedException>(() => View.GetBindings(new SparqlQuery(
                $"SELECT ?g (COUNT(?s) AS ?n) WHERE {{ ?s <{p}> ?o }} GROUP BY (!(?o = 'x') AS ?g)",
                declarePrefixes: false)).ToList(), "GROUP BY");

            Assert.Throws<NotSupportedException>(() => View.GetBindings(new SparqlQuery(
                $"SELECT ?s WHERE {{ ?s <{p}> ?o }} ORDER BY DESC(!(?o = 'x'))",
                declarePrefixes: false)).ToList(), "ORDER BY");
        }

        [Test]
        public virtual void InferenceEnabledThrows()
        {
            Assert.Throws<NotSupportedException>(() => View.GetResources<MappingTestClass>(inferenceEnabled: true).ToList());
            Assert.Throws<NotSupportedException>(() => View.AsQueryable<MappingTestClass>(inferenceEnabled: true));
        }

        [Test]
        public virtual void WritesThrow()
        {
            Assert.Throws<NotSupportedException>(() => View.CreateResource(R1));
            Assert.Throws<NotSupportedException>(() => View.CreateResource<MappingTestClass>(R1));
            Assert.Throws<NotSupportedException>(() => View.DeleteResource(R1));
            Assert.Throws<NotSupportedException>(() => View.UpdateResource(new MappingTestClass(R1)));
            Assert.Throws<NotSupportedException>(() => View.ExecuteUpdate(new SparqlUpdate("CLEAR GRAPH <urn:x>")));
            Assert.Throws<NotSupportedException>(() => View.Clear());
        }

        [Test]
        public virtual void OverlappingLayersAreRejected()
        {
            Assert.Throws<ArgumentException>(() => Store.CreateLayeredModel(Baseline.Uri, Baseline.Uri, Removals.Uri));
            Assert.Throws<ArgumentException>(() => Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Additions.Uri));
        }

        #endregion

        #region Regressions

        [Test]
        public virtual void LazyLoadedResourceHonoursTheOverlay()
        {
            var target = Baseline.CreateResource<ResourceMappingTestClass>(R2);
            target.IntegerValue = 42;
            target.Commit();

            var source = Baseline.CreateResource<ResourceMappingTestClass>(R1);
            source.Resource = target;
            source.Commit();

            StageRemoval(R2, to.intTest);

            var seen = View.GetResource<ResourceMappingTestClass>(R1);

            Assert.IsNotNull(seen.Resource, "the link itself is not staged for removal");
            Assert.AreEqual(0, seen.Resource.IntegerValue,
                "a lazily loaded linked resource must also be read through the overlay");
            Assert.AreEqual(42, Baseline.GetResource<ResourceMappingTestClass>(R2).IntegerValue,
                "the baseline must still hold the linked value");
        }

        #endregion
    }
}
