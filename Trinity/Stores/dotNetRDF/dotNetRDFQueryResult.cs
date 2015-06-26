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
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Query;


namespace Semiodesk.Trinity.Store
{
    interface ITripleProvider
    {
        bool HasNext { get; }
        void SetNext();
        Uri S { get; }
        Uri P { get; }
        INode O { get; }
        int Count { get; }
        void Reset();


    }

    class GraphTripleProvider : ITripleProvider
    {
        IGraph _graph;
        int counter;
        public GraphTripleProvider(IGraph graph)
        {
            _graph = graph;
            counter = 0;
        }

        public int Count
        {
            get { return _graph.Triples.Count; }
        }

        public void Reset()
        {
            counter = 0;
        }


        public bool HasNext
        {
            get { return counter < _graph.Triples.Count; }
        }

        public void SetNext()
        {
            counter += 1;
        }

        public Uri S
        {
            get { return (_graph.Triples.ElementAt(counter).Subject as UriNode).Uri; }
        }

        public Uri P
        {
            get { return (_graph.Triples.ElementAt(counter).Predicate as UriNode).Uri; }
        }

        public INode O
        {
            get { return _graph.Triples.ElementAt(counter).Object; }
        }
    }

    class SparqlResultSetTripleProvider : ITripleProvider
    {
        SparqlResultSet _set;
        string _subjectVar;
        string _predicateVar;
        string _objectVar;

        int counter;
        public SparqlResultSetTripleProvider(SparqlResultSet set, string subjectVar, string predicateVar, string objectVar)
        {
            _set = set;
            counter = 0;

            _subjectVar = subjectVar;
            _predicateVar = predicateVar;
            _objectVar = objectVar;
        }

        public int Count
        {
            get { return _set.Count; }
        }

        public void Reset()
        {
            counter = 0;
        }


        public bool HasNext
        {
            get { return counter < _set.Count; }
        }

        public void SetNext()
        {
            counter += 1;
        }

        public Uri S
        {
            get { return (_set[counter][_subjectVar] as UriNode).Uri; }
        }

        public Uri P
        {
            get { return (_set[counter][_predicateVar] as UriNode).Uri; }
        }

        public INode O
        {
            get { return _set[counter][_objectVar]; }
        }
    }


    class dotNetRDFQueryResult : ISparqlQueryResult
    {
        #region Members

        private SparqlQuery _query;
        private IModel _model;
        private ITripleProvider _tripleProvider;
        private SparqlResultSet _resultSet;
        private dotNetRDFStore _store;

        #endregion

        #region Constructor
        public dotNetRDFQueryResult(dotNetRDFStore store, SparqlQuery query, SparqlResultSet resultSet)
        {
            _query = query;
            _tripleProvider = new SparqlResultSetTripleProvider(resultSet, _query.SubjectVariable, _query.PredicateVariable, _query.ObjectVariable);
            _model = query.Model;
            _resultSet = resultSet;
            _store = store;
        }

        public dotNetRDFQueryResult(dotNetRDFStore store, SparqlQuery query, IGraph graph)
        {
            _query = query;
            _tripleProvider = new GraphTripleProvider(graph);
            _model = query.Model;
            _store = store;
        }
        #endregion

        #region Methods

        #region ISparqlQueryResult
        public bool GetAnwser()
        {
            if (_query.QueryType == SparqlQueryType.Ask)
            {
                return _resultSet.Result;
            }
            else
            {
                throw new Exception();
            }
        }

        public IEnumerable<BindingSet> GetBindings()
        {
            List<BindingSet> result = new List<BindingSet>();
            if (_query.QueryType == SparqlQueryType.Select)
            {
                foreach (var x in _resultSet)
                {
                    BindingSet r = new BindingSet();
                    foreach (var y in x)
                    {
                        if (y.Value != null)
                            r.Add(y.Key, ParseCellValue(y.Value));
                    }
                    result.Add(r);
                }


            }

            return result;
        }

        public IEnumerable<T> GetResources<T>() where T : Resource
        {

            if (_query.ProvidesStatements())
            {
                return GenerateResources<T>();
            }

            throw new ArgumentException("Error: The given query cannot be resolved into statements.");

        }

        private IEnumerable<T> GenerateResources<T>() where T : Resource
        {
            List<T> result = new List<T>();
            if (0 < _tripleProvider.Count)
            {
                // A dictionary mapping URIs to the generated resource objects.
                Dictionary<string, IResource> cache = new Dictionary<string, IResource>();

                Dictionary<string, T> types = FindResourceTypes<T>(_query.InferenceEnabled);
                //Dictionary<string, T> types = new Dictionary<string, T>();
                _tripleProvider.Reset();

                foreach (KeyValuePair<string, T> resourceType in types)
                {
                    cache.Add(resourceType.Key, resourceType.Value);
                }

                // A handle to the currently built resource which may spare the lookup in the dictionary.
                T currentResource = null;

                while (_tripleProvider.HasNext)
                {
                    Uri s, predUri;
                    INode o;
                    Property p;


                    s = _tripleProvider.S;
                    predUri = _tripleProvider.P;
                    o = _tripleProvider.O;
                    _tripleProvider.SetNext();

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
                            currentResource = (T)Activator.CreateInstance(typeof(T), s);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = true;
                            currentResource.Model = _model;

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

                    if (o is IUriNode)
                    {
                        Uri uri = (o as IUriNode).Uri;

                        if (cache.ContainsKey(uri.OriginalString))
                        {
                            currentResource.AddProperty(p, cache[uri.OriginalString], true);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = false;
                            currentResource.Model = _model;
                        }
                        else
                        {
                            Resource r = new Resource(uri);
                            r.IsNew = false;

                            cache.Add(uri.OriginalString, r);
                            currentResource.AddProperty(p, r, true);
                            currentResource.IsNew = false;
                            currentResource.IsSynchronized = false;
                            currentResource.Model = _model;
                        }
                    }
                    else if( o is BlankNode )
                    {
                    }else
                    {
                        currentResource.AddProperty(p, ParseCellValue(o), true);
                    }
                }
            }

            foreach (T r in result)
            {
                yield return r;
            }
        }

        private object ParseCellValue(INode p)
        {
            if (p.NodeType == NodeType.Uri)
                return (p as IUriNode).Uri;
            else if (p.NodeType == NodeType.Literal)
            {
                ILiteralNode literalNode = p as ILiteralNode;
                if (literalNode.DataType == null)
                    return literalNode.Value;
                return XsdTypeMapper.DeserializeString(literalNode.Value, literalNode.DataType);
            }
            return null;
        }


        private Dictionary<string, T> FindResourceTypes<T>(bool inferencingEnabled)
            where T : Resource
        {
            Dictionary<string, T> result = new Dictionary<string, T>();
            Dictionary<string, List<Class>> types = new Dictionary<string, List<Class>>();
            string s, p;
            INode o;

            // Collect all types for every resource in the types dictionary.
            // I was going to use _queryResults.Select(), but that doesn't work with Virtuoso.
            while (_tripleProvider.HasNext)
            {
                s = _tripleProvider.S.ToString();
                p = _tripleProvider.P.ToString();
                o = _tripleProvider.O;

                _tripleProvider.SetNext();

                if (p.ToString() == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
                {
                    string obj = ((IUriNode)o).Uri.OriginalString;

                    if (!types.ContainsKey(s))
                    {
                        types.Add(s, new List<Class>());
                    }

                    if (OntologyDiscovery.Classes.ContainsKey(obj))
                    {
                        types[s].Add(OntologyDiscovery.Classes[obj]);
                    }
                    else
                    {
                        types[s].Add(new Class(new Uri(obj)));
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
                    resource.Model = _model;
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

        public IEnumerable<Resource> GetResources()
        {
            return GetResources<Resource>();
        }

        public virtual int Count()
        {
            string countQuery = SparqlSerializer.SerializeCount(_model, _query);
            // TODO: Apply inferencing if enabled

            var res = _store.ExecuteQuery(countQuery);

            if (res is SparqlResultSet)
            {
                SparqlResultSet result = res as SparqlResultSet;
                if (result.Count > 0 && result[0].Count > 0)
                {
                    var value = ParseCellValue(result[0][0]);
                    if (value.GetType() == typeof(int))
                        return (int)value;
                }
            }
            return -1;
        }

        public IEnumerable<T> GetResources<T>(int offset = -1, int limit = -1) where T : Resource
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Resource> GetResources(int offset = -1, int limit = -1)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #endregion
        #endregion
    }
}
