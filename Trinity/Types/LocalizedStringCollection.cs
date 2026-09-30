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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A mapped text property holding <b>any number of values per language</b>, plus optionally untagged
    /// ones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the collection of the pair: <c>LocalizedStringCollection</c> is to languages what
    /// <c>List&lt;string&gt;</c> is to a predicate, and <see cref="LocalizedString"/> is what
    /// <c>string</c> is (ADR-0048). Declare it for predicates that are genuinely multi-valued per
    /// language — <c>skos:altLabel</c> is the obvious one — where <see cref="LocalizedString"/> would
    /// keep the last value and let a later <c>Commit()</c> delete the rest.
    /// </para>
    /// <para>
    /// The indexer returns <b>every</b> value carrying a tag, and is exact match. Fallback lives in
    /// <see cref="Best()"/>, which necessarily picks a single value.
    /// </para>
    /// </remarks>
    public sealed class LocalizedStringCollection : ILocalizedText
    {
        private readonly LocalizedValueStore _store = new LocalizedValueStore();

        /// <summary>The backing store, so the mapping engine can copy into this container.</summary>
        internal LocalizedValueStore Store => _store;

        /// <summary>
        /// Creates an empty property.
        /// </summary>
        public LocalizedStringCollection()
        {
        }

        /// <summary>
        /// Creates a property holding the given literals.
        /// </summary>
        /// <param name="values">The literals. All are kept, including several sharing a tag.</param>
        public LocalizedStringCollection(IEnumerable<LangString> values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    _store.Add(value);
                }
            }
        }

        /// <summary>
        /// Every value carrying exactly this language tag, in arrival order. Empty when the tag is absent.
        /// </summary>
        /// <param name="language">A language tag, such as <c>de</c>.</param>
        /// <returns>The values for that tag.</returns>
        public IReadOnlyList<string> this[string language] => _store.ValuesFor(language);

        /// <summary>
        /// Every value carrying a culture's language tag.
        /// </summary>
        /// <param name="culture">The culture whose name is the tag.</param>
        /// <returns>The values for that tag.</returns>
        public IReadOnlyList<string> this[CultureInfo culture] => this[CultureName(culture)];

        /// <summary>
        /// The untagged literals on this predicate, in arrival order.
        /// </summary>
        public IReadOnlyList<string> Invariant => _store.Invariant;

        /// <summary>
        /// <c>true</c> when at least one untagged literal is present.
        /// </summary>
        public bool HasInvariant => _store.Invariant.Count > 0;

        /// <inheritdoc/>
        public IReadOnlyCollection<string> Languages => _store.Languages;

        /// <inheritdoc/>
        public int Count => _store.Count;

        /// <inheritdoc/>
        public bool IsEmpty => _store.IsEmpty;

        /// <summary>
        /// Adds a value for a language, keeping any this language already has.
        /// </summary>
        /// <param name="language">A language tag.</param>
        /// <param name="value">The value.</param>
        public void Add(string language, string value)
        {
            _store.Add(language, value);
        }

        /// <summary>
        /// Adds a literal, keeping any its language already has.
        /// </summary>
        /// <param name="value">The literal.</param>
        public void Add(LangString value)
        {
            _store.Add(value);
        }

        /// <summary>
        /// Adds an untagged literal.
        /// </summary>
        /// <param name="value">The value.</param>
        public void AddInvariant(string value)
        {
            _store.AddInvariant(value);
        }

        /// <summary>
        /// Replaces every value for a language with a single one.
        /// </summary>
        /// <param name="language">A language tag.</param>
        /// <param name="values">The values to set. Passing none removes the language.</param>
        public void Set(string language, params string[] values)
        {
            _store.Remove(language);

            if (values != null)
            {
                foreach (var value in values)
                {
                    if (value != null)
                    {
                        _store.Add(language, value);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool Contains(string language) => _store.Contains(language);

        /// <inheritdoc/>
        public bool Remove(string language) => _store.Remove(language);

        /// <summary>
        /// Removes one value carrying a language tag, leaving that language's other values in place.
        /// </summary>
        /// <param name="language">A language tag.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if it was removed.</returns>
        public bool Remove(string language, string value) => _store.Remove(language, value);

        /// <summary>
        /// Removes an untagged literal.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if it was removed.</returns>
        public bool RemoveInvariant(string value) => _store.RemoveInvariant(value);

        /// <inheritdoc/>
        public void Clear() => _store.Clear();

        /// <inheritdoc/>
        public string Best() => _store.BestForCurrentCulture();

        /// <inheritdoc/>
        public string Best(params string[] languageRanges) => _store.Best(languageRanges);

        /// <inheritdoc/>
        public bool TryGetBest(string languageRange, out LangString match) =>
            _store.TryGetBest(languageRange, out match);

        /// <summary>
        /// Enumerates the tagged literals, ordered by tag then value. The untagged values are not
        /// included; read them through <see cref="Invariant"/>.
        /// </summary>
        /// <returns>An enumerator over the tagged literals.</returns>
        public IEnumerator<LangString> GetEnumerator() => _store.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns the best value for the current UI culture, so interpolating the property yields text.
        /// </summary>
        public override string ToString() => Best() ?? string.Empty;

        private static string CultureName(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            if (string.IsNullOrEmpty(culture.Name))
            {
                throw new ArgumentException(
                    "The invariant culture names no language; use the Invariant property for untagged literals.",
                    nameof(culture));
            }

            return culture.Name;
        }
    }
}
