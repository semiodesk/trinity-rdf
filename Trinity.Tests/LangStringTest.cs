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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class LangStringTest
    {
        #region Construction and normalization

        [Test]
        public void KeepsTheValueAndNormalizesTheTag()
        {
            var s = new LangString("Hallo Welt", "DE");

            Assert.AreEqual("Hallo Welt", s.Value);
            Assert.AreEqual("de", s.Language, "The tag is normalized to lower case at construction.");
        }

        [Test]
        public void NormalizesASubtaggedTag()
        {
            Assert.AreEqual("en-gb", new LangString("Colour", "en-GB").Language);
        }

        /// <summary>
        /// Pins <c>ToLowerInvariant</c> rather than <c>ToLower</c>. Under <c>tr-TR</c> the culture-sensitive
        /// overload maps 'I' to the dotless 'ı', which would silently produce a tag no store ever sees --
        /// the hazard the old <c>Resource.Language</c> setter carried.
        /// </summary>
        [Test]
        public void NormalizesTheTagInvariantlyUnderATurkishCulture()
        {
            var culture = Thread.CurrentThread.CurrentCulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

                Assert.AreEqual("id", new LangString("Halo Dunia", "ID").Language);
                Assert.AreEqual("is", new LangString("Halló", "IS").Language);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Test]
        public void TakesTheTagFromACulture()
        {
            Assert.AreEqual("de", new LangString("Hallo", CultureInfo.GetCultureInfo("de")).Language);
            Assert.AreEqual("en-us", new LangString("Color", CultureInfo.GetCultureInfo("en-US")).Language);
        }

        /// <summary>
        /// An empty tag would serialize to <c>'x'@</c>, which is not valid SPARQL. The invariant culture
        /// names no language, so it is refused rather than quietly written.
        /// </summary>
        [Test]
        public void RefusesATagThatNamesNoLanguage()
        {
            Assert.Throws<ArgumentNullException>(() => new LangString("x", (string)null));
            Assert.Throws<ArgumentNullException>(() => new LangString("x", (CultureInfo)null));
            Assert.Throws<ArgumentException>(() => new LangString("x", ""));
            Assert.Throws<ArgumentException>(() => new LangString("x", "   "));
            Assert.Throws<ArgumentException>(() => new LangString("x", CultureInfo.InvariantCulture));
        }

        [Test]
        public void RefusesANullValue()
        {
            Assert.Throws<ArgumentNullException>(() => new LangString(null, "de"));
        }

        [Test]
        public void AcceptsAnEmptyValue()
        {
            Assert.AreEqual(string.Empty, new LangString("", "de").Value,
                "An empty string is a legal RDF literal; only the tag may not be empty.");
        }

        #endregion

        #region Equality

        [Test]
        public void EqualLiteralsAreEqualRegardlessOfTagCasing()
        {
            var a = new LangString("Hallo", "de");
            var b = new LangString("Hallo", "DE");

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void DiffersByValueAndByTag()
        {
            var a = new LangString("Hallo", "de");

            Assert.IsFalse(a.Equals(new LangString("Hello", "de")));
            Assert.IsFalse(a.Equals(new LangString("Hallo", "en")));
            Assert.IsFalse(a.Equals(null));
        }

        /// <summary>
        /// A tagged literal and its untagged twin are distinct RDF terms. Because an untagged literal is a
        /// plain <c>string</c>, this now holds at the type level and cannot be argued with.
        /// </summary>
        [Test]
        public void IsNeverEqualToThePlainString()
        {
            var tagged = new LangString("Hallo", "de");

            Assert.IsFalse(tagged.Equals("Hallo"));
            Assert.IsFalse("Hallo".Equals(tagged));
        }

        /// <summary>
        /// The ADR-0025 regression guard. Operators bind statically, so a class without an explicit
        /// <c>==</c> compares by reference and reports <c>false</c> for equal literals.
        /// </summary>
        [Test]
        public void OperatorEqualsIsValueEqualityNotReferenceEquality()
        {
            var a = new LangString("Hallo", "de");
            var b = new LangString("Hallo", "de");

            Assert.IsFalse(ReferenceEquals(a, b), "The test is meaningless if these are the same instance.");
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
        }

        [Test]
        public void OperatorEqualsHandlesNullOnEitherSide()
        {
            LangString nothing = null;
            var something = new LangString("Hallo", "de");

            Assert.IsTrue(nothing == null);
            Assert.IsFalse(something == null);
            Assert.IsFalse(null == something);
            Assert.IsTrue(something != null);
        }

        /// <summary>
        /// The property bag is a <c>HashSet&lt;object&gt;</c>, so identity there is what decides whether a
        /// value is a duplicate, and whether removing it works.
        /// </summary>
        [Test]
        public void DeduplicatesInAnObjectSet()
        {
            var set = new HashSet<object>
            {
                new LangString("Hallo", "de"),
                new LangString("Hallo", "DE"),
                new LangString("Hallo", "en"),
                "Hallo"
            };

            Assert.AreEqual(3, set.Count,
                "The two German literals collapse; the English one and the plain string stay distinct.");
            Assert.IsTrue(set.Remove(new LangString("Hallo", "de")), "A value must be removable by equality.");
        }

        [Test]
        public void OrdersByTagThenValue()
        {
            var sorted = new[]
                {
                    new LangString("b", "en"),
                    new LangString("a", "en"),
                    new LangString("z", "de")
                }
                .OrderBy(x => x)
                .Select(x => x.ToNTriples())
                .ToList();

            CollectionAssert.AreEqual(
                new[] { "\"z\"@de", "\"a\"@en", "\"b\"@en" }, sorted);
        }

        /// <summary>
        /// Sorting a list that contains a null must not throw, and nulls sort first - the ordering
        /// contract every IComparable is expected to keep.
        /// </summary>
        [Test]
        public void OrdersNullBeforeAnyLiteral()
        {
            var value = new LangString("a", "de");

            Assert.AreEqual(1, value.CompareTo(null));

            var sorted = new List<LangString> { value, null }.OrderBy(x => x).ToList();

            Assert.IsNull(sorted[0]);
            Assert.AreEqual(value, sorted[1]);
        }

        #endregion

        #region Language matching

        [TestCase("de", "de", true)]
        [TestCase("de-de", "de", true)]
        [TestCase("de-de-1901", "de", true)]
        [TestCase("de-de", "de-DE", true)]
        [TestCase("de", "de-DE", false, Description = "Basic filtering extends the tag, not the range.")]
        [TestCase("deu", "de", false, Description = "A range only matches at a subtag boundary.")]
        [TestCase("den", "de", false)]
        [TestCase("en", "de", false)]
        [TestCase("de", "*", true)]
        [TestCase("de", "", false)]
        [TestCase("de", null, false)]
        public void MatchesLanguageFollowsBasicFiltering(string tag, string range, bool expected)
        {
            Assert.AreEqual(expected, new LangString("x", tag).MatchesLanguage(range));
        }

        #endregion

        #region Rendering

        [Test]
        public void ToStringIsTheLexicalFormAndToNTriplesCarriesTheTag()
        {
            var s = new LangString("Hallo", "de");

            Assert.AreEqual("Hallo", s.ToString(), "Interpolating a value must yield the text, not its RDF notation.");
            Assert.AreEqual("Hallo", $"{s}");
            Assert.AreEqual("\"Hallo\"@de", s.ToNTriples());
        }

        #endregion
    }
}
