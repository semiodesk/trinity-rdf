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
using Semiodesk.Trinity.Ontologies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Semiodesk.Trinity.Test
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
        }

        [Test]
        public void LinqTest1()
        {
            var x = from r in Model.ListResources<MappingTestClass>() where r.uniqueIntTest < 5 select r;
            var ret = x.ToList();
        }

        [Test]
        public void LinqTest2()
        {
            var x = from r in Model.ListResources<MappingTestClass>() where r.uniqueIntTest < 5 && r.uniqueResourceTest.uniqueStringTest == "abc" && r.uniqueResourceTest.uniqueStringTest == "Test" || r.uniqueStringTest == "blub" select r;
            var ret = x.ToList();
        }
    }
}