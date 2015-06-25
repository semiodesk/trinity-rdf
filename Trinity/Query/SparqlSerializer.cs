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

        public static string SerializeString(string str)
        {
            // We need to escape specrial characters: http://www.w3.org/TeamSubmission/turtle/#sec-strings
            string s = str.Replace(@"\", @"\\");
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\n", "\\n");

            return string.Format("\"{0}\"", s);
        }

        public static string SerializeTranslatedString(string str, string lang)
        {
            return string.Format("\"{0}\"@{1}", SerializeString(str), lang);
        }

        public static string SerializeTypedLiteral(object obj, Uri typeUri)
        {
            return string.Format("'{0}'^^<{1}>", XsdTypeMapper.SerializeObject(obj), typeUri);
        }

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
                    return SerializeTranslatedString(array.Item1, array.Item2.IetfLanguageTag);
                }
                else if (obj is Uri || typeof(Uri).IsSubclassOf(obj.GetType()))
                {
                    return SerializeUri(obj as Uri);
                }
                else if (obj.GetType().GetInterface("IResource") != null)
                {
                    return SerializeUri((obj as IResource).Uri);
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

        public static string SerializeDateTime(DateTime obj)
        {
            return string.Format("'{0}'^^<http://www.w3.org/2001/XMLSchema#dateTime>", XmlConvert.ToString((DateTime)obj, XmlDateTimeSerializationMode.Utc));
        }

        public static string SerializeUri(Uri uri)
        {
            return string.Format(@"<{0}>", uri.AbsoluteUri);
        }

        public static string SerializeResource(IResource resource)
        {
            StringBuilder result = new StringBuilder(SerializeUri(resource.Uri));

            foreach (var value in resource.ListValues())
            {
                if (value.Item2 != null)
                {
                    result.Append(string.Format("{0} {1};\n", SerializeUri(value.Item1.Uri), SerializeValue(value.Item2)));
                }
            }

            result[result.Length - 2] = '.';

            return result.ToString();
        }

        public static string GenerateDatasetClause(IModelGroup modelGroup)
        {
            if (modelGroup is ModelGroup)
                return ((ModelGroup)modelGroup).DatasetClause;
            return GenerateDatasetClause(modelGroup as IEnumerable<IModel>);               
        }

        public static string GenerateDatasetClause(IEnumerable<IModel> models)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var x in models)
            {
                builder.AppendFormat("FROM {0} ", SparqlSerializer.SerializeUri(x.Uri));
            }
            return builder.ToString();
        }

        public static string GenerateDatasetClause(IModel model)
        {
            if (model is IModelGroup)
                return GenerateDatasetClause(model as IModelGroup);

            return string.Format("FROM {0}", SparqlSerializer.SerializeUri(model.Uri));
        }

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

        public static string SerializeCount(IModel model, ResourceQuery query)
        {
            StringBuilder result = new StringBuilder();
            StringBuilder whereBlock = new StringBuilder();

            Serialize(model, query, whereBlock, new SortedList<int, string>(), new List<ResourceQuery>());

            if (query.Uri == null)
            {
                result.Append(string.Format("SELECT (COUNT(DISTINCT ?s0) AS ?count) {0} WHERE {{{1}}}", GenerateDatasetClause(model), whereBlock.ToString()));
            }
            else
            {
                result.Append(string.Format("SELECT (COUNT(DISTINCT <{2}>) AS ?count) {0} WHERE {{{1}}}", GenerateDatasetClause(model), whereBlock.ToString(), query.Uri));
            }

            return result.ToString();
        }

        public static string SerializeCount(IModel model, SparqlQuery query)
        {
            StringBuilder result = new StringBuilder();

            result.Append(string.Format("SELECT COUNT(DISTINCT {2}) AS ?count {0} WHERE {{{1}}}", GenerateDatasetClause(model), query.ParsedQuery.RootGraphPattern, query.ParsedQuery.Variables.FirstOrDefault()));

            return result.ToString();
        }

        internal static string SerializeFetchUris(IModel model, SparqlQuery query, int offset = -1, int limit = -1)
        {
            StringBuilder queryBuilder = new StringBuilder();

            string modifierBlock = "";

            if (query.ParsedQuery.OrderBy != null && query.ParsedQuery.OrderBy.Variables.Count() > 0)
            {
                string order = query.ParsedQuery.OrderBy.ToString();
                modifierBlock = string.Format("{0} ORDER BY {1}", modifierBlock, order);
            }

            if (offset != -1)
            {
                modifierBlock = string.Format("{0} OFFSET {1}", modifierBlock, offset);
            }

            if (limit != -1)
            {
                modifierBlock = string.Format("{0} LIMIT {1}", modifierBlock, limit);
            }

            foreach (string prefix in query.ParsedQuery.NamespaceMap.Prefixes)
            {
                queryBuilder.AppendLine("PREFIX " + prefix + ": <" + query.ParsedQuery.NamespaceMap.GetNamespaceUri(prefix).OriginalString + ">");
            }

            queryBuilder.Append(string.Format("SELECT DISTINCT {2} {0} WHERE {{ {1} }} {3}", GenerateDatasetClause(model), query.ParsedQuery.RootGraphPattern, query.ParsedQuery.Variables.FirstOrDefault(), modifierBlock));

            return queryBuilder.ToString();
        }

        internal static string SerializeOffsetLimit(IModel model, SparqlQuery query, int offset = -1, int limit = -1)
        {
            StringBuilder result = new StringBuilder();

            string modifierBlock = "";
            if (offset != -1)
            {
                modifierBlock = string.Format(" OFFSET {0}", offset);
            }

            if (limit != -1)
            {
                modifierBlock = string.Format("{0} LIMIT {1}", modifierBlock, limit);
            }

            result.Append(string.Format("SELECT {2} ?p ?o {0} WHERE {{ {2} ?p ?o. {{ SELECT DISTINCT {2} WHERE {{ {1} }} {3} }} }}", GenerateDatasetClause(model), query.ParsedQuery.RootGraphPattern, query.ParsedQuery.Variables.FirstOrDefault(), modifierBlock));


            return result.ToString();
        }

        #endregion
    }
}
