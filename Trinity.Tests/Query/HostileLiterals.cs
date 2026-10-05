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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Query
{
    /// <summary>
    /// String values chosen to end a SPARQL literal early, or to be decoded into something else, in any
    /// of the forms Trinity has written or could write.
    /// </summary>
    /// <remarks>
    /// The payloads name a sentinel subject, <see cref="Sentinel"/>, and a victim graph. A store test
    /// passes a graph it clears in tear-down, so a payload that does take effect is both visible (ask
    /// for the sentinel) and cleaned up, rather than leaving triples behind in the container.
    /// </remarks>
    public static class HostileLiterals
    {
        /// <summary>
        /// The subject every payload tries to write. If it exists in any graph, a value escaped its literal.
        /// </summary>
        public static readonly Uri Sentinel = new Uri("urn:trinity:hostile");

        /// <summary>
        /// The graph payloads aim at when the caller names none.
        /// </summary>
        public static readonly Uri DefaultVictim = new Uri("urn:trinity:hostile:graph");

        /// <summary>
        /// The corpus, with payloads aimed at <paramref name="victim"/>.
        /// </summary>
        public static string[] Values(Uri victim = null)
        {
            string s = "<" + Sentinel.OriginalString + ">";
            string p = "<" + Sentinel.OriginalString + "#p>";
            string g = "<" + (victim ?? DefaultVictim).OriginalString + ">";

            return new[]
            {
                "",
                "'",
                "\"",
                "\\",
                "'''",
                "\"\"\"",
                "ends with an apostrophe'",
                "ends with a backslash\\",
                "an escaped \\\" quote",
                "a double backslash before a quote \\\\\"",
                "line\nfeed",
                "carriage\rreturn",
                "windows\r\nline",
                "tab\there",
                // Several lines and a trailing apostrophe: refused by Fuseki (HTTP 400), and on Virtuoso
                // the commit silently wrote nothing.
                "line one\nends with an apostrophe'",
                // Escape sequences as literal text. SPARQL 1.1 §19.2 decodes \u before parsing, so a
                // serializer that ever wrote one would turn these into the characters they name.
                "\\u0022",
                "\\u005C",
                "\\u000A",
                "\\u0027",
                "\\U00000022",
                "{ } ; # < > @en ^^ ?s ?p ?o",
                "# a comment\n",
                "é 日本語",
                "\U0001F600",
                // Aimed at the long form '''…''', which ended at the first ''' regardless of escaping.
                "x\n''' . " + s + " " + p + " 'injected' . " + s + " " + p + " '''y",
                // Aimed at the long form, closing the operation and appending another.
                "x\n'''. } } WHERE {} ; INSERT DATA { GRAPH " + g + " { " + s + " " + p + " 'injected' } } ; INSERT { GRAPH " + g + " { " + s + " " + p + " '''y",
                // Aimed at the short single-quoted form.
                "x' . " + s + " " + p + " 'injected' . " + s + " " + p + " 'y",
                // Aimed at the short double-quoted form.
                "x\" . " + s + " " + p + " \"injected\" . " + s + " " + p + " \"y",
                "x\\\" . " + s + " " + p + " \"injected\" . " + s + " " + p + " \"y",
                "x\\\\\" . " + s + " " + p + " \"injected\" . " + s + " " + p + " \"y",
                "x\"\"\" . " + s + " " + p + " \"injected\" . " + s + " " + p + " \"\"\"y",
            };
        }

        /// <summary>
        /// Asserts that no payload took effect: the sentinel is in neither <paramref name="model"/> nor any
        /// other graph, and the <paramref name="victim"/> graph the payloads aimed at is still empty.
        /// </summary>
        public static void AssertNothingEscaped(IStore store, IModel model, IModel victim)
        {
            Assert.IsFalse(model.ExecuteQuery(new SparqlQuery("ASK WHERE { @s ?p ?o }").Bind("@s", Sentinel)).GetAnwser(),
                "a value ended its literal early and wrote the sentinel into the model");
            Assert.IsTrue(victim.IsEmpty,
                "a value ended its literal early and wrote into another graph");
            Assert.IsFalse(store.ExecuteQuery(new SparqlQuery("ASK WHERE { GRAPH ?g { @s ?p ?o } }").Bind("@s", Sentinel)).GetAnwser(),
                "a value ended its literal early and wrote the sentinel into some graph");
        }

        /// <summary>
        /// How many values <paramref name="model"/> holds for a property, asked of the store rather than of
        /// a mapped resource: a single-valued mapping shows one value even when two are stored.
        /// </summary>
        public static int CountValues(IModel model, Uri subject, string predicate)
        {
            var query = new SparqlQuery("SELECT ?o WHERE { @subject @predicate ?o }")
                .Bind("@subject", subject)
                .Bind("@predicate", new Uri(predicate));

            return model.GetBindings(query).Count();
        }

        /// <summary>
        /// The corpus as NUnit cases, named so a failure says which value it was.
        /// </summary>
        public static IEnumerable<TestCaseData> Cases()
        {
            return Values().Select(v => new TestCaseData(v).SetArgDisplayNames(SparqlLiteralOracle.Display(v)));
        }

        /// <summary>
        /// Pieces the fuzzer joins. Tokens rather than characters, because the sequences that matter -
        /// <c>'''</c>, <c>\u0022</c>, <c>\\"</c> - are almost never produced one random character at a time.
        /// </summary>
        private static readonly string[] Alphabet =
        {
            "'", "\"", "\\", "\n", "\r", "\t", "'''", "\"\"\"", "\\u0022", "\\u005C", "\\\\", "\\\"", "u",
            "{", "}", ".", ";", "#", "<", ">", "@en", "^^", " ", "a", "é", "\U0001F600",
        };

        /// <summary>
        /// The seed for <see cref="Fuzz"/>, overridable through <c>TRINITY_FUZZ_SEED</c> to explore further.
        /// </summary>
        public static int Seed
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("TRINITY_FUZZ_SEED");

                return int.TryParse(value, out int seed) ? seed : 20261002;
            }
        }

        /// <summary>
        /// Deterministic random values over <see cref="Alphabet"/>.
        /// </summary>
        public static IEnumerable<string> Fuzz(int count, int maxTokens = 12)
        {
            var random = new Random(Seed);

            for (int i = 0; i < count; i++)
            {
                var value = new StringBuilder();
                int length = random.Next(maxTokens + 1);

                for (int j = 0; j < length; j++)
                {
                    value.Append(Alphabet[random.Next(Alphabet.Length)]);
                }

                yield return value.ToString();
            }
        }
    }
}
