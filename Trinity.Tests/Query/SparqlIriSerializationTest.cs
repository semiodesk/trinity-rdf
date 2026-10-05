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
using System.IO;
using NUnit.Framework;
using VDS.RDF.Parsing;

namespace Semiodesk.Trinity.Tests.Query
{
    /// <summary>
    /// The preprocessor writes every IRI through the guard: the graphs it adds, the graphs bound to its
    /// parameters, and the IRI tokens it writes back after dotNetRDF has decoded them (ADR-0052).
    /// </summary>
    [TestFixture]
    public class SparqlIriSerializationTest
    {
        /// <summary>
        /// IRIs that cannot be written verbatim. The first completes a dataset clause into a pattern of its
        /// own; none of them can be written between angle brackets.
        /// </summary>
        private static readonly string[] Unwritable =
        {
            "http://example.org/g> FROM <http://example.org/other",
            "http://example.org/a b",
            "http://example.org/a\"b",
            "http://example.org/a{b",
        };

        /// <summary>
        /// dotNetRDF decodes <c>\u</c> escapes inside an IRI token, so <c>&lt;a\u003Eb&gt;</c> reaches the
        /// preprocessor as a raw <c>&gt;</c>. Writing it back as it was decoded ended the IRI early.
        /// </summary>
        [TestCase("SELECT * WHERE { <http://example.org/a\\u003Eb> ?p ?o }")]
        [TestCase("SELECT * WHERE { <http://example.org/a\\u0020b> ?p ?o }")]
        [TestCase("SELECT * WHERE { ?s ?p \"x\"^^<http://example.org/a\\u003Eb> }")]
        public void AnIriTokenThatDecodesToAForbiddenCharacterIsRefused(string text)
        {
            Assert.Throws<NotSupportedException>(() => new SparqlQuery(text).ToString());
        }

        [Test]
        public void AnIriTokenInAnUpdateThatDecodesToAForbiddenCharacterIsRefused()
        {
            Assert.Throws<NotSupportedException>(() =>
                new SparqlUpdate("INSERT DATA { GRAPH <urn:g> { <urn:s> <urn:p> <urn:o\\u003E> } }").ToString());
        }

        /// <summary>
        /// A decoded character that an IRI may hold is written back decoded.
        /// </summary>
        [Test]
        public void AnIriTokenThatDecodesToAnAllowedCharacterIsWritten()
        {
            StringAssert.Contains("<http://example.org/\u00E9>",
                new SparqlQuery("SELECT * WHERE { <http://example.org/\\u00E9> ?p ?o }").ToString());
        }

        /// <summary>
        /// Assigning a model adds its graph as a dataset clause, which every model read does.
        /// </summary>
        [TestCaseSource(nameof(Unwritable))]
        public void AModelWhoseGraphCannotBeWrittenIsRefused(string graph)
        {
            var model = StoreFactory.CreateStore("provider=dotnetrdf").GetModel(new UriRef(graph, UriKind.RelativeOrAbsolute));

            var e = Assert.Throws<NotSupportedException>(() =>
                new SparqlQuery("SELECT * WHERE { ?s ?p ?o }") { Model = model }.ToString());

            StringAssert.Contains(graph, e.Message);
        }

        [TestCaseSource(nameof(Unwritable))]
        public void AGraphParameterThatCannotBeWrittenIsRefused(string graph)
        {
            Assert.Throws<NotSupportedException>(() =>
                new SparqlQuery("SELECT * FROM @graph WHERE { ?s ?p ?o }").Bind("@graph", new UriRef(graph, UriKind.RelativeOrAbsolute)));
        }

        /// <summary>
        /// A graph parameter names a graph, so a blank node and a string are refused rather than written
        /// where the grammar requires an IRI.
        /// </summary>
        [Test]
        public void AGraphParameterTakesOnlyAGraphIdentifier()
        {
            Assert.Throws<NotSupportedException>(() =>
                new SparqlQuery("SELECT * FROM @graph WHERE { ?s ?p ?o }").Bind("@graph", new UriRef("_:b0", true)));
            Assert.Throws<ArgumentException>(() =>
                new SparqlQuery("SELECT * FROM @graph WHERE { ?s ?p ?o }").Bind("@graph", "http://example.org/g"));
        }

        /// <summary>
        /// Re-binding a graph parameter forgets the graph it was bound to before. It used to remove the
        /// bracketed form from a set that held the bare IRI, so binding the first graph again was refused
        /// as already set.
        /// </summary>
        [Test]
        public void AGraphParameterCanBeBoundAgain()
        {
            var a = new Uri("http://example.org/a");
            var b = new Uri("http://example.org/b");

            var query = new SparqlQuery("SELECT * FROM @graph WHERE { ?s ?p ?o }", declarePrefixes: false)
                .Bind("@graph", a)
                .Bind("@graph", b)
                .Bind("@graph", a);

            Assert.AreEqual("SELECT * FROM <http://example.org/a> WHERE { ?s ?p ?o }", query.ToString());
        }

        /// <summary>
        /// A parameter after <c>FROM NAMED</c> is a graph parameter too. The tokeniser splits the keyword
        /// in two, so the token before the parameter is <c>NAMED</c>, and the parameter was bound as a
        /// plain value: a string as a literal, a blank node as a bare label.
        /// </summary>
        [Test]
        public void AFromNamedParameterTakesOnlyAGraphIdentifier()
        {
            const string text = "SELECT * FROM NAMED @graph WHERE { GRAPH ?g { ?s ?p ?o } }";

            Assert.Throws<NotSupportedException>(() => new SparqlQuery(text).Bind("@graph", new UriRef("_:b0", true)));
            Assert.Throws<ArgumentException>(() => new SparqlQuery(text).Bind("@graph", "http://example.org/g"));

            foreach (string graph in Unwritable)
            {
                Assert.Throws<NotSupportedException>(() =>
                    new SparqlQuery(text).Bind("@graph", new UriRef(graph, UriKind.RelativeOrAbsolute)), graph);
            }
        }

        /// <summary>
        /// A graph bound after <c>FROM NAMED</c> is recorded as a named graph, so adding the same named
        /// graph again is not a second clause - which Jena refuses (ADR-0043) - and adding it as the
        /// default graph is not suppressed.
        /// </summary>
        [Test]
        public void AFromNamedParameterIsRecordedAsANamedGraph()
        {
            var g = new Uri("http://example.org/g");

            var query = Preprocess("SELECT * FROM NAMED @graph WHERE { GRAPH ?g { ?s ?p ?o } }");

            query.Bind("@graph", g);
            query.AddNamedGraph(g);
            query.AddDefaultGraph(g);

            Assert.AreEqual(
                "SELECT * FROM NAMED <http://example.org/g> FROM <http://example.org/g> WHERE { GRAPH ?g { ?s ?p ?o } }",
                query.ToString());
        }

        /// <summary>
        /// Binding a graph parameter to a graph the dataset already has is refused, and the refusal leaves
        /// the graph it was bound to before recorded. Forgetting it first left that graph still written but
        /// no longer recorded, so adding it again wrote a second, identical clause.
        /// </summary>
        [Test]
        public void ARefusedRebindingKeepsThePreviousGraph()
        {
            var a = new Uri("http://example.org/a");
            var b = new Uri("http://example.org/b");

            var query = Preprocess("SELECT * FROM @graph WHERE { ?s ?p ?o }");

            query.Bind("@graph", a);
            query.AddDefaultGraph(b);

            var e = Assert.Throws<ArgumentException>(() => query.Bind("@graph", b));
            // The IRI is in the message itself: it used to be passed as paramName, leaving a literal {0}.
            StringAssert.Contains("http://example.org/b", e.Message);
            StringAssert.DoesNotContain("{0}", e.Message);
            Assert.AreEqual("value", e.ParamName);

            query.AddDefaultGraph(a);

            Assert.AreEqual(
                "SELECT * FROM <http://example.org/a> FROM <http://example.org/b> WHERE { ?s ?p ?o }",
                query.ToString());
        }

        /// <summary>
        /// The preprocessor itself, for adding graphs the way assigning a model does.
        /// </summary>
        private static SparqlQueryPreprocessor Preprocess(string text)
        {
            var preprocessor = new SparqlQueryPreprocessor(new StringReader(text), SparqlQuerySyntax.Extended);
            preprocessor.Process(false);

            return preprocessor;
        }
    }
}
