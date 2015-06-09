﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class ModelGroupTest
    {
        private IModel _model = null;
        private IModel _model2 = null;
        private ModelManager ModelManager;

        [SetUp]
        public void SetUp()
        {
            ModelManager = ModelManager.Instance;
            ModelManager.Connect();

            try
            {
                // Default uri scheme
                _model = ModelManager.GetModel(new Uri("http://example.org/TestModel"));
                _model.Clear();
            }
            catch (Exception)
            {
                _model = ModelManager.CreateModel(new Uri("http://example.org/TestModel"));
            }

            try
            {
                // Urn scheme
                _model2 = ModelManager.GetModel(new Uri("http://example.org/TestModel2"));
                _model2.Clear();
            }
            catch (Exception)
            {
                _model2 = ModelManager.CreateModel(new Uri("http://example.org/TestModel2"));
            }
        }

        [TearDown]
        public void TearDown()
        {
            _model.Clear();
            _model2.Clear();
            ModelManager.Disconnect();
        }

        [Test]
        public void ContainsResourceTest()
        {
            Uri resourceUri = new Uri("http://example.com/testResource");

            IModelGroup g = ModelManager.CreateModelGroup(_model.Uri, _model2.Uri);
            bool res = g.ContainsResource(resourceUri);
            Assert.IsFalse(res);

            IResource resource = _model.CreateResource(resourceUri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();
            
            res = g.ContainsResource(resourceUri);
            Assert.IsTrue(res);

            _model.DeleteResource(resource);

            res = g.ContainsResource(resourceUri);
            Assert.IsFalse(res);

            resource = _model2.CreateResource(resourceUri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            res = g.ContainsResource(resourceUri);
            Assert.IsTrue(res);
        }

        [Test]
        public void GetResourceTest()
        {
            Uri resourceUri = new Uri("http://example.com/testResource");

            IModelGroup g = ModelManager.CreateModelGroup(_model.Uri, _model2.Uri);
            
            Assert.Throws(typeof(ArgumentException), new TestDelegate( () => g.GetResource(resourceUri)));
            
            IResource resource = _model.CreateResource(resourceUri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            IResource res = g.GetResource(resourceUri);
            Assert.IsNotNull(res);
            Assert.IsTrue(res.IsReadOnly);
            Assert.AreEqual(resourceUri, res.Uri);
            Assert.Contains(nco.Contact, res.ListValues(rdf.type));
            
        }

        [Test]
        public void ResourceQueryTest()
        {
            Uri resourceUri = new Uri("http://example.com/testResource");

            IModelGroup g = ModelManager.CreateModelGroup(_model.Uri, _model2.Uri);

            Contact resource = _model.CreateResource<Contact>(resourceUri);
            resource.Fullname = "Hans Peter";
            resource.Commit();

            ResourceQuery q = new ResourceQuery(nco.Contact);
            var res = g.GetResources(q);

            
            

        }

        [Test]
        public void LazyLoadResourceTest()
        {
            MappingDiscovery.RegisterCallingAssembly();
            IModel model = _model;
            IModelGroup modelGroup = ModelManager.CreateModelGroup(_model.Uri, _model2.Uri);
            model.Clear();

            Uri testRes1 = new Uri("semio:test:testInstance");
            Uri testRes2 = new Uri("semio:test:testInstance2");
            MappingTestClass t1 = model.CreateResource<MappingTestClass>(testRes1);
            MappingTestClass2 t2 = model.CreateResource<MappingTestClass2>(new Uri("semio:test:testInstance2"));

            t1.uniqueResourceTest = t2;
            // TODO: Debug messsage, because t2 was not commited
            t1.Commit();

            MappingTestClass p1 = modelGroup.GetResource<MappingTestClass>(testRes1);
            //Assert.AreEqual(null, p1.uniqueResourceTest);

            var v = p1.ListValues(TestOntology.uniqueResourceTest);
            Assert.AreEqual(t2.Uri.OriginalString, (v.First() as IResource).Uri.OriginalString);

            model.DeleteResource(t1);

            model.DeleteResource(t2);

            t1 = model.CreateResource<MappingTestClass>(testRes1);

            t2 = model.CreateResource<MappingTestClass2>(new Uri("semio:test:testInstance2"));
            t2.Commit();

            t1.uniqueResourceTest = t2;
            t1.Commit();

            var tt1 = modelGroup.GetResource<MappingTestClass>(testRes1);
            Assert.AreEqual(t2, tt1.uniqueResourceTest);

            IResource tr1 = modelGroup.GetResource(testRes1);
            Assert.AreEqual(typeof(MappingTestClass), tr1.GetType());

        }
    }
}
