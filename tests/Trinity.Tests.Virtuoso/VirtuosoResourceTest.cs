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

using NUnit.Framework;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Virtuoso
{
    [TestFixture]
    public class VirtuosoResourceTest : ResourceTest<VirtuosoTestSetup>
    {
        [Test]
        public override void Int64Test()
        {
            Assert.Inconclusive("Virtuoso does not support xsd:long.");
        }

        [Test]
        public override void Uint64Test()
        {
            Assert.Inconclusive("Virtuoso does not support xsd:long.");
        }

        // The three below are the small-integer siblings of the two above, and fail for the same reason:
        // Virtuoso widens these datatypes into an integer box, so the value comes back as Int32. The
        // ResourceTest helper reads the *unmapped* property bag and casts with (TValue), which an Int32
        // cannot satisfy.
        //
        // Not a mapping defect. A mapped property declares a target type, so Trinity converts into it
        // (ADR-0040); the unmapped bag declares nothing, so there is no target to convert to and the value
        // is simply whatever the store returned. These are marked inconclusive rather than fixed because
        // the datatype is genuinely lost by the store, not mishandled by Trinity.
        //
        // If ListValues is ever given a CLR-type-fidelity guarantee, these must come back.

        [Test]
        public override void Int16Test()
        {
            Assert.Inconclusive("Virtuoso does not preserve xsd:short; it returns Int32. See ADR-0040.");
        }

        [Test]
        public override void Uint16Test()
        {
            Assert.Inconclusive(
                "Virtuoso does not preserve xsd:unsignedShort; it returns Int32. See ADR-0040.");
        }

        [Test]
        public override void UintTest()
        {
            Assert.Inconclusive(
                "Virtuoso does not preserve xsd:unsignedInt; it returns Int32. See ADR-0040.");
        }

        [Test]
        public override void TimeSpanTest()
        {
            // Virtuoso 7 still has no support xsd:duration 10 years after this was reported as an issue:
            // https://sourceforge.net/p/virtuoso/mailman/virtuoso-users/thread/CAE94aYXGvk0bZr-sJhOM%2BtDpDaEmpUD-GxhTCrMg9ad0QODdLA%40mail.gmail.com/#msg31757337
             
            Assert.Inconclusive("Virtuoso does not support xsd:duration.");
        }
    }
}
