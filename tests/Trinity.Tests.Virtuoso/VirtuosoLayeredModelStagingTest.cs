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

using System.Data;
using Semiodesk.Trinity.Store.Virtuoso;
using NUnit.Framework;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Virtuoso
{
    /// <summary>
    /// Runs the staged-writes suite against Virtuoso.
    /// </summary>
    /// <remarks>
    /// Virtuoso is the one backend where the two atomicity claims are reversed: no failure can be
    /// injected into a multi-operation request because it reports success for every candidate, and it is
    /// the only backend whose <see cref="ITransaction"/> is real. So <c>Accept()</c> is protected here by
    /// its transaction rather than by request atomicity. See ADR-0042.
    /// </remarks>
    [TestFixture]
    public class VirtuosoLayeredModelStagingTest : LayeredModelStagingTest<VirtuosoTestSetup>
    {
        /// <summary>
        /// Virtuoso swallows every failure that could be injected, so the request-atomicity claim is
        /// unprobed here rather than untrue.
        /// </summary>
        protected override bool MultiOperationRequestIsAtomic => false;

        /// <summary>
        /// The inverse of the base assertion: here rollback genuinely undoes the write.
        /// </summary>
        /// <remarks>
        /// This is what ADR-0042 means by "accept starts a transaction unconditionally: real on Virtuoso,
        /// a no-op elsewhere". The base fixture pins the no-op half; this pins the real half, so the
        /// claim is asserted from both sides rather than measured once and trusted.
        /// </remarks>
        [Test]
        public override void RollbackOnANoOpTransactionUndoesNothing()
        {
            GivenBaselineValue(R1, "original");

            using (ITransaction transaction = Store.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Assert.IsInstanceOf<VirtuosoTransaction>(transaction,
                    "Virtuoso is the backend with a real transaction");

                var staged = View.GetResource<MappingTestClass>(R1);
                staged.uniqueStringTest = "changed";
                View.UpdateResource(staged, transaction);

                transaction.Rollback();
            }

            Assert.AreEqual("original", View.GetResource<MappingTestClass>(R1).uniqueStringTest,
                "the rollback undid the staged write, which is what Accept() relies on here");
        }
    }
}
