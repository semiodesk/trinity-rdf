using NUnit.Framework;
using Semiodesk.Trinity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Semiodesk.Trinity.Tests;

namespace dotNetRDFStore.Test
{


    [TestFixture]
    class ModelTest
    {
        IStore Store;
        IModel Model;

        [SetUp]
        public void SetUp()
        {
            Store = Stores.CreateStore("provider=dotnetrdf");
            Uri testModel = new Uri("ex:Test");
            Model = Store.CreateModel(testModel);
        }

        [TearDown]
        public void TearDown()
        {
            Store.Dispose();
            Store = null;
        }

        [Test]
        public void CreateResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            Assert.AreEqual(resourceUri, result.Uri);
            List<Property> properties = result.ListProperties();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, result.GetValue(property));
        }

        [Test]
        public void ModifyResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            
            result.RemoveProperty(property, literal);
            literal = "var2";
            result.AddProperty(property, literal);
            result.Commit();

            result = Model.GetResource(resourceUri);

            Assert.AreEqual(resourceUri, result.Uri);
            List<Property> properties = result.ListProperties();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, result.GetValue(property));

        }

        [Test]
        public void RemoveResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);

            Model.DeleteResource(result);

            Assert.IsFalse(Model.ContainsResource(result));

        }

        [Test]
        public void GetResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            Assert.AreEqual(resourceUri, result.Uri);
            List<Property> properties = result.ListProperties();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, result.GetValue(property));
        }

        [Test]
        public void ResourceQueryTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            ResourceQuery q = new ResourceQuery();
            q.Where(property, literal);
            var queryResult = Model.ExecuteQuery(q);
            Assert.AreEqual(1, queryResult.Count());
            var result = queryResult.GetResources<Resource>();
            Assert.AreEqual(1, result.Count());
            var resultResource = result.First();
            Assert.AreEqual(resourceUri, resultResource.Uri);
            List<Property> properties = resultResource.ListProperties();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, resultResource.GetValue(property));
        }


        [Test]
        public void ResourceQueryCountTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            ResourceQuery q = new ResourceQuery();
            q.Where(property, literal);
            var queryResult = Model.ExecuteQuery(q);

            Assert.AreEqual(1, queryResult.Count());
        }

        [Test]
        public void Mapped_ResourceQueryTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var res = Model.CreateResource<SingleMappingTestClass>(resourceUri);
            res.stringTest.Add(literal);
            res.Commit();

            ResourceQuery q = new ResourceQuery(TestOntology.SingleMappingTestClass);
            q.Where(TestOntology.stringTest, literal);
            var queryResult = Model.ExecuteQuery(q);

            var result = queryResult.GetResources<SingleMappingTestClass>();
            Assert.AreEqual(1, result.Count());
            var resultResource = result.First();
            Assert.AreEqual(resourceUri, resultResource.Uri);
            Assert.AreEqual(1, resultResource.stringTest.Count);
            
        }



        [Test]
        public void ContainsResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            Assert.AreEqual(true, Model.ContainsResource(new UriRef("ex:test:resource")));
        }

        [Test]
        public void AskQueryTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            SparqlQuery q = new SparqlQuery("ASK { <ex:test:resource> ?p ?o . }");
            var b = Model.ExecuteQuery(q);
        }

        [Test]
        public void SparqlQueryTest()
        {

        }
    }
}
