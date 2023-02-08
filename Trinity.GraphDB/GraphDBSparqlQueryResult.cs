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
// Copyright (c) Semiodesk GmbH 2022

using VDS.RDF.Query;
using VDS.RDF;

namespace Semiodesk.Trinity.Store.GraphDB
{
    /// <summary>
    /// The results returned from a SPARQL query.
    /// </summary>
    internal class GraphDBSparqlQueryResult : dotNetRDFQueryResult
    {
        #region Constructors

        /// <summary>
        /// Internal constructor which parses the results returned from a given query.
        /// </summary>
        /// <param name="query">The executed query.</param>
        /// <param name="store"></param>
        /// <param name="resultSet">the results</param>
        internal GraphDBSparqlQueryResult(GraphDBStore store, ISparqlQuery query, SparqlResultSet resultSet) : base(store, query, resultSet)
        {
        }

        /// <summary>
        /// Internal constructor which parses the results returned from a given query.
        /// </summary>
        /// <param name="query">The executed query.</param>
        /// <param name="store"></param>
        /// <param name="graph">the results</param>
        internal GraphDBSparqlQueryResult(GraphDBStore store, ISparqlQuery query, IGraph graph) : base(store, query, graph)
        {
        }

        #endregion
    }
}
