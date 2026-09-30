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

    /// <summary>
    /// A mapped property that hides a member of <c>Resource</c>. <c>Resource.Model</c> is the graph the
    /// resource was read from; a domain model may legitimately mean something else by the name — a car
    /// has a model — and C# says to write <c>new</c>. That has to survive into the generated half or the
    /// build fails with CS8800, with no in-language escape: keeping <c>new</c> is an error, dropping it
    /// is a CS0108 warning whose advice is the thing that just failed.
    ///
    /// This class compiling at all is the regression test.
    /// </summary>
    /// <remarks>
    /// This used to hide <c>Resource.Language</c>, which was the sharper example while that member
    /// existed: hiding the name made the ambient language switch harder to reach without making it
    /// inactive. ADR-0048 removed the member, so the collision it guarded is gone — but the modifier
    /// round-trip it also guards is not, and <c>Model</c> is the same hazard under a name that still
    /// exists.
    /// </remarks>
    [RdfClass("http://example.org/test/Document")]
    public partial class HidingDocument : Resource
    {
        public HidingDocument(Uri uri) : base(uri) { }

        [RdfProperty("http://example.org/test/model")]
        public new partial string Model { get; set; }

        /// <summary>Non-public accessibility has to round-trip too, or it is CS8799.</summary>
        [RdfProperty("http://example.org/test/internalNote")]
        internal partial string InternalNote { get; set; }

        /// <summary>A virtual mapped property: the same CS8800 sentence as <c>new</c>.</summary>
        [RdfProperty("http://example.org/test/subject")]
        public virtual partial string Subject { get; set; }
    }

    [TestFixture]
    public class GeneratorMappingTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            MappingDiscovery.RegisterAssembly(typeof(Person).Assembly);
        }

        /// <summary>
        /// The hiding property has its own storage, distinct from the member it hides.
        /// </summary>
        [Test]
        public void HidingPropertyHasItsOwnStorage()
        {
            var document = new HidingDocument(new Uri("http://example.org/test/doc"));

            document.Model = "Type 2";

            Assert.AreEqual("Type 2", document.Model, "The mapped property holds its own value.");
            Assert.IsNull(((Resource)document).Model, "Writing the mapped property must not set Resource.Model.");
        }

        /// <summary>
        /// Modifiers other than <c>new</c> fall under the same CS8800 rule, and non-public accessibility
        /// under CS8799. Reaching these members at all proves the generated half agreed.
        /// </summary>
        [Test]
        public void CarriesAccessibilityAndVirtualOntoTheGeneratedHalf()
        {
            var document = new HidingDocument(new Uri("http://example.org/test/doc"));

            document.InternalNote = "note";
            document.Subject = "subject";

            Assert.AreEqual("note", document.InternalNote);
            Assert.AreEqual("subject", document.Subject);

            var property = typeof(HidingDocument).GetProperty("Subject");

            Assert.IsTrue(property!.GetMethod!.IsVirtual, "'virtual' must survive into the generated half.");
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
