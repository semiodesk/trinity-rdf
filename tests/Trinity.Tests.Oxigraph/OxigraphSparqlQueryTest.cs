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
using VDS.RDF.Query;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Oxigraph
{
    /// <summary>
    /// Runs the store-independent SPARQL query suite against Oxigraph.
    /// </summary>
    [TestFixture]
    public class OxigraphSparqlQueryTest : SparqlQueryTest<OxigraphTestSetup>
    {
        /// <summary>
        /// Asserts the refusal rather than skipping it. Oxigraph has no reasoner, so the store refuses a query that asks for inferencing rather than answering it without (ADR-0047). ADR-0022 lets a store ignore the flag -- Fuseki does -- but its Consequences name that as the defect: an un-inferred answer is indistinguishable from a correct one.
        /// </summary>
        [Test]
        public override void TestInferencing()
        {
            Assert.Throws<NotSupportedException>(() => base.TestInferencing());
        }

        /// <summary>
        /// A query the server rejects is reported as a query error, not a generic storage failure.
        /// </summary>
        /// <remarks>
        /// Sent raw, past Trinity's own parsing, so it is the server that refuses it. The connector's catch-all must not re-wrap the
        /// <see cref="RdfQueryException"/> the rejection produces as a storage exception.
        /// </remarks>
        [Test]
        public void AMalformedQueryIsReportedAsAQueryError()
        {
            Assert.Throws<RdfQueryException>(() => ((StoreBase)Store).ExecuteQuery("SELECT ?s WHERE { ?s ?p }"));
        }
    }
}
