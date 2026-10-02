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
using NUnit.Framework;
using Semiodesk.Trinity.Query.Sparql;

namespace Semiodesk.Trinity.Tests.Query
{
    /// <summary>
    /// The oracle itself, and the two literal writers that already escape correctly, measured with it.
    /// </summary>
    [TestFixture]
    public class SparqlLiteralOracleTest
    {
        private static readonly Uri FirstName = new Uri("http://xmlns.com/foaf/0.1/firstName");

        [TestCase("\"\"", "")]
        [TestCase("\"plain\"", "plain")]
        [TestCase("\"a\\\"b\"", "a\"b")]
        [TestCase("\"a\\\\b\"", "a\\b")]
        [TestCase("\"a\\nb\\rc\\td\"", "a\nb\rc\td")]
        [TestCase("\"a\\'b\"", "a'b")]
        [TestCase("\"it's\"", "it's")]
        [TestCase("\"\\\\u0022\"", "\\u0022")]
        public void DecodesALiteral(string literal, string value)
        {
            Assert.IsTrue(SparqlLiteralOracle.IsOneLiteral(literal));
            Assert.AreEqual(value, SparqlLiteralOracle.Decode(literal));
        }

        /// <summary>
        /// Each of these is either not a literal, more than one token, or a literal that ends somewhere
        /// other than the end - which is the shape every escaping defect produces.
        /// </summary>
        [TestCase("'single quoted'")]
        [TestCase("'''long form'''")]
        [TestCase("\"ends \" early\"")]
        [TestCase("\"raw\nline feed\"")]
        [TestCase("\"raw\rcarriage return\"")]
        [TestCase("\"escaped closing quote\\\"")]
        [TestCase("\"unknown \\q escape\"")]
        [TestCase("\"\\u0022\"")]
        [TestCase("\"tagged\"@en")]
        [TestCase("\"x\" . \"y\"")]
        [TestCase("\"unterminated")]
        [TestCase("")]
        public void RejectsWhatIsNotExactlyOneLiteral(string text)
        {
            Assert.IsFalse(SparqlLiteralOracle.IsOneLiteral(text), SparqlLiteralOracle.Display(text));
            Assert.Throws<ArgumentException>(() => SparqlLiteralOracle.Decode(text));
        }

        [Test]
        public void SplitsALiteralFromItsSuffix()
        {
            Assert.AreEqual("\"a\\\"b\"", SparqlLiteralOracle.SplitSuffix("\"a\\\"b\"@de", out string tag));
            Assert.AreEqual("@de", tag);

            Assert.AreEqual("\"x\"", SparqlLiteralOracle.SplitSuffix("\"x\"^^<urn:dt> .", out string rest));
            Assert.AreEqual("^^<urn:dt> .", rest);
        }

        [TestCaseSource(typeof(HostileLiterals), nameof(HostileLiterals.Cases))]
        public void TheLinqWriterWritesOneLiteral(string value)
        {
            var s = new VariableTerm("s");
            var query = new SelectQuery();
            query.Projections.Add(new Projection(s));
            query.Where.Add(new TriplePattern(s, new IriTerm(FirstName), new LiteralTerm(value)));

            string text = SparqlQueryWriter.Write(query);
            string predicate = "<" + FirstName.OriginalString + "> ";
            string literal = SparqlLiteralOracle.SplitSuffix(text.Substring(text.IndexOf(predicate) + predicate.Length), out string rest);

            Assert.IsTrue(SparqlLiteralOracle.IsOneLiteral(literal), SparqlLiteralOracle.Display(text));
            Assert.AreEqual(value, SparqlLiteralOracle.Decode(literal));
            StringAssert.StartsWith(" .", rest);
        }

        [TestCaseSource(typeof(HostileLiterals), nameof(HostileLiterals.Cases))]
        public void LangStringWritesOneNTriplesLiteral(string value)
        {
            string text = new LangString(value, "de").ToNTriples();
            string literal = SparqlLiteralOracle.SplitSuffix(text, out string tag);

            Assert.IsTrue(SparqlLiteralOracle.IsOneLiteral(literal), SparqlLiteralOracle.Display(text));
            Assert.AreEqual(value, SparqlLiteralOracle.Decode(literal));
            Assert.AreEqual("@de", tag);
        }
    }
}
