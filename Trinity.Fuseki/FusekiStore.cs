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
// Copyright (c) Semiodesk GmbH 2022

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using VDS.RDF.Parsing;
using VDS.RDF;
using Semiodesk.Trinity.Extensions;
using VDS.RDF.Storage;
using VDS.RDF.Query;
using VDS.RDF.Update;
using VDS.RDF.Parsing.Handlers;

namespace Semiodesk.Trinity.Store.Fuseki
{
    /// <summary>
    /// This class is the implementation of the IStorage inteface for the Fuseki Database.
    /// It provides the backend specific implementation of the storage management functions.
    /// </summary>
    public class FusekiStore : StoreBase
    {
        #region Members

        SparqlUpdateParser _parser;

        /// <summary>
        ///  Handle to the Virtuoso connection.
        /// </summary>
        protected FusekiConnector Connector;

        /// <summary>
        /// The host of the storage service.
        /// </summary>
        public string Hostname { get; protected set; }

        /// <summary>
        /// The username used for establishing the connection.
        /// </summary>
        private string Dataset { get; set; }


        private bool _isDisposed = false;

        #endregion

        #region Constructors


        /// <summary>
        /// Creates a new connection to the Virtuoso storage. 
        /// </summary>
        /// <param name="hostname">The host of the storage service.</param>
        /// <param name="port">The service port on the storage service host.</param>
        /// <param name="username">Username used to connect to storage.</param>
        /// <param name="password">Password needed to connect to storage.</param>
        public FusekiStore(string host, string dataset, string username = null, string password = null)
        {
            Hostname = host;
            Dataset = dataset;
            Connector = new FusekiConnector($"{Hostname}/{dataset}/data");
            if( username != null || password != null )
            Connector.SetCredentials(username, password);

            _parser = new SparqlUpdateParser();
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
            if (!Connector.DeleteSupported)
                throw new NotSupportedException("This store does not support the deletion of graphs.");

            Connector.DeleteGraph(uri);
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public override bool ContainsModel(Uri uri)
        {
            if (uri != null)
            {
                Connector.HasGraph(uri);

            }

            return false;
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public override bool ContainsModel(IModel model)
        {
            return ContainsModel(model.Uri);
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
            string q = query.ToString();

            Log?.Invoke(q);

            //SparqlUpdateCommandSet cmds = _parser.ParseFromString(q);

            Connector.Update(q);
        }

        /// <summary>
        /// Executes a SparqlQuery on the store.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public override ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            string q = query.ToString();

            object results = ExecuteQuery(q);

            if (results is IGraph)
            {
                return new dotNetRDFQueryResult(this, query, results as IGraph);
            }
            else if (results is SparqlResultSet)
            {
                return new dotNetRDFQueryResult(this, query, results as SparqlResultSet);
            }

            return null;
        }

        /// <summary>
        /// This method queries the dotNetRdf store directly.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override object ExecuteQuery(string query)
        {
            Log?.Invoke(query);
            return Connector.Query(query);
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
        /// Indicates if the store is ready to be queried.
        /// </summary>
        public override bool IsReady { get; protected set; } = true;

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
        public override IEnumerable<IModel> ListModels()
        {
            foreach (var graph in Connector.ListGraphs())
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


                default:
                case RdfSerializationFormat.RdfXml:
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
            var exists = Connector.HasGraph(graphUri);
            using (StringReader reader = new StringReader(content))
            {
                IGraph graph = new Graph();

                TryParse(reader, graph, format);

                graph.BaseUri = graphUri;

                if (!update && exists)
                {
                    Connector.DeleteGraph(graphUri);
                }

                Connector.SaveGraph(graph);

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
            var exists = Connector.HasGraph(graphUri);
            using (TextReader reader = new StreamReader(stream))
            {
                IGraph graph = new Graph();

                TryParse(reader, graph, format);

                graph.BaseUri = graphUri;

                if (!update && exists)
                {
                    Connector.DeleteGraph(graphUri);
                }

                Connector.SaveGraph(graph);

                if (!leaveOpen)
                    stream.Close();

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
            var exists = Connector.HasGraph(graphUri);

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
                        TripleStore s = new TripleStore();
                        s.LoadFromFile(path, new TriGParser());

                        foreach (Graph g in s.Graphs)
                        {
                            if (!update && exists)
                            {
                                Connector.DeleteGraph(graphUri);
                            }

                            Connector.SaveGraph(g);
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
                    Connector.DeleteGraph(graphUri);
                }

                Connector.SaveGraph(graph);

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
            if (Connector.HasGraph(graphUri))
            {
                IGraph graph = new Graph();
                Connector.LoadGraph(graph, graphUri);

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
            if (Connector.HasGraph(graphUri))
            {
                IGraph graph = new Graph();
                Connector.LoadGraph(graph, graphUri);

                Write(stream, graph, formatWriter, leaveOpen);
            }
        }

        /// <summary>
        /// 
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
            List<IModel> modelList = new List<IModel>();

            foreach (var x in models)
            {
                modelList.Add(GetModel(x));
            }

            return new ModelGroup(this, modelList);
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public new IModelGroup CreateModelGroup(params IModel[] models)
        {
            List<IModel> modelList = new List<IModel>();

            // This approach might seem a bit redundant, but we want to make sure to get the model from the right store.
            foreach (var x in models)
            {
                this.GetModel(x.Uri);
            }

            return new ModelGroup(this, modelList);
        }

        /// <summary>
        /// Closes the store. It is not usable after this call.
        /// </summary>
        public override void Dispose()
        {
            IsReady = false;
            Connector.Dispose();
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
            ISparqlQuery query = new SparqlQuery("DESCRIBE ?s FROM @model WHERE { ?s ?p ?o . VALUES ?s { @subject } }");
            query.Bind("@model", modelUri);
            query.Bind("@subject", subjectUri);

            return query;
        }

        #endregion
    }
}
