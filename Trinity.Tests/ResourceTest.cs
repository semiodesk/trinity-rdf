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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Xml;
using System.Globalization;
using Semiodesk.Trinity;
using System.Collections.ObjectModel;
using Semiodesk.Trinity.Ontologies;
using System.Reflection;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif

// Notizen:
// - Mapping von resource listen sollten nur einen Itemsprovider zurückliefern, welcher auch virtualisiert genutzt werden kann
// - Probleme bestehen im Augenblick:
//   - Überschreiben von gemappten listen
//   - Hinzufügen von un-commiteten objecten darf nicht funktionieren
//   - Beim ändern von gemappten resource listen, wird der komplette Content abgefragt, obwohl vllt. nur ein element hinzugefügt werden muss.

namespace Semiodesk.Trinity.Test
{
    public class TestOntology
    {
        public static readonly Uri Namespace = new Uri("semio:test");
        public Uri GetNamespace() { return Namespace; }
        public static readonly string Prefix = "test";
        public string GetPrefix() { return Prefix; }

        public static readonly Class SingleMappingTestClass = new Class(new Uri(SingleMappingTestClassString));
        public const string SingleMappingTestClassString = "semio:test:SingleMappingTestClass";
        public static readonly Class SingleResourceMappingTestClass = new Class(new Uri("semio:test:SingleResourceMappingTestClass"));

        public const string SubMappingTestClassString = "semio:test:SubMappingTestClass";
        public static readonly Class SubMappingTestClass = new Class(new Uri(SubMappingTestClassString));

        public const string TestClassString = "semio:test:TestClass";
        public static readonly Class TestClass = new Class(new Uri(TestClassString));
        public static readonly Class TestClass2 = new Class(new Uri("semio:test:TestClass2"));
        public static readonly Class TestClass3 = new Class(new Uri("semio:test:TestClass3"));
        public static readonly Class TestClass4 = new Class(new Uri("semio:test:TestClass4"));

        public const string genericTestString = "semio:test:genericTest";
        public static readonly Property genericTest = new Property(new Uri(genericTestString));

        public const string intTestString = "semio:test:intTest";
        public static readonly Property intTest = new Property(new Uri(intTestString));
        public const string uniqueIntTestString = "semio:test:uniqueIntTest";
        public static readonly Property uniqueIntTest = new Property(new Uri(uniqueIntTestString));

        public const string uintTestString ="semio:test:uintTest";
        public static readonly Property uintTest = new Property(new Uri(uintTestString));
        public const string uniqueUintTestString = "semio:test:uniqueUintTest";
        public static readonly Property uniqueUintTest = new Property(new Uri(uniqueUintTestString));

        public const string stringTestString = "semio:test:stringTest";
        public static readonly Property stringTest = new Property(new Uri(stringTestString));
        public const  string uniqueStringTestString = "semio:test:uniqueStringTest";
        public static readonly Property uniqueStringTest = new Property(new Uri(uniqueStringTestString));

        public static readonly Property floatTest = new Property(new Uri("semio:test:floatTest"));
        public static readonly Property uniqueFloatTest = new Property(new Uri("semio:test:uniqueFloatTest"));

        public static readonly Property doubleTest = new Property(new Uri("semio:test:doubleTest"));
        public static readonly Property uniqueDoubleTest = new Property(new Uri("semio:test:uniqueDoubleTest"));

        public static readonly Property decimalTest = new Property(new Uri("semio:test:decimalTest"));
        public static readonly Property uniqueDecimalTest = new Property(new Uri("semio:test:uniqueDecimalTest"));

        public static readonly Property boolTest = new Property(new Uri("semio:test:boolTest"));
        public static readonly Property uniqueBoolTest = new Property(new Uri("semio:test:uniqueBoolTest"));

        public static readonly Property datetimeTest = new Property(new Uri("semio:test:datetimeTest"));
        public static readonly Property uniqueDatetimeTest = new Property(new Uri("semio:test:uniqueDatetimeTest"));

        public static readonly Property resourceTest = new Property(new Uri("semio:test:resourceTest"));
        public static readonly Property uniqueResourceTest = new Property(new Uri("semio:test:uniqueResourceTest"));
        public static readonly Property uriTest = new Property(new Uri("semio:test:uriTest"));
    }

    




    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class ResourceTest
    {
        [Test]
        public void Equal()
        {
            Resource t1 = new Resource(new Uri("http://test.com"));
            Resource t1a = new Resource(new Uri("http://test.com"));
            Uri u1 = new Uri("http://test.com");
            Resource t2 = new Resource(new Uri("http://test.com#frag"));
            Uri u2 = new Uri("http://test.com#frag");
            Resource t3 = new Resource(new Uri("http://test.com#frag2"));
            Uri u3 = new Uri("http://test.com#frag2");

            Assert.AreEqual(t1, t1a);
            Assert.AreNotEqual(t1, t2);
            //Assert.AreNotEqual(u1, u2);
            Assert.AreNotEqual(t1, t3);
            //Assert.AreNotEqual(u1, u3);
            Assert.AreNotEqual(t2, t3);
            //Assert.AreNotEqual(u2, u3);
        }

        [Test]
        public void Property()
        {
            

            Property myProperty = new Property(new Uri("ex:myProperty"));
            Property myPropertyCopy = new Property(new Uri("ex:myProperty"));
            Resource t1 = new Resource(new Uri("ex:myResource"));
            Resource t2 = new Resource(new Uri("ex:mySecondResource"));
            string sValue = "test";
            int iValue = 123;
            int iNegValue = -123;
            float fValue = (float)2.0234;
            float fNegValue = (float)-2.123;
            double dValue = 3.123;
            double dNegValue = -4.5234;
            DateTime dtValue = new DateTime(2010, 1, 1);
            bool bValue = true;

            Assert.IsFalse(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, sValue));
            try
            {
                t1.AddProperty(myProperty, sValue);
            }
            catch
            {
                Assert.Fail("Exception was raised during adding of property.");
            }
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsTrue(t1.HasProperty(myProperty, sValue));

            Assert.AreEqual(sValue, t1.ListValues(myProperty).First());
            Assert.AreEqual(t1.ListValues(myProperty).First(), t1.ListValues(myPropertyCopy).First());

            Assert.IsFalse(t1.HasProperty(myProperty, iValue));
            try
            {
                t1.AddProperty(myProperty, iValue);
            }
            catch
            {
                Assert.Fail("Exception was raised during adding of property.");
            }
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsTrue(t1.HasProperty(myProperty, iValue));
            
            Assert.IsTrue(t1.ListValues(myProperty).Contains(iValue));

            Assert.IsFalse(t1.HasProperty(myProperty, t2));
            t1.AddProperty(myProperty, t2);
            Assert.IsTrue(t1.HasProperty(myProperty, t2));

            Assert.IsFalse(t1.HasProperty(myProperty, iNegValue));
            t1.AddProperty(myProperty, iNegValue);
            Assert.IsTrue(t1.HasProperty(myProperty, iNegValue));

            Assert.IsFalse(t1.HasProperty(myProperty, fValue));
            t1.AddProperty(myProperty, fValue);
            Assert.IsTrue(t1.HasProperty(myProperty, fValue));

            Assert.IsFalse(t1.HasProperty(myProperty, fNegValue));
            t1.AddProperty(myProperty, fNegValue);
            Assert.IsTrue(t1.HasProperty(myProperty, fNegValue));

            Assert.IsFalse(t1.HasProperty(myProperty, dValue));
            t1.AddProperty(myProperty, dValue);
            Assert.IsTrue(t1.HasProperty(myProperty, dValue));

            Assert.IsFalse(t1.HasProperty(myProperty, dNegValue));
            t1.AddProperty(myProperty, dNegValue);
            Assert.IsTrue(t1.HasProperty(myProperty, dNegValue));

            Assert.IsFalse(t1.HasProperty(myProperty, dtValue));
            t1.AddProperty(myProperty, dtValue);
            Assert.IsTrue(t1.HasProperty(myProperty, dtValue));

            Assert.IsFalse(t1.HasProperty(myProperty, bValue));
            t1.AddProperty(myProperty, bValue);
            Assert.IsTrue(t1.HasProperty(myProperty, bValue));

            t1.RemoveProperty(myProperty, t2);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, t2));

            t1.RemoveProperty(myProperty, sValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, sValue));

            t1.RemoveProperty(myProperty, iValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, iValue));

            t1.RemoveProperty(myProperty, iNegValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, iNegValue));

            t1.RemoveProperty(myProperty, fValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, fValue));

            t1.RemoveProperty(myProperty, fNegValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, fNegValue));

            t1.RemoveProperty(myProperty, dValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, dValue));

            t1.RemoveProperty(myProperty, dNegValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, dNegValue));

            t1.RemoveProperty(myProperty, dtValue);
            Assert.IsTrue(t1.HasProperty(myProperty));
            Assert.IsFalse(t1.HasProperty(myProperty, dtValue));

            t1.RemoveProperty(myProperty, bValue);
            Assert.IsFalse(t1.HasProperty(myProperty, bValue));
            Assert.IsFalse(t1.HasProperty(myProperty));
        }


        #region Datatype fidelity Test

        [Test]
        public void TestBool()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = new Resource(new Uri("ex:myResource"));

            bool val = true;
            r1.AddProperty(myProperty, val);

            object res = r1.ListValues(myProperty).First();

            Assert.AreEqual(res.GetType(), typeof(bool));
            Assert.AreEqual((bool)res, val);
        }

        [Test]
        public void TestInt()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r1 = new Resource(new Uri("ex:myResource"));


            int val1 = 123;
            r1.AddProperty(myProperty, val1);
            object res1 = r1.ListValues(myProperty).First();
            Assert.AreEqual(val1.GetType(), res1.GetType());
            Assert.AreEqual(res1, val1);
            r1.RemoveProperty(myProperty, val1);
        }

        [Test]
        public void TestInt16()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            Int16 val = 124;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);

        }

        [Test]
        public void TestInt32()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            Int32 val = 125;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);

        }

        [Test]
        public void TestInt64()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            Int64 val = 126;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestUint()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            uint val = 126;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestUint16()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            UInt16 val = 126;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestUint32()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            UInt32 val = 126;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestUint64()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            UInt32 val = 126;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestFloat()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            float val = 1.234F;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestDouble()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            double val = 1.223;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestSingle()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            Single val = 1.223F;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestString()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            string val = "Hello World!";
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }


        [Test]
        public void TestLocalizedString()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            string val = "Hello World!";
            var ci = CultureInfo.CreateSpecificCulture("EN");
            r.AddProperty(myProperty, val, ci);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(typeof(Tuple<string, string>), res.GetType());
            Tuple<string, string> v = res as Tuple<string, string>;
            Assert.AreEqual(val, v.Item1);
            Assert.AreEqual(ci.Name.ToLower(), v.Item2.ToLower());
            r.RemoveProperty(myProperty, val, ci);
        }

        [Test]
        public void TestDateTime()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));
            DateTime val = DateTime.Today;
            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);
        }

        [Test]
        public void TestByteArray()
        {
            Property myProperty = new Property(new Uri("ex:myProperty"));
            Resource r = new Resource(new Uri("ex:myResource"));

            byte[] val = new byte[] { 1, 2, 3, 4, 5 };

            r.AddProperty(myProperty, val);
            object res = r.ListValues(myProperty).First();
            Assert.AreEqual(val.GetType(), res.GetType());
            Assert.AreEqual(val, res);
            r.RemoveProperty(myProperty, val);

        }


        #endregion

        #region List*() Tests

        /// <summary>
        ///Ein Test für "ListValues"
        ///</summary>
        [Test()]
        public void ListValuesTest()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";

            Resource target = new Resource(new Uri(baseUri, relativeUri));
            string b = target.Uri.Fragment;

            Property property = new Property(new Uri(baseUri, "#related"));
            List<object> list = new List<object>();

            int v1 = 12;
            list.Add(v1);
            target.AddProperty(property, v1);

            string v2 = "Hello World";
            target.AddProperty(property, v2);
            list.Add(v2);

            v2 = "All your base are belong to us!";
            target.AddProperty(property, v2);
            list.Add(v2);

            float v3 = 0.234F;
            target.AddProperty(property, v3);
            list.Add(v3);

            DateTime v4 = new DateTime(1292, 1, 1);
            target.AddProperty(property, v4);
            list.Add(v4);

            Tuple<string, CultureInfo> v5 = new Tuple<string, CultureInfo>("Hallo Welt!", CultureInfo.GetCultureInfo("DE"));
            target.AddProperty(property, v5.Item1, v5.Item2);
            list.Add(v5);

            IResource v6 = new Resource(new Uri(baseUri, "#mySecondResource"));
            target.AddProperty(property, v6);
            list.Add(v6);

            double v7 = 0.123;
            target.AddProperty(property, v7);
            list.Add(v7);

            bool v8 = true;
            target.AddProperty(property, v8);
            list.Add(v8);


            IEnumerable<object> expected = list;

            IEnumerable<object> actual = target.ListValues(property);
            foreach (object obj in actual)
            {
                if (obj.GetType() == typeof(string[]))
                {
                    Tuple<string, CultureInfo> tmp = (Tuple<string, CultureInfo>)obj;
                    Assert.AreEqual(v5, tmp);

                }
                else
                {
                    Assert.AreEqual(true, expected.Contains(obj), string.Format("Object {0} not in expected list.", obj));
                }
            }


        }

        /// <summary>
        ///Ein Test für "ListProperties"
        ///</summary>
        [Test()]
        public void ListPropertiesTest()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property1 = new Property(new Uri(baseUri, "#related"));
            bool v1 = true;
            Property property2 = new Property(new Uri(baseUri, "#related2"));
            bool v2 = false;
            Property property3 = new Property(new Uri(baseUri, "#related3"));
            bool v3 = true;
            List<Property> expected = new List<Property> { property1, property2, property3 };
            target.AddProperty(property1, v1);
            target.AddProperty(property2, v2);
            target.AddProperty(property3, v3);
            IEnumerable<Property> actual = target.ListProperties();
            foreach (Property prop in actual)
            {
                Assert.AreEqual(true, expected.Contains(prop));
            }

            target.RemoveProperty(property1, v1);
            expected.Remove(property1);
            actual = target.ListProperties();
            foreach (Property prop in actual)
            {
                Assert.AreEqual(true, expected.Contains(prop));
            }

            target.RemoveProperty(property2, v2);
            expected.Remove(property2);
            actual = target.ListProperties();
            foreach (Property prop in actual)
            {
                Assert.AreEqual(true, expected.Contains(prop));
            }


            target.RemoveProperty(property3, v3);
            expected.Remove(property3);
            actual = target.ListProperties();
            Assert.AreEqual(actual.Count(), 0);




        }

        #endregion

        #region HasProperty() Tests

        /// <summary>
        ///Ein Test für "HasProperty"
        ///</summary>
        [Test()]
        public void HasPropertyTest2()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri)); // TODO: Passenden Wert initialisieren
            Property property = new Property(new Uri(baseUri, "#related"));
            string value1 = "Hallo Welt";
            CultureInfo lang1 = CultureInfo.GetCultureInfo("DE");
            string value2 = "Hello World";
            CultureInfo lang2 = CultureInfo.GetCultureInfo("en-US");
            string value3 = "Hello";

            Assert.AreEqual(false, target.HasProperty(property, value1, lang1));
            target.AddProperty(property, value1, lang1);
            // Current interpretation -> Value+Language != Value
            Assert.AreEqual(true, target.HasProperty(property, value1, lang1));
            Assert.AreEqual(false, target.HasProperty(property, value1));
            Assert.AreEqual(false, target.HasProperty(property, value2, lang2));
            Assert.AreEqual(false, target.HasProperty(property, value3));

            target.AddProperty(property, value2, lang2);
            Assert.AreEqual(true, target.HasProperty(property, value1, lang1));
            Assert.AreEqual(true, target.HasProperty(property, value2, lang2));
            Assert.AreEqual(false, target.HasProperty(property, value3));

            target.AddProperty(property, value3);
            Assert.AreEqual(true, target.HasProperty(property, value1, lang1));
            Assert.AreEqual(true, target.HasProperty(property, value2, lang2));
            Assert.AreEqual(true, target.HasProperty(property, value3));


            target.RemoveProperty(property, value3);
            Assert.AreEqual(true, target.HasProperty(property, value1, lang1));
            Assert.AreEqual(true, target.HasProperty(property, value2, lang2));
            Assert.AreEqual(false, target.HasProperty(property, value3));

            target.RemoveProperty(property, value2, lang2);
            Assert.AreEqual(true, target.HasProperty(property, value1, lang1));
            Assert.AreEqual(false, target.HasProperty(property, value2, lang2));
            Assert.AreEqual(false, target.HasProperty(property, value2));
        }


        /// <summary>
        ///Ein Test für "HasProperty"
        ///</summary>
        [Test()]
        public void HasPropertyTest1()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            int value1 = 1;
            int value2 = 2;

            Assert.AreEqual(false, target.HasProperty(property, value1));
            target.AddProperty(property, value1);
            Assert.AreEqual(true, target.HasProperty(property, value1));
            Assert.AreEqual(false, target.HasProperty(property, value2));
            target.AddProperty(property, value2);
            Assert.AreEqual(true, target.HasProperty(property, value1));
            Assert.AreEqual(true, target.HasProperty(property, value2));

            target.RemoveProperty(property, value1);
            Assert.AreEqual(false, target.HasProperty(property, value1));
            Assert.AreEqual(true, target.HasProperty(property, value2));
            target.RemoveProperty(property, value2);
            Assert.AreEqual(false, target.HasProperty(property, value1));
            Assert.AreEqual(false, target.HasProperty(property, value2));


        }

        /// <summary>
        ///Ein Test für "HasProperty"
        ///</summary>
        [Test()]
        public void HasPropertyTest()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            int value1 = 1;
            int value2 = 2;

            Assert.AreEqual(false, target.HasProperty(property));
            target.AddProperty(property, value1);
            Assert.AreEqual(true, target.HasProperty(property));
            target.AddProperty(property, value2);
            Assert.AreEqual(true, target.HasProperty(property));

            target.RemoveProperty(property, value1);
            target.RemoveProperty(property, value2);

            Assert.AreEqual(false, target.HasProperty(property));

        }

        #endregion

        #region Get*() Tests

        /// <summary>
        ///Ein Test für "GetUri"
        ///</summary>
        [Test()]
        public void GetUriTest()
        {
            Uri baseUri = new Uri("http://example.com");
            Resource target = new Resource(new Uri(baseUri, "test"));
            Uri expected = new Uri("http://example.com/test");
            Uri actual = target.Uri;
            Assert.AreEqual(expected, actual);

            baseUri = new Uri("http://example.com/test");
            target = new Resource(new Uri(baseUri, "#Fragment"));
            expected = new Uri("http://example.com/test#Fragment");
            actual = target.Uri;
            Assert.AreEqual(expected, actual);

        }
        #endregion

        #region AddProperty() Tests

        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest8()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            string value = "Hallo Welt!";
            CultureInfo language = CultureInfo.GetCultureInfo("DE");
            target.AddProperty(property, value, language);

            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(typeof(Tuple<string, CultureInfo>), target.ListValues(property).First().GetType());
            Tuple<string, CultureInfo> res = (Tuple<string, CultureInfo>)target.ListValues(property).First();
            Assert.AreEqual(value, res.Item1);
            Assert.AreEqual(language, res.Item2);
            Assert.AreEqual(value.GetType(), res.Item1.GetType());
            Assert.AreEqual(language.GetType(), res.Item2.GetType());
        }

        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest7()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            int value = 17;
            target.AddProperty(property, value);
            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(value, target.ListValues(property).First());
            Assert.AreEqual(value.GetType(), target.ListValues(property).First().GetType());
        }

        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest6()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            IResource value = new Resource(new Uri(baseUri, "#mySecondResource"));
            target.AddProperty(property, value);
            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(value, target.ListValues(property).First());
            Assert.AreEqual(value.GetType(), target.ListValues(property).First().GetType());
        }

        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest5()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            string value = "All your base are belong to us!";
            target.AddProperty(property, value);
            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(value, target.ListValues(property).First());
            Assert.AreEqual(value.GetType(), target.ListValues(property).First().GetType());
        }

        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest4()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            float value = 21.345F;
            target.AddProperty(property, value);
            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(value, target.ListValues(property).First());
            Assert.AreEqual(value.GetType(), target.ListValues(property).First().GetType());
        }

        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest3()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            DateTime value = new DateTime(2010, 3, 17);
            target.AddProperty(property, value);
            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(value, target.ListValues(property).First());
            Assert.AreEqual(value.GetType(), target.ListValues(property).First().GetType());
        }


        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest1()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            double value = 0.234F;
            target.AddProperty(property, value);
            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(value, target.ListValues(property).First());
            Assert.AreEqual(value.GetType(), target.ListValues(property).First().GetType());
        }

        /// <summary>
        ///Ein Test für "AddProperty"
        ///</summary>
        [Test()]
        public void AddPropertyTest()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            bool value = false;
            target.AddProperty(property, value);
            Assert.IsTrue(target.HasProperty(property));
            Assert.AreEqual(value, target.ListValues(property).First());
            Assert.AreEqual(value.GetType(), target.ListValues(property).First().GetType());
        }

        #endregion

        #region Constructor Tests
        /// <summary>
        ///Ein Test für "Resource-Konstruktor"
        ///</summary>
        [Test()]
        public void ResourceConstructorTest()
        {

            Uri uri = new Uri("http://example.com/ex");

            Resource t1 = new Resource(uri);
            Resource t2 = new Resource(uri);
            bool a = t1.Equals(t2);
            Assert.AreEqual(t1, t2);
            Assert.AreEqual(uri, t1.Uri.ToString());

            Uri ns = new Uri("http://example.com/");
            Resource res = new Resource(new Uri(ns, "John"));
            Assert.AreEqual(res.Uri, new Uri("http://example.com/John"));


            uri = new Uri("http://example.com/ex#fragment");

            t1 = new Resource(uri);
            t2 = new Resource(uri);
            a = t1.Equals(t2);
            Assert.AreEqual(t1, t2);
            Assert.AreEqual(uri, t1.Uri.ToString());

            //Assert.AreNotEqual(t1.Uri, new Uri("http://example.com/ex"));
            //Assert.AreNotEqual(t1, new Uri("http://example.com/ex"));
            Assert.AreNotEqual(t1, new Resource(new Uri("http://example.com/ex")));



        }
        #endregion

        #region RemoveProperty() Tests

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest7()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            string value = "Hello";
            CultureInfo language = CultureInfo.GetCultureInfo("en-US");
            target.AddProperty(property, value, language);
            Assert.AreEqual(true, target.HasProperty(property, value, language));
            target.RemoveProperty(property, value, language);
            Assert.AreEqual(false, target.HasProperty(property, value, language));
            Assert.AreEqual(false, target.HasProperty(property));
        }

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest6()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            int value = 12;
            target.AddProperty(property, value);
            Assert.AreEqual(true, target.HasProperty(property, value));
            target.RemoveProperty(property, value);
            Assert.AreEqual(false, target.HasProperty(property, value));
            Assert.AreEqual(false, target.HasProperty(property));

        }

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest5()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            string value = "Cheeseburgers are nice.";
            target.AddProperty(property, value);
            Assert.AreEqual(true, target.HasProperty(property, value));
            target.RemoveProperty(property, value);
            Assert.AreEqual(false, target.HasProperty(property, value));
            Assert.AreEqual(false, target.HasProperty(property));

        }

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest4()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            IResource value = new Resource(new Uri(baseUri, "#mySecondResource"));
            target.AddProperty(property, value);
            Assert.AreEqual(true, target.HasProperty(property, value));
            target.RemoveProperty(property, value);
            Assert.AreEqual(false, target.HasProperty(property, value));
            Assert.AreEqual(false, target.HasProperty(property));
        }

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest3()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            bool value = false;
            target.AddProperty(property, value);
            Assert.AreEqual(true, target.HasProperty(property, value));
            target.RemoveProperty(property, value);
            Assert.AreEqual(false, target.HasProperty(property, value));
            Assert.AreEqual(false, target.HasProperty(property));
        }

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest2()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            DateTime value = new DateTime(1999, 1, 3);
            target.RemoveProperty(property, value);
            target.AddProperty(property, value);
            Assert.AreEqual(true, target.HasProperty(property, value));
            target.RemoveProperty(property, value);
            Assert.AreEqual(false, target.HasProperty(property, value));
            Assert.AreEqual(false, target.HasProperty(property));
        }

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest1()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            float value = 0.211F;
            target.AddProperty(property, value);
            Assert.AreEqual(true, target.HasProperty(property, value));
            target.RemoveProperty(property, value);
            Assert.AreEqual(false, target.HasProperty(property, value));
            Assert.AreEqual(false, target.HasProperty(property));
        }

        /// <summary>
        ///Ein Test für "RemoveProperty"
        ///</summary>
        [Test()]
        public void RemovePropertyTest()
        {
            Uri baseUri = new Uri("http://example.com/test");
            string relativeUri = "#myResource";
            Resource target = new Resource(new Uri(baseUri, relativeUri));
            Property property = new Property(new Uri(baseUri, "#related"));
            double value = 0.12345632F; // TODO: Passenden Wert initialisieren
            target.AddProperty(property, value);
            Assert.AreEqual(true, target.HasProperty(property, value));
            target.RemoveProperty(property, value);
            Assert.AreEqual(false, target.HasProperty(property, value));
            Assert.AreEqual(false, target.HasProperty(property));
        }



        #endregion

  
    }



}
