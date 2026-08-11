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
using System.Text.RegularExpressions;
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
        /// All four RDF formats in the corpus. TriG matters most: it carries named graphs, so it takes the
        /// store-parser branch rather than the single-graph one, and nothing else exercises that path.
        /// </summary>
        [TestCase("rdf.rdf", "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#", TestName = "RDF/XML")]
        [TestCase("rdfs.n3", "rdfs", "http://www.w3.org/2000/01/rdf-schema#", TestName = "N3")]
        [TestCase("owl.n3", "owl", "http://www.w3.org/2002/07/owl#", TestName = "N3 (owl)")]
        [TestCase("dces.ttl", "dces", "http://purl.org/dc/elements/1.1/", TestName = "Turtle")]
        [TestCase("nco.trig", "nco", "http://www.semanticdesktop.org/ontologies/2007/03/22/nco#", TestName = "TriG")]
        [TestCase("foaf.rdf", "foaf", "http://xmlns.com/foaf/0.1/", TestName = "RDF/XML (foaf)")]
        public void ReadsEveryFormatInTheCorpus(string file, string prefix, string uri)
        {
            string path = Path.Combine(_ontologies, file);

            Assume.That(File.Exists(path), $"Corpus file missing: {path}");

            var vocabulary = new VocabularySource { File = path, Prefix = prefix, Uri = new Uri(uri) };

            IReadOnlyList<VocabularyTerm> terms = _generator.ReadTerms(vocabulary);

            Assert.IsNotEmpty(terms, $"{file} produced no terms, so the format was not parsed.");
            Assert.IsTrue(terms.All(t => !string.IsNullOrEmpty(t.Name)), "Every term must be nameable.");
            Assert.IsTrue(terms.Any(t => t.Uri.OriginalString.StartsWith(uri, StringComparison.Ordinal)),
                "At least some terms must come from the vocabulary's own namespace.");
        }

        /// <summary>
        /// The compatibility acceptance: every member of a committed vocabulary class must still be
        /// generated, with the same name and the same kind.
        /// </summary>
        /// <remarks>
        /// The committed classes were produced by the retired 1.x generator and are public API that
        /// consumers compile against, so a rename or a changed kind is a silent break. Extra terms are
        /// fine — the checked-in files predate RDF 1.1 — which is why this asserts a superset rather than
        /// equality. The committed file is read as text rather than by referencing the test assembly, so
        /// this stays independent of Trinity.Tests.
        /// </remarks>
        [TestCase("rdf", "rdf.rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
        [TestCase("rdfs", "rdfs.n3", "http://www.w3.org/2000/01/rdf-schema#")]
        [TestCase("nco", "nco.trig", "http://www.semanticdesktop.org/ontologies/2007/03/22/nco#")]
        public void GeneratesEveryMemberOfTheCommittedVocabulary(string prefix, string file, string uri)
        {
            var committed = ReadCommittedMembers(prefix);

            // Deliberately an assertion, not Assume: a missing or misparsed class would otherwise make
            // this test pass while comparing nothing, which is exactly how an 'owl' case hid here once.
            Assert.Greater(committed.Count, 0, $"No committed '{prefix}' class found to compare against.");

            IReadOnlyList<VocabularyTerm> terms = _generator.ReadTerms(new VocabularySource
            {
                File = Path.Combine(_ontologies, file),
                Prefix = prefix,
                Uri = new Uri(uri)
            });

            var generated = terms.ToDictionary(t => t.Name, t => t.Kind);
            var missing = new List<string>();

            foreach (var member in committed)
            {
                if (!generated.TryGetValue(member.Key, out string kind))
                {
                    missing.Add($"{member.Key} (missing)");
                }
                else if (kind != member.Value)
                {
                    missing.Add($"{member.Key} (was {member.Value}, now {kind})");
                }
            }

            Assert.IsEmpty(missing,
                $"The generated '{prefix}' vocabulary no longer matches the committed one: " +
                string.Join(", ", missing));
        }

        /// <summary>
        /// Two terms whose local names collide must both be emitted under distinct identifiers. The suffix
        /// stage is what makes that possible, and ordinal ordering is what makes the suffixes stable.
        /// </summary>
        [Test]
        public void ResolvesCollidingLocalNames()
        {
            string file = WriteTurtle("collisions.ttl", @"
                @prefix ex: <http://example.org/> .
                @prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
                ex:thing a rdf:Property .
                <http://example.org/nested/thing> a rdf:Property .
                <http://example.org/other/thing> a rdf:Property .");

            IReadOnlyList<VocabularyTerm> terms = _generator.ReadTerms(new VocabularySource
            {
                File = file, Prefix = "ex", Uri = new Uri("http://example.org/")
            });

            var names = terms.Select(t => t.Name).ToList();

            Assert.AreEqual(3, terms.Count, "Every colliding term must still be emitted.");
            Assert.AreEqual(3, names.Distinct().Count(), "Names must be unique: " + string.Join(", ", names));
        }

        /// <summary>
        /// A term named after the vocabulary class itself would not compile, so it must be renamed.
        /// </summary>
        [Test]
        public void RenamesATermThatCollidesWithTheVocabularyClass()
        {
            string file = WriteTurtle("selfname.ttl", @"
                @prefix ex: <http://example.org/> .
                @prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
                ex:ex a rdf:Property .");

            IReadOnlyList<VocabularyTerm> terms = _generator.ReadTerms(new VocabularySource
            {
                File = file, Prefix = "ex", Uri = new Uri("http://example.org/")
            });

            Assert.IsFalse(terms.Any(t => t.Name == "ex"),
                "A term may not take the name of the class that contains it.");
            Assert.AreEqual(1, terms.Count, "It must still be emitted, under another name.");
        }

        /// <summary>
        /// Output feeds a committed file and a check-mode comparison, so it has to be stable run to run.
        /// </summary>
        [Test]
        public void OutputIsDeterministic()
        {
            VocabularySource vocabulary = Rdf();

            Assert.AreEqual(_generator.GenerateFile(vocabulary, Ns), _generator.GenerateFile(vocabulary, Ns),
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

            string generated = _generator.GenerateFile(new VocabularySource
            {
                File = file, Prefix = "ex", Uri = new Uri("http://example.org/")
            }, Ns);

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
            string generated = _generator.GenerateFile(Rdf(), Ns);
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

        private const string Ns = "Generated.Vocabularies";

        private static string Kind(IReadOnlyList<VocabularyTerm> terms, string name) =>
            terms.FirstOrDefault(t => t.Name == name)?.Kind;

        /// <summary>
        /// Extracts <c>name → kind</c> for one vocabulary class from the committed source, read as text so
        /// this fixture does not have to reference the assembly that declares it.
        /// </summary>
        private static Dictionary<string, string> ReadCommittedMembers(string prefix)
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Committed", "Ontologies.cs");
            var result = new Dictionary<string, string>();

            if (!File.Exists(path))
            {
                return result;
            }

            string[] lines = File.ReadAllLines(path);
            var declaration = new Regex(@"^\s*public\s+class\s+(\w+)\s*:\s*Ontology\b");
            var member = new Regex(@"static\s+readonly\s+(Class|Property|Resource)\s+(\w+)\s*=");
            bool inside = false;

            foreach (string line in lines)
            {
                Match start = declaration.Match(line);

                if (start.Success)
                {
                    // The classes are siblings, so entering one leaves the previous.
                    inside = start.Groups[1].Value == prefix;

                    continue;
                }

                if (!inside)
                {
                    continue;
                }

                Match found = member.Match(line);

                if (found.Success)
                {
                    result[found.Groups[2].Value] = found.Groups[1].Value;
                }
            }

            return result;
        }

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
