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
        private IStore _store;

        [SetUp]
        public void SetUp()
        {
           _store = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");

           Uri modelUri1 = new Uri("http://example.org/TestModel");

            if( _store.ContainsModel(modelUri1) )
            {
                // Default uri scheme
                _model = _store.GetModel(modelUri1);
                _model.Clear();
            }
            else
            {
                _model = _store.CreateModel(modelUri1);
            }

            Uri modelUri = new Uri("http://example.org/TestModel2");

            if( _store.ContainsModel(modelUri))
            {
                // Urn scheme
                _model2 = _store.GetModel(modelUri);
                _model2.Clear();
            }
            else
            {
                _model2 = _store.CreateModel(modelUri);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _model.Clear();
            _model2.Clear();
            _store.Dispose();
        }

        [Test]
        public void ContainsResourceTest()
        {
            Uri resourceUri = new Uri("http://example.com/testResource");

            IModelGroup g = _store.CreateModelGroup(_model.Uri, _model2.Uri);
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

            IModelGroup g = _store.CreateModelGroup(_model.Uri, _model2.Uri);
            
            Assert.Throws(typeof(ArgumentException), new TestDelegate( () => g.GetResource(resourceUri)));
            
            IResource resource = _model.CreateResource(resourceUri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            IResource res = g.GetResource(resourceUri);
            Assert.IsNotNull(res);
            Assert.IsTrue(res.IsReadOnly);
            Assert.AreEqual(resourceUri, res.Uri);
            Assert.Contains(nco.Contact, res.ListValues(rdf.type));


            resource = _model2.CreateResource(resourceUri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            res = g.GetResource(resourceUri);
            Assert.IsNotNull(res);
            Assert.AreEqual(1, res.ListValues(rdf.type).Count);
            Assert.IsTrue(res.IsReadOnly);
            Assert.AreEqual(resourceUri, res.Uri);
            Assert.Contains(nco.Contact, res.ListValues(rdf.type));

        }

        [Test]
        public void ResourceQueryTest()
        {
            Uri resourceUri = new Uri("http://example.com/testResource");

            IModelGroup g = _store.CreateModelGroup(_model.Uri, _model2.Uri);

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
            IModelGroup modelGroup = _store.CreateModelGroup(_model.Uri, _model2.Uri);
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
