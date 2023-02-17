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
using System.Linq;
using System;
using System.Globalization;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class ResourceTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members
        
        protected UriRef R1;

        protected Property P1;
        
        #endregion
        
        #region Methods

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            R1 = BaseUri.GetUriRef("r1");
            
            P1 = new Property(BaseUri.GetUriRef("p1"));
        }

        private void Test<TValue>(AddPropertyDelegate addProperty, ValidateValueDelegate<TValue> validate)
        {
            var r1 = Model1.CreateResource<Resource>(R1);
            addProperty(r1);
            r1.Commit();
            
            var actual = Model1.GetResource<Resource>(R1);
            var result = (TValue)actual.ListValues(P1).First();
            
            validate(result);
        }
        
        [Test]
        public virtual void BoolTest()
        {
            const bool value = true;

            Test<bool>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void IntTest()
        {
            const int value = 123;

            Test<int>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void Int16Test()
        {
            const short value = 124;
            
            Test<short>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void Int64Test()
        {
            const long value = 126;
            
            Test<long>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void UintTest()
        {
            const uint value = 126;
            
            Test<uint>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void Uint16Test()
        {
            const ushort value = 126;
            
            Test<ushort>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void Uint64Test()
        {
            const ulong value = 126;
            
            Test<ulong>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void FloatTest()
        {
            const float value = 1.234F;
            
            Test<float>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void DoubleTest()
        {
            const double value = 1.223;
            
            Test<double>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void StringTest()
        {
            const string value = "Hello World!";
            
            Test<string>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }

        [Test]
        public virtual void StringLocalizedTest()
        {
            const string value = "Hello World!";
            var culture = CultureInfo.CreateSpecificCulture("en");
            
            Test<Tuple<string, string>>((r1) =>
                {
                    r1.AddProperty(P1, value, culture);
                },
                (result) =>
                {
                    Assert.AreEqual(value, result.Item1);
                    Assert.AreEqual(culture.Name.ToLower(), result.Item2.ToLower());
                });
        }

        [Test]
        public virtual void DateTimeTest()
        {
            var value = DateTime.Today;
            
            Test<DateTime>((r1) => r1.AddProperty(P1, value),
                (result) =>
                {
                    Assert.AreEqual(value.ToUniversalTime(), result.ToUniversalTime());
                });
        }

        [Test]
        public virtual void TimeSpanTest()
        {
            var value = TimeSpan.FromMinutes(5);
            
            Test<TimeSpan>((r1) => r1.AddProperty(P1, value),
                (result) =>
                {
                    Assert.AreEqual(value.TotalMinutes, result.TotalMinutes);
                });
        }

        [Test]
        public virtual void ByteArrayTest()
        {
            var value = new byte[] { 1, 2, 3, 4, 5 };
            
            Test<byte[]>((r1) => r1.AddProperty(P1, value),
                (result) => Assert.AreEqual(value, result));
        }
        
        #endregion
    }
    
    public delegate void AddPropertyDelegate(IResource resource);
    
    public delegate void ValidateValueDelegate<in TValue>(TValue actual);
}
