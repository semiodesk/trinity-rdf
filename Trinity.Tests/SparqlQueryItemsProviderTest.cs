using System;
using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    class SparqlQueryItemsProviderTest
    {
        protected IModel Model = null;
        IStore _store;

        protected NamespaceManager NamespaceManager = new NamespaceManager();

        public SparqlQueryItemsProviderTest()
        {
            _store = Stores.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");

            Uri modelUri = new Uri("http://example.org/TestModel");

            if( _store.ContainsModel(modelUri) )
            {
                Model = _store.GetModel(modelUri);
            }
            else
            {
                Model = _store.CreateModel(modelUri);
            }

            NamespaceManager.AddNamespace("ex", "http://example.org/");
            NamespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            NamespaceManager.AddNamespace("vcard", "http://www.w3.org/2001/vcard-rdf/3.0#");
            NamespaceManager.AddNamespace("foaf", "http://xmlns.com/foaf/0.1/");
            NamespaceManager.AddNamespace("dbpedia", "http://dbpedia.org/ontology/");
            NamespaceManager.AddNamespace("dbpprop", "http://dbpedia.org/property/");
            NamespaceManager.AddNamespace("schema", "http://schema.org/");
            NamespaceManager.AddNamespace("nie", "http://www.semanticdesktop.org/ontologies/2007/01/19/nie#");
            NamespaceManager.AddNamespace("nco", "http://www.semanticdesktop.org/ontologies/2007/03/22/nco#");
            NamespaceManager.AddNamespace("sfo", sfo.GetNamespace());
            NamespaceManager.AddNamespace(nfo.GetPrefix(), nfo.GetNamespace());

        }

        [SetUp]
        public void SetUp()
        {
            MappingDiscovery.RegisterAllCurrentAssemblies();

            Model.Clear();

            for (int i = 0; i < 100; i++)
            {

                IResource resource0 = Model.CreateResource();
                resource0.AddProperty(rdf.type, nco.PersonContact);
                resource0.AddProperty(nco.fullname, Guid.NewGuid().ToString());
                resource0.Commit();
            }

        }

        [TearDown]
        public void TearDown()
        {
            Model.Clear();
            _store.Dispose();
        }

        [Test]
        public void TestCount()
        {
            SparqlQuery q = new SparqlQuery("Select ?s ?p ?o where { ?s rdf:type nco:PersonContact. ?s ?p ?o. }", NamespaceManager);
            SparqlQueryItemsProvider<Resource> p = new SparqlQueryItemsProvider<Resource>(Model, q, false);
            Assert.AreEqual(100, p.Count());

            q = new SparqlQuery("Select ?s ?p ?o where { ?s rdf:type nco:Contact. ?s ?p ?o. }", NamespaceManager);
            p = new SparqlQueryItemsProvider<Resource>(Model, q, false);
            Assert.AreEqual(0, p.Count());
        }


        [Test]
        public void TestGetItems()
        {
            SparqlQuery q = new SparqlQuery("Select ?s ?p ?o where { ?s rdf:type nco:PersonContact. ?s ?p ?o. }", NamespaceManager);
            SparqlQueryItemsProvider<Resource> p = new SparqlQueryItemsProvider<Resource>(Model, q, false);

            Assert.AreEqual(10, p.GetItems(0, 10).Count());

            q = new SparqlQuery("Select ?s ?p ?o where { ?s rdf:type nco:Contact. ?s ?p ?o. }", NamespaceManager);
            p = new SparqlQueryItemsProvider<Resource>(Model, q, false);
            Assert.AreEqual(0, p.GetItems(0, 10).Count());

        }

    }
}
