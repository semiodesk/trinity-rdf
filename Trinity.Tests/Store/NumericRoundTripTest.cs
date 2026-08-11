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
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Round-trips numeric values through a real store and asserts both the value and the CLR type that
    /// comes back.
    /// </summary>
    /// <remarks>
    /// Generic on purpose: one definition runs against dotNetRDF, Virtuoso and GraphDB, which makes the
    /// per-store type-fidelity differences visible in the suite rather than in a customer's logs. That
    /// divergence is how the original defect survived — a whole-number <c>decimal?</c> was fine in-memory
    /// and threw against Virtuoso, because dotNetRDF preserves <c>xsd:decimal</c> while Virtuoso hands the
    /// value back in an integer box.
    ///
    /// The store is entitled to do that: <c>"400"</c> is a valid lexical form for <c>xsd:decimal</c> and its
    /// value space contains the integers. These tests therefore assert what the *mapping* must tolerate,
    /// not what the store must return.
    /// </remarks>
    [TestFixture]
    public abstract class NumericRoundTripTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Methods

        /// <summary>
        /// The reported case. A whole-number decimal is written, and reading it back must produce the value
        /// whatever numeric type the store chose to store it as.
        /// </summary>
        [Test]
        public void WholeNumberDecimalRoundTrips()
        {
            var uri = BaseUri.GetUriRef("numeric-decimal");

            var thing = Model1.CreateResource<NumericThing>(uri);
            thing.NullableDecimal = 400m;
            thing.Commit();

            var reloaded = Model1.GetResource<NumericThing>(uri);

            Assert.IsTrue(reloaded.NullableDecimal.HasValue,
                "The value must land in the mapped property, not in the unmapped bag. " +
                "Raw marshalled type was: " + RawType(reloaded, "nullableDecimal"));
            Assert.AreEqual(400m, reloaded.NullableDecimal.Value);
        }

        /// <summary>
        /// A fractional decimal is the control: no store canonicalizes it to an integer, so this passes
        /// everywhere and isolates whole numbers as the trigger.
        /// </summary>
        [Test]
        public void FractionalDecimalRoundTrips()
        {
            var uri = BaseUri.GetUriRef("numeric-fraction");

            var thing = Model1.CreateResource<NumericThing>(uri);
            thing.NullableDecimal = 400.5m;
            thing.Commit();

            var reloaded = Model1.GetResource<NumericThing>(uri);

            Assert.IsTrue(reloaded.NullableDecimal.HasValue);
            Assert.AreEqual(400.5m, reloaded.NullableDecimal.Value);
        }

        /// <summary>
        /// The rest of the nullable numeric family, which shares the conversion path.
        /// </summary>
        [Test]
        public void NullableNumericFamilyRoundTrips()
        {
            var uri = BaseUri.GetUriRef("numeric-family");

            var thing = Model1.CreateResource<NumericThing>(uri);
            thing.NullableInt = 400;
            thing.NullableLong = 400L;
            thing.NullableDouble = 400d;
            thing.NullableFloat = 400f;
            thing.NullableShort = 400;
            thing.Commit();

            var reloaded = Model1.GetResource<NumericThing>(uri);

            Assert.AreEqual(400, reloaded.NullableInt, "int?");
            Assert.AreEqual(400L, reloaded.NullableLong, "long?");
            Assert.AreEqual(400d, reloaded.NullableDouble, "double?");
            Assert.AreEqual(400f, reloaded.NullableFloat, "float?");
            Assert.AreEqual((short)400, reloaded.NullableShort, "short?");
        }

        /// <summary>
        /// The collection branch converts independently of the scalar one.
        /// </summary>
        [Test]
        public void WholeNumberDecimalCollectionRoundTrips()
        {
            var uri = BaseUri.GetUriRef("numeric-list");

            var thing = Model1.CreateResource<NumericThing>(uri);
            thing.DecimalList.Add(400m);
            thing.Commit();

            var reloaded = Model1.GetResource<NumericThing>(uri);

            Assert.AreEqual(1, reloaded.DecimalList.Count,
                "The collection value must survive the round trip. Raw marshalled type was: " +
                RawType(reloaded, "decimalList"));
            Assert.AreEqual(400m, reloaded.DecimalList.First());
        }

        #endregion

        #region Helpers

        /// <summary>
        /// The CLR type the store actually handed back, for failure messages — this is what differs between
        /// backends and what the matrix in the investigation records.
        /// </summary>
        private static string RawType(Resource resource, string localName)
        {
            var property = new Property(new Uri("http://example.org/numeric/" + localName));
            var value = resource.ListValues(property).FirstOrDefault();

            return value == null ? "<nothing>" : value.GetType().Name;
        }

        #endregion
    }
}
