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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Semiodesk.Trinity.Query.Sparql;
using Semiodesk.Trinity.Tests.Linq;

namespace Semiodesk.Trinity.Tests.Query.Sparql
{
    /// <summary>
    /// End-to-end vertical-slice tests for the new SPARQL LINQ provider (Where / OfType / OrderBy /
    /// Skip / Take / Any / Count / First over an in-memory store). This is the exemplar the operator
    /// breadth is extended from.
    /// </summary>
    [TestFixture]
    public class SparqlLinqExemplarTest
    {
        private IStore _store;

        private IModel _model;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OntologyDiscovery.AddAssembly(typeof(Person).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Person).Assembly);
            OntologyDiscovery.AddAssembly(typeof(Resource).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Resource).Assembly);

            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            _model = _store.CreateModel(new Uri("http://example.org/exemplar"));
            _model.Clear();

            Group spiders = _model.CreateResource<Group>(ex.TheSpiders);
            spiders.Name = "The Spiders";
            spiders.Commit();

            Agent john = _model.CreateResource<Agent>(ex.John);
            john.FirstName = "John";
            john.Commit();

            Person alice = _model.CreateResource<Person>(ex.Alice);
            alice.FirstName = "Alice";
            alice.Age = 69;
            alice.Group = spiders;
            alice.Commit();

            Person bob = _model.CreateResource<Person>(ex.Bob);
            bob.FirstName = "Bob";
            bob.Age = 76;
            bob.Commit();

            Person eve = _model.CreateResource<Person>(ex.Eve);
            eve.FirstName = "Eve";
            eve.Age = 38;
            eve.Commit();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _store?.Dispose();
        }

        [Test]
        public void SelectsAllResourcesOfType()
        {
            var people = _model.AsSparqlQueryable<Person>().ToList();

            Assert.AreEqual(3, people.Count);
        }

        [Test]
        public void FiltersByStringEquality()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.FirstName == "Alice").ToList();

            Assert.AreEqual(1, people.Count);
            Assert.AreEqual("Alice", people[0].FirstName);
        }

        [Test]
        public void FiltersByNumericComparison()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.Age >= 69).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void FiltersByConjunction()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.Age >= 40 && p.FirstName == "Bob").ToList();

            Assert.AreEqual(1, people.Count);
            Assert.AreEqual("Bob", people[0].FirstName);
        }

        [Test]
        public void FiltersByNestedMemberAccess()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.Group.Name == "The Spiders").ToList();

            Assert.AreEqual(1, people.Count);
            Assert.AreEqual("Alice", people[0].FirstName);
        }

        [Test]
        public void FiltersToExactType()
        {
            // A type query matches resources explicitly typed with that class. John is committed as a
            // foaf:Agent and the persons as foaf:Person; derived resources are NOT re-typed with base
            // classes (a GetTypes/inference concern, tracked separately), so an Agent query returns
            // just John and a Person query excludes him.
            var agents = _model.AsSparqlQueryable<Agent>().ToList();

            Assert.AreEqual(1, agents.Count);
            Assert.AreEqual("John", agents[0].FirstName);

            var people = _model.AsSparqlQueryable<Person>().ToList();

            Assert.AreEqual(3, people.Count);
            CollectionAssert.DoesNotContain(people.Select(p => p.FirstName).ToList(), "John");
        }

        [Test]
        public void OrdersAndTakes()
        {
            var youngest = _model.AsSparqlQueryable<Person>().OrderBy(p => p.Age).First();

            Assert.AreEqual("Eve", youngest.FirstName);
        }

        [Test]
        public void OrdersDescendingAndTakes()
        {
            var oldest = _model.AsSparqlQueryable<Person>().OrderByDescending(p => p.Age).Take(1).ToList();

            Assert.AreEqual(1, oldest.Count);
            Assert.AreEqual("Bob", oldest[0].FirstName);
        }

        [Test]
        public void SkipsWithOrdering()
        {
            var people = _model.AsSparqlQueryable<Person>().OrderBy(p => p.Age).Skip(1).ToList();

            Assert.AreEqual(2, people.Count);
            Assert.AreEqual("Alice", people[0].FirstName);
        }

        [Test]
        public void AnswersAny()
        {
            Assert.IsTrue(_model.AsSparqlQueryable<Person>().Any(p => p.FirstName == "Alice"));
            Assert.IsFalse(_model.AsSparqlQueryable<Person>().Any(p => p.FirstName == "Nobody"));
        }

        [Test]
        public void CountsResources()
        {
            Assert.AreEqual(3, _model.AsSparqlQueryable<Person>().Count());
            Assert.AreEqual(2, _model.AsSparqlQueryable<Person>().Count(p => p.Age >= 69));
        }
    }
}
