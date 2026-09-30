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

        /// <summary>
        /// Exercises the whole <see cref="ILocalizedText"/> surface on <b>both</b> implementations.
        /// </summary>
        /// <remarks>
        /// Written because coverage showed the interface was proven on <see cref="LocalizedString"/> and
        /// unproven on <see cref="LocalizedStringCollection"/> — and the mapping wires itself to the
        /// interface, so the collection path would otherwise go in untested. A [TestCaseSource] over the
        /// two keeps them from drifting apart: a member added to one and forgotten on the other fails here.
        /// </remarks>
        [TestCaseSource(nameof(BothContainers))]
        public void TheSharedSurfaceBehavesIdenticallyOnBothContainers(Func<ILocalizedText> make)
        {
            var text = make();

            Assert.IsTrue(text.IsEmpty);
            Assert.AreEqual(0, text.Count);
            CollectionAssert.IsEmpty(text.Languages);
            Assert.IsFalse(text.Contains("de"));
            Assert.IsNull(text.Best());
            Assert.IsNull(text.Best("de"));

            LangString match;
            Assert.IsFalse(text.TryGetBest("de", out match));
            Assert.IsNull(match);

            Fill(text, "de", "Hallo");
            Fill(text, "en", "Hello");

            Assert.IsFalse(text.IsEmpty);
            Assert.AreEqual(2, text.Count);
            CollectionAssert.AreEqual(new[] { "de", "en" }, text.Languages);
            Assert.IsTrue(text.Contains("DE"), "Lookup normalizes the tag.");

            Assert.IsTrue(text.TryGetBest("de-AT", out match));
            Assert.AreEqual(new LangString("Hallo", "de"), match);
            Assert.AreEqual("Hallo", text.Best("de-AT", "en"));

            CollectionAssert.AreEqual(
                new[] { "\"Hallo\"@de", "\"Hello\"@en" },
                text.Select(x => x.ToNTriples()).ToList(),
                "Enumeration is the tagged literals, ordered by tag.");

            // The non-generic enumerator is what a foreach over IEnumerable uses.
            var loose = new List<object>();
            foreach (var value in (System.Collections.IEnumerable)text)
            {
                loose.Add(value);
            }
            Assert.AreEqual(2, loose.Count);

            Assert.IsTrue(text.Remove("de"));
            Assert.IsFalse(text.Remove("de"), "Removing an absent language reports that it did nothing.");
            Assert.AreEqual(1, text.Count);

            text.Clear();

            Assert.IsTrue(text.IsEmpty);
            CollectionAssert.IsEmpty(text.Languages);
        }

        /// <summary>
        /// <c>ToString()</c> yields text for the current UI culture on both containers, so interpolating a
        /// mapped property never prints a type name.
        /// </summary>
        [TestCaseSource(nameof(BothContainers))]
        public void ToStringYieldsTextForTheCurrentCulture(Func<ILocalizedText> make)
        {
            var culture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                var text = make();

                Assert.AreEqual(string.Empty, $"{text}", "An empty property interpolates to nothing, not null.");

                Fill(text, "de", "Hallo");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

                Assert.AreEqual("Hallo", $"{text}");
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = culture;
            }
        }

        [TestCaseSource(nameof(BothContainers))]
        public void RefusesALanguageThatNamesNothing(Func<ILocalizedText> make)
        {
            var text = make();

            Assert.Throws<ArgumentNullException>(() => text.Contains(null));
            Assert.Throws<ArgumentException>(() => text.Contains(""));
            Assert.Throws<ArgumentException>(() => text.Contains("   "));

            // A blank range is a question with no answer, not an error - Best is a lookup, not a lookup key.
            Assert.IsNull(text.Best((string)null));
            Assert.IsNull(text.Best(""));
            Assert.IsNull(text.Best((string[])null));
        }

        private static IEnumerable<TestCaseData> BothContainers()
        {
            yield return new TestCaseData(
                new Func<ILocalizedText>(() => new LocalizedString())).SetName("{m}(LocalizedString)");
            yield return new TestCaseData(
                new Func<ILocalizedText>(() => new LocalizedStringCollection())).SetName("{m}(LocalizedStringCollection)");
        }

        private static void Fill(ILocalizedText text, string language, string value)
        {
            if (text is LocalizedString single)
            {
                single.Set(language, value);
            }
            else
            {
                ((LocalizedStringCollection)text).Add(language, value);
            }
        }

        #endregion

        #region Members that only one container has

        [Test]
        public void TheScalarContainerReadsAndWritesThroughACulture()
        {
            var label = new LocalizedString();
            var german = CultureInfo.GetCultureInfo("de-DE");

            label[german] = "Hallo";

            Assert.AreEqual("Hallo", label[german], "The culture indexer must read as well as write.");
            Assert.Throws<ArgumentNullException>(() => { var _ = label[(CultureInfo)null]; });
        }

        [Test]
        public void TheCollectionReadsThroughACultureAndRefusesTheInvariantOne()
        {
            var aliases = new LocalizedStringCollection();
            aliases.Add("de-DE", "Erdapfel");

            CollectionAssert.AreEqual(new[] { "Erdapfel" }, aliases[CultureInfo.GetCultureInfo("de-DE")]);
            Assert.Throws<ArgumentNullException>(() => { var _ = aliases[(CultureInfo)null]; });
            Assert.Throws<ArgumentException>(() => { var _ = aliases[CultureInfo.InvariantCulture]; });
        }

        [Test]
        public void TheCollectionAddsAndRemovesLiteralsAndUntaggedValues()
        {
            var aliases = new LocalizedStringCollection();

            aliases.Add(new LangString("Erdapfel", "de"));

            Assert.IsFalse(aliases.HasInvariant);

            aliases.AddInvariant("plain");

            Assert.IsTrue(aliases.HasInvariant);
            Assert.IsTrue(aliases.RemoveInvariant("plain"));
            Assert.IsFalse(aliases.RemoveInvariant("plain"), "Removing it twice reports that it did nothing.");
            Assert.IsFalse(aliases.HasInvariant);

            Assert.Throws<ArgumentNullException>(() => aliases.Add((LangString)null));
            Assert.Throws<ArgumentNullException>(() => aliases.AddInvariant(null));
        }

        [Test]
        public void TheScalarContainerRemovesAndClears()
        {
            var label = new LocalizedString();

            label["de"] = "Hallo";
            label.Invariant = "plain";

            Assert.IsTrue(label.Remove("de"));
            Assert.IsFalse(label.Remove("de"));
            Assert.IsFalse(label.IsEmpty, "The untagged value survives removing a language.");

            label.Clear();

            Assert.IsTrue(label.IsEmpty);
            Assert.IsNull(label.Invariant, "Clear removes the untagged value too.");
        }

        #endregion

        #region Cross-container

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

        #region Container ownership

        /// <summary>
        /// Assigning a container copies into the one the mapping owns rather than replacing it.
        /// </summary>
        /// <remarks>
        /// The container is a mutable view owned by the mapping for its lifetime. Replacing the
        /// reference aliased one container across two resources, so editing either edited both -- a
        /// data-corrupting bug with no error anywhere. TRIN009 warns about a container property that
        /// declares a setter, but a hand-written mapping never reaches the generator, so the runtime
        /// has to hold this on its own (ADR-0048).
        /// </remarks>
        [Test]
        public void AssigningAContainerCopiesItRatherThanSharingIt()
        {
            var a = new LocalizedMappingTestClass(new Uri("semio:test:a"));
            var b = new LocalizedMappingTestClass(new Uri("semio:test:b"));

            b.Title["de"] = "B";

            LocalizedString before = a.Title;

            a.Title = b.Title;

            Assert.AreSame(before, a.Title, "The mapping keeps its own container instance.");
            Assert.AreEqual("B", a.Title["de"], "...but takes on the assigned contents.");

            a.Title["de"] = "A";

            Assert.AreEqual("B", b.Title["de"], "Editing one resource must not touch the other.");
        }

        /// <summary>
        /// Assigning null empties the container instead of discarding it.
        /// </summary>
        /// <remarks>
        /// A null container is not merely an odd state: every subsequent read enumerates the mapping,
        /// so <c>ListValues()</c> threw a NullReferenceException and took <c>Commit()</c>,
        /// <c>HasUnsavedChanges()</c> and the commit snapshot down with it.
        /// </remarks>
        [Test]
        public void AssigningNullEmptiesTheContainerRatherThanDiscardingIt()
        {
            var r = new LocalizedMappingTestClass(new Uri("semio:test:n"));

            r.Title["de"] = "Hallo";
            r.Title.Invariant = "plain";

            LocalizedString before = r.Title;

            r.Title = null;

            Assert.AreSame(before, r.Title);
            Assert.IsNotNull(r.Title);
            Assert.IsTrue(r.Title.IsEmpty, "Assigning null means the property holds nothing.");

            // The operations that used to fail on a null container.
            Assert.DoesNotThrow(() => r.ListValues().ToList());
            Assert.DoesNotThrow(() => r.HasUnsavedChanges());
        }

        /// <summary>
        /// Only the two built-in containers can be mapped; any other ILocalizedText is refused.
        /// </summary>
        /// <remarks>
        /// The engine dispatches on the concrete container types to add a value and to copy one
        /// container into another, so a third implementation was accepted at registration and then
        /// silently dropped every value it was given. The interface itself failed later and more
        /// obscurely still: Activator cannot instantiate an interface.
        /// </remarks>
        [Test]
        public void RefusesAnILocalizedTextThatIsNotABuiltInContainer()
        {
            var thrown = Assert.Throws<ArgumentException>(
                () => new PropertyMapping<ILocalizedText>("Label", to.localizedStringTestString));

            StringAssert.Contains(nameof(LocalizedString), thrown.Message);
            StringAssert.Contains(nameof(LocalizedStringCollection), thrown.Message);
        }

        /// <summary>
        /// A value a single-valued container drops is orphaned in the store, not deleted from it.
        /// </summary>
        /// <remarks>
        /// ADR-0048 originally claimed the opposite - that the delta computes removals from what the
        /// resource now holds, so a later Commit() deletes the dropped value. It does not: the commit
        /// snapshot is taken from ListValues(), which is the resource *after* the container dropped
        /// the duplicate, so the value is in neither side of the delta. This test is what the claim
        /// rests on now, rather than the reasoning.
        /// </remarks>
        [Test]
        public void AValueDroppedByASingleValuedContainerSurvivesInTheStore()
        {
            var store = StoreFactory.CreateStore("provider=dotnetrdf");
            var model = store.CreateModel(new Uri("http://example.org/orphan"));
            model.Clear();

            var uri = new Uri("semio:test:orphan");
            var p = to.localizedStringTest;

            var raw = model.CreateResource(uri);
            raw.AddProperty(p, "Erste", "de");
            raw.AddProperty(p, "Zweite", "de");
            raw.Commit();

            Assert.AreEqual(2, StoredValues(model, uri, p).Count, "Both values start out in the store.");

            var mapped = model.GetResource<LocalizedMappingTestClass>(uri);

            Assert.AreEqual(1, mapped.Title.Count, "The container keeps one value per language.");
            Assert.IsFalse(mapped.HasUnsavedChanges(),
                "Dropping a value on read is not a pending change, which is why nothing removes it.");

            mapped.Commit();

            Assert.AreEqual(2, StoredValues(model, uri, p).Count,
                "The dropped value is orphaned - invisible through the property, still in the store.");
        }

        private static List<string> StoredValues(IModel model, Uri subject, Property property)
        {
            var query = new SparqlQuery(
                $"SELECT ?o WHERE {{ <{subject.OriginalString}> <{property.Uri.OriginalString}> ?o }}");

            return model.ExecuteQuery(query).GetBindings().Select(b => b["o"].ToString()).ToList();
        }

        #endregion
    }
}
