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
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015-2020

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Semiodesk.Trinity.Query.Sparql;
using Semiodesk.Trinity.Tests.Linq;

namespace Semiodesk.Trinity.Tests.Query.Sparql
{
    /// <summary>
    /// End-to-end vertical-slice tests for the new SPARQL LINQ provider (Where / OfType / OrderBy /
    /// Skip / Take / Any / Count / First over an in-memory store). This is the exemplar the operator
    /// breadth is extended from.
    /// </summary>
    [TestFixture]
    public class SparqlLinqExemplarTest
    {
        private IStore _store;

        private IModel _model;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OntologyDiscovery.AddAssembly(typeof(Person).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Person).Assembly);
            OntologyDiscovery.AddAssembly(typeof(Resource).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Resource).Assembly);

            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            _model = _store.CreateModel(new Uri("http://example.org/exemplar"));
            _model.Clear();

            Group spiders = _model.CreateResource<Group>(ex.TheSpiders);
            spiders.Name = "The Spiders";
            spiders.Commit();

            Agent john = _model.CreateResource<Agent>(ex.John);
            john.FirstName = "John";
            john.Commit();

            Person alice = _model.CreateResource<Person>(ex.Alice);
            alice.FirstName = "Alice";
            alice.Age = 69;
            alice.Group = spiders;
            alice.Commit();

            Person bob = _model.CreateResource<Person>(ex.Bob);
            bob.FirstName = "Bob";
            bob.Age = 76;
            bob.Commit();

            Person eve = _model.CreateResource<Person>(ex.Eve);
            eve.FirstName = "Eve";
            eve.Age = 38;
            eve.Commit();

            // Two documents whose localized titles share a lexical form but differ in language, so a
            // query that ignored the tag would return both and one that honoured it returns one.
            Document german = _model.CreateResource<Document>(new Uri("http://example.org/doc/de"));
            german.Title = "Bericht";
            german.LocalizedTitle["de"] = "Bericht";
            german.Commit();

            Document english = _model.CreateResource<Document>(new Uri("http://example.org/doc/en"));
            english.Title = "Report";
            english.LocalizedTitle["en"] = "Bericht";
            english.LocalizedTitle["de"] = "Jahresbericht";
            english.Alternative = new LangString("Annual report", "en");
            english.Commit();

            // A region subtag, which is where stores stop agreeing: Jena canonicalizes a tag it was
            // given as "de-de" back to "de-DE", RDF4J returns it as written. A LANG() comparison
            // against a lower-cased constant therefore matches on some backends and not others, and
            // bare "de"/"en" tags -- which is all the rest of this fixture uses -- never show it.
            Document austrian = _model.CreateResource<Document>(new Uri("http://example.org/doc/at"));
            austrian.LocalizedTitle["de-AT"] = "Jahresbericht";
            austrian.Commit();

            // A tagged literal on the same predicate a mapped string maps. Projecting that string used
            // to throw InvalidCastException for every row because one resource carried a tag
            // (ADR-0048 defect 3).
            Document tagged = _model.CreateResource<Document>(new Uri("http://example.org/doc/tagged"));
            tagged.Title = "Tagged";
            tagged.AddProperty(
                new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#label")), "Markiert", "de");
            tagged.Commit();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _store?.Dispose();
        }

        /// <summary>
        /// A localized property is queried one language at a time, and the tag is part of the match.
        /// </summary>
        /// <remarks>
        /// Both documents carry the lexical form "Bericht" - one tagged @de, one @en - so a translator
        /// that dropped the tag would return both. Before ADR-0048 the translator could not express a
        /// tag at all: LiteralTerm carried one and the writer could emit it, but every construction site
        /// passed null.
        /// </remarks>
        [Test]
        public void FiltersALocalizedPropertyByLanguage()
        {
            var german = _model.AsSparqlQueryable<Document>()
                .Where(d => d.LocalizedTitle["de"] == "Bericht")
                .ToList();

            Assert.AreEqual(1, german.Count);
            Assert.AreEqual(new Uri("http://example.org/doc/de"), german[0].Uri);

            var english = _model.AsSparqlQueryable<Document>()
                .Where(d => d.LocalizedTitle["en"] == "Bericht")
                .ToList();

            Assert.AreEqual(1, english.Count);
            Assert.AreEqual(new Uri("http://example.org/doc/en"), english[0].Uri);
        }

        /// <summary>
        /// The tag is matched, not ignored: asking for a language nothing carries returns nothing, even
        /// though the lexical form exists under another tag.
        /// </summary>
        [Test]
        public void DoesNotMatchTheSameTextUnderAnotherLanguage()
        {
            var french = _model.AsSparqlQueryable<Document>()
                .Where(d => d.LocalizedTitle["fr"] == "Bericht")
                .ToList();

            CollectionAssert.IsEmpty(french);
        }

        /// <summary>
        /// A mapped string is untagged, so it never matches a tagged literal - the counterpart to the
        /// test above, and the reason the translator emits a plain term for it.
        /// </summary>
        [Test]
        public void AMappedStringDoesNotMatchATaggedLiteral()
        {
            var byPlainTitle = _model.AsSparqlQueryable<Document>()
                .Where(d => d.Title == "Jahresbericht")
                .ToList();

            CollectionAssert.IsEmpty(byPlainTitle, "Jahresbericht exists only as a @de literal.");
        }

        /// <summary>
        /// Indexing with a tag that varies per row is refused rather than mistranslated: the tag becomes
        /// part of the query text, which is built once.
        /// </summary>
        /// <remarks>
        /// The tag has to depend on the row to reach this. A closure over a local - even
        /// <c>tags[0].ToUpperInvariant()</c> - is folded to a constant by the partial evaluator before
        /// the translator sees it, and is therefore supported rather than refused.
        /// </remarks>
        [Test]
        public void RefusesALanguageTagThatVariesPerRow()
        {
            Assert.Throws<NotSupportedException>(() =>
                _model.AsSparqlQueryable<Document>()
                    .Where(d => d.LocalizedTitle[d.Title] == "Bericht")
                    .ToList());
        }

        /// <summary>
        /// A tag computed from a closure is folded to a constant before translation, so it works.
        /// </summary>
        [Test]
        public void AcceptsALanguageTagFoldedToAConstant()
        {
            var tags = new[] { "DE", "en" };

            var german = _model.AsSparqlQueryable<Document>()
                .Where(d => d.LocalizedTitle[tags[0].ToLowerInvariant()] == "Bericht")
                .ToList();

            Assert.AreEqual(1, german.Count);
            Assert.AreEqual(new Uri("http://example.org/doc/de"), german[0].Uri);
        }

        /// <summary>
        /// Projecting one language is refused rather than approximated: the bound variable carries every
        /// language of the property, so projecting it would silently return the wrong rows.
        /// </summary>
        [Test]
        public void RefusesProjectingASingleLanguage()
        {
            var thrown = Assert.Throws<NotSupportedException>(() =>
                _model.AsSparqlQueryable<Document>()
                    .Select(d => d.LocalizedTitle["de"])
                    .ToList());

            StringAssert.Contains("localized", thrown.Message);
        }

        /// <summary>
        /// Projecting a mapped string whose predicate also carries tagged literals returns the untagged
        /// text only - it neither throws nor leaks the tagged values.
        /// </summary>
        /// <remarks>
        /// ADR-0048 defect 3: a tagged literal binds as a LangString, which is not IConvertible, so
        /// Convert.ChangeType raised InvalidCastException and the whole projection failed because some
        /// other resource happened to carry a tag on the same predicate. Unwrapping it to its lexical
        /// form fixed the exception and introduced a quieter bug in its place: the projection then
        /// returned tagged values as though they were untagged, which is the very leak that projecting
        /// a single language is refused for. The mapped string is constrained to the empty tag when it
        /// is bound, so neither happens.
        /// <para>
        /// The absence assertion is the whole point. Asserting only that the untagged values are
        /// present passes under all three behaviours.
        /// </para>
        /// </remarks>
        [Test]
        public void ProjectsAPredicateThatAlsoCarriesTaggedLiterals()
        {
            var titles = _model.AsSparqlQueryable<Document>()
                .Select(d => d.Title)
                .ToList();

            CollectionAssert.Contains(titles, "Bericht");
            CollectionAssert.Contains(titles, "Tagged");
            CollectionAssert.DoesNotContain(titles, "Markiert",
                "'Markiert' exists only as a @de literal; a mapped string must not project it.");
        }

        /// <summary>
        /// Two languages of the same property in one predicate each get their own variable.
        /// </summary>
        /// <remarks>
        /// The binding used to be cached by predicate path alone, so both indexers resolved to one
        /// variable and the two constraints met on it as <c>LANG(?v) = "de" AND LANG(?v) = "en"</c> -
        /// unsatisfiable, so a query with an obvious answer returned nothing.
        /// </remarks>
        [Test]
        public void MatchesTwoLanguagesOfTheSamePropertyInOnePredicate()
        {
            var both = _model.AsSparqlQueryable<Document>()
                .Where(d => d.LocalizedTitle["de"] == "Jahresbericht" && d.LocalizedTitle["en"] == "Bericht")
                .ToList();

            Assert.AreEqual(1, both.Count);
            Assert.AreEqual(new Uri("http://example.org/doc/en"), both[0].Uri);
        }

        /// <summary>
        /// A string function over a localized property honours the tag, like equality does.
        /// </summary>
        /// <remarks>
        /// The tag used to be attached only by the equality branch, so every other consumer of the
        /// bound variable - string functions, IN, ORDER BY, a comparison against another member - read
        /// a variable bound to every language of the property at once. StartsWith is the cheapest
        /// witness: "Bericht" is a @de title on one document and an @en title on another.
        /// </remarks>
        [Test]
        public void AStringFunctionOverALocalizedPropertyHonoursTheTag()
        {
            var german = _model.AsSparqlQueryable<Document>()
                .Where(d => d.LocalizedTitle["de"].StartsWith("Bericht"))
                .ToList();

            Assert.AreEqual(1, german.Count);
            Assert.AreEqual(new Uri("http://example.org/doc/de"), german[0].Uri);

            var french = _model.AsSparqlQueryable<Document>()
                .Where(d => d.LocalizedTitle["fr"].Contains("Bericht"))
                .ToList();

            CollectionAssert.IsEmpty(french, "No document carries an @fr title.");
        }

        /// <summary>
        /// Ordering by a localized property orders within one language rather than across all of them.
        /// </summary>
        [Test]
        public void OrdersWithinOneLanguage()
        {
            var ordered = _model.AsSparqlQueryable<Document>()
                .OrderBy(d => d.LocalizedTitle["de"])
                .ToList();

            // Exactly the documents carrying an *exact* @de title, ordered by it. doc/at is absent
            // because its tag is @de-AT and the indexer is exact-match on get (ADR-0048) - a lookup
            // with fallback is Best(), which is refused in a query. doc/tagged has no dcterms:title
            // at all. An unconstrained variable would bind the @en and @de-AT titles too, pulling
            // those documents in and sorting the rows by whichever language happened to bind.
            CollectionAssert.AreEqual(
                new[]
                {
                    new Uri("http://example.org/doc/de"),
                    new Uri("http://example.org/doc/en")
                },
                ordered.Select(d => d.Uri).ToList());
        }

        /// <summary>
        /// A string function over a mapped string does not match tagged literals either.
        /// </summary>
        /// <remarks>
        /// Equality already behaved, because a plain literal term only matches a plain literal. String
        /// functions do not: SPARQL argument compatibility makes <c>STRSTARTS("Markiert"@de, "Mark")</c>
        /// true, so the half of ADR-0048 that says a mapped string never sees a tagged literal held for
        /// <c>==</c> and not for <c>StartsWith</c>.
        /// </remarks>
        [Test]
        public void AStringFunctionOverAMappedStringDoesNotMatchATaggedLiteral()
        {
            var tagged = _model.AsSparqlQueryable<Document>()
                .Where(d => d.Title.StartsWith("Markiert"))
                .ToList();

            CollectionAssert.IsEmpty(tagged, "'Markiert' exists only as a @de literal.");
        }

        /// <summary>
        /// A LangString-typed mapping compares against a tagged constant, tag included.
        /// </summary>
        /// <remarks>
        /// The obvious translation - <c>FILTER(?v = "Annual report"@en)</c> - is tag-blind on
        /// dotNetRDF 3.5.2, which this branch measured and then used anyway for this path. The lexical
        /// form and the tag are compared separately instead.
        /// </remarks>
        [Test]
        public void ComparesALangStringMappingAgainstATaggedConstant()
        {
            var match = _model.AsSparqlQueryable<Document>()
                .Where(d => d.Alternative == new LangString("Annual report", "en"))
                .ToList();

            Assert.AreEqual(1, match.Count);
            Assert.AreEqual(new Uri("http://example.org/doc/en"), match[0].Uri);

            var wrongTag = _model.AsSparqlQueryable<Document>()
                .Where(d => d.Alternative == new LangString("Annual report", "fr"))
                .ToList();

            CollectionAssert.IsEmpty(wrongTag, "Same text, different tag: a different literal.");
        }

        /// <summary>
        /// A region subtag matches regardless of how the store cased it back.
        /// </summary>
        /// <remarks>
        /// In-memory this passes either way; it earns its keep in the store suites, where Jena returns
        /// <c>de-DE</c> for a tag written as <c>de-de</c> and a raw <c>LANG(?v) = "de-at"</c> comparison
        /// therefore matches nothing. Both sides are lower-cased.
        /// </remarks>
        [Test]
        public void MatchesARegionSubtagWhateverCaseTheStoreReturns()
        {
            foreach (string tag in new[] { "de-AT", "de-at", "DE-AT" })
            {
                var austrian = _model.AsSparqlQueryable<Document>()
                    .Where(d => d.LocalizedTitle[tag] == "Jahresbericht")
                    .ToList();

                Assert.AreEqual(1, austrian.Count, $"Tag '{tag}' should match the @de-AT title.");
                Assert.AreEqual(new Uri("http://example.org/doc/at"), austrian[0].Uri);
            }
        }

        /// <summary>
        /// Counting a localized container is refused rather than answered with a triple count.
        /// </summary>
        /// <remarks>
        /// The two containers disagree about what a count is - LocalizedString counts languages,
        /// LocalizedStringCollection counts values - so no single count of matching triples is right
        /// for both, and the one that was emitted also counted untagged literals, which neither
        /// container holds.
        /// </remarks>
        [Test]
        public void RefusesCountingALocalizedContainer()
        {
            var thrown = Assert.Throws<NotSupportedException>(() =>
                _model.AsSparqlQueryable<Document>()
                    .Where(d => d.LocalizedTitle.Count > 1)
                    .ToList());

            StringAssert.Contains("LocalizedTitle", thrown.Message);
        }

        /// <summary>
        /// Best() is refused rather than approximated: RFC 4647 lookup walks a preference list and
        /// falls back to the untagged value, which langMatches() does not do, so translating it would
        /// answer a different question.
        /// </summary>
        [Test]
        public void RefusesBestInsideAQuery()
        {
            var thrown = Assert.Throws<NotSupportedException>(() =>
                _model.AsSparqlQueryable<Document>()
                    .Where(d => d.LocalizedTitle.Best("de") == "Bericht")
                    .ToList());

            StringAssert.Contains("4647", thrown.Message);
        }

        [Test]
        public void SelectsAllResourcesOfType()
        {
            var people = _model.AsSparqlQueryable<Person>().ToList();

            Assert.AreEqual(3, people.Count);
        }

        [Test]
        public void FiltersByStringEquality()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.FirstName == "Alice").ToList();

            Assert.AreEqual(1, people.Count);
            Assert.AreEqual("Alice", people[0].FirstName);
        }

        [Test]
        public void FiltersByNumericComparison()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.Age >= 69).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void FiltersByConjunction()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.Age >= 40 && p.FirstName == "Bob").ToList();

            Assert.AreEqual(1, people.Count);
            Assert.AreEqual("Bob", people[0].FirstName);
        }

        [Test]
        public void FiltersByNestedMemberAccess()
        {
            var people = _model.AsSparqlQueryable<Person>().Where(p => p.Group.Name == "The Spiders").ToList();

            Assert.AreEqual(1, people.Count);
            Assert.AreEqual("Alice", people[0].FirstName);
        }

        [Test]
        public void FiltersToExactType()
        {
            // A type query matches resources explicitly typed with that class. John is committed as a
            // foaf:Agent and the persons as foaf:Person; derived resources are NOT re-typed with base
            // classes (a GetTypes/inference concern, tracked separately), so an Agent query returns
            // just John and a Person query excludes him.
            var agents = _model.AsSparqlQueryable<Agent>().ToList();

            Assert.AreEqual(1, agents.Count);
            Assert.AreEqual("John", agents[0].FirstName);

            var people = _model.AsSparqlQueryable<Person>().ToList();

            Assert.AreEqual(3, people.Count);
            CollectionAssert.DoesNotContain(people.Select(p => p.FirstName).ToList(), "John");
        }

        [Test]
        public void OrdersAndTakes()
        {
            var youngest = _model.AsSparqlQueryable<Person>().OrderBy(p => p.Age).First();

            Assert.AreEqual("Eve", youngest.FirstName);
        }

        [Test]
        public void OrdersDescendingAndTakes()
        {
            var oldest = _model.AsSparqlQueryable<Person>().OrderByDescending(p => p.Age).Take(1).ToList();

            Assert.AreEqual(1, oldest.Count);
            Assert.AreEqual("Bob", oldest[0].FirstName);
        }

        [Test]
        public void SkipsWithOrdering()
        {
            var people = _model.AsSparqlQueryable<Person>().OrderBy(p => p.Age).Skip(1).ToList();

            Assert.AreEqual(2, people.Count);
            Assert.AreEqual("Alice", people[0].FirstName);
        }

        [Test]
        public void AnswersAny()
        {
            Assert.IsTrue(_model.AsSparqlQueryable<Person>().Any(p => p.FirstName == "Alice"));
            Assert.IsFalse(_model.AsSparqlQueryable<Person>().Any(p => p.FirstName == "Nobody"));
        }

        [Test]
        public void CountsResources()
        {
            Assert.AreEqual(3, _model.AsSparqlQueryable<Person>().Count());
            Assert.AreEqual(2, _model.AsSparqlQueryable<Person>().Count(p => p.Age >= 69));
        }
    }
}
