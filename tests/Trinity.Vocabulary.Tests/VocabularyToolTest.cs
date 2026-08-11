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
using System.IO;
using NUnit.Framework;
using Semiodesk.Trinity.Vocabulary.Cli;

namespace Semiodesk.Trinity.Vocabulary.Tests
{
    /// <summary>
    /// Covers the tool around the engine: manifest reading, writing, and the check mode that is meant to
    /// gate CI.
    /// </summary>
    [TestFixture]
    public class VocabularyToolTest
    {
        #region Members

        private string _scratch;
        private string _ontologies;

        #endregion

        #region Setup

        [SetUp]
        public void SetUp()
        {
            _ontologies = Path.Combine(TestContext.CurrentContext.TestDirectory, "Ontologies");
            _scratch = Path.Combine(Path.GetTempPath(), "trinity-vocab-tool", Guid.NewGuid().ToString("N"));

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

        [Test]
        public void WritesTheFileTheManifestNames()
        {
            string manifest = WriteManifest("rdf.rdf", "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            Assert.AreEqual(VocabularyRunner.Success, Run(manifest, check: false));
            Assert.IsTrue(File.Exists(Path.Combine(_scratch, "Vocabularies.g.cs")));
        }

        /// <summary>
        /// The three check-mode outcomes. Exit code 3 is the whole point of the mode: it is what makes a
        /// stale committed vocabulary fail a build rather than pass unnoticed.
        /// </summary>
        [Test]
        public void CheckSucceedsWhenTheGeneratedFileIsCurrent()
        {
            string manifest = WriteManifest("rdf.rdf", "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            Run(manifest, check: false);

            Assert.AreEqual(VocabularyRunner.Success, Run(manifest, check: true));
        }

        [Test]
        public void CheckReportsOutOfDateWhenTheFileDiffers()
        {
            string manifest = WriteManifest("rdf.rdf", "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            Run(manifest, check: false);
            File.AppendAllText(Path.Combine(_scratch, "Vocabularies.g.cs"), "// edited by hand\n");

            Assert.AreEqual(VocabularyRunner.OutOfDate, Run(manifest, check: true));
        }

        [Test]
        public void CheckReportsOutOfDateWhenTheFileIsMissing()
        {
            string manifest = WriteManifest("rdf.rdf", "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            Assert.AreEqual(VocabularyRunner.OutOfDate, Run(manifest, check: true));
        }

        /// <summary>
        /// Line endings must not count as drift, or a checkout with normalized endings fails CI for no
        /// reason.
        /// </summary>
        [Test]
        public void CheckIgnoresLineEndingDifferences()
        {
            string manifest = WriteManifest("rdf.rdf", "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            string output = Path.Combine(_scratch, "Vocabularies.g.cs");

            Run(manifest, check: false);

            string text = File.ReadAllText(output);

            File.WriteAllText(output, text.Replace("\r\n", "\n").Replace("\n", "\r\n"));

            Assert.AreEqual(VocabularyRunner.Success, Run(manifest, check: true));
        }

        [Test]
        public void ReportsAMissingManifest()
        {
            Assert.AreEqual(VocabularyRunner.UsageError,
                Run(Path.Combine(_scratch, "does-not-exist.json"), check: false));
        }

        /// <summary>
        /// A manifest may be an array, so several files can be generated in one run.
        /// </summary>
        [Test]
        public void ReadsAnArrayOfDocuments()
        {
            string manifest = Path.Combine(_scratch, "vocabularies.json");

            File.WriteAllText(manifest, @"[
              { ""namespace"": ""A"", ""output"": ""A.g.cs"", ""vocabularies"": [
                  { ""file"": ""rdf.rdf"", ""prefix"": ""rdf"", ""uri"": ""http://www.w3.org/1999/02/22-rdf-syntax-ns#"" } ] },
              { ""namespace"": ""B"", ""output"": ""B.g.cs"", ""vocabularies"": [
                  { ""file"": ""rdfs.n3"", ""prefix"": ""rdfs"", ""uri"": ""http://www.w3.org/2000/01/rdf-schema#"" } ] }
            ]");

            CopyOntology("rdf.rdf");
            CopyOntology("rdfs.n3");

            Assert.AreEqual(VocabularyRunner.Success, Run(manifest, check: false));
            Assert.IsTrue(File.Exists(Path.Combine(_scratch, "A.g.cs")), "first document");
            Assert.IsTrue(File.Exists(Path.Combine(_scratch, "B.g.cs")), "second document");
        }

        [Test]
        public void RejectsAManifestEntryWithoutAnOutput()
        {
            string manifest = Path.Combine(_scratch, "vocabularies.json");

            File.WriteAllText(manifest, @"{ ""vocabularies"": [
                { ""file"": ""rdf.rdf"", ""prefix"": ""rdf"", ""uri"": ""http://example.org/"" } ] }");

            Assert.Throws<FormatException>(() => Run(manifest, check: false));
        }

        [Test]
        public void RejectsAVocabularyWithoutAPrefix()
        {
            string manifest = Path.Combine(_scratch, "vocabularies.json");

            File.WriteAllText(manifest, @"{ ""output"": ""X.g.cs"", ""vocabularies"": [
                { ""file"": ""rdf.rdf"", ""uri"": ""http://example.org/"" } ] }");

            Assert.Throws<FormatException>(() => Run(manifest, check: false));
        }

        /// <summary>
        /// Paths in the manifest resolve against the manifest, not the working directory, so a checked-in
        /// manifest behaves the same however the tool is invoked.
        /// </summary>
        [Test]
        public void ResolvesVocabularyPathsRelativeToTheManifest()
        {
            Directory.CreateDirectory(Path.Combine(_scratch, "vocab"));
            File.Copy(Path.Combine(_ontologies, "rdf.rdf"), Path.Combine(_scratch, "vocab", "rdf.rdf"));

            string manifest = Path.Combine(_scratch, "vocabularies.json");

            File.WriteAllText(manifest, @"{ ""output"": ""out/Vocabularies.g.cs"", ""vocabularies"": [
                { ""file"": ""vocab/rdf.rdf"", ""prefix"": ""rdf"",
                  ""uri"": ""http://www.w3.org/1999/02/22-rdf-syntax-ns#"" } ] }");

            string previous = Directory.GetCurrentDirectory();

            try
            {
                // Deliberately somewhere else, to prove the manifest's own directory is what counts.
                Directory.SetCurrentDirectory(Path.GetTempPath());

                Assert.AreEqual(VocabularyRunner.Success, Run(manifest, check: false));
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
            }

            Assert.IsTrue(File.Exists(Path.Combine(_scratch, "out", "Vocabularies.g.cs")),
                "The output path is relative to the manifest and its directory is created.");
        }

        #endregion

        #region Helpers

        private static int Run(string manifest, bool check)
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            return VocabularyRunner.Run(manifest, check, output, error);
        }

        private string WriteManifest(string file, string prefix, string uri)
        {
            CopyOntology(file);

            string manifest = Path.Combine(_scratch, "vocabularies.json");

            File.WriteAllText(manifest, $@"{{
              ""namespace"": ""Generated.Vocabularies"",
              ""output"": ""Vocabularies.g.cs"",
              ""vocabularies"": [ {{ ""file"": ""{file}"", ""prefix"": ""{prefix}"", ""uri"": ""{uri}"" }} ]
            }}");

            return manifest;
        }

        private void CopyOntology(string file)
        {
            string source = Path.Combine(_ontologies, file);

            Assume.That(File.Exists(source), $"Corpus file missing: {source}");

            File.Copy(source, Path.Combine(_scratch, file), true);
        }

        #endregion
    }
}
