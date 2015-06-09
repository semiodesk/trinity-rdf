using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.Diagnostics;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class SparqlEndpointTest
    {

        [SetUp]
        public void SetUp()
        {

        }

        [TearDown]
        public void TearDown()
        {

        }


        [Test]
        public void QueryDBPediaTest()
        {

            var store = Stores.CreateStore("provider=sparqlendpoint;endpoint=http://live.dbpedia.org/sparql");
            IModel m = store.GetModel(new Uri("http://dbpedia.org"));
            ResourceQuery q = new ResourceQuery();
            Property wikiPageID = new Property(new Uri("http://dbpedia.org/ontology/wikiPageID"));
            q.Where(wikiPageID, 445980);
            var result = m.ExecuteQuery(q); 
            var b = result.GetResources();
            foreach (var res in b)
            {
                Console.WriteLine(res);
            }
        }

        [Test]
        public void GetResourceDBPediaTest()
        {

            var store = Stores.CreateStore("provider=sparqlendpoint;endpoint=http://live.dbpedia.org/sparql");
            IModel m = store.GetModel(new Uri("http://dbpedia.org"));
            IResource res = m.GetResource(new Uri("http://dbpedia.org/resource/Munich"));
            
        }

        //[Test]
        public void QueryWordnetTest()
        {
            var store = Stores.CreateStore("provider=sparqlendpoint;endpoint=http://wordnet.rkbexplorer.com/sparql/");
            IModel m = store.GetModel(new Uri("http://wordnet.rkbexplorer.com/sparql/"));
            ResourceQuery q = new ResourceQuery();
            q.Where(rdfs.label, "eat");
            var result = m.ExecuteQuery(q);
            var b = result.GetResources();
            foreach (var res in b)
            {
                Console.WriteLine(res);
            }
        }

  
    }
}
