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

        [SetUp]
        public void SetUp()
        {
            // DotNetRdf memory store.
            string connectionString = "provider=dotnetrdf";

            // OpenLink Virtoso store.
            //string connectionString = string.Format("{0};rule=urn:semiodesk/test/ruleset", SetupClass.ConnectionString);

            Store = StoreFactory.CreateStore(connectionString);

            Model = Store.CreateModel(new Uri("http://test.com/test"));
            Model.Clear();

            Assert.IsTrue(Model.IsEmpty);

            Group g1 = Model.CreateResource<Group>();
            g1.Name = "The Spiders";
            g1.Commit();

            Group g2 = Model.CreateResource<Group>();
            g2.Name = "Alicia Keys";
            g2.Commit();

            Person p1 = Model.CreateResource<Person>();
            p1.FirstName = "Alice";
            p1.LastName = "Cooper";
            p1.Age = 69;
            p1.Birthday = new DateTime(1948, 2, 4);
            p1.Group = g1;
            p1.Status = true;
            p1.AccountBalance = 10000000.1f;
            p1.Commit();

            Person p2 = Model.CreateResource<Person>();
            p2.FirstName = "Bob";
            p2.LastName = "Dylan";
            p2.Age = 76;
            p2.Birthday = new DateTime(1941, 5, 24);
            p2.AccountBalance = 1000000.1f;
            p2.Commit();

            Person p3 = Model.CreateResource<Person>();
            p3.FirstName = "Eve";
            p3.LastName = "Jeffers-Cooper";
            p3.Birthday = new DateTime(1978, 11, 10);
            p3.Age = 38;
            p3.Group = g2;
            p3.AccountBalance = 100000.0f;
            p3.Commit();

            p1.KnownPeople.Add(p2);
            p1.Commit();

            p2.KnownPeople.Add(p1);
            p2.KnownPeople.Add(p2);
            p2.Commit();

            Assert.IsFalse(Model.IsEmpty);
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnBoolean()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Status == true select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Status == false select person;

            Assert.IsFalse(persons.Any());
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnDateTime()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Birthday.Equals(new DateTime(1948, 2, 4)) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Birthday == new DateTime(1948, 2, 4) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Birthday == new DateTime(1950, 1, 1) select person;

            Assert.IsFalse(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Birthday != new DateTime(1948, 2, 4) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Birthday < new DateTime(1948, 2, 4) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Birthday <= new DateTime(1948, 2, 4) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Birthday >= new DateTime(1948, 2, 4) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Birthday > new DateTime(1948, 2, 4) select person;

            Assert.IsTrue(persons.Any());
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnFloat()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.AccountBalance.Equals(100000) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance == 100000 select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance != 100000 select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance < 100000 select person;

            Assert.IsFalse(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance <= 100000 select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance >= 100000 select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance > 100000 select person;

            Assert.IsTrue(persons.Any());
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnString()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.LastName == "Alice" select person;

            Assert.IsFalse(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.FirstName != "Alice" select person;

            Assert.IsTrue(persons.Any());
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnResource()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Group.Name.Equals("The Spiders") select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Group.Name == "The Spiders" select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Group.Name == "The Bugs" select person;

            Assert.IsFalse(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Group.Name != "The Spiders" select person;

            Assert.IsTrue(persons.Any());
        }

        [Test]
        public void CanSelectBooleanPropertyWithBinaryExpression()
        {
            var states = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Status;

            Assert.AreEqual(1, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where person.Status == true select person.Status;

            Assert.AreEqual(1, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where person.Status == false select person.Status;

            Assert.AreEqual(0, states.ToList().Count);
        }

        [Test]
        public void CanSelectIntegerPropertyWithBinaryExpression()
        {
            var ages = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Age;

            Assert.AreEqual(1, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status == true select person.Age;

            Assert.AreEqual(1, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status == false select person.Age;

            Assert.AreEqual(0, ages.ToList().Count);
        }

        [Test]
        public void CanSelectDateTimePropertyWithBinaryExpression()
        {
            var birthdays = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Birthday;

            Assert.AreEqual(1, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where person.Status == true select person.Birthday;

            Assert.AreEqual(1, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where person.Status == false select person.Birthday;

            Assert.AreEqual(0, birthdays.ToList().Count);
        }

        [Test]
        public void CanSelectStringPropertyWithBinaryExpression()
        {
            var names = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status == true select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status == false select person.FirstName;

            Assert.AreEqual(0, names.ToList().Count);
        }

        [Test]
        public void CanSelectResourcePropertyWithBinaryExpression()
        {
            var groups = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Group;

            Assert.AreEqual(1, groups.ToList().Count);

            groups = from person in Model.AsQueryable<Person>() where person.Status == true select person.Group;

            Assert.AreEqual(1, groups.ToList().Count);

            groups = from person in Model.AsQueryable<Person>() where person.Status == false select person.Group;

            Assert.AreEqual(0, groups.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnBoolean()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status == true select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status == false select person;

            Assert.AreEqual(0, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnDateTime()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Birthday.Equals(new DateTime(1948, 2, 4)) select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Birthday == new DateTime(1948, 2, 4) select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Birthday != new DateTime(1948, 2, 4) select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Birthday < new DateTime(1948, 2, 4) select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Birthday <= new DateTime(1948, 2, 4) select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Birthday >= new DateTime(1948, 2, 4) select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Birthday > new DateTime(1948, 2, 4) select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnInteger()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Age.Equals(69) select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age == 69 select person;

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
        public void CanSelectResourcesWithBinaryExpressionOnFloat()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.AccountBalance.Equals(100000) select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance == 100000 select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance != 100000 select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance < 100000 select person;

            Assert.AreEqual(0, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance <= 100000 select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance >= 100000 select person;

            Assert.AreEqual(3, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.AccountBalance > 100000 select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnString()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice"  select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName != "Alice" select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnResource()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Group.Name.Equals("The Spiders") select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Group.Name == "The Spiders" select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Group.Name != "The Spiders" select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithCountOperator()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Count != 1 select person;

            Assert.AreEqual(2, persons.ToList().Count);

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
            var persons = from person in Model.AsQueryable<Person>() where person.FirstName.Equals("Alice") select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithFirstResultOperator()
        {
            Assert.Fail("TODO");

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

        [Test]
        public void CanSelectResourcesWithLastResultOperator()
        {
            Assert.Fail("TODO");

            var persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Last().KnownPeople.Count == 1 select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        private void DumpModel()
        {
            Debug.WriteLine("");

            ISparqlQuery q = new SparqlQuery(@"SELECT * WHERE { ?s ?p ?o . }");

            foreach(BindingSet b in Model.GetBindings(q))
            {
                Debug.WriteLine(b["s"] + " " + b["p"] + " " + b["o"]);
            }

            Debug.WriteLine("");
        }
    }
}