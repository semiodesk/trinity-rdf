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
#if NET35
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
            var valueList = resource.ListValues();

            if (!valueList.Any())
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(SerializeUri(resource.Uri));
            result.Append(' ');

            foreach (var value in valueList)
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

        public static string SerializeFetchUris(IModel model, ISparqlQuery query, int offset = -1, int limit = -1)
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

        public static string SerializeOffsetLimit(IModel model, ISparqlQuery query, int offset = -1, int limit = -1)
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
