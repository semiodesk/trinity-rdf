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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// An interface for classes which provide functionality to manage a set of resources.
    /// </summary>
    public interface IModel
    {
        #region Properties

        /// <summary>
        /// Uri of this model.
        /// </summary>
        UriRef Uri { get; }

        /// <summary>
        /// True if the model is empty.
        /// </summary>
        [JsonIgnore]
        bool IsEmpty { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Adds an existing resource to the model and its backing RDF store. The resulting resource supports the use of the Commit() method.
        /// </summary>
        /// <param name="resource">The resource to be added to the model.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>The resource which is now connected to the current model.</returns>
        IResource AddResource(IResource resource, ITransaction transaction = null);

        /// <summary>
        /// Adds an existing resource to the model and its backing RDF store. The resulting resource supports the use of the Commit() method.
        /// </summary>
        /// <param name="resource">The resource to be added to the model.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>The resource which is now connected to the current model.</returns>
        T AddResource<T>(T resource, ITransaction transaction = null) where T : Resource;

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store.
        /// </summary>
        /// <param name="format">The format string from which a globally unique identifier URI should be generated from.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <returns>The newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        IResource CreateResource(string format = "urn:uuid:{0}", ITransaction transaction = null);

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>The newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        IResource CreateResource(Uri uri, ITransaction transaction = null);

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="T">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="format">The format string from which a globally unique identifier URI should be generated from.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        T CreateResource<T>(string format = "urn:uuid:{0}", ITransaction transaction = null) where T : Resource;

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <typeparam name="T">Type of the resource object. Must be derived from Resource.</typeparam>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        T CreateResource<T>(Uri uri, ITransaction transaction = null) where T : Resource;

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// </summary>
        /// <param name="type">The concrete type of the resource. This must be a subclass of resource.</param>
        /// <param name="format">The format of the resulting uri.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="ArgumentException">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        object CreateResource(Type type, string format = "urn:uuid:{0}", ITransaction transaction = null);

        /// <summary>
        /// Creates a new resource in the model and its backing RDF store. Provides a resource object of the given type.
        /// This method can be used to create a resource of a type which was asserted at runtime.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="t">Type of the resource object. Must be derived from Resource. </param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An instance of the given object type wrapping the newly created resource.</returns>
        /// <exception cref="Exception">Throws ArgumentException if a resource with the given URI already exists in the model.</exception>
        object CreateResource(Uri uri, Type t, ITransaction transaction = null);

        /// <summary>
        /// Removes the given resource from the model and its backing RDF store. Note that there is no verification
        /// that the given resource and its stored represenation have identical properties.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">The transaction associated with this action.</param>
        void DeleteResource(Uri uri, ITransaction transaction = null);

        /// <summary>
        /// Removes the given resource from the model and its backing RDF store. Note that there is no verification
        /// that the given resource and its stored represenation have identical properties.
        /// </summary>
        /// <param name="resource">Resource that is to be removed from the model.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        void DeleteResource(IResource resource, ITransaction transaction = null);

        /// <summary>
        /// Indicates wheter a given resource is part of the model.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>True if the resource is part of the model, False if not.</returns>
        bool ContainsResource(Uri uri, ITransaction transaction = null);

        /// <summary>
        /// Indicates wheter a given resource is part of the model.
        /// </summary>
        /// <param name="resource">Resource that should be looked up in the model.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>True if the resource is part of the model, False if not.</returns>
        bool ContainsResource(IResource resource, ITransaction transaction = null);

        /// <summary>
        /// Execute a SPARQL query against the model.
        /// </summary>
        /// <param name="query">A SparqlQuery object.</param>
        /// <param name="inferenceEnabled">Modifier to enable inferencing. Default is false.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A SPARQL query result object.</returns>
        ISparqlQueryResult ExecuteQuery(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null);

        /// <summary>
        /// Execute a SparqlUpdate against the model.
        /// </summary>
        /// <param name="update">A sparql update object.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        void ExecuteUpdate(SparqlUpdate update, ITransaction transaction = null);

        /// <summary>
        /// Retrieves a resource from the model.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        IResource GetResource(Uri uri, ITransaction transaction = null);

        /// <summary>
        /// Retrieves a resource from the model. Provides a resource object of the given type.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        T GetResource<T>(Uri uri, ITransaction transaction = null) where T : Resource;

        /// <summary>
        /// Retrieves a resource from the model.
        /// </summary>
        /// <param name="resource">The instance of IResource to be retrieved.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        T GetResource<T>(IResource resource, ITransaction transaction = null) where T : Resource;

        /// <summary>
        /// Retrieves a resource from the model. Provides a resource object of the given type.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="type">The type the resource should have.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        object GetResource(Uri uri, Type type, ITransaction transaction = null);

        /// <summary>
        /// Retrieves a resource from the model.
        /// </summary>
        /// <param name="resource">The instance of IResource to be retrieved.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all asserted properties.</returns>
        IResource GetResource(IResource resource, ITransaction transaction = null);

        /// <summary>
        /// Executes a SPARQL query and provides an enumeration of matching resources.
        /// </summary>
        /// <param name="query">A SparqlQuery object.</param>
        /// <param name="inferenceEnabled">Modifier to enable inferencing. Default is false.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>An enumeration of resources that match the given query.</returns>
        IEnumerable<Resource> GetResources(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null);

        /// <summary>
        /// Executes a SPARQL query and provides an enumeration of matching resources.
        /// </summary>
        /// <param name="query">A SparqlQuery object.</param>
        /// <param name="inferenceEnabled">Modifier to enable inferencing. Default is false.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>An enumeration of resources that match the given query.</returns>
        IEnumerable<T> GetResources<T>(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource;

        /// <summary>
        /// Returns a enumeration of all resources that match the given type.
        /// </summary>
        /// <param name="inferenceEnabled">Modifier to enable inferencing. Default is false.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>An enumeration of resources that match the given query.</returns>
        IEnumerable<T> GetResources<T>(bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource;

        /// <summary>
        /// Returns a queryable object that can be used to build LINQ statements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> AsQueryable<T>(bool inferenceEnabled = false) where T : Resource;

        /// <summary>
        /// Executes a SPARQL query and provides an enumeration of matching resources.
        /// </summary>
        /// <param name="query">A SparqlQuery object.</param>
        /// <param name="inferenceEnabled">Modifier to enable inferencing. Default is false.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>An enumeration of resources that match the given query.</returns>
        IEnumerable<BindingSet> GetBindings(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null);

        /// <summary>
        /// Imports the contents of a model located by the given URL. The method supports
        /// importing files and other models stored in the local RDF store. 
        /// </summary>
        /// <param name="url">A uniform resource locator.</param>
        /// <param name="format">The serialization format.</param>
        /// <param name="update">True to update the model, false to replace the data.</param>
        /// <returns>True if the contents of the model were imported, False if not.</returns>
        bool Read(Uri url, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Imports the contents of a graph serialized in the stream to this model.
        /// </summary>
        /// <param name="stream">The stream containing the serialization</param>
        /// <param name="format">Format of the serialization</param>
        /// <param name="update">True to update the model, false to replace the data.</param>
        /// <returns>True if the contents of the model were imported, False if not.</returns>
        bool Read(Stream stream, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Serializes the contents of the model and provides a memory stream.
        /// </summary>
        /// <param name="fs">The file stream to write to.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns>A serialization of the models contents.</returns>
        void Write(Stream fs, RdfSerializationFormat format);

        /// <summary>
        /// Updates a resource with it's current state in the model.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="transaction"></param>
        void UpdateResource(Resource resource, ITransaction transaction = null);

        /// <summary>
        /// Removes all elements from the model.
        /// </summary>
        void Clear();

        /// <summary>
        /// Starts a transaction which can be used to group more queries together to be executed as one.
        /// </summary>
        /// <param name="isolationLevel">Isolation level used to lock the database.</param>
        /// <returns>A handle to the transaction.</returns>
        ITransaction BeginTransaction(IsolationLevel isolationLevel);
        
        #endregion
    }
}
