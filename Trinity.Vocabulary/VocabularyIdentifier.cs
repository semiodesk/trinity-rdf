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
using System.Linq;

namespace Semiodesk.Trinity.Vocabulary
{
    /// <summary>
    /// Turns a term URI into a C# member name, resolving collisions within one vocabulary.
    /// </summary>
    /// <remarks>
    /// The rules are reproduced from the retired 1.x <c>OntologyGenerator</c> deliberately, down to
    /// prefixing keywords with an underscore rather than using <c>@</c>. Existing hand-written
    /// vocabularies were produced by that tool and the test suite references the names it chose, so any
    /// change here silently renames public API.
    /// </remarks>
    public sealed class VocabularyIdentifier
    {
        #region Members

        /// <summary>
        /// C# keywords, which cannot be used as member names.
        /// </summary>
        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        /// <summary>
        /// Names already used in this vocabulary. Seeded with the vocabulary class's own name so a term
        /// cannot collide with the type that contains it.
        /// </summary>
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);

        private readonly Uri _namespaceUri;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an identifier factory for one vocabulary.
        /// </summary>
        /// <param name="namespaceUri">The vocabulary's namespace, used to build a relative name on collision.</param>
        /// <param name="prefix">The vocabulary prefix, which becomes the containing class name.</param>
        public VocabularyIdentifier(Uri namespaceUri, string prefix)
        {
            _namespaceUri = namespaceUri;

            if (!string.IsNullOrEmpty(prefix))
            {
                _used.Add(prefix);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Produces a unique C# member name for a term, or null if the term cannot be named.
        /// </summary>
        /// <remarks>
        /// Three stages, matching 1.x: the local name (fragment, else last path segment); on collision
        /// the URI made relative to the vocabulary namespace; on further collision a numeric suffix.
        /// </remarks>
        /// <param name="termUri">The term's URI.</param>
        /// <returns>A unique member name, or null when the term is the vocabulary itself or unnameable.</returns>
        public string Next(Uri termUri)
        {
            // The vocabulary's own URI is not a term.
            if (_namespaceUri != null && termUri.OriginalString == _namespaceUri.OriginalString)
            {
                return null;
            }

            string name = Sanitize(LocalName(termUri));

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (_used.Contains(name))
            {
                string relative = Sanitize(RelativeName(termUri));

                if (!string.IsNullOrEmpty(relative))
                {
                    name = relative;
                }
            }

            if (_used.Contains(name))
            {
                int suffix = 0;

                while (_used.Contains(name + "_" + suffix))
                {
                    suffix++;
                }

                name = name + "_" + suffix;
            }

            _used.Add(name);

            return name;
        }

        /// <summary>
        /// The fragment if there is one, otherwise the last path segment.
        /// </summary>
        private static string LocalName(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                if (!string.IsNullOrEmpty(uri.Fragment) && uri.Fragment.Length > 1)
                {
                    return uri.Fragment.Substring(1);
                }

                string last = uri.Segments.LastOrDefault();

                if (!string.IsNullOrEmpty(last))
                {
                    return last;
                }
            }

            // Relative or opaque URIs (urn:, semio:test:…) have no fragment or segments to use.
            string text = uri.OriginalString;
            int separator = text.LastIndexOfAny(new[] { '#', '/', ':' });

            return separator >= 0 && separator < text.Length - 1 ? text.Substring(separator + 1) : text;
        }

        private string RelativeName(Uri uri)
        {
            if (_namespaceUri == null || !_namespaceUri.IsAbsoluteUri || !uri.IsAbsoluteUri)
            {
                return null;
            }

            return _namespaceUri.MakeRelativeUri(uri).ToString();
        }

        /// <summary>
        /// Makes a raw local name into a legal C# identifier.
        /// </summary>
        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (Keywords.Contains(name))
            {
                name = "_" + name;
            }

            if (char.IsDigit(name[0]))
            {
                name = "_" + name;
            }

            name = name.Trim('/');

            return name
                .Replace('/', '_')
                .Replace('.', '_')
                .Replace('-', '_')
                .Replace('#', '_')
                .Replace(':', '_')
                .Replace('%', '_')
                .Replace('~', '_')
                .Replace('(', '_')
                .Replace(')', '_');
        }

        #endregion
    }
}
