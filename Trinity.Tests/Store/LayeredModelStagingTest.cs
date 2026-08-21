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

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Staged writes through a layered view: <c>Commit()</c> stages, <c>Accept()</c> applies,
    /// <c>Discard()</c> abandons.
    /// </summary>
    /// <remarks>
    /// Runs on every backend with a fixture, because staging and accept are both multi-operation
    /// SPARQL updates and that is exactly where backends have differed throughout this work.
    /// </remarks>
    [TestFixture]
    public abstract class LayeredModelStagingTest<T> : StoreTest<T> where T : IStoreTestSetup
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

            Baseline = Store.GetModel(BaseUri.GetUriRef("stage-baseline"));
            Additions = Store.GetModel(BaseUri.GetUriRef("stage-additions"));
            Removals = Store.GetModel(BaseUri.GetUriRef("stage-removals"));

            foreach (IModel model in new[] { Baseline, Additions, Removals })
            {
                if (!model.IsEmpty) model.Clear();
            }

            View = Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri);

            R1 = BaseUri.GetUriRef("sr1");
            R2 = BaseUri.GetUriRef("sr2");
        }

        [TearDown]
        public void TearDownStaging()
        {
            Baseline.Clear();
            Additions.Clear();
            Removals.Clear();
        }

        private MappingTestClass GivenBaseline(UriRef uri, string value)
        {
            var resource = Baseline.CreateResource<MappingTestClass>(uri);
            resource.uniqueStringTest = value;
            resource.Commit();

            return resource;
        }

        private int CountIn(IModel model, Uri subject)
        {
            var query = new SparqlQuery($"SELECT ?p ?o FROM <{model.Uri}> WHERE {{ <{subject}> ?p ?o }}",
                declarePrefixes: false);

            return model.GetBindings(query).Count();
        }

        #endregion

        #region Staging

        /// <summary>
        /// A resource read through a view is writable, and committing it stages rather than writes.
        /// </summary>
        /// <remarks>
        /// This used to be a silent no-op: <c>Attach</c> marked the resource read-only and
        /// <c>Resource.Commit()</c> is guarded by that flag, so a caller's change was discarded without
        /// a word.
        /// </remarks>
        [Test]
        public virtual void CommitThroughTheViewStagesRatherThanWrites()
        {
            GivenBaseline(R1, "original");

            var seen = View.GetResource<MappingTestClass>(R1);
            seen.uniqueStringTest = "staged";
            seen.Commit();

            Assert.AreEqual("staged", View.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "the view must show the staged value");
            Assert.AreEqual("original", Baseline.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "the baseline must be untouched");
        }

        /// <summary>
        /// Staging the same property twice must not leave both values visible.
        /// </summary>
        /// <remarks>
        /// The routing crux. Sending every deleted value to the removals graph looks obviously right and
        /// is silently wrong: the first staged value would sit in additions, the second too, and the
        /// first would also be in removals — where additions win, so both stay visible.
        /// </remarks>
        [Test]
        public virtual void StagingTheSamePropertyTwiceLeavesOneValue()
        {
            GivenBaseline(R1, "original");

            var first = View.GetResource<MappingTestClass>(R1);
            first.uniqueStringTest = "staged once";
            first.Commit();

            var second = View.GetResource<MappingTestClass>(R1);
            second.uniqueStringTest = "staged twice";
            second.Commit();

            var values = View.GetResource(R1).ListValues(to.uniqueStringTest).Select(v => v.ToString()).ToList();

            Assert.AreEqual(new[] { "staged twice" }, values,
                "only the most recently staged value may be visible");
        }

        /// <summary>
        /// Re-staging the baseline's own value un-stages the removal rather than piling up graphs.
        /// </summary>
        [Test]
        public virtual void RestoringTheBaselineValueEmptiesBothLayers()
        {
            GivenBaseline(R1, "original");

            var changed = View.GetResource<MappingTestClass>(R1);
            changed.uniqueStringTest = "temporary";
            changed.Commit();

            var restored = View.GetResource<MappingTestClass>(R1);
            restored.uniqueStringTest = "original";
            restored.Commit();

            Assert.AreEqual("original", View.GetResource<MappingTestClass>(R1).uniqueStringTest);
            Assert.AreEqual(0, CountIn(Additions, R1), "additions must not keep a value the baseline already has");
            Assert.AreEqual(0, CountIn(Removals, R1), "the staged removal must have been un-staged");
        }

        /// <summary>
        /// The invariants that keep the pre-change baseline reconstructible (ADR-0042).
        /// </summary>
        [Test]
        public virtual void StagingMaintainsTheAncestorInvariants()
        {
            GivenBaseline(R1, "original");
            GivenBaseline(R2, "second");

            var a = View.GetResource<MappingTestClass>(R1);
            a.uniqueStringTest = "changed";
            a.Commit();

            var b = View.CreateResource<MappingTestClass>(BaseUri.GetUriRef("sr3"));
            b.uniqueStringTest = "brand new";
            b.Commit();

            View.DeleteResource(R2);

            Assert.IsFalse(Ask($"ASK FROM NAMED <{Additions.Uri}> FROM NAMED <{Baseline.Uri}> " +
                    $"WHERE {{ GRAPH <{Additions.Uri}> {{ ?s ?p ?o }} GRAPH <{Baseline.Uri}> {{ ?s ?p ?o }} }}"),
                "additions must share no triple with the baseline (A n B0 = {})");

            Assert.IsFalse(Ask($"ASK FROM NAMED <{Removals.Uri}> FROM NAMED <{Baseline.Uri}> " +
                    $"WHERE {{ GRAPH <{Removals.Uri}> {{ ?s ?p ?o }} FILTER NOT EXISTS {{ GRAPH <{Baseline.Uri}> {{ ?s ?p ?o }} }} }}"),
                "every staged removal must be present in the baseline (R subset of B0)");
        }

        private bool Ask(string sparql) =>
            Baseline.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)).GetAnwser();

        [Test]
        public virtual void CreateResourceThroughTheViewStagesAnAddition()
        {
            var created = View.CreateResource<MappingTestClass>(R1);
            created.uniqueStringTest = "new";
            created.Commit();

            Assert.AreEqual("new", View.GetResource<MappingTestClass>(R1).uniqueStringTest);
            Assert.IsFalse(Baseline.ContainsResource(R1), "the baseline must not have gained it");
        }

        [Test]
        public virtual void DeleteResourceThroughTheViewStagesRemovals()
        {
            GivenBaseline(R1, "original");

            View.DeleteResource(R1);

            Assert.IsFalse(View.ContainsResource(R1), "the resource must be invisible through the view");
            Assert.IsTrue(Baseline.ContainsResource(R1), "but still present in the baseline");
        }

        #endregion

        #region Accept and discard

        [Test]
        public virtual void AcceptAppliesTheStagedChangeAndEmptiesTheLayers()
        {
            GivenBaseline(R1, "original");

            var changed = View.GetResource<MappingTestClass>(R1);
            changed.uniqueStringTest = "accepted";
            changed.Commit();

            View.Accept();

            Assert.AreEqual("accepted", Baseline.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "the baseline must now hold what the view showed");
            Assert.IsTrue(Additions.IsEmpty, "additions must be empty after accept");
            Assert.IsTrue(Removals.IsEmpty, "removals must be empty after accept");
        }

        [Test]
        public virtual void DiscardLeavesTheBaselineUntouched()
        {
            GivenBaseline(R1, "original");

            var changed = View.GetResource<MappingTestClass>(R1);
            changed.uniqueStringTest = "abandoned";
            changed.Commit();

            View.Discard();

            Assert.AreEqual("original", Baseline.GetResource<MappingTestClass>(R1).uniqueStringTest);
            Assert.AreEqual("original", View.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "and the view reads the baseline again");
            Assert.IsTrue(Additions.IsEmpty);
            Assert.IsTrue(Removals.IsEmpty);
        }

        /// <summary>
        /// Accept applies removals before additions, so a triple staged in both survives.
        /// </summary>
        [Test]
        public virtual void AcceptGivesAdditionsPrecedenceJustAsReadsDo()
        {
            GivenBaseline(R1, "original");

            // Stage the baseline triple for removal and, separately, right back as an addition.
            Removals.ExecuteUpdate(new SparqlUpdate(
                "INSERT { GRAPH @removals { ?s ?p ?o } } WHERE { GRAPH @baseline { ?s ?p ?o } }")
                .Bind("@removals", Removals).Bind("@baseline", Baseline));
            Additions.ExecuteUpdate(new SparqlUpdate(
                "INSERT { GRAPH @additions { ?s ?p ?o } } WHERE { GRAPH @removals { ?s ?p ?o } }")
                .Bind("@additions", Additions).Bind("@removals", Removals));

            Assert.AreEqual("original", View.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "precondition: additions win on read");

            View.Accept(force: true);

            Assert.AreEqual("original", Baseline.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "and additions win on accept too");
        }

        #endregion

        #region Divergence

        [Test]
        public virtual void AcceptRefusesWhenTheBaselineMovedUnderTheChange()
        {
            GivenBaseline(R1, "original");

            var changed = View.GetResource<MappingTestClass>(R1);
            changed.uniqueStringTest = "mine";
            changed.Commit();

            Assert.IsFalse(View.HasDiverged(), "nothing has moved yet");

            // A third party changes the same property.
            var theirs = Baseline.GetResource<MappingTestClass>(R1);
            theirs.uniqueStringTest = "theirs";
            theirs.Commit();

            Assert.IsTrue(View.HasDiverged(), "the triple staged for removal is no longer in the baseline");

            var ex = Assert.Throws<InvalidOperationException>(() => View.Accept());
            Assert.That(ex.Message, Does.Contain("staged"));
        }

        /// <summary>
        /// Forcing a stale change merges instead of failing — and the damage is invisible through the
        /// mapped API.
        /// </summary>
        /// <remarks>
        /// This is the whole reason <see cref="ILayeredModel.Accept"/> refuses by default. A changeset
        /// is a set of triples and INSERT/DELETE are idempotent, so applying one computed against a
        /// baseline that has moved cannot fail; it silently merges. Here the baseline ends up holding
        /// <b>two</b> values for a single-valued property.
        /// <para>
        /// Worse, and the reason this test asserts against the store rather than through a resource:
        /// <c>uniqueStringTest</c> is a <c>PropertyMapping&lt;string&gt;</c>, so a mapped read collapses
        /// the two stored values to one. The schema violation is real in the data and invisible through
        /// the API a caller would normally use to look for it.
        /// </para>
        /// </remarks>
        [Test]
        public virtual void ForcedAcceptMergesAndTheDamageIsInvisibleToMappedReads()
        {
            GivenBaseline(R1, "original");

            var changed = View.GetResource<MappingTestClass>(R1);
            changed.uniqueStringTest = "mine";
            changed.Commit();

            var theirs = Baseline.GetResource<MappingTestClass>(R1);
            theirs.uniqueStringTest = "theirs";
            theirs.Commit();

            View.Accept(force: true);

            var stored = Baseline.GetBindings(new SparqlQuery(
                $"SELECT ?o FROM <{Baseline.Uri}> WHERE {{ <{R1}> <{to.uniqueStringTest.Uri}> ?o }}",
                declarePrefixes: false))
                .Select(b => b["o"].ToString())
                .OrderBy(v => v)
                .ToList();

            Assert.AreEqual(new[] { "mine", "theirs" }, stored,
                "forcing a stale change merges rather than failing, leaving two values for a " +
                "single-valued property");

            Assert.AreEqual(1,
                Baseline.GetResource<MappingTestClass>(R1).uniqueStringTest == null ? 0 : 1,
                "and a mapped read shows only one of them, so the violation does not surface where a " +
                "caller would look for it");
        }

        [Test]
        public virtual void AnUnrelatedBaselineChangeIsNotDivergence()
        {
            GivenBaseline(R1, "original");
            GivenBaseline(R2, "second");

            var changed = View.GetResource<MappingTestClass>(R1);
            changed.uniqueStringTest = "mine";
            changed.Commit();

            var other = Baseline.GetResource<MappingTestClass>(R2);
            other.uniqueStringTest = "also changed";
            other.Commit();

            Assert.IsFalse(View.HasDiverged(), "a change elsewhere in the baseline is not a conflict");
            Assert.DoesNotThrow(() => View.Accept());
        }

        #endregion

        #region Still refused

        [Test]
        public virtual void CallerUpdatesAndSerializationStayRefused()
        {
            Assert.Throws<NotSupportedException>(
                () => View.ExecuteUpdate(new SparqlUpdate($"CLEAR GRAPH <{Additions.Uri}>")));
            Assert.Throws<NotSupportedException>(() => View.Clear());
            Assert.Throws<NotSupportedException>(
                () => View.Write(new System.IO.MemoryStream(), RdfSerializationFormat.Turtle));
        }

        #endregion
    }
}
