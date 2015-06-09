/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) Semiodesk GmbH 2015

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System.Collections.Generic;
using System.Linq;
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
    public class SparqlQuery
    {
        #region Members

        /// <summary>
        /// Indicates if run-time inferencing is enabled.
        /// </summary>
        public bool InferenceEnabled = false;

        /// <summary>
        /// Indicates if the query parser is enabled.
        /// </summary>
        protected bool QueryParserEnabled = true;

        /// <summary>
        /// The query parser is used to determine the query type and 
        /// wheter a given SELECT expression provides triple bindings.
        /// </summary>
        internal VDS.RDF.Query.SparqlQuery ParsedQuery;

        /// <summary>
        /// The SPARQL query string including all generated PREFIX declarations.
        /// </summary>
        public string Query { get; protected set; }

        /// <summary>
        /// The query type being returned if the query parser is not enabled.
        /// </summary>
        private SparqlQueryType _queryType = SparqlQueryType.Unknown;

        public IModel Model { get; private set; }

        /// <summary>
        /// The query form as defined in http://www.w3.org/TR/rdf-sparql-query/#QueryForms
        /// </summary>
        public SparqlQueryType QueryType
        {
            get
            {
                if (QueryParserEnabled)
                {
                    switch (ParsedQuery.QueryType)
                    {
                        case VDS.RDF.Query.SparqlQueryType.Describe:
                        case VDS.RDF.Query.SparqlQueryType.DescribeAll:
                            return SparqlQueryType.Describe;
                        case VDS.RDF.Query.SparqlQueryType.Select:
                        case VDS.RDF.Query.SparqlQueryType.SelectAll:
                        case VDS.RDF.Query.SparqlQueryType.SelectAllDistinct:
                        case VDS.RDF.Query.SparqlQueryType.SelectAllReduced:
                        case VDS.RDF.Query.SparqlQueryType.SelectDistinct:
                        case VDS.RDF.Query.SparqlQueryType.SelectReduced:
                            return SparqlQueryType.Select;
                        case VDS.RDF.Query.SparqlQueryType.Construct:
                            return SparqlQueryType.Construct;
                        case VDS.RDF.Query.SparqlQueryType.Ask:
                            return SparqlQueryType.Ask;
                        default:
                            return SparqlQueryType.Unknown;
                    }
                }
                else
                {
                    return _queryType;
                }
            }
            protected set
            {
                _queryType = value;
            }
        }

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

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new SPARQL Query with an optional namespace manager instance which
        /// can be used to declare PREFIX declarations for the namespace abbreviations
        /// used in the query string.
        /// </summary>
        /// <param name="query">The SPARQL query string.</param>
        /// <param name="namespaceManager">The optional namespace manager used to declare SPARQL PREFIXes.</param>
        /// <param name="parseVariables"></param>
        /// <param name="variables"></param>
        public SparqlQuery(string query, NamespaceManager namespaceManager = null)
        {
            if (namespaceManager == null)
            {
                Query = query;
            }
            else
            {
                Query = SparqlSerializer.GeneratePrologue(query, namespaceManager);
            }

            QueryParserEnabled = true;
            ParsedQuery = new SparqlQueryParser(SparqlQuerySyntax.Extended).ParseFromString(Query);

            if (ParsedQuery.Variables != null && ParsedQuery.Variables.Count() == 3)
            {
                SubjectVariable = ParsedQuery.Variables.ElementAt(0).Name;
                PredicateVariable = ParsedQuery.Variables.ElementAt(1).Name;
                ObjectVariable = ParsedQuery.Variables.ElementAt(2).Name;
            }
        }

        /// <summary>
        /// Create a new SPARQL query without parsing the query with the built-in query parser.
        /// </summary>
        /// <param name="queryType">The SPARQL query type.</param>
        /// <param name="query">The SPARQL query string.</param>
        /// <param name="namespaceManager">The optional namespace manager used to declare Sparql PREFIXes.</param>
        /// <param name="statementVariables">The variable names which are being used to parse the statements returend by the query when creating resources.</param>
        public SparqlQuery(SparqlQueryType queryType, string query, NamespaceManager namespaceManager, IList<string> statementVariables = null)
        {
            if (namespaceManager != null)
            {
                Query = SparqlSerializer.GeneratePrologue(query, namespaceManager);
            }
            else
            {
                Query = query;
            }

            QueryParserEnabled = false;
            QueryType = queryType;

            if (statementVariables != null && statementVariables.Count == 3)
            {
                SubjectVariable = statementVariables[0];
                PredicateVariable = statementVariables[1];
                ObjectVariable = statementVariables[2];
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Counts the maximum number of variables in a given query graph pattern and
        /// its child graph patterns. One can use this method to determine if a given
        /// query selects has a variable set which defines triples or not.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private bool ParseStatementVariables(GraphPattern pattern)
        {
            if (QueryParserEnabled)
            {
                foreach (ITriplePattern p in pattern.TriplePatterns)
                {
                    if (p is TriplePattern)
                    {
                        TriplePattern t = p as TriplePattern;

                        if (t.Variables.Count == 3)
                        {
                            // TODO: ensure that the order of the variables is as stated in the query.
                            SubjectVariable = t.Subject.ToString().Substring(1);
                            PredicateVariable = t.Predicate.ToString().Substring(1);
                            ObjectVariable = t.Object.ToString().Substring(1);

                            return true;
                        }
                    }
                }

                foreach (GraphPattern p in pattern.ChildGraphPatterns)
                {
                    if (ParseStatementVariables(p))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Indicates if the query provides a description of one or more resources.
        /// </summary>
        public bool ProvidesStatements()
        {
            if (QueryType == SparqlQueryType.Describe || QueryType == SparqlQueryType.Construct)
            {
                return true;
            }
            else if (QueryType == SparqlQueryType.Select)
            {
                return (ParsedQuery.Variables.Count() > 2 && ParseStatementVariables(ParsedQuery.RootGraphPattern));
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates if the query will be matched against the background/default graph.
        /// </summary>
        /// <returns>True if the query will be matched against the background graph.</returns>
        internal bool IsAgainstDefaultModel()
        {
            // NOTE: This could also be done using _parser.UsesDefaultGraph. However, due to a bug
            //       in the Parser this method sometimes results in a NullReferenceException.
            if (QueryParserEnabled)
            {
                return ParsedQuery.DefaultGraphs.Count() == 0;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a FROM <Uri> clause to the query in order to restrict it to the given model. 
        /// </summary>
        /// <param name="model">A model the query should be executed on.</param>
        internal void SetModel(IModel model)
        {
            Model = model;

            //if (!QueryParserEnabled)
            //{
            //    QueryParserEnabled = true;
            //    QueryParser = new SparqlQueryParser(SparqlQuerySyntax.Extended).ParseFromString(Query);
            //}

            if (QueryParserEnabled)
            {
                if (model is IModelGroup)
                {
                    foreach (var m in (model as IModelGroup))
                        ParsedQuery.AddDefaultGraph(m.Uri);
                }else
                    ParsedQuery.AddDefaultGraph(model.Uri);
            }
        }

        internal void SetModelGroup(IModelGroup modelGroup)
        {
            Model = modelGroup;

            if (QueryParserEnabled)
            {
                foreach (var m in modelGroup)
                {
                    ParsedQuery.AddNamedGraph(m.Uri);
                }
            }
        }

        /// <summary>
        /// Adds a LIMIT <int> clause to the query in order to restrict it to put an upper bound on the number of solutions returned. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        internal void SetLimit(int limit)
        {
            if (QueryParserEnabled)
            {
                ParsedQuery.Limit = limit;
            }
        }

        /// <summary>
        /// Adds an Offset <int> clause to the query which causes the solutions generated to start after the specified number of solutions. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        internal void SetOffset(int offset)
        {
            if (QueryParserEnabled)
            {
                ParsedQuery.Offset = offset;
            }
        }

        private bool IsOrdered(VDS.RDF.Query.SparqlQuery query)
        {
            if (query.OrderBy != null)
                return true;
            else
                return IsOrdered(query.RootGraphPattern);
        }

        private bool IsOrdered(GraphPattern graphPattern)
        {
            foreach (ITriplePattern pattern in graphPattern.TriplePatterns)
            {
                if (pattern is SubQueryPattern)
                {
                    SubQueryPattern b = (pattern as SubQueryPattern);
                    if (b.SubQuery.OrderBy != null)
                    {
                        return true;
                    }
                    else
                    {
                        if (IsOrdered(b.SubQuery.RootGraphPattern))
                            return true;
                    }
                }

            }
            

            return false;
        }

        internal bool IsSorted()
        {
            if (!QueryParserEnabled) return false;

            return IsOrdered(ParsedQuery);
        }

        /// <summary>
        /// Returns the query string with generated prefixes and optional definitions for
        /// the Virtuoso store.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (QueryParserEnabled) ? FixFilterSyntax(ParsedQuery.ToString()) : Query;
        }

        /// <summary>
        /// Fix for a bug in dotNetRdf where it replaces valid SPARQL1.1 FILTER NOT EXISTS() expressions with invalid FILTER(NOT EXISTS..).
        /// </summary>
        /// <param name="query">A query string.</param>
        /// <returns>A valid SPARQL query.</returns>
        internal static string FixFilterSyntax(string query)
        {
            return Regex.Replace(query, @"filter\(not\sexists\s*{(?<expression>.+)}\s*\)", delegate(Match match)
            {
                return string.Format("FILTER NOT EXISTS {{{0}}}", match.Groups["expression"]);
            },
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        #endregion
    }

    /// <summary>
    /// The SPARQL query forms as specified in http://www.w3.org/TR/rdf-sparql-query/#QueryForms
    /// </summary>
    public enum SparqlQueryType { Unknown, Ask, Construct, Describe, Select };
}
