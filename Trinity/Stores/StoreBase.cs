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

using Semiodesk.Trinity.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This class encapsulates the functionality of an abstract triple store. Cannot be used directly. 
    /// Use StoreFactory to get a concret implementation.
    /// </summary>
    public abstract class StoreBase : IStore
    {
        #region Methods

        /// <summary>
        /// Indicates if the store is connected and awaiting queries.
        /// </summary>
        public virtual bool IsReady { get; protected set; } = true;

        /// <summary>
        /// Set this property to log the SPARQL queries which are executed on this store.
        /// For example, to log to the console, set this property to System.Console.Write(System.String).
        /// </summary>
        public Action<string> Log { get; set; }

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="uri">Uri of the model which is to be removed.</param>
        public abstract void RemoveModel(Uri uri);

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="model">Handle of the model which is to be removed.</param>
        public virtual void RemoveModel(IModel model)
        {
            RemoveModel(model.Uri);
        }

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty
        /// </summary>
        /// <param name="uri">Uri of the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty")]
        public abstract bool ContainsModel(Uri uri);

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty
        /// </summary>
        /// <param name="model">Handle to the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty")]
        public virtual bool ContainsModel(IModel model)
        {
            return ContainsModel(model.Uri);
        }

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
        public abstract IEnumerable<IModel> ListModels();

        /// <summary>
        /// Executes a <c>SparqlQuery</c> on the store.
        /// </summary>
        /// <param name="query">SPARQL query to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        /// <returns></returns>
        public abstract ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null);

        /// <summary>
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="update">SPARQL Update to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        public abstract void ExecuteNonQuery(SparqlUpdate update, ITransaction transaction = null);

        /// <summary>
        /// Starts a transaction. The resulting transaction handle can be used to chain operations together.
        /// </summary>
        /// <param name="isolationLevel">Isolation level of the operations executed in the transaction.</param>
        /// <returns></returns>
        public abstract ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel);

        /// <summary>
        /// Loads a serialized graph from the given location into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="modelUri">Uri of the graph in this store</param>
        /// <param name="url">Location</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public abstract Uri Read(Uri modelUri, Uri url, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Loads a serialized graph from the given stream into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public abstract Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="fs">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <returns></returns>
        public abstract void Write(Stream fs, Uri graphUri, RdfSerializationFormat format);

        /// <summary>
        /// Initializes the store from the configuration. It uses either the provided file or attempts to load from "ontologies.config" located next to the executing assembly.
        /// For legacy reasons it also looks in the app.config file.
        /// If the ontology files are in a different path, this can be supplied as a base path..
        /// </summary>
        /// <param name="configPath">Load a specific configuration file.</param>
        /// <param name="sourceDir">If given, this function tries to load the ontologies from this folder.</param>
        [Obsolete("This method will be removed in the future. Use InitializeFromConfiguration() instead.")]
        public virtual void LoadOntologySettings(string configPath = null, string sourceDir = null)
        {
            var config = LoadConfiguration(configPath);

            LoadOntologies(config, sourceDir);
        }

        /// <summary>
        /// Initializes the store from the configuration. It uses either the provided file or attempts to load from "ontologies.config" located next to the executing assembly.
        /// For legacy reasons it also looks in the app.config file.
        /// If the ontology files are in a different path, this can be supplied as a base path..
        /// </summary>
        /// <param name="configPath">Path the configuration should be read from.</param>
        /// <param name="sourceDir">Path where the ontologies should be searched for.</param>
        public virtual void InitializeFromConfiguration(string configPath = null, string sourceDir = null)
        {
            var config = LoadConfiguration(configPath);

            LoadOntologies(config, sourceDir);
        }

        /// <summary>
        /// This method loads the configuration data from the given file. 
        /// This can read the old App.config and new ontologies.config files.
        /// </summary>
        /// <param name="configPath">Path to either ontologies.config or App.config file.</param>
        /// <returns></returns>
        protected IConfiguration LoadConfiguration(string configPath = null)
        {
            FileInfo configFile = null;

            if (!string.IsNullOrEmpty(configPath))
            {
                configFile = new FileInfo(configPath);
            }

            return ConfigurationLoader.LoadConfiguration(configFile);
        }

        /// <summary>
        /// Loads Ontologies defined in the currently loaded config file into the store.
        /// </summary>
        /// <param name="configuration">Handle of the configuration.</param>
        /// <param name="sourceDir">Searchpath for the ontologies.</param>
        protected void LoadOntologies(IConfiguration configuration, string sourceDir = null)
        {
            DirectoryInfo srcDir;

            if (string.IsNullOrEmpty(sourceDir))
            {
                srcDir = new DirectoryInfo(Environment.CurrentDirectory);
            }
            else
            {
                srcDir = new DirectoryInfo(sourceDir);
            }

            StoreUpdater updater = new StoreUpdater(this, srcDir);

            updater.UpdateOntologies(configuration.ListOntologies());
        }

        /// <summary>
        /// Disposes this store and it's underlying connection. This object cannot be reused after disposing.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Adds a new model with the given uri to the storage. 
        /// </summary>
        /// <param name="uri">Uri of the model</param>
        /// <returns>Handle to the model</returns>
        [Obsolete("It is not necessary to create models explicitly. Use GetModel() instead, if the model does not exist, it will be created implicitly.")]
        public virtual IModel CreateModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        /// <summary>
        /// Gets a handle to a model in the store.
        /// </summary>
        /// <param name="uri">Model URI.</param>
        /// <returns></returns>
        public virtual IModel GetModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of uris of the models that should be grouped together.</param>
        /// <returns></returns>
        public virtual IModelGroup CreateModelGroup(params Uri[] models)
        {
            List<IModel> result = new List<IModel>();

            foreach (var model in models)
            {
                result.Add(GetModel(model));
            }

            return new ModelGroup(this, result);
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of model handles that should be grouped together.</param>
        /// <returns></returns>
        public virtual IModelGroup CreateModelGroup(params IModel[] models)
        {
            List<IModel> result = new List<IModel>();

            foreach (var model in models)
            {
                result.Add(GetModel(model.Uri));
            }

            return new ModelGroup(this, result);
        }

        #endregion
    }
}
