using System;
using System.IO;
using NUnit.Framework;
using Semiodesk.Trinity;

namespace dotNetRDFStore.Test
{


    [TestFixture]
    class StoreTest
    {
        IStore Store;

        [SetUp]
        public void SetUp()
        {
            Store = Stores.CreateStore("provider=dotnetrdf");
        }

        [TearDown]
        public void TearDown()
        {
            Store.Dispose();
            Store = null;
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

            Assert.IsFalse(Store.ContainsModel(testModel));

            IModel m = Store.CreateModel(testModel);

            var res = m.CreateResource(new Uri("ex:test:resource"));

            res.AddProperty(new Property(new Uri("ex:test:property")), "var");
            res.Commit();

            Assert.IsTrue(Store.ContainsModel(testModel));

            IModel model2 = Store.GetModel(testModel);
            Assert.AreEqual(testModel, model2.Uri);

            Assert.IsTrue(model2.ContainsResource(res));
        }

        [Test]
        public void RemoveModelTest()
        {
            Uri testModel = new Uri("ex:Test");

            Assert.IsFalse(Store.ContainsModel(testModel));

            IModel m = Store.CreateModel(testModel);

            var res = m.CreateResource(new Uri("ex:test:resource"));

            res.AddProperty(new Property(new Uri("ex:test:property")), "var");
            res.Commit();

            Assert.IsTrue(Store.ContainsModel(testModel));

            IModel model2 = Store.GetModel(testModel);
            Assert.AreEqual(testModel, model2.Uri);

            Store.RemoveModel(testModel);
            Assert.IsFalse(Store.ContainsModel(testModel));

            model2 = Store.GetModel(testModel);
            Assert.IsNull(model2);
        }

    }
}
