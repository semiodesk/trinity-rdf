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
using System.Text.Json;

namespace Semiodesk.Trinity.Vocabulary.Cli
{
    /// <summary>
    /// Reads the JSON manifest that tells the tool what to generate.
    /// </summary>
    /// <remarks>
    /// This replaces the 1.x <c>ontologies.config</c>, but only as tool input — Trinity itself no longer
    /// reads configuration at runtime (ADR-0011). Manifest parsing lives in the CLI on purpose: the
    /// engine takes typed options and knows nothing about files or JSON.
    /// </remarks>
    internal static class VocabularyManifest
    {
        /// <summary>
        /// Reads a manifest, which may hold a single document or an array of them.
        /// </summary>
        /// <param name="path">Path to the manifest.</param>
        /// <param name="root">Directory that relative paths inside the manifest resolve against.</param>
        /// <returns>The documents to generate.</returns>
        public static List<VocabularyDocument> Read(string path, string root)
        {
            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path), options);

            var documents = new List<VocabularyDocument>();

            if (json.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement element in json.RootElement.EnumerateArray())
                {
                    documents.Add(ReadDocument(element, root));
                }
            }
            else
            {
                documents.Add(ReadDocument(json.RootElement, root));
            }

            return documents;
        }

        private static VocabularyDocument ReadDocument(JsonElement element, string root)
        {
            var document = new VocabularyDocument
            {
                Namespace = GetString(element, "namespace") ?? string.Empty,
                // Optional: an empty directory means the manifest's own, which is the common case now that
                // each vocabulary writes its own file.
                Output = GetString(element, "output") ?? string.Empty
            };

            if (!element.TryGetProperty("vocabularies", out JsonElement vocabularies) ||
                vocabularies.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("A manifest entry has no 'vocabularies' array.");
            }

            foreach (JsonElement entry in vocabularies.EnumerateArray())
            {
                string file = GetString(entry, "file")
                              ?? throw new FormatException("A vocabulary entry is missing 'file'.");
                string prefix = GetString(entry, "prefix")
                                ?? throw new FormatException($"Vocabulary '{file}' is missing 'prefix'.");
                string uri = GetString(entry, "uri")
                             ?? throw new FormatException($"Vocabulary '{file}' is missing 'uri'.");

                document.Vocabularies.Add(new VocabularySource
                {
                    // Combine rather than concatenate, so paths containing spaces work unchanged.
                    File = Path.GetFullPath(Path.Combine(root, file)),
                    Prefix = prefix,
                    Uri = new Uri(uri, UriKind.RelativeOrAbsolute)
                });
            }

            if (document.Vocabularies.Count == 0)
            {
                throw new FormatException("A manifest entry lists no vocabularies.");
            }

            return document;
        }

        private static string? GetString(JsonElement element, string name) =>
            element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}
