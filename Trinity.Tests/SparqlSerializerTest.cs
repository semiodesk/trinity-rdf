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

            string binding = SparqlSerializer.GenerateSubjectBindings("?s", uris).Single();

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
            string binding = SparqlSerializer.GenerateSubjectBindings("?s", new[] { new Uri("http://example.org/a") }).Single();

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
        /// The chain this replaced interpolated <c>Uri.ToString()</c>, which returns the *display*
        /// form and unescapes percent-encoding: <c>http://example.org/a%20b</c> came out as
        /// <c>http://example.org/a b</c>. Where the unescaped character is one SPARQL forbids inside
        /// an <c>IRIREF</c> — a space is the obvious one — the query is a **parse error** that takes
        /// every other subject in the same batch down with it, not merely a subject that fails to
        /// match.
        /// </summary>
        /// <remarks>
        /// Unrelated to the .NET 10 <c>Uri</c> equality change (ADR-0025): this is the serialization
        /// path rather than identity, and it behaves identically on .NET 8, 9 and 10.
        /// </remarks>
        [TestCase("http://example.org/a%20b")]
        [TestCase("http://example.org/a%3Eb")]
        [TestCase("http://example.org/caf%C3%A9")]
        public void PreservesPercentEncoding(string original)
        {
            var uri = new Uri(original);

            Assert.AreNotEqual(original, uri.ToString(),
                "the premise of this test is that ToString() differs — pick another case if this fails");

            string binding = SparqlSerializer.GenerateSubjectBindings("?s", new[] { uri }).Single();

            Assert.AreEqual("VALUES ?s { <" + original + "> } ", binding);
        }

        /// <summary>
        /// The end of that story: the emitted query has to actually parse. A space inside an
        /// <c>IRIREF</c> is what the old interpolation produced for <c>%20</c>, and dotNetRDF
        /// rejects it with <c>"Illegal white space in URI"</c>.
        /// </summary>
        [Test]
        public void TheEmittedQueryParsesForAPercentEncodedSubject()
        {
            var uris = new[]
            {
                new Uri("http://example.org/a%20b"),
                new Uri("http://example.org/plain")
            };

            string sparql = ResourceQuery(uris);

            Assert.IsFalse(sparql.Contains("a b"), "the display form must never reach the query text");
            Assert.IsTrue(new SparqlQuery(sparql).ProvidesStatements());

            var parser = new VDS.RDF.Parsing.SparqlQueryParser();

            Assert.DoesNotThrow(() => parser.ParseFromString(sparql),
                "a subject whose display form contains a character SPARQL forbids in an IRIREF must "
                + "not turn the whole query into a parse error:\n" + sparql);
        }

        /// <summary>
        /// The datatype IRI of a typed literal goes through the same hazard: interpolating the
        /// <see cref="Uri"/> would emit its display form. A custom datatype is the realistic case —
        /// the built-in ones are all <c>xsd:</c> and never percent-encoded.
        /// </summary>
        [Test]
        public void SerializeTypedLiteralEscapesTheDatatypeIri()
        {
            var datatype = new Uri("http://example.org/dt/my%20type");

            string literal = SparqlSerializer.SerializeTypedLiteral(42, datatype);

            Assert.IsTrue(literal.EndsWith("^^<http://example.org/dt/my%20type>"),
                "the datatype IRI must keep its percent-encoding, was: " + literal);

            string sparql = "SELECT ?s ?p ?o WHERE { ?s ?p " + literal + ". }";

            Assert.DoesNotThrow(() => new VDS.RDF.Parsing.SparqlQueryParser().ParseFromString(sparql),
                "the display form of the datatype IRI must never reach the query text:\n" + sparql);
        }

        /// <summary>
        /// And so does a declared prefix: <c>SparqlQuery</c> injects <c>PREFIX p: &lt;ns&gt;</c> for
        /// every registered namespace the query uses, which is on the path of essentially every
        /// query Trinity emits (ADR-0024).
        /// </summary>
        [Test]
        public void DeclaredPrefixesEscapeTheirNamespaceIri()
        {
            const string prefix = "encodedtestns";
            var ns = new Uri("http://example.org/encoded%20ns/");

            OntologyDiscovery.Namespaces[prefix] = ns;

            try
            {
                var query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s " + prefix + ":name ?o. ?s ?p ?o. }");
                string sparql = query.ToString();

                Assert.IsTrue(sparql.Contains("encoded%20ns"),
                    "the namespace must keep its percent-encoding:\n" + sparql);
                Assert.IsFalse(sparql.Contains("encoded ns"),
                    "the display form must never reach the query text:\n" + sparql);

                Assert.DoesNotThrow(() => new VDS.RDF.Parsing.SparqlQueryParser().ParseFromString(sparql),
                    "a percent-encoded namespace must not turn every query using it into a parse error:\n" + sparql);
            }
            finally
            {
                OntologyDiscovery.Namespaces.Remove(prefix);
            }
        }

        /// <summary>
        /// The blank-node guard must key on the label, not on how the identifier was constructed.
        /// Only <c>UriRef(string, bool)</c> sets the <c>IsBlankId</c> property, so a consumer that
        /// builds one any other way used to slip a bare <c>_:0</c> into the VALUES block and take
        /// every addressable subject in that batch down with it.
        /// </summary>
        [Test]
        public void SkipsBlankNodeIdentifiersHoweverTheyWereConstructed()
        {
            var uris = new List<Uri>
            {
                new UriRef("http://example.org/a"),
                new UriRef("_:flagged", true),                      // the flag-setting constructor
                new UriRef("_:unflagged", UriKind.RelativeOrAbsolute), // IsBlankId == false
                new Uri("_:plain", UriKind.RelativeOrAbsolute),        // not a UriRef at all
                new UriRef("http://example.org/b")
            };

            string binding = SparqlSerializer.GenerateSubjectBindings("?s", uris).Single();

            Assert.AreEqual("VALUES ?s { <http://example.org/a> <http://example.org/b> } ", binding);
            Assert.IsFalse(binding.Contains("_:"), "no blank node label may reach the query text");

            Assert.DoesNotThrow(() => new VDS.RDF.Parsing.SparqlQueryParser().ParseFromString(
                SparqlSerializer.GenerateResourceQuery(binding)));
        }

        /// <summary>
        /// A blank identifier that is spelled as an absolute IRI must stay bracketed and must stay in
        /// the binding. Virtuoso hands its blank nodes back as <c>nodeID://b10000</c> with the
        /// <c>IsBlankId</c> flag set, so deciding on the flag rather than the spelling emits them bare
        /// and drops them from subject bindings — which breaks blank-node round-trips on Virtuoso
        /// while the in-memory store, whose blank ids really are <c>_:</c> labels, shows nothing.
        /// </summary>
        /// <remarks>
        /// Found by the Virtuoso suite, not by this one. Same lesson as ADR-0043: a second backend
        /// finds what the first hides.
        /// </remarks>
        [Test]
        public void KeepsAnAbsoluteIriBlankIdentifierAddressable()
        {
            var virtuosoBlank = new UriRef("nodeID://b10000", true);

            Assert.IsTrue(virtuosoBlank.IsBlankId(), "it is semantically a blank node");
            Assert.IsFalse(virtuosoBlank.IsBlankNodeLabel(), "but it is not spelled as a label");

            Assert.AreEqual("<nodeID://b10000>", SparqlSerializer.SerializeUri(virtuosoBlank),
                "an absolute IRI must stay bracketed, whatever the flag says");

            string binding = SparqlSerializer.GenerateSubjectBindings("?s",
                new Uri[] { new UriRef("http://example.org/a"), virtuosoBlank }).Single();

            StringAssert.Contains("<nodeID://b10000>", binding,
                "an addressable blank identifier must not be skipped");
        }

        /// <summary>
        /// An underscore alone does not make a blank node label — <c>_:</c> does. A relative URI
        /// beginning with <c>_</c> is an ordinary identifier and must stay bracketed.
        /// </summary>
        [Test]
        public void OnlyTreatsUnderscoreColonAsABlankLabel()
        {
            Assert.IsFalse(new Uri("_foo", UriKind.Relative).IsBlankId());
            Assert.IsTrue(new Uri("_:foo", UriKind.Relative).IsBlankId());
        }

        /// <summary>
        /// The counterpart to <see cref="PreservesPercentEncoding"/>: an IRI that cannot be written
        /// verbatim is refused where the mistake is, naming it, rather than becoming an
        /// <c>RdfParseException</c> deep inside a batch that also kills unrelated subjects.
        /// </summary>
        [TestCase("http://example.org/a b")]
        [TestCase("http://example.org/a\u0009b")]
        [TestCase("http://example.org/a>b")]
        [TestCase("http://example.org/a\"b")]
        [TestCase("http://example.org/a{b")]
        [TestCase("http://example.org/a|b")]
        public void RefusesAnIriThatCannotBeWrittenVerbatim(string original)
        {
            var uri = new UriRef(original, UriKind.RelativeOrAbsolute);

            var e = Assert.Throws<NotSupportedException>(() => SparqlSerializer.SerializeUri(uri));

            StringAssert.Contains(original, e.Message, "the message must name the offending IRI");
        }

        /// <summary>
        /// Serialization must be verbatim. This is the guard that was missing: nothing would have
        /// failed if <c>SerializeUri</c> started using <c>AbsoluteUri</c>, which normalizes host
        /// casing, default ports, dot-segments and percent-encoding case. Trinity compares and
        /// hashes resources on the ordinal <c>OriginalString</c>
        /// (<see cref="Resource.Equals(object)"/>), and the LINQ provider joins two result sets on
        /// it, so a normalized spelling would silently break mapped-collection dedup and drop rows.
        /// <c>SparqlQueryWriterTest.WritesIrisExactlyAsGiven</c> is the same guard on the read path.
        /// </summary>
        [TestCase("http://Example.ORG/x", TestName = "SerializeUri_does_not_lower_case_the_host")]
        [TestCase("http://example.org:80/x", TestName = "SerializeUri_keeps_a_default_port")]
        [TestCase("http://example.org/a%2Fb/../c", TestName = "SerializeUri_keeps_dot_segments")]
        [TestCase("http://example.org/a%2fb", TestName = "SerializeUri_keeps_percent_encoding_case")]
        public void SerializesVerbatimAndNeverNormalizes(string original)
        {
            Assert.AreEqual("<" + original + ">", SparqlSerializer.SerializeUri(new UriRef(original)));
        }

        /// <summary>
        /// A blank node label is legal as an RDF term in a few positions and in none of the places
        /// the grammar demands an IRI, so those use <c>SerializeIriRef</c>, which never drops the
        /// brackets and refuses a blank identifier outright.
        /// </summary>
        [Test]
        public void SerializeIriRefAlwaysBracketsAndRefusesBlankNodes()
        {
            Assert.AreEqual("<http://example.org/a>",
                SparqlSerializer.SerializeIriRef(new UriRef("http://example.org/a")));

            Assert.Throws<NotSupportedException>(
                () => SparqlSerializer.SerializeIriRef(new UriRef("_:b0", true)));
            Assert.Throws<ArgumentNullException>(() => SparqlSerializer.SerializeIriRef(null));

            // SerializeUri, by contrast, emits the bare label - which is why these positions cannot use it.
            Assert.AreEqual("_:b0", SparqlSerializer.SerializeUri(new UriRef("_:b0", true)));
        }

        /// <summary>
        /// Argument validation belongs at the call, not at the caller's foreach. An iterator defers
        /// its whole body, so a bad argument would otherwise surface with a stack pointing at the
        /// consumer instead of the mistake.
        /// </summary>
        [Test]
        public void ValidatesItsArgumentsEagerly()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SparqlSerializer.GenerateSubjectBindings("?s", new Uri[0], 0));
            Assert.Throws<ArgumentException>(
                () => SparqlSerializer.GenerateSubjectBindings("", new Uri[0]));
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
