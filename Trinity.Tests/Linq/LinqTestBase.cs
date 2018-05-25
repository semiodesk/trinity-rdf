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

namespace Semiodesk.Trinity.Test.Linq
{
    [TestFixture]
    public abstract class LinqTestBase
    {
        protected IStore Store;

        protected IModel Model;

        public LinqTestBase() { }

        [SetUp]
        public abstract void SetUp();

        [Test]
        public void CanAskResourceWithBinaryExpressionOnBoolean()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Status select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Status == true select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where !person.Status select person;

            Assert.IsTrue(persons.Any());

            persons = from person in Model.AsQueryable<Person>() where person.Status == false select person;

            Assert.IsTrue(persons.Any());
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

            states = from person in Model.AsQueryable<Person>() where person.Status select person.Status;

            Assert.AreEqual(1, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where person.Status == true select person.Status;

            Assert.AreEqual(1, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Status;

            Assert.AreEqual(2, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where !person.Status select person.Status;

            Assert.AreEqual(2, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() where person.Status == false select person.Status;

            Assert.AreEqual(2, states.ToList().Count);

            states = from person in Model.AsQueryable<Person>() select person.Status;

            Assert.AreEqual(3, states.ToList().Count);
        }

        [Test]
        public void CanSelectIntegerWithBinaryExpression()
        {
            var ages = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Age;

            Assert.AreEqual(1, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status select person.Age;

            Assert.AreEqual(1, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status == true select person.Age;

            Assert.AreEqual(1, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Age;

            Assert.AreEqual(2, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where !person.Status select person.Age;

            Assert.AreEqual(2, ages.ToList().Count);

            ages = from person in Model.AsQueryable<Person>() where person.Status == false select person.Age;

            Assert.AreEqual(2, ages.ToList().Count);
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

            birthdays = from person in Model.AsQueryable<Person>() where person.Status select person.Birthday;

            Assert.AreEqual(1, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where person.Status == true select person.Birthday;

            Assert.AreEqual(1, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Birthday;

            Assert.AreEqual(2, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where !person.Status select person.Birthday;

            Assert.AreEqual(2, birthdays.ToList().Count);

            birthdays = from person in Model.AsQueryable<Person>() where person.Status == false select person.Birthday;

            Assert.AreEqual(2, birthdays.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithBinaryExpression()
        {
            var names = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status == true select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where !person.Status select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.Status == false select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);
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
        public void CanSelectStringWithAsQueryable()
        {
            var names = Model.AsQueryable<Person>().Select(p => p.FirstName);

            Assert.AreEqual(3, names.ToList().Count);

            names = from p in Model.AsQueryable<Person>() select p.FirstName;

            Assert.AreEqual(3, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithMethodEquals()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.Equals("Alice") select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where !person.FirstName.Equals("Alice") select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithMethodContains()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.Contains("e") select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where !person.FirstName.Contains("e") select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithMethodCount()
        {
            var count = Model.AsQueryable<Person>().Count(p => p.FirstName == "Bob");

            Assert.AreEqual(1, count);

            count = Model.AsQueryable<Person>().Count(p => p.FirstName != "Bob");

            Assert.AreEqual(2, count);

            count = Model.AsQueryable<Agent>().Count(p => p.FirstName != "Bob");

            Assert.AreEqual(1, count);

            count = Model.AsQueryable<Person>().Count(p => p.KnownPeople.Any(q => q.FirstName.Equals("Alice") && q.LastName.StartsWith("C")));

            Assert.AreEqual(1, count);

            count = Model.AsQueryable<Person>().Count(p => p.KnownPeople.Any(q => q.FirstName.Equals("Alice") && q.LastName.StartsWith("X")));

            Assert.AreEqual(0, count);

            count = Model.AsQueryable<Person>().Count(p => p.KnownPeople.Any(q => q.FirstName.Equals("Alice") || q.LastName.StartsWith("d", StringComparison.InvariantCultureIgnoreCase)));

            Assert.AreEqual(2, count);

            count = Model.AsQueryable<Person>().Count(p => !p.Interests.Any());

            Assert.AreEqual(2, count);
        }

        [Test]
        public void CanSelectStringWithMethodStartsWith()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("A") select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("a", true, CultureInfo.CurrentCulture) select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("a", StringComparison.CurrentCultureIgnoreCase) select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("a", StringComparison.InvariantCultureIgnoreCase) select person.FirstName;

            Assert.AreEqual(1, names.ToList().Count);
        }

        [Test]
        public void CanSelectStringWithMethodEndsWith()
        {
            var names = from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("e") select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("E", true, CultureInfo.CurrentCulture) select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("E", StringComparison.CurrentCultureIgnoreCase) select person.FirstName;

            Assert.AreEqual(2, names.ToList().Count);

            names = from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("E", StringComparison.InvariantCultureIgnoreCase) select person.FirstName;

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
        public void CanSelectResourcesWithAsQueryable()
        {
            var persons = Model.AsQueryable<Person>();

            Assert.AreEqual(3, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourceWithBinaryExpression()
        {
            var groups = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Group;

            Assert.AreEqual(1, groups.ToList().Count);

            groups = from person in Model.AsQueryable<Person>() where person.Status select person.Group;

            Assert.AreEqual(1, groups.ToList().Count);

            groups = from person in Model.AsQueryable<Person>() where person.Status == true select person.Group;

            Assert.AreEqual(1, groups.ToList().Count);

            groups = from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Group;

            Assert.AreEqual(2, groups.ToList().Count);

            groups = from person in Model.AsQueryable<Person>() where !person.Status select person.Group;

            Assert.AreEqual(2, groups.ToList().Count);

            groups = from person in Model.AsQueryable<Person>() where person.Status == false select person.Group;

            Assert.AreEqual(2, groups.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnBoolean()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status == true select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status != true select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status == false select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Status != false select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where !person.Status select person;

            Assert.AreEqual(2, persons.ToList().Count);
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
            var persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName != "Alice" select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnResource()
        {
            Person p = new Person(ex.Alice);

            var persons = from person in Model.AsQueryable<Person>() where person == p select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person != p select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnResourceMember()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Group.Name.Equals("The Spiders") select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Group.Name == "The Spiders" select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Group.Name != "The Spiders" select person;

            Assert.AreEqual(1, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnUri()
        {
            Person p = new Person(ex.Alice);

            var persons = from person in Model.AsQueryable<Person>() where person.Uri == p.Uri select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Uri != p.Uri select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Group.Uri == ex.TheSpiders select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Group.Uri != ex.TheSpiders select person;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnNull()
        {
            var persons = from person in Model.AsQueryable<Person>() where person.Group == null select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.Group != null select person;

            Assert.AreEqual(2, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName == "Bob" && person.Group == null select person;

            Assert.AreEqual(1, persons.ToList().Count);

            persons = from person in Model.AsQueryable<Person>() where person.FirstName != "Bob" && person.Group == null select person;

            Assert.AreEqual(0, persons.ToList().Count);
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

            persons = from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(q => q.FirstName.Equals("Alice") && q.LastName.StartsWith("C")) select person;

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
            Assert.Throws<InvalidOperationException>(() =>
            {
                Model.AsQueryable<Person>().OrderBy(p => p.FirstName).First(p => p.Age > 40 && p.Age < 40);
            });
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorFirstOrDefault()
        {
            var persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).ToList();
            var person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).FirstOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());

            persons = Model.AsQueryable<Person>().OrderBy(p => p.Age).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.Age).FirstOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());

            persons = Model.AsQueryable<Person>().OrderBy(p => p.Birthday).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.Birthday).FirstOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age > 40).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).FirstOrDefault(p => p.Age > 40);

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());
            Assert.IsTrue(person.Age > 40);

            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age > 40).FirstOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());
            Assert.IsTrue(person.Age > 40);

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age > 40 && p.Age < 40).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).FirstOrDefault(p => p.Age > 40 && p.Age < 40);

            Assert.IsEmpty(persons);
            Assert.IsNull(person);

            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age > 40 && p.Age < 40).FirstOrDefault();

            Assert.IsEmpty(persons);
            Assert.IsNull(person);

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age < 10 || p.Age > 40).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).FirstOrDefault(p => p.Age < 10 || p.Age > 40);

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());
            Assert.IsTrue(person.Age < 10 || person.Age > 40);

            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age < 10 || p.Age > 40).FirstOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());
            Assert.IsTrue(person.Age < 10 || person.Age > 40);

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.KnownPeople.Any(p0 => p0.FirstName == "Alice")).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).FirstOrDefault(p => p.KnownPeople.Any(p0 => p0.FirstName == "Alice"));

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());
            Assert.IsTrue(person.KnownPeople.Any(p0 => p0.FirstName == "Alice"));

            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.KnownPeople.Any(p0 => p0.FirstName == "Alice")).FirstOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.FirstOrDefault());
            Assert.IsTrue(person.KnownPeople.Any(p0 => p0.FirstName == "Alice"));

            // Using FirstOrDefault in subqueries is not yet supported.
            persons = Model.AsQueryable<Person>().Where(p => p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault(q => q.KnownPeople.Count == 1) != null).ToList();

            Assert.IsNotEmpty(persons);

            foreach (Person p in persons)
            {
                Assert.AreEqual(1, p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault().KnownPeople.Count);
            }

            persons = Model.AsQueryable<Person>().Where(p => p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault().KnownPeople.Count == 1).ToList();

            Assert.IsNotEmpty(persons);

            foreach (Person p in persons)
            {
                Assert.AreEqual(1, p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault().KnownPeople.Count);
            }
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorLast()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Last(p => p.Age > 40 && p.Age < 40);
            });
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorLastOrDefault()
        {
            var persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).ToList();
            var person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).LastOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());

            persons = Model.AsQueryable<Person>().OrderBy(p => p.Age).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.Age).LastOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());

            persons = Model.AsQueryable<Person>().OrderBy(p => p.Birthday).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.Birthday).LastOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age > 40).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).LastOrDefault(p => p.Age > 40);

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());
            Assert.IsTrue(person.Age > 40);

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age > 40 && p.Age < 40).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).LastOrDefault(p => p.Age > 40 && p.Age < 40);

            Assert.IsEmpty(persons);
            Assert.IsNull(person);

            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age > 40 && p.Age < 40).LastOrDefault();

            Assert.IsEmpty(persons);
            Assert.IsNull(person);

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age < 10 || p.Age > 40).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).LastOrDefault(p => p.Age < 10 || p.Age > 40);

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());
            Assert.IsTrue(person.Age < 10 || person.Age > 40);

            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.Age < 10 || p.Age > 40).LastOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());
            Assert.IsTrue(person.Age < 10 || person.Age > 40);

            persons = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.KnownPeople.Any(p0 => p0.FirstName == "Alice")).ToList();
            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).LastOrDefault(p => p.KnownPeople.Any(p0 => p0.FirstName == "Alice"));

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());
            Assert.IsTrue(person.KnownPeople.Any(p0 => p0.FirstName == "Alice"));

            person = Model.AsQueryable<Person>().OrderBy(p => p.FirstName).Where(p => p.KnownPeople.Any(p0 => p0.FirstName == "Alice")).LastOrDefault();

            Assert.IsNotNull(person);
            Assert.AreEqual(person, persons.LastOrDefault());
            Assert.IsTrue(person.KnownPeople.Any(p0 => p0.FirstName == "Alice"));

            // Using LastOrDefault in subqueries is not yet supported.
            persons = Model.AsQueryable<Person>().Where(p => p.KnownPeople.OrderBy(q => q.FirstName).LastOrDefault(q => q.KnownPeople.Count == 1) != null).ToList();

            Assert.IsNotEmpty(persons);

            foreach (Person p in persons)
            {
                Assert.AreEqual(1, p.KnownPeople.OrderBy(q => q.FirstName).LastOrDefault().KnownPeople.Count);
            }

            /*
            Assert.Throws<NotSupportedException>(() =>
            {
                Model.AsQueryable<Person>().Where(p => p.KnownPeople.LastOrDefault(q => q.KnownPeople.Count == 1) != null).ToList();
            });
            */
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

            Assert.AreEqual(7, resources.ToList().Count);

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
            foreach (int age in new[] { 40, 50, 60 })
            {
                CanSelectResourcesWithVariableExpression(age);
            }
        }

        [Test]
        public void CanSelectResourcesWhichImplementInterface()
        {
            var images = (from image in Model.AsQueryable<Image>() where image.DepictedAgent.Uri == ex.Alice select image).ToList();

            Assert.AreEqual(1, images.Count);

            // Tests if retrieving resources is possible through extension methods 
            // that have generic parameters with iterfaces.
            Agent agent = Model.GetResource<Agent>(ex.Alice);

            images = agent.GetImages<Image>(Model).Where(i => i.DepictedAgent == agent).ToList();

            Assert.AreEqual(1, images.Count);
        }

        [Test]
        public void CanExecuteCollectionWithInferencingEnabled()
        {
            // See if inferencing works on resource queries.
            var agents = Model.AsQueryable<Agent>().ToList();

            Assert.AreEqual(1, agents.Count);

            agents = Model.AsQueryable<Agent>(true).ToList();

            Assert.AreEqual(6, agents.Count);

            // See if inferencing works with queries that return bindings.
            var names = Model.AsQueryable<Agent>().Select(a => a.FirstName).ToList();

            Assert.AreEqual(1, names.Count);

            names = Model.AsQueryable<Agent>(true).Select(a => a.FirstName).ToList();

            Assert.AreEqual(4, names.Count);
        }

        [Test]
        public void CanExecuteScalarWithInferencingEnabled()
        {
            // See if inferencing works for boolean (ASK) queries.
            bool hasAgent = Model.AsQueryable<Agent>().Where(a => a.FirstName == "Alice").Any();

            Assert.IsFalse(hasAgent);

            hasAgent = Model.AsQueryable<Agent>(true).Where(a => a.FirstName == "Alice").Any();

            Assert.IsTrue(hasAgent);

            // See if inferencing works for queries that return numeric bindings.
            int agentCount = Model.AsQueryable<Agent>().Where(a => a.FirstName == "Alice").Count();

            Assert.AreEqual(0, agentCount);

            agentCount = Model.AsQueryable<Agent>(true).Where(a => a.FirstName == "Alice").Count();

            Assert.AreEqual(1, agentCount);
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

            foreach (BindingSet b in Model.GetBindings(q))
            {
                Debug.WriteLine(b["s"] + " " + b["p"] + " " + b["o"]);
            }

            Debug.WriteLine("");
        }
    }
}