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
using System.Collections.Generic;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests
{
    /// <summary>
    /// A mapped resource with nullable numeric properties. The suite had none before this fixture, which
    /// is why numeric hydration into a nullable property was never exercised.
    /// </summary>
    [RdfClass("http://example.org/numeric/Thing")]
    public partial class NumericThing : Resource
    {
        public NumericThing(Uri uri) : base(uri) { }

        [RdfProperty("http://example.org/numeric/nullableDecimal")]
        public partial decimal? NullableDecimal { get; set; }

        [RdfProperty("http://example.org/numeric/decimalValue")]
        public partial decimal DecimalValue { get; set; }

        [RdfProperty("http://example.org/numeric/nullableInt")]
        public partial int? NullableInt { get; set; }

        [RdfProperty("http://example.org/numeric/nullableLong")]
        public partial long? NullableLong { get; set; }

        [RdfProperty("http://example.org/numeric/nullableDouble")]
        public partial double? NullableDouble { get; set; }

        [RdfProperty("http://example.org/numeric/nullableFloat")]
        public partial float? NullableFloat { get; set; }

        [RdfProperty("http://example.org/numeric/intValue")]
        public partial int IntValue { get; set; }

        [RdfProperty("http://example.org/numeric/nullableShort")]
        public partial short? NullableShort { get; set; }

        [RdfProperty("http://example.org/numeric/decimalList")]
        public partial List<decimal> DecimalList { get; set; }
    }

    /// <summary>
    /// Covers what happens when a store hands back a numeric literal whose CLR type is not exactly the
    /// mapped property's type.
    /// </summary>
    /// <remarks>
    /// Deliberately store-free: it drives <see cref="Resource.AddPropertyToMapping"/> directly, which is
    /// the path a reload takes. That reproduces the defect with no container, so the default CI gate
    /// covers it — the original escaped precisely because it only showed up against a real store.
    ///
    /// The trigger is a store canonicalizing a whole-number `xsd:decimal` into an integer box, which it is
    /// entitled to do: "400" is a valid lexical form for xsd:decimal and the value space contains the
    /// integers. `XsdTypeMapper` then deserializes `xsd:integer` to `Int32`.
    /// </remarks>
    [TestFixture]
    public class NumericHydrationTest
    {
        #region Members

        private static readonly Uri Subject = new Uri("http://example.org/numeric/a");

        #endregion

        #region Methods

        /// <summary>
        /// The reported case: Virtuoso returns Int32 for a whole-number decimal, and it has to land in a
        /// mapped <c>decimal?</c>.
        /// </summary>
        [Test]
        public void Int32LandsInANullableDecimal()
        {
            var thing = new NumericThing(Subject);

            thing.AddPropertyToMapping(Property("nullableDecimal"), 400, fromModel: true);

            Assert.AreEqual(400m, thing.NullableDecimal);
        }

        /// <summary>
        /// The control: the same value into a non-nullable decimal. If this passes while the nullable case
        /// fails, nullability is the trigger rather than the decimal type.
        /// </summary>
        [Test]
        public void Int32LandsInANonNullableDecimal()
        {
            var thing = new NumericThing(Subject);

            thing.AddPropertyToMapping(Property("decimalValue"), 400, fromModel: true);

            Assert.AreEqual(400m, thing.DecimalValue);
        }

        /// <summary>
        /// The exact underlying type. This case already worked before the fix, because
        /// <c>typeof(decimal?).IsAssignableFrom(typeof(decimal))</c> is <b>true</b>, so it takes the fast
        /// path without any conversion.
        /// </summary>
        /// <remarks>
        /// Worth pinning, because it is the reason the defect never appeared in-memory: dotNetRDF preserves
        /// <c>xsd:decimal</c>, so a decimal arrives and lands directly. Only a store that hands back a
        /// different numeric type needs the conversion that used to fail.
        /// </remarks>
        [Test]
        public void DecimalLandsInANullableDecimal()
        {
            var thing = new NumericThing(Subject);

            thing.AddPropertyToMapping(Property("nullableDecimal"), 400m, fromModel: true);

            Assert.AreEqual(400m, thing.NullableDecimal);
        }

        [TestCase("nullableInt", 400, TestName = "Int32 into int?")]
        [TestCase("nullableLong", 400, TestName = "Int32 into long?")]
        [TestCase("nullableShort", (short)400, TestName = "Int16 into short?")]
        public void WideningLandsInTheNullableFamily(string property, object value)
        {
            var thing = new NumericThing(Subject);

            Assert.DoesNotThrow(() => thing.AddPropertyToMapping(Property(property), value, fromModel: true));
            Assert.IsNotEmpty(thing.ListValues(Property(property)),
                "The value must reach the mapped property, not the unmapped bag.");
        }

        /// <summary>
        /// The collection branch converts separately, so it needs its own case.
        /// </summary>
        [Test]
        public void Int32LandsInADecimalCollection()
        {
            var thing = new NumericThing(Subject);

            thing.AddPropertyToMapping(Property("decimalList"), 400, fromModel: true);

            Assert.Contains(400m, thing.DecimalList);
        }

        /// <summary>
        /// Narrowing is refused rather than rounded. Before the conversion allowlist,
        /// <c>IsPrecisionCompatible</c> ended in an unconditional <c>return true</c>, so
        /// <c>Convert.ChangeType(3.7m, typeof(int))</c> silently produced <c>4</c>.
        /// </summary>
        /// <remarks>
        /// Refusal happens at the mapping gate, so the value is not written to the typed property and no
        /// exception is raised: it lands among the unmapped values instead. That is deliberate. Throwing
        /// would make one unexpected literal render the whole resource unreadable, which is precisely the
        /// failure this change exists to remove — and resources are open by design (ADR-0017), so an
        /// unmapped value is a normal place for it to live. Nothing is lost; it is simply not typed.
        /// </remarks>
        [TestCase("nullableInt", TestName = "into int?")]
        [TestCase("intValue", TestName = "into int")]
        public void FractionalDecimalIsNotRoundedIntoAnInt(string property)
        {
            var thing = new NumericThing(Subject);

            thing.AddPropertyToMapping(Property(property), 3.7m, fromModel: true);

            Assert.IsNull(thing.NullableInt, "A fractional decimal must not be rounded into int?.");
            Assert.AreEqual(0, thing.IntValue, "A fractional decimal must not be rounded into int.");

            // Refused for the property, but still retrievable — refusal must not mean data loss.
            Assert.Contains(3.7m, new List<object>(thing.ListValues(Property(property))),
                "The refused value must remain reachable as an unmapped value.");
        }

        #endregion

        #region Helpers

        private static Property Property(string localName) =>
            new Property(new Uri("http://example.org/numeric/" + localName));

        #endregion
    }
}
