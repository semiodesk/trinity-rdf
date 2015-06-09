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

Copyright (c) 2015 Semiodesk GmbH

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Diagnostics;
using VDS.RDF.Query;
using VDS.RDF;

namespace Semiodesk.Trinity.Store
{
    class SparqlEndpointQueryResult : ISparqlQueryResult
    {
        #region Members
        SparqlResultSet _queryResults;
        IModel _model;
        SparqlQuery _query;
        #endregion

        #region Constructor
        public SparqlEndpointQueryResult(SparqlResultSet result, SparqlQuery query)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            _queryResults = result;
            _model = query.Model;
            _query = query;
        }
        #endregion


        public bool GetAnwser()
        {
            if (_query.QueryType == SparqlQueryType.Ask && _queryResults.ResultsType == SparqlResultsType.Boolean)
            {
                return _queryResults.Result;
            }
            throw new InvalidQueryException("The given query was not an ask query or could not be processed as such.");
        }

        public IEnumerable<Resource> GetResources()
        {
            return GetResources<Resource>();
        }

        protected object ParseValue(INode value)
        {
            switch (value.NodeType)
            {
                case (NodeType.Literal):
                {
                    return XsdTypeMapper.DeserializeLiteralNode(value as BaseLiteralNode);
                }
                case (NodeType.Uri):
                {
                    return new Resource((value as UriNode).Uri);
                }
                case (NodeType.GraphLiteral):
                {
                    return null;
                }
                case (NodeType.Variable):
                {
                    return null;
                }
                default:
                {
                    return null;
                }
            }
        }

        public IEnumerable<T> GetResources<T>() where T : Resource
        {
            if (_query.ProvidesStatements())
            {
                return GenerateResources<T>();
            }
            else
            {
                throw new ArgumentException("Error: The given SELECT query cannot be resolved into statements.");
            }
        }

        private IEnumerable<T> GenerateResources<T>() where T : Resource
        {
            List<T> result = new List<T>();

            if (0 < _queryResults.Results.Count)
            {
                // A dictionary mapping URIs to the generated resource objects.
                Dictionary<string, IResource> cache = new Dictionary<string, IResource>();

                Dictionary<string, T> types = FindResourceTypes<T>(
                    _queryResults.Variables.ElementAt(0),
                    _queryResults.Variables.ElementAt(1),
                    _queryResults.Variables.ElementAt(2),
                    _query.InferenceEnabled);

                foreach (KeyValuePair<string, T> resourceType in types)
                {
                    cache.Add(resourceType.Key, resourceType.Value);
                }

                // A handle to the currently built resource which may spare the lookup in the dictionary.
                T currentResource = null;

                foreach (SparqlResult row in _queryResults.Results)
                {
                    Uri s, p;
                    object o;

                    if (_query.QueryType == SparqlQueryType.Describe || _query.QueryType == SparqlQueryType.Construct)
                    {
                        s = new Uri(row[0].ToString());
                        p = new Uri(row[1].ToString());
                        o = ParseValue(row[2]);
                    }
                    else if (_query.QueryType == SparqlQueryType.Select)
                    {
                        s = new Uri(row[_query.SubjectVariable].ToString());
                        p = new Uri(row[_query.PredicateVariable].ToString());
                        o = ParseValue(row[_query.ObjectVariable]);
                    }
                    else
                    {
                        break;
                    }

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
                            currentResource = (T)Activator.CreateInstance(typeof(T), s);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = true;
                            currentResource.SetModel( _model);

                            cache.Add(s.OriginalString, currentResource);
                            result.Add(currentResource);
                        }
                        catch
                        {
#if DEBUG
                            Debug.WriteLine("[SparqlQueryResult] Info: Could not create resource " + s.OriginalString);
#endif

                            continue;
                        }
                    }

                    if (o is Uri)
                    {
                        Uri uri = o as Uri;

                        if (cache.ContainsKey(uri.OriginalString))
                        {
                            currentResource.AddProperty(new Property(p), cache[uri.OriginalString], true);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = false;
                            currentResource.SetModel( _model);
                        }
                        else
                        {
                            Resource r = new Resource(uri);
                            r.IsNew = false;

                            cache.Add(uri.OriginalString, r);
                            currentResource.AddProperty(new Property(p), r, true);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = false;
                            currentResource.SetModel( _model);
                        }
                    }
                    else
                    {
                        currentResource.AddProperty(new Property(p), o, true);
                    }
                }
            }

            foreach (T r in result)
            {
                yield return r;
            }
        }

        public IEnumerable<BindingSet> GetBindings()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method gets the RDF classes from the query result 
        /// and tries to match it to a C# class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subjectColumn"></param>
        /// <param name="preducateColumn"></param>
        /// <param name="objectColumn"></param>
        /// <returns></returns>
        private Dictionary<string, T> FindResourceTypes<T>(string subjectColumn, string preducateColumn, string objectColumn, bool inferencingEnabled = false) where T : Resource
        {
            Dictionary<string, T> result = new Dictionary<string, T>();
            Dictionary<string, List<Class>> types = new Dictionary<string, List<Class>>();
            string s, p, o;

            // Collect all types for every resource in the types dictionary.
            // I was going to use _queryResults.Select(), but that doesn't work with Virtuoso.
            foreach (SparqlResult row in _queryResults)
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

   
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #region IResourceQueryResult Members

        public int Count()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Resource> GetResources(int offset = -1, int limit = -1)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetResources<T>(int offset = -1, int limit = -1) where T : Resource
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
