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
using Semiodesk.Trinity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dotNetRDFStore.Test
{
    [TestFixture]
    class StoreTest : SetupClass
    {
        IStore Store;

        [SetUp]
        public void SetUp()
        {
            Store = StoreFactory.CreateStore("provider=dotnetrdf");
        }

        [TearDown]
        public void TearDown()
        {
            if(Store != null)
            {
                Store.Dispose();
                Store = null;
            }
        }

        [Test]
        public void LoadOntologiesTest()
        {
            Store.InitializeFromConfiguration();

            List<IModel> models0 = Store.ListModels().ToList();

            // Note: the NCO ontology contains a metadata graph.
            Assert.AreEqual(8, models0.Count);
        }

        [Test]
        public void LoadOntologiesFromFileTest()
        {
            string configFile = Path.Combine(Environment.CurrentDirectory, "custom.config");

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
            string configFile = Path.Combine(Environment.CurrentDirectory, "without_store.config");

            Store.InitializeFromConfiguration(configFile);

            Assert.AreEqual(4, Store.ListModels().Count());
        }

        [Test]
        public void AddModelTest()
        {
            IModel model = Store.CreateModel(new Uri("ex:Test"));

            Assert.IsNotNull(model);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        [Test]
        public void ContainsModelTest()
        {
            Uri testModel = new Uri("ex:Test");

            Assert.IsFalse(Store.ContainsModel(testModel));

            IModel model = Store.CreateModel(testModel);

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
            Uri testModel = new Uri("ex:Test");

            IModel model0 = Store.CreateModel(testModel);

            IResource r = model0.CreateResource(new Uri("ex:test:resource"));
            r.AddProperty(new Property(new Uri("ex:test:property")), "var");
            r.Commit();

            IModel model1 = Store.GetModel(testModel);

            Assert.AreEqual(testModel, model1.Uri);
            Assert.IsTrue(model1.ContainsResource(r));
        }

        [Test]
        public void RemoveModelTest()
        {
            Uri testModel = new Uri("ex:Test");

            IModel model0 = Store.CreateModel(testModel);

            var res = model0.CreateResource(new Uri("ex:test:resource"));
            res.AddProperty(new Property(new Uri("ex:test:property")), "var");
            res.Commit();

            IModel model1 = Store.GetModel(testModel);
            Assert.AreEqual(testModel, model1.Uri);

            Store.RemoveModel(testModel);

            model1 = Store.GetModel(testModel);

            Assert.IsTrue(model1.IsEmpty);
        }
    }
}
