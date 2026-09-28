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
using VDS.RDF.Query;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.GraphDB
{
    [TestFixture]
    public class GraphDBSparqlQueryTest : SparqlQueryTest<GraphDBTestSetup>
    {
        /// <summary>
        /// A query the server rejects is reported as a query error, not a generic storage failure.
        /// </summary>
        /// <remarks>
        /// Sent raw, past Trinity's own parsing, so it is the server that refuses it. The move to
        /// HttpClient left a <c>catch (WebException)</c> in the connector that could no longer run
        /// (HttpClient throws <c>HttpRequestException</c>), so a rejected query stopped being translated
        /// into an <see cref="RdfQueryException"/>.
        /// </remarks>
        [Test]
        public void AMalformedQueryIsReportedAsAQueryError()
        {
            Assert.Throws<RdfQueryException>(() => ((StoreBase)Store).ExecuteQuery("SELECT ?s WHERE { ?s ?p }"));
        }
    }
}