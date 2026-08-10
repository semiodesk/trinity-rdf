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
                }");

            Assert.IsEmpty(diagnostics, "A correctly authored mapped class must produce no diagnostics.");
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

        private static ImmutableArray<Diagnostic> Run(string source)
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
                .RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

            return diagnostics;
        }

        private static string SingleId(ImmutableArray<Diagnostic> diagnostics)
        {
            Assert.AreEqual(1, diagnostics.Length,
                "Expected exactly one diagnostic but got: " + string.Join(", ", diagnostics.Select(d => d.Id)));

            return diagnostics[0].Id;
        }

        private static string Message(ImmutableArray<Diagnostic> diagnostics) =>
            diagnostics[0].GetMessage();

        #endregion
    }
}
