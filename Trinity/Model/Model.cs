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
using System.Globalization;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using OpenLink.Data.Virtuoso;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A set of resources which represent a logical model for a given application domain.
    /// </summary>
    public class Model : IModel
    {
        #region Members

        // The backing RDF store.
        private IStore _store;

        // A handle to the generic version of the GetResource method which is being used
        // for implementing the GetResource(Uri, Type) method that supports runtime type specification.
        private MethodInfo _getResourceMethod;

        Dictionary<string, List<Resource>> _currentResources = new Dictionary<string, List<Resource>>();

        /// <summary>
        /// The Uniform Resource Identifier which provides a name for the model.
        /// </summary>
        public UriRef Uri { get; set; }

        /// <summary>
        /// Indicates if the model contains statements.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                SparqlQuery query = new SparqlQuery(string.Format(@"ASK FROM {0} {{ ?s ?p ?o . }}", SparqlSerializer.SerializeUri(Uri)));
                return !ExecuteQuery(query).GetAnwser();
            }
        }

        /// <summary>
        /// Indicates if all changes in the model have been written back to its backing RDF store(s).
        /// </summary>
        [DefaultValue(false)]
        public bool IsSynchronized { get; private set; }

        public bool RefreshChangedResources { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// This constructor is intended to be used only be the ModelManager.
        /// </summary>
        /// <param name="uri">Uniform Resource Identifier of the model.</param>
        /// <param name="graph">Graph containing the RDF statements.</param>
        /// <param name="graphManager">RDF backend to manage the models RDF graph.</param>
        public Model(IStore store, UriRef uri)
        {
            Uri = uri;
            _store = store;


            // Searches for the generic method T GetResource<T>(Uri) and saves a handle
            // for later use within GetResource(Uri, Type);
            foreach (MethodInfo methodInfo in GetType().GetMethods())
            {
                if (methodInfo.Name == "GetResource" && methodInfo.IsGenericMethod)
                {
                    _getResourceMethod = methodInfo;
                    break;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Removes all elements from the model.
        /// </summary>
        public void Clear()
        {
            if (_store != null)
            {
                _store.RemoveModel(Uri);
                _store.CreateModel(Uri);
            }
        }

        /// <summary>
        /// Adds an existing resource to the model and its backing RDF store. The resulting resource supports the use of the Commit() method.
        /// </summary>
        /// <param name="resource">The resource to be added to the model.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>The resource which is now connected to the current model.</returns>
        public IResource AddResource(IResource resource, ITransaction transaction = null)
        {
            Resource result = CreateResource<Resource>(resource.Uri, transaction);
            foreach (var v in resource.ListValues())
            {
                result.AddProperty(v.Item1, v.Item2);
            }
            result.Commit();

            return result;
        }

        

        /// <summary>
        /// Adds an existing resource to the model and its backing RDF store. The resulting resource supports the use of the Commit() method.
        /// </summary>
        /// <param name="resource">The resource to be added to the model.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>The resource which is now connected to the current model.</returns>
        public T AddResource<T>(T resource, ITransaction transaction = null) where T : Resource
        {
            T result = CreateResource<T>(resource.Uri, transaction);
            foreach (var v in resource.ListValues())
            {
                result.AddProperty(v.Item1, v.Item2);
            }
            result.Commit();

            return result;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="T">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="format">The format of the resulting uri.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public IResource CreateResource(string format = "http://semiodesk.com/id/{0}", ITransaction transaction = null)
        {
            return CreateResource<Resource>(format);
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <returns>The newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public IResource CreateResource(Uri uri, ITransaction transaction = null)
        {
            if (ContainsResource(uri))
            {
                string msg = "A resource with the given URI already exists.";
                throw new ArgumentException(msg);
            }

            Resource resource = new Resource(uri);
            resource.IsNew = true;
            resource.SetModel(this);
            return resource;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="T">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="format">The format of the resulting uri.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public T CreateResource<T>(string format = "http://semiodesk.com/id/{0}", ITransaction transaction = null) where T : Resource
        {
            return CreateResource(UriRef.GetGuid(format), typeof(T)) as T;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="T">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public T CreateResource<T>(Uri uri, ITransaction transaction = null) where T : Resource 
        {
            return CreateResource(uri, typeof(T)) as T;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="t">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="format">The format of the resulting uri.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public object CreateResource(Type t, string format = "http://semiodesk.com/id/{0}", ITransaction transaction = null)
        {
            return CreateResource(UriRef.GetGuid(format), t);
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// This method can be used to create a resource of a type which was asserted at runtime.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="t">Type of the resource object. Must be derived from Resource. </param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="Exception">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public object CreateResource(Uri uri, Type t, ITransaction transaction = null)
        {
            if (!typeof(Resource).IsAssignableFrom(t))
            {
                throw new ArgumentException("The given type is not derived from Resource.");
            }

            if (ContainsResource(uri))
            {
                string msg = "A resource with the given URI already exists.";
                throw new ArgumentException(msg);
            }

            Resource resource = (Resource)Activator.CreateInstance(t, uri);
            resource.SetModel(this);
            resource.IsNew = true;

            return resource;
        }

        /// <summary>
        /// Removes the given resource from the model and its backing RDF store. Note that there is no verification
        /// that the given resource and its stored represenation have identical properties.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        public void DeleteResource(Uri uri, ITransaction transaction = null)
        {
            string updateString = string.Format(@"WITH {0} DELETE {{ {1} ?p ?o. ?s1 ?p1 {1} . }} WHERE {{ {1} ?p ?o. OPTIONAL {{ ?s1 ?p1 {1} . }} }}",
                SparqlSerializer.SerializeUri(Uri),
                SparqlSerializer.SerializeUri(uri));
            SparqlUpdate update = new SparqlUpdate(updateString);
            _store.ExecuteNonQuery(update);
        }

        /// <summary>
        /// Removes the given resource from the model and its backing RDF store. Note that there is no verification
        /// that the given resource and its stored represenation have identical properties.
        /// </summary>
        /// <param name="uri">A resource object.</param>
        public void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            DeleteResource(resource.Uri);
        }

        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resource">Resource that is to be updated in the backing store.</param>
        public void UpdateResource(Resource resource, ITransaction transaction = null)
        {
            if (resource.IsNew)
            {
                string updateString = string.Format(@"WITH {0} INSERT {{ {1} }} WHERE {{}}",
                    SparqlSerializer.SerializeUri(Uri),
                    SparqlSerializer.SerializeResource(resource));
                SparqlUpdate update = new SparqlUpdate(updateString);
                update.Resource = resource;
                ExecuteUpdate(update);
            }
            else
            {
                string updateString = string.Format(@"WITH {0} DELETE {{ {1} ?p ?o. }} INSERT {{ {2} }} WHERE {{ {1} ?p ?o. }} ",
                    SparqlSerializer.SerializeUri(Uri),
                    SparqlSerializer.SerializeUri(resource.Uri),
                    SparqlSerializer.SerializeResource(resource));
                SparqlUpdate update = new SparqlUpdate(updateString);
                update.Resource = resource;
                ExecuteUpdate(update);
            }

            resource.IsNew = false;
        }

        /// <summary>
        /// Indicates wheter a given resource is part of the model.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <returns>True if the resource is part of the model, False if not.</returns>
        public bool ContainsResource(Uri uri, ITransaction transaction = null)
        {
            return ExecuteQuery(new SparqlQuery(string.Format(@"ASK FROM {0} {{ {1} ?p ?o . }}",
                SparqlSerializer.SerializeUri(Uri),
                SparqlSerializer.SerializeUri(uri))), transaction:transaction).GetAnwser();
        }

        /// <summary>
        /// Indicates wheter a given resource is part of the model.
        /// </summary>
        /// <param name="uri">A resource object.</param>
        /// <returns>True if the resource is part of the model, False if not.</returns>
        public bool ContainsResource(IResource resource, ITransaction transaction = null)
        {
            return ContainsResource(resource.Uri, transaction);
        }

        /// <summary>
        /// Execute a SPARQL Query.
        /// </summary>
        /// <param name="query">A SparqlQuery object.</param>
        /// <returns>A SparqlQueryResults object in any case.</returns>
        public ISparqlQueryResult ExecuteQuery(SparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            if (Uri != null && (query.Model == null || query.IsAgainstDefaultModel()))
            {
                query.SetModel(this);
            }

            query.InferenceEnabled = inferenceEnabled;

            return _store.ExecuteQuery(query, transaction);
        }

        /// <summary>
        /// Execute a resource query.
        /// </summary>
        /// <param name="query">A ResourceQuery object.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public IResourceQueryResult ExecuteQuery(ResourceQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            return new ResourceQueryResult(this, query, inferenceEnabled, transaction);
        }

        /// <summary>
        /// Execute a SPARQL Update.
        /// </summary>
        /// <param name="update">A SparqlUpdate object.</param>
        public void ExecuteUpdate(SparqlUpdate update, ITransaction transaction = null)
        {
            _store.ExecuteNonQuery(update);
        }

        /// <summary>
        /// Retrieves a resource from the model.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <returns>A resource with all asserted properties.</returns>
        public IResource GetResource(Uri uri, ITransaction transaction = null)
        {
            //SparqlQuery query = new SparqlQuery(String.Format("DESCRIBE {0} FROM {1}", SparqlSerializer.SerializeUri(uri), SparqlSerializer.SerializeUri(this.Uri)));
            SparqlQuery query = new SparqlQuery(String.Format("SELECT ?s ?p ?o FROM {0} WHERE {{ ?s ?p ?o. FILTER (?s ={0}) }}", SparqlSerializer.SerializeUri(uri), SparqlSerializer.SerializeUri(this.Uri)));
            ISparqlQueryResult result = ExecuteQuery(query, transaction: transaction);

            IList resources = result.GetResources().ToList();

            if (resources.Count > 0)
            {
                Resource res = resources[0] as Resource;
                res.IsNew = false;
                res.IsSynchronized = true;
                res.SetModel(this);
                return (IResource) resources[0];
            }
            else
            {
                string msg = "Error: Could not find resource {0}.";
                throw new ArgumentException(string.Format(msg, uri));
            }
        }

        /// <summary>
        /// Retrieves a resource from the model. Provides a resource object of the given type.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <returns>A resource with all asserted properties.</returns>
        public T GetResource<T>(Uri uri, ITransaction transaction = null) where T : Resource
        {
            //SparqlQuery query = new SparqlQuery(String.Format("DESCRIBE {0} FROM {1}", SparqlSerializer.SerializeUri(uri), SparqlSerializer.SerializeUri(this.Uri)));
            SparqlQuery query = new SparqlQuery(String.Format("SELECT ?s ?p ?o FROM {0} WHERE {{ ?s ?p ?o. FILTER (?s ={0}) }}", SparqlSerializer.SerializeUri(uri), SparqlSerializer.SerializeUri(this.Uri)));
            ISparqlQueryResult result = ExecuteQuery(query, transaction:transaction);

            IList resources = result.GetResources<T>().ToList();

            if (resources.Count > 0)
            {
                T res = resources[0] as T;
                res.IsNew = false;
                res.IsSynchronized = true;
                res.SetModel(this);
                return res;
            }
            else
            {
                string msg = "Error: Could not find resource <{0}>.";
                throw new ArgumentException(string.Format(msg, uri));
            }
        }

        /// <summary>
        /// Retrieves a resource from the model. Provides a resource object of the given type.
        /// This method can be used for runtime asserted types.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="type">Type of the resource object. Must be derived from Resource.</param>
        /// <returns>An instance of the given resource object type, Null otherwise.</returns>
        public object GetResource(Uri uri, Type type, ITransaction transaction = null)
        {
            if (_getResourceMethod != null)
            {
                if (typeof(IResource).IsAssignableFrom(type))
                {
                    MethodInfo getResource = _getResourceMethod.MakeGenericMethod(type);
                    Resource res = getResource.Invoke(this, new object[] { uri, transaction }) as Resource;
                    res.IsNew = false;
                    res.IsSynchronized = true;
                    res.SetModel(this);
                    return res;
                }
                else
                {
                    string msg = string.Format("Error: The given type {0} does not implement the IResource interface.", type);
                    throw new ArgumentException(msg);
                }
            }
            else
            {
                string msg = string.Format("Error: No handle to the generic method T GetResource<T>(Uri)");
                throw new InvalidOperationException(msg);
            }
        }

        /// <summary>
        /// Executes a SPARQL query and provides an enumeration of matching resources.
        /// </summary>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<Resource> GetResources(SparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            IEnumerable<Resource> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<Resource>();

            if (result != null)
            {
                // TODO: Should be done in the SparqlQueryResult for increased performance.
                foreach (Resource r in result)
                {
                    r.SetModel(this);
                    r.IsNew = false;
                    r.IsSynchronized = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Executes a resource query and provides an enumeration of matching resources.
        /// </summary>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<Resource> GetResources(ResourceQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            IEnumerable<Resource> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<Resource>();

            if (result != null)
            {
                // TODO: Should be done in the SparqlQueryResult for increased performance.
                foreach (Resource r in result)
                {
                    r.SetModel(this);
                    r.IsNew = false;
                    r.IsSynchronized = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Executes a SPARQL query and provides an enumeration of matching resources. 
        /// Provides a resource object of the given type.
        /// </summary>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<T> GetResources<T>(SparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            IEnumerable<T> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<T>();

            // TODO: Could be done in the SparqlQueryResult for increased performance.
            if (result != null)
            {
                foreach (object r in result)
                {
                    T t = r as T;

                    // NOTE: This safeguard is required because of a bug in ExecuteQuery where 
                    // it returns null objects when a rdf:type triple is missing..
                    if (t == null) continue;

                    t.SetModel(this);
                    t.IsNew = false;
                    t.IsSynchronized = true;

                    yield return t;
                }
            }
        }

        /// <summary>
        /// Executes a Resource query and provides an enumeration of matching resources. 
        /// Provides a resource object of the given type.
        /// </summary>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<T> GetResources<T>(ResourceQuery query, bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            IEnumerable<T> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<T>();

            // TODO: Could be done in the SparqlQueryResult for increased performance.
            if (result != null)
            {
                foreach (object r in result)
                {
                    T t = r as T;

                    // NOTE: This safeguard is required because of a bug in ExecuteQuery where 
                    // it returns null objects when a rdf:type triple is missing..
                    if (t == null) continue;

                    t.SetModel(this);
                    t.IsNew = false;
                    t.IsSynchronized = true;

                    yield return t;
                }
            }
        }

        /// <summary>
        /// Returns a enumeration of all resources that match the given type.
        /// </summary>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<T> GetResources<T>(bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            T temp = (T)Activator.CreateInstance(typeof(T), new Uri("semio:desk"));
            ResourceQuery query = new ResourceQuery(temp.GetTypes());            

            return GetResources<T>(query, inferenceEnabled, transaction);
        }

        /// <summary>
        /// Executes a SPARQL-select query and provides a list of binding sets. This method 
        /// implements transparent type marshalling and delivers the bound variables in C#
        /// native data types.
        /// </summary>
        /// <param name="query">A SPARQL-select query which results in a set of bound variables.</param>
        /// <returns>An enumeration of bound variables that match the given query.</returns>
        public IEnumerable<BindingSet> GetBindings(SparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            return ExecuteQuery(query, inferenceEnabled, transaction).GetBindings();
        }

        /// <summary>
        /// Exports the contents of the model and provides a memory stream.
        /// </summary>
        /// <param name="format">The serialization format.</param>
        /// <returns>A serialization of the models contents.</returns>
        public void Write(Stream fs, RdfSerializationFormat format)
        {
            _store.Write(fs, Uri, format);
        }

        /// <summary>
        /// Imports the contents of a model located by the given URL. The method supports
        /// importing files and other models stored in the local RDF store. The location
        /// of the model is determined by the URI scheme.
        /// </summary>
        /// <param name="url">A uniform resource locator.</param>
        /// <returns>True if the contents of the model were imported, False if not.</returns>
        public bool Read(Uri url, RdfSerializationFormat format)
        {
            if (format == RdfSerializationFormat.Trig)
                throw new ArgumentException("Quadruple serialization formats are not supported by this method. Use IStore.Read() instead.");

            return (_store.Read(Uri, url, format) != null);
        }

        /// <summary>
        /// Starts a transaction which can be used to group more queries together to be executed as one.
        /// </summary>
        /// <param name="isolationLevel">Isolation level used to lock the database.</param>
        /// <returns>A handle to the transaction.</returns>
        public ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return _store.BeginTransaction(isolationLevel);
        }


        #endregion

    }
}
