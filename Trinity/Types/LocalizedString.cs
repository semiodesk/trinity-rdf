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

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A mapped text property holding <b>one value per language</b>, plus optionally an untagged one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the scalar of the pair: <c>LocalizedString</c> is to languages what <c>string</c> is to a
    /// predicate, and <see cref="LocalizedStringCollection"/> is what <c>List&lt;string&gt;</c> is
    /// (ADR-0048). Unlike the design it replaces, the property holds <b>every</b> language at once, so
    /// <see cref="Languages"/> can answer which ones exist and reading one never hides another.
    /// </para>
    /// <para>
    /// <b>Declaring this against genuinely multi-valued data is lossy, deliberately.</b> If the store
    /// holds two <c>@de</c> labels, the second replaces the first on the mapped surface — exactly what
    /// a mapped <c>string</c> already does to a multi-valued predicate.
    /// </para>
    /// <para>
    /// The dropped value is <b>orphaned, not deleted</b>. The commit snapshot is taken from
    /// <c>ListValues()</c>, which is the resource after this container already dropped the duplicate,
    /// so the value is in neither side of the ADR-0039 delta: <c>HasUnsavedChanges()</c> reports
    /// <c>false</c> and <c>Commit()</c> emits nothing for it. It therefore stays in the store,
    /// permanently invisible through this property, and no commit will ever remove it.
    /// <see cref="LocalizedStringCollection"/> is the escape hatch, as <c>List&lt;string&gt;</c> is today.
    /// </para>
    /// <para>
    /// The indexer is <b>exact match</b> on get and on set. Fallback lives in <see cref="Best()"/>, where
    /// it is named and opted into — a getter that fell back while the setter did not would make
    /// <c>t[k] = t[k]</c> change the data.
    /// </para>
    /// </remarks>
    public sealed class LocalizedString : ILocalizedText
    {
        private readonly LocalizedValueStore _store = new LocalizedValueStore();

        /// <summary>The backing store, so the mapping engine can copy into this container.</summary>
        internal LocalizedValueStore Store => _store;

        /// <summary>
        /// Creates an empty property.
        /// </summary>
        public LocalizedString()
        {
        }

        /// <summary>
        /// Creates a property holding the given literals.
        /// </summary>
        /// <param name="values">The literals. A later value replaces an earlier one with the same tag.</param>
        public LocalizedString(IEnumerable<LangString> values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    Set(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value for exactly this language tag. Getting an absent tag yields <c>null</c>;
        /// setting <c>null</c> removes it.
        /// </summary>
        /// <param name="language">A language tag, such as <c>de</c>.</param>
        /// <returns>The value, or <c>null</c>.</returns>
        public string this[string language]
        {
            get { return _store.FirstValueFor(language); }
            set { _store.Set(language, value); }
        }

        /// <summary>
        /// Gets or sets the value for a culture's language tag.
        /// </summary>
        /// <param name="culture">The culture whose name is the tag.</param>
        /// <returns>The value, or <c>null</c>.</returns>
        public string this[CultureInfo culture]
        {
            get { return this[CultureName(culture)]; }
            set { this[CultureName(culture)] = value; }
        }

        /// <summary>
        /// The untagged literal on this predicate, if any.
        /// </summary>
        /// <remarks>
        /// A plain literal is not "the value in some default language" — it is an RDF term with no tag at
        /// all, and it is kept separate so that reading it can never be confused with a language choice.
        /// </remarks>
        public string Invariant
        {
            get { return _store.Invariant.Count > 0 ? _store.Invariant[0] : null; }
            set { _store.SetInvariant(value); }
        }

        /// <summary>
        /// <c>true</c> when an untagged literal is present.
        /// </summary>
        public bool HasInvariant => _store.Invariant.Count > 0;

        /// <inheritdoc/>
        public IReadOnlyCollection<string> Languages => _store.Languages;

        /// <inheritdoc/>
        public int Count => _store.Count;

        /// <inheritdoc/>
        public bool IsEmpty => _store.IsEmpty;

        /// <summary>
        /// Sets the value for a language, replacing whatever that language held.
        /// </summary>
        /// <param name="language">A language tag.</param>
        /// <param name="value">The value, or <c>null</c> to remove.</param>
        public void Set(string language, string value)
        {
            _store.Set(language, value);
        }

        /// <summary>
        /// Sets a literal, replacing whatever its language held.
        /// </summary>
        /// <param name="value">The literal.</param>
        public void Set(LangString value)
        {
            if (value != null)
            {
                _store.Set(value.Language, value.Value);
            }
        }

        /// <inheritdoc/>
        public bool Contains(string language) => _store.Contains(language);

        /// <inheritdoc/>
        public bool Remove(string language) => _store.Remove(language);

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
        /// Enumerates the tagged literals, ordered by tag. The untagged value is not included; read it
        /// through <see cref="Invariant"/>.
        /// </summary>
        /// <returns>An enumerator over the tagged literals.</returns>
        public IEnumerator<LangString> GetEnumerator() => _store.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns the best value for the current UI culture, so interpolating the property yields text.
        /// </summary>
        public override string ToString() => Best() ?? string.Empty;

        private static string CultureName(CultureInfo culture) =>
            LangString.CultureName(culture, nameof(culture));
    }
}
