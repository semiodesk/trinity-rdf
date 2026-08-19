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
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Patterns;

namespace Semiodesk.Trinity.Tests
{
    /// <summary>
    /// Verifies that <see cref="SparqlExpressionWriter"/> round-trips a SPARQL expression without
    /// changing what it means — including the cases where dotNetRDF's own serialization does not.
    /// </summary>
    /// <remarks>
    /// Each case is asserted twice: that our output re-parses to the same expression tree, and
    /// (documenting the reason this class exists) which cases dotNetRDF gets wrong. The second half
    /// is not there to criticise dotNetRDF but to pin the defect: if a future version fixes it, these
    /// assertions fail and the writer can be reconsidered.
    /// </remarks>
    [TestFixture]
    public class SparqlExpressionWriterTest
    {
        /// <summary>
        /// Expressions dotNetRDF re-serializes faithfully, and expressions it does not — both must
        /// round-trip through <see cref="SparqlExpressionWriter"/>.
        /// </summary>
        private static readonly string[] Expressions =
        {
            "?r >= 3",
            "?r != 1",
            "?r > 1 && ?r < 4",
            "(?r + 1) * 2 > 6",
            "str(?l) = 'x'",
            "!bound(?l)",
            "regex(str(?l), 'a', 'i')",
            "?r IN (1, 2, 3)",
            "?r NOT IN (1, 2)",
            "lang(?l) = 'en'",
            "datatype(?r) = <http://www.w3.org/2001/XMLSchema#integer>",
            "isIRI(?s)",
            "-?r < 0",
            "abs(?r - 5) > 1",
            "if(?r > 2, 'hi', 'lo') = 'hi'",
            "coalesce(?l, 'none') = 'none'",
            // The ones dotNetRDF drops the parentheses from.
            "!(?r < 3)",
            "!(?r = 1 || ?r = 2)",
            "!(?r > 1 && ?r < 4)",
            "!(!(?r < 3))",
            "strlen(str(?l)) > 2 && !(?r = 3)",
            "!(?r < 3) || !(?r > 9)",
        };

        /// <summary>
        /// The subset dotNetRDF is known to serialize incorrectly.
        /// </summary>
        private static readonly HashSet<string> KnownUnfaithful = new HashSet<string>
        {
            "!(?r < 3)",
            "!(?r = 1 || ?r = 2)",
            "!(?r > 1 && ?r < 4)",
            "!(!(?r < 3))",
            "strlen(str(?l)) > 2 && !(?r = 3)",
            "!(?r < 3) || !(?r > 9)",
        };

        public static IEnumerable<TestCaseData> Cases()
        {
            return Expressions.Select(e => new TestCaseData(e));
        }

        [TestCaseSource(nameof(Cases))]
        public void WriterRoundTripsWithoutChangingMeaning(string expression)
        {
            ISparqlExpression original = Parse(expression);
            string written = SparqlExpressionWriter.Write(original);

            Assert.AreEqual(
                SparqlExpressionWriter.Signature(original),
                SparqlExpressionWriter.Signature(Parse(written)),
                $"'{expression}' was re-serialized as '{written}', which does not mean the same thing");
        }

        [TestCaseSource(nameof(Cases))]
        public void DotNetRdfIsUnfaithfulExactlyWhereExpected(string expression)
        {
            ISparqlExpression original = Parse(expression);
            string theirs = original.ToString();

            string before = SparqlExpressionWriter.Signature(original);
            string after;

            try
            {
                after = SparqlExpressionWriter.Signature(Parse(theirs));
            }
            catch (Exception)
            {
                // Its output does not even re-parse; that counts as unfaithful.
                after = "(unparseable)";
            }

            bool faithful = before == after;

            if (KnownUnfaithful.Contains(expression))
            {
                Assert.IsFalse(faithful,
                    $"dotNetRDF now serializes '{expression}' faithfully. If that holds generally, " +
                    "SparqlExpressionWriter may no longer be needed - re-check before removing it.");
            }
            else
            {
                Assert.IsTrue(faithful,
                    $"dotNetRDF changed '{expression}' into '{theirs}'. Add it to KnownUnfaithful; " +
                    "SparqlExpressionWriter already handles it, so nothing is broken.");
            }
        }

        /// <summary>
        /// Every case that dotNetRDF mangles is a case this writer has to carry.
        /// </summary>
        [Test]
        public void WriterIsFaithfulForEveryExpressionIncludingTheUnfaithfulOnes()
        {
            var failures = new List<string>();

            foreach (string expression in Expressions)
            {
                ISparqlExpression original = Parse(expression);
                string written = SparqlExpressionWriter.Write(original);

                if (SparqlExpressionWriter.Signature(original) != SparqlExpressionWriter.Signature(Parse(written)))
                {
                    failures.Add($"{expression} -> {written}");
                }
            }

            Assert.IsEmpty(failures, "expressions that did not survive: " + string.Join("; ", failures));
            Assert.Greater(KnownUnfaithful.Count, 0, "the corpus must include cases dotNetRDF gets wrong");
        }

        [Test]
        public void WriteRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => SparqlExpressionWriter.Write(null));
        }

        [Test]
        public void SignatureOfNullIsStable()
        {
            Assert.AreEqual("(null)", SparqlExpressionWriter.Signature(null));
        }

        /// <summary>
        /// Parses a bare expression by wrapping it in a minimal query.
        /// </summary>
        private static ISparqlExpression Parse(string expression)
        {
            var query = new VDS.RDF.Parsing.SparqlQueryParser().ParseFromString(
                "SELECT ?s WHERE { ?s <http://example.org/r> ?r . ?s <http://example.org/l> ?l . " +
                "FILTER(" + expression + ") }");

            var pattern = query.RootGraphPattern;

            if (pattern.IsFiltered && pattern.Filter != null)
            {
                return pattern.Filter.Expression;
            }

            return pattern.TriplePatterns.OfType<IFilterPattern>()
                .Select(f => f.Filter.Expression)
                .Concat(pattern.UnplacedFilters.Select(f => f.Expression))
                .First();
        }
    }
}
