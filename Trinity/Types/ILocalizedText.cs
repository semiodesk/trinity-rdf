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

using System.Collections.Generic;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// The read surface shared by the localized-text containers, <see cref="LocalizedString"/> and
    /// <see cref="LocalizedStringCollection"/>.
    /// </summary>
    /// <remarks>
    /// The two differ only in multiplicity — one value per language, or several — which is the same
    /// choice an author already makes between <c>string</c> and <c>List&lt;string&gt;</c> one level up
    /// (ADR-0048). Everything that does not depend on that choice lives here, including the question the
    /// previous design could not answer at all: <see cref="Languages"/>.
    /// </remarks>
    public interface ILocalizedText : IEnumerable<LangString>
    {
        /// <summary>
        /// The distinct language tags carried by this property, ordered. Never contains an entry for the
        /// untagged value.
        /// </summary>
        IReadOnlyCollection<string> Languages { get; }

        /// <summary>
        /// The number of tagged values. The untagged value, if any, is not counted.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// <c>true</c> when this property carries nothing at all — no tagged value and no untagged one.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Indicates whether a value is present for exactly this language tag.
        /// </summary>
        /// <param name="language">A language tag.</param>
        /// <returns><c>true</c> if a value carries that tag.</returns>
        bool Contains(string language);

        /// <summary>
        /// Removes every value carrying this language tag.
        /// </summary>
        /// <param name="language">A language tag.</param>
        /// <returns><c>true</c> if anything was removed.</returns>
        bool Remove(string language);

        /// <summary>
        /// Removes every value, tagged and untagged.
        /// </summary>
        void Clear();

        /// <summary>
        /// The best value for the current UI culture, falling back to the untagged value.
        /// </summary>
        /// <returns>The value, or <c>null</c> when nothing matches.</returns>
        string Best();

        /// <summary>
        /// The best value for an ordered list of language ranges, falling back to the untagged value.
        /// </summary>
        /// <param name="languageRanges">Language ranges, most preferred first. <c>*</c> matches anything.</param>
        /// <returns>The value, or <c>null</c> when nothing matches.</returns>
        string Best(params string[] languageRanges);

        /// <summary>
        /// Looks up the best match for a single language range, tag included.
        /// </summary>
        /// <param name="languageRange">A language range.</param>
        /// <param name="match">The matching literal, or <c>null</c>.</param>
        /// <returns><c>true</c> if a match was found.</returns>
        bool TryGetBest(string languageRange, out LangString match);
    }
}
