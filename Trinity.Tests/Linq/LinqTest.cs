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
// Copyright (c) Semiodesk GmbH 2017

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Semiodesk.Trinity.Test.Linq
{
    [TestFixture]
    public class LinqTest
    {
        protected IStore Store;

        protected IModel Model;

        public LinqTest() {}

        public void SetUp()
        {
            string connectionString = SetupClass.ConnectionString;

            Store = StoreFactory.CreateStore(string.Format("{0};rule=urn:semiodesk/test/ruleset", connectionString));

            Model = Store.CreateModel(new Uri("http://test.com/test"));
            Model.Clear();

            Assert.IsTrue(Model.IsEmpty);

            Person p1 = Model.CreateResource<Person>();
            p1.FirstName = "Alice";
            p1.LastName = "Cooper";
            p1.Age = 69;
            p1.Commit();

            Person p2 = Model.CreateResource<Person>();
            p2.FirstName = "Bob";
            p2.LastName = "Dylan";
            p2.Age = 76;
            p2.Commit();

            Person p3 = Model.CreateResource<Person>();
            p3.FirstName = "Eve";
            p3.LastName = "Jeffers-Cooper";
            p3.Age = 38;
            p3.Commit();

            p1.KnownPeople.Add(p2);
            p1.Commit();

            p2.KnownPeople.Add(p1);
            p2.KnownPeople.Add(p2);
            p2.Commit();

            Assert.IsFalse(Model.IsEmpty);
        }

        [Test]
        public void CanSelectResourcesWithIntegerBinaryExpression()
        {
            SetUp();

            var persons = from person in Model.AsQueryable<Person>() where person.Age == 69 select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age != 69 select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age < 50 select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age <= 69 select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age >= 69 select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age > 50 select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithStringBinaryExpression()
        {
            SetUp();

            var persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice"  select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName != "Alice" select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithSubQuery()
        {
            SetUp();

            var persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Count != 1 select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Count > 0 select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Count >= 1 select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Count < 1 select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Count <= 1 select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithEqualsMethodCall()
        {
            SetUp();

            var persons = from person in Model.AsQueryable<Person>() where person.FirstName.Equals("Alice") select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithFirstResultOperator()
        {
            SetUp();

            SparqlQuery q1 = new SparqlQuery(@"
				SELECT DISTINCT ?person ( MIN(?o0) AS ?o0_first ) WHERE
				{
                    { SELECT ?person WHERE { ?person <http://xmlns.com/foaf/0.1/knows> ?o0 . } }
                    INTERSECT
					{ SELECT ?person WHERE { ?person <http://xmlns.com/foaf/0.1/knows> ?o0 . } ORDER BY ?o0 LIMIT 1 }
				}
				GROUP BY ?person ?o0 ORDER BY ?o0
            ");

            IList<BindingSet> b1 = Model.GetBindings(q1).ToList();

            SparqlQuery q2 = new SparqlQuery(@"
		        SELECT ?person ( COUNT ( ?o1 ) AS ?o1_count ) WHERE
		        {
			        {
				        SELECT ?person ( MIN ( ?o0 ) AS ?o0_first ) WHERE
				        {
					        ?person <http://xmlns.com/foaf/0.1/knows> ?o0 . 
				        }
				        GROUP BY ?person ?o0 ORDER BY ?o0
			        }

			        ?o0_first <http://xmlns.com/foaf/0.1/knows> ?o1 .
		        }
		        GROUP BY ?person
            ");

            IList<BindingSet> b2 = Model.GetBindings(q2).ToList();

            var persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.First().KnownPeople.Count == 1 select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }
    }
}