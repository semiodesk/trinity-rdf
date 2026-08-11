using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace Semiodesk.Trinity.Generator.Tests
{
    /// <summary>
    /// Covers TRIN006, which reports a mapped URI belonging to a generated vocabulary that is not one of
    /// its terms.
    ///
    /// The negative cases matter more than the positive ones. The rule reads a set of known URIs and judges
    /// everything else wrong, so one false positive on legitimate code gets it suppressed — and suppressing
    /// the category takes TRIN001-005 with it.
    /// </summary>
    [TestFixture]
    public class VocabularyUriAnalyzerTest
    {
        #region Members

        /// <summary>A vocabulary as trinity-vocab emits it: marked generated, terms as const strings.</summary>
        private const string GeneratedFoaf = @"
            [System.CodeDom.Compiler.GeneratedCode(""trinity-vocab"", ""2.0"")]
            public static class FOAF
            {
                public const string Namespace = ""http://xmlns.com/foaf/0.1/"";
                public const string Prefix = ""foaf"";
                public const string name = ""http://xmlns.com/foaf/0.1/name"";
                public const string Person = ""http://xmlns.com/foaf/0.1/Person"";
            }";

        /// <summary>The same shape but hand-written: no marker, and in practice an incomplete term list.</summary>
        private const string HandWrittenMusic = @"
            public static class MUSIC
            {
                public const string Namespace = ""http://example.org/music/"";
                public const string Prefix = ""music"";
                public const string title = ""http://example.org/music/title"";
            }";

        #endregion

        #region Methods

        [Test]
        public void ReportsAMistypedPropertyUri()
        {
            var diagnostics = Run(GeneratedFoaf, @"
                [RdfClass(""http://xmlns.com/foaf/0.1/Person"")]
                public partial class Person : Resource
                {
                    public Person(Uri uri) : base(uri) { }

                    [RdfProperty(""http://xmlns.com/foaf/0.1/nmae"")]
                    public string Name { get; set; }
                }");

            Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
            Assert.AreEqual("TRIN006", diagnostics[0].Id);
            Assert.That(diagnostics[0].GetMessage(), Does.Contain("nmae").And.Contain("foaf"));
        }

        [Test]
        public void ReportsAMistypedClassUri()
        {
            var diagnostics = Run(GeneratedFoaf, @"
                [RdfClass(""http://xmlns.com/foaf/0.1/Persson"")]
                public partial class Person : Resource
                {
                    public Person(Uri uri) : base(uri) { }
                }");

            Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
            Assert.AreEqual("TRIN006", diagnostics[0].Id);
        }

        [Test]
        public void StaysSilentWhenEveryUriIsAKnownTerm()
        {
            var diagnostics = Run(GeneratedFoaf, @"
                [RdfClass(""http://xmlns.com/foaf/0.1/Person"")]
                public partial class Person : Resource
                {
                    public Person(Uri uri) : base(uri) { }

                    [RdfProperty(""http://xmlns.com/foaf/0.1/name"")]
                    public string Name { get; set; }
                }");

            Assert.IsEmpty(diagnostics, Describe(diagnostics));
        }

        /// <summary>
        /// A URI from a vocabulary the compilation knows nothing about must never be reported: there is no
        /// basis for calling it wrong.
        /// </summary>
        [Test]
        public void StaysSilentForAnUnknownNamespace()
        {
            var diagnostics = Run(GeneratedFoaf, @"
                [RdfClass(""http://example.org/custom/Thing"")]
                public partial class Thing : Resource
                {
                    public Thing(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/custom/anything"")]
                    public string Anything { get; set; }
                }");

            Assert.IsEmpty(diagnostics, Describe(diagnostics));
        }

        /// <summary>
        /// The critical false-positive case. A hand-written vocabulary is typically partial, so its term
        /// list is not authoritative — and both external consumers hand-write vocabularies today. Treating
        /// one as complete would flag their correct URIs.
        /// </summary>
        [Test]
        public void StaysSilentForAHandWrittenVocabulary()
        {
            var diagnostics = Run(HandWrittenMusic, @"
                [RdfClass(""http://example.org/music/Album"")]
                public partial class Album : Resource
                {
                    public Album(Uri uri) : base(uri) { }

                    [RdfProperty(""http://example.org/music/releasedOn"")]
                    public string ReleasedOn { get; set; }
                }");

            Assert.IsEmpty(diagnostics,
                "A vocabulary without the trinity-vocab marker is not authoritative: " + Describe(diagnostics));
        }

        /// <summary>
        /// With no generated vocabulary present the analyzer must do nothing, which is the state of every
        /// project that has not adopted the tool.
        /// </summary>
        [Test]
        public void StaysSilentWhenNoVocabularyIsGenerated()
        {
            var diagnostics = Run(string.Empty, @"
                [RdfClass(""http://xmlns.com/foaf/0.1/Persson"")]
                public partial class Person : Resource
                {
                    public Person(Uri uri) : base(uri) { }
                }");

            Assert.IsEmpty(diagnostics, Describe(diagnostics));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Composes a compilation unit. Usings have to lead the file, so the parts are assembled here
        /// rather than concatenated by the callers.
        /// </summary>
        private static ImmutableArray<Diagnostic> Run(string vocabulary, string mappedType)
        {
            string source = "using System;\nusing Semiodesk.Trinity;\n" + vocabulary + "\n" + mappedType;

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

            CSharpCompilation compilation = CSharpCompilation.Create(
                "VocabularyUriAnalyzerTest",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // A source that does not bind would make every "stays silent" assertion pass for the wrong
            // reason, so the fixture fails loudly instead.
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (errors.Count > 0)
            {
                Assert.Fail("Test source did not compile:\n" + string.Join("\n", errors));
            }

            return compilation
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new VocabularyUriAnalyzer()))
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult();
        }

        private static string Describe(ImmutableArray<Diagnostic> diagnostics) =>
            diagnostics.Length == 0
                ? "no diagnostics"
                : string.Join("; ", diagnostics.Select(d => d.Id + ": " + d.GetMessage()));

        #endregion
    }
}
