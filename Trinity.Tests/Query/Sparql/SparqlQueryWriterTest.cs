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
// Copyright (c) Semiodesk GmbH 2015-2020

using System;
using NUnit.Framework;
using Semiodesk.Trinity.Query.Sparql;
using VDS.RDF.Parsing;

namespace Semiodesk.Trinity.Tests.Query.Sparql
{
    /// <summary>
    /// White-box tests for the owned SPARQL AST serializer (<see cref="SparqlQueryWriter"/>).
    /// Each test asserts the emitted text is accepted by dotNetRDF's own SPARQL parser (a validity
    /// oracle), and pins the exact string for the simple shapes.
    /// </summary>
    [TestFixture]
    public class SparqlQueryWriterTest
    {
        private static readonly Uri Person = new Uri("http://xmlns.com/foaf/0.1/Person");
        private static readonly Uri FirstName = new Uri("http://xmlns.com/foaf/0.1/firstName");
        private static readonly Uri Age = new Uri("http://xmlns.com/foaf/0.1/age");
        private static readonly Uri XsdInteger = new Uri("http://www.w3.org/2001/XMLSchema#integer");

        private static void AssertValidSparql(string query)
        {
            var parser = new SparqlQueryParser();
            Assert.DoesNotThrow(() => parser.ParseFromString(query), "Emitted SPARQL was rejected by the parser:\n" + query);
        }

        private static SelectQuery ResourceQuery()
        {
            // SELECT ?s ?p ?o WHERE { ?s ?p ?o . ?s a <Person> . } — the shape GetResources<T> expects.
            var s = new VariableTerm("s");
            var query = new SelectQuery();
            query.Projections.Add(new Projection(s));
            query.Projections.Add(new Projection(new VariableTerm("p")));
            query.Projections.Add(new Projection(new VariableTerm("o")));
            query.Where.Add(new TriplePattern(s, new VariableTerm("p"), new VariableTerm("o")));
            query.Where.Add(new TriplePattern(s, RdfTypeTerm.Instance, new IriTerm(Person)));
            return query;
        }

        [Test]
        public void WritesCanonicalResourceQuery()
        {
            var text = SparqlQueryWriter.Write(ResourceQuery());

            Assert.AreEqual(
                "SELECT ?s ?p ?o WHERE { ?s ?p ?o . ?s a <http://xmlns.com/foaf/0.1/Person> . }",
                text);

            AssertValidSparql(text);
        }

        [Test]
        public void WritesFilterWithTypedLiteral()
        {
            var query = ResourceQuery();
            var v = new VariableTerm("v0");
            query.Where.Add(new TriplePattern(new VariableTerm("s"), new IriTerm(Age), v));
            query.Where.AddFilter(new SparqlBinaryExpression(
                SparqlBinaryOperator.GreaterThanOrEqual,
                new SparqlVariableExpression("v0"),
                new SparqlConstantExpression(new LiteralTerm("38", XsdInteger))));

            var text = SparqlQueryWriter.Write(query);

            StringAssert.Contains("?s <http://xmlns.com/foaf/0.1/age> ?v0 .", text);
            StringAssert.Contains("FILTER((?v0 >= \"38\"^^<http://www.w3.org/2001/XMLSchema#integer>))", text);
            AssertValidSparql(text);
        }

        [Test]
        public void WritesStringFunctionFilter()
        {
            var query = ResourceQuery();
            query.Where.Add(new TriplePattern(new VariableTerm("s"), new IriTerm(FirstName), new VariableTerm("v0")));
            query.Where.AddFilter(new SparqlFunctionExpression(
                "CONTAINS",
                new SparqlVariableExpression("v0"),
                new SparqlConstantExpression(new LiteralTerm("Ali"))));

            var text = SparqlQueryWriter.Write(query);

            StringAssert.Contains("FILTER(CONTAINS(?v0, \"Ali\"))", text);
            AssertValidSparql(text);
        }

        [Test]
        public void WritesAskQuery()
        {
            var query = new AskQuery();
            query.Where.Add(new TriplePattern(new VariableTerm("s"), RdfTypeTerm.Instance, new IriTerm(Person)));

            var text = SparqlQueryWriter.Write(query);

            Assert.AreEqual("ASK { ?s a <http://xmlns.com/foaf/0.1/Person> . }", text);
            AssertValidSparql(text);
        }

        [Test]
        public void WritesPagingWithInnerSubSelect()
        {
            // Per-resource paging: outer SELECT ?s ?p ?o over an inner sub-SELECT that pages ?s.
            var s = new VariableTerm("s");

            var inner = new SelectQuery { Limit = 2, Offset = 1 };
            inner.Projections.Add(new Projection(s));
            inner.Where.Add(new TriplePattern(s, RdfTypeTerm.Instance, new IriTerm(Person)));
            inner.OrderBy.Add(new OrderCondition(new SparqlVariableExpression("s")));

            var outer = new SelectQuery();
            outer.Projections.Add(new Projection(s));
            outer.Projections.Add(new Projection(new VariableTerm("p")));
            outer.Projections.Add(new Projection(new VariableTerm("o")));
            outer.Where.Add(new TriplePattern(s, new VariableTerm("p"), new VariableTerm("o")));
            outer.Where.Add(new SubSelectPattern(inner));

            var text = SparqlQueryWriter.Write(outer);

            StringAssert.Contains("{ SELECT ?s WHERE", text);
            StringAssert.Contains("ORDER BY ?s LIMIT 2 OFFSET 1", text);
            AssertValidSparql(text);
        }

        [Test]
        public void WritesDistinctUnionAndOptional()
        {
            var s = new VariableTerm("s");
            var query = new SelectQuery { IsDistinct = true };
            query.Projections.Add(new Projection(s));

            var left = new GroupGraphPattern();
            left.Add(new TriplePattern(s, RdfTypeTerm.Instance, new IriTerm(Person)));
            var right = new GroupGraphPattern();
            right.Add(new TriplePattern(s, RdfTypeTerm.Instance, new IriTerm(new Uri("http://xmlns.com/foaf/0.1/Group"))));
            query.Where.Add(new UnionPattern(left, right));

            var optional = new GroupGraphPattern();
            optional.Add(new TriplePattern(s, new IriTerm(FirstName), new VariableTerm("v0")));
            query.Where.Add(new OptionalPattern(optional));

            var text = SparqlQueryWriter.Write(query);

            StringAssert.StartsWith("SELECT DISTINCT ?s WHERE", text);
            StringAssert.Contains("UNION", text);
            StringAssert.Contains("OPTIONAL {", text);
            AssertValidSparql(text);
        }

        /// <summary>
        /// IRIs are written from OriginalString, matching the write path (SparqlSerializer.SerializeUri).
        /// AbsoluteUri, which this used to use, normalizes percent-encoding case, dot-segments, default
        /// ports and host casing -- so a resource stored under its original spelling could not be found
        /// by a query built from the very same Uri.
        /// </summary>
        [Test]
        public void WritesIrisExactlyAsGiven()
        {
            var s = new VariableTerm("s");
            var query = new SelectQuery();
            query.Projections.Add(new Projection(s));
            query.Where.Add(new TriplePattern(s, RdfTypeTerm.Instance,
                new IriTerm(new UriRef("http://example.org/a%2Fb/../c"))));

            var text = SparqlQueryWriter.Write(query);

            StringAssert.Contains("<http://example.org/a%2Fb/../c>", text);
        }

        /// <summary>
        /// A blank node identifier is a relative URI, and reading AbsoluteUri on one throws. Naming a
        /// blank node in a query used to crash for that reason; SPARQL wants the bare label anyway.
        /// </summary>
        [Test]
        public void WritesBlankNodeIdentifiersWithoutThrowing()
        {
            var query = new SelectQuery();
            query.Projections.Add(new Projection(new VariableTerm("p")));
            query.Where.Add(new TriplePattern(
                new IriTerm(new UriRef("_:b0", true)), new VariableTerm("p"), new VariableTerm("o")));

            string text = null;

            Assert.DoesNotThrow(() => text = SparqlQueryWriter.Write(query));
            StringAssert.Contains("_:b0", text);
            Assert.IsFalse(text.Contains("<_:b0>"), "A blank node label must not be wrapped in angle brackets.");
            AssertValidSparql(text);
        }
    }
}
