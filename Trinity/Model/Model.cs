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
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Semiodesk.Trinity.Query;
using Remotion.Linq.Parsing.Structure;

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

        private MethodInfo _getResourcesMethod;

        /// <summary>
        /// The Uniform Resource Identifier which provides a name for the model.
        /// </summary>
        public UriRef Uri { get; set; }

        /// <summary>
        /// Indicates if the model contains statements.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty
        {
            get
            {
                SparqlQuery query = new SparqlQuery(string.Format(@"ASK FROM {0} {{ ?s ?p ?o . }}", SparqlSerializer.SerializeUri(Uri)));

                return !ExecuteQuery(query).GetAnwser();
            }
        }

        /// <summary>
        /// All unampped properties will be ignored for update and thus deleted. 
        /// This reduces the amount of data thats get sent to the database but also might remove important data.
        /// Setting this to true essentialy disables the triple API.
        /// </summary>
        public bool IgnoreUnmappedProperties { get; set; } = false;

        #endregion

        #region Constructors

        /// <summary>
        /// This constructor is intended to be used only be the ModelManager.
        /// </summary>
        /// <param name="store">The underlying triple store implementation to be used.</param>
        /// <param name="uri">Uniform Resource Identifier of the model.</param>
        public Model(IStore store, UriRef uri)
        {
            _store = store;

            Uri = uri;

            // Searches for the generic method T GetResource<T>(Uri) and saves a handle
            // for later use within GetResource(Uri, Type);
            foreach (MethodInfo methodInfo in GetType().GetMethods())
            {
                if (methodInfo.Name == "GetResource" && methodInfo.IsGenericMethod)
                {
                    _getResourceMethod = methodInfo; break;
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
            }
        }

        /// <summary>
        /// Adds an existing resource to the model and its backing RDF store. The resulting resource supports the use of the Commit() method.
        /// </summary>
        /// <param name="resource">The resource to be added to the model.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>The resource which is now connected to the current model.</returns>
        public virtual IResource AddResource(IResource resource, ITransaction transaction = null)
        {
            Resource result = CreateResource<Resource>(resource.Uri, transaction);

            foreach (var v in resource.ListValues())
            {
                result.AddPropertyToMapping(v.Item1, v.Item2, false);
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
        public virtual T AddResource<T>(T resource, ITransaction transaction = null) where T : Resource
        {
            T result = CreateResource<T>(resource.Uri, transaction);

            foreach (var v in resource.ListValues())
            {
                result.AddPropertyToMapping(v.Item1, v.Item2, false);
            }

            result.Commit();

            return result;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <param name="format">The format of the resulting uri.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public virtual IResource CreateResource(string format = "urn:uuid:{0}", ITransaction transaction = null)
        {
            return CreateResource<Resource>(format, transaction);
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>The newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public virtual IResource CreateResource(Uri uri, ITransaction transaction = null)
        {
            if (ContainsResource(uri, transaction))
            {
                throw new ArgumentException("A resource with the given URI already exists.");
            }

            Resource resource = new Resource(uri)
            {
                IsNew = true
            };
            resource.SetModel(this);

            return resource;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="T">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="format">The format of the resulting uri.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public virtual T CreateResource<T>(string format = "urn:uuid:{0}", ITransaction transaction = null) where T : Resource
        {
            return CreateResource(UriRef.GetGuid(format), typeof(T), transaction) as T;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="T">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public virtual T CreateResource<T>(Uri uri, ITransaction transaction = null) where T : Resource
        {
            return CreateResource(uri, typeof(T), transaction) as T;
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <param name="t">Type of the resource object. Must be derived from Resource.</param>
        /// <param name="format">The format of the resulting uri.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public virtual object CreateResource(Type t, string format = "urn:uuid:{0}", ITransaction transaction = null)
        {
            return CreateResource(UriRef.GetGuid(format), t, transaction);
        }

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// This method can be used to create a resource of a type which was asserted at runtime.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="t">Type of the resource object. Must be derived from Resource. </param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="Exception">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        public virtual object CreateResource(Uri uri, Type t, ITransaction transaction = null)
        {
            if (!typeof(Resource).IsAssignableFrom(t))
            {
                throw new ArgumentException("The given type is not derived from Resource.");
            }

            if (ContainsResource(uri, transaction))
            {
                throw new ArgumentException("A resource with the given URI already exists.");
            }

            Resource resource = (Resource)Activator.CreateInstance(t, uri);
            resource.SetModel(this);
            resource.IsNew = true;

            return resource;
        }

        /// <summary>
        /// Removes the given resource from the model and its backing RDF store. Note that there is no verification
        /// that the given resource and its stored representation have identical properties.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void DeleteResource(Uri uri, ITransaction transaction = null)
        {
            // NOTE: Regrettably, dotNetRDF does not support the full SPARQL 1.1 update syntax. To be precise,
            // it does not support FILTERs or OPTIONAL in Modify clauses. This requires us to formulate the
            // deletion of the resource in subject and object of any triples in two statements.

            SparqlUpdate deleteSubject = new SparqlUpdate(@"DELETE WHERE { GRAPH @graph { @subject ?p ?o . } }");
            deleteSubject.Bind("@graph", Uri);
            deleteSubject.Bind("@subject", uri);

            _store.ExecuteNonQuery(deleteSubject, transaction);

            SparqlUpdate deleteObject = new SparqlUpdate(@"DELETE WHERE { GRAPH @graph { ?s ?p @object . } }");
            deleteObject.Bind("@graph", Uri);
            deleteObject.Bind("@object", uri);

            _store.ExecuteNonQuery(deleteObject, transaction);
        }

        /// <summary>
        /// Removes the given resource from the model and its backing RDF store. Note that there is no verification
        /// that the given resource and its stored representation have identical properties.
        /// </summary>
        /// <param name="resource">A resource object.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            DeleteResource(resource.Uri);
        }

        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resource">Resource that is to be updated in the backing store.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void UpdateResource(Resource resource, ITransaction transaction = null)
        {
            string updateString;

            if (resource.IsNew)
            {
                updateString = string.Format(@"WITH {0} INSERT {{ {1} }} WHERE {{}}",
                    SparqlSerializer.SerializeUri(Uri),
                    SparqlSerializer.SerializeResource(resource, IgnoreUnmappedProperties));
            }
            else
            {
                updateString = string.Format(@"WITH {0} DELETE {{ {1} ?p ?o. }} INSERT {{ {2} }} WHERE {{ OPTIONAL {{ {1} ?p ?o. }} }}",
                    SparqlSerializer.SerializeUri(Uri),
                    SparqlSerializer.SerializeUri(resource.Uri),
                    SparqlSerializer.SerializeResource(resource, IgnoreUnmappedProperties));
            }

            SparqlUpdate update = new SparqlUpdate(updateString);

            ExecuteUpdate(update, transaction);

            resource.IsNew = false;
            resource.IsSynchronized = true;
        }

        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resources">Resources that is to be updated in the backing store.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void UpdateResources(IEnumerable<Resource> resources, ITransaction transaction = null)
        {
            string WITH = $"{SparqlSerializer.SerializeUri(Uri)} ";
            StringBuilder INSERT = new StringBuilder();
            StringBuilder DELETE = new StringBuilder();
            StringBuilder OPTIONAL = new StringBuilder();

            foreach( var res in resources)
            { 
                if (res.IsNew)
                {
                    INSERT.Append(" ");
                    INSERT.Append(SparqlSerializer.SerializeResource(res, IgnoreUnmappedProperties));
                }
                else
                {
                    DELETE.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p ?o. ");
                    OPTIONAL.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p ?o. ");
                    INSERT.Append($" {SparqlSerializer.SerializeResource(res, IgnoreUnmappedProperties)} ");
                }
            }
            string updateString = $"WITH {WITH} DELETE {{ {DELETE} }} INSERT {{ {INSERT} }} WHERE {{ OPTIONAL {{ {OPTIONAL} }} }}";
            SparqlUpdate update = new SparqlUpdate(updateString);

            ExecuteUpdate(update, transaction);

            foreach( var resource in resources) 
            { 
                resource.IsNew = false;
                resource.IsSynchronized = true;
            }
        }

        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resources">Resources that is to be updated in the backing store.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void UpdateResources(ITransaction transaction = null, params Resource[] resources)
        {
            UpdateResources(resources, transaction);
        }

        /// <summary>
        /// Indicates wheter a given resource is part of the model.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>True if the resource is part of the model, False if not.</returns>
        public bool ContainsResource(Uri uri, ITransaction transaction = null)
        {
            ISparqlQuery query = new SparqlQuery("ASK FROM @graph { @subject ?p ?o . }");
            query.Bind("@graph", this.Uri);
            query.Bind("@subject", uri);

            return ExecuteQuery(query, transaction: transaction).GetAnwser();
        }

        /// <summary>
        /// Indicates wheter a given resource is part of the model.
        /// </summary>
        /// <param name="resource">A resource object.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>True if the resource is part of the model, False if not.</returns>
        public bool ContainsResource(IResource resource, ITransaction transaction = null)
        {
            return ContainsResource(resource.Uri, transaction);
        }

        /// <summary>
        /// Execute a SPARQL Query.
        /// </summary>
        /// <param name="query">A SparqlQuery object.</param>
        /// <param name="inferenceEnabled">Indicate that this query should work with enabled inferencing.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A SparqlQueryResults object in any case.</returns>
        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            if (query.Model == null || !query.GetDefaultModels().Any())
            {
                query.Model = this;
            }

            query.IsInferenceEnabled = inferenceEnabled;

            return _store.ExecuteQuery(query, transaction);
        }

        /// <summary>
        /// Execute a SPARQL Update.
        /// </summary>
        /// <param name="update">A SparqlUpdate object.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public void ExecuteUpdate(SparqlUpdate update, ITransaction transaction = null)
        {
            _store.ExecuteNonQuery(update, transaction);
        }

        /// <summary>
        /// Retrieves a resource from the model.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        public IResource GetResource(Uri uri, ITransaction transaction = null)
        {
            ISparqlQuery query = new SparqlQuery("SELECT DISTINCT ?s ?p ?o FROM @model WHERE { ?s ?p ?o. FILTER (?s = @subject) }");
            query.Bind("@model", this.Uri);
            query.Bind("@subject", uri);

            ISparqlQueryResult result = ExecuteQuery(query, transaction: transaction);

            IEnumerable<Resource> resources = result.GetResources();

            foreach (Resource r in resources)
            {
                r.IsNew = false;
                r.IsSynchronized = true;
                r.SetModel(this);

                return r;
            }

            throw new ResourceNotFoundException(uri);
        }

        /// <summary>
        /// Retrieves a resource from the model.
        /// </summary>
        /// <param name="resource">The instance of IResource to be retrieved.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        public IResource GetResource(IResource resource, ITransaction transaction = null)
        {
            return GetResource(resource.Uri, transaction);
        }

        /// <summary>
        /// Retrieves a resource from the model. Provides a resource object of the given type.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        public T GetResource<T>(Uri uri, ITransaction transaction = null) where T : Resource
        {
            ISparqlQuery query = new SparqlQuery("SELECT DISTINCT ?s ?p ?o FROM @model WHERE { ?s ?p ?o. FILTER (?s = @subject) }");
            query.Bind("@model", this.Uri);
            query.Bind("@subject", uri);

            ISparqlQueryResult result = ExecuteQuery(query, transaction: transaction);

            IEnumerable<T> resources = result.GetResources<T>();

            foreach (T r in resources)
            {
                r.IsNew = false;
                r.IsSynchronized = true;
                r.SetModel(this);

                return r;
            }

            string msg = "Error: Could not find resource <{0}>.";

            throw new ArgumentException(string.Format(msg, uri));
        }

        /// <summary>
        /// Retrieves a resource from the model. Provides a resource object of the given type.
        /// </summary>
        /// <param name="resource">The resource that should be retrieved.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        public T GetResource<T>(IResource resource, ITransaction transaction = null) where T : Resource
        {
            return GetResource<T>(resource.Uri, transaction);
        }

        /// <summary>
        /// Retrieves a resource from the model. Provides a resource object of the given type.
        /// This method can be used for runtime asserted types.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="type">Type of the resource object. Must be derived from Resource.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
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
        /// <param name="query">The SparqlQuery object that should be executed.</param>
        /// <param name="inferenceEnabled">Indicate that this query should work with enabled inferencing.</param>
        /// <param name="transaction">transaction associated with this action.</param>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<Resource> GetResources(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
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
        /// Retrieves resources from the model. Provides resources object of the given type.
        /// </summary>
        /// <param name="uris">A List Uniform Resource Identifier.</param>
        /// <param name="type">The type the resource should have.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        public IEnumerable<object> GetResources(IEnumerable<Uri> uris, Type type, ITransaction transaction = null)
        {
            if (typeof(IResource).IsAssignableFrom(type))
            {
                StringBuilder queryString = new StringBuilder();
                queryString.Append("SELECT ?s ?p ?o WHERE { ?s ?p ?o. FILTER ( ");
                queryString.Append(string.Join("||", from s in uris select $"?s = <{s}>"));
                queryString.Append(")}");
                var query = new SparqlQuery(queryString.ToString());

                ISparqlQueryResult result = ExecuteQuery(query, transaction: transaction);

                IEnumerable<Resource> resources = result.GetResources(type);

                foreach (Resource r in resources)
                {
                    if( type.IsAssignableFrom(r.GetType()))
                    r.IsNew = false;
                    r.IsSynchronized = true;
                    r.SetModel(this);

                    yield return r;
                }

            }
            else
            {
                string msg = string.Format("Error: The given type {0} does not implement the IResource interface.", type);
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// Executes a SPARQL query and provides an enumeration of matching resources. 
        /// Provides a resource object of the given type.
        /// </summary>
        /// <param name="query">The SparqlQuery object that should be executed.</param>
        /// <param name="inferenceEnabled">Indicate that this query should work with enabled inferencing.</param>
        /// <param name="transaction">transaction associated with this action.</param>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<T> GetResources<T>(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
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
        /// <param name="inferenceEnabled">Indicate that this query should work with enabled inferencing.</param>
        /// <param name="transaction">transaction associated with this action.</param>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<T> GetResources<T>(bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT ?s ?p ?o WHERE { ?s ?p ?o . ");

            T instance = (T)Activator.CreateInstance(typeof(T), new UriRef("urn:"));

            foreach(Class type in instance.GetTypes())
            {
                queryBuilder.Append($"?s a <{type.Uri}> . ");
            }

            queryBuilder.Append("}");

            SparqlQuery query = new SparqlQuery(queryBuilder.ToString());

            return GetResources<T>(query, inferenceEnabled, transaction);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IQueryable<T> AsQueryable<T>(bool inferenceEnabled = false) where T : Resource
        {
            SparqlQueryExecutor executor = new SparqlQueryExecutor(this, inferenceEnabled);

            QueryParser queryParser = QueryParser.CreateDefault();

            return new SparqlQueryable<T>(queryParser, executor);
        }

        /// <summary>
        /// Executes a SPARQL-select query and provides a list of binding sets. This method 
        /// implements transparent type marshalling and delivers the bound variables in C#
        /// native data types.
        /// </summary>
        /// <param name="query">A SPARQL-select query which results in a set of bound variables.</param>
        /// <param name="inferenceEnabled">Indicate that this query should work with enabled inferencing.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An enumeration of bound variables that match the given query.</returns>
        public IEnumerable<BindingSet> GetBindings(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            return ExecuteQuery(query, inferenceEnabled, transaction).GetBindings();
        }

        /// <summary>
        /// Exports the contents of the model and provides a memory stream.
        /// </summary>
        /// <param name="fs">File stream to write to.</param>
        /// <param name="format">The serialization format. <see cref="RdfSerializationFormat"/></param>
        /// <param name="namespaces">Defines namespace to prefix mappings for the output.</param>
        /// <returns>A serialization of the models contents.</returns>
        public void Write(Stream fs, RdfSerializationFormat format, INamespaceMap namespaces = null)
        {
            _store.Write(fs, Uri, format, namespaces);
        }

        /// <summary>
        /// Imports the contents of a model located by the given URL. The method supports
        /// importing files and other models stored in the local RDF store. The location
        /// of the model is determined by the URI scheme.
        /// </summary>
        /// <param name="url">A uniform resource locator.</param>
        /// <param name="format">Serialization format <see cref="RdfSerializationFormat"/></param>
        /// <param name="update">Pass false if you want to overwrite existing data. True if you want to keep the data and add the new entries.</param>
        /// <returns>True if the contents of the model were imported, False if not.</returns>
        public bool Read(Uri url, RdfSerializationFormat format, bool update)
        {
            if (format == RdfSerializationFormat.Trig)
            {
                throw new ArgumentException("Quadruple serialization formats are not supported by this method. Use IStore.Read() instead.");
            }

            return (_store.Read(Uri, url, format, update) != null);
        }

        /// <summary>
        /// Reads model contents from a stream. The method supports importing files and other models stored in the local RDF store.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <param name="format">Serialization format <see cref="RdfSerializationFormat"/></param>
        /// <param name="update">Pass false if you want to overwrite existing data. True if you want to keep the data and add the new entries.</param>
        /// <returns>True if the contents of the model were imported, False if not.</returns>
        public bool Read(Stream stream, RdfSerializationFormat format, bool update)
        {
            if (format == RdfSerializationFormat.Trig)
            {
                throw new ArgumentException("Quadruple serialization formats are not supported by this method. Use IStore.Read() instead.");
            }

            return (_store.Read(stream, Uri, format, update) != null);
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
