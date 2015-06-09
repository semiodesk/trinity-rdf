using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.IO;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class SparqlUpdateTest
    {
        private IModel _model = null;
        IStore _store;

        private NamespaceManager _namespaceManager = new NamespaceManager();

        public SparqlUpdateTest()
        {
            _store = Stores.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");

            Uri modelUri = new Uri("ex:TestModel");
            if( _store.ContainsModel(modelUri))
            {
                _model = _store.GetModel(modelUri);
            }
            else
            {
                _model = _store.CreateModel(modelUri);
            }

            _namespaceManager.AddNamespace("vcard", "http://www.w3.org/2001/vcard-rdf/3.0#");
            _namespaceManager.AddNamespace("foaf", "http://xmlns.com/foaf/0.1/");
            _namespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            _namespaceManager.AddNamespace("ex", "http://example.org/");
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
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }", _namespaceManager);

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }", _namespaceManager);

            ISparqlQueryResult result = _model.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());
        }

        [Test]
        public void TestModify()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }", _namespaceManager);

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA FROM <ex:TestModel> { ex:book dc:title 'This is an example title' . }
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title too' . }", _namespaceManager);

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }", _namespaceManager);

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title too' . }", _namespaceManager);

            Assert.AreEqual(true, _model.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestDelete()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }", _namespaceManager);

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA FROM <ex:TestModel> { ex:book dc:title 'This is an example title' . }", _namespaceManager);

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }", _namespaceManager);

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestLoad()
        {
            SparqlUpdate update = new SparqlUpdate(@"LOAD <http://gov.tso.co.uk/research/sparql> INTO <ex:TestModel>");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                SELECT * WHERE { ?s ?p ?o . }", _namespaceManager);

            Assert.Greater(_model.ExecuteQuery(query).GetBindings().Count(), 0);
        }

        [Test]
        public void TestClear()
        {
            SparqlUpdate update = new SparqlUpdate(@"
                INSERT DATA INTO <ex:TestModel> { ex:book dc:title 'This is an example title' . }", _namespaceManager);

            _model.ExecuteUpdate(update);

            update = new SparqlUpdate(@"CLEAR GRAPH <ex:TestModel>");

            _model.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"
                ASK WHERE { ?s dc:title 'This is an example title' . }", _namespaceManager);

            Assert.AreEqual(false, _model.ExecuteQuery(query).GetAnwser());
        }
    }
}
