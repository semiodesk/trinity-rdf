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

using System;
using System.Collections.Generic;
using Semiodesk.Trinity.Collections;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// The item provider for sparql queries.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete]
    public class SparqlQueryItemsProvider<T> : IItemsProvider<T> where T : Resource
    {
        #region Members

        private readonly ISparqlQueryResult _queryResult;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for the SparqlQueryItemsProvider.
        /// </summary>
        /// <param name="model">The model on which the query should be executed.</param>
        /// <param name="query">The query that should be executed.</param>
        /// <param name="inferenceEnabled">Modifier if inferncing should be enabled. Default is true</param>
        public SparqlQueryItemsProvider(IModel model, SparqlQuery query, bool inferenceEnabled = true)
        {
            _queryResult = model.ExecuteQuery(query, inferenceEnabled);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Number of elements in the result.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return _queryResult.Count();
        }

        /// <summary>
        /// Enumerator of the items. Should be narrowed with offset and limit.
        /// </summary>
        /// <param name="offset">Offset of the element where to start.</param>
        /// <param name="limit">Number of elements.</param>
        /// <returns></returns>
        public IEnumerable<T> GetItems(int offset, int limit)
        {
            return _queryResult.GetResources<T>(offset, limit);
        }

        #endregion
    }
}