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
    /// With empty additions and removals a layered view must answer exactly as the plain baseline
    /// model does — or refuse. Answering <i>differently</i> is a rewriter defect by construction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the cheapest possible oracle for the rewriter: no expected results have to be written
    /// down, because the baseline model computes them. That matters because the rewriter's failure
    /// mode is a query that still parses and still returns rows, just not the right ones — which a
    /// corpus of hand-written expectations will only catch where someone thought to write the case
    /// down.
    /// </para>
    /// <para>
    /// It earns its place: three defects found in review were of exactly this shape and all three are
    /// caught here — a union reached as an alternative of another union losing its disjunction, and
    /// GROUP BY / ORDER BY expressions escaping the round-trip check.
    /// </para>
    /// </remarks>
    [TestFixture]
    public abstract class LayeredModelDifferentialTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members

        private const string EX = "http://example.org/diff/";

        protected IModel Baseline;
        protected IModel Additions;
        protected IModel Removals;
        protected ILayeredModel View;

        #endregion

        #region Setup

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            Baseline = Store.GetModel(BaseUri.GetUriRef("diff-baseline"));
            Additions = Store.GetModel(BaseUri.GetUriRef("diff-additions"));
            Removals = Store.GetModel(BaseUri.GetUriRef("diff-removals"));

            foreach (IModel model in new[] { Baseline, Additions, Removals })
            {
                if (!model.IsEmpty) model.Clear();
            }

            View = Store.CreateLayeredModel(Baseline.Uri, Additions.Uri, Removals.Uri);

            Baseline.ExecuteUpdate(new SparqlUpdate(
                "INSERT DATA { GRAPH @g { " +
                $"<{EX}a> a <{EX}Thing> ; <{EX}rank> 1 ; <{EX}q> 'qa' ; <{EX}peer> <{EX}c> . " +
                $"<{EX}b> a <{EX}Thing> ; <{EX}rank> 2 ; <{EX}r> 'rb' . " +
                $"<{EX}c> a <{EX}Other> ; <{EX}rank> 3 ; <{EX}s> <{EX}a> . " +
                "} }").Bind("@g", Baseline));

            // Additions and removals stay empty: that is what makes the baseline the oracle.
        }

        [TearDown]
        public void TearDownDifferential()
        {
            Baseline.Clear();
            Additions.Clear();
            Removals.Clear();
        }

        #endregion

        #region The sweep

        /// <summary>
        /// Query shapes the rewriter claims to support, chosen to exercise how patterns nest rather
        /// than what they match.
        /// </summary>
        public static IEnumerable<TestCaseData> Shapes()
        {
            string thing = $"<{EX}Thing>", other = $"<{EX}Other>";
            string rank = $"<{EX}rank>", q = $"<{EX}q>", r = $"<{EX}r>", s = $"<{EX}s>", peer = $"<{EX}peer>";

            var shapes = new (string Label, string Sparql, bool Ordered)[]
            {
                ("basic graph pattern",        $"SELECT ?x ?v WHERE {{ ?x {rank} ?v }}", false),
                ("join",                       $"SELECT ?x WHERE {{ ?x a {thing} . ?x {rank} ?v }}", false),
                ("predicate-object list",      $"SELECT ?x WHERE {{ ?x a {thing} ; {rank} ?v }}", false),
                ("object list",                $"SELECT ?x WHERE {{ ?x a {thing}, {thing} }}", false),
                ("OPTIONAL",                   $"SELECT ?x ?w WHERE {{ ?x {rank} ?v . OPTIONAL {{ ?x {q} ?w }} }}", false),
                ("MINUS",                      $"SELECT ?x WHERE {{ ?x {rank} ?v . MINUS {{ ?x a {thing} }} }}", false),
                ("UNION",                      $"SELECT ?x WHERE {{ {{ ?x {q} ?v }} UNION {{ ?x {r} ?v }} }}", false),

                // Regression, review finding 1: an alternative of a union may itself be a union.
                // A UNION B UNION C parses left-nested, so this is the ordinary three-way case.
                ("UNION nested left",          $"SELECT ?x WHERE {{ {{ {{ ?x {q} ?v }} UNION {{ ?x {r} ?v }} }} UNION {{ ?x {s} ?v }} }}", false),
                ("UNION nested right",         $"SELECT ?x WHERE {{ {{ ?x {q} ?v }} UNION {{ {{ ?x {r} ?v }} UNION {{ ?x {s} ?v }} }} }}", false),
                ("UNION three-way flat",       $"SELECT ?x WHERE {{ {{ ?x {q} ?v }} UNION {{ ?x {r} ?v }} UNION {{ ?x {s} ?v }} }}", false),
                ("UNION four-way",             $"SELECT ?x WHERE {{ {{ ?x {q} ?v }} UNION {{ ?x {r} ?v }} UNION {{ ?x {s} ?v }} UNION {{ ?x {peer} ?v }} }}", false),
                ("OPTIONAL inside UNION",      $"SELECT ?x ?w WHERE {{ {{ ?x {q} ?v }} UNION {{ ?x {rank} ?v . OPTIONAL {{ ?x {r} ?w }} }} }}", false),
                ("UNION inside OPTIONAL",      $"SELECT ?x WHERE {{ ?x {rank} ?v . OPTIONAL {{ {{ ?x {q} ?w }} UNION {{ ?x {r} ?w }} }} }}", false),
                ("MINUS inside UNION",         $"SELECT ?x WHERE {{ {{ ?x {rank} ?v . MINUS {{ ?x a {thing} }} }} UNION {{ ?x {s} ?v }} }}", false),
                ("UNION of two types",         $"SELECT ?x WHERE {{ {{ ?x a {thing} }} UNION {{ ?x a {other} }} }}", false),

                ("sub-SELECT",                 $"SELECT ?x WHERE {{ ?x {rank} ?v . {{ SELECT ?x WHERE {{ ?x a {thing} }} }} }}", false),
                ("sub-SELECT with LIMIT",      $"SELECT ?x WHERE {{ ?x {rank} ?v . {{ SELECT ?x WHERE {{ ?x {rank} ?z }} ORDER BY ?z LIMIT 2 }} }}", false),
                ("FILTER",                     $"SELECT ?x WHERE {{ ?x {rank} ?v . FILTER(?v > 1) }}", false),
                ("FILTER negated",             $"SELECT ?x WHERE {{ ?x {rank} ?v . FILTER(!(?v < 2)) }}", false),
                ("FILTER IN",                  $"SELECT ?x WHERE {{ ?x {rank} ?v . FILTER(?v IN (1, 3)) }}", false),
                ("VALUES",                     $"SELECT ?x WHERE {{ VALUES ?x {{ <{EX}a> <{EX}b> }} ?x {rank} ?v }}", false),

                // Regression, review finding 5: BIND is order-sensitive, unlike a triple pattern.
                ("BIND before a pattern",      $"SELECT ?v WHERE {{ BIND(<{EX}a> AS ?x) ?x {rank} ?v }}", false),
                ("BIND after a pattern",       $"SELECT ?y WHERE {{ ?x {rank} ?v . BIND(?v AS ?y) }}", false),
                ("BIND between patterns",      $"SELECT ?y WHERE {{ ?x a {thing} . BIND(?x AS ?y) . ?x {rank} ?v }}", false),

                ("DISTINCT",                   $"SELECT DISTINCT ?v WHERE {{ ?x {rank} ?v }}", false),
                ("COUNT(*)",                   $"SELECT (COUNT(*) AS ?n) WHERE {{ ?x {rank} ?v }}", false),
                ("COUNT DISTINCT",             $"SELECT (COUNT(DISTINCT ?x) AS ?n) WHERE {{ ?x {rank} ?v }}", false),
                ("GROUP BY",                   $"SELECT ?x (COUNT(?v) AS ?n) WHERE {{ ?x {rank} ?v }} GROUP BY ?x", false),
                ("GROUP BY with HAVING",       $"SELECT ?x WHERE {{ ?x {rank} ?v }} GROUP BY ?x HAVING(COUNT(?v) = 1)", false),
                ("SELECT *",                   $"SELECT * WHERE {{ ?x {s} ?v }}", false),

                ("ORDER BY",                   $"SELECT ?x WHERE {{ ?x {rank} ?v }} ORDER BY DESC(?v)", true),
                ("ORDER BY two keys",          $"SELECT ?x WHERE {{ ?x {rank} ?v }} ORDER BY ?v DESC(?x)", true),
                ("ORDER BY with LIMIT",        $"SELECT ?x WHERE {{ ?x {rank} ?v }} ORDER BY ?v LIMIT 2", true),
                ("ORDER BY with OFFSET",       $"SELECT ?x WHERE {{ ?x {rank} ?v }} ORDER BY ?v OFFSET 1", true),
            };

            return shapes.Select(x => new TestCaseData(x.Label, x.Sparql, x.Ordered));
        }

        [TestCaseSource(nameof(Shapes))]
        public virtual void ViewAgreesWithTheBaselineOrRefuses(string label, string sparql, bool ordered)
        {
            string expected = string.Join(" | ", Rows(Baseline, sparql, ordered));

            string actual;

            try
            {
                actual = string.Join(" | ", Rows(View, sparql, ordered));
            }
            catch (NotSupportedException)
            {
                // Refusing is always allowed; the promise is only that it never answers differently.
                Assert.Pass($"{label}: refused");

                return;
            }

            Assert.AreEqual(expected, actual,
                $"{label} answered differently through the view than against the baseline, with nothing " +
                $"staged - so the rewrite changed the query's meaning.\n  query: {sparql}");
        }

        /// <summary>
        /// ASK is separate because it yields an answer rather than bindings.
        /// </summary>
        [Test]
        public virtual void AskAgreesWithTheBaseline()
        {
            foreach (string sparql in new[]
            {
                $"ASK {{ <{EX}a> <{EX}rank> 1 }}",
                $"ASK {{ <{EX}a> <{EX}rank> 99 }}",
                $"ASK {{ ?x a <{EX}Thing> }}",
                $"ASK {{ {{ ?x <{EX}q> ?v }} UNION {{ ?x <{EX}r> ?v }} }}",
            })
            {
                bool expected = Baseline.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)).GetAnwser();
                bool actual = View.ExecuteQuery(new SparqlQuery(sparql, declarePrefixes: false)).GetAnwser();

                Assert.AreEqual(expected, actual, sparql);
            }
        }

        private static IEnumerable<string> Rows(IModel model, string sparql, bool ordered)
        {
            var rows = model.GetBindings(new SparqlQuery(sparql, declarePrefixes: false))
                .Select(b => string.Join(",", b.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                                              .Select(kv => kv.Key + "=" + kv.Value)))
                .ToList();

            if (!ordered)
            {
                rows.Sort(StringComparer.Ordinal);
            }

            return rows;
        }

        #endregion
    }
}
