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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Semiodesk.Trinity.Tests.Linq;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Oxigraph
{
    /// <summary>
    /// Runs the store-independent materialized layered-view suite against Oxigraph.
    /// </summary>
    /// <remarks>
    /// The two inference cases are inverted here, and the inversion is only in the second half of
    /// each. The shared fixture asserts that a rewriting view refuses an inferencing query and a
    /// materialized one accepts it: the view is then one ordinary graph, so Trinity has nothing left
    /// to refuse, and what the store does with the flag is the store's own business (ADR-0022).
    ///
    /// Oxigraph's answer to that business is a refusal. It has no reasoner, so honouring the flag is
    /// impossible, and answering without it would return an un-inferred result indistinguishable
    /// from a correct one (ADR-0046). Materialization lifts Trinity's refusal; it cannot conjure a
    /// reasoner. The first half of each test -- that the rewriting view refuses -- is unchanged and
    /// still asserted, because that half really is store-independent.
    ///
    /// Written out rather than delegating to <c>base</c>: the base wraps its second half in
    /// <c>Assert.DoesNotThrow</c>, so the refusal would arrive as an <c>AssertionException</c> and
    /// asserting on that would pin NUnit's plumbing rather than the store's behaviour.
    /// </remarks>
    [TestFixture]
    public class OxigraphLayeredModelMaterializationTest : LayeredModelMaterializationTest<OxigraphTestSetup>
    {
        private const string Ex = "http://example.org/mat/";

        [Test]
        public override void InferenceIsRefusedRewritingButAcceptedMaterialized()
        {
            Assert.Throws<NotSupportedException>(
                () => Rewriting.GetBindings(
                    new SparqlQuery($"SELECT ?s WHERE {{ ?s a <{Ex}Thing> }}", declarePrefixes: false),
                    inferenceEnabled: true).ToList(),
                "the overlay cannot be reasoned over -- Trinity's refusal, and store-independent");

            Assert.Throws<NotSupportedException>(
                () => Materialized.GetBindings(
                    new SparqlQuery($"SELECT ?s WHERE {{ ?s a <{Ex}Thing> }}", declarePrefixes: false),
                    inferenceEnabled: true).ToList(),
                "materialization lifts Trinity's refusal and leaves the store's: Oxigraph has no reasoner");
        }

        [Test]
        public override void InferenceThroughLinqIsAcceptedWhenMaterialized()
        {
            Assert.Throws<NotSupportedException>(
                () => Rewriting.AsQueryable<Agent>(inferenceEnabled: true).ToList());

            // Unwrapped, because the LINQ provider materializes results through reflection and so
            // hands back a TargetInvocationException around the store's refusal. Asserting on the
            // wrapper would pin the plumbing; asserting on the inner exception pins the refusal.
            // Worth knowing as a consumer: catching NotSupportedException around AsQueryable does
            // not work today for any backend that refuses something.
            var error = Assert.Catch(
                () => Materialized.AsQueryable<Agent>(inferenceEnabled: true).ToList());

            Assert.IsInstanceOf<NotSupportedException>(
                error is TargetInvocationException wrapped ? wrapped.InnerException : error,
                "the LINQ path carries the flag separately and reaches the same refusal");
        }
    }
}
