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

using System;
using System.IO;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace Semiodesk.Trinity.Test.Stardog
{
    /// <summary>
    /// How to set up test database on Windows:
    /// 
    /// 1. Download community edition
    /// 2. Unzip to folder
    /// 3. Copy license file "stardog-license-key.bin" to stardog folder
    /// 4. Open commandline in bin directory
    /// 5. Run 
    /// \>stardog-admin server start
    /// 
    /// 6. Open another commandline in bin directory
    /// 7. Run 
    /// \> stardog-admin user passwd
    /// 
    /// 8. Set password to admin when promted
    /// 9. Run 
    /// \>stardog-admin db create -n test
    /// </summary>
    [TestFixture]
    class StardogStoreTest : SetupClass
    {
        protected IStore Store;

        protected Uri ModelUri = new Uri("ex:Test");

        [SetUp]
        public void SetUp()
        {
            Store = StoreFactory.CreateStore("provider=stardog;host=http://localhost:5820;uid=admin;pw=admin;sid=test");
            Store.RemoveModel(ModelUri);
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
            Store.InitializeFromConfiguration();

            Assert.AreEqual(8, Store.ListModels().Count());
        }

        [Test]
        public void LoadOntologiesFromFileTest()
        {
            Assert.Inconclusive("How to make sure we have an empty store");

            /*
            string configFile = Path.Combine(Environment.CurrentDirectory, "custom.config");
            Store.InitializeFromConfiguration(configFile);

            Assert.AreEqual(4, Store.ListModels().Count());
            */
        }

        [Test]
        public void ListModelsTest()
        {
            IModel m = Store.CreateModel(ModelUri);

            Assert.IsNotNull(m);
            Assert.Greater(Store.ListModels().Count(), 0);
        }

        [Test]
        public void CreateModelTest()
        {
            IModel m = Store.CreateModel(ModelUri);

            Assert.IsNotNull(m);
        }

        #pragma warning disable CS0618 // Type or member is obsolete
        [Test]
        public void ContainsModelTest()
        {
            Store.RemoveModel(ModelUri);

            Assert.IsFalse(Store.ContainsModel(ModelUri));

            IModel m1 = Store.CreateModel(ModelUri);

            IResource r = m1.CreateResource(new Uri("ex:test:resource"));
            
            r.AddProperty(new Property(new Uri("ex:test:property")), "var");
            r.Commit();


            Assert.IsTrue(Store.ContainsModel(ModelUri));

            Assert.IsFalse(Store.ContainsModel(new Uri("ex:NoTest")));
        }
        #pragma warning restore CS0618 // Type or member is obsolete

        [Test]
        public void GetModelTest()
        {
            IModel m1 = Store.CreateModel(ModelUri);
            Assert.IsTrue(m1.IsEmpty);

            IResource r = m1.CreateResource(new Uri("ex:test:resource"));
            r.AddProperty(new Property(new Uri("ex:test:property")), "var");
            r.Commit();

            IModel m2 = Store.GetModel(ModelUri);

            Assert.AreEqual(ModelUri, m2.Uri);
            Assert.IsTrue(m2.ContainsResource(r));
        }

        [Test]
        public void RemoveModelTest()
        {
            Store.RemoveModel(ModelUri);

            IModel m1 = Store.CreateModel(ModelUri);

            IResource r = m1.CreateResource(new Uri("ex:test:resource"));
            r.AddProperty(new Property(new Uri("ex:test:property")), "var");
            r.Commit();

            IModel m2 = Store.GetModel(ModelUri);

            Assert.AreEqual(ModelUri, m2.Uri);

            Store.RemoveModel(ModelUri);

            m2 = Store.GetModel(ModelUri);

            Assert.IsTrue(m2.IsEmpty);
        }
    }
}
