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

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Semiodesk.Trinity.Vocabulary.Cli
{
    /// <summary>
    /// Generates or verifies the files a manifest describes.
    /// </summary>
    /// <remarks>
    /// Separated from <c>Program</c> so the exit codes — particularly <see cref="OutOfDate"/>, which is
    /// what makes <c>--check</c> usable in CI — can be tested without launching a process. Output goes to
    /// injected writers for the same reason.
    /// </remarks>
    internal static class VocabularyRunner
    {
        public const int Success = 0;
        public const int UsageError = 1;
        public const int GenerationError = 2;
        public const int OutOfDate = 3;

        /// <summary>
        /// Runs the tool over one manifest.
        /// </summary>
        /// <param name="manifestPath">Path to the manifest.</param>
        /// <param name="check">Verify only; write nothing.</param>
        /// <param name="output">Where progress is written.</param>
        /// <param name="error">Where problems are written.</param>
        /// <returns>The process exit code.</returns>
        public static int Run(string manifestPath, bool check, TextWriter output, TextWriter error)
        {
            if (!File.Exists(manifestPath))
            {
                error.WriteLine($"error: manifest not found: {manifestPath}");

                return UsageError;
            }

            // Paths inside the manifest resolve against the manifest itself, so a checked-in manifest
            // behaves the same from any working directory.
            string root = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? ".";

            List<VocabularyDocument> documents = VocabularyManifest.Read(manifestPath, root);
            var generator = new VocabularyGenerator();
            int stale = 0;

            foreach (VocabularyDocument document in documents)
            {
                string generated = generator.Generate(document);
                string outputPath = Path.GetFullPath(Path.Combine(root, document.Output));

                if (check)
                {
                    if (!File.Exists(outputPath))
                    {
                        error.WriteLine($"out of date: {document.Output} does not exist");
                        stale++;
                    }
                    else if (!Equivalent(File.ReadAllText(outputPath), generated))
                    {
                        error.WriteLine($"out of date: {document.Output} differs from the vocabularies");
                        stale++;
                    }
                    else
                    {
                        output.WriteLine($"up to date: {document.Output}");
                    }

                    continue;
                }

                string? directory = Path.GetDirectoryName(outputPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(outputPath, generated, new UTF8Encoding(false));

                output.WriteLine($"wrote {document.Output}");
            }

            if (stale > 0)
            {
                error.WriteLine(
                    $"{stale} file(s) out of date. Run trinity-vocab without --check to regenerate.");

                return OutOfDate;
            }

            return Success;
        }

        /// <summary>
        /// Compares generated output to a file on disk, ignoring line-ending differences so a checkout with
        /// normalized endings does not read as out of date.
        /// </summary>
        private static bool Equivalent(string left, string right) =>
            left.Replace("\r\n", "\n") == right.Replace("\r\n", "\n");
    }
}
