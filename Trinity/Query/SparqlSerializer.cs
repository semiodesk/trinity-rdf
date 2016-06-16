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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Xml;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Provides functionality to perform serialization of native .NET types into SPARQL strings.
    /// </summary>
    public class SparqlSerializer
    {
        #region Methods

        /// <summary>
        /// Declares the PREFIX expressions for the used namespaces in SPARQL 
        /// language based strings (Query or Update).
        /// </summary>
        /// <param name="sparqlBody">A SPARQL Query/Update expression.</param>
        /// <param name="namespaceManager">A namepsace manager</param>
        /// <returns>The query string with generated PREFIXes.</returns>
        internal static string GeneratePrologue(string sparqlBody, NamespaceManager namespaceManager)
        {
            string result = "";

            Dictionary<string, bool> declaredPrefixes = new Dictionary<string, bool>();
            Regex prefixExpression = new Regex(@"\s(?<prefix>[A-Za-z0-9]+)\:", RegexOptions.Compiled);

            foreach (Match match in prefixExpression.Matches(sparqlBody))
            {
                string prefix = match.Groups["prefix"].Value;

                if (!declaredPrefixes.ContainsKey(prefix))
                {
                    result += String.Format("PREFIX {0}: <{1}> ", prefix, namespaceManager.LookupNamespace(prefix));
                    declaredPrefixes.Add(prefix, true);
                }
            }

            return result + sparqlBody;
        }

        /// <summary>
        /// Serializes a string and excapes special characters
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SerializeString(string str)
        {
            // We need to escape specrial characters: http://www.w3.org/TeamSubmission/turtle/#sec-strings
            string s = str.Replace(@"\", @"\\");

            if(s.Contains('\n'))
            {
                return string.Format("'''{0}'''", s);
            }
            else
            {
                s = s.Replace("'", "\\'");

                return string.Format("'{0}'", s);
            }
        }

        /// <summary>
        /// Serializes a string with a translation
        /// </summary>
        /// <param name="str"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string SerializeTranslatedString(string str, string lang)
        {
            return string.Format("{0}@{1}", SerializeString(str), lang);
        }

        /// <summary>
        /// Serializes a typed literal
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="typeUri"></param>
        /// <returns></returns>
        public static string SerializeTypedLiteral(object obj, Uri typeUri)
        {
            return string.Format("'{0}'^^<{1}>", XsdTypeMapper.SerializeObject(obj), typeUri);
        }

        /// <summary>
        /// Serializes a value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeValue(object obj)
        {
            try
            {
                if (obj is string)
                {
                    return SerializeString(obj as string);
                }
                else if (obj is string[])
                {
                    // string + language
                    string[] array = obj as string[];
                    return SerializeTranslatedString(array[0], array[1]);
                }
                else if (obj is Tuple<string, CultureInfo>)
                {
                    // string + language
                    Tuple<string, CultureInfo> array = obj as Tuple<string, CultureInfo>;
                    return SerializeTranslatedString(array.Item1, array.Item2.Name);
                }
                else if (obj is Tuple<string, string>)
                {
                    // string + language
                    Tuple<string, string> array = obj as Tuple<string, string>;
                    return SerializeTranslatedString(array.Item1, array.Item2);
                }
                else if (obj is Uri || typeof(Uri).IsSubclassOf(obj.GetType()))
                {
                    return SerializeUri(obj as Uri);
                }
                else if (obj.GetType().GetInterface("IResource") != null)
                {
                    return SerializeUri((obj as IResource).Uri);
                }
                else if (obj.GetType().GetInterface("IModel") != null)
                {
                    return SerializeUri((obj as IModel).Uri);
                }
                else
                {
                    return SerializeTypedLiteral(obj, XsdTypeMapper.GetXsdTypeUri(obj.GetType()));
                }
            }
            catch
            {
                string msg = string.Format("No serializer availabe for object of type {0}.", obj.GetType());
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// Serializes a DateTime
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeDateTime(DateTime obj)
        {
            return string.Format("'{0}'^^<http://www.w3.org/2001/XMLSchema#dateTime>", XmlConvert.ToString((DateTime)obj, XmlDateTimeSerializationMode.Utc));
        }

        /// <summary>
        /// Serializes a Uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string SerializeUri(Uri uri)
        {
            return string.Format(@"<{0}>", uri.AbsoluteUri);
        }

        /// <summary>
        /// Serializes a resource
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static string SerializeResource(IResource resource)
        {
            StringBuilder result = new StringBuilder(SerializeUri(resource.Uri));
            result.Append(' ');

            foreach (var value in resource.ListValues())
            {
                if (value.Item2 == null)
                {
                    continue;
                }

                result.AppendFormat("{0} {1}; ", SerializeUri(value.Item1.Uri), SerializeValue(value.Item2));
            }

            result[result.Length - 2] = '.';

            return result.ToString();
        }

        /// <summary>
        /// Generate the Dataset for a single model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GenerateDatasetClause(IModel model)
        {
            if (model == null)
            {
                return "";
            }

            if (model is IModelGroup)
            {
                return GenerateDatasetClause(model as IModelGroup);
            }

            return "FROM " + SparqlSerializer.SerializeUri(model.Uri) + " ";
        }

        /// <summary>
        /// Generate the Dataset for the given model group
        /// </summary>
        /// <param name="modelGroup"></param>
        /// <returns></returns>
        public static string GenerateDatasetClause(IModelGroup modelGroup)
        {
            if (modelGroup is ModelGroup)
            {
                return (modelGroup as ModelGroup).DatasetClause;
            }
            else
            {
                return GenerateDatasetClause(modelGroup as IEnumerable<IModel>);
            }
        }

        /// <summary>
        /// Generate the Dataset for a enumeration of models
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public static string GenerateDatasetClause(IEnumerable<IModel> models)
        {
            if (!models.Any())
            {
                return "";
            }

            StringBuilder resultBuilder = new StringBuilder();

            foreach (var model in models)
            {
                resultBuilder.Append("FROM ");
                resultBuilder.Append(SparqlSerializer.SerializeUri(model.Uri));
                resultBuilder.Append(" ");
            }

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Serialize a ResourceQuery from certain model. Can also only deliver the Uris of the result resources.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="query"></param>
        /// <param name="onlyUris"></param>
        /// <returns></returns>
        public static string Serialize(IModel model, ResourceQuery query, bool onlyUris = false)
        {
            StringBuilder result = new StringBuilder();
            StringBuilder whereBlock = new StringBuilder();
            StringBuilder modifierBlock = new StringBuilder();
            SortedList<int, string> orderModifiers = new SortedList<int, string>();

            Serialize(model, query, whereBlock, orderModifiers, new List<ResourceQuery>());

            if (orderModifiers.Count > 0)
            {
                modifierBlock.Append("ORDER BY ");

                foreach (string value in orderModifiers.Values)
                {
                    modifierBlock.Append(value);
                }
            }

            if (query.Offset != -1)
            {
                modifierBlock.Append(" OFFSET ");
                modifierBlock.Append(query.Offset);
            }

            if(query.Limit != -1)
            {
                modifierBlock.Append(" LIMIT ");
                modifierBlock.Append(query.Limit);
            }
            if (onlyUris)
            {
                string q = "SELECT ?s0 {0} WHERE {{ ?s0 ?p0 ?o0 . {{ SELECT DISTINCT ?s0 WHERE {{{1}}} {2} }}}}";
                result.Append(string.Format(q, GenerateDatasetClause(model), whereBlock.ToString(), modifierBlock.ToString()));
            }
            else
            {

                if (whereBlock.Length > 0)
                {
                    // Because of a bug in OpenLink Virtuoso where it ignores ORDER BY expressions in 
                    // DESCRIBE queries we have to emulate the describe query by a nested SELECT query.
                    string q = "SELECT ?s0 ?p0 ?o0 {0} WHERE {{ ?s0 ?p0 ?o0 . {{ SELECT DISTINCT ?s0 WHERE {{{1}}} {2} }}}}";
                    result.Append(string.Format(q, GenerateDatasetClause(model), whereBlock.ToString(), modifierBlock.ToString()));
                }
                else
                {
                    string q = "DESCRIBE ?s0 {0} WHERE {{ ?s0 ?p0 ?o0 . }} {1}";
                    result.Append(string.Format(q, GenerateDatasetClause(model), modifierBlock.ToString()));
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Serialize a query from the given parameters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="query"></param>
        /// <param name="whereBlock"></param>
        /// <param name="solutionModifiers"></param>
        /// <param name="processed"></param>
        public static void Serialize(IModel model, ResourceQuery query, StringBuilder whereBlock, SortedList<int, string> solutionModifiers, IList<ResourceQuery> processed)
        {
            // The index of the currently processed resource query used for generating variable names.
            int i = processed.Count;

            // Prevent endless loops for cyclic references.
            processed.Add(query);

            // Recursively serialize all the queries which reference to this query.
            foreach(ResourceQuery dependentQuery in query.DependentQueries)
            {
                if (!processed.Contains(dependentQuery))
                {
                    Serialize(model, dependentQuery, whereBlock, solutionModifiers, processed);

                    whereBlock.Append(" ");
                }
            }

            if (query.WhereStatements.Count > 0)
            {
                // Counts the variables used for this resource.
                string s = "?s" + i;
                string p = "?p" + i;
                string o = "?o" + i;
                int pCount = 0;
                int oCount = 0;

                // We iterate over the asserted properties and generate SPARQL statements.
                foreach (StatementPattern pattern in query.WhereStatements)
                {
                    if (pattern.Object is ResourceQuery && !processed.Contains(pattern.Object as ResourceQuery))
                    {
                        continue;
                    }

                    if (pattern.Subject == null)
                    {
                        pattern.SubjectName = s;

                        whereBlock.Append(pattern.SubjectName + " ");
                    }
                    else
                    {
                        whereBlock.Append(SerializeUri(pattern.Subject) + " ");
                    }

                    // PREDICATE
                    if (pattern.Predicate is Uri)
                    {
                        // If there is a property we write the absolute URI..
                        whereBlock.Append(SerializeUri(pattern.Predicate) + " ");
                    }
                    else
                    {
                        pattern.PredicateName = p + pCount++;

                        whereBlock.Append(pattern.PredicateName + " ");
                    }

                    // OBJECT
                    if (pattern.Object is ResourceQuery)
                    {
                        ResourceQuery q = pattern.Object as ResourceQuery;

                        if (q.Uri != null)
                        {
                            // If the value is a reference to another query we write its variable name and close the statement.
                            whereBlock.Append(SerializeUri(q.Uri) + " . ");
                        }
                        else
                        {
                            // If the value is a reference to another query we write its variable name and close the statement.
                            whereBlock.Append("?s" + processed.IndexOf(q) + " . ");
                        }
                    }
                    else if (pattern.FilterOperation != FilterOperation.None)
                    {
                        pattern.ObjectName = o + oCount++;

                        switch (pattern.FilterOperation)
                        {
                            case FilterOperation.None:
                                break;
                            case FilterOperation.Equal:
                                whereBlock.Append(string.Format("{0} . FILTER({0} == {1}) . ", pattern.ObjectName, SerializeValue(pattern.Object)));
                                break;
                            case FilterOperation.NotEqual:
                                whereBlock.Append(string.Format("{0} . FILTER({0} != {1}) . ", pattern.ObjectName, SerializeValue(pattern.Object)));
                                break;
                            case FilterOperation.LessThan:
                                whereBlock.Append(string.Format("{0} . FILTER({0} < {1}) . ", pattern.ObjectName, SerializeValue(pattern.Object)));
                                break;
                            case FilterOperation.LessOrEqual:
                                whereBlock.Append(string.Format("{0} . FILTER({0} <= {1}) . ", pattern.ObjectName, SerializeValue(pattern.Object)));
                                break;
                            case FilterOperation.GreaterThan:
                                whereBlock.Append(string.Format("{0} . FILTER({0} > {1}) . ", pattern.ObjectName, SerializeValue(pattern.Object)));
                                break;
                            case FilterOperation.GreaterOrEqual:
                                whereBlock.Append(string.Format("{0} . FILTER({0} >= {1}) . ", pattern.ObjectName, SerializeValue(pattern.Object)));
                                break;
                            case FilterOperation.Contains:
                                whereBlock.Append(string.Format("{0} . FILTER ISLITERAL({0}) . FILTER REGEX(STR({0}), \"{1}\", \"i\") . ", pattern.ObjectName, pattern.Object));
                                break;
                        }
                    }
                    else if (pattern.Object != null)
                    {
                        // If a literal value was specified we append it and close the statement.
                        whereBlock.Append(SerializeValue(pattern.Object) + " . ");
                    }
                    else
                    {
                        // Save the current variable for later processing the sort descriptions..
                        pattern.ObjectName = o + oCount++;

                        // If the value is null we append a variable to the statement.
                        whereBlock.Append(pattern.ObjectName + " . ");
                    }

                    if (!string.IsNullOrEmpty(pattern.ObjectName))
                    {
                        if (pattern.SortDirection == SortDirection.Ascending)
                        {
                            solutionModifiers.Add(pattern.SortPriority, " ASC(" + pattern.ObjectName + ")");
                        }
                        else if (pattern.SortDirection == SortDirection.Descending)
                        {
                            solutionModifiers.Add(pattern.SortPriority, " DESC(" + pattern.ObjectName + ")");
                        }
                    }
                }
            }
            else if (query.Uri != null)
            {
                whereBlock.Append(SerializeUri(query.Uri) + " ?p" + i + " ?o" + i + " . ");
            }
            else
            {
                whereBlock.Append("?s" + i + " ?p" + i + " ?o" + i + " . ");
            }
        }

        /// <summary>
        /// Serialize a count query for the given ResourceQuery
        /// </summary>
        /// <param name="model"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string SerializeCount(IModel model, ResourceQuery query)
        {
            StringBuilder result = new StringBuilder();
            StringBuilder where = new StringBuilder();

            Serialize(model, query, where, new SortedList<int, string>(), new List<ResourceQuery>());

            if (query.Uri == null)
            {
                result.Append(string.Format("SELECT (COUNT(DISTINCT ?s0) AS ?count) {0} WHERE {{{1}}}", GenerateDatasetClause(model), where.ToString()));
            }
            else
            {
                result.Append(string.Format("SELECT (COUNT(DISTINCT <{2}>) AS ?count) {0} WHERE {{{1}}}", GenerateDatasetClause(model), where.ToString(), query.Uri));
            }

            return result.ToString();
        }

        /// <summary>
        /// Serialize a count query for the given SparqlQuery
        /// </summary>
        /// <param name="model"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string SerializeCount(IModel model, ISparqlQuery query)
        {
            string variable = "?" + query.GetGlobalScopeVariableNames()[0];
            string from = GenerateDatasetClause(model);
            string where = query.GetRootGraphPattern();

            StringBuilder queryBuilder = new StringBuilder();

            queryBuilder.Append("SELECT COUNT(DISTINCT ");
            queryBuilder.Append(variable);
            queryBuilder.Append(") AS ?count ");
            queryBuilder.Append(from);
            queryBuilder.Append(" WHERE { ");
            queryBuilder.Append(where);
            queryBuilder.Append(" }");

            return queryBuilder.ToString();
        }

        internal static string SerializeFetchUris(IModel model, ISparqlQuery query, int offset = -1, int limit = -1)
        {
            string variable = "?" + query.GetGlobalScopeVariableNames()[0];
            string from = GenerateDatasetClause(model);
            string where = query.GetRootGraphPattern();
            string orderby = query.GetRootOrderByClause();

            StringBuilder queryBuilder = new StringBuilder();
            
            foreach(string prefix in query.GetDeclaredPrefixes())
            {
                queryBuilder.AppendFormat("prefix <{0}> ", prefix);
            }

            queryBuilder.Append("select distinct ");
            queryBuilder.Append(variable);
            queryBuilder.Append(from);
            queryBuilder.Append(" where { ");
            queryBuilder.Append(where);
            queryBuilder.Append(" } ");
            queryBuilder.Append(orderby);

            if (offset != -1)
            {
                queryBuilder.Append(" offset ");
                queryBuilder.Append(offset);
            }

            if (limit != -1)
            {
                queryBuilder.Append(" limit ");
                queryBuilder.Append(limit);
            }

            return queryBuilder.ToString();
        }

        internal static string SerializeOffsetLimit(IModel model, ISparqlQuery query, int offset = -1, int limit = -1)
        {
            string variable = "?" + query.GetGlobalScopeVariableNames()[0];
            string from = GenerateDatasetClause(model);
            string where = query.GetRootGraphPattern();

            StringBuilder resultBuilder = new StringBuilder();
            resultBuilder.AppendFormat("SELECT {0} ?p ?o {1} WHERE {{ {0} ?p ?o {{", variable, from);
            resultBuilder.AppendFormat("SELECT DISTINCT {0} WHERE {{ {1} }}", variable, where);

            if (offset != -1)
            {
                resultBuilder.Append(" OFFSET ");
                resultBuilder.Append(offset);
            }

            if (limit != -1)
            {
                resultBuilder.Append(" LIMIT ");
                resultBuilder.Append(limit);
            }

            resultBuilder.Append(" } }");

            return resultBuilder.ToString();
        }

        #endregion
    }
}
