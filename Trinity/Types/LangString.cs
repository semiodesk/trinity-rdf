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
using System.Diagnostics;
using System.Globalization;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// An RDF language-tagged string literal: a lexical form plus a BCP-47 language tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one representation of a language-tagged literal in Trinity (ADR-0048). It replaces the
    /// four shapes that preceded it — <c>Tuple&lt;string,string&gt;</c>, <c>Tuple&lt;string,CultureInfo&gt;</c>,
    /// <c>string[] { value, lang }</c>, and a bare string carrying its tag out of band on the mapping.
    /// Those disagreed with each other, and every defect in ADR-0048 followed from a call site re-deriving
    /// what a tagged literal is.
    /// </para>
    /// <para>
    /// A <b>plain</b> (untagged) literal is a plain <see cref="string"/>, not a <c>LangString</c> with a null
    /// tag. <see cref="Language"/> is never null or empty. Admitting a null tag would create a second
    /// representation of an untagged literal and reopen the problem this type closes.
    /// </para>
    /// <para>
    /// Consequently <c>"Hallo"</c> and <c>new LangString("Hallo", "de")</c> are different types and never
    /// compare equal — which is the RDF reading, and the semantics
    /// <c>ResourceTest.HasPropertyTest2</c> has always pinned.
    /// </para>
    /// <para>
    /// This is a class rather than a struct deliberately. The mapping engine is <see cref="object"/>-typed
    /// throughout, so a struct's allocation saving is boxed away on the first hop, while
    /// <c>PropertyMapping&lt;T&gt;.Clear()</c> assigns <c>default(T)</c> — which for a struct would be a
    /// <c>LangString</c> with a null <see cref="Value"/>, an invalid literal handed to user code — and
    /// <c>Resource.AddPropertyToMapping</c>'s null guard could never fire. <see cref="UriRef"/> is a class
    /// for the same family of reasons.
    /// </para>
    /// <para>
    /// <b>There is deliberately no conversion to or from <see cref="string"/>, implicit or explicit.</b>
    /// An implicit conversion to <c>string</c> would bind <c>label == "Hallo"</c> to
    /// <c>string.operator ==</c> and return <c>true</c> for <c>"Hallo"@fr</c>; the reverse conversion would
    /// make the same expression return <c>false</c> for <c>"Hallo"@de</c>. Both are silent, in the most
    /// commonly written expression in the API. With neither, that expression does not compile — which is
    /// what ADR-0025 wishes it could have achieved for <see cref="Uri"/> and could not.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{ToNTriples(),nq}")]
    public sealed class LangString : IEquatable<LangString>, IComparable<LangString>
    {
        #region Members

        /// <summary>
        /// The lexical form of the literal. Never <c>null</c>.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The BCP-47 language tag, normalized to lower case. Never <c>null</c> or empty.
        /// </summary>
        /// <remarks>
        /// Normalizing at construction is what makes the tag a single fact rather than something each call
        /// site lower-cases or forgets to. RDF 1.1 compares language tags case-insensitively, so this loses
        /// nothing semantically; it does mean a store returning <c>de-DE</c> round-trips as <c>de-de</c>.
        /// It also keeps the ADR-0039 commit delta stable, since that compares serialized triples ordinally.
        /// </remarks>
        public string Language { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a language-tagged literal.
        /// </summary>
        /// <param name="value">The lexical form.</param>
        /// <param name="language">A BCP-47 language tag, such as <c>de</c> or <c>en-GB</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="language"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="language"/> is empty or white space.</exception>
        public LangString(string value, string language)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language),
                    "A LangString always carries a tag. An untagged literal is a plain string.");
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException(
                    "The language tag must not be empty. An untagged literal is a plain string, not a " +
                    "LangString with an empty tag — an empty tag would serialize to the invalid SPARQL \"…\"@.",
                    nameof(language));
            }

            Value = value;
            Language = NormalizeLanguage(language, nameof(language));
        }

        /// <summary>
        /// Validates and normalizes a language tag: the single place a tag is checked and lower-cased.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The shape check is not cosmetic. A tag is written into SPARQL update text unescaped -- there
        /// is no way to escape one, because a tag is grammar rather than a string literal -- so an
        /// unvalidated tag taken from a request reaches the store as query syntax. Rejecting anything
        /// that is not a well-formed tag closes that at the only point every tag passes through.
        /// </para>
        /// <para>
        /// It also converts a class of silent failures into an immediate, located one: a tag such as
        /// <c>de DE</c> or <c>de-DE_phonebook</c> used to be accepted here and then fail the entire
        /// <c>Commit()</c>, naming neither the property nor the tag.
        /// </para>
        /// <para>
        /// The grammar enforced is SPARQL's and Turtle's <c>LANGTAG</c>:
        /// <c>[a-zA-Z]+('-'[a-zA-Z0-9]+)*</c> — letters, digits and hyphens, with no empty subtag. That
        /// is exactly the set of tags that can be written back verbatim, which is what makes it the
        /// right boundary: a tag outside it cannot be serialized at all, and everything inside it is
        /// inert as query text.
        /// </para>
        /// <para>
        /// It deliberately does <b>not</b> enforce BCP-47 <i>well-formedness</i>. An earlier version
        /// also capped each subtag at eight characters, per RFC 5646, and that was a real bug rather
        /// than a strict-but-harmless check: every literal read from a store passes through here, so a
        /// single triple that some other writer tagged <c>@en-abcdefghij</c> — legal in Turtle, SPARQL
        /// and Virtuoso — made <b>every</b> read of that resource throw, including the untyped
        /// <c>GetResource</c>. Length adds nothing to the safety argument above, and policing a tag
        /// registry is not this type's job. The typo cases that motivated validation are still caught,
        /// because <c>de DE</c> and <c>de-DE_phonebook</c> are outside the grammar.
        /// </para>
        /// </remarks>
        internal static string NormalizeLanguage(string language, string parameterName)
        {
            if (language == null)
            {
                throw new ArgumentNullException(parameterName,
                    "A LangString always carries a tag. An untagged literal is a plain string.");
            }

            string tag = language.Trim();

            if (tag.Length == 0)
            {
                throw new ArgumentException(
                    "The language tag must not be empty. An untagged literal is a plain string, not a " +
                    "LangString with an empty tag - an empty tag would serialize to the invalid SPARQL \"...\"@.",
                    parameterName);
            }

            if (!IsLanguageTagShaped(tag))
            {
                throw new ArgumentException(
                    $"'{language}' is not a language tag. A tag is one or more letters, optionally " +
                    "followed by '-'-separated subtags of letters or digits, such as 'de', 'en-GB' or " +
                    "'zh-Hans-CN'. Tags are written into SPARQL as syntax rather than as escapable " +
                    "text, so one that cannot be written verbatim is refused here rather than passed " +
                    "on.",
                    parameterName);
            }

            return tag.ToLowerInvariant();
        }

        /// <summary>
        /// Indicates whether a tag matches the SPARQL/Turtle <c>LANGTAG</c> grammar
        /// <c>[a-zA-Z]+('-'[a-zA-Z0-9]+)*</c>.
        /// </summary>
        private static bool IsLanguageTagShaped(string tag)
        {
            int length = 0;
            bool primary = true;

            for (int i = 0; i < tag.Length; i++)
            {
                char c = tag[i];

                if (c == '-')
                {
                    // Catches a leading, trailing or doubled separator, all of which would otherwise
                    // produce an empty subtag.
                    if (length == 0)
                    {
                        return false;
                    }

                    length = 0;
                    primary = false;

                    continue;
                }

                bool letter = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

                // The primary language subtag is letters only; later subtags may also carry digits.
                if (!letter && !(!primary && c >= '0' && c <= '9'))
                {
                    return false;
                }

                length++;
            }

            return length > 0;
        }

        /// <summary>
        /// Creates a language-tagged literal from a culture.
        /// </summary>
        /// <param name="value">The lexical form.</param>
        /// <param name="culture">The culture whose name supplies the language tag.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="culture"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="culture"/> is the invariant culture, whose name is the empty string and therefore
        /// names no language.
        /// </exception>
        public LangString(string value, CultureInfo culture)
            : this(value, CultureName(culture, nameof(culture)))
        {
        }

        /// <summary>
        /// The language tag a culture names: the single implementation the containers share.
        /// </summary>
        /// <remarks>
        /// There were three identical copies of this, differing only in the wording of the exception.
        /// Nothing kept them in step, which is how the four representations ADR-0048 replaced came to
        /// disagree in the first place.
        /// </remarks>
        internal static string CultureName(CultureInfo culture, string parameterName)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (string.IsNullOrEmpty(culture.Name))
            {
                throw new ArgumentException(
                    "The invariant culture names no language; use a plain string for an untagged " +
                    "literal, or the container's Invariant property.",
                    parameterName);
            }

            return culture.Name;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates whether this literal's tag matches a language range under RFC 4647 basic filtering —
        /// the same rule SPARQL's <c>langMatches</c> implements.
        /// </summary>
        /// <remarks>
        /// A range matches its own tag and any tag that extends it at a subtag boundary, so <c>de</c> matches
        /// <c>de</c>, <c>de-DE</c> and <c>de-DE-1901</c>, but not <c>deu</c>. The wildcard <c>*</c> matches
        /// every tag.
        /// </remarks>
        /// <param name="range">A language range, such as <c>de</c> or <c>*</c>.</param>
        /// <returns><c>true</c> if the range matches this literal's tag.</returns>
        public bool MatchesLanguage(string range)
        {
            if (string.IsNullOrEmpty(range))
            {
                return false;
            }

            if (range == "*")
            {
                return true;
            }

            var normalized = range.Trim().ToLowerInvariant();

            if (Language.Equals(normalized, StringComparison.Ordinal))
            {
                return true;
            }

            // An extension only counts at a subtag boundary: "de" matches "de-DE" but must not match "deu".
            return Language.Length > normalized.Length
                && Language[normalized.Length] == '-'
                && Language.StartsWith(normalized, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the N-Triples form of this literal, such as <c>"Hallo"@de</c>.
        /// </summary>
        /// <remarks>
        /// The lexical form is escaped per N-Triples, so a value containing a quote, a backslash or a
        /// newline still produces something parseable rather than something that merely looks right.
        /// The tag needs no escaping: it is validated at construction and can only be letters, digits
        /// and hyphens. The literal is written by <see cref="SparqlSerializer.SerializeString"/>, the same
        /// function every query takes it through: the short double-quoted form N-Triples requires is
        /// the one SPARQL is given (ADR-0052). This used to be a copy of that escaping, one of three.
        /// </remarks>
        public string ToNTriples()
        {
            return SparqlSerializer.SerializeString(Value) + "@" + Language;
        }

        /// <summary>
        /// Returns the lexical form, so that interpolating a localized value yields the text rather than
        /// its RDF notation. Use <see cref="ToNTriples"/> when the tag should be visible.
        /// </summary>
        public override string ToString()
        {
            return Value;
        }

        #endregion

        #region Equality

        /// <summary>
        /// Indicates whether this literal equals another: same lexical form and same tag.
        /// </summary>
        /// <param name="other">The literal to compare with.</param>
        /// <returns><c>true</c> if both are equal.</returns>
        public bool Equals(LangString other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Both tags are already normalized, so an ordinal comparison is the case-insensitive one.
            return string.Equals(Value, other.Value, StringComparison.Ordinal)
                && string.Equals(Language, other.Language, StringComparison.Ordinal);
        }

        /// <summary>
        /// Indicates whether this literal equals another object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an equal <see cref="LangString"/>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as LangString);
        }

        /// <summary>
        /// Returns a hash code consistent with <see cref="Equals(LangString)"/>.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Value.GetHashCode() * 397) ^ Language.GetHashCode();
            }
        }

        /// <summary>
        /// Orders by language tag, then by lexical form, so enumerating a set of literals is stable.
        /// </summary>
        /// <param name="other">The literal to compare with.</param>
        /// <returns>A signed number indicating the relative order.</returns>
        public int CompareTo(LangString other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            var byLanguage = string.CompareOrdinal(Language, other.Language);

            return byLanguage != 0 ? byLanguage : string.CompareOrdinal(Value, other.Value);
        }

        /// <summary>
        /// Indicates whether two literals are equal.
        /// </summary>
        /// <remarks>
        /// Declared explicitly because operators bind <b>statically</b>: without this, <c>a == b</c> on two
        /// <c>LangString</c> references would fall back to reference equality and silently report <c>false</c>
        /// for equal literals. That is the ADR-0025 defect, and this type does not repeat it.
        /// </remarks>
        /// <param name="left">The first literal.</param>
        /// <param name="right">The second literal.</param>
        /// <returns><c>true</c> if both are equal.</returns>
        public static bool operator ==(LangString left, LangString right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two literals are not equal.
        /// </summary>
        /// <param name="left">The first literal.</param>
        /// <param name="right">The second literal.</param>
        /// <returns><c>true</c> if they differ.</returns>
        public static bool operator !=(LangString left, LangString right)
        {
            return !(left == right);
        }

        #endregion
    }
}
