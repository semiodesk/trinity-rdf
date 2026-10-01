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
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Semiodesk.Trinity.Query.Sparql;
using Semiodesk.Trinity.Tests.Linq;

namespace Semiodesk.Trinity.Tests.Query.Sparql
{
    /// <summary>
    /// Pins the SPARQL a LINQ equality lookup on a mapped string becomes (#64).
    /// </summary>
    /// <remarks>
    /// The answers are the same with and without the lookup, so no result-based test can tell them apart;
    /// what differs is whether an engine can answer it from an index (ADR-0051 has the figures). The
    /// answers it must keep, on every store and for every spelling Virtuoso stores, are pinned by
    /// <c>ResourceMappingTest.QueriesAMappedStringByValueAcrossStores</c>.
    /// </remarks>
    [TestFixture]
    public class LinqEqualityLookupTest
    {
        private IModel _model;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OntologyDiscovery.AddAssembly(typeof(Person).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Person).Assembly);
            OntologyDiscovery.AddAssembly(typeof(Resource).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Resource).Assembly);

            _model = StoreFactory.CreateStore("provider=dotnetrdf").GetModel(new Uri("http://example.org/lookup"));
        }

        private static string Sparql(IQueryable queryable)
        {
            return Sparql(queryable.Expression);
        }

        private static string Sparql(Expression expression)
        {
            return SparqlQueryWriter.Write(new SparqlQueryTranslator().Translate(expression).Query);
        }

        /// <summary>
        /// The position of the lookup for <paramref name="value"/> in the query, or -1: a sub-select of the
        /// distinct subjects holding it in either spelling.
        /// </summary>
        private static int Lookup(string sparql, string value)
        {
            string pattern = $@"\{{ SELECT DISTINCT \?s WHERE \{{ \{{ \?s <([^>]+)> ""{value}"" \. \}} UNION "
                + $@"\{{ \?s <\1> ""{value}""\^\^<http://www\.w3\.org/2001/XMLSchema\#string> \. \}} \}} \}}";

            var match = Regex.Match(sparql, pattern);

            return match.Success ? match.Index : -1;
        }

        [Test]
        public void AnEqualityLookupNamesTheConstantAheadOfTheTypeConstraint()
        {
            string sparql = Sparql(_model.AsQueryable<Person>().Where(p => p.FirstName == "Alice"));

            int lookup = Lookup(sparql, "Alice");

            Assert.GreaterOrEqual(lookup, 0, "no lookup for the constant:\n" + sparql);
            Assert.Less(lookup, sparql.IndexOf(" a <", StringComparison.Ordinal), "the lookup must lead the type constraint:\n" + sparql);

            // The binding and its filters stay: the lookup narrows them, it does not replace them.
            StringAssert.Contains("STR(?v0)", sparql);
            StringAssert.Contains("LANG(?v0)", sparql);
        }

        [Test]
        public void EveryRequiredConjunctIsNamed()
        {
            string sparql = Sparql(_model.AsQueryable<Person>()
                .Where(p => p.FirstName == "Alice" && "Liddell" == p.LastName));

            Assert.GreaterOrEqual(Lookup(sparql, "Alice"), 0, sparql);
            Assert.GreaterOrEqual(Lookup(sparql, "Liddell"), 0, sparql);
        }

        [Test]
        public void ALookupInsideAPagedOrCountedQueryIsNamedToo()
        {
            string paged = Sparql(_model.AsQueryable<Person>().Where(p => p.FirstName == "Alice").Take(1));

            Assert.GreaterOrEqual(Lookup(paged, "Alice"), 0, paged);

            // Count(predicate) executes, so its expression is built by hand.
            Expression<Func<Person, bool>> predicate = p => p.FirstName == "Alice";

            string count = Sparql(Expression.Call(typeof(Queryable), nameof(Queryable.Count), new[] { typeof(Person) },
                _model.AsQueryable<Person>().Expression, predicate));

            Assert.GreaterOrEqual(Lookup(count, "Alice"), 0, count);
        }

        [Test]
        public void ADisjunctOrANegationIsNotNamed()
        {
            // Neither is required of every row, so naming it would drop the rows the other branch matches.
            string or = Sparql(_model.AsQueryable<Person>().Where(p => p.FirstName == "Alice" || p.FirstName == "Bob"));
            string not = Sparql(_model.AsQueryable<Person>().Where(p => !(p.FirstName == "Alice")));

            Assert.AreEqual(-1, Lookup(or, "Alice"), or);
            Assert.AreEqual(-1, Lookup(not, "Alice"), not);
        }

        [Test]
        public void AMemberOfAMemberIsNotNamed()
        {
            // The lookup projects ?s, and the group's name is not attached to ?s.
            string sparql = Sparql(_model.AsQueryable<Person>().Where(p => p.Group.Name == "The Spiders"));

            Assert.AreEqual(-1, Lookup(sparql, "The Spiders"), sparql);
        }

        [Test]
        public void ALookupHoldsWhenTheBindingBecameOptional()
        {
            // The disjunct turns the shared binding into an OPTIONAL. The lookup does not touch its variable,
            // and the conjunct is still required of every row, so the lookup still applies.
            string sparql = Sparql(_model.AsQueryable<Person>()
                .Where(p => p.FirstName == "Alice" && (p.FirstName == "Alice" || p.LastName == "Liddell")));

            StringAssert.Contains("OPTIONAL", sparql);
            Assert.GreaterOrEqual(Lookup(sparql, "Alice"), 0, sparql);
        }
    }
}
