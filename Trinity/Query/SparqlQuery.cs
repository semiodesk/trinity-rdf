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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Patterns;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A simple SPAQL Query class. This class aims to ease formulating SPARQL
    /// query strings by automatically setting PREFIX declarations from a given
    /// namespace manager.
    /// </summary>
    /// <see href="http://www.w3.org/TR/rdf-sparql-query/#grammar"/>
    public class SparqlQuery : ISparqlQuery
    {
        #region Members

        /// <summary>
        /// Cached version of the query string.
        /// </summary>
        private string _query;

        /// <summary>
        /// Indicates if a query parameter value has been changed and the cached query string needs to be regenerated.
        /// </summary>
        private bool _isModified;

        /// <summary>
        /// The SPARQL query processor used to determine the prefixes and statement variables in the query.
        /// </summary>
        private SparqlQueryPreprocessor _preprocessor;

        /// <summary>
        /// Names of the globally defined variables without the preceding '?'.
        /// </summary>
        private string[] _globalScopeVariableNames;

        /// <summary>
        /// The default model of the Query, if there is excactly one.
        /// </summary>
        private IModel _model;

        /// <summary>
        /// Get or set the model used for this query.
        /// </summary>
        public IModel Model
        {
            get { return _model; }
            set
            {
                if(value != _model && _preprocessor != null)
                {
                    _model = value;

                    if (value is IModelGroup)
                    {
                        IModelGroup group = value as IModelGroup;

                        foreach (IModel m in group)
                        {
                            _preprocessor.AddNamedGraph(m.Uri);
                        }
                    }
                    else
                    {
                        _preprocessor.AddDefaultGraph(value.Uri);
                    }

                    _isModified = true;
                }
            }
        }

        /// <summary>
        /// The query form as defined in http://www.w3.org/TR/rdf-sparql-query/#QueryForms
        /// </summary>
        public SparqlQueryType QueryType { get; protected set; }

        /// <summary>
        /// Indicates if the query result should be expanded using run-time inferencing.
        /// </summary>
        public bool IsInferenceEnabled { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new SPARQL query. If enabled, the PREFIXES used in any of the query's graph patterns will
        /// be declared in the query header if they are found in the application config. Additionally, the query 
        /// may be compacted in order to reduce processing overhead when being used repeatedly in loops.
        /// </summary>
        /// <param name="queryString">The SPARQL query string.</param>
        /// <param name="declarePrefixes">Set to <c>true</c> if the namespace prefixes used in the query should be declared.</param>
        /// <param name="compactQuery">Set to <c>true</c> if formatting characters should be removed from the query to increase processing speed.</param>
        public SparqlQuery(string queryString, bool declarePrefixes = true)
        {
            using (TextReader reader = new StringReader(queryString))
            {
                // Parse the query for namespace prefixes and optionally remove any formatting characters.
                _preprocessor = new SparqlQueryPreprocessor(reader, SparqlQuerySyntax.Extended);
                _preprocessor.Process(declarePrefixes);

                QueryType = _preprocessor.QueryType;

                _globalScopeVariableNames = _preprocessor.GlobalScopeVariables.Select(v => v.Substring(1)).ToArray();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates if the query provides a description of one or more resources.
        /// </summary>
        public bool ProvidesStatements()
        {
            return _preprocessor.QueryProvidesStatements;
        }

        /// <summary>
        /// Set the value for a query parameter which is preceeded by '@'.
        /// </summary>
        /// <param name="parameter">The parameter name including the '@'.</param>
        /// <param name="value">The paramter value.</param>
        public void Bind(string parameter, object value)
        {
            if(_preprocessor == null)
            {
                throw new NotSupportedException("SPARQL query parameters can only be used with a query processor. Try using the default constructor.");
            }

            _preprocessor.Bind(parameter, value);

            _isModified = true;
        }

        /// <summary>
        /// Enumerates the graphs which are declared in FROM and FROM NAMED directives at the root level.
        /// </summary>
        /// <returns>An enumeration of URI strings.</returns>
        public IEnumerable<string> GetDefaultModels()
        {
            return _preprocessor.DefaultGraphs;
        }

        public IEnumerable<string> GetDeclaredPrefixes()
        {
            return _preprocessor.DeclaredPrefixes;
        }

        public string[] GetGlobalScopeVariableNames()
        {
            return _globalScopeVariableNames;
        }

        public string GetRootGraphPattern()
        {
            return _preprocessor.GetRootGraphPattern();
        }

        /// <summary>
        /// Indicates if the query contains an ORDER BY clause in any of its graph patterns.
        /// </summary>
        /// <returns><c>true</c> if the query contains an ORDER BY clause, <c>false</c> otherwise.</returns>
        public string GetRootOrderByClause()
        {
            return _preprocessor.GetOrderByClause();
        }

        /// <summary>
        /// Adds a LIMIT &lt;int&gt; clause to the query in order to restrict it to put an upper bound on the number of solutions returned. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        internal void SetLimit(int limit)
        {
            _preprocessor.SetLimit(limit);
        }

        /// <summary>
        /// Adds an Offset &lt;int&gt; clause to the query which causes the solutions generated to start after the specified number of solutions. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        internal void SetOffset(int offset)
        {
            _preprocessor.SetOffset(offset);
        }

        /// <summary>
        /// Returns the query string with generated prefixes and subsituted parameters.
        /// </summary>
        /// <returns>A valid SPARQL string.</returns>
        public override string ToString()
        {
            if(_query == null || _isModified)
            {
                _query = _preprocessor.ToString();
            }

            return _query;
        }

        #endregion
    }

    /// <summary>
    /// The SPARQL query forms as specified in http://www.w3.org/TR/rdf-sparql-query/#QueryForms
    /// </summary>
    public enum SparqlQueryType { Unknown, Ask, Construct, Describe, Select };
}
