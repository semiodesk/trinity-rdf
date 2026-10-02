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
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Semiodesk.Trinity.Tests.Query
{
    /// <summary>
    /// Decides whether a piece of text is exactly one SPARQL string literal, and what value it denotes,
    /// from the grammar alone.
    /// </summary>
    /// <remarks>
    /// Deliberately not dotNetRDF. dotNetRDF is the tokenizer whose output Trinity re-serializes and the
    /// engine behind the in-memory store, so asking it whether Trinity's escaping is right only shows
    /// that the two agree. This is written from SPARQL 1.1 §19.8:
    /// <code>
    /// STRING_LITERAL2 ::= '"' ( ([^#x22#x5C#xA#xD]) | ECHAR )* '"'
    /// ECHAR           ::= '\' [tbnrf\"']
    /// </code>
    /// If text matches the whole pattern, every conforming lexer reads it as one token whatever
    /// surrounds it, so no template the literal is placed in can be broken out of. That is a stronger
    /// statement than "the update parses", which depends on the template.
    /// </remarks>
    public static class SparqlLiteralOracle
    {
        private static readonly Regex ShortDoubleQuoted = new Regex(
            "^\"(?:[^\"\\\\\\r\\n]|\\\\[tbnrf\"'\\\\])*\"$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Indicates whether <paramref name="text"/> is exactly one double-quoted string literal.
        /// </summary>
        public static bool IsOneLiteral(string text)
        {
            return text != null && ShortDoubleQuoted.IsMatch(text);
        }

        /// <summary>
        /// The value a single double-quoted string literal denotes.
        /// </summary>
        /// <exception cref="ArgumentException">The text is not exactly one such literal.</exception>
        public static string Decode(string text)
        {
            if (!IsOneLiteral(text))
            {
                throw new ArgumentException($"Not a single SPARQL string literal: {Display(text)}", nameof(text));
            }

            var value = new StringBuilder(text.Length);

            for (int i = 1; i < text.Length - 1; i++)
            {
                char c = text[i];

                if (c != '\\')
                {
                    value.Append(c);
                    continue;
                }

                // The pattern guarantees an escape character follows.
                switch (text[++i])
                {
                    case 't': value.Append('\t'); break;
                    case 'b': value.Append('\b'); break;
                    case 'n': value.Append('\n'); break;
                    case 'r': value.Append('\r'); break;
                    case 'f': value.Append('\f'); break;
                    case '"': value.Append('"'); break;
                    case '\'': value.Append('\''); break;
                    case '\\': value.Append('\\'); break;
                }
            }

            return value.ToString();
        }

        /// <summary>
        /// Splits a literal followed by a language tag (<c>"x"@de</c>) or a datatype (<c>"x"^^&lt;dt&gt;</c>)
        /// into the literal and what follows it.
        /// </summary>
        /// <remarks>
        /// The literal ends at the first unescaped quote after the opening one. Everything after it is
        /// returned as the suffix for the caller to compare exactly.
        /// </remarks>
        public static string SplitSuffix(string text, out string suffix)
        {
            if (text == null || text.Length < 2 || text[0] != '"')
            {
                throw new ArgumentException($"Does not start with a string literal: {Display(text)}", nameof(text));
            }

            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                }
                else if (text[i] == '"')
                {
                    suffix = text.Substring(i + 1);

                    return text.Substring(0, i + 1);
                }
            }

            throw new ArgumentException($"Unterminated string literal: {Display(text)}", nameof(text));
        }

        /// <summary>
        /// Renders a value with its control characters visible, for assertion messages and test names.
        /// </summary>
        public static string Display(string value)
        {
            if (value == null)
            {
                return "null";
            }

            var text = new StringBuilder(value.Length + 2);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\n': text.Append("\\n"); break;
                    case '\r': text.Append("\\r"); break;
                    case '\t': text.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            text.Append("\\x").Append(((int)c).ToString("X2"));
                        }
                        else
                        {
                            text.Append(c);
                        }

                        break;
                }
            }

            return text.ToString();
        }
    }
}
