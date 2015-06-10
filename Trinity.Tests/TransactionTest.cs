using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    class TransactionTest
    {
        UriRef transactionModel = new UriRef("ex:TransactionTest");
        IStore Store;
        string providerString = "provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba";


        [SetUp]
        public void SetUp()
        {
            if (ResourceMappingTest.RegisteredOntology == false)
            {
                OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
                MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
                RegisterOntologies.Register();
                ResourceMappingTest.RegisteredOntology = true;
            }

            Store = Stores.CreateStore(providerString);

            if (Store.ContainsModel(transactionModel))
            {
                Store.RemoveModel(transactionModel);
            }
            IModel m = Store.CreateModel(transactionModel);

            


        }

        [TearDown]
        public void TearDown()
        {
            if (Store.ContainsModel(transactionModel))
            {
                Store.RemoveModel(transactionModel);
            }
        }

        public IModel GetModel(out IStore store)
        {
            store = Stores.CreateStore(providerString);
            return store.GetModel(transactionModel);
        }

        [Test]
        public void AddingElements()
        {
            List<SingleMappingTestClass> list1 = new List<SingleMappingTestClass>();
            List<SingleMappingTestClass> list2 = new List<SingleMappingTestClass>();

            Barrier sync = new Barrier(3);

            Thread worker1 = new Thread(() =>
            {
                IStore s;
                IModel m = GetModel(out s);
                ITransaction t = m.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                sync.SignalAndWait();
                for (int i = 0; i < 50; i++)
                {
                    var res = m.CreateResource<SingleMappingTestClass>(new Uri("ex:Resource:Thread1#" + i), t);
                    res.stringTest.Add("Thread1 " + i.ToString());
                    res.Commit();
                    list1.Add(res);
                }
                t.Commit();
                s.Dispose();
            });

            Thread worker2 = new Thread(() =>
            {
                IStore s;
                IModel m = GetModel(out s);
                ITransaction t = m.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                sync.SignalAndWait();
                for (int i = 0; i < 50; i++)
                {
                    var res = m.CreateResource<SingleMappingTestClass>(new Uri("ex:Resource:Thread2#" + i), t);
                    res.stringTest.Add("Thread2 " + i.ToString());
                    res.Commit();
                    list2.Add(res);
                }
                t.Commit();
                s.Dispose();
            });

            worker1.Start();
            worker2.Start();
            sync.SignalAndWait();

            worker1.Join();
            worker2.Join();

            Assert.AreEqual(50, list1.Count());
            Assert.AreEqual(50, list2.Count());

            IModel model = Store.GetModel(transactionModel);
            foreach (var res in list1)
            {
                var actual = model.GetResource<SingleMappingTestClass>(res.Uri);
                Assert.AreEqual(res.stringTest.Count(), actual.stringTest.Count());
                Assert.AreEqual(res.stringTest[0], actual.stringTest[0]);
            }

            foreach (var res in list2)
            {
                var actual = model.GetResource<SingleMappingTestClass>(res.Uri);
                Assert.AreEqual(res.stringTest.Count(), actual.stringTest.Count());
                Assert.AreEqual(res.stringTest[0], actual.stringTest[0]);
            }
        }


        [Test]
        public void ModifyElement()
        {
            IModel model = Store.GetModel(transactionModel);
            var newResource = model.CreateResource<SingleMappingTestClass>();
            newResource.stringTest.Add("Hello");
            newResource.stringTest.Add("my");
            newResource.stringTest.Add("dear");
            newResource.Commit();

            Barrier sync = new Barrier(3);
            Barrier sync2 = new Barrier(2);

            Thread worker1 = new Thread(() =>
            {
                IStore s;
                IModel m = GetModel(out s);

                try
                {
                    ITransaction t = m.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
                    sync.SignalAndWait();
                    var res = m.GetResource<SingleMappingTestClass>(newResource.Uri, t);
                    
                    res.stringTest.Add("Thread1");
                    res.stringTest.Remove("my");
                    
                    res.Commit();
                    t.Commit();
                    
                    s.Dispose();
                }
                catch (Exception)
                {
                    sync2.SignalAndWait();
                }
            });

            Thread worker2 = new Thread(() =>
            {
                try
                {
                    IStore s;
                    IModel m = GetModel(out s);
                    ITransaction t = m.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);

                    
                    var res = m.GetResource<SingleMappingTestClass>(newResource.Uri, t);
                    sync.SignalAndWait();
                    res.stringTest.Add("Thread2");
                    res.stringTest.Remove("dear");
                    sync2.SignalAndWait();
                    res.Commit();
                    t.Commit();
                    s.Dispose();
                }
                catch (Exception) {}
            });

            // Start both threads
            worker1.Start();
            worker2.Start();

            // Wait to enter transaction
            sync.SignalAndWait();

            // Wait for threads to end
            worker2.Join();
            worker1.Join();
            

            var actualResource = model.GetResource<SingleMappingTestClass>(newResource.Uri);
            Assert.IsFalse(actualResource.stringTest.Contains("dear"));
            Assert.IsTrue(actualResource.stringTest.Contains("my"));
            Assert.IsTrue(actualResource.stringTest.Contains("Thread2"));

        }


        [Test]
        public void ModifyAndAddElement()
        {
            IModel model = Store.GetModel(transactionModel);
            var newResource = model.CreateResource<SingleMappingTestClass>();
            newResource.stringTest.Add("Hello");
            newResource.stringTest.Add("my");
            newResource.stringTest.Add("dear");
            newResource.Commit();

            Uri addedResourceUri = new Uri("ex:blub");

            Barrier sync = new Barrier(3);

            Thread worker1 = new Thread(() =>
            {
                IStore s;
                IModel m = GetModel(out s);
                ITransaction t = m.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                Debug.WriteLine("Worker 1: Started Transaction");
                sync.SignalAndWait();
                var res = m.CreateResource<SingleMappingTestClass>(addedResourceUri);
                res.stringTest.Add("Thread1");
                
                res.Commit();
                Debug.WriteLine("Worker 1: Commit Resource");
                t.Commit();
                Debug.WriteLine("Worker 1: Commit Transaction");
                s.Dispose();
            });

            Thread worker2 = new Thread(() =>
            {
                IStore s;
                IModel m = GetModel(out s);
                ITransaction t = m.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
                Debug.WriteLine("Worker 2: started Transaction");
                sync.SignalAndWait();

                try
                {
                    ModifyData(m, newResource.Uri, t);
                }catch(Exception)
                {
                    // Retry
                    ModifyData(m, newResource.Uri, t);
                }

                Debug.WriteLine("Worker 2: Commit Resource");
                t.Commit();
                Debug.WriteLine("Worker 2: Commit Transcation");
                s.Dispose();

            });

            worker1.Start();
            worker2.Start();
            sync.SignalAndWait();

            worker1.Join();
            worker2.Join();

            var actualResource = model.GetResource<SingleMappingTestClass>(newResource.Uri);
            var actualResource2 = model.GetResource<SingleMappingTestClass>(addedResourceUri);


        }

        protected void ModifyData(IModel m, Uri uri, ITransaction t)
        {
            var res = m.GetResource<SingleMappingTestClass>(uri, t);
            res.stringTest.Add("Thread2");
            res.stringTest.Remove("dear");
            res.Commit();
        }
    }
}
