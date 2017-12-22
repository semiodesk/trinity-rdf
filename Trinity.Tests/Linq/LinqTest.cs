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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
            //string connectionString = "provider=dotnetrdf";

            // OpenLink Virtoso store.
            string connectionString = string.Format("{0};rule=urn:semiodesk/test/ruleset", SetupClass.ConnectionString);

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

            p3.Interests.Add(g2);
            p3.Interests.Add(p3);
            p3.Commit();

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
        public void CanSelectBooleanWithBinaryExpression()
        {
            var states = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Status;

            Assert.AreEqual(1, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where person.Status == true select person.Status;

            Assert.AreEqual(1, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where person.Status == false select person.Status;

            Assert.AreEqual(0, states.ToList().Count);
        }

        [Test]
        public void CanSelectIntegerWithBinaryExpression()
        {
            var ages = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Age;

            Assert.AreEqual(1, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status == true select person.Age;

            Assert.AreEqual(1, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status == false select person.Age;

            Assert.AreEqual(0, ages.ToList().Count);
        }

        [Test]
        public void CanSelectIntegerWithOrderBy()
        {
            var n = from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count select person.KnownPeople.Count;

            Assert.AreEqual(new[] { 0, 1, 2 }, n.ToArray());

            n = from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count descending select person.KnownPeople.Count;

            Assert.AreEqual(new[] { 2, 1, 0 }, n.ToArray());

            n = (from person in Model.AsQueryable<Person>() select person.KnownPeople.Count).OrderBy(i => i);

            Assert.AreEqual(new[] { 0, 1, 2 }, n.ToArray());

            n = (from person in Model.AsQueryable<Person>() select person.KnownPeople.Count).OrderByDescending(i => i);

            Assert.AreEqual(new[] { 2, 1, 0 }, n.ToArray());
        }

        [Test]
        public void CanSelectIntegerWithResultOperatorSkip()
        {
            var ages = (from person in Model.AsQueryable<Person>() select person.Age).Skip(0);

            Assert.AreEqual(3, ages.ToArray().Length);

            ages = (from person in Model.AsQueryable<Person>() select person.Age).Skip(1);

            Assert.AreEqual(2, ages.ToArray().Length);

            ages = (from person in Model.AsQueryable<Person>() select person.Age).Skip(2);

            Assert.AreEqual(1, ages.ToArray().Length);

            ages = (from person in Model.AsQueryable<Person>() select person.Age).Skip(3);

            Assert.AreEqual(0, ages.ToArray().Length);
        }

        [Test]
        public void CanSelectIntegerWithResultOperatorTake()
        {
            var ages = (from person in Model.AsQueryable<Person>() select person.Age).Take(0);

            Assert.AreEqual(0, ages.ToArray().Length);

            ages = (from person in Model.AsQueryable<Person>() select person.Age).Take(1);

            Assert.AreEqual(1, ages.ToArray().Length);

            ages = (from person in Model.AsQueryable<Person>() select person.Age).Take(2);

            Assert.AreEqual(2, ages.ToArray().Length);

            ages = (from person in Model.AsQueryable<Person>() select person.Age).Take(3);

            Assert.AreEqual(3, ages.ToArray().Length);
        }

        [Test]
        public void CanSelectDateTimeWithBinaryExpression()
        {
            var birthdays = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Birthday;

            Assert.AreEqual(1, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where person.Status == true select person.Birthday;

            Assert.AreEqual(1, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where person.Status == false select person.Birthday;

            Assert.AreEqual(0, birthdays.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithBinaryExpression()
        {
            var names = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status == true select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status == false select person.FirstName;

            Assert.AreEqual(0, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithBinaryExpressionOnStringLength()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.Length == 5 select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.Length != 5 select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.Length < 5 select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.Length <= 5 select person.FirstName;

            Assert.AreEqual(3, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.Length > 5 select person.FirstName;

            Assert.AreEqual(0, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.Length >= 5 select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithMethodContains()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.Contains("e") select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithMethodStartsWith()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("A") select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("a", true, CultureInfo.CurrentCulture) select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithMethodEndsWith()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("e") select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("E", true, CultureInfo.CurrentCulture) select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithRegexIsMatch()
        {
            var names = from person in Model.AsQueryable<Person>() where Regex.IsMatch(person.FirstName, "e") select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where Regex.IsMatch(person.FirstName, "E", RegexOptions.IgnoreCase) select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);
        }

        [Test]
        public void CanSelectResourceWithBinaryExpression()
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
        public void CanSelectResourcesWithNodeTypeAndAlso()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" && person.LastName == "Cooper" select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" && person.LastName == "Dylan" select person;

            Assert.AreEqual(0, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" && person.LastName == "Cooper" && person.KnownPeople.Count == 1 select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithNodeTypeOrElse()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" || person.FirstName == "Bob" select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" || person.FirstName == "Bob" || person.FirstName == "Eve" select person;

            Assert.AreEqual(3, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" || person.FirstName == "Bob" || person.KnownPeople.Count == 0 select person;

            Assert.AreEqual(3, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithEqualsOnString()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.FirstName.Equals("Alice") select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorCount()
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
        public void CanSelectResourcesWithResultOperatorFirst()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.First().KnownPeople.Count == 1 select person;

            // Trinity does not store list values in a defined order yet.
            // TODO: Add support for SPARQL list syntax to Trinity. Breaks compatibility.
            Assert.Throws<NotSupportedException>(() => { persons.ToList(); });
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorLast()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Last().KnownPeople.Count == 1 select person;

            // Trinity does not store list values in a defined order yet.
            // TODO: Add support for SPARQL list syntax to Trinity. Breaks compatibility.
            Assert.Throws<NotSupportedException>(() => { persons.ToList(); });
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorSkip()
        {
            var persons = (from person in Model.AsQueryable<Person>() select person).Skip(0);

            Assert.AreEqual(3, persons.ToArray().Length);

            persons = (from person in Model.AsQueryable<Person>() select person).Skip(1);

            Assert.AreEqual(2, persons.ToArray().Length);

            persons = (from person in Model.AsQueryable<Person>() select person).Skip(2);

            Assert.AreEqual(1, persons.ToArray().Length);

            persons = (from person in Model.AsQueryable<Person>() select person).Skip(3);

            Assert.AreEqual(0, persons.ToArray().Length);
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorTake()
        {
            var persons = (from person in Model.AsQueryable<Person>() select person).Take(0);

            Assert.AreEqual(0, persons.ToArray().Length);

            persons = (from person in Model.AsQueryable<Person>() select person).Take(1);

            Assert.AreEqual(1, persons.ToArray().Length);

            persons = (from person in Model.AsQueryable<Person>() select person).Take(2);

            Assert.AreEqual(2, persons.ToArray().Length);

            persons = (from person in Model.AsQueryable<Person>() select person).Take(3);

            Assert.AreEqual(3, persons.ToArray().Length);
        }

        [Test]
        public void CanSelectResourcesWithOperatorTypeOf()
        {
            var resources = from resource in Model.AsQueryable<Resource>() select resource;

            Assert.AreEqual(5, resources.ToList().Count);

            resources = from resource in Model.AsQueryable<Resource>() where resource is Person select resource;

            Assert.AreEqual(3, resources.ToList().Count);

            resources = from resource in Model.AsQueryable<Person>() where resource.Interests.OfType<Group>().Count() > 0 select resource;

            Assert.AreEqual(1, resources.ToList().Count);

            resources = from resource in Model.AsQueryable<Person>() where resource.Interests.OfType<Person>().Count() > 0 select resource;

            Assert.AreEqual(1, resources.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithSubQuery()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") && person.KnownPeople.Any(p => p.FirstName == "Bob") && person.KnownPeople.Any(p => p.FirstName == "Eve") select person;

            var x = persons.ToList();

            Assert.AreEqual(0, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") || person.KnownPeople.Any(p => p.FirstName == "Eve") select person;

            var y = persons.ToList();

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") || person.KnownPeople.Any(p => p.FirstName == "Bob") select person;

            var z = persons.ToList();

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithOrderBy()
        {
            var persons = from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count select person;

            var P = persons.ToArray();

            Assert.AreEqual(3, P.Length);
            Assert.AreEqual(0, P[0].KnownPeople.Count);
            Assert.AreEqual(1, P[1].KnownPeople.Count);
            Assert.AreEqual(2, P[2].KnownPeople.Count);

            persons = from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count descending select person;

            P = persons.ToArray();

            Assert.AreEqual(3, P.Length);
            Assert.AreEqual(2, P[0].KnownPeople.Count);
            Assert.AreEqual(1, P[1].KnownPeople.Count);
            Assert.AreEqual(0, P[2].KnownPeople.Count);

            persons = (from person in Model.AsQueryable<Person>() select person).OrderBy(p => p.KnownPeople.Count);

            P = persons.ToArray();

            Assert.AreEqual(3, P.Length);
            Assert.AreEqual(0, P[0].KnownPeople.Count);
            Assert.AreEqual(1, P[1].KnownPeople.Count);
            Assert.AreEqual(2, P[2].KnownPeople.Count);

            persons = (from person in Model.AsQueryable<Person>() select person).OrderByDescending(p => p.KnownPeople.Count);

            P = persons.ToArray();

            Assert.AreEqual(3, P.Length);
            Assert.AreEqual(2, P[0].KnownPeople.Count);
            Assert.AreEqual(1, P[1].KnownPeople.Count);
            Assert.AreEqual(0, P[2].KnownPeople.Count);
        }

        [Test]
        public void CanSelectResourcesWithVariableExpression()
        {
            foreach(int age in new [] { 40, 50, 60 })
            {
                CanSelectResourcesWithVariableExpression(age);
            }
        }

        private void CanSelectResourcesWithVariableExpression(int minAge)
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Age > minAge select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age >= minAge select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age < minAge select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age <= minAge select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age != minAge select person;

            Assert.AreEqual(3, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Age == minAge select person;

            Assert.AreEqual(0, persons.ToList().Count);
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