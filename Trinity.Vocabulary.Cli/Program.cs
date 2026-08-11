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

namespace Semiodesk.Trinity.Vocabulary.Cli
{
    /// <summary>
    /// Entry point for the <c>trinity-vocab</c> tool. Argument handling and usage text only — the work
    /// lives in <see cref="VocabularyRunner"/> so it can be tested without launching a process.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                WriteUsage();

                return args.Length == 0 ? VocabularyRunner.UsageError : VocabularyRunner.Success;
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

                    return VocabularyRunner.UsageError;
                }
            }

            try
            {
                return VocabularyRunner.Run(manifestPath, check, Console.Out, Console.Error);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("error: " + exception.Message);

                return VocabularyRunner.GenerationError;
            }
        }

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
