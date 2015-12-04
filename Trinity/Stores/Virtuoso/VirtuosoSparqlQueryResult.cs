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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Data;
using OpenLink.Data.Virtuoso;
using System.Globalization;
using System.Diagnostics;
using System.Xml;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// The results returned from a SPARQL query.
    /// </summary>
    internal class VirtuosoSparqlQueryResult : ISparqlQueryResult, IResourceQueryResult
    {
        #region Members

        private readonly IModel _model;
        private readonly VirtuosoStore _store;

        private SparqlQuery _query;

        //private DataTable _queryResults;

        private readonly ITransaction _transaction;

        bool IsSorted
        {
            get { return _query.IsSorted(); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Internal constructor which parses the results returned from a given query.
        /// </summary>
        /// <param name="query">The executed query.</param>
        /// <param name="store"></param>
        /// <param name="transaction"></param>
        /// <param name="model"></param>
        internal VirtuosoSparqlQueryResult(IModel model, SparqlQuery query, VirtuosoStore store, ITransaction transaction=null)
        {
            _store = store;
            _transaction = transaction;
            _query = query;
            _model = model;

        }

        #endregion

        #region Methods

        /// <summary>
        /// Takes a data value from the _queryResults datatable and returns a marshalled data object.
        /// </summary>
        /// <param name="cellValue">A cell value from a Virtuoso results datatable.</param>
        /// <returns>A reference to the marshalled data object.</returns>
        private object ParseCellValue(object cellValue)
        {
            if (cellValue is SqlExtendedString)
            {
                SqlExtendedString extendedString = cellValue as SqlExtendedString;

                if (extendedString.IsResource())
                {
                    // NOTE: We create an UriRef for correct equality comparision with fragment identifiers.
                    return new UriRef(extendedString.ToString(), UriKind.RelativeOrAbsolute);
                }
                else if (extendedString.IsString() || extendedString.IsBlankId())
                {
                    return extendedString.ToString();
                }
            }
            else if (cellValue is SqlRdfBox)
            {
                SqlRdfBox box = cellValue as SqlRdfBox;

                if (box.StrType != null)
                {
                    try
                    {
                        // NOTE: We create an UriRef for correct equality comparision with fragment identifiers.
                        return XsdTypeMapper.DeserializeString(box.Value.ToString(), new UriRef(box.StrType));
                    }
                    catch (KeyNotFoundException)
                    {
                        // The given data type is not known by the XsdTypeMapper.
                        return box.Value.ToString();
                    }
                }
                else if (box.Value is SqlExtendedString && box.StrLang != null)
                {
                    return new Tuple<string, CultureInfo>(box.Value.ToString(), new CultureInfo(box.StrLang));
                }
                else
                {
                    return box.Value.ToString();
                }
            }
            else if (cellValue is int)
            {
                //TODO: We need a different approach to store and read boolean s
                return cellValue;
                /*
                if ((int)cellValue == 1)
                    return true;
                else
                    return false;
                */

            }
            else if (cellValue is DateTime)
            {
                // Virtuoso delivers the time not as UTC but as "unspecified"
                // we convert it to local time
                DateTime res = ((DateTime)cellValue).ToLocalTime();
                return res;
            }

            //else if (cellValue is VirtuosoDateTimeOffset)
            //{
            //    return ((VirtuosoDateTimeOffset)cellValue).Value.UtcDateTime.ToUniversalTime();
            //}
            

            return cellValue;
        }

        /// <summary>
        /// Generates BindingSet object from the data in _queryResults.
        /// </summary>
        /// <returns>An enumeration of BindingSet objects.</returns>
        private IEnumerable<BindingSet> GenerateBindings(DataTable queryResults)
        {
            foreach (DataRow row in queryResults.Rows)
            {
                BindingSet binding = new BindingSet();

                foreach (DataColumn column in queryResults.Columns)
                {
                    binding[column.Caption] = ParseCellValue(row[column]);
                }

                yield return binding;
            }
        }

        /// <summary>
        /// Tries to marshall the data in the _queryResults variable as Resource objects.
        /// </summary>
        /// <typeparam name="T">The Resource type.</typeparam>
        /// <returns>An enumeration of marshalled objects of the given type.</returns>
        private IEnumerable<T> GenerateResources<T>(DataTable queryResults) where T : Resource
        {
            List<T> result = new List<T>();

            if (0 < queryResults.Columns.Count)
            {
                // A dictionary mapping URIs to the generated resource objects.
                Dictionary<string, IResource> cache = new Dictionary<string, IResource>();

                Dictionary<string, T> types = FindResourceTypes<T>(
                    queryResults,
                    queryResults.Columns[0].ColumnName,
                    queryResults.Columns[1].ColumnName,
                    queryResults.Columns[2].ColumnName,
                    _query.InferenceEnabled);

                foreach (KeyValuePair<string, T> resourceType in types)
                {
                    cache.Add(resourceType.Key, resourceType.Value);
                }

                // A handle to the currently built resource which may spare the lookup in the dictionary.
                T currentResource = null;

                foreach (DataRow row in queryResults.Rows)
                {
                    // NOTE: We create an UriRef for correct equality comparision with fragment identifiers.
                    UriRef s, predUri;
                    Property p;
                    object o;

                    if (_query.QueryType == SparqlQueryType.Describe ||
                        _query.QueryType == SparqlQueryType.Construct)
                    {
                        s = new UriRef(row[0].ToString());
                        predUri = new UriRef(row[1].ToString());
                        o = ParseCellValue(row[2]);
                    }
                    else if (_query.QueryType == SparqlQueryType.Select)
                    {
                        s = new UriRef(row[_query.SubjectVariable].ToString());
                        predUri = new UriRef(row[_query.PredicateVariable].ToString());
                        o = ParseCellValue(row[_query.ObjectVariable]);
                    }
                    else
                    {
                        break;
                    }

                    p = OntologyDiscovery.GetProperty(predUri);

                    if (currentResource != null && currentResource.Uri.OriginalString == s.OriginalString)
                    {
                        // We already have the handle to the resource which the property should be added to.
                    }
                    else if (cache.ContainsKey(s.OriginalString))
                    {
                        currentResource = cache[s.OriginalString] as T;

                        // In this case we may have encountered a resource which was 
                        // added to the cache by the object value handler below.
                        if (!result.Contains(currentResource))
                        {
                            result.Add(currentResource);
                        }
                    }
                    else
                    {
                        try
                        {
                            currentResource = (T) Activator.CreateInstance(typeof (T), s);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = true;
                            currentResource.SetModel(_model);

                            cache.Add(s.OriginalString, currentResource);
                            result.Add(currentResource);
                        }
                        catch
                        {
#if DEBUG
                            Debug.WriteLine("[SparqlQueryResult] Info: Could not create resource " +
                                            s.OriginalString);
#endif

                            continue;
                        }
                    }

                    if (currentResource == null) continue;

                    if (o is UriRef)
                    {
                        UriRef uri = o as UriRef;

                        if (cache.ContainsKey(uri.OriginalString))
                        {
                            currentResource.AddProperty(p, cache[uri.OriginalString], true);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = false;
                            currentResource.SetModel(_model);
                        }
                        else
                        {
                            Resource r = new Resource(uri);
                            r.IsNew = false;

                            cache.Add(uri.OriginalString, r);
                            currentResource.AddProperty(p, r, true);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = false;
                            currentResource.SetModel(_model);
                        }
                    }
                    else
                    {
                        currentResource.AddProperty(p, o, true);
                    }
                }
            }

            foreach (T r in result)
            {
                yield return r;
            }

        }

        /// <summary>
        /// This method gets the RDF classes from the query result 
        /// and tries to match it to a C# class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subjectColumn"></param>
        /// <param name="preducateColumn"></param>
        /// <param name="objectColumn"></param>
        /// <param name="queryResults"></param>
        /// <param name="inferencingEnabled"></param>
        /// <returns></returns>
        private Dictionary<string, T> FindResourceTypes<T>(DataTable queryResults, string subjectColumn, string preducateColumn, string objectColumn, bool inferencingEnabled = false) where T : Resource
        {
            Dictionary<string, T> result = new Dictionary<string, T>();
            Dictionary<string, List<Class>> types = new Dictionary<string, List<Class>>();
            string s, p, o;

            // Collect all types for every resource in the types dictionary.
            // I was going to use _queryResults.Select(), but that doesn't work with Virtuoso.
            foreach (DataRow row in queryResults.Rows)
            {
                s = row[subjectColumn].ToString();
                p = row[preducateColumn].ToString();

                if (p == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
                {
                    o = row[objectColumn].ToString();

                    if (!types.ContainsKey(s))
                    {
                        types.Add(s, new List<Class>());
                    }

                    if (OntologyDiscovery.Classes.ContainsKey(o))
                    {
                        types[s].Add(OntologyDiscovery.Classes[o]);
                    }
                    else
                    {
                        types[s].Add(new Class(new Uri(o)));
                    }
                }
            }

            // Iterate over all types and find the right class and instatiate it.
            foreach (string subject in types.Keys)
            {
                IList<Type> classType = MappingDiscovery.GetMatchingTypes(types[subject], typeof(T), inferencingEnabled);

                if (classType.Count > 0)
                {
                    #if DEBUG
                    if (classType.Count > 1)
                    {
                        string msg = "Info: There is more that one assignable type for <{0}>. It was initialized using the first.";
                        Debug.WriteLine(string.Format(msg, subject));
                    }
                    #endif

                    T resource = (T)Activator.CreateInstance(classType[0], new Uri(subject));
                    resource.SetModel(_model);
                    resource.IsNew = false;
                    result[subject] = resource;
                }
                #if DEBUG
                else if (typeof(T) != typeof(Resource))
                {
                    string msg = "Info: No assignable type found for <{0}>.";

                    if (inferencingEnabled)
                    {
                        msg += " Try disabling inference.";
                    }

                    Debug.WriteLine(string.Format(msg, subject));  
                }
                #endif
            }

            return result;
        }

        /// <summary>
        /// Returns the bool value from ASK query forms.
        /// </summary>
        /// <returns>True on success, False otherwise.</returns>
        public bool GetAnwser()
        {
            using (DataTable queryResults = _store.ExecuteQuery(_store.CreateQuery(_query), _transaction))
            {

                if (queryResults.Rows.Count > 0)
                {
                    return ((int) queryResults.Rows[0][0] != 0);
                }
                else
                {
                    return false;
                }
            }
        }

        public int Count()
        {
            string countQuery = SparqlSerializer.SerializeCount(_model, _query);
            SparqlQuery query = new SparqlQuery(countQuery);
            query.InferenceEnabled = _query.InferenceEnabled;

            string q = _store.CreateQuery(query);

            foreach (BindingSet b in GenerateBindings(_store.ExecuteQuery(q)))
            {
                return (int)b["count"];
            }

            return -1;
        }

        public IEnumerable<Resource> GetResources(int offset = -1, int limit = -1)
        {
            return GetResources<Resource>(offset, limit);
        }

        public IEnumerable<T> GetResources<T>(int offset = -1, int limit = -1) where T : Resource
        {
            if (!_query.ProvidesStatements())
            {
                throw new ArgumentException("Error: The given SELECT query cannot be resolved into statements.");
            }

            if (!_query.InferenceEnabled)
            {
                String queryString = SparqlSerializer.SerializeOffsetLimit(_model, _query, offset, limit);

                SparqlQuery query = new SparqlQuery(queryString);

                using (DataTable queryResults = _store.ExecuteQuery(_store.CreateQuery(query), _transaction))
                {
                    foreach (T t in GenerateResources<T>(queryResults))
                    {
                        yield return t;
                    }
                }
            }
            else
            {
                // TODO: Make resources which are returned from a inferenced query read-only in order to improve query performance.

                // NOTE: When inferencing is enabled, we are unable to determine which triples were inferred and
                // which not. Therefore we need to issue a query to get the URIs of all the resources the original
                // query would return and issue another query to describe those resources withoud inference.
                List<UriRef> uris = FetchUris(offset, limit).ToList();

                if (!uris.Count.Equals(0))
                {
                    StringBuilder queryBuilder = new StringBuilder();

                    foreach (Uri uri in uris)
                    {
                        queryBuilder.Append(SparqlSerializer.SerializeUri(uri));
                    }

                    SparqlQuery query = new SparqlQuery(string.Format("DESCRIBE {0}", queryBuilder.ToString()));

                    ISparqlQueryResult queryResult = _model.ExecuteQuery(query);

                    if (IsSorted)
                    {
                        foreach (T t in queryResult.GetResources<T>().OrderBy(o => uris.IndexOf(o.Uri)))
                        {
                            yield return t;
                        }
                    }
                    else
                    {
                        foreach (T t in queryResult.GetResources<T>())
                        {
                            yield return t;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns marshalled Resource objects returned from DESCRIBE, CONSTRUCT 
        /// or interpretable SELECT query forms.
        /// </summary>
        /// <returns>An enumeration of Resource objects.</returns>
        public IEnumerable<Resource> GetResources()
        {
            return GetResources<Resource>();
        }

        /// <summary>
        /// Returns marshalled instances of the given Resource type which were 
        /// returned from DESCRIBE, CONSTRUCT or interpretable SELECT query forms.
        /// </summary>
        /// <typeparam name="T">The Resource type object.</typeparam>
        /// <returns>An enumeration of instances of the given type.</returns>
        public IEnumerable<T> GetResources<T>() where T : Resource
        {
            if (_query.ProvidesStatements())
            {
                using (DataTable queryResults = _store.ExecuteQuery(_store.CreateQuery(_query), _transaction))
                {
                    return GenerateResources<T>(queryResults);
                }
            }
            else
            {
                throw new ArgumentException("Error: The given SELECT query cannot be resolved into statements.");
            }
        }

        /// <summary>
        /// Returns a set of bound values (bindings) returned from SELECT query forms.
        /// </summary>
        /// <returns>An enumeration of bound solution variables (BindingSet).</returns>
        public IEnumerable<BindingSet> GetBindings()
        {
            try
            {
                using (DataTable queryResults = _store.ExecuteQuery(_store.CreateQuery(_query), _transaction))
                {
                    return GenerateBindings(queryResults);
                }
            }
            #if DEBUG
            catch (Exception e)
            {
                
                Debug.WriteLine(e);
                

                return null;
            }
            #else
            catch (Exception)
            {
                return null;
            }
            #endif
        }

        /// <remarks>
        /// It is important to return UriRefs for correct equality comparision of URIs with fragment identifiers.
        /// </remarks>
        IEnumerable<UriRef> FetchUris(int offset, int limit)
        {
            String queryString = SparqlSerializer.SerializeFetchUris(_model, _query, offset, limit);

            SparqlQuery query = new SparqlQuery(queryString) { InferenceEnabled = _query.InferenceEnabled };

            using (DataTable queryResults = _store.ExecuteQuery(_store.CreateQuery(query), _transaction))
            {
                IEnumerable<BindingSet> bindings = GenerateBindings(queryResults);

                UriRef previousUri = null;

                foreach (BindingSet binding in bindings)
                {
                    UriRef currentUri = binding[_query.SubjectVariable] as UriRef;

                    if (currentUri == null) continue;

                    if (!currentUri.Equals(previousUri))
                    {
                        yield return currentUri;
                    }

                    previousUri = currentUri;
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
        }

        #endregion
    }

    internal static class SqlExtendedStringExtensions
    {
        public static bool IsResource(this SqlExtendedString extendedString)
        {
            return extendedString.IriType == SqlExtendedStringType.IRI && extendedString.StrType == SqlExtendedStringType.IRI;
        }

        public static bool IsString(this SqlExtendedString extendedString)
        {
            return extendedString.IriType == SqlExtendedStringType.IRI && extendedString.StrType == SqlExtendedStringType.BNODE;
        }

        public static bool IsBlankId(this SqlExtendedString extendedString)
        {
            return extendedString.IriType == SqlExtendedStringType.BNODE;
        }
    }  
}
