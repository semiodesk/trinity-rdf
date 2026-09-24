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
using System.Globalization;
using System.Linq;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// The storage and lookup shared by <see cref="LocalizedString"/> and
    /// <see cref="LocalizedStringCollection"/>.
    /// </summary>
    /// <remarks>
    /// The two containers differ only in how many values they let a language carry, so everything else —
    /// storage, tag normalization, RFC 4647 lookup — lives here once. That matters more than the code it
    /// saves: a second copy of the matching rules is exactly how the four representations ADR-0047
    /// replaced came to disagree with each other.
    /// </remarks>
    internal sealed class LocalizedValueStore
    {
        /// <summary>Tagged values, in arrival order.</summary>
        private readonly List<LangString> _tagged = new List<LangString>();

        /// <summary>Untagged (plain) literals on the same predicate, in arrival order.</summary>
        private readonly List<string> _invariant = new List<string>();

        internal int Count => _tagged.Count;

        internal bool IsEmpty => _tagged.Count == 0 && _invariant.Count == 0;

        internal IReadOnlyList<LangString> Tagged => _tagged;

        internal IReadOnlyList<string> Invariant => _invariant;

        internal IReadOnlyCollection<string> Languages =>
            _tagged.Select(x => x.Language).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>
        /// Normalizes a tag the same way <see cref="LangString"/> does, so a lookup and a stored value
        /// cannot disagree about casing.
        /// </summary>
        internal static string Normalize(string language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("The language tag must not be empty.", nameof(language));
            }

            return language.Trim().ToLowerInvariant();
        }

        internal bool Contains(string language)
        {
            var tag = Normalize(language);

            return _tagged.Any(x => x.Language == tag);
        }

        internal IReadOnlyList<string> ValuesFor(string language)
        {
            var tag = Normalize(language);

            return _tagged.Where(x => x.Language == tag).Select(x => x.Value).ToList();
        }

        internal string FirstValueFor(string language)
        {
            var tag = Normalize(language);

            return _tagged.FirstOrDefault(x => x.Language == tag)?.Value;
        }

        internal void Add(LangString value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _tagged.Add(value);
        }

        internal void Add(string language, string value)
        {
            Add(new LangString(value, language));
        }

        /// <summary>
        /// Replaces every value carrying this tag with one value, or removes them all when
        /// <paramref name="value"/> is <c>null</c>.
        /// </summary>
        internal void Set(string language, string value)
        {
            Remove(language);

            if (value != null)
            {
                _tagged.Add(new LangString(value, language));
            }
        }

        internal bool Remove(string language)
        {
            var tag = Normalize(language);

            return _tagged.RemoveAll(x => x.Language == tag) > 0;
        }

        internal bool Remove(string language, string value)
        {
            var tag = Normalize(language);

            return _tagged.RemoveAll(x => x.Language == tag && x.Value == value) > 0;
        }

        internal void AddInvariant(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _invariant.Add(value);
        }

        internal void SetInvariant(string value)
        {
            _invariant.Clear();

            if (value != null)
            {
                _invariant.Add(value);
            }
        }

        internal bool RemoveInvariant(string value)
        {
            return _invariant.Remove(value);
        }

        internal void Clear()
        {
            _tagged.Clear();
            _invariant.Clear();
        }

        internal IEnumerator<LangString> GetEnumerator()
        {
            return _tagged
                .OrderBy(x => x.Language, StringComparer.Ordinal)
                .ThenBy(x => x.Value, StringComparer.Ordinal)
                .GetEnumerator();
        }

        /// <summary>
        /// RFC 4647 §3.4 Lookup against an ordered preference list, then the untagged value.
        /// </summary>
        internal string Best(params string[] languageRanges)
        {
            if (languageRanges != null)
            {
                foreach (var range in languageRanges)
                {
                    LangString match;

                    if (TryGetBest(range, out match))
                    {
                        return match.Value;
                    }
                }
            }

            return _invariant.FirstOrDefault();
        }

        internal string BestForCurrentCulture()
        {
            var name = CultureInfo.CurrentUICulture.Name;

            // The invariant culture names no language, so there is nothing to look up.
            return string.IsNullOrEmpty(name) ? _invariant.FirstOrDefault() : Best(name);
        }

        /// <summary>
        /// RFC 4647 §3.4 Lookup for a single range: try the range, then progressively drop its last
        /// subtag — and a trailing singleton with it — until something matches or nothing is left.
        /// </summary>
        /// <remarks>
        /// Lookup truncates the <b>request</b>, not the available tags, so <c>de-DE</c> finds a <c>de</c>
        /// value but <c>de</c> does not find a <c>de-DE</c> one. That asymmetry is the specified
        /// behaviour and is deliberate here; it is also why this is not the same operation as
        /// <see cref="LangString.MatchesLanguage"/>, which is basic filtering and matches the other way.
        /// </remarks>
        internal bool TryGetBest(string languageRange, out LangString match)
        {
            match = null;

            if (string.IsNullOrWhiteSpace(languageRange))
            {
                return false;
            }

            var range = languageRange.Trim().ToLowerInvariant();

            if (range == "*")
            {
                match = _tagged
                    .OrderBy(x => x.Language, StringComparer.Ordinal)
                    .ThenBy(x => x.Value, StringComparer.Ordinal)
                    .FirstOrDefault();

                return match != null;
            }

            while (true)
            {
                var candidate = _tagged.FirstOrDefault(x => x.Language == range);

                if (candidate != null)
                {
                    match = candidate;

                    return true;
                }

                var cut = range.LastIndexOf('-');

                if (cut < 0)
                {
                    return false;
                }

                range = range.Substring(0, cut);

                // "If the last subtag is a single character, remove it too" - RFC 4647 3.4.
                if (range.Length >= 2 && range[range.Length - 2] == '-')
                {
                    cut = range.LastIndexOf('-');

                    if (cut < 0)
                    {
                        return false;
                    }

                    range = range.Substring(0, cut);
                }
            }
        }
    }
}
