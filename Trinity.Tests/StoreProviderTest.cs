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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity.Store;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    class StoreProviderTest
    {
        [Test]
        public void ParseConfigurationTest()
        {
            Dictionary<string, string> result = StoreFactory.ParseConfiguration("provider=virtuoso");
            Assert.AreEqual("virtuoso", result["provider"]);

            result = StoreFactory.ParseConfiguration("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");
            Assert.AreEqual("localhost", result["host"]);
            Assert.AreEqual("1111", result["port"]);
            Assert.AreEqual("dba", result["uid"]);
            Assert.AreEqual("dba", result["pw"]);

            result = StoreFactory.ParseConfiguration("provider=virtuoso;path=c:/path/to/my/files.ext");
            Assert.AreEqual("c:/path/to/my/files.ext", result["path"]);

            result = StoreFactory.ParseConfiguration("provider=dotnetrdf;schema=c:/path/to/my/schema1.rdf,c:/path/to/my/schema2.rdf");

        }

        [Test]
        public void DotNetRDFConfigTest()
        {
          dotNetRDFStoreProvider p = new dotNetRDFStoreProvider();
          p.GetStore(StoreFactory.ParseConfiguration("provider=dotnetrdf;schema=Models/rdf-schema.rdf,Models/rdf-syntax.rdf"));
        }

        [Test]
        public void VirtuosoConfigurationStringTest()
        {
            IStore anObject = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");
            Assert.IsNotNull(anObject);
            anObject.Dispose();
        }


    }
}
