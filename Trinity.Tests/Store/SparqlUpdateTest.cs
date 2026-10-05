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
// Copyright (c) Semiodesk GmbH 2023

using NUnit.Framework;
using System;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests.Linq;
using Semiodesk.Trinity.Tests.Query;
using System.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class SparqlUpdateTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            OntologyDiscovery.AddNamespace("vcard", vcard.Namespace);
            OntologyDiscovery.AddNamespace("foaf", foaf.Namespace);
            OntologyDiscovery.AddNamespace("dc", dc.Namespace);
            OntologyDiscovery.AddNamespace("ex", new Uri("http://example.org/"));
        }
        
        [Test]
        public void TestInsert()
        {
            var update = new SparqlUpdate(@"INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");
            var result = Model1.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());

            Model1.Clear();
            
            update = new SparqlUpdate(@"INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title'@en . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title'@en . }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());
        }

        [Test]
        public void TestModify()
        {
            var update = new SparqlUpdate(@"
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } };
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title too' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, Model1.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title too' . }");

            Assert.AreEqual(true, Model1.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestMultipleModify()
        {
            var update = new SparqlUpdate(@"
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } };
                INSERT DATA { GRAPH @graph { ex:book2 dc:title 'This is an example title2' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(true, Model1.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title2' . }");

            Assert.AreEqual(true, Model1.ExecuteQuery(query).GetAnwser());
        }


        [Test]
        public void TestDelete()
        {
            var update = new SparqlUpdate(@"
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, Model1.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestClear()
        {
            var update = new SparqlUpdate(@"INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            update = new SparqlUpdate(@"CLEAR GRAPH @graph").Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, Model1.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestUpdateParameters()
        {
            var update = new SparqlUpdate(@"
                DELETE { ?s ?p @oldValue . }
                INSERT { ?s ?p @newValue . }
                WHERE { ?s ?p ?o . }");

            update.Bind("@oldValue", "Fail");
            update.Bind("@newValue", "Success");

            var updateString = update.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(updateString));

            Model1.ExecuteUpdate(update);
            
            // If no exception is thrown then the SPARQL query was valid.
        }
        
        [Test]
        public void TestLanguageTagsVariableIssue()
        {
            const string updateString = @"
            WITH <ex:test>
            DELETE { <http://www.w3.org/2006/time> ?p ?o. }
            WHERE { OPTIONAL { <http://www.w3.org/2006/time> ?p ?o. } }
            INSERT { 
                <http://www.w3.org/2006/time> <http://schema.org/name> 'OWL-Time'@en; 
                <http://www.w3.org/2004/02/skos/core#changeNote> '2017-04-06 - hasTime, hasXSDDuration added; Number removed; all duration elements changed to xsd:decimal'; 
                <http://www.w3.org/2004/02/skos/core#historyNote> 
                '''Update of OWL-Time ontology, extended to support general temporal reference systems. 
                    Ontology engineering by Simon J D Cox'''@en.
                }
            ";
            
            var update = new SparqlUpdate(updateString);

            Assert.DoesNotThrow(() => update.ToString());
        }

        /// <summary>
        /// Any string written as the lexical form of a typed literal is stored exactly, with the datatype,
        /// and writes nothing else (ADR-0052).
        /// </summary>
        /// <remarks>
        /// The datatype is one no deserializer knows, so every store hands the lexical form back as a
        /// string. <c>SerializeTypedLiteral</c> used to place a string between quotes of
        /// <c>XsdTypeMapper</c>'s own, unescaped: <c>abc</c> was stored with the quotes, and an apostrophe
        /// ended the literal early.
        /// </remarks>
        [Test]
        public void AnyStringIsStoredExactlyAsTheLexicalFormOfATypedLiteral()
        {
            var datatype = new Uri("http://example.org/datatype#text");
            var predicate = new Uri("http://example.org/typed");

            string[] values = HostileLiterals.Values(Model2.Uri);

            for (int i = 0; i < values.Length; i++)
            {
                var subject = BaseUri.GetUriRef("typed-" + i);

                // Concatenated on purpose: what SerializeTypedLiteral writes into query text is what is
                // under test, and binding the value would go through SerializeValue instead.
                Model1.ExecuteUpdate(new SparqlUpdate("INSERT DATA { GRAPH @graph { @subject @predicate " +
                        SparqlSerializer.SerializeTypedLiteral(values[i], datatype) + " . } }")
                    .Bind("@graph", Model1)
                    .Bind("@subject", subject)
                    .Bind("@predicate", predicate));

                var query = new SparqlQuery("SELECT ?o WHERE { @subject @predicate ?o . FILTER(DATATYPE(?o) = @datatype) }")
                    .Bind("@subject", subject)
                    .Bind("@predicate", predicate)
                    .Bind("@datatype", datatype);

                CollectionAssert.AreEqual(new[] { values[i] }, Model1.GetBindings(query).Select(b => b["o"]),
                    SparqlLiteralOracle.Display(values[i]));
            }

            HostileLiterals.AssertNothingEscaped(Store, Model1, Model2);
        }
    }
}
