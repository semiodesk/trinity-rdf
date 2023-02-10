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
// Copyright (c) Semiodesk GmbH 2023

using NUnit.Framework;
using Newtonsoft.Json;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Serialization;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Semiodesk.Trinity.Test.GraphDB
{
    [TestFixture]
    public class ResourceMappingTest : TestBase
    {
        #region Members
        
        private UriRef _r1;

        private UriRef _r2;

        private UriRef _r3;

        private Property _p1;

        private Property _p2;
        
        #endregion
        
        #region Methods

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _r1 = BaseUri.GetUriRef("r1");
            _r2 = BaseUri.GetUriRef("r2");
            _r3 = BaseUri.GetUriRef("r3");
            
            _p1 = new Property(BaseUri.GetUriRef("p1"));
            _p2 = new Property(BaseUri.GetUriRef("p2"));
        }
        
        protected void InitializeModels()
        {
            var m1_r1 = Model1.CreateResource(_r1);
            m1_r1.AddProperty(_p1, "in the jungle");
            m1_r1.AddProperty(_p1, 123);
            m1_r1.AddProperty(_p1, DateTime.Now);
            m1_r1.Commit();

            var m1_r2 = Model1.CreateResource(_r2);
            m1_r2.AddProperty(_p1, "in the jungle");
            m1_r2.AddProperty(_p1, 123);
            m1_r2.AddProperty(_p1, DateTime.Now);
            m1_r2.Commit();
            
            var m2_r1 = Model2.CreateResource(_r1);
            m2_r1.AddProperty(_p1, "in the jungle");
            m2_r1.AddProperty(_p1, 123);
            m2_r1.AddProperty(_p1, DateTime.Now);
            m2_r1.Commit();

            var m2_r2 = Model2.CreateResource(_r2);
            m2_r2.AddProperty(_p1, "in the jungle");
            m2_r2.AddProperty(_p1, 123);
            m2_r2.AddProperty(_p1, DateTime.Now);
            m2_r2.Commit();
        }
        
        //[Test]
        // This test does not run, but it needs to.
        public void AddUnmappedType()
        {
            var r2 = Model1.CreateResource(_r2);
            r2.AddProperty(rdf.type, TestOntology.TestClass2);
            
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.AddProperty(TestOntology.uniqueResourceTest, r2);
            r1.AddProperty(TestOntology.resourceTest, r2);

            Assert.IsNull(r1.uniqueResourceTest);
            Assert.AreEqual(0, r1.resourceTest.Count);
        }

        [Test]
        public void GetTypesTest()
        {
            var r1 = new MappingTestClass2(_r1);
            var types1 = r1.GetTypes().ToList();
            
            Assert.AreEqual(1, types1.Count);
            Assert.Contains(TestOntology.TestClass2, types1);

            var r2 = new MappingTestClass3(_r2);
            var types2 = r2.GetTypes().ToList();
            
            Assert.AreEqual(1, types2.Count);
            Assert.Contains(TestOntology.TestClass3, types2);
        }

        [Test]
        public void AddRemoveIntegerTest()
        {
            // Add value using the mapping interface
            int value = 1;
            
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueIntTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);

            // Test if value was stored
            Assert.AreEqual(value, actual.uniqueIntTest);

            // Test if property is present
            var properties = actual.ListProperties();
            
            Assert.True(properties.Contains(TestOntology.uniqueIntTest));
            Assert.AreEqual(2, properties.Count()); // rdf:type, to:uniqueIntTest

            // Test if ListValues works
            Assert.AreEqual(typeof(int), actual.ListValues(TestOntology.uniqueIntTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.uniqueIntTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueIntTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(r1);

            // Test if ListProperties works
            properties = actual.ListProperties();
            
            Assert.False(properties.Contains(TestOntology.uniqueIntTest));
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueIntTest).Count());
        }

        [Test]
        public void AddRemoveIntegerListTest()
        {
            var value1 = 2;
            var value2 = -18583;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.intTest.Add(value1);
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties();

            // Test if value was stored
            Assert.AreEqual(1, actual.intTest.Count());
            Assert.AreEqual(value1, actual.intTest.First());

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.intTest));
            Assert.AreEqual(2, properties.Count()); // rdf:type, to:intTest

            // Test if ListValues works
            Assert.AreEqual(typeof(int), actual.ListValues(TestOntology.intTest).First().GetType());
            Assert.AreEqual(value1, actual.ListValues(TestOntology.intTest).First());

            // Add another value
            r1.intTest.Add(value2);
            r1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties();
            
            // Test if value was stored
            Assert.AreEqual(2, actual.intTest.Count());
            Assert.IsTrue(actual.intTest.Contains(value1));
            Assert.IsTrue(actual.intTest.Contains(value2));

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.intTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var values = actual.ListValues(TestOntology.intTest).ToList();
            
            Assert.AreEqual(typeof(int), values[0].GetType());
            Assert.AreEqual(typeof(int), values[1].GetType());
            Assert.IsTrue(values.Contains(value1));
            Assert.IsTrue(values.Contains(value2));

            // Remove value from mapped list
            r1.intTest.Remove(value2);
            r1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if removed
            Assert.AreEqual(1, actual.intTest.Count());
            Assert.True(properties.Contains(TestOntology.intTest));

            // Test if first added property is still present
            Assert.AreEqual(typeof(int), actual.ListValues(TestOntology.intTest).First().GetType());
            Assert.AreEqual(value1, actual.ListValues(TestOntology.intTest).First());

            r1.intTest.Remove(value1);
            r1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties();
            
            // Test if ListValues works
            Assert.False(properties.Contains(TestOntology.intTest));
            Assert.AreEqual(0, actual.ListValues(TestOntology.intTest).Count());
        }

        /// <summary>
        /// This Test fails because the datatype "unsigned int" is not stored correctly in the database. 
        /// To be more specific the xsd type is missing although it is given at the insert.
        /// </summary>
        //[Test]
        public void AddRemoveUnsignedIntegerTest()
        {
            uint value = 1;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueUintTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = r1.ListProperties();

            // Test if value was stored
            Assert.AreEqual(value, actual.uniqueUintTest);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueUintTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(uint), actual.ListValues(TestOntology.uniqueUintTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.uniqueUintTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueUintTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = (List<Property>)actual.ListProperties();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueUintTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueUintTest).Count());
        }

        //[Test]
        public void AddRemoveUnsignedIntegerListTest()
        {
            uint value = 2;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uintTest.Add(value);
            r1.Commit();
            
            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = r1.ListProperties();

            // Test if value was stored
            Assert.AreEqual(1, actual.uintTest.Count());
            Assert.AreEqual(value, actual.uintTest[0]);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uintTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(uint), actual.ListValues(TestOntology.uintTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.uintTest).First());

            // Remove value from mapped list
            r1.uintTest.Remove(value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = (List<Property>)actual.ListProperties();

            // Test if removed
            Assert.AreEqual(0, actual.uintTest.Count());

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uintTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uintTest).Count());
        }

        [Test]
        public void AddRemoveStringTest()
        {
            var value = "Hallo Welt!";
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueStringTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties();

            // Test if value was stored
            Assert.AreEqual(value, actual.uniqueStringTest);
            
            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueStringTest));
            Assert.AreEqual(2, properties.Count());
            Assert.IsTrue(actual.HasProperty(TestOntology.uniqueStringTest));
            Assert.IsTrue(actual.HasProperty(TestOntology.uniqueStringTest, value));

            // Test if ListValues works
            Assert.AreEqual(typeof(string), actual.ListValues(TestOntology.uniqueStringTest).First().GetType());
            Assert.AreEqual(value, r1.ListValues(TestOntology.uniqueStringTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueStringTest, value);
            r1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueStringTest));
            Assert.IsFalse(actual.HasProperty(TestOntology.uniqueStringTest));
            Assert.IsFalse(actual.HasProperty(TestOntology.uniqueStringTest, value));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueStringTest).Count());
            
            // Test if escaping works
            r1.uniqueStringTest = "ASK { < http://steadymojo.com/sleepState> <http://www.close-game.com/ontologies/2015/ia/hasBoolValue> ?o. Filter( ?o != 'false'^^<http://www.w3.org/2001/XMLSchema#boolean>) }";
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r1.uniqueStringTest, actual.uniqueStringTest);
        }

        [Test]
        public void AddRemoveStringListTest()
        {
            var value = "（╯°□°）╯︵ ┻━┻";
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.stringTest.Add(value);
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties();

            // Test if value was stored
            Assert.AreEqual(1, actual.stringTest.Count());
            Assert.AreEqual(value, actual.stringTest[0]);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.stringTest));
            Assert.AreEqual(2, properties.Count());
            Assert.IsTrue(actual.HasProperty(TestOntology.stringTest));
            Assert.IsTrue(actual.HasProperty(TestOntology.stringTest, value));

            // Test if ListValues works
            Assert.AreEqual(typeof(string), actual.ListValues(TestOntology.stringTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.stringTest).First());

            // Remove value from mapped list
            r1.stringTest.Remove(value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties();

            // Test if removed
            Assert.AreEqual(0, actual.boolTest.Count());

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.stringTest));
            Assert.IsFalse(actual.HasProperty(TestOntology.stringTest));
            Assert.IsFalse(actual.HasProperty(TestOntology.stringTest, value));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.stringTest).Count());
        }

        [Test]
        public void AddRemoveBoolTest()
        {
            var value = true;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueBoolTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties();

            // Test if value was stored
            Assert.AreEqual(value, actual.uniqueBoolTest);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueBoolTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(bool), actual.ListValues(TestOntology.uniqueBoolTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.uniqueBoolTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueBoolTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueBoolTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueBoolTest).Count());
        }

        [Test]
        public void AddRemoveBoolListTest()
        {
            var value = true;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.boolTest.Add(value);
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties();

            // Test if value was stored
            Assert.AreEqual(1, actual.boolTest.Count());
            Assert.AreEqual(value, actual.boolTest[0]);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.boolTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(bool), actual.ListValues(TestOntology.boolTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.boolTest).First());

            // Remove value from mapped list
            r1.boolTest.Remove(value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if removed
            Assert.AreEqual(0, actual.boolTest.Count());

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.boolTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.boolTest).Count());
        }

        [Test]
        public void AddRemoveFloatTest()
        {
            var value = 1.0f;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueFloatTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties().ToList();

            // Test if value was stored
            Assert.AreEqual(value, actual.uniqueFloatTest);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueFloatTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(float), actual.ListValues(TestOntology.uniqueFloatTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.uniqueFloatTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueFloatTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueFloatTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueFloatTest).Count());
        }

        [Test]
        public void AddRemoveDoubleTest()
        {
            var value = 1.0;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueDoubleTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties().ToList();

            // Test if value was stored
            Assert.AreEqual(value, actual.uniqueDoubleTest);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueDoubleTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(double), actual.ListValues(TestOntology.uniqueDoubleTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.uniqueDoubleTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueDoubleTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueDoubleTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueDoubleTest).Count());

            r1.DoubleTest.Add(1);
            r1.DoubleTest.Add(3);
            r1.DoubleTest.Add(6);
            r1.DoubleTest.Add(17);
            r1.DoubleTest.Add(19.111);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(5, actual.DoubleTest.Count);
        }

        [Test]
        public void AddRemoveDecimalTest()
        {
            var value = 1.0m;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueDecimalTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties().ToList();

            // Test if value was stored
            Assert.AreEqual(value, actual.uniqueDecimalTest);

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueDecimalTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(decimal), actual.ListValues(TestOntology.uniqueDecimalTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(TestOntology.uniqueDecimalTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueDecimalTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueDecimalTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueDecimalTest).Count());
        }

        /// <summary>
        /// Note: 
        /// Datetime precision in Virtuoso is not as high as native .net datetime precision.
        /// </summary>
        [Test]
        public void AddRemoveDateTimeTest()
        {
            var value = new DateTime(2012, 8, 15, 12, 3, 55, DateTimeKind.Local);
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueDateTimeTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties();

            // Test if value was stored
            Assert.AreEqual(value.ToUniversalTime(), actual.uniqueDateTimeTest.ToUniversalTime());

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueDatetimeTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var v = (DateTime)actual.ListValues(TestOntology.uniqueDatetimeTest).First();
            Assert.IsNotNull(v);
            Assert.AreEqual(value.ToUniversalTime(), v.ToUniversalTime());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueDatetimeTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueDatetimeTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueDatetimeTest).Count());
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out var t));

            r1.uniqueDateTimeTest = t;
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r1.uniqueDateTimeTest, actual.uniqueDateTimeTest.ToLocalTime());
        }

        [Test]
        public void AddRemoveUriTest()
        {
            // 1. Create a new instance of the test class and commit it to the model.
            var test1 = Model1.CreateResource<MappingTestClass>(_r1);
            test1.resProperty = new Resource(_r2);
            test1.Commit();

            // 2. Retrieve a new copy of the instance and validate the mapped URI property.
            test1 = Model1.GetResource<MappingTestClass>(_r1);

            Assert.NotNull(test1.resProperty);
            Assert.AreEqual(test1.resProperty.Uri, _r2);

            // 3. Change the property and commit the resource.
            test1.resProperty = new Resource(_r3);
            test1.Commit();

            // 4. Retrieve a new copy of the instance and validate the changed URI property.
            test1 = Model1.GetResource<MappingTestClass>(_r1);

            Assert.NotNull(test1.resProperty);
            Assert.AreEqual(test1.resProperty.Uri, _r3);
        }

        [Test]
        public void AddRemoveUriPropTest()
        {
            var value = _r2;
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueUriTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = actual.ListProperties();

            // Test if value was stored
            Assert.IsNotNull(actual.uniqueUriTest);
            Assert.AreEqual(value.ToString(), actual.uniqueUriTest.ToString());

            // Test if property is present
            Assert.True(properties.Contains(TestOntology.uniqueUriTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var v = (Uri)actual.ListValues(TestOntology.uniqueUriTest).First();
            
            Assert.IsNotNull(v);
            Assert.AreEqual(value.ToString(), v.ToString());

            // Remove with RemoveProperty
            r1.RemoveProperty(TestOntology.uniqueUriTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.uniqueUriTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueUriTest).Count());

            r1.uriTest.Add(new Uri("urn:test#myUri1"));
            r1.uriTest.Add(new Uri("urn:test#myUri2"));
            r1.uriTest.Add(new Uri("urn:test3"));
            r1.uriTest.Add(new Uri("urn:test/my#Uri4"));
            r1.uriTest.Add(new Uri("urn:test#5"));
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r1.uriTest.Count, actual.uriTest.Count);
        }
        [Test]
        public void TimeZoneTest()
        {
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out var value));

            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueDateTimeTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r1.uniqueDateTimeTest, actual.uniqueDateTimeTest);
        }

        [Test]
        public void AddRemoveDateTimeListTest()
        {
            var value = new DateTime(2012, 8, 15, 12, 3, 55, DateTimeKind.Local);

            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.dateTimeTest.Add(value);
            r1.Commit();
            
            var actual = Model1.GetResource<MappingTestClass>(_r1);
            var properties = r1.ListProperties();

            // Test if value was stored
            Assert.AreEqual(1, r1.dateTimeTest.Count());
            Assert.AreEqual(value, r1.dateTimeTest[0]);
            
            // Test if property is present
            Assert.True(properties.Contains(TestOntology.datetimeTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var v = (DateTime)actual.ListValues(TestOntology.datetimeTest).First();
            
            Assert.IsNotNull(v);
            Assert.AreEqual(value.ToUniversalTime(), v.ToUniversalTime());

            // Remove value from mapped list
            r1.dateTimeTest.Remove(value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if removed
            Assert.AreEqual(0, actual.dateTimeTest.Count());

            // Test if ListProperties works
            Assert.False(properties.Contains(TestOntology.datetimeTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.datetimeTest).Count());
        }

        [Test]
        public void AddRemoveResourceTest()
        {
            var t2 = new MappingTestClass2(_r2);

            var t1 = Model1.CreateResource<MappingTestClass>(_r1);
            t1.uniqueResourceTest = t2;
            t1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(t2, actual.uniqueResourceTest);
            
            var properties = actual.ListProperties().ToList();
            
            Assert.Contains(TestOntology.uniqueResourceTest, properties);
            Assert.AreEqual(2, properties.Count());

            Assert.IsTrue(actual.HasProperty(TestOntology.uniqueResourceTest));
            Assert.IsTrue(actual.HasProperty(TestOntology.uniqueResourceTest, t2));

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            var values = actual.ListValues().ToList();
            
            Assert.Contains( new Tuple<Property, object>(TestOntology.uniqueResourceTest, t2), values);
            
            Assert.IsTrue(typeof(Resource).IsAssignableFrom(actual.ListValues(TestOntology.uniqueResourceTest).First().GetType()));
            //Assert.AreEqual(t2, t_actual.ListValues(TestOntology.uniqeResourceTest).First());

            t1.RemoveProperty(TestOntology.uniqueResourceTest, t2);
            t1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();
            
            Assert.False(properties.Contains(TestOntology.uniqueResourceTest));
            Assert.IsFalse(actual.HasProperty(TestOntology.uniqueResourceTest));
            Assert.IsFalse(actual.HasProperty(TestOntology.uniqueResourceTest, t2));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(TestOntology.uniqueResourceTest).Count());

            var t3 = Model1.CreateResource<MappingTestClass3>(_r3);
            t3.Commit(); // Force loading the resource from the model with the appropriate (derived) type.
            
            // Test if derived types get properly mapped.
            t1.uniqueResourceTest = t3;
            t1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(t3, actual.uniqueResourceTest);
        }

        [Test]
        public void AddRemoveResourceListTest()
        {
            var t2 = new MappingTestClass2(_r2);
            var t3 = new MappingTestClass3(_r3);

            // Add value using the mapping interface
            var t1 = Model1.CreateResource<MappingTestClass>(_r1);
            t1.resourceTest.Add(t2);
            t1.resourceTest.Add(t3);
            t1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(2, actual.resourceTest.Count);
            Assert.Contains(t2, actual.resourceTest);
            Assert.Contains(t3, actual.resourceTest);
            
            var properties = actual.ListProperties();

            Assert.AreEqual(2, properties.Count());
            Assert.IsTrue(properties.Contains(TestOntology.resourceTest));
            Assert.IsTrue(actual.HasProperty(TestOntology.resourceTest));
            Assert.IsTrue(actual.HasProperty(TestOntology.resourceTest, t2));
            Assert.IsTrue(actual.HasProperty(TestOntology.resourceTest, t3));

            var values = actual.ListValues(TestOntology.resourceTest);

            Assert.AreEqual(2, properties.Count());
            Assert.IsTrue(values.Contains(t2));
            Assert.IsTrue(values.Contains(t3));

            t1.resourceTest.Remove(t2);
            t1.resourceTest.Remove(t3);
            t1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.IsFalse(actual.HasProperty(TestOntology.resourceTest));
            Assert.IsFalse(actual.HasProperty(TestOntology.resourceTest, t2));

            Assert.AreEqual(0, actual.resourceTest.Count);
        }

        [Test]
        public void LazyLoadResourceTest()
        {
            var r2 = Model1.CreateResource<MappingTestClass2>(_r2);

            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueResourceTest = r2; // TODO: Debug message, because t2 was not committed.
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            //Assert.AreEqual(null, actual.uniqueResourceTest);

            var values = actual.ListValues(TestOntology.uniqueResourceTest);
            
            Assert.AreEqual(r2.Uri.OriginalString, (values.First() as IResource).Uri.OriginalString);

            Model1.DeleteResource(r1);
            Model1.DeleteResource(r2);

            r2 = Model1.CreateResource<MappingTestClass2>(_r2);
            r2.Commit();

            r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueResourceTest = r2;
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r2, actual.uniqueResourceTest);
            Assert.AreEqual(typeof(MappingTestClass), Model1.GetResource(_r1).GetType());
        }

        [Test]
        public void MappingTypeTest()
        {
            var r1 = Model1.CreateResource<MappingTestClass2>(_r1);
            r1.uniqueStringTest = "testing 1";
            r1.Commit();
            
            var actual1 = Model1.GetResource<Resource>(_r1);
            Assert.AreEqual(r1, actual1);
            
            var r2 = Model1.CreateResource<MappingTestClass3>(_r2);
            r2.uniqueStringTest = "testing 2";
            r2.Commit();
            
            var actual2 = Model1.GetResource<Resource>(_r2);
            
            Assert.AreEqual(r2, actual2);
            
            actual2 = Model1.GetResource<MappingTestClass2>(_r2);
            
            Assert.AreEqual(r2, actual2);
            
            var r3 = Model1.CreateResource<MappingTestClass4>(_r3);
            r3.uniqueStringTest = "testing 3";
            r3.Commit();

            var actual3 = Model1.GetResource<Resource>(_r3);
            Assert.AreEqual(r3, actual3);
        }

        [Test]
        public void MappingTypeCollectionWithInferencingTest()
        {
            var t2 = Model1.CreateResource<PersonContact>(_r1);
            t2.NameFamily = "Doe";
            t2.Commit();

            var contacts = Model1.GetResources<Contact>(true).ToList() ;
            
            Assert.AreEqual(1, contacts.Count);
        }

        [Test]
        public void MultipeTypesMappingTest()
        {
            var r1 = Model1.CreateResource<MappingTestClass5>(_r1);
            r1.uniqueStringTest = "testing 3";
            r1.AddProperty(rdf.type, nco.Affiliation);
            r1.Commit();

            var actual = Model1.GetResource<Resource>(_r1);

            Assert.AreEqual(typeof(MappingTestClass5), actual.GetType());

            Model1.Clear();
            
            r1 = Model1.CreateResource<MappingTestClass5>(_r1);
            r1.uniqueStringTest = "testing 3";
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass5>(_r1);
            
            Assert.AreEqual(typeof(MappingTestClass5), actual.GetType());

            actual = Model1.GetResource<Contact>(_r1);
            
            Assert.AreEqual(typeof(Contact), actual.GetType());
        }

        [Test]
        public void MappingTypeWithInferencingTest()
        {
            var r1 = Model1.CreateResource<PersonContact>(_r1);
            r1.NameGiven = "Hans";
            r1.Commit();

            var query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o . ?s a @type .}");
            query.Bind("@type", nco.Contact);

            Assert.AreEqual(1, Model1.ExecuteQuery(query, true).GetResources().Count());
        }

        [Test]
        public void RollbackTest()
        {
            var value = "Hallo Welt!";
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueStringTest = value;
            r1.Commit();

            r1.uniqueStringTest = "HelloWorld!";
            r1.Rollback();

            Assert.AreEqual(value, r1.uniqueStringTest);

            // Get a new reference to the same resource; rX and r1 should be the same object.
            var rX = Model1.GetResource<MappingTestClass>(_r1);
            rX.stringTest.Add("Hi");
            rX.stringTest.Add("Blub");
            rX.Commit();

            r1.Rollback();

            Assert.AreEqual(2, r1.stringTest.Count);
            Assert.IsTrue(r1.stringTest.Contains("Hi"));
            Assert.IsTrue(r1.stringTest.Contains("Blub"));
            
            var r2 = Model1.CreateResource<MappingTestClass2>(_r2);
            r2.uniqueStringTest = "blub";
            r2.Commit();

            rX = Model1.GetResource<MappingTestClass>(_r1);
            rX.resourceTest.Add(r2);
            rX.Commit();

            r1.Rollback();

            Assert.IsTrue(r1.resourceTest.Count == 1);
            Assert.IsTrue(r1.resourceTest.Contains(r2));
        }

        [Test]
        public void RollbackMappedResourcesTest()
        {
            var r1 = Model1.CreateResource<SingleResourceMappingTestClass>(_r1);
            r1.Commit();

            var r2 = Model1.CreateResource<SingleMappingTestClass>(_r2);
            r2.stringTest.Add("blub");
            r2.Commit();

            var rX = Model1.GetResource<SingleResourceMappingTestClass>(_r1);
            rX.ResourceTest.Add(r2);
            rX.Commit();

            r1.Rollback();

            Assert.IsTrue(r1.ResourceTest.Count == 1);
            Assert.IsTrue(r1.ResourceTest.Contains(r2));
        }

        [Test]
        public void ListValuesTest()
        {
            var value = "Hallo Welt!";
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueStringTest = value;
            r1.Commit();

            r1.stringTest.Add("Hi");
            r1.stringTest.Add("Blub");
            r1.Commit();

            var values1 = r1.ListValues(TestOntology.stringTest).ToList();

            var actual = Model1.GetResource<MappingTestClass>(r1.Uri);
            
            var values2 = actual.ListValues(TestOntology.stringTest).ToList().ToList();

            Assert.AreEqual(values1.Count, values2.Count);
            Assert.IsTrue(values2.Contains(values1[0]));
            Assert.IsTrue(values2.Contains(values1[1]));
        }

        [Test]
        public void KeepListsAfterRollbackTest()
        {
            var r1 = Model1.CreateResource<SingleMappingTestClass>(_r1);
            r1.AddProperty(TestOntology.uniqueStringTest, "Hello");
            r1.Commit();
            r1.Rollback();

            r1.stringTest.Add("Hi");
            r1.stringTest.Add("Blub");
            
            var values1 = r1.ListValues(TestOntology.stringTest).ToList();
            
            Assert.AreEqual(2, values1.Count);
            
            r1.Commit();

            var rX = Model1.GetResource<SingleMappingTestClass>(_r1);

            var values2 = rX.ListValues(TestOntology.stringTest).ToList();

            Assert.AreEqual(values1.Count, values2.Count);
            Assert.IsTrue(values2.Contains(values1[0]));
            Assert.IsTrue(values2.Contains(values1[1]));
        }

        [Test]
        public void TestEquality()
        {
            var r1 = new Resource(ncal.cancelledEventStatus);
            var r2 = new Resource(ncal.cancelledEventStatus);

            Assert.IsTrue(r1.Equals(r2));
            Assert.IsFalse(r1 == r2);
        }

        [Test]
        public void TestStringPropertyMapping()
        {
            var r1 = new StringMappingTestClass(_r1);
            r1.uniqueStringTest = "Test string";

            var v = r1.GetValue(TestOntology.uniqueStringTest);
            
            Assert.AreEqual(r1.uniqueStringTest, v);

            r1.RandomProperty = "Test string 2";

            v = r1.GetValue(new Property(new Uri("http://www.example.com/property")));
            
            Assert.AreEqual(r1.RandomProperty, v);
        }

        [Test]
        public void TestLocalizedStringPropertyMapping()
        {
            var germanValue = "Hallo Welt";
            var englishValue = "Hello World";
            
            var r1 = Model1.CreateResource<StringMappingTestClass>(_r1);
            r1.AddProperty(TestOntology.uniqueStringTest, germanValue, "de");
            r1.AddProperty(TestOntology.uniqueStringTest, englishValue, "en");
            
            Assert.AreEqual(null, r1.uniqueStringTest);
            
            r1.Language = "de";
            
            Assert.AreEqual(germanValue, r1.uniqueStringTest);
            
            r1.Language = "en";
            
            Assert.AreEqual(englishValue, r1.uniqueStringTest);

            r1.Language = null;
            
            Assert.AreEqual(null, r1.uniqueStringTest);
        }

        [Test]
        public void TestLocalizedStringInvariancy()
        {
            var contact = Model1.CreateResource<PersonContact>(_r1);
            contact.NameGiven = "Peter";
            contact.Language = "de";
            
            Assert.AreEqual("Peter", contact.NameGiven);
        }
        
        [Test]
        public void TestLocalizedStringListPropertyMapping()
        {
            var germanValue = "Hallo Welt";
            var englishValue = "Hello World";
            
            var r1 = Model1.CreateResource<StringMappingTestClass>(_r1);
            r1.AddProperty(TestOntology.stringTest, germanValue+1, "de");
            r1.AddProperty(TestOntology.stringTest, germanValue+2, "de");
            r1.AddProperty(TestOntology.stringTest, germanValue+3, "de");
            r1.AddProperty(TestOntology.stringTest, englishValue+1, "en");
            r1.AddProperty(TestOntology.stringTest, englishValue+2, "en");
            r1.AddProperty(TestOntology.stringTest, englishValue+3, "en");
            r1.AddProperty(TestOntology.stringTest, englishValue+4, "en");
            
            Assert.AreEqual(0, r1.stringListTest.Count);
            
            var values = r1.ListValues(TestOntology.stringTest);
            
            Assert.AreEqual(7, values.Count());
            
            r1.AddProperty(TestOntology.stringTest, "Hello international World"+1);
            r1.AddProperty(TestOntology.stringTest, "Hello international World"+2);
            
            Assert.AreEqual(2, r1.stringListTest.Count);
            Assert.AreEqual(9, r1.ListValues(TestOntology.stringTest).Count());
            
            r1.RemoveProperty(TestOntology.stringTest, "Hello international World"+1);
            
            Assert.AreEqual(1, r1.stringListTest.Count);
            Assert.AreEqual(8, r1.ListValues(TestOntology.stringTest).Count());
            
            r1.Language = "de";
            
            Assert.AreEqual(3, r1.stringListTest.Count);
            Assert.AreEqual(8, r1.ListValues(TestOntology.stringTest).Count());
            
            r1.Language = "en";
            
            Assert.AreEqual(4, r1.stringListTest.Count);
            Assert.AreEqual(8, r1.ListValues(TestOntology.stringTest).Count());
            
            r1.RemoveProperty(TestOntology.stringTest, germanValue + 1, "de");
            
            Assert.AreEqual(7, r1.ListValues(TestOntology.stringTest).Count());

            r1.RemoveProperty(TestOntology.stringTest, englishValue + 1, "en");
            
            Assert.AreEqual(7, r1.ListValues(TestOntology.stringTest).Count());
        }

        [Test]
        public void TestLocalizedStringListPropertyMapping2()
        {
            var germanValue = "Hallo Welt";
            var englishValue = "Hello World";
            
            var r1 = Model1.CreateResource<StringMappingTestClass>(_r1);
            r1.stringListTest.Add("Hello interanational World" + 1);
            r1.stringListTest.Add("Hello interanational World" + 2);
            
            r1.Language = "de";
            r1.stringListTest.Add(germanValue + 1);
            r1.stringListTest.Add(germanValue + 2);
            r1.stringListTest.Add(germanValue + 3);
            
            Assert.AreEqual(3, r1.stringListTest.Count);
            Assert.AreEqual(5, r1.ListValues(TestOntology.stringTest).Count());

            r1.Language = "en";
            r1.stringListTest.Add(englishValue + 1);
            r1.stringListTest.Add(englishValue + 2);
            r1.stringListTest.Add(englishValue + 3);
            r1.stringListTest.Add(englishValue + 4);
            
            Assert.AreEqual(4, r1.stringListTest.Count);
            Assert.AreEqual(9, r1.ListValues(TestOntology.stringTest).Count());

            r1.Language = null;
            
            Assert.AreEqual(2, r1.stringListTest.Count);
            Assert.AreEqual(9, r1.ListValues(TestOntology.stringTest).Count());
        }

        [Test]
        public void TestJsonSerialization()
        {
            var expected = Model1.CreateResource<JsonMappingTestClass>();
            expected.stringTest.Add("Hello World!");
            expected.stringTest.Add("Hallo Welt!");
            expected.Commit();

            var json = JsonConvert.SerializeObject(expected);
            var jsonSettings = new JsonResourceSerializerSettings(Store);

            var actual = JsonConvert.DeserializeObject<JsonMappingTestClass>(json, jsonSettings);

            Assert.AreEqual(expected.Uri, actual.Uri);
            Assert.AreEqual(expected.Model.Uri, actual.Model.Uri);
            Assert.AreEqual(2, actual.stringTest.Count);
        }
        
        #endregion
    }
}
