using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semiodesk.Trinity.Tests
{
    public class MappingTestOntology
    {
        public static readonly Uri Namespace = new Uri("semio:mapping:test");

        public static readonly string Prefix = "mapping";

        public Uri GetNamespace() { return Namespace; }

        public string GetPrefix() { return Prefix; }

        public static readonly Class BaseClass = new Class(new UriRef(BaseClassString));
        public const string BaseClassString = "semio:mapping:test:baseclass";

        public static readonly Class SubClass = new Class(new UriRef(SubClassString));
        public const string SubClassString = "semio:mapping:test:subclass";

        public static readonly Class SubSubClass = new Class(new UriRef(SubSubClassString));
        public const string SubSubClassString = "semio:mapping:test:subsubclass";

        public static readonly Class AnotherBaseClass = new Class(new UriRef(AnotherBaseClassString));
        public const string AnotherBaseClassString = "semio:mapping:test:anotherBaseClass";
    }

    public class BaseClass : Resource
    {
        public BaseClass(UriRef uri) : base(uri) { }

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { MappingTestOntology.BaseClass };
        }
    }

    public class SubClass : BaseClass
    {
        public SubClass(UriRef uri) : base(uri) { }

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { MappingTestOntology.SubClass };
        }
    }

    public class SubSubClass : SubClass
    {
        public SubSubClass(UriRef uri) : base(uri) { }

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { MappingTestOntology.SubSubClass };
        }
    }

    public class AnotherBaseClas : Resource
    {
        public AnotherBaseClas(UriRef uri) : base(uri) { }

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { MappingTestOntology.BaseClass, MappingTestOntology.AnotherBaseClass };
        }
    }

    /// <summary>
    /// This class should test more, if we don't derive from SetupClass we can compare before and after discovery also.
    /// </summary>
    [TestFixture]
    public class MappingDiscoveryTest : SetupClass
    {
        [SetUp]
        public void SetUp()
        {
        }

        /// <summary>
        /// A class that fails to register must not take the rest of the batch down with it. It used to:
        /// AddMappingClass rethrows, the loop had no guard, and every class ordered after the offender
        /// stayed unregistered -- which raises no error at all, it just makes queries return base
        /// Resource instances. Which classes survived depended on Assembly.GetTypes() ordering.
        ///
        /// This became reachable when PropertyMapping&lt;T&gt; started refusing System.Uri: the throw
        /// fires from an instance field initializer, so a single un-migrated mapping anywhere in an
        /// assembly silently truncated its registration, and TRIN007 being only a warning meant the
        /// build still succeeded.
        /// </summary>
        [Test]
        public void ReportsEveryFailingClassRatherThanStoppingAtTheFirst()
        {
            // Neither is a Resource, so each fails in AddMappingClass; neither is picked up by an
            // assembly scan, so nothing else in this suite is affected.
            var error = Assert.Throws<AggregateException>(
                () => MappingDiscovery.AddMappingClasses(new List<Type> { typeof(string), typeof(int) }));

            Assert.AreEqual(2, error.InnerExceptions.Count,
                "The loop must attempt every class; stopping at the first hides the rest.");
            Assert.That(error.InnerExceptions.Select(e => e.Message),
                Has.Some.Contains("String").And.Some.Contains("Int32"));
        }

        [Test]
        public void TestGetRdfClasses()
        {
            List<Class> classTypes = MappingDiscovery.GetRdfClasses(typeof(BaseClass)).ToList();
            Assert.AreEqual(1, classTypes.Count());
            Assert.Contains(MappingTestOntology.BaseClass, classTypes);

            classTypes = MappingDiscovery.GetRdfClasses(typeof(SubClass)).ToList();
            Assert.AreEqual(1, classTypes.Count());
            Assert.Contains(MappingTestOntology.SubClass, classTypes);

            classTypes = MappingDiscovery.GetRdfClasses(typeof(AnotherBaseClas)).ToList();
            Assert.AreEqual(2, classTypes.Count());
            Assert.Contains(MappingTestOntology.BaseClass, classTypes);
            Assert.Contains(MappingTestOntology.BaseClass, classTypes);
        }

        [Test]
        public void TestGetBaseClasses()
        {
            List<Class> baseTypes = new List<Class>();
            MappingDiscovery.GetBaseTypes(typeof(BaseClass), ref baseTypes);
            Assert.AreEqual(0, baseTypes.Count());

            baseTypes = new List<Class>();
            MappingDiscovery.GetBaseTypes(typeof(SubClass), ref baseTypes);
            Assert.AreEqual(1, baseTypes.Count());
            Assert.Contains(MappingTestOntology.BaseClass, baseTypes);

            baseTypes = new List<Class>();
            MappingDiscovery.GetBaseTypes(typeof(SubSubClass), ref baseTypes);
            Assert.AreEqual(2, baseTypes.Count());
            Assert.Contains(MappingTestOntology.BaseClass, baseTypes);
            Assert.Contains(MappingTestOntology.SubClass, baseTypes);
        }

        [Test]
        public void TestGetMatchingTypes()
        {
            Class[] types = new[] { MappingTestOntology.BaseClass };
            var res = MappingDiscovery.GetMatchingTypes(types, typeof(BaseClass), false);

            Assert.AreEqual(1, res.Length);
            Assert.AreEqual(typeof(BaseClass), res.First());

            types = new[] { MappingTestOntology.BaseClass, MappingTestOntology.SubClass };
            res = MappingDiscovery.GetMatchingTypes(types, typeof(BaseClass), true);

            //Assert.AreEqual(1, res.Length);
            Assert.AreEqual(typeof(SubClass), res.First());

            types = new[] { MappingTestOntology.SubClass, MappingTestOntology.BaseClass };
            res = MappingDiscovery.GetMatchingTypes(types, typeof(BaseClass), true);

            //Assert.AreEqual(1, res.Length);
            Assert.AreEqual(typeof(SubClass), res.First());
        }
    }
}
