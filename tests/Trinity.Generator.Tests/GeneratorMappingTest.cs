using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Generator.Tests
{
    /// <summary>
    /// A generator-driven mapped resource: attribute-decorated <c>partial</c> properties whose
    /// bodies (and the PropertyMapping fields + GetTypes) are emitted by the source generator.
    /// No cilg weaving is involved.
    /// </summary>
    [RdfClass("http://example.org/test/Person")]
    public partial class Person : Resource
    {
        public Person(Uri uri) : base(uri) { }

        public Person(string uri) : base(uri) { }

        [RdfProperty("http://example.org/test/name")]
        public partial string Name { get; set; }

        [RdfProperty("http://example.org/test/age")]
        public partial int Age { get; set; }

        [RdfProperty("http://example.org/test/knows")]
        public partial List<Person> Knows { get; set; }
    }

    [TestFixture]
    public class GeneratorMappingTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            MappingDiscovery.RegisterAssembly(typeof(Person).Assembly);
        }

        [Test]
        public void GeneratedAccessorsRoundtripInMemory()
        {
            var person = new Person(new Uri("http://example.org/test/alice"));

            person.Name = "Alice";
            person.Age = 42;

            Assert.AreEqual("Alice", person.Name);
            Assert.AreEqual(42, person.Age);

            // Collection-typed mapping is initialized with a usable default instance.
            Assert.IsNotNull(person.Knows);
            person.Knows.Add(new Person(new Uri("http://example.org/test/bob")));
            Assert.AreEqual(1, person.Knows.Count);
        }

        [Test]
        public void GetTypesReturnsTheRdfClass()
        {
            var person = new Person(new Uri("http://example.org/test/alice"));

            Assert.IsTrue(person.GetTypes().Any(c => c.Uri.OriginalString == "http://example.org/test/Person"));
        }

        [Test]
        public void MappingIsDiscoveredAndListed()
        {
            // Proves InitializePropertyMappings discovered the generated (protected) mapping field
            // and SetValue routed the value into it.
            var person = new Person(new Uri("http://example.org/test/alice"));
            person.Name = "Alice";

            var values = person.ListValues().ToList();

            Assert.IsTrue(values.Any(v => (v.Item2 as string) == "Alice"));
        }

        [Test]
        public void PersistsAndReloadsViaMemoryStore()
        {
            var store = StoreFactory.CreateStore("provider=dotnetrdf");
            var model = store.GetModel(new Uri("http://example.org/test/model"));
            model.Clear();

            var uri = new Uri("http://example.org/test/carol");

            var person = model.CreateResource<Person>(uri);
            person.Name = "Carol";
            person.Age = 7;
            person.Commit();

            var reloaded = model.GetResource<Person>(uri);

            Assert.AreEqual("Carol", reloaded.Name);
            Assert.AreEqual(7, reloaded.Age);
        }
    }
}
