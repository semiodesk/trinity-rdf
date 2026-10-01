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
using Semiodesk.Trinity.Tests.Store;
using System;

namespace Semiodesk.Trinity.Tests.DotNetRDF
{
    [TestFixture]
    public class DotNetRDFResourceMappingTest : ResourceMappingTest<DotNetRDFTestSetup>
    {
        /// <summary>
        /// Reading a resource of about 3000 values allocates in proportion to its size.
        /// </summary>
        /// <remarks>
        /// Reading a resource used to be quadratic in its size, for two reasons (#63): the graph result was read
        /// by position, re-enumerating it on every access, and the in-memory store's <c>DESCRIBE</c> described
        /// the subject once per triple. Measured at this size by reverting each fix alone: 6.2 GB and 1.25 GB,
        /// against 12 MB for the fixed read. So a 100 MB bound can be loose and still never flaky, which a
        /// bound on time could not. It also pins the call site: a <c>Model.GetResource</c> that built its
        /// own pattern-bound <c>DESCRIBE</c> would fail here.
        /// <para>
        /// In memory only, where the store evaluates on the calling thread, so the per-thread counter sees the
        /// engine's allocations too; the 1.25 GB above is the engine's. On the server backends the figure
        /// would include HTTP buffers.
        /// </para>
        /// </remarks>
        [Test]
        public void ReadingAResourceAllocatesInProportionToItsSize()
        {
            var uri = SeedResourceWithThousandsOfValues();

            // The first read of a type pays one-time costs that are not the read's own.
            var warmup = Model1.CreateResource<MappingTestClass>(BaseUri.GetUriRef("warmup"));
            warmup.uniqueStringTest = "warmup";
            warmup.Commit();

            Model1.GetResource<MappingTestClass>(warmup.Uri);

            long before = GC.GetAllocatedBytesForCurrentThread();

            var actual = Model1.GetResource<MappingTestClass>(uri);

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            // A read that came back short would be cheap for the wrong reason.
            Assert.AreEqual(ValuesPerKind, actual.intTest.Count);

            Assert.Less(allocated, 100L * 1024 * 1024,
                $"reading one resource of {2 * ValuesPerKind} values allocated {allocated / (1024 * 1024)} MB");
        }
    }
}
