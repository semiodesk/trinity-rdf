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

using System.Collections.Generic;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Exposes a SPARQL query.
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
        ISparqlQuery Bind(string parameter, object value);

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

        /// <summary>
        /// Get an array of all variable names that are defined in the root scope of the query.
        /// </summary>
        /// <returns>An array of avaiable names without the preceding '$' or '?' characters, if any.</returns>
        string[] GetGlobalScopeVariableNames();

        /// <summary>
        /// Get the root graph pattern.
        /// </summary>
        /// <returns>A non empty string, on success.</returns>
        string GetRootGraphPattern();

        /// <summary>
        /// Gets the outermost ORDER BY clause.
        /// </summary>
        /// <returns>A non empty string if a ORDER BY clause is defined.</returns>
        string GetRootOrderByClause();

        /// <summary>
        /// Indicates if the query selects variables that are used as subject, predicate and object in a triple pattern.
        /// </summary>
        /// <returns><c>true</c> if the query selects triples, <c>false</c> otherwise.</returns>
        bool ProvidesStatements();

        /// <summary>
        /// Returns the string representation of the query.
        /// </summary>
        /// <returns>The SPARQL query string.</returns>
        string ToString();

        #endregion
    }
}
