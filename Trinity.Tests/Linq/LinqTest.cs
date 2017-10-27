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

using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace Semiodesk.Trinity.Test.Linq
{
    [TestFixture]
    class LinqTest
    {
        protected IStore Store { get; private set; }
        protected IModel Model { get; private set; }

        [SetUp]
        public void SetUp()
        {
            if (ResourceMappingTest.RegisteredOntology == false)
            {
                OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
                MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
                ResourceMappingTest.RegisteredOntology = true;
            }

            Store = StoreFactory.CreateStore("provider=dotnetrdf");
            Model = Store.CreateModel(new Uri("http://test.com/test"));

            Person p1 = Model.CreateResource<Person>();
            p1.FirstName = "Alice";
            p1.LastName = "Cooper";
            p1.Age = 69;
            p1.Commit();

            Person p2 = Model.CreateResource<Person>();
            p2.FirstName = "Bob";
            p2.LastName = "Dylan";
            p2.Age = 76;
            p2.Commit();

            Assert.IsFalse(Model.IsEmpty);
        }

        [Test]
        public void LinqTest1()
        {
            foreach(Person p in Model.GetResources<Person>())
            {
                Assert.Greater(p.Age, 50);
            }

            var persons = from p in Model.QueryResources<Person>() where p.Age < 50 select p;

            Assert.AreEqual(0, persons.ToList().Count);

            persons = from p in Model.QueryResources<Person>() where p.Age > 50 select p;

            Assert.AreEqual(2, persons.ToList().Count);
        }

        [Test]
        public void LinqTest2()
        {
            var x = from r in Model.QueryResources<MappingTestClass>() where r.uniqueIntTest < 5 && r.uniqueResourceTest.uniqueStringTest == "abc" && r.uniqueResourceTest.uniqueStringTest == "Test" || r.uniqueStringTest == "blub" select r;
            var ret = x.ToList();
        }
    }
}