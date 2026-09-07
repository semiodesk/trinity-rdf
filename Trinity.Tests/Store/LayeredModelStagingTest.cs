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
using System.Data;
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

        /// <summary>
        /// A resource in the baseline with one mapped value. Protected so a backend subclass overriding
        /// a store-specific expectation can build the same starting point.
        /// </summary>
        protected MappingTestClass GivenBaselineValue(UriRef uri, string value)
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
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");
            GivenBaselineValue(R2, "second");

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
            GivenBaselineValue(R1, "original");

            View.DeleteResource(R1);

            Assert.IsFalse(View.ContainsResource(R1), "the resource must be invisible through the view");
            Assert.IsTrue(Baseline.ContainsResource(R1), "but still present in the baseline");
        }

        #endregion

        #region Accept and discard

        [Test]
        public virtual void AcceptAppliesTheStagedChangeAndEmptiesTheLayers()
        {
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");

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
            GivenBaselineValue(R1, "original");
            GivenBaselineValue(R2, "second");

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

        #region The reconstructible ancestor (ADR-0042)

        /// <summary>
        /// Every triple of a graph, as comparable strings.
        /// </summary>
        private static ISet<string> Triples(IModel model)
        {
            var query = new SparqlQuery($"SELECT ?s ?p ?o FROM <{model.Uri}> WHERE {{ ?s ?p ?o }}",
                declarePrefixes: false);

            return Rows(model.GetBindings(query));
        }

        /// <summary>The effective triples, read through the view.</summary>
        private ISet<string> Effective()
        {
            return Rows(View.GetBindings(new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o }",
                declarePrefixes: false)));
        }

        private static ISet<string> Rows(IEnumerable<BindingSet> bindings)
        {
            return new HashSet<string>(bindings.Select(b =>
                string.Join("|", b.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Value))));
        }

        /// <summary>
        /// The reconstruction ADR-0042 rests on: <c>B0 = (effective \ A) u R</c>.
        /// </summary>
        private ISet<string> ReconstructedAncestor()
        {
            var reconstructed = new HashSet<string>(Effective());

            reconstructed.ExceptWith(Triples(Additions));
            reconstructed.UnionWith(Triples(Removals));

            return reconstructed;
        }

        /// <summary>
        /// The pre-change baseline is recoverable from the working copy without being stored, for every
        /// changeset the view itself produces.
        /// </summary>
        /// <remarks>
        /// ADR-0042 presents this as a measured table and the proposed branching work depends on it, but
        /// only the two invariants underneath it were asserted - not the inversion itself. The three
        /// disciplined cases are exact because <c>TrySerializeResourceDelta</c> records only what
        /// changed, which is what keeps <c>A n B0 = {}</c> and <c>R subset of B0</c> true.
        /// </remarks>
        [Test]
        public virtual void TheAncestorIsRecoverableAfterAValueChange()
        {
            GivenBaselineValue(R1, "original");

            ISet<string> before = Triples(Baseline);

            var staged = View.GetResource<MappingTestClass>(R1);
            staged.uniqueStringTest = "changed";
            staged.Commit();

            CollectionAssert.IsNotEmpty(before, "precondition: the baseline is not empty");
            CollectionAssert.AreEquivalent(before, ReconstructedAncestor(),
                "a value change must invert exactly");
        }

        [Test]
        public virtual void TheAncestorIsRecoverableAfterAPureAddition()
        {
            GivenBaselineValue(R1, "original");

            ISet<string> before = Triples(Baseline);

            var added = View.CreateResource<MappingTestClass>(R2);
            added.uniqueStringTest = "brand new";
            added.Commit();

            CollectionAssert.AreEquivalent(before, ReconstructedAncestor(),
                "a pure addition must invert exactly");
        }

        [Test]
        public virtual void TheAncestorIsRecoverableAfterAPureRemoval()
        {
            GivenBaselineValue(R1, "original");
            GivenBaselineValue(R2, "second");

            ISet<string> before = Triples(Baseline);

            View.DeleteResource(R2);

            CollectionAssert.AreEquivalent(before, ReconstructedAncestor(),
                "a pure removal must invert exactly");
        }

        /// <summary>
        /// Reconstruction is lossy when the invariants are violated - which only a caller writing to the
        /// layer graphs directly can do.
        /// </summary>
        /// <remarks>
        /// The other half of ADR-0042's table, and the reason staging belongs to the view rather than to
        /// the caller. These assert the <i>failure</i> deliberately: if reconstruction ever became exact
        /// here, the invariants would have stopped being load-bearing and the ADR would need rewriting.
        /// </remarks>
        [Test]
        public virtual void ReAddingABaselineTripleLosesItFromTheReconstruction()
        {
            GivenBaselineValue(R1, "original");

            ISet<string> before = Triples(Baseline);

            // Violates A n B0 = {} - a triple staged as an addition that the baseline already holds.
            Additions.ExecuteUpdate(new SparqlUpdate(
                $"INSERT {{ GRAPH <{Additions.Uri}> {{ ?s ?p ?o }} }} " +
                $"WHERE {{ GRAPH <{Baseline.Uri}> {{ ?s ?p ?o }} }}"));

            Assert.IsTrue(ReconstructedAncestor().Count < before.Count,
                "subtracting the additions removes triples the baseline genuinely had, so the " +
                "reconstruction is short - this is why the view must own staging");
        }

        [Test]
        public virtual void StagingTheRemovalOfAnAbsentTripleInventsIt()
        {
            GivenBaselineValue(R1, "original");

            ISet<string> before = Triples(Baseline);

            // Violates R subset of B0 - a removal of something the baseline never held.
            Removals.ExecuteUpdate(new SparqlUpdate(
                "INSERT DATA { GRAPH @removals { @subject @predicate 'never there' } }")
                .Bind("@removals", Removals)
                .Bind("@subject", R2)
                .Bind("@predicate", to.uniqueStringTest.Uri));

            ISet<string> reconstructed = ReconstructedAncestor();

            Assert.IsTrue(reconstructed.Count > before.Count,
                "unioning the removals invents a triple the baseline never had");
            CollectionAssert.IsSupersetOf(reconstructed, before,
                "and it does so by addition, so nothing genuine is lost");
        }

        #endregion

        #region Precondition completeness (ADR-0042)

        /// <summary>
        /// The precondition is deliberately conservative: it flags the benign case where a third party
        /// already made the same removal.
        /// </summary>
        /// <remarks>
        /// The fourth row of ADR-0042's precondition table, and the only one that was unasserted. It
        /// matters because it is the cost of soundness - the check cannot distinguish "the baseline moved
        /// in a way that matters" from "someone already did what I was about to do", and that is the
        /// documented trade rather than a defect.
        /// </remarks>
        [Test]
        public virtual void TheSameRemovalByAThirdPartyIsFlaggedAnyway()
        {
            GivenBaselineValue(R1, "original");

            var staged = View.GetResource<MappingTestClass>(R1);
            staged.uniqueStringTest = "changed";
            staged.Commit();

            Assert.IsFalse(View.HasDiverged(), "precondition: no divergence yet");

            // A third party removes the very triple this changeset stages for removal.
            Baseline.ExecuteUpdate(new SparqlUpdate(
                "DELETE WHERE { GRAPH @baseline { @subject @predicate 'original' } }")
                .Bind("@baseline", Baseline)
                .Bind("@subject", R1)
                .Bind("@predicate", to.uniqueStringTest.Uri));

            Assert.IsTrue(View.HasDiverged(),
                "conservative by design: the removal's precondition no longer holds, even though " +
                "applying the change would still give the intended result");

            Assert.Throws<InvalidOperationException>(() => View.Accept());
            Assert.DoesNotThrow(() => View.Accept(force: true));

            Assert.AreEqual("changed", Baseline.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "and the outcome was benign after all, which is the cost of soundness");
        }

        #endregion

        #region Multi-operation atomicity (ADR-0042)

        /// <summary>
        /// Whether this backend rolls back a multi-operation request whose later operation fails.
        /// </summary>
        /// <remarks>
        /// Overridden to <c>false</c> where the claim could not be probed rather than where it is known
        /// to be untrue - see the Virtuoso subclass.
        /// </remarks>
        protected virtual bool MultiOperationRequestIsAtomic => true;

        /// <summary>
        /// <c>Accept()</c> is one multi-operation request, and ADR-0042 claims request atomicity is what
        /// protects it on the stores whose transactions are no-ops.
        /// </summary>
        /// <remarks>
        /// That claim was measured once in a throwaway harness and then relied on by the design: accept
        /// starts a transaction unconditionally, which is real only on Virtuoso, so on the other two
        /// backends a half-applied changeset would be silent corruption of the baseline. It belongs in
        /// the suite, because it is a store behaviour and every store behaviour in this work has
        /// eventually differed from the assumption.
        /// </remarks>
        [Test]
        public virtual void AFailedOperationRollsBackTheOnesBeforeIt()
        {
            if (!MultiOperationRequestIsAtomic)
            {
                Assert.Inconclusive(
                    "No failure can be injected into a multi-operation request on this backend: it " +
                    "reports success for every candidate. Accept() relies on its real transaction here " +
                    "instead. See ADR-0042.");
            }

            GivenBaselineValue(R1, "original");

            // The first operation is valid; the second fails at runtime. LOAD of an unresolvable URL is
            // an error per SPARQL 1.1 absent SILENT.
            var update = new SparqlUpdate(
                "INSERT DATA { GRAPH @g { @subject @predicate 'injected' } }; " +
                "LOAD <http://127.0.0.1:9/does-not-resolve>")
                .Bind("@g", Baseline)
                .Bind("@subject", R2)
                .Bind("@predicate", to.uniqueStringTest.Uri);

            // Catch rather than Throws: the store wraps the transport failure, and which wrapper it
            // uses is not the point of this test.
            Assert.Catch(() => Baseline.ExecuteUpdate(update),
                "the failing operation must surface rather than be swallowed");

            Assert.IsFalse(Baseline.ContainsResource(R2),
                "and the operation before it must have been rolled back - this is what makes Accept() " +
                "safe on a backend whose ITransaction is a NoOpTransaction");
        }

        /// <summary>
        /// The corollary ADR-0042 records: on these backends rollback demonstrably undoes nothing, so a
        /// transaction gives a false sense of safety and request atomicity is what protects the write.
        /// </summary>
        [Test]
        public virtual void RollbackOnANoOpTransactionUndoesNothing()
        {
            if (!MultiOperationRequestIsAtomic)
            {
                Assert.Inconclusive("This backend has a real transaction; see the Virtuoso override.");
            }

            GivenBaselineValue(R1, "original");

            using (ITransaction transaction = Store.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Assert.IsNotNull(transaction, "ADR-0039: never null, even where it isolates nothing");

                var staged = View.GetResource<MappingTestClass>(R1);
                staged.uniqueStringTest = "changed";
                View.UpdateResource(staged, transaction);

                transaction.Rollback();
            }

            Assert.AreEqual("changed", View.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "the rollback undid nothing, which is why Accept() cannot rely on it here");
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
