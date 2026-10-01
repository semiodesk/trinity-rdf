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
using Semiodesk.Trinity.Query.Sparql;
using Semiodesk.Trinity.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class ResourceMappingTest<T> : StoreTest<T> where T : IStoreTestSetup
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

        //[Test]
        // This test does not run, but it needs to.
        public virtual void AddUnmappedType()
        {
            var r2 = Model1.CreateResource(_r2);
            r2.AddProperty(rdf.type, to.TestClass2);
            
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.AddProperty(to.uniqueResourceTest, r2);
            r1.AddProperty(to.resourceTest, r2);

            Assert.IsNull(r1.uniqueResourceTest);
            Assert.AreEqual(0, r1.resourceTest.Count);
        }

        [Test]
        public virtual void GetTypesTest()
        {
            var r1 = new MappingTestClass2(_r1);
            var types1 = r1.GetTypes().ToList();
            
            Assert.AreEqual(1, types1.Count);
            Assert.Contains(to.TestClass2, types1);

            var r2 = new MappingTestClass3(_r2);
            var types2 = r2.GetTypes().ToList();
            
            Assert.AreEqual(1, types2.Count);
            Assert.Contains(to.TestClass3, types2);
        }

        [Test]
        public virtual void AddRemoveIntegerTest()
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
            
            Assert.True(properties.Contains(to.uniqueIntTest));
            Assert.AreEqual(2, properties.Count()); // rdf:type, to:uniqueIntTest

            // Test if ListValues works
            Assert.AreEqual(typeof(int), actual.ListValues(to.uniqueIntTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.uniqueIntTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueIntTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(r1);

            // Test if ListProperties works
            properties = actual.ListProperties();
            
            Assert.False(properties.Contains(to.uniqueIntTest));
            Assert.AreEqual(0, actual.ListValues(to.uniqueIntTest).Count());
        }

        [Test]
        public virtual void AddRemoveIntegerListTest()
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
            Assert.True(properties.Contains(to.intTest));
            Assert.AreEqual(2, properties.Count()); // rdf:type, to:intTest

            // Test if ListValues works
            Assert.AreEqual(typeof(int), actual.ListValues(to.intTest).First().GetType());
            Assert.AreEqual(value1, actual.ListValues(to.intTest).First());

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
            Assert.True(properties.Contains(to.intTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var values = actual.ListValues(to.intTest).ToList();
            
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
            Assert.True(properties.Contains(to.intTest));

            // Test if first added property is still present
            Assert.AreEqual(typeof(int), actual.ListValues(to.intTest).First().GetType());
            Assert.AreEqual(value1, actual.ListValues(to.intTest).First());

            r1.intTest.Remove(value1);
            r1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties();
            
            // Test if ListValues works
            Assert.False(properties.Contains(to.intTest));
            Assert.AreEqual(0, actual.ListValues(to.intTest).Count());
        }

        /// <summary>
        /// This Test fails because the datatype "unsigned int" is not stored correctly in the database. 
        /// To be more specific the xsd type is missing although it is given at the insert.
        /// </summary>
        //[Test]
        public virtual void AddRemoveUnsignedIntegerTest()
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
            Assert.True(properties.Contains(to.uniqueUintTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(uint), actual.ListValues(to.uniqueUintTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.uniqueUintTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueUintTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = (List<Property>)actual.ListProperties();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueUintTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueUintTest).Count());
        }

        //[Test]
        public virtual void AddRemoveUnsignedIntegerListTest()
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
            Assert.True(properties.Contains(to.uintTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(uint), actual.ListValues(to.uintTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.uintTest).First());

            // Remove value from mapped list
            r1.uintTest.Remove(value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = (List<Property>)actual.ListProperties();

            // Test if removed
            Assert.AreEqual(0, actual.uintTest.Count());

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uintTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uintTest).Count());
        }

        [Test]
        public virtual void AddRemoveStringTest()
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
            Assert.True(properties.Contains(to.uniqueStringTest));
            Assert.AreEqual(2, properties.Count());
            Assert.IsTrue(actual.HasProperty(to.uniqueStringTest));
            Assert.IsTrue(actual.HasProperty(to.uniqueStringTest, value));

            // Test if ListValues works
            Assert.AreEqual(typeof(string), actual.ListValues(to.uniqueStringTest).First().GetType());
            Assert.AreEqual(value, r1.ListValues(to.uniqueStringTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueStringTest, value);
            r1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueStringTest));
            Assert.IsFalse(actual.HasProperty(to.uniqueStringTest));
            Assert.IsFalse(actual.HasProperty(to.uniqueStringTest, value));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueStringTest).Count());
            
            // Test if escaping works
            r1.uniqueStringTest = "ASK { < http://steadymojo.com/sleepState> <http://www.close-game.com/ontologies/2015/ia/hasBoolValue> ?o. Filter( ?o != 'false'^^<http://www.w3.org/2001/XMLSchema#boolean>) }";
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r1.uniqueStringTest, actual.uniqueStringTest);
        }

        [Test]
        public virtual void AddRemoveStringListTest()
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
            Assert.True(properties.Contains(to.stringTest));
            Assert.AreEqual(2, properties.Count());
            Assert.IsTrue(actual.HasProperty(to.stringTest));
            Assert.IsTrue(actual.HasProperty(to.stringTest, value));

            // Test if ListValues works
            Assert.AreEqual(typeof(string), actual.ListValues(to.stringTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.stringTest).First());

            // Remove value from mapped list
            r1.stringTest.Remove(value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties();

            // Test if removed
            Assert.AreEqual(0, actual.boolTest.Count());

            // Test if ListProperties works
            Assert.False(properties.Contains(to.stringTest));
            Assert.IsFalse(actual.HasProperty(to.stringTest));
            Assert.IsFalse(actual.HasProperty(to.stringTest, value));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.stringTest).Count());
        }

        [Test]
        public virtual void AddRemoveBoolTest()
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
            Assert.True(properties.Contains(to.uniqueBoolTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(bool), actual.ListValues(to.uniqueBoolTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.uniqueBoolTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueBoolTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueBoolTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueBoolTest).Count());
        }

        [Test]
        public virtual void AddRemoveBoolListTest()
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
            Assert.True(properties.Contains(to.boolTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(bool), actual.ListValues(to.boolTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.boolTest).First());

            // Remove value from mapped list
            r1.boolTest.Remove(value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if removed
            Assert.AreEqual(0, actual.boolTest.Count());

            // Test if ListProperties works
            Assert.False(properties.Contains(to.boolTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.boolTest).Count());
        }

        [Test]
        public virtual void AddRemoveFloatTest()
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
            Assert.True(properties.Contains(to.uniqueFloatTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(float), actual.ListValues(to.uniqueFloatTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.uniqueFloatTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueFloatTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueFloatTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueFloatTest).Count());
        }

        [Test]
        public virtual void AddRemoveDoubleTest()
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
            Assert.True(properties.Contains(to.uniqueDoubleTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(double), actual.ListValues(to.uniqueDoubleTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.uniqueDoubleTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueDoubleTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueDoubleTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueDoubleTest).Count());

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
        public virtual void AddRemoveDecimalTest()
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
            Assert.True(properties.Contains(to.uniqueDecimalTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            Assert.AreEqual(typeof(decimal), actual.ListValues(to.uniqueDecimalTest).First().GetType());
            Assert.AreEqual(value, actual.ListValues(to.uniqueDecimalTest).First());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueDecimalTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueDecimalTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueDecimalTest).Count());
        }

        /// <summary>
        /// Note: 
        /// Datetime precision in Virtuoso is not as high as native .net datetime precision.
        /// </summary>
        [Test]
        public virtual void AddRemoveDateTimeTest()
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
            Assert.True(properties.Contains(to.uniqueDatetimeTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var v = (DateTime)actual.ListValues(to.uniqueDatetimeTest).First();
            Assert.IsNotNull(v);
            Assert.AreEqual(value.ToUniversalTime(), v.ToUniversalTime());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueDatetimeTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueDatetimeTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueDatetimeTest).Count());
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out var t));

            r1.uniqueDateTimeTest = t;
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r1.uniqueDateTimeTest, actual.uniqueDateTimeTest.ToLocalTime());
        }

        [Test]
        public virtual void AddRemoveUriTest()
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
        public virtual void AddRemoveUriPropTest()
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
            Assert.True(properties.Contains(to.uniqueUriTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var v = (Uri)actual.ListValues(to.uniqueUriTest).First();
            
            Assert.IsNotNull(v);
            Assert.AreEqual(value.ToString(), v.ToString());

            // Remove with RemoveProperty
            r1.RemoveProperty(to.uniqueUriTest, value);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();

            // Test if ListProperties works
            Assert.False(properties.Contains(to.uniqueUriTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueUriTest).Count());

            r1.uriTest.Add(new UriRef("urn:test#myUri1"));
            r1.uriTest.Add(new UriRef("urn:test#myUri2"));
            r1.uriTest.Add(new UriRef("urn:test3"));
            r1.uriTest.Add(new UriRef("urn:test/my#Uri4"));
            r1.uriTest.Add(new UriRef("urn:test#5"));
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.AreEqual(r1.uriTest.Count, actual.uriTest.Count);
        }
        /// <summary>
        /// The mapped properties are typed UriRef, which TRIN007 and the PropertyMapping check require.
        /// Callers still reach the unmapped surface with a plain System.Uri — AddProperty/RemoveProperty
        /// both take one — so a raw Uri has to add *and* remove against a UriRef mapping. Removal was the
        /// half that broke: the scalar path threw, and the collection path silently did nothing, which
        /// the Commit() below would then undo by re-persisting the value.
        /// </summary>
        [Test]
        public virtual void AddRemovePlainUriAgainstUriRefMappingTest()
        {
            var plain = new Uri(_r2.OriginalString);

            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.AddProperty(to.uniqueUriTest, plain);
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.IsNotNull(actual.uniqueUriTest, "A plain Uri must reach the mapped property, not the unmapped bag.");
            Assert.AreEqual(_r2.OriginalString, actual.uniqueUriTest.OriginalString);

            r1.RemoveProperty(to.uniqueUriTest, plain);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.IsNull(actual.uniqueUriTest, "Removal must persist; a silent no-op is re-committed.");
            Assert.AreEqual(0, actual.ListValues(to.uniqueUriTest).Count());

            // The collection half, where the failure was silent rather than loud.
            var item = new Uri("urn:test#plain1");

            r1.AddProperty(to.uriTest, item);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(1, actual.uriTest.Count);

            r1.RemoveProperty(to.uriTest, item);
            r1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(0, actual.uriTest.Count, "Removal from a mapped collection must not silently do nothing.");
        }

        [Test]
        public virtual void TimeZoneTest()
        {
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out var value));

            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueDateTimeTest = value;
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);

            var v0 = r1.uniqueDateTimeTest.ToUniversalTime();
            var v1 = actual.uniqueDateTimeTest.ToUniversalTime();
            
            Assert.AreEqual(v0, v1);
        }

        [Test]
        public virtual void AddRemoveDateTimeListTest()
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
            Assert.True(properties.Contains(to.datetimeTest));
            Assert.AreEqual(2, properties.Count());

            // Test if ListValues works
            var v = (DateTime)actual.ListValues(to.datetimeTest).First();
            
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
            Assert.False(properties.Contains(to.datetimeTest));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.datetimeTest).Count());
        }

        [Test]
        public virtual void AddRemoveResourceTest()
        {
            var t2 = new MappingTestClass2(_r2);

            var t1 = Model1.CreateResource<MappingTestClass>(_r1);
            t1.uniqueResourceTest = t2;
            t1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(t2, actual.uniqueResourceTest);
            
            var properties = actual.ListProperties().ToList();
            
            Assert.Contains(to.uniqueResourceTest, properties);
            Assert.AreEqual(2, properties.Count());

            Assert.IsTrue(actual.HasProperty(to.uniqueResourceTest));
            Assert.IsTrue(actual.HasProperty(to.uniqueResourceTest, t2));

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            var values = actual.ListValues().ToList();
            
            Assert.Contains( new Tuple<Property, object>(to.uniqueResourceTest, t2), values);
            
            Assert.IsTrue(typeof(Resource).IsAssignableFrom(actual.ListValues(to.uniqueResourceTest).First().GetType()));
            //Assert.AreEqual(t2, t_actual.ListValues(to.uniqeResourceTest).First());

            t1.RemoveProperty(to.uniqueResourceTest, t2);
            t1.Commit();
            
            actual = Model1.GetResource<MappingTestClass>(_r1);
            properties = actual.ListProperties().ToList();
            
            Assert.False(properties.Contains(to.uniqueResourceTest));
            Assert.IsFalse(actual.HasProperty(to.uniqueResourceTest));
            Assert.IsFalse(actual.HasProperty(to.uniqueResourceTest, t2));

            // Test if ListValues works
            Assert.AreEqual(0, actual.ListValues(to.uniqueResourceTest).Count());

            var t3 = Model1.CreateResource<MappingTestClass3>(_r3);
            t3.Commit(); // Force loading the resource from the model with the appropriate (derived) type.
            
            // Test if derived types get properly mapped.
            t1.uniqueResourceTest = t3;
            t1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(t3, actual.uniqueResourceTest);
        }

        [Test]
        public virtual void AddRemoveResourceListTest()
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
            Assert.IsTrue(properties.Contains(to.resourceTest));
            Assert.IsTrue(actual.HasProperty(to.resourceTest));
            Assert.IsTrue(actual.HasProperty(to.resourceTest, t2));
            Assert.IsTrue(actual.HasProperty(to.resourceTest, t3));

            var values = actual.ListValues(to.resourceTest);

            Assert.AreEqual(2, properties.Count());
            Assert.IsTrue(values.Contains(t2));
            Assert.IsTrue(values.Contains(t3));

            t1.resourceTest.Remove(t2);
            t1.resourceTest.Remove(t3);
            t1.Commit();

            actual = Model1.GetResource<MappingTestClass>(_r1);
            
            Assert.IsFalse(actual.HasProperty(to.resourceTest));
            Assert.IsFalse(actual.HasProperty(to.resourceTest, t2));

            Assert.AreEqual(0, actual.resourceTest.Count);
        }

        [Test]
        public virtual void LazyLoadResourceTest()
        {
            var r2 = Model1.CreateResource<MappingTestClass2>(_r2);

            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueResourceTest = r2; // TODO: Debug message, because t2 was not committed.
            r1.Commit();

            var actual = Model1.GetResource<MappingTestClass>(_r1);
            //Assert.AreEqual(null, actual.uniqueResourceTest);

            var values = actual.ListValues(to.uniqueResourceTest);
            
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

        /// <summary>
        /// How many integers, and how many links, <see cref="SeedResourceWithThousandsOfValues"/> gives a resource.
        /// </summary>
        protected const int ValuesPerKind = 1500;

        /// <summary>
        /// Writes <c>r1</c> as a <see cref="MappingTestClass"/> with <see cref="ValuesPerKind"/> integers,
        /// as many links and one scalar: about 3000 triples. The linked members are not written; a link reads
        /// back without them, as <see cref="AddRemoveResourceListTest"/> relies on too.
        /// </summary>
        /// <remarks>
        /// Seeded as Turtle, not through the mapper, because reading is what is under test and Virtuoso cannot
        /// take this resource as one <c>Commit()</c>. That goes out as a single <c>INSERT { … }</c> template,
        /// which Virtuoso compiles to SQL and refuses at this size (<c>SP031</c>: generated SQL text exceeded
        /// 10000 lines). The adapter swallows the error (#50), so the commit returns normally having written
        /// nothing. <c>Read</c> sends <c>INSERT DATA</c> instead, which holds up to #70's 10,000 triples.
        /// </remarks>
        protected UriRef SeedResourceWithThousandsOfValues()
        {
            string subject = SparqlSerializer.SerializeUri(_r1);

            var turtle = new StringBuilder();

            void Triple(Property predicate, string value)
            {
                turtle.Append(subject).Append(' ').Append(SparqlSerializer.SerializeUri(predicate.Uri))
                    .Append(' ').Append(value).Append(" .\n");
            }

            Triple(rdf.type, SparqlSerializer.SerializeUri(to.TestClass.Uri));
            Triple(to.uniqueStringTest, "\"hub\"");

            for (int i = 0; i < ValuesPerKind; i++)
            {
                Triple(to.intTest, "\"" + i + "\"^^<http://www.w3.org/2001/XMLSchema#int>");
                Triple(to.resourceTest, SparqlSerializer.SerializeUri(BaseUri.GetUriRef("member" + i)));
            }

            Assert.IsTrue(Model1.Read(turtle.ToString(), RdfSerializationFormat.Turtle, true));

            return _r1;
        }

        /// <summary>
        /// A resource with a few thousand values reads back whole: its scalar, every integer, every link.
        /// </summary>
        /// <remarks>
        /// Every other resource this fixture reads is small, and reading one was quadratic in its own size
        /// (#63): 26.5 s on GraphDB at 2000 links. The answer was right, only slow, so this asserts the answer
        /// at a size where the old cost shows; <c>DotNetRDFResourceMappingTest</c> bounds the cost itself.
        /// </remarks>
        [Test]
        public virtual void ReadsAResourceWithThousandsOfValues()
        {
            var uri = SeedResourceWithThousandsOfValues();

            var actual = Model1.GetResource<MappingTestClass>(uri);

            Assert.AreEqual("hub", actual.uniqueStringTest);

            CollectionAssert.AreEquivalent(Enumerable.Range(0, ValuesPerKind), actual.intTest);

            CollectionAssert.AreEquivalent(
                Enumerable.Range(0, ValuesPerKind).Select(i => BaseUri.GetUriRef("member" + i).OriginalString),
                actual.resourceTest.Select(r => r.Uri.OriginalString));
        }

        /// <summary>
        /// Reading many resources compares each against the others a bounded number of times, not once per
        /// resource already read.
        /// </summary>
        /// <remarks>
        /// Both result readers kept the resources they had emitted in a list and asked
        /// <c>result.Contains</c> before adding one, which is a linear scan per resource. A typed resource is
        /// always in the reader's cache before its first triple, so every typed read took that branch: about
        /// n²/2 comparisons, 74 s for 64,000 resources on Virtuoso against 0.46 s for the raw query. Counting
        /// <see cref="object.Equals(object)"/> rather than timing makes the bound deterministic; before the
        /// fix this read made about two million calls.
        /// </remarks>
        [Test]
        public virtual void ReadingManyResourcesComparesEachABoundedNumberOfTimes()
        {
            const int count = 2000;

            var turtle = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                turtle.Append(SparqlSerializer.SerializeUri(BaseUri.GetUriRef("counted" + i)))
                    .Append(" a ").Append(SparqlSerializer.SerializeUri(to.EqualityCountingTestClass.Uri))
                    .Append(" .\n");
            }

            Assert.IsTrue(Model1.Read(turtle.ToString(), RdfSerializationFormat.Turtle, true));

            EqualityCountingTestClass.EqualsCalls = 0;

            var actual = Model1.GetResources<EqualityCountingTestClass>().ToList();

            int calls = EqualityCountingTestClass.EqualsCalls;

            Assert.AreEqual(count, actual.Count);
            Assert.Less(calls, count, $"reading {count} resources made {calls} Equals calls");
        }

        /// <summary>
        /// Every store reads a resource with a bare <c>DESCRIBE</c>, the one form <c>StoreBase</c> builds.
        /// </summary>
        /// <remarks>
        /// This enforces a policy: one describe query, in <c>StoreBase</c>, and no store override. Four stores
        /// used to override it with a <c>VALUES</c>-bound pattern, a 2021 blank-id workaround copied from store
        /// to store. That form has one solution per triple, and the in-memory engine describes the subject once
        /// per solution (#63). On GraphDB it also happened to hide that store's incoming triples, a job
        /// <c>Model.GetResource&lt;T&gt;</c> now does for every store by checking the URI (see
        /// <see cref="GetResourceOfAnIriThatIsOnlyAnObjectThrows"/>). So no store needs a pattern for
        /// correctness, and a store that wants one anyway has to change this test and say why. Only the
        /// query is built, so this costs no round trip.
        /// </remarks>
        [Test]
        public virtual void DescribesTheSubjectWithoutAPattern()
        {
            string sparql = Store.GetDescribeQuery(Model1.Uri, _r1).ToString();

            StringAssert.Contains("DESCRIBE", sparql);
            StringAssert.Contains(SparqlSerializer.SerializeUri(_r1), sparql);

            Assert.IsFalse(sparql.Contains("WHERE", StringComparison.OrdinalIgnoreCase),
                "a pattern makes one solution per triple, each described again:\n" + sparql);
            Assert.IsFalse(sparql.Contains("VALUES", StringComparison.OrdinalIgnoreCase),
                "the subject is named directly, not bound:\n" + sparql);
        }

        /// <summary>
        /// Writes <paramref name="triples"/> into <c>Model1</c> as Turtle, each a subject, a predicate and an
        /// already serialized object.
        /// </summary>
        private void Seed(params (Uri Subject, Property Predicate, string Object)[] triples)
        {
            var turtle = new StringBuilder();

            foreach (var t in triples)
            {
                turtle.Append(SparqlSerializer.SerializeUri(t.Subject)).Append(' ')
                    .Append(SparqlSerializer.SerializeUri(t.Predicate.Uri)).Append(' ')
                    .Append(t.Object).Append(" .\n");
            }

            Assert.IsTrue(Model1.Read(turtle.ToString(), RdfSerializationFormat.Turtle, true));
        }

        /// <summary>
        /// Reading an IRI that occurs only as an object finds no resource, rather than the resource that
        /// refers to it.
        /// </summary>
        /// <remarks>
        /// A <c>DESCRIBE</c> is the store's choice of triples, and GraphDB's includes the incoming ones: for
        /// <c>&lt;c&gt; p &lt;d&gt;</c>, <c>DESCRIBE &lt;d&gt;</c> answers that triple. <c>GetResource&lt;T&gt;</c>
        /// used to return the first resource in the answer, so on GraphDB it returned <c>&lt;c&gt;</c> for
        /// <c>&lt;d&gt;</c>. The <c>VALUES</c>-bound pattern GraphDB's override used (#63) hid this by accident,
        /// because a subject without outgoing triples gave it no solution to describe; the URI check in
        /// <c>Model.GetResource&lt;T&gt;</c> now does that job on every store.
        /// </remarks>
        [Test]
        public virtual void GetResourceOfAnIriThatIsOnlyAnObjectThrows()
        {
            var target = BaseUri.GetUriRef("only-an-object");

            Seed(
                (_r2, rdf.type, SparqlSerializer.SerializeUri(to.TestClass.Uri)),
                (_r2, to.resourceTest, SparqlSerializer.SerializeUri(target)));

            Assert.Throws<ArgumentException>(() => Model1.GetResource<MappingTestClass>(target));
        }

        /// <summary>
        /// A subject that other resources refer to reads back as itself, whatever order the store puts the
        /// incoming triples in.
        /// </summary>
        /// <remarks>
        /// On GraphDB the answer to <c>DESCRIBE</c> holds the referrers too (see
        /// <see cref="GetResourceOfAnIriThatIsOnlyAnObjectThrows"/>). Returning the first resource was right
        /// there only because the subject happened to come first, which nothing guarantees. Several referrers
        /// give the order more chances to differ.
        /// </remarks>
        [Test]
        public virtual void GetResourceOfASubjectWithReferrersReadsItself()
        {
            var triples = new List<(Uri, Property, string)>
            {
                (_r1, rdf.type, SparqlSerializer.SerializeUri(to.TestClass.Uri)),
                (_r1, to.uniqueStringTest, "\"target\""),
            };

            for (int i = 0; i < 20; i++)
            {
                var referrer = BaseUri.GetUriRef("referrer" + i);

                triples.Add((referrer, rdf.type, SparqlSerializer.SerializeUri(to.TestClass.Uri)));
                triples.Add((referrer, to.uniqueStringTest, "\"referrer\""));
                triples.Add((referrer, to.resourceTest, SparqlSerializer.SerializeUri(_r1)));
            }

            Seed(triples.ToArray());

            var actual = Model1.GetResource<MappingTestClass>(_r1);

            Assert.AreEqual(_r1.OriginalString, actual.Uri.OriginalString);
            Assert.AreEqual("target", actual.uniqueStringTest);
        }

        /// <summary>
        /// A subject whose host is not lower case reads back as itself, with a referrer in the answer.
        /// </summary>
        /// <remarks>
        /// <c>GetResource&lt;T&gt;</c> picks the subject out of the store's answer, so how it compares IRIs
        /// decides whether this read works. Measured: the in-memory store and Fuseki hand the subject back
        /// with its host lower-cased (GraphDB, Oxigraph and Virtuoso keep it), the same loss
        /// <c>doc/known-test-failures.md</c> records for result sets. An ordinal match on
        /// <c>OriginalString</c> would turn this read into a not-found on those two stores, so when no subject
        /// matches exactly the match falls back to <see cref="UriRef"/> identity, and this test fails if that
        /// fallback goes (<c>DescribeAnswerMatchTest</c> pins that the exact match wins). It asserts that
        /// identity rather than the spelling, because restoring the spelling is the quarantined defect's fix,
        /// not this one's. Seeded through <c>Commit()</c>: a Turtle <c>Read</c> lower-cases the host before
        /// writing, which would test the write instead.
        /// </remarks>
        [Test]
        public virtual void GetResourceOfAMixedCaseHostSubjectReadsItself()
        {
            var subject = new UriRef("http://Mixed.Example.org/trinity/subject");

            var resource = Model1.CreateResource<MappingTestClass>(subject);
            resource.uniqueStringTest = "mixed";
            resource.Commit();

            var referrer = Model1.CreateResource<MappingTestClass>(_r2);
            referrer.resourceTest.Add(new MappingTestClass2(subject));
            referrer.Commit();

            var actual = Model1.GetResource<MappingTestClass>(subject);

            Assert.IsTrue(subject.Equals(actual.Uri), $"read <{actual.Uri.OriginalString}> for <{subject.OriginalString}>");
            Assert.AreEqual("mixed", actual.uniqueStringTest);
        }

        [Test]
        public virtual void MappingTypeTest()
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
        public virtual void MappingTypeCollectionWithInferencingTest()
        {
            var t2 = Model1.CreateResource<PersonContact>(_r1);
            t2.NameFamily = "Doe";
            t2.Commit();

            var contacts = Model1.GetResources<Contact>(true).ToList() ;
            
            Assert.AreEqual(1, contacts.Count);
        }

        [Test]
        public virtual void MultipeTypesMappingTest()
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
        public virtual void MappingTypeWithInferencingTest()
        {
            var r1 = Model1.CreateResource<PersonContact>(_r1);
            r1.NameGiven = "Hans";
            r1.Commit();

            var query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o . ?s a @type .}");
            query.Bind("@type", nco.Contact);

            Assert.AreEqual(1, Model1.ExecuteQuery(query, true).GetResources().Count());
        }

        [Test]
        public virtual void RollbackTest()
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
        public virtual void RollbackMappedResourcesTest()
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
        public virtual void ListValuesTest()
        {
            var value = "Hallo Welt!";
            
            // Add value using the mapping interface
            var r1 = Model1.CreateResource<MappingTestClass>(_r1);
            r1.uniqueStringTest = value;
            r1.Commit();

            r1.stringTest.Add("Hi");
            r1.stringTest.Add("Blub");
            r1.Commit();

            var values1 = r1.ListValues(to.stringTest).ToList();

            var actual = Model1.GetResource<MappingTestClass>(r1.Uri);
            
            var values2 = actual.ListValues(to.stringTest).ToList().ToList();

            Assert.AreEqual(values1.Count, values2.Count);
            Assert.IsTrue(values2.Contains(values1[0]));
            Assert.IsTrue(values2.Contains(values1[1]));
        }

        [Test]
        public virtual void KeepListsAfterRollbackTest()
        {
            var r1 = Model1.CreateResource<SingleMappingTestClass>(_r1);
            r1.AddProperty(to.uniqueStringTest, "Hello");
            r1.Commit();
            r1.Rollback();

            r1.stringTest.Add("Hi");
            r1.stringTest.Add("Blub");
            
            var values1 = r1.ListValues(to.stringTest).ToList();
            
            Assert.AreEqual(2, values1.Count);
            
            r1.Commit();

            var rX = Model1.GetResource<SingleMappingTestClass>(_r1);

            var values2 = rX.ListValues(to.stringTest).ToList();

            Assert.AreEqual(values1.Count, values2.Count);
            Assert.IsTrue(values2.Contains(values1[0]));
            Assert.IsTrue(values2.Contains(values1[1]));
        }

        [Test]
        public virtual void TestEquality()
        {
            var r1 = new Resource(ncal.cancelledEventStatus);
            var r2 = new Resource(ncal.cancelledEventStatus);

            Assert.IsTrue(r1.Equals(r2));
            Assert.IsFalse(r1 == r2);
        }

        [Test]
        public virtual void TestStringPropertyMapping()
        {
            var r1 = new StringMappingTestClass(_r1);
            r1.uniqueStringTest = "Test string";

            var v = r1.GetValue(to.uniqueStringTest);
            
            Assert.AreEqual(r1.uniqueStringTest, v);

            r1.RandomProperty = "Test string 2";

            v = r1.GetValue(new Property(new Uri("http://www.example.com/property")));
            
            Assert.AreEqual(r1.RandomProperty, v);
        }

        /// <summary>
        /// A mapped container holds every language at once and survives a store round trip.
        /// </summary>
        /// <remarks>
        /// This is what the ambient Resource.Language could not do: every assertion here about a
        /// language other than the "current" one was previously unanswerable from the mapped surface.
        /// </remarks>
        [Test]
        public virtual void LocalizedContainersRoundTripEveryLanguage()
        {
            var r1 = Model1.CreateResource<LocalizedMappingTestClass>(_r1);

            r1.Label["de"] = "Hallo Welt";
            r1.Label["en"] = "Hello World";
            r1.Label.Invariant = "plain";

            r1.Aliases.Add("de", "Erdapfel");
            r1.Aliases.Add("de", "Kartoffel");
            r1.Aliases.Add("en", "Potato");

            r1.Commit();

            var actual = Model1.GetResource<LocalizedMappingTestClass>(_r1);

            // Both languages are readable, at once, without switching anything.
            Assert.AreEqual("Hallo Welt", actual.Label["de"]);
            Assert.AreEqual("Hello World", actual.Label["en"]);
            CollectionAssert.AreEqual(new[] { "de", "en" }, actual.Label.Languages);

            // The untagged literal on the same predicate is kept apart from the languages.
            Assert.AreEqual("plain", actual.Label.Invariant);

            // The collection keeps both German values; the scalar container would have kept one.
            CollectionAssert.AreEquivalent(new[] { "Erdapfel", "Kartoffel" }, actual.Aliases["de"]);
            CollectionAssert.AreEqual(new[] { "Potato" }, actual.Aliases["en"]);

            // Lookup works against what came back from the store, not only against what was written.
            Assert.AreEqual("Hallo Welt", actual.Label.Best("de-AT", "en"));

            // And the untyped surface reports the same tags.
            CollectionAssert.AreEqual(new[] { "de", "en" }, actual.ListLanguages(to.uniqueLocalizedStringCultureTest));
        }

        /// <summary>
        /// Writing one language must not disturb another. Under the previous design this held only
        /// because every other language sat in the untyped bag while one was mapped - an invariant
        /// nothing stated or tested.
        /// </summary>
        [Test]
        public virtual void EditingOneLanguageLeavesTheOthersAlone()
        {
            var r1 = Model1.CreateResource<LocalizedMappingTestClass>(_r1);

            r1.Label["de"] = "Hallo Welt";
            r1.Label["en"] = "Hello World";
            r1.Commit();

            var edited = Model1.GetResource<LocalizedMappingTestClass>(_r1);
            edited.Label["de"] = "Servus";
            edited.Commit();

            var actual = Model1.GetResource<LocalizedMappingTestClass>(_r1);

            Assert.AreEqual("Servus", actual.Label["de"]);
            Assert.AreEqual("Hello World", actual.Label["en"], "Committing German must not delete English.");
            Assert.AreEqual(2, actual.Label.Count);
        }

        /// <summary>
        /// Removing a language removes only that language's triples.
        /// </summary>
        [Test]
        public virtual void RemovingALanguageLeavesTheOthersAlone()
        {
            var r1 = Model1.CreateResource<LocalizedMappingTestClass>(_r1);

            r1.Label["de"] = "Hallo Welt";
            r1.Label["en"] = "Hello World";
            r1.Commit();

            var edited = Model1.GetResource<LocalizedMappingTestClass>(_r1);
            Assert.IsTrue(edited.Label.Remove("de"));
            edited.Commit();

            var actual = Model1.GetResource<LocalizedMappingTestClass>(_r1);

            Assert.IsNull(actual.Label["de"]);
            Assert.AreEqual("Hello World", actual.Label["en"]);
            CollectionAssert.AreEqual(new[] { "en" }, actual.Label.Languages);
        }

        /// <summary>
        /// The same contract through the generator instead of a hand-written mapping, declared get-only
        /// as ADR-0048 recommends.
        /// </summary>
        /// <remarks>
        /// Both authoring routes have to stay first-class (ADR-0018), and this is the one that was
        /// briefly impossible: emitting get+set unconditionally made a get-only mapped property CS9253.
        /// </remarks>
        [Test]
        public virtual void GeneratedContainersRoundTripEveryLanguage()
        {
            var r1 = Model1.CreateResource<LocalizedDocument>(_r1);

            r1.Title["de"] = "Bericht";
            r1.Title["en"] = "Report";
            r1.Keywords.Add("de", "Jahresbericht");
            r1.Keywords.Add("de", "Geschäftsbericht");
            r1.Code = "DOC-1";
            r1.Commit();

            var actual = Model1.GetResource<LocalizedDocument>(_r1);

            Assert.AreEqual("Bericht", actual.Title["de"]);
            Assert.AreEqual("Report", actual.Title["en"]);
            CollectionAssert.AreEqual(new[] { "de", "en" }, actual.Title.Languages);
            CollectionAssert.AreEquivalent(
                new[] { "Jahresbericht", "Geschäftsbericht" }, actual.Keywords["de"]);

            // A string property beside the containers still sees untagged literals only.
            Assert.AreEqual("DOC-1", actual.Code);
            CollectionAssert.IsEmpty(actual.ListLanguages(new Property(new Uri("semio:test:documentCode"))));
        }

        /// <summary>
        /// A tag that no Trinity caller could write must still be readable when it is already there.
        /// </summary>
        /// <remarks>
        /// Every literal read from a store is constructed as a <c>LangString</c>, so tag validation sits
        /// directly on the read path. Capping subtags at eight characters (RFC 5646 well-formedness)
        /// therefore did not reject a bad write — it made <b>every</b> read of a resource carrying such
        /// a tag throw, the untyped <c>GetResource</c> included, for data that Turtle, SPARQL and every
        /// backend accept. One triple written by another tool took the whole resource out.
        /// </remarks>
        [Test]
        public virtual void ReadsALanguageTagLongerThanBcp47Allows()
        {
            const string tag = "en-abcdefghij";

            try
            {
                Model1.ExecuteUpdate(new SparqlUpdate(
                    $"INSERT DATA {{ GRAPH <{Model1.Uri.OriginalString}> {{ " +
                    $"<{_r1.OriginalString}> a <semio:test:LocalizedDocument> ; " +
                    $"<semio:test:documentTitle> \"lang\"@{tag} }}}}"));
            }
            catch (Exception e)
            {
                // Oxigraph enforces BCP-47 in its parser and rejects the tag outright.
                Assert.Inconclusive(
                    $"This store refuses to store '@{tag}' at all: {e.GetType().Name}. " +
                    "The read path cannot be exercised where the data cannot be written.");
            }

            // Backends disagree about whether such a literal can exist at all: Virtuoso, Jena and
            // RDF4J store and return it, Oxigraph rejects it in its parser. That disagreement is
            // itself the argument for the fix -- three of the four will hand Trinity a tag its own
            // constructor used to refuse -- and the reason the precondition is asked rather than
            // assumed. It is asked as a raw triple count because a mapped read is what is under test
            // and so cannot also be the precondition.
            int stored = Model1.ExecuteQuery(new SparqlQuery(
                    $"SELECT ?o WHERE {{ <{_r1.OriginalString}> <semio:test:documentTitle> ?o }}"))
                .GetBindings().Count();

            if (stored == 0)
            {
                Assert.Inconclusive(
                    $"This store accepted the update for '@{tag}' but stored nothing. " +
                    "The read path cannot be exercised where the data cannot be written.");
            }

            var typed = Model1.GetResource<LocalizedDocument>(_r1);

            // Reading must not throw, whatever the store did with the tag. That is the regression:
            // a BCP-47 length check on the read path made every read of this resource fail, the
            // untyped one included, which takes down anything touching the graph.
            Assert.DoesNotThrow(() => Model1.GetResource(_r1).ListValues().ToList());
            Assert.DoesNotThrow(() => Model1.AsSparqlQueryable<LocalizedDocument>().ToList());

            if (!typed.Title.Languages.Contains(tag))
            {
                Assert.Inconclusive(
                    $"This store accepted '@{tag}' but did not return it: got " +
                    $"[{string.Join(", ", typed.Title.Languages)}]. Reading still succeeds, which is " +
                    "what this test guards; the tag itself is the store's business.");
            }

            Assert.AreEqual("lang", typed.Title[tag]);
        }

        /// <summary>
        /// A LINQ query over a localized property honours the tag on a real store, region subtag and
        /// all, whatever case the store hands the tag back in.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the case the in-memory suite cannot see. Stores disagree about canonicalizing a
        /// language tag: Jena returns <c>de-DE</c> for one written as <c>de-de</c>, RDF4J returns it as
        /// written. A <c>LANG(?v) = "de-de"</c> comparison against a lower-cased constant therefore
        /// matches on some backends and not others, and every other localized test in this repository
        /// uses bare <c>de</c>/<c>en</c> tags, which have no case to disagree about. Both sides are
        /// lower-cased, and this runs on all four backends to prove it.
        /// </para>
        /// <para>
        /// The two-language conjunction is here for a different reason: it is the case that returned
        /// zero rows when both indexers shared one variable, and it costs nothing to check that the
        /// stores agree with the in-memory engine about it.
        /// </para>
        /// </remarks>
        [Test]
        public virtual void QueriesALocalizedPropertyByTagAcrossStores()
        {
            var r1 = Model1.CreateResource<LocalizedDocument>(_r1);
            r1.Title["de-AT"] = "Jahresbericht";
            r1.Title["de"] = "Bericht";
            r1.Title["en"] = "Report";
            r1.Commit();

            var other = Model1.CreateResource<LocalizedDocument>(_r2);
            other.Title["de"] = "Jahresbericht";
            other.Commit();

            // The region subtag matches however the caller cased it and however the store returns it.
            foreach (string tag in new[] { "de-AT", "de-at", "DE-AT" })
            {
                var austrian = Model1.AsSparqlQueryable<LocalizedDocument>()
                    .Where(d => d.Title[tag] == "Jahresbericht")
                    .ToList();

                Assert.AreEqual(1, austrian.Count, $"Tag '{tag}' should match the @de-AT title.");
                Assert.AreEqual(_r1, austrian[0].Uri);
            }

            // Exact match, not lookup: @de-AT is not reachable by asking for @de.
            var german = Model1.AsSparqlQueryable<LocalizedDocument>()
                .Where(d => d.Title["de"] == "Jahresbericht")
                .ToList();

            Assert.AreEqual(1, german.Count);
            Assert.AreEqual(_r2, german[0].Uri);

            // Two languages of one property in one predicate: each gets its own variable.
            var both = Model1.AsSparqlQueryable<LocalizedDocument>()
                .Where(d => d.Title["de"] == "Bericht" && d.Title["en"] == "Report")
                .ToList();

            Assert.AreEqual(1, both.Count);
            Assert.AreEqual(_r1, both[0].Uri);
        }

        /// <summary>
        /// A LINQ equality on a mapped string returns the same resources on every store, once each, now that
        /// it looks the resources up by the constant as well as filtering it (#64).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The lookup names the constant in both spellings, plain and <c>xsd:string</c>, because Virtuoso
        /// stores an untagged string either way depending on how it was written: <c>Commit()</c> stores a
        /// term only the plain constant matches, the replacing <c>Read</c> below one only the typed constant
        /// matches. Seeding only through Turtle hid that from the first version of this lookup, which is why
        /// one resource here is committed. The four RDF 1.1 stores treat the two spellings as one term, and
        /// a lookup that let both match returned every resource twice there; a duplicated row is a doubled
        /// value in a mapped collection, hence the <c>NameAdditional</c> count.
        /// </para>
        /// <para>
        /// In the last query the disjunct makes the member's binding optional. The lookup does not touch
        /// that variable, and the conjunct is still required, so the answer must not change.
        /// </para>
        /// </remarks>
        [Test]
        public virtual void QueriesAMappedStringByValueAcrossStores()
        {
            var plain = BaseUri.GetUriRef("plain");
            var typed = BaseUri.GetUriRef("typed");
            var tagged = BaseUri.GetUriRef("tagged");
            var other = BaseUri.GetUriRef("other");
            var committed = BaseUri.GetUriRef("committed");

            string Subject(UriRef uri) => SparqlSerializer.SerializeUri(uri) + $" a <{NCO.PersonContact}> ; <{NCO.nameGiven}> ";

            string turtle = string.Join("\n",
                Subject(plain) + $"\"x\" ; <{NCO.nameAdditional}> \"a\", \"b\" .",
                Subject(typed) + "\"x\"^^<http://www.w3.org/2001/XMLSchema#string> .",
                Subject(tagged) + "\"x\"@en .",
                Subject(other) + "\"y\" .");

            Assert.IsTrue(Model1.Read(turtle, RdfSerializationFormat.Turtle, false));

            var contact = Model1.CreateResource<PersonContact>(committed);
            contact.NameGiven = "x";
            contact.Commit();

            var x = Model1.AsSparqlQueryable<PersonContact>().Where(c => c.NameGiven == "x").ToList();

            CollectionAssert.AreEquivalent(new[] { plain, typed, committed }, x.Select(c => c.Uri));
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, x.Single(c => c.Uri == plain).NameAdditional);

            var either = Model1.AsSparqlQueryable<PersonContact>()
                .Where(c => c.NameGiven == "x" || c.NameGiven == "y")
                .ToList();

            CollectionAssert.AreEquivalent(new[] { plain, typed, committed, other }, either.Select(c => c.Uri));

            var optional = Model1.AsSparqlQueryable<PersonContact>()
                .Where(c => c.NameGiven == "x" && (c.NameGiven == "x" || c.NameGiven == "z"))
                .ToList();

            CollectionAssert.AreEquivalent(new[] { plain, typed, committed }, optional.Select(c => c.Uri));
        }

        /// <summary>
        /// A mapped <c>string</c> sees untagged literals only.
        /// </summary>
        /// <remarks>
        /// The successor to TestLocalizedStringPropertyMapping, which asserted that one property showed
        /// German, then English, then nothing, as Resource.Language was switched. That property is gone:
        /// a string is untagged by construction, and language-tagged values are read through a container
        /// (LocalizedContainersRoundTripEveryLanguage). Nothing is lost by the narrower mapped view --
        /// the tagged values are still on the resource and still visible untyped, which is what the
        /// second half asserts.
        /// </remarks>
        [Test]
        public virtual void MappedStringSeesUntaggedLiteralsOnly()
        {
            var r1 = Model1.CreateResource<StringMappingTestClass>(_r1);

            r1.AddProperty(to.uniqueStringTest, "Hallo Welt", "de");
            r1.AddProperty(to.uniqueStringTest, "Hello World", "en");

            Assert.IsNull(r1.uniqueStringTest, "Tagged values are not what a string property maps.");

            r1.AddProperty(to.uniqueStringTest, "plain");

            Assert.AreEqual("plain", r1.uniqueStringTest);

            // Open resources (ADR-0017): the mapped window narrowed, the data did not.
            var values = r1.ListValues(to.uniqueStringTest).ToList();

            Assert.AreEqual(3, values.Count);
            CollectionAssert.AreEquivalent(
                new[] { new LangString("Hallo Welt", "de"), new LangString("Hello World", "en") },
                values.OfType<LangString>().ToList());
            CollectionAssert.AreEqual(new[] { "de", "en" }, r1.ListLanguages(to.uniqueStringTest));
        }

        /// <summary>
        /// The successor to TestLocalizedStringInvariancy. There is no flag to set and no ambient
        /// language to opt out of: the declared type carries the guarantee.
        /// </summary>
        [Test]
        public virtual void MappedStringIsLanguageInvariantByConstruction()
        {
            var contact = Model1.CreateResource<PersonContact>(_r1);
            contact.NameGiven = "Peter";
            contact.Commit();

            var actual = Model1.GetResource<PersonContact>(_r1);

            Assert.AreEqual("Peter", actual.NameGiven);
            CollectionAssert.IsEmpty(actual.ListLanguages(), "A string property writes no language tag.");
        }

        /// <summary>
        /// A mapped <c>List&lt;string&gt;</c> sees untagged literals only -- the collection counterpart
        /// of the scalar case. Successor to the two TestLocalizedStringListPropertyMapping tests, which
        /// asserted counts as Resource.Language was switched between de, en and null.
        /// </summary>
        [Test]
        public virtual void MappedStringCollectionSeesUntaggedLiteralsOnly()
        {
            var r1 = Model1.CreateResource<StringMappingTestClass>(_r1);

            r1.AddProperty(to.stringTest, "Hallo Welt1", "de");
            r1.AddProperty(to.stringTest, "Hallo Welt2", "de");
            r1.AddProperty(to.stringTest, "Hello World1", "en");

            CollectionAssert.IsEmpty(r1.stringListTest);

            r1.AddProperty(to.stringTest, "plain1");
            r1.AddProperty(to.stringTest, "plain2");

            CollectionAssert.AreEquivalent(new[] { "plain1", "plain2" }, r1.stringListTest);
            Assert.AreEqual(5, r1.ListValues(to.stringTest).Count());
            CollectionAssert.AreEqual(new[] { "de", "en" }, r1.ListLanguages(to.stringTest));
        }

        /// <summary>
        /// The untyped read surface reports values, not tagged nulls.
        /// </summary>
        /// <remarks>
        /// Successor to the characterization test from step 2, which reproduced ADR-0048 defect 1 by
        /// asking this of a mapped property while a language was active. There is no active language
        /// now, so the same question is put to a container -- whose GetValueObject() is the container
        /// itself, and which would therefore report one unusable value if EnumerateValues were ever
        /// bypassed again.
        /// </remarks>
        [Test]
        public virtual void ListValuesReportsTheValuesOfAContainer()
        {
            var r1 = Model1.CreateResource<LocalizedMappingTestClass>(_r1);

            r1.Label["de"] = "Hallo Welt";
            r1.Label["en"] = "Hello World";
            r1.Label.Invariant = "plain";

            var values = r1.ListValues(to.uniqueLocalizedStringCultureTest).ToList();

            Assert.AreEqual(3, values.Count, "Two tagged literals and the untagged one, flattened.");
            CollectionAssert.AreEquivalent(
                new[] { new LangString("Hallo Welt", "de"), new LangString("Hello World", "en") },
                values.OfType<LangString>().ToList());
            CollectionAssert.AreEqual(new[] { "plain" }, values.OfType<string>().ToList());

            Assert.AreEqual(3, r1.ListValues().Count(x => Equals(x.Item1, to.uniqueLocalizedStringCultureTest)),
                "The serialization path flattens the container the same way.");
        }

        [Test]
        public virtual void TestJsonSerialization()
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
