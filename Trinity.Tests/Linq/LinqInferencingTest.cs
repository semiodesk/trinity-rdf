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
// Copyright (c) Semiodesk GmbH 2017

using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Test.Linq
{
    [TestFixture]
    public class LinqInferencingTest
    {
        protected IStore Store;

        protected IModel Model;

        [SetUp]
        public void SetUp()
        {
            // DotNetRdf memory store.
            //string connectionString = "provider=dotnetrdf";

            // OpenLink Virtoso store.
            string connectionString = string.Format("{0};rule=urn:semiodesk/test/ruleset", SetupClass.ConnectionString);

            Store = StoreFactory.CreateStore(connectionString);
            Store.LoadOntologySettings();

            Model = Store.CreateModel(new Uri("http://test.com/test"));
            Model.Clear();

            Assert.IsTrue(Model.IsEmpty);

            Document doc = Model.CreateResource<Document>();
            doc.Title = "Hello World!";
            doc.Commit();

            Person p1 = Model.CreateResource<Person>();
            p1.FirstName = "Peter";
            p1.LastName = "Steel";
            p1.Made.Add(doc);
            p1.Commit();


            Assert.IsFalse(Model.IsEmpty);
        }

        [Test]
        public void TestInverse()
        {
            var maker = from document in Model.AsQueryable<Document>(true) where document.Maker.FirstName == "Peter" select document.Maker;
            Assert.AreEqual(1, maker.ToList().Count);
        }

      
    }
}