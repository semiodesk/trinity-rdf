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
// Copyright (c) Semiodesk GmbH 2023

using Semiodesk.Trinity.Extensions;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF;

namespace Semiodesk.Trinity.Store.GraphDB
{
    /// <summary>
    /// This class is the implementation of the IStorage inteface for GraphDB.
    /// </summary>
    public class GraphDBStore : StoreBase
    {
        #region Members
        
        /// <summary>
        /// Indicates if the store is ready to be queried.
        /// </summary>
        public override bool IsReady => _connector != null && _connector.IsReady;

        /// <summary>
        /// Handle to the database connection.
        /// </summary>
        private readonly GraphDBConnector  _connector;

        /// <summary>
        /// Get the URL of the GraphDB database service.
        /// </summary>
        public string HostUri { get; }

        #endregion

        #region Constructors
        
        /// <summary>
        /// Creates a new connection to the Virtuoso storage. 
        /// </summary>
        /// <param name="hostUri">The URL of the GraphDB database service.</param>
        /// <param name="repositoryName">Name of the GraphDB repository.</param>
        /// <param name="username">Username used to connect to storage.</param>
        /// <param name="password">Password needed to connect to storage.</param>
        public GraphDBStore(string hostUri, string repositoryName, string username = null, string password = null)
        {
            HostUri = hostUri;
            
            _connector = new GraphDBConnector(HostUri, repositoryName, username, password);

            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
            {
                _connector.SetCredentials(username, password);
            }
        }

        #endregion

        #region Methods
        
        [Obsolete("It is not necessary to create models explicitly. Use GetModel() instead, if the model does not exist, it will be created implicitly.")]
        public override IModel CreateModel(Uri uri)
        {
            return GetModel(uri);
        }

        public override void RemoveModel(Uri uri)
        {
            if (!_connector.DeleteSupported)
            {
                throw new NotSupportedException("This store does not support the deletion of graphs.");
            }

            _connector.DeleteGraph(uri);
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public override bool ContainsModel(Uri uri)
        {
            return uri != null && _connector.ListGraphs().Contains(uri);
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public override bool ContainsModel(IModel model)
        {
            return model != null && ContainsModel(model.Uri);
        }

        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resource">Resource that is to be updated in the backing store.</param>
        /// <param name="modelUri">Uri of the model where the resource will be updated</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <param name="ignoreUnmappedProperties">Set this to true to update only mapped properties.</param>
        public override void UpdateResource(Resource resource, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (modelUri == null) throw new ArgumentNullException(nameof(modelUri));
            
            string updateString;

            if (resource.IsNew)
            {
                updateString = string.Format(@"
                    INSERT DATA {{ GRAPH <{0}> {{  {1} }} }} ",
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
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="query">The update query</param>
        /// <param name="transaction">An associated transaction</param>
        public override void ExecuteNonQuery(ISparqlUpdate query, ITransaction transaction = null)
        {
            var q = query.ToString();

            Log?.Invoke(q);

            _connector.Update(q);
        }

        /// <summary>
        /// Executes a SparqlQuery on the store.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public override ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            var results = ExecuteQuery(query.ToString(), query.IsInferenceEnabled);

            switch (results)
            {
                case IGraph graph:
                    return new dotNetRDFQueryResult(this, query, graph);
                case SparqlResultSet set:
                    return new dotNetRDFQueryResult(this, query, set);
                default:
                    return null;
            }
        }

        /// <summary>
        /// This method queries the GraphDB store directly.
        /// </summary>
        /// <param name="query">The SPARQL query to be executed.</param>
        /// <returns></returns>
        public override object ExecuteQuery(string query)
        {
            Log?.Invoke(query);
            
            return _connector.Query(query, false, false);
        }
        
        /// <summary>
        /// This method queries the GraphDB store directly.
        /// </summary>
        /// <param name="query">The SPARQL query to be executed.</param>
        /// <param name="inferenceEnabled">Indicate if the query should be executed with reasoning.</param>
        /// <returns></returns>
        public object ExecuteQuery(string query, bool inferenceEnabled)
        {
            Log?.Invoke(query);
            
            return _connector.Query(query, false, inferenceEnabled);
        }

        /// <summary>
        /// Gets a handle to a model in the store.
        /// </summary>
        /// <param name="uri">Uri of the model.</param>
        /// <returns></returns>
        public override IModel GetModel(Uri uri)
        {
            return new Model(this, uri.ToUriRef());
        }

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
        public override IEnumerable<IModel> ListModels()
        {
            foreach (var graph in _connector.ListGraphs())
            {
                yield return new Model(this, new UriRef(graph));   
            }
        }

        /// <summary>
        /// Try parse RDF from a given text reader into the store.
        /// </summary>
        /// <param name="reader">The text reader to read from.</param>
        /// <param name="graph">The graph to store the read triples.</param>
        /// <param name="format">RDF format to be read.</param>
        public static void TryParse(TextReader reader, IGraph graph, RdfSerializationFormat format)
        {
            switch (format)
            {
                case RdfSerializationFormat.N3:
                    new Notation3Parser().Load(graph, reader); break;

                case RdfSerializationFormat.NTriples:
                    new NTriplesParser().Load(graph, reader); break;
                
                case RdfSerializationFormat.NQuads:
                    new NQuadsParser().Load(new GraphHandler(graph), reader); break;
                
                case RdfSerializationFormat.Turtle:
                    new TurtleParser().Load(graph, reader); break;

                case RdfSerializationFormat.Json:
                    new RdfJsonParser().Load(graph, reader); break;

                case RdfSerializationFormat.JsonLd:
                    new JsonLdParser().Load(new GraphHandler(graph), reader); break;
                
                case RdfSerializationFormat.RdfXml:
                default:
                    new RdfXmlParser().Load(graph, reader); break;
            }
        }

        /// <summary>
        /// Loads a serialized graph from the given String into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="content">String containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public override Uri Read(string content, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            var exists = _connector.ListGraphs().Contains(graphUri);
            
            using (var reader = new StringReader(content))
            {
                var graph = new Graph();

                TryParse(reader, graph, format);

                graph.BaseUri = graphUri;

                if (exists && !update)
                {
                    _connector.DeleteGraph(graphUri);
                }

                _connector.SaveGraph(graph);

                return graphUri;
            }
        }

        /// <summary>
        /// Loads a serialized graph from the given stream into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public override Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update, bool leaveOpen = false)
        {
            var exists = _connector.ListGraphs().Contains(graphUri);
            
            using (TextReader reader = new StreamReader(stream))
            {
                var graph = new Graph();

                TryParse(reader, graph, format);

                graph.BaseUri = graphUri;

                if (exists && !update)
                {
                    _connector.DeleteGraph(graphUri);
                }

                _connector.SaveGraph(graph);

                if (!leaveOpen)
                {
                    stream.Close();
                }

                return graphUri;
            }
        }

        /// <summary>
        /// Loads a serialized graph from the given location into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="url">Location</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public override Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update)
        {
            IGraph graph = null;
            
            var exists = _connector.ListGraphs().Contains(graphUri);

            if (url.AbsoluteUri.StartsWith("file:"))
            {
                string path;

                if (url.IsAbsoluteUri)
                {
                    path = url.LocalPath;
                }
                else
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), url.OriginalString.Substring(5));
                }

                if (graphUri != null)
                {
                    if (format == RdfSerializationFormat.Trig)
                    {
                        var store = new TripleStore();
                        store.LoadFromFile(path, new TriGParser());

                        foreach (Graph g in store.Graphs)
                        {
                            if (!update && exists)
                            {
                                _connector.DeleteGraph(graphUri);
                            }

                            _connector.SaveGraph(g);
                        }
                    }
                    else
                    {
                        graph = new Graph();
                        graph.LoadFromFile(path);
                        graph.BaseUri = graphUri;
                    }
                }
            }
            else if (url.Scheme == "http")
            {
                graph = new Graph();

                UriLoader.Load(graph, url);

                graph.BaseUri = graphUri;
            }

            if (graph != null)
            {
                if (!update && exists)
                {
                    _connector.DeleteGraph(graphUri);
                }

                _connector.SaveGraph(graph);

                return graphUri;
            }

            return null;
        }

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store.</param>
        /// <param name="format">Allowed formats.</param>
        /// <param name="namespaces">Defines namespace to prefix mappings for the output.</param>
        /// <param name="baseUri">Base URI for shortening URIs in formats that support it.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after writing completes.</param>
        /// <returns></returns>
        public override void Write(Stream stream, Uri graphUri, RdfSerializationFormat format, INamespaceMap namespaces = null, Uri baseUri = null, bool leaveOpen = false)
        {
            var graphs = _connector.ListGraphs();
            
            if (!graphs.Contains(graphUri)) return;
            
            var graph = new Graph();
                
            _connector.LoadGraph(graph, graphUri);

            if (namespaces != null)
            {
                graph.NamespaceMap.ImportNamespaces(namespaces);
            }

            if (baseUri != null)
            {
                graph.BaseUri = baseUri;
            }

            Write(stream, graph, format, leaveOpen);
        }

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store</param>
        /// <param name="formatWriter">A RDF format writer.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after writing completes.</param>
        /// <returns></returns>
        public override void Write(Stream stream, Uri graphUri, IRdfWriter formatWriter, bool leaveOpen = false)
        {
            if (!_connector.ListGraphs().Contains(graphUri)) return;
            
            IGraph graph = new Graph();
            
            _connector.LoadGraph(graph, graphUri);

            Write(stream, graph, formatWriter, leaveOpen);
        }

        /// <summary>
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        public override ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return null;
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public override IModelGroup CreateModelGroup(params Uri[] models)
        {
            var modelList = models.Select(m => GetModel(m)).ToList();

            return new ModelGroup(this, modelList);
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public new IModelGroup CreateModelGroup(params IModel[] models)
        {
            // This approach might seem a bit redundant, but we want to make sure to get the model from the right store.
            var modelList = models.Select(m => GetModel(m.Uri)).ToList();

            return new ModelGroup(this, modelList);
        }

        /// <summary>
        /// Closes the store. It is not usable after this call.
        /// </summary>
        public override void Dispose()
        {
            IsReady = false;
            
            _connector.Dispose();
        }

        /// <summary>
        /// Gets a SPARQL query which is used to retrieve all triples about a subject that is
        /// either referenced using a URI or blank node.
        /// </summary>
        /// <param name="modelUri">The graph to be queried.</param>
        /// <param name="subjectUri">The subject to be described.</param>
        /// <returns>An instance of <c>ISparqlQuery</c></returns>
        public override ISparqlQuery GetDescribeQuery(Uri modelUri, Uri subjectUri)
        {
            var query = new SparqlQuery("DESCRIBE ?s FROM @model WHERE { ?s ?p ?o . VALUES ?s { @subject } }");
            query.Bind("@model", modelUri);
            query.Bind("@subject", subjectUri);

            return query;
        }

        #endregion
    }
}
