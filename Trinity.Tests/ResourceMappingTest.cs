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
using System.Globalization;
using Semiodesk.Trinity;
using System.Collections.ObjectModel;
using Semiodesk.Trinity.Ontologies;
using System.Reflection;
using NUnit.Framework;
using Semiodesk.Trinity.Test;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif

namespace Semiodesk.Trinity.Test
{
    public class SingleMappingTestClass : Resource
    {
        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            yield return TestOntology.SingleMappingTestClass;
        }

        protected PropertyMapping<ObservableCollection<string>> stringTestMapping =
            new PropertyMapping<ObservableCollection<string>>("stringTest", TestOntology.stringTest, new ObservableCollection<string>());

        public ObservableCollection<string> stringTest
        {
            get { return GetValue(stringTestMapping); }
            set { SetValue(stringTestMapping, value); }
        }

        #endregion

        #region Constructors

        public SingleMappingTestClass(Uri uri) : base(uri) {}

        #endregion
    }

    public class SingleResourceMappingTestClass : Resource
    {
        #region Constructors

        public SingleResourceMappingTestClass(Uri uri) : base(uri) {}

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.SingleResourceMappingTestClass };
        }

        protected PropertyMapping<ObservableCollection<Resource>> resourceTestMapping =
            new PropertyMapping<ObservableCollection<Resource>>("ResourceTest", TestOntology.resourceTest, new ObservableCollection<Resource>());

        public ObservableCollection<Resource> ResourceTest
        {
            get { return GetValue(resourceTestMapping); }
            set { SetValue(resourceTestMapping, value); }
        }

        #endregion
    }

    public class MappingTestClass : Resource
    {
        #region Constructors

        public MappingTestClass(Uri uri) : base(uri) {}

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass };
        }

        protected PropertyMapping<ObservableCollection<int>> intTestMapping =
            new PropertyMapping<ObservableCollection<int>>("intTest", TestOntology.intTest, new ObservableCollection<int>());

        public ObservableCollection<int> intTest
        {
            get { return GetValue(intTestMapping); }
            set { SetValue(intTestMapping, value); }
        }

        protected PropertyMapping<int> uniqueIntTestMapping =
            new PropertyMapping<int>("uniqueIntTest", TestOntology.uniqueIntTest);

        public int uniqueIntTest
        {
            get { return GetValue(uniqueIntTestMapping); }
            set { SetValue(uniqueIntTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<uint>> uintTestMapping =
            new PropertyMapping<ObservableCollection<uint>>("uintTest", TestOntology.uintTest, new ObservableCollection<uint>());

        public ObservableCollection<uint> uintTest
        {
            get { return GetValue(uintTestMapping); }
            set { SetValue(uintTestMapping, value); }
        }


        protected PropertyMapping<uint> uniqueUintTestMapping =
            new PropertyMapping<uint>("uniqueUintTest", TestOntology.uniqueUintTest);

        public uint uniqueUintTest
        {
            get { return GetValue(uniqueUintTestMapping); }
            set { SetValue(uniqueUintTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<string>> stringTestMapping =
            new PropertyMapping<ObservableCollection<string>>("stringTest", TestOntology.stringTest, new ObservableCollection<string>());

        public ObservableCollection<string> stringTest
        {
            get { return GetValue(stringTestMapping); }
            set { SetValue(stringTestMapping, value); }
        }

        protected PropertyMapping<string> uniqueStringTestMapping =
            new PropertyMapping<string>("uniqueStringTest", TestOntology.uniqueStringTest);

        public string uniqueStringTest
        {
            get { return GetValue(uniqueStringTestMapping); }
            set { SetValue(uniqueStringTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<bool>> boolTestMapping =
            new PropertyMapping<ObservableCollection<bool>>("boolTest", TestOntology.boolTest, new ObservableCollection<bool>());

        public ObservableCollection<bool> boolTest
        {
            get { return GetValue(boolTestMapping); }
            set { SetValue(boolTestMapping, value); }
        }

        protected PropertyMapping<bool> uniqueBoolTestMapping =
            new PropertyMapping<bool>("uniqueBoolTest", TestOntology.uniqueBoolTest);

        public bool uniqueBoolTest
        {
            get { return GetValue(uniqueBoolTestMapping); }
            set { SetValue(uniqueBoolTestMapping, value); }
        }

        protected PropertyMapping<float> uniqueFloatTestMapping =
            new PropertyMapping<float>("uniqueFloatTest", TestOntology.uniqueFloatTest);

        public float uniqueFloatTest
        {
            get { return GetValue(uniqueFloatTestMapping); }
            set { SetValue(uniqueFloatTestMapping, value); }
        }

        protected PropertyMapping<double> uniqueDoubleTestMapping =
            new PropertyMapping<double>("uniqueDoubleTest", TestOntology.uniqueDoubleTest);

        public double uniqueDoubleTest
        {
            get { return GetValue(uniqueDoubleTestMapping); }
            set { SetValue(uniqueDoubleTestMapping, value); }
        }

        protected PropertyMapping<decimal> uniqueDecimalTestMapping =
            new PropertyMapping<decimal>("uniqueDecimalTest", TestOntology.uniqueDecimalTest);

        public decimal uniqueDecimalTest
        {
            get { return GetValue(uniqueDecimalTestMapping); }
            set { SetValue(uniqueDecimalTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<Resource>> _genericPropertyMapping =
            new PropertyMapping<ObservableCollection<Resource>>("genericProperty", TestOntology.genericTest);

        public ObservableCollection<Resource> genericProperty
        {
            get { return GetValue(_genericPropertyMapping); }
            set { SetValue(_genericPropertyMapping, value); }
        }

        protected PropertyMapping<DateTime> uniqueDateTimeTestMapping =
            new PropertyMapping<DateTime>("uniqueDateTimeTest", TestOntology.uniqueDatetimeTest);

        public DateTime uniqueDateTimeTest
        {
            get { return GetValue(uniqueDateTimeTestMapping); }
            set { SetValue(uniqueDateTimeTestMapping, value); }
        }

        protected PropertyMapping<ObservableCollection<DateTime>> dateTimeTestMapping =
            new PropertyMapping<ObservableCollection<DateTime>>("dateTimeTest", TestOntology.datetimeTest, new ObservableCollection<DateTime>());

        public ObservableCollection<DateTime> dateTimeTest
        {
            get { return GetValue(dateTimeTestMapping); }
            set { SetValue(dateTimeTestMapping, value); }
        }


        protected PropertyMapping<ObservableCollection<MappingTestClass2>> resourceTestMapping =
            new PropertyMapping<ObservableCollection<MappingTestClass2>>("resourceTest", TestOntology.resourceTest, new ObservableCollection<MappingTestClass2>());

        public ObservableCollection<MappingTestClass2> resourceTest
        {
            get { return GetValue(resourceTestMapping); }
            set { SetValue(resourceTestMapping, value); }
        }

        protected PropertyMapping<MappingTestClass2> uniqueResourceTestMapping =
            new PropertyMapping<MappingTestClass2>("uniqueResourceTest", TestOntology.uniqueResourceTest);

        public MappingTestClass2 uniqueResourceTest
        {
            get { return GetValue(uniqueResourceTestMapping); }
            set { SetValue(uniqueResourceTestMapping, value); }
        }

        /*
        protected MappingProperty uriProperty = new MappingProperty("UriProperty", typeof(Resource), TestOntology.uriTest);
         * 
        public Resource UriProperty
        {
            get { return (Resource)GetValue(uriProperty); }
            set { SetValue(uriProperty, value); }
        }
        */

        #endregion

    }

    public class MappingTestClass2 : Resource
    {
        #region Constructors

        public MappingTestClass2(Uri uri) : base(uri) {}

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass2 };
        }

        protected PropertyMapping<string> uniqueStringTestMapping =
            new PropertyMapping<string>("uniqueStringTest", TestOntology.uniqueStringTest);

        public string uniqueStringTest
        {
            get { return GetValue(uniqueStringTestMapping); }
            set { SetValue(uniqueStringTestMapping, value); }
        }

        #endregion
    }

    public class MappingTestClass3 : MappingTestClass2
    {
        #region Constructors

        public MappingTestClass3(Uri uri) : base(uri) {}

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass3 };
        }

        #endregion
    }

    public class MappingTestClass4 : MappingTestClass3
    {
        #region Constructors

        public MappingTestClass4(Uri uri) : base(uri) {}

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass3 };
        }

        #endregion
    }
    public class MappingTestClass5 : MappingTestClass3
    {
        #region Constructors

        public MappingTestClass5(Uri uri) : base(uri) {}

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass4 };
        }

        #endregion
    }

    public class StringMappingTestClass : Resource
    {
        #region Constructors

        public StringMappingTestClass(Uri uri) : base(uri) {}

        #endregion

        #region Mapping

        public override IEnumerable<Class> GetTypes()
        {
            return new List<Class> { TestOntology.TestClass3 };
        }

        public PropertyMapping<string> randomPropertyTestMapping =
            new PropertyMapping<string>("RandomProperty", "http://www.example.com/property");

        public string RandomProperty
        {
            get { return GetValue(randomPropertyTestMapping); }
            set { SetValue(randomPropertyTestMapping, value); }
        }

        public PropertyMapping<string> uniqueStringTestMapping =
            new PropertyMapping<string>("uniqueStringTest", TestOntology.uniqueStringTest.Uri.OriginalString);

        public string uniqueStringTest
        {
            get { return GetValue(uniqueStringTestMapping); }
            set { SetValue(uniqueStringTestMapping, value); }
        }

        public PropertyMapping<ObservableCollection<int>> intTestMapping =
            new PropertyMapping<ObservableCollection<int>>("intTest", "semio:test:intTest", new ObservableCollection<int>());

        public ObservableCollection<int> intTest
        {
            get { return GetValue(intTestMapping); }
            set { SetValue(intTestMapping, value); }
        }
        
        #endregion
    }

    [TestFixture]
    public class ResourceMappingTest
    {
        public static bool RegisteredOntology = false;

        private IStore _store;

        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            if (_store != null)
            {
                _store.Dispose();
            }
        }

        //[Test]
        // This test does not run, but it needs to.
        public void AddUnmappedType()
        {
            IModel m = GetModel();
            m.Clear();

            Uri t1Uri = new Uri("semio:test:testInstance1");
            Uri t2Uri = new Uri("semio:test:testInstance2");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);

            IResource r = m.CreateResource(t2Uri);
            r.AddProperty(rdf.type, TestOntology.TestClass2);

            t1.AddProperty(TestOntology.uniqueResourceTest, r);
            t1.AddProperty(TestOntology.resourceTest, r);

            Assert.IsNull(t1.uniqueResourceTest);
            Assert.AreEqual(0, t1.resourceTest.Count);

            m.Clear();
        }

        [Test]
        public void GetTypesTest()
        {
            MappingTestClass2 t2 = new MappingTestClass2(new Uri("semio:t2"));
            List<Class> classes = t2.GetTypes().ToList();
            Assert.AreEqual(1, classes.Count);
            Assert.Contains(TestOntology.TestClass2, classes);

            MappingTestClass3 t3 = new MappingTestClass3(new Uri("semio:t3"));
            classes = t3.GetTypes().ToList();
            Assert.AreEqual(1, classes.Count);
            Assert.Contains(TestOntology.TestClass3, classes);
        }

        [Test]
        public void AddRemoveIntegerTest()
        {
            IModel m = GetModel();
            m.Clear();

            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);

            // Add value using the mapping interface
            int value = 1;
            t1.uniqueIntTest = value;

            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(value, t_actual.uniqueIntTest);


            // Test if property is present
            IEnumerable<Property> l = t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.uniqueIntTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(int), t_actual.ListValues(TestOntology.uniqueIntTest).First().GetType());
            Assert.AreEqual(value, t_actual.ListValues(TestOntology.uniqueIntTest).First());

            // Remove with RemoveProperty
            t1.RemoveProperty(TestOntology.uniqueIntTest, value);
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.uniqueIntTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.uniqueIntTest).Count());

            m.Clear();
        }

        [Test]
        public void AddRemoveIntegerListTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);
            // Add value using the mapping interface
            int value = 2;
            t1.intTest.Add(value);

            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(1, t_actual.intTest.Count());
            Assert.AreEqual(value, t_actual.intTest[0]);

            // Test if property is present
            IEnumerable<Property> l = t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.intTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(int), t_actual.ListValues(TestOntology.intTest).First().GetType());
            Assert.AreEqual(value, t_actual.ListValues(TestOntology.intTest).First());

            // Add another value
            int value2 = -18583;
            t1.intTest.Add(value2);
            t1.Commit();
            t_actual = m.GetResource<MappingTestClass>(t1Uri);


            // Test if value was stored
            Assert.AreEqual(2, t_actual.intTest.Count());
            Assert.IsTrue(t_actual.intTest.Contains(value));
            Assert.IsTrue(t_actual.intTest.Contains(value2));

            // Test if property is present
            l = t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.intTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            var res = t_actual.ListValues(TestOntology.intTest).ToList();
            Assert.AreEqual(typeof(int), res[0].GetType());
            Assert.AreEqual(typeof(int), res[1].GetType());
            Assert.IsTrue(res.Contains(value));
            Assert.IsTrue(res.Contains(value2));

            // Remove value from mapped list
            t1.intTest.Remove(value2);
            t1.Commit();
            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if removed
            Assert.AreEqual(1, t_actual.intTest.Count());

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.intTest));

            // Test if first added property is still present
            Assert.AreEqual(typeof(int), t_actual.ListValues(TestOntology.intTest).First().GetType());
            Assert.AreEqual(value, t_actual.ListValues(TestOntology.intTest).First());

            t1.intTest.Remove(value);
            t1.Commit();
            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.intTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.intTest).Count());

            m.Clear();
        }

        /// <summary>
        /// This Test fails because the datatype "unsigned int" is not stored correctly in the database. 
        /// To be more specific the xsd type is missing although it is given at the insert.
        /// </summary>
        //[Test]
        public void AddRemoveUnsignedIntegerTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);

            // Add value using the mapping interface
            uint uValue = 1;
            t1.uniqueUintTest = uValue;

            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(uValue, t_actual.uniqueUintTest);


            // Test if property is present
            var l = t1.ListProperties();
            Assert.True(l.Contains(TestOntology.uniqueUintTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(uint), t_actual.ListValues(TestOntology.uniqueUintTest).First().GetType());
            Assert.AreEqual(uValue, t_actual.ListValues(TestOntology.uniqueUintTest).First());

            // Remove with RemoveProperty
            t1.RemoveProperty(TestOntology.uniqueUintTest, uValue);
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.uniqueUintTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.uniqueUintTest).Count());

            m.Clear();
        }

        //[Test]
        public void AddRemoveUnsignedIntegerListTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);

            // Add value using the mapping interface
            uint uValue = 2;
            t1.uintTest.Add(uValue);

            t1.Commit();
            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(1, t_actual.uintTest.Count());
            Assert.AreEqual(uValue, t_actual.uintTest[0]);


            // Test if property is present
            var l = t1.ListProperties();
            Assert.True(l.Contains(TestOntology.uintTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(uint), t_actual.ListValues(TestOntology.uintTest).First().GetType());
            Assert.AreEqual(uValue, t_actual.ListValues(TestOntology.uintTest).First());

            // Remove value from mapped list
            t1.uintTest.Remove(uValue);
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if removed
            Assert.AreEqual(0, t_actual.uintTest.Count());

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.uintTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.uintTest).Count());
            m.Clear();
        }

        [Test]
        public void AddRemoveStringTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);


            // Add value using the mapping interface
            string strValue = "Hallo Welt!";
            t1.uniqueStringTest = strValue;
            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(strValue, t_actual.uniqueStringTest);


            // Test if property is present
            var l = t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.uniqueStringTest));
            Assert.AreEqual(2, l.Count());

            var x = t_actual.HasProperty(TestOntology.uniqueStringTest);
            Assert.IsTrue(x);

            x = t_actual.HasProperty(TestOntology.uniqueStringTest, strValue);
            Assert.IsTrue(x);

            // Test if ListValues works
            Assert.AreEqual(typeof(string), t_actual.ListValues(TestOntology.uniqueStringTest).First().GetType());
            Assert.AreEqual(strValue, t1.ListValues(TestOntology.uniqueStringTest).First());

            // Remove with RemoveProperty
            t1.RemoveProperty(TestOntology.uniqueStringTest, strValue);
            t1.Commit();
            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.uniqueStringTest));

            x = t_actual.HasProperty(TestOntology.uniqueStringTest);
            Assert.IsFalse(x);

            x = t_actual.HasProperty(TestOntology.uniqueStringTest, strValue);
            Assert.IsFalse(x);

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.uniqueStringTest).Count());
            m.Clear();
        }

        [Test]
        public void AddRemoveStringListTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);

            // Add value using the mapping interface
            string strValue = "（╯°□°）╯︵ ┻━┻";
            t1.stringTest.Add(strValue);

            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(1, t_actual.stringTest.Count());
            Assert.AreEqual(strValue, t_actual.stringTest[0]);


            // Test if property is present
            var l = t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.stringTest));
            Assert.AreEqual(2, l.Count());

            var x = t_actual.HasProperty(TestOntology.stringTest);
            Assert.IsTrue(x);

            x = t_actual.HasProperty(TestOntology.stringTest, strValue);
            Assert.IsTrue(x);

            // Test if ListValues works
            Assert.AreEqual(typeof(string), t_actual.ListValues(TestOntology.stringTest).First().GetType());
            Assert.AreEqual(strValue, t_actual.ListValues(TestOntology.stringTest).First());


            // Remove value from mapped list
            t1.stringTest.Remove(strValue);
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if removed
            Assert.AreEqual(0, t_actual.boolTest.Count());

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.stringTest));

            x = t_actual.HasProperty(TestOntology.stringTest);
            Assert.IsFalse(x);

            x = t_actual.HasProperty(TestOntology.stringTest, strValue);
            Assert.IsFalse(x);

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.stringTest).Count());

            m.Clear();
        }

        [Test]
        public void AddRemoveBoolTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);


            // Add value using the mapping interface
            bool bValue = true;
            t1.uniqueBoolTest = bValue;

            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(bValue, t_actual.uniqueBoolTest);


            // Test if property is present
            var l = t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.uniqueBoolTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(bool), t_actual.ListValues(TestOntology.uniqueBoolTest).First().GetType());
            Assert.AreEqual(bValue, t_actual.ListValues(TestOntology.uniqueBoolTest).First());

            // Remove with RemoveProperty
            t1.RemoveProperty(TestOntology.uniqueBoolTest, bValue);
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.uniqueBoolTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.uniqueBoolTest).Count());

            m.Clear();
        }

        [Test]
        public void AddRemoveBoolListTest()
        {
            IModel model = GetModel();
            model.Clear();

            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = model.CreateResource<MappingTestClass>(t1Uri);

            // Add value using the mapping interface
            bool value = true;
            t1.boolTest.Add(value);
            t1.Commit();

            MappingTestClass t_actual = model.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(1, t_actual.boolTest.Count());
            Assert.AreEqual(value, t_actual.boolTest[0]);

            // Test if property is present
            var l = t_actual.ListProperties();

            Assert.True(l.Contains(TestOntology.boolTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(bool), t_actual.ListValues(TestOntology.boolTest).First().GetType());
            Assert.AreEqual(value, t_actual.ListValues(TestOntology.boolTest).First());

            // Remove value from mapped list
            t1.boolTest.Remove(value);
            t1.Commit();

            t_actual = model.GetResource<MappingTestClass>(t1Uri);

            // Test if removed
            Assert.AreEqual(0, t_actual.boolTest.Count());

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();

            Assert.False(l.Contains(TestOntology.boolTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.boolTest).Count());

            model.Clear();
        }

        [Test]
        public void AddRemoveFloatTest()
        {
            IModel m = GetModel();
            m.Clear();

            Uri uri = new Uri("semio:test:testInstance1");

            MappingTestClass testResource = m.CreateResource<MappingTestClass>(uri);

            // Add value using the mapping interface
            float floatValue = 1.0f;

            testResource.uniqueFloatTest = floatValue;
            testResource.Commit();

            MappingTestClass storedResource = m.GetResource<MappingTestClass>(uri);

            // Test if value was stored
            Assert.AreEqual(floatValue, storedResource.uniqueFloatTest);

            // Test if property is present
            List<Property> properties = storedResource.ListProperties();

            Assert.True(properties.Contains(TestOntology.uniqueFloatTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(float), storedResource.ListValues(TestOntology.uniqueFloatTest).First().GetType());
            Assert.AreEqual(floatValue, storedResource.ListValues(TestOntology.uniqueFloatTest).First());

            // Remove with RemoveProperty
            testResource.RemoveProperty(TestOntology.uniqueFloatTest, floatValue);
            testResource.Commit();

            storedResource = m.GetResource<MappingTestClass>(uri);

            // Test if ListProperties works
            properties = storedResource.ListProperties();

            Assert.False(properties.Contains(TestOntology.uniqueFloatTest));

            // Test if ListValues works
            Assert.AreEqual(0, storedResource.ListValues(TestOntology.uniqueFloatTest).Count());

            m.Clear();
        }

        [Test]
        public void AddRemoveDoubleTest()
        {
            IModel m = GetModel();
            m.Clear();

            Uri uri = new Uri("semio:test:testInstance1");

            MappingTestClass testResource = m.CreateResource<MappingTestClass>(uri);

            // Add value using the mapping interface
            double doubleValue = 1.0;

            testResource.uniqueDoubleTest = doubleValue;
            testResource.Commit();

            MappingTestClass storedResource = m.GetResource<MappingTestClass>(uri);

            // Test if value was stored
            Assert.AreEqual(doubleValue, storedResource.uniqueDoubleTest);

            // Test if property is present
            List<Property> properties = storedResource.ListProperties();

            Assert.True(properties.Contains(TestOntology.uniqueDoubleTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(double), storedResource.ListValues(TestOntology.uniqueDoubleTest).First().GetType());
            Assert.AreEqual(doubleValue, storedResource.ListValues(TestOntology.uniqueDoubleTest).First());

            // Remove with RemoveProperty
            testResource.RemoveProperty(TestOntology.uniqueDoubleTest, doubleValue);
            testResource.Commit();

            storedResource = m.GetResource<MappingTestClass>(uri);

            // Test if ListProperties works
            properties = storedResource.ListProperties();

            Assert.False(properties.Contains(TestOntology.uniqueDoubleTest));

            // Test if ListValues works
            Assert.AreEqual(0, storedResource.ListValues(TestOntology.uniqueDoubleTest).Count());

            m.Clear();
        }

        [Test]
        public void AddRemoveDecimalTest()
        {
            IModel m = GetModel();
            m.Clear();

            Uri uri = new Uri("semio:test:testInstance1");

            MappingTestClass testResource = m.CreateResource<MappingTestClass>(uri);

            // Add value using the mapping interface
            decimal decimalValue = 1.0m;

            testResource.uniqueDecimalTest = decimalValue;
            testResource.Commit();

            MappingTestClass storedResource = m.GetResource<MappingTestClass>(uri);

            // Test if value was stored
            Assert.AreEqual(decimalValue, storedResource.uniqueDecimalTest);

            // Test if property is present
            List<Property> properties = storedResource.ListProperties();

            Assert.True(properties.Contains(TestOntology.uniqueDecimalTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(decimal), storedResource.ListValues(TestOntology.uniqueDecimalTest).First().GetType());
            Assert.AreEqual(decimalValue, storedResource.ListValues(TestOntology.uniqueDecimalTest).First());

            // Remove with RemoveProperty
            testResource.RemoveProperty(TestOntology.uniqueDecimalTest, decimalValue);
            testResource.Commit();

            storedResource = m.GetResource<MappingTestClass>(uri);

            // Test if ListProperties works
            properties = storedResource.ListProperties();

            Assert.False(properties.Contains(TestOntology.uniqueDecimalTest));

            // Test if ListValues works
            Assert.AreEqual(0, storedResource.ListValues(TestOntology.uniqueDecimalTest).Count());

            m.Clear();
        }

        /// <summary>
        /// Note: 
        /// Datetime precision in Virtuoso is not as high as native .net datetime precision.
        /// </summary>
        [Test]
        public void AddRemoveDateTimeTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);


            // Add value using the mapping interface
            DateTime Value = new DateTime(2012, 8, 15, 12, 3, 55, DateTimeKind.Local);
            t1.uniqueDateTimeTest = Value;
            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(Value.ToUniversalTime(), t_actual.uniqueDateTimeTest.ToUniversalTime());


            // Test if property is present
            var l = t_actual.ListProperties();
            Assert.True(l.Contains(TestOntology.uniqueDatetimeTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(DateTime), t_actual.ListValues(TestOntology.uniqueDatetimeTest).First().GetType());
            DateTime time = (DateTime)t_actual.ListValues(TestOntology.uniqueDatetimeTest).First();
            Assert.AreEqual(Value.ToUniversalTime(), time.ToUniversalTime());

            // Remove with RemoveProperty
            t1.RemoveProperty(TestOntology.uniqueDatetimeTest, Value);
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.uniqueBoolTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.uniqueDatetimeTest).Count());


            DateTime t = new DateTime();
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out t));

            t1.uniqueDateTimeTest = t;
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);
            Assert.AreEqual(t1.uniqueDateTimeTest, t_actual.uniqueDateTimeTest.ToLocalTime());

            m.Clear();
        }

        [Test]
        public void TimeZoneTest()
        {
            IModel m = GetModel();
            m.Clear();

            Uri t1Uri = new Uri("semio:test:testInstance1");
            DateTime t = new DateTime();
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out t));

            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);
            t1.uniqueDateTimeTest = t;
            t1.Commit();

            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);
        }

        [Test]
        public void AddRemoveDateTimeListTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);


            // Add value using the mapping interface
            DateTime value = new DateTime(2012, 8, 15, 12, 3, 55, DateTimeKind.Local);
            t1.dateTimeTest.Add(value);
            t1.Commit();
            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if value was stored
            Assert.AreEqual(1, t1.dateTimeTest.Count());
            Assert.AreEqual(value, t1.dateTimeTest[0]);


            // Test if property is present
            var l = t1.ListProperties();
            Assert.True(l.Contains(TestOntology.datetimeTest));
            Assert.AreEqual(2, l.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(DateTime), t_actual.ListValues(TestOntology.datetimeTest).First().GetType());
            DateTime time = (DateTime)t_actual.ListValues(TestOntology.datetimeTest).First();
            Assert.AreEqual(value.ToUniversalTime(), time.ToUniversalTime());

            // Remove value from mapped list
            t1.dateTimeTest.Remove(value);
            t1.Commit();

            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            // Test if removed
            Assert.AreEqual(0, t_actual.dateTimeTest.Count());

            // Test if ListProperties works
            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.datetimeTest));

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.datetimeTest).Count());
        }

        [Test]
        public void AddRemoveResourceTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);

            Uri testClass2Uri = new Uri("semio:test:testInstance2");
            MappingTestClass2 t2 = new MappingTestClass2(testClass2Uri);

            t1.uniqueResourceTest = t2;
            t1.Commit();
            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);


            Assert.AreEqual(t2, t_actual.uniqueResourceTest);

            var l = t_actual.ListProperties();
            Assert.Contains(TestOntology.uniqueResourceTest, l);
            Assert.AreEqual(2, l.Count());

            var x = t_actual.HasProperty(TestOntology.uniqueResourceTest);
            Assert.IsTrue(x);

            x = t_actual.HasProperty(TestOntology.uniqueResourceTest, t2);
            Assert.IsTrue(x);

            t_actual = m.GetResource<MappingTestClass>(t1Uri);
            var values = t_actual.ListValues().ToList();
            Assert.Contains( new Tuple<Property, object>(TestOntology.uniqueResourceTest, t2), values);
            

            Assert.IsTrue(typeof(Resource).IsAssignableFrom(t_actual.ListValues(TestOntology.uniqueResourceTest).First().GetType()));
            //Assert.AreEqual(t2, t_actual.ListValues(TestOntology.uniqeResourceTest).First());

            t1.RemoveProperty(TestOntology.uniqueResourceTest, t2);
            t1.Commit();
            t_actual = m.GetResource<MappingTestClass>(t1Uri);


            l = (List<Property>)t_actual.ListProperties();
            Assert.False(l.Contains(TestOntology.uniqueResourceTest));

            x = t_actual.HasProperty(TestOntology.uniqueResourceTest);
            Assert.IsFalse(x);

            x = t_actual.HasProperty(TestOntology.uniqueResourceTest, t2);
            Assert.IsFalse(x);

            // Test if ListValues works
            Assert.AreEqual(0, t_actual.ListValues(TestOntology.uniqueResourceTest).Count());
        }

        [Test]
        public void AddRemoveResourceListTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);


            // Add value using the mapping interface
            MappingTestClass2 t2 = new MappingTestClass2(new Uri("semio:test:testInstance2"));
            t1.resourceTest.Add(t2);
            t1.Commit();
            MappingTestClass t_actual = m.GetResource<MappingTestClass>(t1Uri);

            Assert.AreEqual(1, t_actual.resourceTest.Count);
            Assert.AreEqual(t2, t_actual.resourceTest[0]);

            var l = t_actual.ListProperties();
            Assert.AreEqual(2, l.Count());
            Assert.IsTrue(l.Contains(TestOntology.resourceTest));

            var x = t_actual.HasProperty(TestOntology.resourceTest);
            Assert.IsTrue(x);

            x = t_actual.HasProperty(TestOntology.resourceTest, t2);
            Assert.IsTrue(x);

            var v = t_actual.ListValues(TestOntology.resourceTest);
            Assert.AreEqual(2, l.Count());
            Assert.IsTrue(v.Contains(t2));

            Assert.AreEqual(t2.GetType(), v.First().GetType());

            t1.resourceTest.Remove(t2);
            t1.Commit();
            t_actual = m.GetResource<MappingTestClass>(t1Uri);

            x = t_actual.HasProperty(TestOntology.resourceTest);
            Assert.IsFalse(x);

            x = t_actual.HasProperty(TestOntology.resourceTest, t2);
            Assert.IsFalse(x);

            Assert.AreEqual(0, t_actual.resourceTest.Count);
        }

        [Test]
        public void LazyLoadResourceTest()
        {
            
            IModel model = GetModel();
            model.Clear();

            Uri testRes1 = new Uri("semio:test:testInstance");
            Uri testRes2 = new Uri("semio:test:testInstance2");
            MappingTestClass t1 = model.CreateResource<MappingTestClass>(testRes1);
            MappingTestClass2 t2 = model.CreateResource<MappingTestClass2>(new Uri("semio:test:testInstance2"));

            t1.uniqueResourceTest = t2;
            // TODO: Debug messsage, because t2 was not commited
            t1.Commit();

            MappingTestClass p1 = model.GetResource<MappingTestClass>(testRes1);
            //Assert.AreEqual(null, p1.uniqueResourceTest);

            var v = p1.ListValues(TestOntology.uniqueResourceTest);
            Assert.AreEqual(t2.Uri.OriginalString, (v.First() as IResource).Uri.OriginalString);

            model.DeleteResource(t1);

            model.DeleteResource(t2);

            t1 = model.CreateResource<MappingTestClass>(testRes1);

            t2 = model.CreateResource<MappingTestClass2>(new Uri("semio:test:testInstance2"));
            t2.Commit();

            t1.uniqueResourceTest = t2;
            t1.Commit();

            var tt1 = model.GetResource<MappingTestClass>(testRes1);
            Assert.AreEqual(t2, tt1.uniqueResourceTest);

            IResource tr1 = model.GetResource(testRes1);
            Assert.AreEqual(typeof(MappingTestClass), tr1.GetType());
            
            model.Clear();
            _store.RemoveModel(model);
        }

        [Test]
        public void MappingTypeTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass2 t1 = m.CreateResource<MappingTestClass2>(t1Uri);
            //Assert.AreEqual(1, t1.Classes.Count);
            t1.uniqueStringTest = "testing 1";
            t1.Commit();

            Uri t2Uri = new Uri("semio:test:testInstance2");
            MappingTestClass3 t2 = m.CreateResource<MappingTestClass3>(t2Uri);
            t2.uniqueStringTest = "testing 2";
            t2.Commit();

            Uri t3Uri = new Uri("semio:test:testInstance3");
            MappingTestClass4 t3 = m.CreateResource<MappingTestClass4>(t3Uri);
            t3.uniqueStringTest = "testing 3";
            t3.Commit();

            Resource r1 = m.GetResource<Resource>(t1Uri);
            Assert.AreEqual(t1, r1);

            Resource r2 = m.GetResource<Resource>(t2Uri);
            Assert.AreEqual(t2, r2);

            Resource r3 = m.GetResource<Resource>(t3Uri);
            Assert.AreEqual(t3, r3);

        }

        [Test]
        public void MultipeTypesMappingTest()
        {
            IModel m = GetModel();
            m.Clear();

            Uri t3Uri = new Uri("semio:test:testInstance3");
            MappingTestClass5 t3 = m.CreateResource<MappingTestClass5>(t3Uri);
            t3.uniqueStringTest = "testing 3";
            t3.AddProperty(rdf.type, nco.Affiliation);
            t3.Commit();

            Resource r3 = m.GetResource<Resource>(t3Uri);
            Assert.AreEqual(typeof(MappingTestClass5), r3.GetType());

            m.Clear();
            t3 = m.CreateResource<MappingTestClass5>(t3Uri);
            t3.uniqueStringTest = "testing 3";
            t3.AddProperty(rdf.type, nco.Contact);
            t3.Commit();

            r3 = m.GetResource<MappingTestClass5>(t3Uri);
            Assert.AreEqual(typeof(MappingTestClass5), r3.GetType());

            r3 = m.GetResource<Contact>(t3Uri);
            Assert.AreEqual(typeof(Contact), r3.GetType());
        }

        [Test]
        public void MappingTypeWithInferencingTest()
        {
            IModel m = GetModel();
            m.Clear();

            Uri t3Uri = new Uri("semio:test:testInstance3");
            PersonContact t3 = m.CreateResource<PersonContact>(t3Uri);
            t3.NameGiven = "Hans";
            t3.Commit();

            ResourceQuery q = new ResourceQuery(nco.Contact);

            var queryResult = m.ExecuteQuery(q, true);

            Assert.IsNotEmpty(queryResult.GetResources());
        }

        IModel GetModel()
        {
            string connectionString = SetupClass.ConnectionString;
            _store = StoreFactory.CreateStore(string.Format("{0};rule=urn:semiodesk/test/ruleset", connectionString));

            Uri testModelUri = new Uri("http://example.org/TestModel");

            IModel model;

            if (_store.ContainsModel(testModelUri))
            {
                model = _store.GetModel(testModelUri);
            }
            else
            {
                model = _store.CreateModel(testModelUri);
            }

            return model;
        }

        [Test]
        public void RollbackTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);


            // Add value using the mapping interface
            string strValue = "Hallo Welt!";
            t1.uniqueStringTest = strValue;
            t1.Commit();

            t1.uniqueStringTest = "HelloWorld!";

            t1.Rollback();


            Assert.AreEqual(strValue, t1.uniqueStringTest);

            MappingTestClass newRef = m.GetResource<MappingTestClass>(t1Uri);
            newRef.stringTest.Add("Hi");
            newRef.stringTest.Add("Blub");
            newRef.Commit();

            t1.Rollback();


            Assert.AreEqual(2, t1.stringTest.Count);
            Assert.IsTrue(t1.stringTest.Contains("Hi"));
            Assert.IsTrue(t1.stringTest.Contains("Blub"));


            Uri t2Uri = new Uri("semio:test:testInstance2");
            MappingTestClass2 p = m.CreateResource<MappingTestClass2>(t2Uri);
            p.uniqueStringTest = "blub";
            p.Commit();

            newRef = m.GetResource<MappingTestClass>(t1Uri);
            newRef.resourceTest.Add(p);
            newRef.Commit();

            t1.Rollback();


            Assert.IsTrue(t1.resourceTest.Count == 1);
            Assert.IsTrue(t1.resourceTest.Contains(p));
        }

        [Test]
        public void RollbackMappedResourcesTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            SingleResourceMappingTestClass t1 = m.CreateResource<SingleResourceMappingTestClass>(t1Uri);
            t1.Commit();

            Uri t2Uri = new Uri("semio:test:testInstance2");
            SingleMappingTestClass p = m.CreateResource<SingleMappingTestClass>(t2Uri);
            p.stringTest.Add("blub");
            p.Commit();

            var newRef = m.GetResource<SingleResourceMappingTestClass>(t1Uri);
            newRef.ResourceTest.Add(p);
            newRef.Commit();

            t1.Rollback();

            Assert.IsTrue(t1.ResourceTest.Count == 1);
            Assert.IsTrue(t1.ResourceTest.Contains(p));
        }

        [Test]
        public void ListValuesTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance1");
            MappingTestClass t1 = m.CreateResource<MappingTestClass>(t1Uri);


            // Add value using the mapping interface
            string strValue = "Hallo Welt!";
            t1.uniqueStringTest = strValue;
            t1.Commit();

            t1.stringTest.Add("Hi");
            t1.stringTest.Add("Blub");
            t1.Commit();

            var x = t1.ListValues(TestOntology.stringTest);

            MappingTestClass actual = m.GetResource<MappingTestClass>(t1.Uri);
            var x2 = actual.ListValues(TestOntology.stringTest);

            Assert.AreEqual(x.Count, x2.Count);
            Assert.IsTrue(x2.Contains(x[0]));
            Assert.IsTrue(x2.Contains(x[1]));
        }

        [Test]
        public void KeepListsAfterRollbackTest()
        {
            IModel m = GetModel();
            m.Clear();
            Uri t1Uri = new Uri("semio:test:testInstance8");
            SingleMappingTestClass t1 = m.CreateResource<SingleMappingTestClass>(t1Uri);
            t1.AddProperty(TestOntology.uniqueStringTest, "Hello");
            t1.Commit();
            t1.Rollback();

            t1.stringTest.Add("Hi");
            t1.stringTest.Add("Blub");
            var x = t1.ListValues(TestOntology.stringTest);
            Assert.AreEqual(2, x.Count);
            t1.Commit();

            SingleMappingTestClass t2 = m.GetResource<SingleMappingTestClass>(t1Uri);

            var x2 = t2.ListValues(TestOntology.stringTest);

            Assert.AreEqual(x.Count, x2.Count);
            Assert.IsTrue(x2.Contains(x[0]));
            Assert.IsTrue(x2.Contains(x[1]));

        }

        [Test]
        public void TestEquality()
        {
            Resource c1 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledStatus"));
            Resource c2 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledStatus"));

            Assert.IsTrue(c1.Equals(c2));
            Assert.IsFalse(c1 == c2);
        }

        [Test]
        public void TestStringPropertyMapping()
        {
            StringMappingTestClass p = new StringMappingTestClass(new Uri("http://test.example.com"));
            p.uniqueStringTest = "Test string";

            var x = p.GetValue(TestOntology.uniqueStringTest);
            Assert.AreEqual(p.uniqueStringTest, x);

            p.RandomProperty = "Test string 2";

            x = p.GetValue(new Property(new Uri("http://www.example.com/property")));
            Assert.AreEqual(p.RandomProperty, x);
        }
    }
}
