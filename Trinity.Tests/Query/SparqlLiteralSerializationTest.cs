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
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Patterns;

namespace Semiodesk.Trinity.Tests.Query
{
    /// <summary>
    /// <see cref="SparqlSerializer.SerializeString"/> writes one literal holding exactly the value, and
    /// the preprocessor's second pass leaves it as it is (ADR-0052).
    /// </summary>
    [TestFixture]
    public class SparqlLiteralSerializationTest
    {
        /// <summary>
        /// Templates a literal is placed in on its way to a store. The second pass tokenizes each and
        /// writes every literal back out; a different template could expose a different defect.
        /// </summary>
        private static readonly string[] UpdateTemplates =
        {
            "INSERT DATA { GRAPH <urn:g> { <urn:s> <urn:p> {0} . } }",
            "INSERT DATA { GRAPH <urn:g> { <urn:s> <urn:p> {0}@en . } }",
            "INSERT DATA { GRAPH <urn:g> { <urn:s> <urn:p> {0}^^<http://www.w3.org/2001/XMLSchema#anyURI> . } }",
            "DELETE { GRAPH <urn:g> { <urn:s> <urn:p> {0}. } } INSERT { GRAPH <urn:g> { <urn:s> <urn:p> {0}. } } WHERE {}",
        };

        private static readonly string[] QueryTemplates =
        {
            "SELECT ?s WHERE { ?s ?p ?o . FILTER(?o = {0}) }",
            "SELECT ?s WHERE { VALUES ?o { {0} } ?s ?p ?o . }",
            "ASK WHERE { ?s ?p {0} }",
        };

        [TestCaseSource(typeof(HostileLiterals), nameof(HostileLiterals.Cases))]
        public void WritesOneLiteralHoldingTheValue(string value)
        {
            string literal = SparqlSerializer.SerializeString(value);

            Assert.IsTrue(SparqlLiteralOracle.IsOneLiteral(literal), SparqlLiteralOracle.Display(literal));
            Assert.AreEqual(value, SparqlLiteralOracle.Decode(literal));
        }

        [Test]
        public void WritesOneLiteralHoldingTheValueForFuzzedValues()
        {
            var failures = HostileLiterals.Fuzz(5000)
                .Where(v =>
                {
                    string literal = SparqlSerializer.SerializeString(v);

                    return !SparqlLiteralOracle.IsOneLiteral(literal) || SparqlLiteralOracle.Decode(literal) != v;
                })
                .Take(10)
                .Select(SparqlLiteralOracle.Display)
                .ToList();

            Assert.IsEmpty(failures, "seed " + HostileLiterals.Seed);
        }

        /// <summary>
        /// dotNetRDF reads the value the oracle reads. Agreement only, not evidence: dotNetRDF is also
        /// the tokenizer of the second pass, which is exactly why the oracle is the primary check.
        /// </summary>
        [TestCaseSource(typeof(HostileLiterals), nameof(HostileLiterals.Cases))]
        public void DotNetRdfReadsTheSameValue(string value)
        {
            var query = new SparqlQueryParser().ParseFromString(
                "SELECT * WHERE { <urn:s> <urn:p> " + SparqlSerializer.SerializeString(value) + " }");

            var patterns = query.RootGraphPattern.TriplePatterns.OfType<TriplePattern>().ToList();

            Assert.AreEqual(1, patterns.Count);
            Assert.AreEqual(value, ((ILiteralNode)((NodeMatchPattern)patterns[0].Object).Node).Value);
        }

        /// <summary>
        /// The preprocessor writes each literal back out unchanged, and doing it again changes nothing -
        /// which matters because a query is re-wrapped and preprocessed again for counting and paging.
        /// </summary>
        [TestCaseSource(typeof(HostileLiterals), nameof(HostileLiterals.Cases))]
        public void ThePreprocessorReproducesTheLiteral(string value)
        {
            AssertFixedPoint(value);
        }

        [Test]
        public void ThePreprocessorReproducesTheLiteralForFuzzedValues()
        {
            var failures = new List<string>();

            foreach (string value in HostileLiterals.Fuzz(2000))
            {
                try
                {
                    AssertFixedPoint(value);
                }
                catch (AssertionException)
                {
                    failures.Add(SparqlLiteralOracle.Display(value));
                }

                if (failures.Count == 10) break;
            }

            Assert.IsEmpty(failures, "seed " + HostileLiterals.Seed);
        }

        [TestCaseSource(typeof(HostileLiterals), nameof(HostileLiterals.Cases))]
        public void ABoundValueIsWrittenAsOneLiteral(string value)
        {
            string text = new SparqlQuery("SELECT ?s WHERE { ?s <urn:p> @value . }")
                .Bind("@value", value)
                .ToString();

            StringAssert.Contains(" " + SparqlSerializer.SerializeString(value) + " ", text);

            var query = new SparqlQueryParser().ParseFromString(text);
            var pattern = query.RootGraphPattern.TriplePatterns.OfType<TriplePattern>().Single();

            Assert.AreEqual(value, ((ILiteralNode)((NodeMatchPattern)pattern.Object).Node).Value);
        }

        /// <summary>
        /// A typed literal's lexical form is escaped like any other literal, and holds the value without
        /// quotes of its own.
        /// </summary>
        [TestCaseSource(typeof(HostileLiterals), nameof(HostileLiterals.Cases))]
        public void ATypedLiteralHoldsTheValueAsItsLexicalForm(string value)
        {
            var datatype = new Uri("http://www.w3.org/2001/XMLSchema#anyURI");

            string text = SparqlSerializer.SerializeTypedLiteral(value, datatype);
            string literal = SparqlLiteralOracle.SplitSuffix(text, out string suffix);

            Assert.IsTrue(SparqlLiteralOracle.IsOneLiteral(literal), SparqlLiteralOracle.Display(text));
            Assert.AreEqual(value, SparqlLiteralOracle.Decode(literal));
            Assert.AreEqual("^^<http://www.w3.org/2001/XMLSchema#anyURI>", suffix);
        }

        /// <summary>
        /// The reported case: a string passed to <see cref="SparqlSerializer.SerializeTypedLiteral"/> used
        /// to come out as <c>'"abc"'^^…</c>, a lexical form with the quotes in it.
        /// </summary>
        [Test]
        public void ATypedLiteralFromAStringHasNoQuotesInItsLexicalForm()
        {
            Assert.AreEqual("\"abc\"^^<http://www.w3.org/2001/XMLSchema#anyURI>",
                SparqlSerializer.SerializeTypedLiteral("abc", new Uri("http://www.w3.org/2001/XMLSchema#anyURI")));
        }

        [Test]
        public void ADateTimeIsWrittenAsATypedLiteral()
        {
            var date = new DateTime(2026, 9, 21, 10, 15, 0, DateTimeKind.Utc);

            Assert.AreEqual("\"2026-09-21T10:15:00Z\"^^<http://www.w3.org/2001/XMLSchema#dateTime>",
                SparqlSerializer.SerializeDateTime(date));
            Assert.AreEqual(SparqlSerializer.SerializeValue(date), SparqlSerializer.SerializeDateTime(date));
        }

        [Test]
        public void RefusesNull()
        {
            Assert.Throws<ArgumentNullException>(() => SparqlSerializer.SerializeString(null));
        }

        private static void AssertFixedPoint(string value)
        {
            string literal = SparqlSerializer.SerializeString(value);

            foreach (string template in UpdateTemplates)
            {
                string first = new SparqlUpdate(template.Replace("{0}", literal)).ToString();
                string second = new SparqlUpdate(first).ToString();

                StringAssert.Contains(literal, first, SparqlLiteralOracle.Display(first));
                Assert.AreEqual(first, second, SparqlLiteralOracle.Display(value));
            }

            foreach (string template in QueryTemplates)
            {
                string first = new SparqlQuery(template.Replace("{0}", literal)).ToString();
                string second = new SparqlQuery(first).ToString();

                StringAssert.Contains(literal, first, SparqlLiteralOracle.Display(first));
                Assert.AreEqual(first, second, SparqlLiteralOracle.Display(value));
            }
        }
    }
}
