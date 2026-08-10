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
// Copyright (c) Semiodesk GmbH

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Semiodesk.Trinity.Vocabulary.Tests
{
    /// <summary>
    /// Covers the vocabulary generator against the repository's real ontologies.
    /// </summary>
    [TestFixture]
    public class VocabularyGeneratorTest
    {
        #region Members

        private const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        private VocabularyGenerator _generator;
        private string _ontologies;
        private string _scratch;

        #endregion

        #region Setup

        [SetUp]
        public void SetUp()
        {
            _generator = new VocabularyGenerator();
            _ontologies = Path.Combine(TestContext.CurrentContext.TestDirectory, "Ontologies");
            _scratch = Path.Combine(Path.GetTempPath(), "trinity-vocab-tests", Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_scratch);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_scratch))
            {
                Directory.Delete(_scratch, true);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The three term categories must be classified as 1.x did, since existing code depends on the
        /// distinction: a Property is not interchangeable with a Class.
        /// </summary>
        [Test]
        public void ClassifiesTermsIntoClassPropertyAndResource()
        {
            IReadOnlyList<VocabularyTerm> terms = _generator.ReadTerms(Rdf());

            Assert.AreEqual("Property", Kind(terms, "type"), "rdf:type is a property.");
            Assert.AreEqual("Class", Kind(terms, "List"), "rdf:List is a class.");
            // Neither a class nor a property: the fallback category, which 1.x also emitted.
            Assert.AreEqual("Resource", Kind(terms, "nil"), "rdf:nil is an individual.");
        }

        /// <summary>
        /// Every term the hand-written vocabulary exposes must still be generated, with the same name and
        /// the same kind. Extra terms are acceptable — the checked-in file predates RDF 1.1 datatypes —
        /// but a rename or a changed kind would silently break consumers.
        /// </summary>
        [Test]
        public void ReproducesEveryTermOfTheHandWrittenVocabulary()
        {
            // Taken from Trinity.Tests/Ontologies.cs, which the retired 1.x generator produced. Note
            // '_object': rdf:object collides with a C# keyword and the underscore prefix is part of the
            // public surface consumers already compile against.
            var expected = new Dictionary<string, string>
            {
                { "type", "Property" }, { "first", "Property" }, { "rest", "Property" },
                { "subject", "Property" }, { "predicate", "Property" }, { "_object", "Property" },
                { "value", "Property" },
                { "Alt", "Class" }, { "Bag", "Class" }, { "List", "Class" }, { "Property", "Class" },
                { "Seq", "Class" }, { "Statement", "Class" },
                { "nil", "Resource" }, { "XMLLiteral", "Resource" }
            };

            IReadOnlyList<VocabularyTerm> terms = _generator.ReadTerms(Rdf());

            foreach (var entry in expected)
            {
                Assert.AreEqual(entry.Value, Kind(terms, entry.Key),
                    $"rdf:{entry.Key} must still be generated as {entry.Value}.");
            }
        }

        /// <summary>
        /// Output feeds a committed file and a --check comparison, so it has to be stable run to run.
        /// </summary>
        [Test]
        public void OutputIsDeterministic()
        {
            var document = Document(Rdf());

            Assert.AreEqual(_generator.Generate(document), _generator.Generate(document),
                "Two runs over the same input must produce identical source.");
        }

        /// <summary>
        /// The corpus deliberately contains filenames with spaces; a generator that builds URIs from paths
        /// tends to break on them.
        /// </summary>
        [Test]
        public void ReadsVocabulariesFromPathsContainingSpaces()
        {
            string path = Path.Combine(_ontologies, "space test ontology.ttl");

            Assume.That(File.Exists(path), $"Corpus file missing: {path}");

            Assert.DoesNotThrow(() => _generator.ReadTerms(new VocabularySource
            {
                File = path,
                Prefix = "spacetest",
                Uri = new Uri("http://www.semiodesk.com/space/")
            }));
        }

        /// <summary>
        /// The 1.x generator wrote rdfs:comment into doc comments unescaped, so a comment containing
        /// markup produced a malformed XML doc — and sometimes uncompilable source.
        /// </summary>
        [Test]
        public void EscapesMarkupInDocComments()
        {
            string file = WriteTurtle("escaping.ttl", @"
                @prefix ex: <http://example.org/> .
                @prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
                @prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
                ex:thing a rdf:Property ; rdfs:comment ""Use <b>a & b</b> carefully"" .");

            string generated = _generator.Generate(Document(new VocabularySource
            {
                File = file, Prefix = "ex", Uri = new Uri("http://example.org/")
            }));

            Assert.That(generated, Does.Contain("&lt;b&gt;"), "Markup must be escaped in the doc comment.");
            Assert.That(generated, Does.Contain("&amp;"), "Ampersands must be escaped in the doc comment.");
            Assert.That(generated, Does.Not.Contain("<b>a & b</b>"), "The raw comment must not be emitted.");
        }

        /// <summary>
        /// Terms whose local names are C# keywords or start with a digit have to be renamed. The
        /// underscore prefix is inherited from 1.x deliberately — switching to '@' would rename public API.
        /// </summary>
        [Test]
        public void SanitizesKeywordsAndLeadingDigits()
        {
            string file = WriteTurtle("names.ttl", @"
                @prefix ex: <http://example.org/> .
                @prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
                ex:class a rdf:Property .
                ex:2fast a rdf:Property .
                ex:with-dash a rdf:Property .");

            IReadOnlyList<VocabularyTerm> terms = _generator.ReadTerms(new VocabularySource
            {
                File = file, Prefix = "ex", Uri = new Uri("http://example.org/")
            });

            var names = terms.Select(t => t.Name).ToList();

            Assert.Contains("_class", names, "A C# keyword must be prefixed with an underscore.");
            Assert.Contains("_2fast", names, "A leading digit must be prefixed with an underscore.");
            Assert.Contains("with_dash", names, "A dash is not legal in an identifier.");
        }

        /// <summary>
        /// The load-bearing test. OntologyDiscovery finds vocabularies purely by reflection — a class
        /// deriving directly from Ontology, with a parameterless constructor and static Prefix/Namespace
        /// fields. Nothing enforces that at compile time, so this compiles the generated source and checks
        /// Trinity actually discovers it.
        /// </summary>
        [Test]
        public void GeneratedSourceIsDiscoveredByOntologyDiscovery()
        {
            string generated = _generator.Generate(Document(Rdf()));
            Assembly assembly = Compile(generated);

            OntologyDiscovery.AddAssembly(assembly);

            Assert.IsTrue(OntologyDiscovery.Namespaces.ContainsKey("rdf"),
                "The generated vocabulary must register its prefix with OntologyDiscovery.");
            Assert.AreEqual(RdfNamespace, OntologyDiscovery.Namespaces["rdf"].OriginalString,
                "The registered namespace must match the vocabulary.");
        }

        #endregion

        #region Helpers

        private VocabularySource Rdf() => new VocabularySource
        {
            File = Path.Combine(_ontologies, "rdf.rdf"),
            Prefix = "rdf",
            Uri = new Uri(RdfNamespace)
        };

        private static VocabularyDocument Document(VocabularySource vocabulary) => new VocabularyDocument
        {
            Namespace = "Generated.Vocabularies",
            Output = "Vocabularies.g.cs",
            Vocabularies = new List<VocabularySource> { vocabulary }
        };

        private static string Kind(IReadOnlyList<VocabularyTerm> terms, string name) =>
            terms.FirstOrDefault(t => t.Name == name)?.Kind;

        private string WriteTurtle(string name, string content)
        {
            string path = Path.Combine(_scratch, name);

            File.WriteAllText(path, content);

            return path;
        }

        /// <summary>
        /// Compiles generated source into a real assembly, failing the test with the compiler's own
        /// diagnostics if the emission is not valid C#.
        /// </summary>
        private static Assembly Compile(string source)
        {
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

            CSharpCompilation compilation = CSharpCompilation.Create(
                "GeneratedVocabularies_" + Guid.NewGuid().ToString("N"),
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var stream = new MemoryStream();

            Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);

            if (!result.Success)
            {
                Assert.Fail("Generated source did not compile:\n" + string.Join("\n",
                    result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
            }

            return Assembly.Load(stream.ToArray());
        }

        #endregion
    }
}
