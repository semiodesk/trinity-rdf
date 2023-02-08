﻿// LICENSE:
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

using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using System.Linq;
using System;

namespace Semiodesk.Trinity.Test.Fuseki
{
    [TestFixture]
    public class ModelGroupTest : SetupClass
    {
        protected IStore Store;

        protected IModel Model = null;

        protected IModel Model2 = null;

        [SetUp]
        public void SetUp()
        {
            string connectionString = SetupClass.ConnectionString;

            Store = StoreFactory.CreateStore(string.Format("{0};rule=urn:semiodesk/test/ruleset", connectionString));

            Model = Store.GetModel(new Uri("http://example.org/TestModel"));

            if (!Model.IsEmpty)
            {
                Model.Clear();
            }

            Model2 = Store.GetModel(new Uri("http://example.org/TestModel2"));

            if (!Model.IsEmpty)
            {
                Model.Clear();
            }
        }

        [TearDown]
        public void TearDown()
        {
            Model.Clear();
            Model2.Clear();
            Store.Dispose();
        }

        [Test]
        public void ContainsResourceTest()
        {
            Uri uri = new Uri("http://example.com/testResource");

            IModelGroup group = Store.CreateModelGroup(Model.Uri, Model2.Uri);

            Assert.IsFalse(group.ContainsResource(uri));

            IResource resource = Model.CreateResource(uri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();
            
            Assert.IsTrue(group.ContainsResource(uri));

            Model.DeleteResource(resource);

            Assert.IsFalse(group.ContainsResource(uri));

            resource = Model2.CreateResource(uri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            Assert.IsTrue(group.ContainsResource(uri));
        }

        [Test]
        public void DeleteResouceTest2()
        {
            var uri = new Uri("ex:Resource2");

            IResource resource = Model.CreateResource(uri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            Assert.IsTrue(Model.ContainsResource(uri));

            Model.DeleteResource(resource);

            Assert.IsFalse(Model.ContainsResource(uri));
        }

        [Test]
        public void GetResourceTest()
        {
            Uri resourceUri = new Uri("http://example.com/testResource");

            IModelGroup g = Store.CreateModelGroup(Model.Uri, Model2.Uri);
            
            Assert.Throws<ResourceNotFoundException>(new TestDelegate( () => g.GetResource(resourceUri)));
            
            IResource resource = Model.CreateResource(resourceUri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            IResource res = g.GetResource(resourceUri);
            Assert.IsNotNull(res);
            Assert.IsTrue(res.IsReadOnly);
            Assert.AreEqual(resourceUri, res.Uri);
            Assert.Contains(nco.Contact, res.ListValues(rdf.type).ToList());


            resource = Model2.CreateResource(resourceUri);
            resource.AddProperty(rdf.type, nco.Contact);
            resource.Commit();

            res = g.GetResource(resourceUri);
            Assert.IsNotNull(res);
            Assert.AreEqual(1, res.ListValues(rdf.type).Count());
            Assert.IsTrue(res.IsReadOnly);
            Assert.AreEqual(resourceUri, res.Uri);
            Assert.Contains(nco.Contact, res.ListValues(rdf.type).ToList());

        }

        [Test]
        public void LazyLoadResourceTest()
        {
            IModel model = Model;
            IModelGroup modelGroup = Store.CreateModelGroup(Model.Uri, Model2.Uri);
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
