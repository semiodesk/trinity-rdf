﻿// LICENSE:
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
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Writing;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This class encapsulates the functionality of an abstract triple store. Cannot be used directly. 
    /// Use StoreFactory to get a concrete implementation.
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
        /// Executes a string query directly on the store.
        /// </summary>
        /// <param name="queryString">SPARQL query to be executed.</param>
        /// <returns>A native return value is possible here.</returns>
        public virtual object ExecuteQuery(string queryString)
        {
            return null;
        }

        /// <summary>
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="update">SPARQL Update to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        public abstract void ExecuteNonQuery(ISparqlUpdate update, ITransaction transaction = null);

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
        public abstract Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update, bool leaveOpen=false);

        /// <summary>
        /// Loads a serialized graph from the given string into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="content">string containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public abstract Uri Read(string content, Uri graphUri, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="fs">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="namespaces">Defines namespace to prefix mappings for the output.</param>
        /// <param name="baseUri">Base URI for shortening URIs in formats that support it.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after the writing finished.</param>
        /// <returns></returns>
        public abstract void Write(Stream fs, Uri graphUri, RdfSerializationFormat format, INamespaceMap namespaces = null, Uri baseUri = null, bool leaveOpen = false);

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="fs">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store</param>
        /// <param name="writer">A RDF writer.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after the writing finished.</param>
        /// <returns></returns>
        public abstract void Write(Stream fs, Uri graphUri, IRdfWriter writer, bool leaveOpen = false);

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


        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resource">Resource that is to be updated in the backing store.</param>
        /// <param name="modelUri">Uri of the model where the resource will be updated</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <param name="ignoreUnmappedProperties">Set this to true to update only mapped properties.</param>
        public virtual void UpdateResource(Resource resource, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            string updateString;

            if (resource.IsNew)
            {
                if (resource.Uri.IsBlankId)
                {
                    string queryString = string.Format(@"SELECT BNODE() AS ?x FROM <{0}> WHERE {{}}", modelUri.OriginalString);

                    var result = ExecuteQuery(new SparqlQuery(queryString), transaction);
                    var id = result.GetBindings().First()["x"] as UriRef;

                    resource.Uri = id;
                }

                updateString = string.Format(@"
                    WITH <{0}>
                    INSERT {{ {1} }} 
                    WHERE {{}}",
                modelUri.OriginalString,
                SparqlSerializer.SerializeResource(resource, ignoreUnmappedProperties));
            }
            else
            {
                updateString = string.Format(@"
                    WITH <{0}>
                    DELETE {{ {1} ?p ?o. }}
                    INSERT {{ {2} }}
                    WHERE {{ OPTIONAL {{ {1} ?p ?o. }} }} ",
                modelUri.OriginalString,
                SparqlSerializer.SerializeUri(resource.Uri),
                SparqlSerializer.SerializeResource(resource, ignoreUnmappedProperties));
            }

            ExecuteNonQuery(new SparqlUpdate(updateString), transaction);

            resource.IsNew = false;
            resource.IsSynchronized = true;
        }

        /// <summary>
        /// Updates the properties of multiple resources in the backing RDF store.
        /// </summary>
        /// <param name="resources">Resources that are to be updated in the backing store.</param>
        /// <param name="modelUri">Uri of the model where the resource will be updated</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <param name="ignoreUnmappedProperties">Set this to true to update only mapped properties.</param>
        public virtual void UpdateResources(IEnumerable<Resource> resources, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            string WITH = $"{SparqlSerializer.SerializeUri(modelUri)} ";
            StringBuilder INSERT = new StringBuilder();
            StringBuilder DELETE = new StringBuilder();
            StringBuilder OPTIONAL = new StringBuilder();

            int count = 0;
            foreach (var res in resources)
            {
                DELETE.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p{count} ?o{count}. ");
                OPTIONAL.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p{count} ?o{count}. ");
                INSERT.Append($" {SparqlSerializer.SerializeResource(res, ignoreUnmappedProperties)} ");
                count++;
            }
            string updateString = $"WITH {WITH} DELETE {{ {DELETE} }} INSERT {{ {INSERT} }} WHERE {{ OPTIONAL {{ {OPTIONAL} }} }}";
            SparqlUpdate update = new SparqlUpdate(updateString);

            ExecuteNonQuery(update, transaction);

            foreach (var resource in resources)
            {
                resource.IsNew = false;
                resource.IsSynchronized = true;
            }
        }

        public void Write(Stream stream, IGraph graph, RdfSerializationFormat format, bool leaveOpen)
        {
            StreamWriter writer = new StreamWriter(stream);
            
            switch (format)
            {
                case RdfSerializationFormat.GZippedJsonLd:
                    {
                        var w = new GZippedJsonLdWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedN3:
                    {
                        var w = new GZippedNotation3Writer();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedNQuads:
                    {
                        var w = new GZippedNQuadsWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedRdfXml:
                    {
                        var w = new GZippedRdfXmlWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedTrig:
                    {
                        var w = new GZippedTriGWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedTurtle:
                    {
                        var w = new GZippedTurtleWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.Json:
                    {
                        var w = new RdfJsonWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }

#if !NET35
                case RdfSerializationFormat.JsonLd:
                    {
                        var w = new JsonLdWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
#endif
                case RdfSerializationFormat.N3:
                    {
                        var w = new Notation3Writer();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }

#if !NET35
                case RdfSerializationFormat.NQuads:
                    {
                        var w = new NQuadsWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
#endif
                case RdfSerializationFormat.NTriples:
                    {
                        var w = new NTriplesWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }

                
                case RdfSerializationFormat.RdfXml:
                    {
                        var w = new RdfXmlWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.Trig:
                    {
                        var w = new TriGWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                default:
                case RdfSerializationFormat.Turtle:
                    {
                        var w = new CompressingTurtleWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
            }

            if(leaveOpen)
            {
                writer.Flush();
            }
        }

        public void Write(Stream stream, IGraph graph, IRdfWriter formatWriter, bool leaveOpen)
        {
            StreamWriter streamWriter = new StreamWriter(stream);

            formatWriter.Save(graph, streamWriter, leaveOpen);

            if(leaveOpen)
            {
                streamWriter.Flush();
            }
        }

        public virtual void DeleteResource(Uri modelUri, Uri resourceUri, ITransaction transaction = null)
        {
            // NOTE: Regrettably, dotNetRDF does not support the full SPARQL 1.1 update syntax. To be precise,
            // it does not support FILTERs or OPTIONAL in Modify clauses.

            SparqlUpdate delete = new SparqlUpdate(@"
                DELETE WHERE { GRAPH @graph { @subject ?p ?o . } }; 
                DELETE WHERE { GRAPH @graph { ?s ?p @object . } }");
            delete.Bind("@graph", modelUri);
            delete.Bind("@subject", resourceUri);
            delete.Bind("@object", resourceUri);

            ExecuteNonQuery(delete, transaction);
        }

        public virtual void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            DeleteResource(resource.Model.Uri, resource.Uri, transaction);
        }

        public virtual void DeleteResources(Uri modelUri, IEnumerable<Uri> resources, ITransaction transaction = null)
        {
            foreach (var resource in resources)
                DeleteResource(modelUri, resource, transaction);
        }

        public virtual void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null)
        {
            foreach (var resource in resources)
                DeleteResource(resource, transaction);
        }

        /// <summary>
        /// Gets a SPARQL query which is used to retrieve all triples about a subject that is
        /// either referenced using a URI or blank node.
        /// </summary>
        /// <param name="modelUri">The graph to be queried.</param>
        /// <param name="subjectUri">The subject to be described.</param>
        /// <returns>An instance of <c>ISparqlQuery</c></returns>
        public virtual ISparqlQuery GetDescribeQuery(Uri modelUri, Uri subjectUri)
        {
            ISparqlQuery query = new SparqlQuery("DESCRIBE @subject FROM @model");
            query.Bind("@model", modelUri);
            query.Bind("@subject", subjectUri);

            return query;
        }

        #endregion
    }
}
