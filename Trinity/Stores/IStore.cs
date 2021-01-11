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
using System.Data;
using System.IO;
using VDS.RDF;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// The IStorage interface describes the methods an RDF Storage has to implement.
    /// </summary>
    public interface IStore : IDisposable
    {
        #region Properties

        /// <summary>
        /// Indicates if the store is ready to be queried.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Set this property to log the SPARQL queries which are executed on this store.
        /// For example, to log to the console, set this property to System.Console.Write(System.String).
        /// </summary>
        Action<string> Log { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a new model with the given uri to the storage. 
        /// </summary>
        /// <param name="uri">Uri of the model</param>
        /// <returns>Handle to the model</returns>
        IModel CreateModel(Uri uri);

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="uri">Uri of the model which is to be removed.</param>
        void RemoveModel(Uri uri);

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="model">Handle to the model which is to be removed.</param>
        void RemoveModel(IModel model);

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty
        /// </summary>
        /// <param name="uri">Uri of the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty")]
        bool ContainsModel(Uri uri);

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty
        /// </summary>
        /// <param name="model">Handle to the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty")]
        bool ContainsModel(IModel model);

        /// <summary>
        /// Gets a handle to a model in the store.
        /// </summary>
        /// <param name="uri">Uri of the model.</param>
        /// <returns></returns>
        IModel GetModel(Uri uri);

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
        IEnumerable<IModel> ListModels();

        /// <summary>
        /// Executes a SparqlQuery on the store.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null);

        /// <summary>
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="transaction"></param>
        void ExecuteNonQuery(SparqlUpdate update, ITransaction transaction = null);

        /// <summary>
        /// Starts a transaction. The resulting transaction handle can be used to chain operations together.
        /// </summary>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <returns></returns>
        ITransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of uris of the models that should be grouped together.</param>
        /// <returns></returns>
        IModelGroup CreateModelGroup(params Uri[] models);

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of uris of the models that should be grouped together.</param>
        /// <returns></returns>
        IModelGroup CreateModelGroup(params IModel[] models);

        /// <summary>
        /// Loads a serialized graph from the given location into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="url">Location</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Loads a serialized graph from the given stream into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <param name="leaveOpen">Leaves the stream open</param>
        /// <returns></returns>
        Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update, bool leaveOpen = false);

        /// <summary>
        /// Loads a serialized graph from the given string into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="content">String containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        Uri Read(string content, Uri graphUri, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="fs">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store.</param>
        /// <param name="format">Allowed formats.</param>
        /// <param name="namespaces">Defines namespace to prefix mappings for the output.</param>
        /// <param name="baseUri">Base URI for shortening URIs in formats that support it.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after writing completes.</param>
        /// <returns></returns>
        void Write(Stream fs, Uri graphUri, RdfSerializationFormat format, INamespaceMap namespaces = null, Uri baseUri = null, bool leaveOpen = false);

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="fs">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store.</param>
        /// <param name="formatWriter">A RDF writer.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after writing completes.</param>
        /// <returns></returns>
        void Write(Stream fs, Uri graphUri, IRdfWriter formatWriter, bool leaveOpen = false);


        /// <summary>
        /// Initializes the store from the configuration. It uses either the provided file or attempts to load from "ontologies.config" located next to the executing assembly.
        /// For legacy reasons it also looks in the app.config file.
        /// If the ontology files are in a different path, this can be supplied as a base path..
        /// </summary>
        /// <param name="configPath">Path the configuration should be read from.</param>
        /// <param name="sourceDir">Path where the ontologies should be searched for.</param>
        void InitializeFromConfiguration(string configPath = null, string sourceDir = null);

        /// <summary>
        /// Initializes the store from the configuration. It uses either the provided file or attempts to load from "ontologies.config" located next to the executing assembly.
        /// For legacy reasons it also looks in the app.config file.
        /// If the ontology files are in a different path, this can be supplied as a base path..
        /// </summary>
        /// <param name="configPath">Load a specific configuration file.</param>
        /// <param name="sourceDir">If given, this function tries to load the ontologies from this folder.</param>
        [Obsolete("This method will be removed in the future. Use InitializeFromConfiguration() instead.")]
        void LoadOntologySettings(string configPath = null, string sourceDir = null);

        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resource">Resource that is to be updated in the backing store.</param>
        /// <param name="modelUri">The uri of the model where the resource should be updated.</param>
        /// <param name="ignoreUnmappedProperties">Omits unmapped properties from the update query. This essentially deletes triples that do not match the mappings.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        void UpdateResource(Resource resource, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false);

        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resources">Resources that is to be updated in the backing store.</param>
        /// <param name="modelUri">The uri of the model where the resource should be updated.</param>
        /// <param name="ignoreUnmappedProperties">Omits unmapped properties from the update query. This essentially deletes triples that do not match the mappings.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        void UpdateResources(IEnumerable<Resource> resources, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false);

        /// <summary>
        /// Deletes a resource from a model.
        /// </summary>
        /// <param name="modelUri"></param>
        /// <param name="resourceUri"></param>
        /// <param name="transaction"></param>
        void DeleteResource(Uri modelUri, Uri resourceUri, ITransaction transaction = null);

        /// <summary>
        /// Deletes a resource from a model.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="transaction"></param>
        void DeleteResource(IResource resource, ITransaction transaction = null);

        /// <summary>
        /// Deletes a list of resource from a model.
        /// </summary>
        /// <param name="modelUri"></param>
        /// <param name="resources"></param>
        /// <param name="transaction"></param>
        void DeleteResources(Uri modelUri, IEnumerable<Uri> resources, ITransaction transaction = null);

        /// <summary>
        /// Deletes a list of resources from a model.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="transaction"></param>
        void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null);

        #endregion
    }
}
