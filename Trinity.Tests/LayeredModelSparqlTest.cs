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
using System.Reflection;
using NUnit.Framework;
using Semiodesk.Trinity.Query.Sparql;

namespace Semiodesk.Trinity.Tests
{
    /// <summary>
    /// Unit tests for the SPARQL a layered model emits. These need no store: they assert the shape
    /// of the generated text and the invariants that make it correct, which is where the subtle
    /// failures live.
    /// </summary>
    [TestFixture]
    public class LayeredModelSparqlTest
    {
        #region Members

        private IStore _store;

        private ILayeredModel _view;

        private static readonly Uri BaseGraph = new Uri("http://example.org/g/baseline");
        private static readonly Uri AddGraph = new Uri("http://example.org/g/additions");
        private static readonly Uri RemGraph = new Uri("http://example.org/g/removals");

        #endregion

        [SetUp]
        public void SetUp()
        {
            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            _view = _store.CreateLayeredModel(BaseGraph, AddGraph, RemGraph);
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
        }

        #region The guard primitive

        /// <summary>
        /// A pattern with a variable is subtracted with <c>MINUS</c>, which engines evaluate as an
        /// anti-join rather than a per-row probe.
        /// </summary>
        /// <remarks>
        /// The overlay also carries a <c>FILTER NOT EXISTS</c> that keeps the two branches disjoint,
        /// so the assertion is about which primitive guards the <b>baseline</b> branch - the part
        /// before the <c>UNION</c>.
        /// </remarks>
        [Test]
        public void OverlayUsesMinusWhenThePatternBindsAVariable()
        {
            string overlay = LayeredModelSparql.Overlay(_view, "?s", "?p", "?o");
            string baselineBranch = overlay.Substring(0, overlay.IndexOf("UNION", StringComparison.Ordinal));

            Assert.IsTrue(baselineBranch.Contains("MINUS"), overlay);
            Assert.IsFalse(baselineBranch.Contains("NOT EXISTS"), overlay);
        }

        /// <summary>
        /// The two branches must be disjoint, or the bag semantics of <c>UNION</c> duplicate a triple
        /// that is in both the baseline and the additions.
        /// </summary>
        [Test]
        public void OverlayBranchesAreDisjoint()
        {
            string overlay = LayeredModelSparql.Overlay(_view, "?s", "?p", "?o");
            string additionsBranch = overlay.Substring(overlay.IndexOf("UNION", StringComparison.Ordinal));

            Assert.IsTrue(additionsBranch.Contains(AddGraph.ToString()),
                "the branch after UNION is the additions branch");
            Assert.IsTrue(additionsBranch.Contains("FILTER NOT EXISTS"),
                "the additions branch must exclude what the baseline branch already yielded, or the " +
                "two overlap and UNION duplicates the solution:\n" + overlay);
            Assert.IsTrue(additionsBranch.Contains(BaseGraph.ToString()),
                "that exclusion is expressed against the baseline graph:\n" + overlay);
        }

        /// <summary>
        /// The correctness rule that decides the primitive: SPARQL <c>MINUS</c> removes nothing when
        /// the two sides share no variables, and a fully ground pattern has none — so there it would
        /// silently fail to subtract. Ground patterns must use <c>FILTER NOT EXISTS</c>.
        /// </summary>
        [Test]
        public void OverlayUsesNotExistsWhenThePatternIsFullyGround()
        {
            string overlay = LayeredModelSparql.Overlay(_view,
                "<http://example.org/s>", "<http://example.org/p>", "<http://example.org/o>");

            Assert.IsTrue(overlay.Contains("FILTER NOT EXISTS"), overlay);
            Assert.IsFalse(overlay.Contains("MINUS"), overlay);
        }

        /// <summary>
        /// Verifies the trap the previous test guards against is real on this engine, so the rule
        /// cannot be "simplified" away later: a ground pattern subtracted with MINUS comes back.
        /// </summary>
        [Test]
        public void MinusWouldNotSubtractAGroundPattern()
        {
            const string triple = "<http://example.org/s> <http://example.org/p> <http://example.org/o>";

            _store.ExecuteNonQuery(new SparqlUpdate(
                $"INSERT DATA {{ GRAPH <{BaseGraph}> {{ {triple} . }} GRAPH <{RemGraph}> {{ {triple} . }} }}"));

            Assert.IsFalse(Ask(GuardedWithNotExists(triple)),
                "FILTER NOT EXISTS must subtract the removed triple");

            Assert.IsTrue(Ask(GuardedWithMinus(triple)),
                "MINUS must NOT subtract a ground pattern - this is why Overlay() switches primitive. " +
                "If this assertion ever fails the engine changed and the rule can be revisited.");
        }

        private static string GuardedWithNotExists(string triple) =>
            $"ASK FROM NAMED <{BaseGraph}> FROM NAMED <{RemGraph}> {{ GRAPH <{BaseGraph}> {{ {triple} }} " +
            $"FILTER NOT EXISTS {{ GRAPH <{RemGraph}> {{ {triple} }} }} }}";

        private static string GuardedWithMinus(string triple) =>
            $"ASK FROM NAMED <{BaseGraph}> FROM NAMED <{RemGraph}> {{ GRAPH <{BaseGraph}> {{ {triple} }} " +
            $"MINUS {{ GRAPH <{RemGraph}> {{ {triple} }} }} }}";

        private bool Ask(string sparql) =>
            _store.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)).GetAnwser();

        #endregion

        #region Overlay structure

        /// <summary>
        /// The removals guard applies only to the baseline branch, which is what makes additions win.
        /// </summary>
        [Test]
        public void OverlayGuardsOnlyTheBaselineBranch()
        {
            string overlay = LayeredModelSparql.Overlay(_view, "?s", "?p", "?o");

            int union = overlay.IndexOf("UNION", StringComparison.Ordinal);
            string baselineBranch = overlay.Substring(0, union);
            string additionsBranch = overlay.Substring(union);

            Assert.IsTrue(baselineBranch.Contains(BaseGraph.ToString()), overlay);
            Assert.IsTrue(baselineBranch.Contains(RemGraph.ToString()),
                "the baseline branch is the one the removals guard applies to");

            // The additions branch references the removals graph only inside its disjointness guard,
            // never to subtract from itself - subtracting there would stop additions winning.
            int additionsGraphAt = additionsBranch.IndexOf(AddGraph.ToString(), StringComparison.Ordinal);
            int notExistsAt = additionsBranch.IndexOf("FILTER NOT EXISTS", StringComparison.Ordinal);

            Assert.Less(additionsGraphAt, notExistsAt,
                "the additions graph is matched first, then filtered - not guarded before matching");
        }

        /// <summary>
        /// The dataset clause names the graphs rather than merging them; FROM would union the
        /// removals graph straight back in.
        /// </summary>
        [Test]
        public void DatasetClauseNamesAllThreeGraphsAndNeverMergesThem()
        {
            string clause = LayeredModelSparql.NamedDatasetClause(_view);

            Assert.AreEqual(3, CountOccurrences(clause, "FROM NAMED"));
            Assert.IsTrue(clause.Contains($"<{BaseGraph}>"));
            Assert.IsTrue(clause.Contains($"<{AddGraph}>"));
            Assert.IsTrue(clause.Contains($"<{RemGraph}>"));

            // No bare FROM: every FROM in the clause must be a FROM NAMED.
            Assert.AreEqual(3, CountOccurrences(clause, "FROM "));
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0, i = 0;

            while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
            {
                count++;
                i += needle.Length;
            }

            return count;
        }

        #endregion

        #region ProvidesStatements

        /// <summary>
        /// Resource materialization refuses any query for which <c>ProvidesStatements()</c> is
        /// false, and that is decided by a token-level heuristic which wants exactly three
        /// same-ordered global variables. The overlay adds nesting, UNION and VALUES around the
        /// triple patterns, so this is easy to break without noticing — every emitted shape is
        /// asserted here.
        /// </summary>
        [TestCaseSource(nameof(EmittedShapes))]
        public void EveryEmittedShapeProvidesStatements(string label, string sparql)
        {
            var query = new SparqlQuery(sparql, declarePrefixes: false);

            Assert.IsTrue(query.ProvidesStatements(),
                $"{label} must provide statements or GetResources() refuses it outright:\n{sparql}");
            Assert.AreEqual(new[] { "s", "p", "o" }, query.GetGlobalScopeVariableNames(), label);
        }

        public static IEnumerable<TestCaseData> EmittedShapes()
        {
            var store = StoreFactory.CreateStore("provider=dotnetrdf");
            var view = store.CreateLayeredModel(BaseGraph, AddGraph, RemGraph);

            string dataset = LayeredModelSparql.NamedDatasetClause(view);
            string wildcard = LayeredModelSparql.Overlay(view, "?s", "?p", "?o");
            string typed = LayeredModelSparql.Overlay(view, "?s", "a", "<http://example.org/Thing>");
            string values = LayeredModelSparql.BindSubjects("?s", new[] { new Uri("http://example.org/r1") });

            yield return new TestCaseData("subject-bound resource read",
                $"SELECT DISTINCT ?s ?p ?o {dataset}WHERE {{ {values}{wildcard} }}");

            yield return new TestCaseData("multi-subject resource read",
                "SELECT DISTINCT ?s ?p ?o " + dataset + "WHERE { " +
                LayeredModelSparql.BindSubjects("?s", new[] { new Uri("http://example.org/r1"), new Uri("http://example.org/r2") }) +
                wildcard + " }");

            yield return new TestCaseData("type-constrained read",
                $"SELECT DISTINCT ?s ?p ?o {dataset}WHERE {{ {typed} {wildcard} }}");
        }

        #endregion

        #region Completeness guard

        /// <summary>
        /// The one structural defence against the design's dangerous failure mode: a read path added
        /// to <see cref="LayeredModel"/> later that forgets the overlay and quietly returns
        /// unsubtracted triples.
        /// </summary>
        /// <remarks>
        /// Every public <see cref="IModel"/> member is either in the honoured list below — and
        /// covered by an assertion in <c>LayeredModelTest</c> — or it must throw. A new member, or a
        /// member moved between the two groups, fails this test and forces the decision to be made
        /// explicitly rather than by omission.
        /// </remarks>
        [Test]
        public void EveryModelMemberIsEitherHonouredOrThrows()
        {
            var honoured = new HashSet<string>
            {
                // Reads that apply the overlay. Each is asserted in LayeredModelTest.
                "Uri", "get_Uri",
                "IsEmpty", "get_IsEmpty",
                "IgnoreUnmappedProperties", "get_IgnoreUnmappedProperties", "set_IgnoreUnmappedProperties",
                "ContainsResource",
                "GetResource",
                "GetResources",
                "AsQueryable",
                // Reads that rewrite a caller query, refusing forms they cannot handle.
                "ExecuteQuery", "GetBindings",
                // Writes, which stage into the additions and removals graphs rather than write.
                "AddResource", "CreateResource", "DeleteResource", "DeleteResources",
                "UpdateResource", "UpdateResources",
                // Delegated to the store so Accept can run in a real transaction where there is one.
                "BeginTransaction",
            };

            var refused = new HashSet<string>
            {
                // Cannot be routed into the layers: there is no way to tell which of a caller update's
                // effects should become an addition and which a removal.
                "ExecuteUpdate",
                // Ambiguous on a view; Discard() is the meaningful operation.
                "Clear",
                // A different operation from reading into or writing out a model.
                "Read", "Write",
            };

            var unclassified = new List<string>();

            foreach (MemberInfo member in typeof(IModel).GetMembers())
            {
                string name = member.Name;

                if (!honoured.Contains(name) && !refused.Contains(name))
                {
                    unclassified.Add(name);
                }
            }

            Assert.IsEmpty(unclassified,
                "IModel gained member(s) that a layered model neither honours nor refuses: " +
                string.Join(", ", unclassified) +
                ". Decide explicitly: apply the overlay and add a LayeredModelTest assertion, or throw. " +
                "Leaving it unhandled risks silently returning triples staged for removal.");

            // And prove the refusals really refuse, rather than merely being listed above.
            var view = _view;

            var alwaysThrows = new List<TestDelegate>
            {
                () => view.ExecuteUpdate(new SparqlUpdate("CLEAR GRAPH <urn:x>")),
                () => view.Clear(),
                () => view.Read(new Uri("urn:x"), RdfSerializationFormat.Turtle, false),
                () => view.Write(new System.IO.MemoryStream(), RdfSerializationFormat.Turtle),
            };

            foreach (TestDelegate call in alwaysThrows)
            {
                Assert.Throws<NotSupportedException>(call);
            }
        }

        #endregion

        #region Layer validation

        [Test]
        public void OverlappingLayersAreRejected()
        {
            Assert.Throws<ArgumentException>(() => _store.CreateLayeredModel(BaseGraph, BaseGraph, RemGraph));
            Assert.Throws<ArgumentException>(() => _store.CreateLayeredModel(BaseGraph, AddGraph, BaseGraph));
            Assert.Throws<ArgumentException>(() => _store.CreateLayeredModel(BaseGraph, AddGraph, AddGraph));
        }

        /// <summary>
        /// A layered model cannot span two stores, and must say so rather than answer from whichever
        /// graphs the executing store happens to hold.
        /// </summary>
        /// <remarks>
        /// Measured before this guard existed: with the baseline in Virtuoso and the layers in an
        /// in-memory store, a read returned the triple staged for removal and raised nothing. With the
        /// stores swapped it silently dropped the baseline instead. Both are exactly the silent wrong
        /// answer this design exists to prevent.
        /// </remarks>
        [Test]
        public void ModelsFromAnotherStoreAreRejected()
        {
            var other = StoreFactory.CreateStore("provider=dotnetrdf");

            try
            {
                var ex = Assert.Throws<NotSupportedException>(() => _store.CreateLayeredModel(
                    _store.GetModel(BaseGraph), other.GetModel(AddGraph), _store.GetModel(RemGraph)));

                Assert.That(ex.Message, Does.Contain("different store"));

                Assert.Throws<NotSupportedException>(() => _store.CreateLayeredModel(
                    other.GetModel(BaseGraph), _store.GetModel(AddGraph), _store.GetModel(RemGraph)));

                Assert.Throws<NotSupportedException>(() => _store.CreateLayeredModel(
                    _store.GetModel(BaseGraph), _store.GetModel(AddGraph), other.GetModel(RemGraph)));
            }
            finally
            {
                other.Dispose();
            }
        }

        /// <summary>
        /// Models from the same store are of course accepted - the guard must not be a blanket refusal.
        /// </summary>
        [Test]
        public void ModelsFromTheSameStoreAreAccepted()
        {
            Assert.DoesNotThrow(() => _store.CreateLayeredModel(
                _store.GetModel(BaseGraph), _store.GetModel(AddGraph), _store.GetModel(RemGraph)));
        }

        [Test]
        public void LayeredModelHasNoUriAndIsNotAModelGroup()
        {
            Assert.IsNull(_view.Uri, "a view spanning three graphs has no URI of its own");

            // Keeping the types apart is what stops the union-only IModelGroup code paths from
            // treating a layered view as a plain group.
            Assert.IsFalse(_view is IModelGroup);
        }

        #endregion
    }
}
