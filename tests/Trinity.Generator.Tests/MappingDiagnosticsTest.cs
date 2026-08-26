using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Semiodesk.Trinity.Generator.Tests
{
    /// <summary>
    /// Covers the diagnostics the mapping generator reports.
    ///
    /// Each case is an attribute that looks right, compiles, and silently produces no mapping — the
    /// failure would otherwise only surface at runtime as a query returning nothing. The generator is
    /// driven in-process here because that is the only way to observe what it reports; the other tests
    /// in this project compile against it and can only see what it emits.
    /// </summary>
    [TestFixture]
    public class MappingDiagnosticsTest
    {
        #region Methods

        [Test]
        public void ReportsPropertyThatIsNotPartial()
        {
            var diagnostics = Run(@"
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public partial class Thing : Resource
                {
                    public Thing(System.Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/name"")]
                    public string Name { get; set; }
                }");

            Assert.AreEqual("TRIN001", SingleId(diagnostics));
            Assert.That(Message(diagnostics), Does.Contain("Name").And.Contain("partial"));
        }

        [Test]
        public void ReportsClassThatIsNotPartial()
        {
            var diagnostics = Run(@"
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public class Thing : Resource
                {
                    public Thing(System.Uri uri) : base(uri) { }
                }");

            Assert.AreEqual("TRIN002", SingleId(diagnostics));
            Assert.That(Message(diagnostics), Does.Contain("Thing").And.Contain("GetTypes"));
        }

        /// <summary>
        /// The migration case: nothing is partial yet. Reported during a 1.x → 2.0 migration of a 163-file
        /// model, where a silent build wrote every resource untyped and 837 tests failed with queries
        /// returning nothing. The class-level diagnostic must not depend on the properties being fixed
        /// first, because on the first build of a migration nothing has been converted at all.
        /// </summary>
        [Test]
        public void ReportsTheClassEvenWhenItsPropertiesAreAlsoNotPartial()
        {
            var diagnostics = Run(@"
                using System;
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Person"")]
                public class Person : Resource
                {
                    public Person(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/name"")]
                    public string Name { get; set; }

                    [RdfProperty(""http://example.org/age"")]
                    public int Age { get; set; }
                }");

            Assert.AreEqual(1, diagnostics.Count(d => d.Id == "TRIN002"),
                "The class must be reported exactly once: " + Describe(diagnostics));
            Assert.AreEqual(2, diagnostics.Count(d => d.Id == "TRIN001"),
                "Each unconverted property must still be reported: " + Describe(diagnostics));
        }

        /// <summary>
        /// A class with mapped properties but no <c>[RdfClass]</c> of its own — the GetTypes-only subclass
        /// pattern — must still be told it needs to be partial, and only once however many properties it
        /// has.
        /// </summary>
        [Test]
        public void ReportsAClassWithMappedPropertiesButNoRdfClass()
        {
            var diagnostics = Run(@"
                using System;
                using Semiodesk.Trinity;

                public class Employee : Resource
                {
                    public Employee(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/salary"")]
                    public int Salary { get; set; }

                    [RdfProperty(""http://example.org/title"")]
                    public string Title { get; set; }
                }");

            Assert.AreEqual(1, diagnostics.Count(d => d.Id == "TRIN002"),
                "The class must be reported once, not once per property: " + Describe(diagnostics));
        }

        /// <summary>
        /// The checks are independent, so a class that is both non-partial and missing its Uri constructor
        /// reports both rather than surfacing one problem per build.
        /// </summary>
        [Test]
        public void ReportsEveryApplicableClassProblemAtOnce()
        {
            var diagnostics = Run(@"
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public class Thing : Resource
                {
                    public Thing(string uri) : base(uri) { }
                }");

            var ids = diagnostics.Select(d => d.Id).OrderBy(id => id).ToList();

            Assert.Contains("TRIN002", ids, "not partial: " + Describe(diagnostics));
            Assert.Contains("TRIN005", ids, "no Uri constructor: " + Describe(diagnostics));
        }

        /// <summary>
        /// The regression guard for the previous behaviour: once the class is partial, an unconverted
        /// property is still reported and the class is not.
        /// </summary>
        [Test]
        public void ReportsOnlyThePropertyWhenTheClassIsAlreadyPartial()
        {
            var diagnostics = Run(@"
                using System;
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Person"")]
                public partial class Person : Resource
                {
                    public Person(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/name"")]
                    public string Name { get; set; }
                }");

            Assert.AreEqual("TRIN001", SingleId(diagnostics));
        }

        [Test]
        public void ReportsNestedMappedType()
        {
            var diagnostics = Run(@"
                using Semiodesk.Trinity;

                public class Outer
                {
                    [RdfClass(""http://example.org/Thing"")]
                    public partial class Thing : Resource
                    {
                        public Thing(System.Uri uri) : base(uri) { }
                    }
                }");

            Assert.AreEqual("TRIN003", SingleId(diagnostics));
        }

        [Test]
        public void ReportsMappedClassThatIsNotAResource()
        {
            var diagnostics = Run(@"
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public partial class Thing
                {
                    public Thing(System.Uri uri) { }
                }");

            Assert.AreEqual("TRIN004", SingleId(diagnostics));
        }

        [Test]
        public void ReportsMappedClassWithoutAUriConstructor()
        {
            var diagnostics = Run(@"
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public partial class Thing : Resource
                {
                    public Thing(string uri) : base(uri) { }
                }");

            Assert.AreEqual("TRIN005", SingleId(diagnostics));
        }

        /// <summary>
        /// The case that matters most: a correctly authored class must stay silent, or the diagnostics
        /// are noise and get suppressed wholesale.
        /// </summary>
        [Test]
        public void ReportsNothingForACorrectlyAuthoredClass()
        {
            var diagnostics = Run(@"
                using System;
                using System.Collections.Generic;
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public partial class Thing : Resource
                {
                    public Thing(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/name"")]
                    public partial string Name { get; set; }

                    [RdfProperty(""http://example.org/related"")]
                    public partial List<Thing> Related { get; set; }

                    [RdfProperty(""http://example.org/homepage"")]
                    public partial UriRef Homepage { get; set; }

                    [RdfProperty(""http://example.org/seeAlso"")]
                    public partial List<UriRef> SeeAlso { get; set; }
                }");

            Assert.IsEmpty(diagnostics, "A correctly authored mapped class must produce no diagnostics.");
        }

        /// <summary>
        /// System.Uri.Equals ignores the fragment, so a Uri-typed mapping conflates resources that RDF
        /// treats as distinct. Unlike the other property diagnostics this one does not suppress the
        /// mapping -- the generated code is correct, the declared type is not.
        /// </summary>
        [Test]
        public void ReportsMappedPropertyTypedAsRawUri()
        {
            var diagnostics = Run(@"
                using System;
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public partial class Thing : Resource
                {
                    public Thing(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/homepage"")]
                    public partial Uri Homepage { get; set; }
                }");

            Assert.AreEqual("TRIN007", SingleId(diagnostics));
            Assert.That(Message(diagnostics), Does.Contain("Homepage").And.Contain("UriRef"));
        }

        /// <summary>
        /// The element type is what gets stored, so a collection of raw URIs has exactly the same defect.
        /// </summary>
        [Test]
        public void ReportsMappedCollectionOfRawUri()
        {
            var diagnostics = Run(@"
                using System;
                using System.Collections.Generic;
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public partial class Thing : Resource
                {
                    public Thing(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/seeAlso"")]
                    public partial List<Uri> SeeAlso { get; set; }
                }");

            Assert.AreEqual("TRIN007", SingleId(diagnostics));
            Assert.That(Message(diagnostics), Does.Contain("SeeAlso").And.Contain("UriRef"));
        }

        /// <summary>
        /// TRIN007 must not suppress emission: the mapping it warns about is still generated, so a
        /// consumer who ignores the warning gets working code (and the runtime check in
        /// PropertyMapping&lt;T&gt;), not a silently unmapped property.
        /// </summary>
        [Test]
        public void StillGeneratesTheMappingForARawUriProperty()
        {
            var source = @"
                using System;
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Thing"")]
                public partial class Thing : Resource
                {
                    public Thing(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/homepage"")]
                    public partial Uri Homepage { get; set; }
                }";

            var generated = Generated(source);

            Assert.That(generated, Does.Contain("PropertyMapping<global::System.Uri>"),
                "TRIN007 must warn about the declared type without suppressing the mapping.");
            Assert.That(generated, Does.Contain("\"http://example.org/homepage\""));
        }

        /// <summary>
        /// An abstract base is never materialized directly, so requiring a Uri constructor on it would be
        /// a false positive.
        /// </summary>
        [Test]
        public void ReportsNothingForAnAbstractMappedClass()
        {
            var diagnostics = Run(@"
                using System;
                using Semiodesk.Trinity;

                [RdfClass(""http://example.org/Base"")]
                public abstract partial class Base : Resource
                {
                    protected Base(string uri) : base(uri) { }
                }");

            Assert.IsEmpty(diagnostics, "An abstract mapped class is never constructed directly.");
        }

        #endregion

        #region Helpers

        private static ImmutableArray<Diagnostic> Run(string source) => Drive(source).Diagnostics;

        /// <summary>
        /// The source the generator emitted for <paramref name="source"/>, concatenated.
        /// </summary>
        private static string Generated(string source) =>
            string.Join(
                Environment.NewLine,
                Drive(source).Compilation.SyntaxTrees.Skip(1).Select(t => t.ToString()));

        private static (ImmutableArray<Diagnostic> Diagnostics, Compilation Compilation) Drive(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source,
                new CSharpParseOptions(LanguageVersion.Latest));

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

            var compilation = CSharpCompilation.Create(
                "MappingDiagnosticsTest",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            CSharpGeneratorDriver
                .Create(new MappingSourceGenerator())
                .RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);

            return (diagnostics, updated);
        }

        private static string SingleId(ImmutableArray<Diagnostic> diagnostics)
        {
            Assert.AreEqual(1, diagnostics.Length,
                "Expected exactly one diagnostic but got: " + string.Join(", ", diagnostics.Select(d => d.Id)));

            return diagnostics[0].Id;
        }

        private static string Describe(ImmutableArray<Diagnostic> diagnostics) =>
            diagnostics.Length == 0
                ? "no diagnostics"
                : string.Join("; ", diagnostics.Select(d => d.Id + " " + d.GetMessage()));

        private static string Message(ImmutableArray<Diagnostic> diagnostics) =>
            diagnostics[0].GetMessage();

        #endregion
    }
}
