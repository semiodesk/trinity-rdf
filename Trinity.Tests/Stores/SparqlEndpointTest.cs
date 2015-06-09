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
        private IModel _remoteModel = null;
        private IModel _localModel = null;

        public SparqlEndpointTest()
        {
            ModelManager modelManager = ModelManager.Instance;
            modelManager.Connect();
            modelManager.ConnectRemote(new Uri("http://127.0.0.1:8890/sparql"));

            try
            {
                // Default uri scheme
                _localModel = modelManager.GetModel(new Uri("http://example.org/TestModel"));
                _localModel.Clear();
            }
            catch (Exception)
            {
                _localModel = modelManager.CreateModel(new Uri("http://example.org/TestModel"));
            }

            try
            {
                // Urn scheme
                _remoteModel = modelManager.GetRemoteModel(new Uri("http://example.org/TestModel"));
                
            }
            catch (Exception)
            {
            }
        }

        [SetUp]
        public void SetUp()
        {
            IResource model_resource = _localModel.CreateResource(new Uri("http://example.org/MyResource"));

            //TODO: all test all datatypes
            Property property = new Property(new Uri("http://example.org/MyProperty"));
            model_resource.AddProperty(rdf.type, nao.Agent);
            model_resource.AddProperty(property, "in the jungle");
            model_resource.AddProperty(property, 123);
            model_resource.AddProperty(property, DateTime.Now);
            model_resource.Commit();
        }

        [TearDown]
        public void TearDown()
        {
            _localModel.Clear();
        }

        //[Test]
        public void QueryTest()
        {
            ResourceQuery q = new ResourceQuery();
            q.Where(rdf.type, nao.Agent);
            var result = _remoteModel.ExecuteQuery(q);
            var b = result.GetResources();
            foreach (var res in b)
            {
                Console.WriteLine(res);
            }
        }


        //[Test]
        public void QueryDBPediaTest()
        {
            IModel m =  ModelManager.Instance.GetRemoteModel(new Uri("http://live.dbpedia.org/sparql"));
            ResourceQuery q = new ResourceQuery();
            //q.Where(dbpedia.wikiPageExternalLink, new Uri("http://dachau.de"));
            var result = m.ExecuteQuery(q); 
            var b = result.GetResources();
            foreach (var res in b)
            {
                Console.WriteLine(res);
            }
        }

        //[Test]
        public void QueryWordnetTest()
        {
            IModel m =  ModelManager.Instance.GetRemoteModel(new Uri("http://wordnet.rkbexplorer.com/sparql/"));
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
