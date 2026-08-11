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

namespace Semiodesk.Trinity.Vocabulary
{
    /// <summary>
    /// What to generate: one output file covering one or more vocabularies.
    /// </summary>
    public sealed class VocabularyDocument
    {
        /// <summary>
        /// The C# namespace the generated classes are placed in. May be empty for the global namespace.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// The directory the generated files are written to, relative to the manifest. Empty means the
        /// manifest's own directory.
        /// </summary>
        /// <remarks>
        /// A directory rather than a filename: each vocabulary gets its own <c>&lt;prefix&gt;.g.cs</c>, so
        /// regenerating one does not rewrite the others.
        /// </remarks>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// The vocabularies to include, in the order they should be emitted.
        /// </summary>
        public IList<VocabularySource> Vocabularies { get; set; } = new List<VocabularySource>();
    }

    /// <summary>
    /// One RDF vocabulary to read.
    /// </summary>
    /// <remarks>
    /// Local files only. The 1.x config also accepted a web source and a separate metadata source; both
    /// were dropped in 2.0 — resolve a remote vocabulary to a local file yourself, so generation stays
    /// reproducible and offline.
    /// </remarks>
    public sealed class VocabularySource
    {
        /// <summary>The path to the RDF file, in any format dotNetRDF can parse.</summary>
        public string File { get; set; }

        /// <summary>The vocabulary prefix, which becomes the generated class name.</summary>
        public string Prefix { get; set; }

        /// <summary>The vocabulary's namespace URI.</summary>
        public Uri Uri { get; set; }
    }

    /// <summary>
    /// One generated file: what to call it, and what goes in it.
    /// </summary>
    public sealed class GeneratedFile
    {
        /// <summary>
        /// Creates a generated file.
        /// </summary>
        /// <param name="fileName">The filename, without a directory.</param>
        /// <param name="source">The C# source.</param>
        public GeneratedFile(string fileName, string source)
        {
            FileName = fileName;
            Source = source;
        }

        /// <summary>The filename, conventionally <c>&lt;prefix&gt;.g.cs</c>.</summary>
        public string FileName { get; }

        /// <summary>The C# source.</summary>
        public string Source { get; }
    }

    /// <summary>
    /// A single term, already named and classified.
    /// </summary>
    public sealed class VocabularyTerm
    {
        /// <summary>
        /// Creates a term.
        /// </summary>
        /// <param name="name">Its C# member name.</param>
        /// <param name="uri">Its URI.</param>
        /// <param name="kind">The Trinity type to emit: Class, Property or Resource.</param>
        /// <param name="comment">Its <c>rdfs:comment</c>, if any.</param>
        public VocabularyTerm(string name, Uri uri, string kind, string comment)
        {
            Name = name;
            Uri = uri;
            Kind = kind;
            Comment = comment;
        }

        /// <summary>The C# member name.</summary>
        public string Name { get; }

        /// <summary>The term URI.</summary>
        public Uri Uri { get; }

        /// <summary>
        /// The emitted Trinity type: <c>Class</c>, <c>Property</c>, or <c>Resource</c> for a term that is
        /// neither — <c>rdf:nil</c> being the canonical example.
        /// </summary>
        public string Kind { get; }

        /// <summary>The term's <c>rdfs:comment</c>, or null.</summary>
        public string Comment { get; }
    }
}
