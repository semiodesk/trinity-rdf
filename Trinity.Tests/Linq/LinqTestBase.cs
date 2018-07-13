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

        [SetUp]
        public abstract void SetUp();

        [Test]
        public void CanAskResourceWithBinaryExpressionOnBoolean()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(true) select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == true select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == false select person).Any();
            Assert.IsTrue(actual);
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnDateTime()
        {
            DateTime value = new DateTime(1948, 2, 4);

            var actual = (from person in Model.AsQueryable<Person>() where person.Birthday.Equals(value) select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Birthday.Equals(value) select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday == value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday != value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday < value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday <= value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday >= value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday > value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday > DateTime.MaxValue select person).Any();
            Assert.IsFalse(actual);
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnFloat()
        {
            float value = 100000;

            var actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance.Equals(value) select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.AccountBalance.Equals(value) select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance == value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance != value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance < value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance <= value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance >= value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance > value select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance > float.MaxValue select person).Any();
            Assert.IsFalse(actual);
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnString()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.LastName == "Alice" select person).Any();
            Assert.IsFalse(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName != "Alice" select person).Any();
            Assert.IsTrue(actual);
        }

        [Test]
        public void CanAskResourceWithBinaryExpressionOnResource()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.Group.Name.Equals("The Spiders") select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Name == "The Spiders" select person).Any();
            Assert.IsTrue(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Name == "The Bugs" select person).Any();
            Assert.IsFalse(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Name != "The Spiders" select person).Any();
            Assert.IsTrue(actual);
        }

        [Test]
        public void CanSelectBooleanWithBinaryExpression()
        {
            // True
            var expectedTrue = new[] { true };

            var actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(false) select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == true select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            // True
            var expectedFalse = new[] { false, false };

            actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(true) select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == false select person.Status).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);
        }

        [Test]
        public void CanSelectIntegerWithBinaryExpression()
        {
            // True
            var expectedTrue = new[] { 69 };

            var actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(false) select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == true select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            // False
            var expectedFalse = new[] { 38, 76 };

            actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(true) select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == false select person.Age).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);
        }

        [Test]
        public void CanSelectIntegerWithOrderBy()
        {
            var actual = (from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count select person.KnownPeople.Count).ToList();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count descending select person.KnownPeople.Count).ToList();
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, actual);

            actual = (from person in Model.AsQueryable<Person>() select person.KnownPeople.Count).OrderBy(i => i).ToList();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, actual);

            actual = (from person in Model.AsQueryable<Person>() select person.KnownPeople.Count).OrderByDescending(i => i).ToList();
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, actual);
        }

        [Test]
        public void CanSelectIntegerWithResultOperatorSkip()
        {
            var actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Skip(0).ToList();
            CollectionAssert.AreEqual(new[] { 38, 69, 76 }, actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Skip(1).ToList();
            CollectionAssert.AreEqual(new[] { 69, 76 }, actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Skip(2).ToList();
            CollectionAssert.AreEqual(new[] { 76 }, actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Skip(3).ToList();
            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void CanSelectIntegerWithResultOperatorTake()
        {
            var actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Take(0).ToList();
            CollectionAssert.IsEmpty(actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Take(1).ToList();
            CollectionAssert.AreEqual(new[] { 38 }, actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Take(2).ToList();
            CollectionAssert.AreEqual(new[] { 38, 69 }, actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.Age select person.Age).Take(3).ToList();
            CollectionAssert.AreEqual(new[] { 38, 69, 76 }, actual);
        }

        [Test]
        public void CanSelectDateTimeWithBinaryExpression()
        {
            // True
            var expectedTrue = new[] { new DateTime(1948, 2, 4) };

            var actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(false) select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == true select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            // False
            var expectedFalse = new[] { new DateTime(1941, 5, 24), new DateTime(1978, 11, 10) };

            actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(true) select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == false select person.Birthday).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);
        }

        [Test]
        public void CanSelectStringWithBinaryExpression()
        {
            // True
            var expectedTrue = new[] { "Alice" };

            var actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(false) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == true select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual);

            // False
            var expectedFalse = new[] { "Bob", "Eve" };

            actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status.Equals(true) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.Status select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Status == false select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual);
        }

        [Test]
        public void CanSelectStringWithBinaryExpressionOnStringLength()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Length == 5 select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new[] { "Alice" }, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Length != 5 select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new[] { "Bob", "Eve" }, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Length < 5 select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new[] { "Bob", "Eve" }, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Length <= 5 select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new[] { "Alice", "Bob", "Eve" }, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Length > 5 select person.FirstName).ToList();
            CollectionAssert.IsEmpty(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Length >= 5 select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new[] { "Alice" }, actual);
        }

        [Test]
        public void CanSelectStringWithAsQueryable()
        {
            var actual = (Model.AsQueryable<Person>().Select(p => p.FirstName));
            CollectionAssert.AreEquivalent(new[] { "Alice", "Bob", "Eve" }, actual);

            actual = (from p in Model.AsQueryable<Person>() select p.FirstName);
            CollectionAssert.AreEquivalent(new[] { "Alice", "Bob", "Eve" }, actual);
        }

        [Test]
        public void CanSelectStringWithMethodEquals()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Equals("Alice") select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new [] { "Alice" }, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.FirstName.Equals("Alice") select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new [] { "Bob", "Eve" }, actual);
        }

        [Test]
        public void CanSelectStringWithMethodContains()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Contains("e") select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new [] { "Alice", "Eve" }, actual);

            actual = (from person in Model.AsQueryable<Person>() where !person.FirstName.Contains("e") select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(new [] { "Bob" }, actual);
        }

        [Test]
        public void CanSelectStringWithMethodCount()
        {
            var actual = Model.AsQueryable<Person>().Count(p => p.FirstName == "Bob");
            Assert.AreEqual(1, actual);

            actual = Model.AsQueryable<Person>().Count(p => p.FirstName != "Bob");
            Assert.AreEqual(2, actual);

            actual = Model.AsQueryable<Agent>().Count(p => p.FirstName != "Bob");
            Assert.AreEqual(1, actual);

            actual = Model.AsQueryable<Person>().Count(p => p.KnownPeople.Any(q => q.FirstName.Equals("Alice") && q.LastName.StartsWith("C")));
            Assert.AreEqual(1, actual);

            actual = Model.AsQueryable<Person>().Count(p => p.KnownPeople.Any(q => q.FirstName.Equals("Alice") && q.LastName.StartsWith("X")));
            Assert.AreEqual(0, actual);

            actual = Model.AsQueryable<Person>().Count(p => p.KnownPeople.Any(q => q.FirstName.Equals("Alice") || q.LastName.StartsWith("d", StringComparison.InvariantCultureIgnoreCase)));
            Assert.AreEqual(2, actual);

            actual = Model.AsQueryable<Person>().Count(p => !p.Interests.Any());
            Assert.AreEqual(2, actual);
        }

        [Test]
        public void CanSelectStringWithMethodStartsWith()
        {
            var expected = new [] { "Alice" };

            // Case-sensitive
            var actual = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("A") select person.FirstName;
            CollectionAssert.AreEquivalent(expected, actual);

            // Not case-sensitive
            actual = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("a", true, CultureInfo.CurrentCulture) select person.FirstName;
            CollectionAssert.AreEquivalent(expected, actual);

            actual = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("a", StringComparison.CurrentCultureIgnoreCase) select person.FirstName;
            CollectionAssert.AreEquivalent(expected, actual);

            actual = from person in Model.AsQueryable<Person>() where person.FirstName.StartsWith("a", StringComparison.InvariantCultureIgnoreCase) select person.FirstName;
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void CanSelectStringWithMethodEndsWith()
        {
            var expected = new [] { "Alice", "Eve" };

            // Case-sensitive
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("e") select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expected, actual);

            // Not case-sensitive
            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("E", true, CultureInfo.CurrentCulture) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expected, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("E", StringComparison.CurrentCultureIgnoreCase) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expected, actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName.EndsWith("E", StringComparison.InvariantCultureIgnoreCase) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void CanSelectStringWithRegexIsMatch()
        {
            var expected = new [] { "Alice", "Eve" };

            // Case-sensitive
            var actual = (from person in Model.AsQueryable<Person>() where Regex.IsMatch(person.FirstName, "e") select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expected, actual);

            // Not case-sensitive
            actual = (from person in Model.AsQueryable<Person>() where Regex.IsMatch(person.FirstName, "E", RegexOptions.IgnoreCase) select person.FirstName).ToList();
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void CanSelectResourcesWithAsQueryable()
        {
            var expected = new [] { ex.Alice, ex.Bob, ex.Eve };
            var actual = Model.AsQueryable<Person>().ToList();

            CollectionAssert.AreEquivalent(expected, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourceWithBinaryExpression()
        {
            // True
            var expectedTrue = new [] { ex.TheSpiders };

            var actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person.Group).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status select person.Group).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status == true select person.Group).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status != false select person.Group).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            // False
            var expectedFalse = new [] { ex.AlicaKeys };
            actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person.Group).ToList();

            CollectionAssert.AreEquivalent(expectedFalse, actual.Select(p => p.Uri));
            actual = (from person in Model.AsQueryable<Person>() where !person.Status select person.Group).ToList();

            CollectionAssert.AreEquivalent(expectedFalse, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status == false select person.Group).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnBoolean()
        {
            // True
            var expectedTrue = new [] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(true) select person).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status select person).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status == true select person).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status != false select person).ToList();
            CollectionAssert.AreEquivalent(expectedTrue, actual.Select(p => p.Uri));

            // False
            var expectedFalse = new UriRef[] { ex.Bob, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Status.Equals(false) select person).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status == false select person).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where !person.Status select person).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Status != true select person).ToList();
            CollectionAssert.AreEquivalent(expectedFalse, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnDateTime()
        {
            var value = new DateTime(1948, 2, 4);

            // Equal
            var expectedEqual = new UriRef[] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.Birthday.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday == value select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            // NotEqual
            var expectedNotEqual = new UriRef[] { ex.Bob, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday != value select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where !person.Birthday.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            // Less
            var expectedLess = new UriRef[] { ex.Bob };

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday < value select person).ToList();
            CollectionAssert.AreEquivalent(expectedLess, actual.Select(p => p.Uri));

            // LessOrEqual
            var expectedLessOrEqual = new UriRef[] { ex.Alice, ex.Bob };

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday <= value select person).ToList();
            CollectionAssert.AreEquivalent(expectedLessOrEqual, actual.Select(p => p.Uri));

            // Greater
            var expectedGreater = new UriRef[] { ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday > value select person).ToList();
            CollectionAssert.AreEquivalent(expectedGreater, actual.Select(p => p.Uri));

            // GreaterOrEqual
            var expectedGreaterOrEqual = new UriRef[] { ex.Alice, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Birthday >= value select person).ToList();
            CollectionAssert.AreEquivalent(expectedGreaterOrEqual, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnInteger()
        {
            var value = 69;

            // Equal
            var expectedEqual = new UriRef[] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.Age.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Age == value select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            // NotEqual
            var expectedNotEqual = new UriRef[] { ex.Bob, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Age != value select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where !person.Age.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            // Less
            var expectedLess = new UriRef[] { ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Age < value select person).ToList();
            CollectionAssert.AreEquivalent(expectedLess, actual.Select(p => p.Uri));

            // LessOrEqual
            var expectedLessOrEqual = new UriRef[] { ex.Alice, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Age <= value select person).ToList();
            CollectionAssert.AreEquivalent(expectedLessOrEqual, actual.Select(p => p.Uri));

            // Greater
            var expectedGreater = new UriRef[] { ex.Bob };
            actual = (from person in Model.AsQueryable<Person>() where person.Age > value select person).ToList();

            CollectionAssert.AreEquivalent(expectedGreater, actual.Select(p => p.Uri));

            // GreaterOrEqual
            var expectedGreaterOrEqual = new UriRef[] { ex.Alice, ex.Bob };

            actual = (from person in Model.AsQueryable<Person>() where person.Age >= value select person).ToList();
            CollectionAssert.AreEquivalent(expectedGreaterOrEqual, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnFloat()
        {
            float value = 100000f;

            // Equal
            var expectedEqual = new UriRef[] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance == value select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            // NotEqual
            var expectedNotEqual = new UriRef[] { ex.Bob, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance != value select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where !person.AccountBalance.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            // Less
            var expectedLess = new UriRef[] { ex.Bob };

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance < value select person).ToList();
            CollectionAssert.AreEquivalent(expectedLess, actual.Select(p => p.Uri));

            // LessOrEqual
            var expectedLessOrEqual = new UriRef[] { ex.Alice, ex.Bob };

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance <= value select person).ToList();
            CollectionAssert.AreEquivalent(expectedLessOrEqual, actual.Select(p => p.Uri));

            // Greater
            var expectedGreater = new UriRef[] { ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance > value select person).ToList();
            CollectionAssert.AreEquivalent(expectedGreater, actual.Select(p => p.Uri));

            // GreaterOrEqual
            var expectedGreaterOrEqual = new UriRef[] { ex.Alice, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.AccountBalance >= value select person).ToList();
            CollectionAssert.AreEquivalent(expectedGreaterOrEqual, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnString()
        {
            string value = "Alice";

            // Equal
            var expectedEqual = new UriRef[] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName == value select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            // NotEqual
            var expectedNotEqual = new UriRef[] { ex.Bob, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where !person.FirstName.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName != value select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnResource()
        {
            Person value = new Person(ex.Alice);

            // Equal
            var expectedEqual = new UriRef[] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person == value select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            // NotEqual
            var expectedNotEqual = new UriRef[] { ex.Bob, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where !person.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person != value select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnResourceMember()
        {
            string value = "The Spiders";

            // Equal
            var expectedEqual = new UriRef[] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.Group.Name.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Name == value select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            // NotEqual
            var expectedNotEqual = new UriRef[] { ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where !person.Group.Name.Equals(value) select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Name != value select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnUri()
        {
            Person value = new Person(ex.Alice);

            // Equal
            var expectedEqual = new UriRef[] { ex.Alice };

            var actual = (from person in Model.AsQueryable<Person>() where person.Uri == value.Uri select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Uri == ex.TheSpiders select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Uri.Equals(ex.TheSpiders) select person).ToList();
            CollectionAssert.AreEquivalent(expectedEqual, actual.Select(p => p.Uri));

            // NotEqual
            var expectedNotEqual = new UriRef[] { ex.Bob, ex.Eve };

            actual = (from person in Model.AsQueryable<Person>() where person.Uri != value.Uri select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Group.Uri != ex.TheSpiders select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where !person.Group.Uri.Equals(ex.TheSpiders) select person).ToList();
            CollectionAssert.AreEquivalent(expectedNotEqual, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithBinaryExpressionOnNull()
        {
            string value = "Bob";

            var actual = (from person in Model.AsQueryable<Person>() where person.Group == null select person).ToList();
            CollectionAssert.AreEquivalent(new [] { ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Group == null && person.FirstName == value select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.Group == null && person.FirstName != value select person).ToList();
            CollectionAssert.IsEmpty(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.Group != null select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Eve }, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithNodeTypeAndAlso()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" && person.LastName == "Cooper" select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" && person.LastName == "Dylan" select person).ToList();
            CollectionAssert.IsEmpty(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" && person.LastName == "Cooper" && person.KnownPeople.Count == 1 select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(q => q.FirstName.Equals("Alice") && q.LastName.StartsWith("C")) select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Bob }, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithNodeTypeOrElse()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" || person.FirstName == "Bob" select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" || person.FirstName == "Bob" || person.FirstName == "Eve" select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob, ex.Eve }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.FirstName == "Alice" || person.FirstName == "Bob" || person.KnownPeople.Count == 0 select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob, ex.Eve }, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithEqualsOnString()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.FirstName.Equals("Alice") select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice }, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorCount()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Count != 1 select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Bob, ex.Eve }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Count > 0 select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Count >= 1 select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Count < 1 select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Eve }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Count <= 1 select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Eve }, actual.Select(p => p.Uri));
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

            // Note: Using FirstOrDefault in subqueries is not yet supported.
            Assert.Throws<NotSupportedException>(() =>
            {
                persons = Model.AsQueryable<Person>().Where(p => p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault(q => q.KnownPeople.Count == 1) != null).ToList();

                //Assert.IsNotEmpty(persons);

                //foreach (Person p in persons)
                //{
                //    Assert.AreEqual(1, p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault().KnownPeople.Count);
                //}
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                persons = Model.AsQueryable<Person>().Where(p => p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault().KnownPeople.Count == 1).ToList();

                //Assert.IsNotEmpty(persons);

                //foreach (Person p in persons)
                //{
                //    Assert.AreEqual(1, p.KnownPeople.OrderBy(q => q.FirstName).FirstOrDefault().KnownPeople.Count);
                //}
            });
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

            // Note: Using LastOrDefault in subqueries is not yet supported.
            Assert.Throws<NotSupportedException>(() =>
            {
                persons = Model.AsQueryable<Person>().Where(p => p.KnownPeople.OrderBy(q => q.FirstName).LastOrDefault(q => q.KnownPeople.Count == 1) != null).ToList();

                //Assert.IsNotEmpty(persons);

                //foreach (Person p in persons)
                //{
                //    Assert.AreEqual(1, p.KnownPeople.OrderBy(q => q.FirstName).LastOrDefault().KnownPeople.Count);
                //}
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                Model.AsQueryable<Person>().Where(p => p.KnownPeople.LastOrDefault(q => q.KnownPeople.Count == 1) != null).ToList();

                //Assert.IsNotEmpty(persons);

                //foreach (Person p in persons)
                //{
                //    Assert.AreEqual(1, p.KnownPeople.OrderBy(q => q.FirstName).LastOrDefault().KnownPeople.Count);
                //}
            });
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorSkip()
        {
            var actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Skip(0).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob, ex.Eve }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Skip(1).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Bob, ex.Eve }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Skip(2).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Eve }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Skip(3).ToList();
            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void CanSelectResourcesWithResultOperatorTake()
        {
            var actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Take(0).ToList();
            CollectionAssert.IsEmpty(actual);

            actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Take(1).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Take(2).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() orderby person.FirstName select person).Take(3).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob, ex.Eve }, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithOperatorTypeOf()
        {
            var actual0 = (from resource in Model.AsQueryable<Resource>() select resource).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual0, typeof(Resource));
            Assert.AreEqual(7, actual0.Count);

            actual0 = (from resource in Model.AsQueryable<Resource>() where resource is Person select resource).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual0, typeof(Resource));
            Assert.AreEqual(3, actual0.Count);

            var actual1 = (from person in Model.AsQueryable<Person>() where person.Interests.OfType<Group>().Count() > 0 select person).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual1, typeof(Person));
            Assert.AreEqual(1, actual1.Count);

            actual1 = (from person in Model.AsQueryable<Person>() where person.Interests.OfType<Person>().Count() > 0 select person).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual1, typeof(Person));
            Assert.AreEqual(1, actual1.Count);

            // TODO: Implement and test NotEquals.
            actual1 = (from person in Model.AsQueryable<Person>() where person.Group.GetType() == typeof(Group) select person).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual1, typeof(Person));
            Assert.AreEqual(2, actual1.Count);

            actual1 = (from person in Model.AsQueryable<Person>() where person.Group.GetType() == typeof(Person) select person).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual1, typeof(Person));
            Assert.AreEqual(0, actual1.Count);

            actual1 = (from person in Model.AsQueryable<Person>() where person.Group.GetType() != typeof(Person) select person).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual1, typeof(Person));
            Assert.AreEqual(2, actual1.Count);

            actual1 = (from person in Model.AsQueryable<Person>() where person.GetType() != typeof(Person) select person).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual1, typeof(Person));
            Assert.AreEqual(0, actual1.Count);

            var actual2 = (from agent in Model.AsQueryable<Agent>() where agent.GetType() == typeof(Person) select agent).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(actual2, typeof(Person));
            Assert.AreEqual(3, actual2.Count);
        }

        [Test]
        public void CanSelectResourcesWithSubQuery()
        {
            var actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") && person.KnownPeople.Any(p => p.FirstName == "Bob") && person.KnownPeople.Any(p => p.FirstName == "Eve") select person).ToList();
            CollectionAssert.IsEmpty(actual);

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") || person.KnownPeople.Any(p => p.FirstName == "Eve") select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Bob }, actual.Select(p => p.Uri));

            actual = (from person in Model.AsQueryable<Person>() where person.KnownPeople.Any(p => p.FirstName == "Alice") || person.KnownPeople.Any(p => p.FirstName == "Bob") select person).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob }, actual.Select(p => p.Uri));
        }

        [Test]
        public void CanSelectResourcesWithOrderBy()
        {
            var actual = (from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count select person).ToList();

            Assert.AreEqual(3, actual.Count);
            Assert.AreEqual(0, actual[0].KnownPeople.Count);
            Assert.AreEqual(1, actual[1].KnownPeople.Count);
            Assert.AreEqual(2, actual[2].KnownPeople.Count);
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[1].KnownPeople, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[2].KnownPeople, typeof(Person));

            actual = (from person in Model.AsQueryable<Person>() orderby person.KnownPeople.Count descending select person).ToList();

            Assert.AreEqual(3, actual.Count);
            Assert.AreEqual(2, actual[0].KnownPeople.Count);
            Assert.AreEqual(1, actual[1].KnownPeople.Count);
            Assert.AreEqual(0, actual[2].KnownPeople.Count);
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[0].KnownPeople, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[1].KnownPeople, typeof(Person));

            actual = (from person in Model.AsQueryable<Person>() select person).OrderBy(p => p.KnownPeople.Count).ToList();

            Assert.AreEqual(3, actual.Count);
            Assert.AreEqual(0, actual[0].KnownPeople.Count);
            Assert.AreEqual(1, actual[1].KnownPeople.Count);
            Assert.AreEqual(2, actual[2].KnownPeople.Count);
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[1].KnownPeople, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[2].KnownPeople, typeof(Person));

            actual = (from person in Model.AsQueryable<Person>() select person).OrderByDescending(p => p.KnownPeople.Count).ToList();

            Assert.AreEqual(3, actual.Count);
            Assert.AreEqual(2, actual[0].KnownPeople.Count);
            Assert.AreEqual(1, actual[1].KnownPeople.Count);
            Assert.AreEqual(0, actual[2].KnownPeople.Count);
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[0].KnownPeople, typeof(Person));
            CollectionAssert.AllItemsAreInstancesOfType(actual[1].KnownPeople, typeof(Person));
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
            var actual = (from image in Model.AsQueryable<Image>() where image.DepictedAgent.Uri == ex.Alice select image).ToList();

            Assert.AreEqual(1, actual.Count);
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(Image));

            // Tests if retrieving resources is possible through extension methods 
            // that have generic parameters with iterfaces.
            Agent agent = Model.GetResource<Agent>(ex.Alice);

            actual = agent.GetImages<Image>(Model).Where(i => i.DepictedAgent == agent).ToList();

            Assert.AreEqual(1, actual.Count);
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(Image));
        }

        [Test]
        public void CanSelectResourcesFromQuerySourceProperty()
        {
            var actual = (from image in Model.AsQueryable<Image>(true) where image.DepictedAgent.FirstName == "Alice" select image.DepictedAgent).ToList();

            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Alice", actual.First().FirstName);
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(Agent));
        }

        [Test]
        public void CanExecuteCollectionWithInferencingEnabled()
        {
            // Check if inferencing works on resource queries.
            var actual0 = Model.AsQueryable<Agent>().ToList();
            CollectionAssert.AreEquivalent(new[] { ex.John }, actual0.Select(p => p.Uri));

            actual0 = Model.AsQueryable<Agent>(true).ToList();
            CollectionAssert.AreEquivalent(new[] { ex.Alice, ex.Bob, ex.Eve, ex.John, ex.TheSpiders, ex.AlicaKeys }, actual0.Select(p => p.Uri));

            // Check if inferencing works with queries that return bindings.
            var actual1 = Model.AsQueryable<Agent>().Select(a => a.FirstName).ToList();
            CollectionAssert.AreEquivalent(new[] { "John" }, actual1);

            actual1 = Model.AsQueryable<Agent>(true).Select(a => a.FirstName).ToList();
            CollectionAssert.AreEquivalent(new[] { "John", "Alice", "Bob", "Eve" }, actual1);
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
            var actual = (from person in Model.AsQueryable<Person>() where person.Age > minAge select person).ToList();
            Assert.AreEqual(2, actual.Count);

            actual = (from person in Model.AsQueryable<Person>() where person.Age >= minAge select person).ToList();
            Assert.AreEqual(2, actual.Count);

            actual = (from person in Model.AsQueryable<Person>() where person.Age < minAge select person).ToList();
            Assert.AreEqual(1, actual.Count);

            actual = (from person in Model.AsQueryable<Person>() where person.Age <= minAge select person).ToList();
            Assert.AreEqual(1, actual.Count);

            actual = (from person in Model.AsQueryable<Person>() where person.Age != minAge select person).ToList();
            Assert.AreEqual(3, actual.Count);

            actual = (from person in Model.AsQueryable<Person>() where person.Age == minAge select person).ToList();
            Assert.AreEqual(0, actual.Count);
        }
    }
}