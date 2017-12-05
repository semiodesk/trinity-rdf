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
using System.IO;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.Linq;
using System.Reflection;

namespace dotNetRDFStore.Test
{
    [TestFixture]
    class StoreTest
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
            Store.Dispose();
            Store = null;
        }

        [Test]
        public void LoadOntologiesTest()
        {
            Uri testModel = new Uri("ex:Test");

            Store.LoadOntologySettings();

            Assert.AreEqual(6, Store.ListModels().Count());
        }

        [Test]
        public void LoadOntologiesFromFileTest()
        {
            Uri testModel = new Uri("ex:Test");

            DirectoryInfo assembly = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

            string configFile = Path.Combine(assembly.FullName, "custom.config");

            Store.LoadOntologySettings(configFile);

            Assert.AreEqual(4, Store.ListModels().Count());


        }

        [Test]
        public void AddModelTest()
        {
            Uri testModel = new Uri("ex:Test");

            IModel m = Store.CreateModel(testModel);

            Assert.IsNotNull(m);
        }

        [Test]
        public void ContainsModelTest()
        {
            Uri testModel = new Uri("ex:Test");

            Assert.IsFalse(Store.ContainsModel(testModel));

            IModel m = Store.CreateModel(testModel);

            var res = m.CreateResource(new Uri("ex:test:resource"));
            
            res.AddProperty(new Property(new Uri("ex:test:property")), "var");
            res.Commit();

            Assert.IsTrue(Store.ContainsModel(testModel));
            Assert.IsFalse(Store.ContainsModel(new Uri("ex:NoTest")));
        }

        [Test]
        public void GetModelTest()
        {
            Uri testModel = new Uri("ex:Test");

            IModel m1 = Store.CreateModel(testModel);

            IResource r = m1.CreateResource(new Uri("ex:test:resource"));
            r.AddProperty(new Property(new Uri("ex:test:property")), "var");
            r.Commit();

            IModel m2 = Store.GetModel(testModel);

            Assert.AreEqual(testModel, m2.Uri);
            Assert.IsTrue(m2.ContainsResource(r));
        }

        [Test]
        public void RemoveModelTest()
        {
            Uri testModel = new Uri("ex:Test");

            IModel m1 = Store.CreateModel(testModel);

            var res = m1.CreateResource(new Uri("ex:test:resource"));
            res.AddProperty(new Property(new Uri("ex:test:property")), "var");
            res.Commit();

            IModel m2 = Store.GetModel(testModel);
            Assert.AreEqual(testModel, m2.Uri);

            Store.RemoveModel(testModel);

            m2 = Store.GetModel(testModel);

            Assert.IsTrue(m2.IsEmpty);
        }
    }
}
