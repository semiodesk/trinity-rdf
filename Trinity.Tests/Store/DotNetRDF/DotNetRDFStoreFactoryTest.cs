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
using System.Text;

namespace Semiodesk.Trinity.Tests.DotNetRDF
{
    [TestFixture]
    class DotNetRDFStoreFactory
    {
        [Test]
        public void TestMemoryStoreMethod()
        {
            var p = new Property(new Uri("ex:myProperty"));

            var store = StoreFactory.CreateMemoryStore();
            
            var model = store.CreateModel(new UriRef("semio:test"));
            
            Assert.IsNotNull(model);
            
            model.Clear();
            
            Assert.IsTrue(model.IsEmpty);
            
            var x = model.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            
            Assert.IsFalse(model.IsEmpty);
            
            store.Dispose();
        }

        [Test]
        public void TestOpenClose()
        {
            var p = new Property(new Uri("ex:myProperty"));

            var store = StoreFactory.CreateStore("provider=dotnetrdf");
            
            var model = store.CreateModel(new UriRef("semio:test"));
            model.Clear();
            
            var x = model.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            
            store.Dispose();
            store = StoreFactory.CreateStore("provider=dotnetrdf");
            
            model = store.CreateModel(new UriRef("semio:test"));
            model.Clear();
            
            x = model.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            
            store.Dispose();
            store = StoreFactory.CreateStore("provider=dotnetrdf");
            
            model = store.CreateModel(new UriRef("semio:test"));
            model.Clear();
            
            x = model.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            
            store.Dispose();
            store = StoreFactory.CreateStore("provider=dotnetrdf");
            
            model = store.CreateModel(new UriRef("semio:test"));
            model.Clear();
            
            x = model.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            
            store.Dispose();
            store = StoreFactory.CreateStore("provider=dotnetrdf");
            
            model = store.CreateModel(new UriRef("semio:test"));
            model.Clear();
            
            x = model.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            
            store.Dispose();
        }
    }
}
