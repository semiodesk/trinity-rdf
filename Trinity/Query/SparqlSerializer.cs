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
using System.Linq;
using System.Text;
using System.Globalization;
using System.Xml;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Provides functionality to perform serialization of native .NET types into SPARQL strings.
    /// </summary>
    public class SparqlSerializer
    {
        #region Methods

        /// <summary>
        /// Serializes a string and excapes special characters.
        /// </summary>
        /// <param name="str">A string literal.</param>
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
        /// <param name="str">A string literal.</param>
        /// <param name="lang">A language tag.</param>
        /// <returns></returns>
        public static string SerializeTranslatedString(string str, string lang)
        {
            return string.Format("{0}@{1}", SerializeString(str), lang);
        }

        /// <summary>
        /// Serializes a typed literal.
        /// </summary>
        /// <param name="obj">A value.</param>
        /// <param name="typeUri">A type URI.</param>
        /// <returns></returns>
        public static string SerializeTypedLiteral(object obj, Uri typeUri)
        {
            // SerializeUri, not the raw Uri: interpolating one calls Uri.ToString(), which returns the
            // display form and unescapes percent-encoding. See SerializeUri and ADR-0046.
            return string.Format("'{0}'^^{1}", XsdTypeMapper.SerializeObject(obj), SerializeUri(typeUri));
        }

        /// <summary>
        /// Serializes a value depdening on its type.
        /// </summary>
        /// <param name="obj">An object.</param>
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
        /// Serializes a DateTime object.
        /// </summary>
        /// <param name="date">A date time object.</param>
        /// <returns></returns>
        public static string SerializeDateTime(DateTime date)
        {
            return string.Format("'{0}'^^<http://www.w3.org/2001/XMLSchema#dateTime>", XmlConvert.ToString((DateTime)date, XmlDateTimeSerializationMode.Utc));
        }

        /// <summary>
        /// Serializes a URI.
        /// </summary>
        /// <param name="uri">A uniform resource identifier.</param>
        /// <returns></returns>
        public static string SerializeUri(Uri uri)
        {
            return uri.OriginalString.StartsWith("_") ? uri.OriginalString : $"<{uri.OriginalString}>";
        }

        /// <summary>
        /// The number of subjects bound by a single <c>VALUES</c> block before
        /// <see cref="GenerateSubjectBindings"/> starts a new one.
        /// </summary>
        /// <remarks>
        /// <c>VALUES</c> removes the nesting limit that sinks the equality chain, but it is not
        /// unbounded: Virtuoso compiles the block into one built-in call and refuses the 4095th
        /// operand with <c>SP030: Too many arguments for standard built-in function</c> (measured
        /// on 7.2.12 and 7.2.14 alike — a compile-time cap, not a query-text-length limit, and
        /// unmoved by <c>ThreadStackSize</c>). 1000 keeps a 4x margin under that, which matters
        /// because the ceiling is a property of the server build rather than of this code, and it
        /// is where the measured advantage already saturates: 1000 subjects answered in 83 ms
        /// against the chain's 957 ms. See <c>doc/adr/0046-bulk-subject-binding-with-values.md</c>.
        /// </remarks>
        internal const int SubjectBindingBatchSize = 1000;

        /// <summary>
        /// Binds a variable to one or more known subjects with <c>VALUES</c>.
        /// </summary>
        /// <remarks>
        /// This has to be emitted <b>before</b> the pattern it constrains. Restricting the
        /// subject afterwards with <c>FILTER (?s = ...)</c> instead leaves the engine to push the
        /// filter into a <c>UNION</c> containing an anti-join, which neither the in-memory engine
        /// nor GraphDB does — measured at 15x and 38x respectively, against 1.0x for the
        /// <c>VALUES</c> form. Binding up front turns every read into an indexed probe.
        /// <para>
        /// The equality chain it replaces is also a correctness problem, not only a slow one:
        /// Virtuoso parses <c>?s = &lt;a&gt; || ?s = &lt;b&gt; || ...</c> as nested binary pairs and
        /// its compiler caps the nesting depth, answering <c>SP031: The nesting depth of
        /// subexpressions exceed limits of SPARQL compiler</c> beyond it. The same subjects bound
        /// with <c>VALUES</c> compile fine.
        /// </para>
        /// </remarks>
        /// <param name="variable">The variable to bind, including its leading <c>?</c>.</param>
        /// <param name="uris">The subjects to bind it to.</param>
        internal static string GenerateSubjectBinding(string variable, IEnumerable<Uri> uris)
        {
            var result = new StringBuilder();

            result.Append("VALUES ").Append(variable).Append(" { ");

            foreach (Uri uri in uris)
            {
                result.Append(SerializeUri(uri)).Append(' ');
            }

            result.Append("} ");

            return result.ToString();
        }

        /// <summary>
        /// Splits a set of subjects into <c>VALUES</c> blocks of at most
        /// <paramref name="batchSize"/> entries, dropping the ones SPARQL cannot address.
        /// </summary>
        /// <remarks>
        /// Blank node identifiers are <b>skipped rather than serialized</b>. A blank node label is
        /// not a legal <c>DataBlockValue</c> (<c>iri | RDFLiteral | NumericLiteral |
        /// BooleanLiteral | 'UNDEF'</c>) and is not legal in a <c>FILTER</c> expression either, so
        /// there is no query shape that can address one by label — a label in a query is an
        /// existential variable, not a reference. <see cref="SerializeUri"/> would happily emit a
        /// bare <c>_:b0</c> and the store would reject the whole query, taking the addressable
        /// subjects down with it. Callers that need the stricter contract reject blank ids before
        /// calling; <c>ResourceCache.LoadCachedValues</c> instead materializes whatever does not
        /// come back as an unresolved resource, which is the right outcome here.
        /// <para>
        /// Yields nothing for an empty, all-blank or <c>null</c> subject set, so a caller that
        /// iterates the result issues no query at all rather than an unconstrained one.
        /// </para>
        /// </remarks>
        /// <param name="variable">The variable to bind, including its leading <c>?</c>.</param>
        /// <param name="uris">The subjects to bind it to.</param>
        /// <param name="batchSize">The maximum number of subjects per block.</param>
        internal static IEnumerable<string> GenerateSubjectBindings(string variable, IEnumerable<Uri> uris, int batchSize = SubjectBindingBatchSize)
        {
            if (batchSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize));
            }

            if (uris == null)
            {
                yield break;
            }

            var batch = new List<Uri>(batchSize);

            foreach (Uri uri in uris)
            {
                if (uri == null || (uri is UriRef uriRef && uriRef.IsBlankId))
                {
                    continue;
                }

                batch.Add(uri);

                if (batch.Count == batchSize)
                {
                    yield return GenerateSubjectBinding(variable, batch);

                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                yield return GenerateSubjectBinding(variable, batch);
            }
        }

        /// <summary>
        /// Serializes a resource.
        /// </summary>
        /// <param name="resource">A resource.</param>
        /// <param name="ignoreUnmappedProperties">Ignores all unmapped properties for serialization.</param>
        /// <returns></returns>
        public static string SerializeResource(IResource resource, bool ignoreUnmappedProperties=false)
        {
            var valueList = resource.ListValues(ignoreUnmappedProperties);

            if (!valueList.Any())
            {
                return string.Empty;
            }

            string subject = SerializeUri(resource.Uri);

            StringBuilder result = new StringBuilder(subject);
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
        /// Serializes a property and one of its values as a SPARQL <c>predicate object</c> fragment.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <returns>A SPARQL fragment such as <c>&lt;http://…/name&gt; 'Alice'</c>.</returns>
        internal static string SerializePredicateObject(Property property, object value)
        {
            return string.Format("{0} {1}", SerializeUri(property.Uri), SerializeValue(value));
        }

        /// <summary>
        /// Computes the difference between a resource's current values and the values last known to be
        /// in the backing store, so that a commit writes only what the caller actually changed.
        /// </summary>
        /// <remarks>
        /// This is what keeps a commit from overwriting a whole resource. Two callers that each load the
        /// same resource and add a different value both keep their addition, because neither one's update
        /// mentions the other's triple. Without it, the second writer's full re-serialization — which
        /// never contained the first writer's value — silently erases it.
        ///
        /// Removals are computed against the resource's complete value list, never the filtered one, so
        /// <paramref name="ignoreUnmappedProperties"/> can only ever suppress a write, never cause a
        /// delete.
        /// </remarks>
        /// <param name="resource">The resource being committed.</param>
        /// <param name="ignoreUnmappedProperties">Set this to true to write only mapped properties.</param>
        /// <param name="deleteTriples">Receives the <c>predicate object</c> fragments to remove.</param>
        /// <param name="insertTriples">Receives the <c>predicate object</c> fragments to add.</param>
        /// <returns>
        /// False if the resource has never been synchronized, in which case no delta can be computed and
        /// the caller must fall back to replacing the resource wholesale.
        /// </returns>
        public static bool TrySerializeResourceDelta(Resource resource, bool ignoreUnmappedProperties, out List<string> deleteTriples, out List<string> insertTriples)
        {
            deleteTriples = null;
            insertTriples = null;

            var persisted = resource.PersistedValues;

            if (persisted == null)
            {
                return false;
            }

            var current = SerializeValueSet(resource, ignoreUnmappedProperties);

            // Removals are judged against everything the resource currently holds, so a value excluded
            // from the write by the filter is never mistaken for a deletion.
            var currentForRemoval = ignoreUnmappedProperties ? SerializeValueSet(resource, false) : current;

            deleteTriples = persisted.Where(t => !currentForRemoval.Contains(t)).ToList();
            insertTriples = current.Where(t => !persisted.Contains(t)).ToList();

            return true;
        }

        /// <summary>
        /// The resource's values as <c>predicate object</c> fragments.
        /// </summary>
        /// <remarks>
        /// Internal rather than private because a layered model stages a resource that has no snapshot
        /// yet - a newly created one - by treating every value it holds as an addition, which needs the
        /// same fragments the delta writer compares.
        /// </remarks>
        internal static HashSet<string> SerializeValueSet(IResource resource, bool ignoreUnmappedProperties)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);

            foreach (var value in resource.ListValues(ignoreUnmappedProperties))
            {
                if (value.Item2 == null)
                {
                    continue;
                }

                result.Add(SerializePredicateObject(value.Item1, value.Item2));
            }

            return result;
        }

        /// <summary>
        /// Generate the dataset clause for a given model.
        /// </summary>
        /// <param name="model">A model.</param>
        /// <returns></returns>
        public static string GenerateDatasetClause(IModel model)
        {
            if (model == null)
            {
                return "";
            }

            if (model is ILayeredModel layered)
            {
                // A materialized view holds its effective triples in one ordinary graph, so a plain
                // FROM is right. Otherwise FROM would merge the three layers into the default graph,
                // which is union - the overlay needs them addressable by name instead.
                return layered.IsMaterialized
                    ? "FROM " + SerializeUri(layered.Materialized.Uri) + " "
                    : LayeredModelSparql.NamedDatasetClause(layered);
            }

            if (model is IModelGroup)
            {
                return GenerateDatasetClause(model as IModelGroup);
            }

            return "FROM " + SerializeUri(model.Uri) + " ";
        }

        /// <summary>
        /// Generate a dataset clause for a model group.
        /// </summary>
        /// <param name="modelGroup">A model group.</param>
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
        /// Generate a dataset clause for an enumeration of models.
        /// </summary>
        /// <param name="models">An enumeration of models.</param>
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
        /// Serialize a count query for the given SPARQL query.
        /// </summary>
        /// <param name="model">The model to be queried.</param>
        /// <param name="query">The query which results should be counted.</param>
        /// <returns></returns>
        public static string SerializeCount(IModel model, ISparqlQuery query)
        {
            string variable = "?" + query.GetGlobalScopeVariableNames()[0];
            string from = GenerateDatasetClause(model);
            string where = query.GetRootGraphPattern();

            StringBuilder queryBuilder = new StringBuilder();

            queryBuilder.Append("SELECT ( COUNT(DISTINCT ");
            queryBuilder.Append(variable);
            queryBuilder.Append(") AS ?count )");
            queryBuilder.Append(from);
            queryBuilder.Append(" WHERE { ");
            queryBuilder.Append(where);
            queryBuilder.Append(" }");

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Generate a query which returns the URIs of all resources selected in a given query.
        /// </summary>
        /// <param name="model">The model to be queried.</param>
        /// <param name="query">The SPARQL query which provides resources.</param>
        /// <param name="offset">Offset solution modifier.</param>
        /// <param name="limit">Limit solution modifier.</param>
        /// <returns></returns>
        public static string SerializeFetchUris(IModel model, ISparqlQuery query, int offset = -1, int limit = -1)
        {
            string variable = "?" + query.GetGlobalScopeVariableNames()[0];
            string from = GenerateDatasetClause(model);
            string where = query.GetRootGraphPattern();
            string orderby = query.GetRootOrderByClause();

            StringBuilder queryBuilder = new StringBuilder();
            
            foreach(string prefix in query.GetDeclaredPrefixes())
            {
                queryBuilder.Append($"PREFIX <{prefix}> ");
            }

            queryBuilder.Append("SELECT DISTINCT ");
            queryBuilder.Append(variable);
            queryBuilder.Append(from);
            queryBuilder.Append(" WHERE { ");
            queryBuilder.Append(where);
            queryBuilder.Append(" } ");
            queryBuilder.Append(orderby);

            if (offset != -1)
            {
                queryBuilder.Append(" OFFSET ");
                queryBuilder.Append(offset);
            }

            if (limit != -1)
            {
                queryBuilder.Append(" LIMIT ");
                queryBuilder.Append(limit);
            }

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Add an offset or limit solution modifier to a given SPARQL query.
        /// </summary>
        /// <param name="model">The model to be queried.</param>
        /// <param name="query">The SPARQL query to be executed.</param>
        /// <param name="offset">Offset solution modifier.</param>
        /// <param name="limit">Limit solution modifier.</param>
        /// <returns></returns>
        public static string SerializeOffsetLimit(IModel model, ISparqlQuery query, int offset = -1, int limit = -1)
        {
            string variable = "?" + query.GetGlobalScopeVariableNames()[0];
            string from = GenerateDatasetClause(model);
            string where = query.GetRootGraphPattern();

            // The inner pattern already carries the overlay - it came from the query root graph
            // pattern - but the outer triple pattern this method adds does not, and unguarded it
            // would fetch the paged resources triples straight from the baseline, removals
            // included. Wrap it too.
            // A materialized view needs no guard here: its effective triples are already one graph,
            // which the dataset clause above selects.
            string outer = model is ILayeredModel layeredModel && !layeredModel.IsMaterialized
                ? LayeredModelSparql.Overlay(layeredModel, variable, "?p", "?o")
                : string.Format("{0} ?p ?o", variable);

            StringBuilder resultBuilder = new StringBuilder();
            resultBuilder.AppendFormat("SELECT {0} ?p ?o {1} WHERE {{ {2} {{", variable, from, outer);
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
