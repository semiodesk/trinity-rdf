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
// Copyright (c) Semiodesk GmbH 2016

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A generic sparql query interface.  
    /// </summary>
    public interface ISparqlQuery
    {
        #region Members

        /// <summary>
        /// The model on which the query will be run.
        /// </summary>
        IModel Model { get; set; }

        /// <summary>
        /// The type of the query.
        /// </summary>
        SparqlQueryType QueryType { get; }

        /// <summary>
        /// Indicates if inference should be enabled. It depends on the underlying store if and how this is used.
        /// </summary>
        bool IsInferenceEnabled { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Bind parameters to specified values.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        void Bind(string parameter, object value);

        /// <summary>
        /// Returns all prefixes that were specified by the query.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetDeclaredPrefixes();

        /// <summary>
        /// Enumerates the graphs which are declared in FROM and FROM NAMED directives at the root level.
        /// </summary>
        /// <returns>An enumeration of URI strings.</returns>
        IEnumerable<string> GetDefaultModels();

        string[] GetGlobalScopeVariableNames();

        string GetRootGraphPattern();

        string GetRootOrderByClause();

        bool ProvidesStatements();

        /// <summary>
        /// Returns the string representation of the query.
        /// </summary>
        /// <returns></returns>
        string ToString();

        #endregion
    }
}
