using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    class SparqlSerializerTest
    {
        [TestCase]
        public void TestStringSerializeResource()
        {
            Resource r = new Resource("http://example.com/ex");
            r.AddProperty(Ontologies.dc.title, "MyResource");

            string res = SparqlSerializer.SerializeResource(r);
            string expected = "<http://example.com/ex> <http://purl.org/dc/elements/1.1/title> 'MyResource'. ";

            Assert.AreEqual(expected, res);
        }

        [TestCase]
        public void TestStringSerializeResourceWithMapping()
        {
            PersonContact contact = new PersonContact(new Uri("http://example.com/ex"));
            contact.NameGiven = "Peter";

            var res = SparqlSerializer.SerializeResource(contact);
            var expected = "<http://example.com/ex> <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameGiven> 'Peter'; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PersonContact>. ";
            
            Assert.AreEqual(expected, res);

            contact.Language = "DE";
            res = SparqlSerializer.SerializeResource(contact);

            Assert.AreEqual(expected, res);
        }

        [TestCase]
        public void TestStringSerializeResourceEmpty()
        {
            Resource empty = new Resource("http://test.com/ex");

            var res = SparqlSerializer.SerializeResource(empty);
            var expected = "";

            Assert.AreEqual(expected, res);
        }

        [TestCase]
        public void TestSerializeResourceWithBlankNode()
        {
            Resource r0 = new Resource(new UriRef("_:0", true));
            Resource r1 = new Resource(new UriRef("_:1", true));
            r1.AddProperty(new Property(new UriRef("http://schema.org/relatedTo")), r0);

            var s = SparqlSerializer.SerializeResource(r1);

            Assert.IsTrue(s.Contains("_:1 <http://schema.org/relatedTo> _:0"));
        }

        #region Subject binding

        /// <summary>
        /// The shape <see cref="Model"/> and <see cref="ModelGroup"/> emit for a bulk lazy load.
        /// Kept here rather than inlined in the assertions so a change to the emission has to be
        /// made once, and every test below moves with it.
        /// </summary>
        private static string ResourceQuery(IEnumerable<Uri> uris)
        {
            string binding = SparqlSerializer.GenerateSubjectBindings("?s", uris).Single();

            return "SELECT ?s ?p ?o WHERE { " + binding + "?s ?p ?o. }";
        }

        [Test]
        public void GeneratesAValuesBlockRatherThanAnEqualityChain()
        {
            var uris = new[] { new Uri("http://example.org/a"), new Uri("http://example.org/b") };

            string binding = SparqlSerializer.GenerateSubjectBinding("?s", uris);

            Assert.AreEqual("VALUES ?s { <http://example.org/a> <http://example.org/b> } ", binding);
        }

        /// <summary>
        /// Resource materialization refuses any query for which <c>ProvidesStatements()</c> is
        /// false — and it is decided by a token-level heuristic, not by parsing, so adding a
        /// <c>VALUES</c> block in front of the triple pattern is exactly the kind of change that
        /// could break it silently. The failure would not look like a query problem: it surfaces
        /// as <c>ArgumentException: The given query cannot be resolved into statements.</c>
        /// </summary>
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(300)]
        public void TheEmittedResourceQueryProvidesStatements(int count)
        {
            var uris = Enumerable.Range(0, count)
                .Select(i => new Uri("http://example.org/r" + i))
                .ToList();

            string sparql = ResourceQuery(uris);
            var query = new SparqlQuery(sparql);

            Assert.IsTrue(query.ProvidesStatements(),
                "the bulk lazy-load query must provide statements or GetResources() refuses it outright:\n" + sparql);
            Assert.AreEqual(new[] { "s", "p", "o" }, query.GetGlobalScopeVariableNames(),
                "the projection must stay ?s ?p ?o, in that order");
        }

        /// <summary>
        /// The heuristic latches on the pattern terminator, so dropping the trailing '.' silently
        /// turns materialization off. This pins the reason the emission keeps it.
        /// </summary>
        [Test]
        public void DroppingTheTrailingDotWouldBreakMaterialization()
        {
            string binding = SparqlSerializer.GenerateSubjectBinding("?s", new[] { new Uri("http://example.org/a") });

            Assert.IsFalse(new SparqlQuery("SELECT ?s ?p ?o WHERE { " + binding + "?s ?p ?o }").ProvidesStatements());
            Assert.IsTrue(new SparqlQuery("SELECT ?s ?p ?o WHERE { " + binding + "?s ?p ?o. }").ProvidesStatements());
        }

        [Test]
        public void BatchesSubjectsAndKeepsEveryOne()
        {
            var uris = Enumerable.Range(0, 2500)
                .Select(i => (Uri)new UriRef("http://example.org/r" + i))
                .ToList();

            var batches = SparqlSerializer.GenerateSubjectBindings("?s", uris, 1000).ToList();

            Assert.AreEqual(3, batches.Count, "2500 subjects at 1000 per batch is 3 batches");
            Assert.AreEqual(1000, Occurrences(batches[0], "http://example.org/r"));
            Assert.AreEqual(1000, Occurrences(batches[1], "http://example.org/r"));
            Assert.AreEqual(500, Occurrences(batches[2], "http://example.org/r"));

            foreach (string batch in batches)
            {
                Assert.IsTrue(new SparqlQuery("SELECT ?s ?p ?o WHERE { " + batch + "?s ?p ?o. }").ProvidesStatements(),
                    "every batch must independently provide statements");
            }
        }

        /// <summary>
        /// A blank node label is not a legal <c>DataBlockValue</c>, and a label in a query is an
        /// existential variable rather than a reference, so there is no query shape that addresses
        /// one by label. Serializing it anyway would take the addressable subjects down with it.
        /// </summary>
        [Test]
        public void SkipsBlankNodeIdentifiers()
        {
            var uris = new List<Uri>
            {
                new UriRef("http://example.org/a"),
                new UriRef("_:b0", true),
                new UriRef("http://example.org/b")
            };

            string binding = SparqlSerializer.GenerateSubjectBindings("?s", uris).Single();

            Assert.AreEqual("VALUES ?s { <http://example.org/a> <http://example.org/b> } ", binding);
            Assert.IsFalse(binding.Contains("_:"), "a blank node label must never reach the query text");
        }

        [Test]
        public void YieldsNothingWhenThereIsNothingAddressable()
        {
            Assert.IsEmpty(SparqlSerializer.GenerateSubjectBindings("?s", null).ToList());
            Assert.IsEmpty(SparqlSerializer.GenerateSubjectBindings("?s", new Uri[0]).ToList());
            Assert.IsEmpty(SparqlSerializer.GenerateSubjectBindings("?s", new Uri[] { new UriRef("_:b0", true) }).ToList());
        }

        /// <summary>
        /// The chain this replaced interpolated <c>Uri.ToString()</c>, which unescapes
        /// percent-encoding — so a resource stored under an escaped spelling was silently not found.
        /// </summary>
        [Test]
        public void PreservesPercentEncoding()
        {
            var uri = new Uri("http://example.org/a%20b");

            string binding = SparqlSerializer.GenerateSubjectBinding("?s", new[] { uri });

            Assert.AreEqual("VALUES ?s { <http://example.org/a%20b> } ", binding);
        }

        private static int Occurrences(string haystack, string needle)
        {
            int count = 0;

            for (int i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = haystack.IndexOf(needle, i + 1, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        #endregion
    }
}
