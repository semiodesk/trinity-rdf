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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Semiodesk.Trinity;
using NUnit.Framework;
using System.Globalization;

namespace Semiodesk.Trinity.Test.Stardog
{
    [TestFixture]
    class StardogResourceTest
    {
        IStore Store;
        IModel Model;

        [SetUp]
        public void SetUp()
        {
            Store = StoreFactory.CreateStore("provider=stardog;host=http://localhost:5820;uid=admin;pw=admin;sid=test");

            Uri testModel = new Uri("ex:Test");
            Model = Store.CreateModel(testModel);
            Model.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Store.Dispose();
            Store = null;
        }

        #region Datatype fidelity Test

        [Test]
        public void TestBool()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);

            bool val = true;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(bool), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestInt()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);

            int val = 123;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(int), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestInt16()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            Int16 val = 124;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(Int16), res.GetType());
            Assert.AreEqual(val, res);

        }

        [Test]
        public void TestInt32()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            Int32 val = 125;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(Int32), res.GetType());
            Assert.AreEqual(val, res);

        }

        [Test]
        public void TestInt64()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            Int64 val = 126;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(Int64), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestUint()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            uint val = 126;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(uint), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestUint16()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            UInt16 val = 126;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(UInt16), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestUint32()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            UInt32 val = 126;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(UInt32), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestUint64()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            UInt64 val = 126;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(UInt64), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestFloat()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            float val = 1.234F;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(float), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestDouble()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            double val = 1.223;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(double), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestSingle()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            Single val = 1.223F;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(Single), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestString()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            string val = "Hello World!";
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(string), res.GetType());
            Assert.AreEqual(val, res);
        }

        [Test]
        public void TestLocalizedString()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = Model.CreateResource<Resource>(new Uri("ex:myResource"));
            string val = "Hello World!";
            var ci = CultureInfo.CreateSpecificCulture("EN");
            r.AddProperty(myProperty, val, ci);
            r.Commit();

            var r1 = Model.GetResource<Resource>(r.Uri);
            object res = r1.ListValues(myProperty).First();
            Assert.AreEqual(typeof(Tuple<string, string>), res.GetType());
            Tuple<string, string> v = res as Tuple<string, string>;
            Assert.AreEqual(val, v.Item1);
            Assert.AreEqual(ci.Name.ToLower(), v.Item2.ToLower());
            r.RemoveProperty(myProperty, val, ci);
        }

        [Test]
        public void TestDateTime()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);
            DateTime val = DateTime.Today;
            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(DateTime), res.GetType());
            Assert.AreEqual(val.ToLocalTime(), ((DateTime)res).ToLocalTime());
        }

        [Test]
        public void TestByteArray()
        {
            Uri resourceUri = new Uri("ex:myResource");
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = Model.CreateResource<Resource>(resourceUri);

            byte[] val = new byte[] { 1, 2, 3, 4, 5 };

            r1.AddProperty(myProperty, val);
            r1.Commit();
            r1 = Model.GetResource<Resource>(resourceUri);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(typeof(byte[]), res.GetType());
            Assert.AreEqual(val, res);

        }


        #endregion

    }
}
