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
        /// Get or set the model used for this query.
        /// </summary>
        public IModel Model { get; private set; }

        /// <summary>
        /// The query form as defined in http://www.w3.org/TR/rdf-sparql-query/#QueryForms
        /// </summary>
        public SparqlQueryType QueryType { get; protected set; }

        /// <summary>
        /// The SPARQL query processor used to determine the prefixes and statement variables in the query.
        /// </summary>
        internal SparqlProcessor QueryProcessor;

        /// <summary>
        /// Holds the variable name of the subject for SELECT queries which
        /// provide triple results. Please check with ProvidesStatements() before
        /// using this variable.
        /// </summary>
        internal string SubjectVariable { get; private set; }

        /// <summary>
        /// Holds the variable name of the predicate for SELECT queries which
        /// provide triple results. Please check with ProvidesStatements() before
        /// using this variable.
        /// </summary>
        internal string PredicateVariable { get; private set; }

        /// <summary>
        /// Holds the variable name of the object for SELECT queries which
        /// provide triple results. Please check with ProvidesStatements() before
        /// using this variable.
        /// </summary>
        internal string ObjectVariable { get; private set; }

        /// <summary>
        /// Indicates if the query result should be expanded using run-time inferencing.
        /// </summary>
        public bool InferenceEnabled { get; set; }

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
                QueryProcessor = new SparqlProcessor(reader, SparqlQuerySyntax.Extended);
                QueryProcessor.Process();

                // Declare the globally registered RDF namespace prefixes used in the query.
                if (declarePrefixes)
                {
                    foreach (string prefix in QueryProcessor.UsedPrefixes)
                    {
                        if (OntologyDiscovery.Namespaces.ContainsKey(prefix))
                        {
                            Uri uri = OntologyDiscovery.Namespaces[prefix];

                            QueryProcessor.AddPrefix(prefix, uri);
                        }
                        else
                        {
                            string msg = string.Format("The prefix '{0}' is not registered with any ontology in app.config", prefix);

                            throw new KeyNotFoundException(msg);
                        }
                    }
                }

                QueryType = QueryProcessor.QueryType;

                if (QueryProcessor.ProvidesStatements && QueryProcessor.GlobalScopeVariables.Count == 3)
                {
                    SubjectVariable = QueryProcessor.GlobalScopeVariables[0].Substring(1);
                    PredicateVariable = QueryProcessor.GlobalScopeVariables[1].Substring(1);
                    ObjectVariable = QueryProcessor.GlobalScopeVariables[2].Substring(1);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates if the query will be matched against the background/default graph.
        /// </summary>
        /// <returns>True if the query will be matched against the background graph.</returns>
        internal bool IsAgainstDefaultModel()
        {
            return QueryProcessor.DefaultGraphs.Count == 0;
        }

        /// <summary>
        /// Indicates if the query provides a description of one or more resources.
        /// </summary>
        public bool ProvidesStatements()
        {
            return QueryProcessor.ProvidesStatements;
        }

        /// <summary>
        /// Set the value for a query parameter which is preceeded by '@'.
        /// </summary>
        /// <param name="parameter">The parameter name including the '@'.</param>
        /// <param name="value">The paramter value.</param>
        public void Set(string parameter, object value)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                throw new ArgumentException("Empty or null value for SPARQL query parameter.");
            }
            else if (parameter[0] != '@')
            {
                throw new ArgumentException("SPARQL query parameters must start with '@'.");
            }
            else if (value == null)
            {
                throw new ArgumentNullException("SPARQL query parameter values may not be null.");
            }

            QueryProcessor.ParameterValues[parameter] = SparqlSerializer.SerializeValue(value);
        }

        /// <summary>
        /// Adds a FROM clause to the query in order to restrict it to the given model. 
        /// </summary>
        /// <param name="model">A model the query should be executed on.</param>
        internal void SetModel(IModel model)
        {
            Model = model;

            if (model is IModelGroup)
            {
                IModelGroup group = model as IModelGroup;

                foreach (IModel m in group)
                {
                    QueryProcessor.AddNamedGraph(m.Uri);
                }
            }
            else
            {
                QueryProcessor.AddDefaultGraph(model.Uri);
            }
        }

        /// <summary>
        /// Indicates if the query contains an ORDER BY clause in any of its graph patterns.
        /// </summary>
        /// <returns><c>true</c> if the query contains an ORDER BY clause, <c>false</c> otherwise.</returns>
        internal bool IsOrdered()
        {
            return QueryProcessor.IsOrdered;
        }

        internal void SetLimit(int limit)
        {
            QueryProcessor.SetLimit(limit);
        }

        internal void SetOffset(int offset)
        {
            QueryProcessor.SetOffset(offset);
        }

        /// <summary>
        /// Returns the query string with generated prefixes and optional definitions for
        /// the Virtuoso store.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return QueryProcessor.ToString();
        }

        #endregion
    }

    /// <summary>
    /// The SPARQL query forms as specified in http://www.w3.org/TR/rdf-sparql-query/#QueryForms
    /// </summary>
    public enum SparqlQueryType { Unknown, Ask, Construct, Describe, Select };
}
