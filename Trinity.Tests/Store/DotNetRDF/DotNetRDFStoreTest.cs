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

using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests.Store;
using System.IO;
using System.Linq;
using System;

namespace Semiodesk.Trinity.Tests.DotNetRDF
{
    [TestFixture]
    class DotNetRDFStoreTest : StoreTest<DotNetRDFTestSetup>
    {
        #region Members

        private UriRef _r1;

        private Property _p1;
        
        #endregion
        
        #region Methods
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _r1 = BaseUri.GetUriRef("r1");
            _p1 = new Property(BaseUri.GetUriRef("p1"));
        }
        
        private void InitializeModels()
        {
            var r1 = Model1.CreateResource(_r1);
            r1.AddProperty(_p1, "in the jungle");
            r1.AddProperty(_p1, 123);
            r1.AddProperty(_p1, DateTime.Now);
            r1.Commit();
        }
        
        [Test]
        public void LoadOntologiesTest()
        {
            Store = StoreFactory.CreateStore(ConnectionString);
            Store.InitializeFromConfiguration();
            
            var models = Store.ListModels().ToList();

            Assert.AreEqual(8, models.Count);
        }

        [Test]
        public void LoadOntologiesFromFileTest()
        {
            // Reset the store that was initialized in base.SetUp();
            Store = StoreFactory.CreateStore(ConnectionString);
            
            var configFile = Path.Combine(Environment.CurrentDirectory, "custom.config");

            Store.InitializeFromConfiguration(configFile);

            Assert.AreEqual(4, Store.ListModels().Count());

            configFile = Path.Combine(Environment.CurrentDirectory, "nonexistent.config");

            Assert.Throws<FileNotFoundException>(() =>
            {
                Store.InitializeFromConfiguration(configFile);
            });
        }

        [Test]
        public void LoadOntologiesFromFileWithoutStoreTest()
        {
            // Reset the store that was initialized in base.SetUp();
            Store = StoreFactory.CreateStore(ConnectionString);
            
            var configFile = Path.Combine(Environment.CurrentDirectory, "without_store.config");

            Store.InitializeFromConfiguration(configFile);

            Assert.AreEqual(4, Store.ListModels().Count());
        }

        [Test]
        public void AddModelTest()
        {
            var model = Store.CreateModel(new Uri("ex:Test"));

            Assert.IsNotNull(model);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        [Test]
        public void ContainsModelTest()
        {
            var testModel = new Uri("ex:Test");

            Assert.IsFalse(Store.ContainsModel(testModel));

            var model = Store.CreateModel(testModel);

            var r = model.CreateResource(new Uri("ex:test:resource"));
            r.AddProperty(new Property(new Uri("ex:test:property")), "var");
            r.Commit();

            Assert.IsTrue(Store.ContainsModel(testModel));
            Assert.IsFalse(Store.ContainsModel(new Uri("ex:NoTest")));
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [Test]
        public void GetModelTest()
        {
            InitializeModels();
            
            var model1 = Store.GetModel(Model1.Uri);

            Assert.AreEqual(Model1.Uri, model1.Uri);
            Assert.IsTrue(model1.ContainsResource(_r1));
        }

        [Test]
        public void RemoveModelTest()
        {
            var r1 = Model1.CreateResource(_r1);
            r1.AddProperty(_p1, "x");
            r1.Commit();

            var model = Store.GetModel(Model1.Uri);
            
            Assert.AreEqual(Model1.Uri, model.Uri);

            Store.RemoveModel(model);

            model = Store.GetModel(Model1.Uri);

            Assert.IsTrue(model.IsEmpty);
        }

        [Test]
        public void ReadJsonLdContentTest()
        {
            var modelUri = new Uri("http://trinty-rdf.net/models/test/jsonld");

            var model = Store.GetModel(modelUri);

            Assert.IsTrue(model.IsEmpty);

            string content = @"
            [
              {
                '@type': ['http://www.w3.org/2002/07/owl#Class'],
                '@id': 'https://ontologies.semanticarts.com/gist/Message',
                'http://www.w3.org/2000/01/rdf-schema#label': 'Message',
                'http://schema.org/name': [{ '@value': 'Message', '@language': 'en' }],
                'http://schema.org/description': [
                  {
                    '@value': 'A specific instance of content sent from an Organization, Person, or Application to at least one other Organization, Person, or Application.',
                    '@language': 'en'
                  }
                ]
              }
            ]
            ";

            Store.Read(content, modelUri, RdfSerializationFormat.JsonLd, false);

            Assert.IsFalse(model.IsEmpty);

            var r = model.GetResource(new Uri("https://ontologies.semanticarts.com/gist/Message"));

            Assert.IsNotNull(r);
            Assert.AreEqual(r.GetValue(rdfs.label), "Message");
        }
        
        #endregion
    }
}
