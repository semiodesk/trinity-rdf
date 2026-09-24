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
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class LocalizedStringTest
    {
        #region The single-valued container

        [Test]
        public void HoldsEveryLanguageAtOnce()
        {
            var label = new LocalizedString();

            label["de"] = "Hallo Welt";
            label["en"] = "Hello World";

            Assert.AreEqual("Hallo Welt", label["de"]);
            Assert.AreEqual("Hello World", label["en"]);

            // The question the previous design could not answer from the mapped surface at all.
            CollectionAssert.AreEqual(new[] { "de", "en" }, label.Languages);
            Assert.AreEqual(2, label.Count);
        }

        [Test]
        public void NormalizesTagsOnBothSidesOfTheIndexer()
        {
            var label = new LocalizedString();

            label["DE"] = "Hallo";

            Assert.AreEqual("Hallo", label["de"]);
            Assert.AreEqual("Hallo", label["De"]);
            CollectionAssert.AreEqual(new[] { "de" }, label.Languages);
        }

        [Test]
        public void ReturnsNullForAnAbsentLanguage()
        {
            Assert.IsNull(new LocalizedString()["de"]);
        }

        [Test]
        public void SettingNullRemovesTheLanguage()
        {
            var label = new LocalizedString();

            label["de"] = "Hallo";
            label["de"] = null;

            Assert.IsFalse(label.Contains("de"));
            Assert.IsTrue(label.IsEmpty);
        }

        /// <summary>
        /// The reason the indexer does not fall back: a getter that fell back while the setter did not
        /// would make this assignment move a value from one language to another.
        /// </summary>
        [Test]
        public void ReadingAndWritingBackTheSameKeyChangesNothing()
        {
            var label = new LocalizedString();

            label["en"] = "Hello";
            label["de"] = label["de"];

            Assert.IsFalse(label.Contains("de"), "Writing back an absent value must not create one.");
            Assert.AreEqual("Hello", label["en"], "It must not disturb another language either.");
            Assert.AreEqual(1, label.Count);
        }

        [Test]
        public void SettingALanguageTwiceKeepsTheLastValue()
        {
            var label = new LocalizedString();

            label["de"] = "Hallo";
            label["de"] = "Servus";

            Assert.AreEqual("Servus", label["de"]);
            Assert.AreEqual(1, label.Count, "One value per language is the whole point of this container.");
        }

        [Test]
        public void KeepsTheUntaggedLiteralApartFromEveryLanguage()
        {
            var label = new LocalizedString();

            label.Invariant = "42";
            label["de"] = "Hallo";

            Assert.AreEqual("42", label.Invariant);
            Assert.IsTrue(label.HasInvariant);
            Assert.AreEqual(1, label.Count, "Count is tagged values; the untagged one is not a language.");
            CollectionAssert.AreEqual(new[] { "de" }, label.Languages);
            Assert.IsFalse(label.IsEmpty);
        }

        [Test]
        public void IsEmptyOnlyWhenNothingIsPresentAtAll()
        {
            var label = new LocalizedString();

            Assert.IsTrue(label.IsEmpty);

            label.Invariant = "x";

            Assert.IsTrue(label.Count == 0 && !label.IsEmpty,
                "An untagged-only property has no languages but is not empty.");
        }

        [Test]
        public void TakesACulture()
        {
            var label = new LocalizedString();

            label[CultureInfo.GetCultureInfo("de-DE")] = "Hallo";

            Assert.AreEqual("Hallo", label["de-de"]);
            Assert.Throws<ArgumentException>(() => label[CultureInfo.InvariantCulture] = "x");
        }

        [Test]
        public void EnumeratesTaggedLiteralsInAStableOrder()
        {
            var label = new LocalizedString();

            label["en"] = "Hello";
            label["de"] = "Hallo";

            CollectionAssert.AreEqual(
                new[] { "\"Hallo\"@de", "\"Hello\"@en" },
                label.Select(x => x.ToNTriples()).ToList());
        }

        #endregion

        #region Lookup

        /// <summary>
        /// RFC 4647 Lookup truncates the <b>request</b>, so a more specific request finds a more general
        /// value but not the other way round. Asserted as a table because the asymmetry is the part
        /// people expect to work both ways.
        /// </summary>
        [TestCase("de-DE", "de", "Hallo", Description = "Truncates to the base tag.")]
        [TestCase("de-DE-1901", "de", "Hallo", Description = "Truncates repeatedly.")]
        [TestCase("de", "de", "Hallo")]
        [TestCase("de-CH", "de", "Hallo")]
        [TestCase("de", "de-DE", null, Description = "Lookup does not extend the request.")]
        [TestCase("fr", "de", null)]
        [TestCase("*", "de", "Hallo")]
        public void BestFollowsRfc4647Lookup(string request, string stored, string expected)
        {
            var label = new LocalizedString();
            label[stored] = "Hallo";

            Assert.AreEqual(expected, label.Best(request));
        }

        [Test]
        public void BestTruncatesASingletonSubtagWithTheOneBeforeIt()
        {
            var label = new LocalizedString();
            label["de"] = "Hallo";

            // "de-a-xyz" -> drop "xyz" -> the last subtag is the singleton "a", which goes too -> "de".
            Assert.AreEqual("Hallo", label.Best("de-a-xyz"));
        }

        [Test]
        public void BestTakesTheFirstRangeThatMatches()
        {
            var label = new LocalizedString();

            label["en"] = "Hello";
            label["fr"] = "Bonjour";

            Assert.AreEqual("Hello", label.Best("de", "en", "fr"));
            Assert.AreEqual("Bonjour", label.Best("fr", "en"));
        }

        [Test]
        public void BestFallsBackToTheUntaggedValueAndThenToNull()
        {
            var label = new LocalizedString();

            Assert.IsNull(label.Best("de"));

            label.Invariant = "plain";

            Assert.AreEqual("plain", label.Best("de"), "No language matched, so the untagged value answers.");

            label["de"] = "Hallo";

            Assert.AreEqual("Hallo", label.Best("de"), "A matching language wins over the untagged value.");
        }

        [Test]
        public void BestUsesTheCurrentUiCulture()
        {
            var culture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                var label = new LocalizedString();
                label["de"] = "Hallo";
                label["en"] = "Hello";

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

                Assert.AreEqual("Hallo", label.Best());

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

                Assert.AreEqual("Hello", label.Best());
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = culture;
            }
        }

        [Test]
        public void TryGetBestYieldsTheTagAsWell()
        {
            var label = new LocalizedString();
            label["de"] = "Hallo";

            LangString match;

            Assert.IsTrue(label.TryGetBest("de-AT", out match));
            Assert.AreEqual(new LangString("Hallo", "de"), match);

            Assert.IsFalse(label.TryGetBest("fr", out match));
            Assert.IsNull(match);
        }

        #endregion

        #region The multi-valued container

        [Test]
        public void TheCollectionKeepsEveryValueForALanguage()
        {
            var aliases = new LocalizedStringCollection();

            aliases.Add("de", "Erdapfel");
            aliases.Add("de", "Kartoffel");
            aliases.Add("en", "Potato");

            CollectionAssert.AreEqual(new[] { "Erdapfel", "Kartoffel" }, aliases["de"]);
            CollectionAssert.AreEqual(new[] { "Potato" }, aliases["en"]);
            Assert.AreEqual(3, aliases.Count);
            CollectionAssert.AreEqual(new[] { "de", "en" }, aliases.Languages);
        }

        [Test]
        public void TheCollectionReturnsAnEmptyListForAnAbsentLanguage()
        {
            CollectionAssert.IsEmpty(new LocalizedStringCollection()["de"]);
        }

        [Test]
        public void TheCollectionRemovesOneValueOrTheWholeLanguage()
        {
            var aliases = new LocalizedStringCollection();

            aliases.Add("de", "Erdapfel");
            aliases.Add("de", "Kartoffel");

            Assert.IsTrue(aliases.Remove("de", "Erdapfel"));
            CollectionAssert.AreEqual(new[] { "Kartoffel" }, aliases["de"]);

            Assert.IsTrue(aliases.Remove("de"));
            Assert.IsTrue(aliases.IsEmpty);
        }

        [Test]
        public void TheCollectionSetReplacesEveryValueForALanguage()
        {
            var aliases = new LocalizedStringCollection();

            aliases.Add("de", "Erdapfel");
            aliases.Add("de", "Kartoffel");
            aliases.Set("de", "Grundbirne");

            CollectionAssert.AreEqual(new[] { "Grundbirne" }, aliases["de"]);
        }

        [Test]
        public void TheCollectionKeepsSeveralUntaggedLiterals()
        {
            var aliases = new LocalizedStringCollection();

            aliases.AddInvariant("a");
            aliases.AddInvariant("b");

            CollectionAssert.AreEqual(new[] { "a", "b" }, aliases.Invariant);
            Assert.AreEqual(0, aliases.Count);
            Assert.IsFalse(aliases.IsEmpty);
        }

        /// <summary>
        /// The difference that makes two containers worth having: the same data, declared one way, keeps
        /// both values; declared the other, keeps the last.
        /// </summary>
        [Test]
        public void TheTwoContainersDifferExactlyInMultiplicity()
        {
            var values = new[] { new LangString("Erdapfel", "de"), new LangString("Kartoffel", "de") };

            var single = new LocalizedString(values);
            var many = new LocalizedStringCollection(values);

            Assert.AreEqual(1, single.Count);
            Assert.AreEqual("Kartoffel", single["de"], "The scalar container keeps the last value, as a mapped string does.");

            Assert.AreEqual(2, many.Count);
            CollectionAssert.AreEqual(new[] { "Erdapfel", "Kartoffel" }, many["de"]);
        }

        [Test]
        public void BothContainersShareTheLookupRules()
        {
            var single = new LocalizedString();
            var many = new LocalizedStringCollection();

            single["de"] = "Hallo";
            many.Add("de", "Hallo");

            Assert.AreEqual("Hallo", single.Best("de-AT"));
            Assert.AreEqual("Hallo", many.Best("de-AT"));
        }

        #endregion
    }
}
