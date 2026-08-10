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
using System.Text;
using Semiodesk.Trinity.Vocabulary;

namespace Semiodesk.Trinity.Vocabulary.Cli
{
    /// <summary>
    /// Entry point for the <c>trinity-vocab</c> tool.
    /// </summary>
    internal static class Program
    {
        private const int Success = 0;
        private const int UsageError = 1;
        private const int GenerationError = 2;
        private const int OutOfDate = 3;

        private static int Main(string[] args)
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                WriteUsage();

                return args.Length == 0 ? UsageError : Success;
            }

            string manifestPath = args[0];
            bool check = false;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--check")
                {
                    check = true;
                }
                else
                {
                    Console.Error.WriteLine($"Unknown option '{args[i]}'.");
                    WriteUsage();

                    return UsageError;
                }
            }

            try
            {
                return Run(manifestPath, check);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("error: " + exception.Message);

                return GenerationError;
            }
        }

        private static int Run(string manifestPath, bool check)
        {
            if (!File.Exists(manifestPath))
            {
                Console.Error.WriteLine($"error: manifest not found: {manifestPath}");

                return UsageError;
            }

            // Paths inside the manifest are relative to the manifest itself, so a checked-in manifest
            // works the same from any working directory.
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
                        Console.Error.WriteLine($"out of date: {document.Output} does not exist");
                        stale++;
                    }
                    else if (!Equivalent(File.ReadAllText(outputPath), generated))
                    {
                        Console.Error.WriteLine($"out of date: {document.Output} differs from the vocabularies");
                        stale++;
                    }
                    else
                    {
                        Console.WriteLine($"up to date: {document.Output}");
                    }

                    continue;
                }

                string? directory = Path.GetDirectoryName(outputPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(outputPath, generated, new UTF8Encoding(false));

                Console.WriteLine($"wrote {document.Output}");
            }

            if (stale > 0)
            {
                Console.Error.WriteLine(
                    $"{stale} file(s) out of date. Run trinity-vocab without --check to regenerate.");

                return OutOfDate;
            }

            return Success;
        }

        /// <summary>
        /// Compares generated output to a file on disk, ignoring line-ending differences so a checkout
        /// with normalized endings does not read as out of date.
        /// </summary>
        private static bool Equivalent(string left, string right) =>
            left.Replace("\r\n", "\n") == right.Replace("\r\n", "\n");

        private static bool IsHelp(string argument) =>
            argument == "-h" || argument == "--help" || argument == "help";

        private static void WriteUsage()
        {
            Console.WriteLine("Generates Semiodesk.Trinity vocabulary classes from RDF files.");
            Console.WriteLine();
            Console.WriteLine("  trinity-vocab <manifest.json> [--check]");
            Console.WriteLine();
            Console.WriteLine("  --check   Do not write anything; exit non-zero if the generated files on");
            Console.WriteLine("            disk no longer match the vocabularies. Intended for CI.");
            Console.WriteLine();
            Console.WriteLine("Manifest format:");
            Console.WriteLine();
            Console.WriteLine("  {");
            Console.WriteLine("    \"namespace\": \"My.Project\",");
            Console.WriteLine("    \"output\": \"Ontologies.g.cs\",");
            Console.WriteLine("    \"vocabularies\": [");
            Console.WriteLine("      { \"file\": \"ontologies/foaf.rdf\", \"prefix\": \"foaf\",");
            Console.WriteLine("        \"uri\": \"http://xmlns.com/foaf/0.1/\" }");
            Console.WriteLine("    ]");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("Paths are relative to the manifest. A manifest may also be an array of such");
            Console.WriteLine("objects to generate several files in one run.");
        }
    }
}
