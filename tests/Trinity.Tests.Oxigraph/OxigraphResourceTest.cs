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
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using NUnit.Framework;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Oxigraph
{
    /// <summary>
    /// Runs the store-independent resource suite against Oxigraph.
    /// </summary>
    [TestFixture]
    public class OxigraphResourceTest : ResourceTest<OxigraphTestSetup>
    {
        // Oxigraph canonicalizes the integer-derived XSD datatypes into xsd:integer: a literal
        // written as "5"^^xsd:short is read back as "5"^^xsd:integer. Confirmed at the protocol
        // level with raw SPARQL, independently of Trinity, so it is the store's value-space
        // normalization rather than anything the adapter does.
        //
        // These tests read the *unmapped* property bag and cast with (TValue), and an unmapped value
        // declares no target type to convert into, so an Int32 cannot satisfy an Int16 cast. A
        // mapped property does declare one and converts into it (ADR-0040), which is why
        // NumericRoundTripTest passes here -- the same split as Virtuoso, for the same reason.
        //
        // If ListValues is ever given a CLR-type-fidelity guarantee, these must come back.

        [Test]
        public override void Int16Test()
        {
            Assert.Inconclusive("Oxigraph canonicalizes xsd:short to xsd:integer; it returns Int32. See ADR-0040.");
        }

        [Test]
        public override void Int64Test()
        {
            Assert.Inconclusive("Oxigraph canonicalizes xsd:long to xsd:integer; it returns Int32. See ADR-0040.");
        }

        [Test]
        public override void Uint16Test()
        {
            Assert.Inconclusive("Oxigraph canonicalizes xsd:unsignedShort to xsd:integer; it returns Int32. See ADR-0040.");
        }

        [Test]
        public override void Uint64Test()
        {
            Assert.Inconclusive("Oxigraph canonicalizes xsd:unsignedLong to xsd:integer; it returns Int32. See ADR-0040.");
        }

        [Test]
        public override void UintTest()
        {
            Assert.Inconclusive("Oxigraph canonicalizes xsd:unsignedInt to xsd:integer; it returns Int32. See ADR-0040.");
        }
    }
}
