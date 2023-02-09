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
// Copyright (c) Semiodesk GmbH 2015-2019

using System;
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    public class SparqlUpdateTest : SetupClass
    {
        private IStore _store;

        private IModel _model = null;

        [SetUp]
        public void SetUp()
        {
            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            _model = _store.GetModel(new Uri("http://example.org/TestModel"));

            if (!_model.IsEmpty)
            {
                _model.Clear();
            }

            OntologyDiscovery.AddNamespace("vcard", new Uri("http://www.w3.org/2001/vcard-rdf/3.0#"));
            OntologyDiscovery.AddNamespace("foaf", new Uri("http://xmlns.com/foaf/0.1/"));
            OntologyDiscovery.AddNamespace("dc", new Uri("http://purl.org/dc/elements/1.1/"));
            OntologyDiscovery.AddNamespace("ex", new Uri("http://example.org/"));
        }

        [TearDown]
        public override void TearDown()
        {
            _model.Clear();
            _store.Dispose();
        }

        [Test]
        public void TestLanguageTagsVariableIssue()
        {
            string str = @"
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
            SparqlUpdate update = new SparqlUpdate(str);
            Assert.DoesNotThrow(() =>
           {
               update.ToString();
           });
 
        }

        [Test]
        public void TestInsert()
        {
            SparqlUpdate update = new SparqlUpdate(@"INSERT DATA { GRAPH <http://example.org/TestModel> {ex:book dc:title 'This is an example title' .} }");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            ISparqlQueryResult result = _model.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());

            /// TEST WITH LANGUAGE TAG
            /// 
            update = new SparqlUpdate(@"INSERT DATA { GRAPH @graph {ex:book dc:title 'This is an example title'@en . }}");
            update.Bind("@graph", new Uri("http://example.org/TestModel"));
            _model.ExecuteUpdate(update);

            query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title'@en . }");

            result = _model.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());
        }

        [Test]
        public void TestModify()
        {
            SparqlUpdate update = new SparqlUpdate(@"
               INSERT DATA { GRAPH <http://example.org/TestModel> { ex:book dc:title 'This is an example title' . } }");

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                WITH <http://example.org/TestModel>   
                DELETE  { ex:book dc:title 'This is an example title' . }      
                INSERT { ex:book dc:title 'This is an example title too' . }
                WHERE { ex:book dc:title 'This is an example title' . }"
);

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title too' . }");

            Assert.AreEqual(true, _model.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestMultipleModify()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA { GRAPH <http://example.org/TestModel> { ex:book dc:title 'This is an example title' . } };
                INSERT DATA {  GRAPH <http://example.org/TestModel> { ex:book2 dc:title 'This is an example title2' . } }");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(true, _model.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title2' . }");

            Assert.AreEqual(true, _model.ExecuteQuery(query).GetAnwser());
        }


        [Test]
        public void TestDelete()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA { GRAPH <http://example.org/TestModel>  { ex:book dc:title 'This is an example title' . } }");

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA  { GRAPH <http://example.org/TestModel> { ex:book dc:title 'This is an example title' . } }");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestLoad()
        {
            Assert.Inconclusive();
            SparqlUpdate update = new SparqlUpdate(@"LOAD <http://eurostat.linked-statistics.org/sparql> INTO <http://example.org/TestModel>");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"SELECT * WHERE { ?s ?p ?o . }");

            Assert.Greater(_model.ExecuteQuery(query).GetBindings().Count(), 0);
        }

        [Test]
        public void TestClear()
        {
            SparqlUpdate update = new SparqlUpdate(@"INSERT DATA { GRAPH <ex:TestModel> { ex:book dc:title 'This is an example title' . }}");

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"CLEAR GRAPH <http://example.org/TestModel>");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestUpdateParameters()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                DELETE { ?s ?p @oldValue . }
                INSERT { ?s ?p @newValue . }
                WHERE { ?s ?p ?o . }");

            update.Bind("@oldValue", "Fail");
            update.Bind("@newValue", "Success");

            string updateString = update.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(updateString));

            _model.ExecuteUpdate(update);
        }
    }
}
