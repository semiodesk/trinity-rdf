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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.IO;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    public class SparqlUpdateTest
    {
        private IModel _model = null;
        IStore _store;

        private NamespaceManager _namespaceManager = new NamespaceManager();

        [SetUp]
        public void SetUp()
        {
            string connectionString = SetupClass.ConnectionString;

            _store = StoreFactory.CreateStore(string.Format("{0};rule=urn:semiodesk/test/ruleset", connectionString));
            _model = _store.GetModel(new Uri("ex:TestModel"));

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
        public void TearDown()
        {
            _model.Clear();
            _store.Dispose();
        }

        [Test]
        public void TestInsert()
        {
            SparqlUpdate update = new SparqlUpdate(@"INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            ISparqlQueryResult result = _model.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());
        }

        [Test]
        public void TestModify()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }");

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA FROM <ex:TestModel> { ex:book dc:title 'This is an example title' . }
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title too' . }");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title too' . }");

            Assert.AreEqual(true, _model.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestDelete()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }");

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA FROM <ex:TestModel> { ex:book dc:title 'This is an example title' . }");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestLoad()
        {
            Assert.Inconclusive();
            SparqlUpdate update = new SparqlUpdate(@"LOAD <http://eurostat.linked-statistics.org/sparql> INTO <ex:TestModel>");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"SELECT * WHERE { ?s ?p ?o . }");

            Assert.Greater(_model.ExecuteQuery(query).GetBindings().Count(), 0);
        }

        [Test]
        public void TestClear()
        {
            SparqlUpdate update = new SparqlUpdate(@"INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }");

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"CLEAR GRAPH <ex:TestModel>");

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
