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

using Semiodesk.Trinity.Extensions;
using System.Collections.Generic;
using System.IO;
using System;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF;

namespace Semiodesk.Trinity.Store.Fuseki
{
    /// <summary>
    /// This class is the implementation of the IStorage interface for Fuseki.
    /// </summary>
    public class FusekiStore : StoreBase
    {
        #region Members

        /// <summary>
        /// Handle to the Fuseki connection.
        /// </summary>
        protected FusekiConnector Connector;

        /// <summary>
        /// The host of the storage service.
        /// </summary>
        public string Hostname { get; protected set; }

        /// <summary>
        /// The name of the Fuseki dataset this store is bound to.
        /// </summary>
        public string Dataset { get; protected set; }

        /// <summary>
        /// Indicates if the store is connected and awaiting queries.
        /// </summary>
        public override bool IsReady => Connector != null && Connector.IsReady;
        
        #endregion

        #region Constructors
        
        /// <summary>
        /// Creates a new connection to an Apache Jena Fuseki storage.
        /// </summary>
        /// <param name="host">Base URI of the Fuseki server, e.g. <c>http://localhost:3030</c>.</param>
        /// <param name="dataset">Name of the dataset to bind to, e.g. <c>ds</c>.</param>
        /// <param name="username">Username used to connect to storage.</param>
        /// <param name="password">Password needed to connect to storage.</param>
        public FusekiStore(string host, string dataset, string username = null, string password = null)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("A Fuseki host is required.", nameof(host));
            }

            if (string.IsNullOrEmpty(dataset))
            {
                // Without a dataset every request 404s, and nothing about the resulting store says
                // why -- it connects, reports ready, and fails only when queried.
                throw new ArgumentException("A Fuseki dataset name is required.", nameof(dataset));
            }

            // Trim the trailing slash a caller may or may not supply, or the composed URL ends up
            // with a double slash (the provider's default host carries one, a mapped container
            // host does not).
            Hostname = host.TrimEnd('/');
            Dataset = dataset;

            // The connector takes the Graph Store Protocol endpoint and derives the query and
            // update endpoints from it by replacing the trailing "data", so this URL is the one
            // it needs -- /<dataset>/query and /<dataset>/update are what Fuseki serves.
            Connector = new FusekiConnector($"{Hostname}/{Dataset}/data");

            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
            {
                Connector.SetCredentials(username ?? "", password ?? "");
            }
        }

        #endregion

        #region Methods


        /// <summary>
        /// Adds a new model with the given URI to the store.
        /// </summary>
        /// <param name="uri">URI of the model.</param>
        /// <returns>Handle to the model.</returns>
        [Obsolete("It is not necessary to create models explicitly. Use GetModel() instead, if the model does not exist, it will be created implicitly.")]
        public override IModel CreateModel(Uri uri)
        {
            return GetModel(uri);
        }

        /// <summary>
        /// Removes the model with the given URI from the store.
        /// </summary>
        /// <param name="uri">URI of the model.</param>
        public override void RemoveModel(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!Connector.DeleteSupported)
            {
                throw new NotSupportedException("This store does not support the deletion of graphs.");
            }

            Connector.DeleteGraph(uri);
        }

        /// <summary>
        /// Queries whether the model exists in the store.
        /// </summary>
        /// <param name="uri">URI of the model.</param>
        /// <returns><c>true</c> if the store holds a graph with that URI.</returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public override bool ContainsModel(Uri uri)
        {
            return uri != null && Connector.HasGraph(uri);
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
            else if (TryBuildDeltaUpdate(resource, modelUri, ignoreUnmappedProperties, out updateString))
            {
                if (updateString == null)
                {
                    // Nothing changed since this copy was loaded — writing would only risk clobbering
                    // whatever another writer has done in the meantime.
                    resource.IsNew = false;
                    resource.IsSynchronized = true;

                    return;
                }
            }
            else
            {
                // The resource was never synchronized, so there is no baseline to diff against and the
                // whole resource has to be replaced.
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

            Connector.Update(q);
        }

        /// <summary>
        /// Executes a SparqlQuery on the store.
        /// </summary>
        /// <remarks>
        /// <c>query.IsInferenceEnabled</c> is ignored. Fuseki has no per-query inference switch --
        /// a reasoner is a property of the dataset, so it applies to every query or to none -- and
        /// ADR-0022 makes inferencing a capability a store may ignore rather than a contract.
        /// </remarks>
        /// <param name="query">The query to be executed.</param>
        /// <param name="transaction">Transaction associated with this action. Ignored; Fuseki is not
        /// transactional (see <see cref="BeginTransaction"/>).</param>
        /// <returns>The query result, or <c>null</c> for a query form the connector does not answer
        /// with a graph or a result set.</returns>
        public override ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            var q = query.ToString();
            var results = ExecuteQuery(q);

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
        /// This method queries the Fuseki store directly.
        /// </summary>
        /// <param name="queryString">The SPARQL query to be executed.</param>
        /// <returns>An <c>IGraph</c> or a <c>SparqlResultSet</c>, depending on the query form.</returns>
        public override object ExecuteQuery(string queryString)
        {
            Log?.Invoke(queryString);
            
            return Connector.Query(queryString);
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
            // ListGraphNames rather than the obsolete ListGraphs; it yields the names as strings
            // (ADR-0038).
            foreach (var graph in Connector.ListGraphNames())
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

                case RdfSerializationFormat.Trig:
                    new TriGParser().Load(new GraphHandler(graph), reader); break;

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
            var exists = Connector.HasGraph(graphUri);
            
            using (StringReader reader = new StringReader(content))
            {
                IGraph graph = new Graph(graphUri);

                TryParse(reader, graph, format);

                // Restore the target graph: a parsed @base directive overwrites BaseUri, and
                // dotNetRDF connectors still derive the graph they write to from BaseUri.
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
                IGraph graph = new Graph(graphUri);

                TryParse(reader, graph, format);

                // Restore the target graph: a parsed @base directive overwrites BaseUri, and
                // dotNetRDF connectors still derive the graph they write to from BaseUri.
                graph.BaseUri = graphUri;


                if (!update && exists)
                {
                    Connector.DeleteGraph(graphUri);
                }

                Connector.SaveGraph(graph);

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
                        graph = new Graph(graphUri);
                        graph.LoadFromFile(path);

                        // Restore the target graph: a parsed @base directive overwrites BaseUri,
                        // and dotNetRDF connectors still derive the graph they write to from it.
                        graph.BaseUri = graphUri;
                    }
                }
            }
            else if (url.Scheme == "http" || url.Scheme == "https")
            {
                graph = new Graph(graphUri);

                UriLoader.Load(graph, url);

                // Restore the target graph: a parsed @base directive overwrites BaseUri, and
                // dotNetRDF connectors still derive the graph they write to from BaseUri.
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
                IGraph graph = new Graph(graphUri);
                
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
                IGraph graph = new Graph(graphUri);
                
                Connector.LoadGraph(graph, graphUri);

                Write(stream, graph, formatWriter, leaveOpen);
            }
        }

        /// <summary>
        /// Returns a no-op transaction handle. Fuseki is not transactional through this adapter.
        /// </summary>
        /// <param name="isolationLevel">Ignored; there is no isolation to configure.</param>
        /// <returns>A handle whose Commit and Rollback do nothing (ADR-0028, ADR-0039).</returns>
        public override ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            // Not transactional — the Fuseki connector exposes no transaction handle. A no-op handle is returned rather
            // than null so callers need not null-check and so transaction code stays testable.
            return new NoOpTransaction();
        }

        /// <summary>
        /// Closes the store. It is not usable after this call.
        /// </summary>
        public override void Dispose()
        {
            if (Connector == null)
            {
                return;
            }

            Connector.Dispose();
            Connector = null;
        }

        /// <summary>
        /// Gets a SPARQL query which is used to retrieve all triples about a subject.
        /// </summary>
        /// <remarks>
        /// URI subjects only. The subject is bound through <c>VALUES</c>, which admits an IRI or a
        /// literal but never a blank node, so a blank-node subject would emit unparseable SPARQL.
        /// Unreachable in practice: <c>Model.GetResource</c> rejects a blank id before it gets here.
        /// </remarks>
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
