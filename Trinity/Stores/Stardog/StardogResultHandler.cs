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

using System.Collections.Generic;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Store.Stardog
{
    /// <summary>
    /// RDF result handler for Stardog triple stores.
    /// </summary>
    public class StardogResultHandler : BaseResultsHandler
    {
        #region Members

        private List<SparqlResult> _results = new List<SparqlResult>();

        /// <summary>
        /// Result value of ASK queries.
        /// </summary>
        private bool _boolResult { get; set; }

        /// <summary>
        /// Binding result of SELECT queries.
        /// </summary>
        public SparqlResultSet SparqlResultSet { get { return new SparqlResultSet(_results); } }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new instance of the class <c>StardogResultHandler</c>.
        /// </summary>
        public StardogResultHandler() {}

        #endregion

        #region Methods

        /// <summary>
        /// Must be overridden by derived handlers to appropriately handle boolean results.
        /// </summary>
        /// <param name="result">Boolean result value.</param>
        protected override void HandleBooleanResultInternal(bool result)
        {
            _boolResult = result;
        }

        /// <summary>
        /// Must be overridden by derived handlers to appropriately handler SPARQL Results.
        /// </summary>
        /// <param name="result">SPARQL bindings.</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            _results.Add(result);

            return true;
        }

        /// <summary>
        /// Must be overridden by derived handlers to appropriately handle variable declarations.
        /// </summary>
        /// <param name="var">Variable name.</param>
        /// <returns></returns>
        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }

        /// <summary>
        /// Indicates the result value of ASK queries.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c></returns>
        public bool GetAnwser()
        {
            return _boolResult;
        }

        #endregion
    }
}
