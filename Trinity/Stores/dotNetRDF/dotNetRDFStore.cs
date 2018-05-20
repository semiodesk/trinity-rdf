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
// Copyright (c) Semiodesk GmbH 2015


using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Inference;
using VDS.RDF.Update;
using VDS.RDF.Writing;
using TrinitySettings = Semiodesk.Trinity.Configuration.TrinitySettings;

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// </summary>

    public class dotNetRDFStore : StoreBase
    {
        #region Members

        TripleStore _store;

        ISparqlUpdateProcessor _updateProcessor;

        ISparqlQueryProcessor _queryProcessor;

        SparqlUpdateParser _parser;

        RdfsReasoner _reasoner;


        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new dotNetRDFStore.
        /// </summary>
        /// <param name="schema">A list of ontology file paths relative to this assembly. The store will be populated with these ontologies.</param>
        public dotNetRDFStore(string[] schema)
        {
            _store = new TripleStore();
            _updateProcessor = new LeviathanUpdateProcessor(_store);
            _queryProcessor = new LeviathanQueryProcessor(_store);
            _parser = new SparqlUpdateParser();

            if (schema == null)
            {
                return;
            }

            _reasoner = new RdfsReasoner();
            _store.AddInferenceEngine(_reasoner);

            foreach (string m in schema)
            {
                var x = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
                FileInfo s = new FileInfo( Path.Combine(x.FullName, m));
                IGraph schemaGraph = LoadSchema(s.FullName);

                _store.Add(schemaGraph);
                _reasoner.Initialise(schemaGraph);
            }
        }

        #endregion

        #region Methods

        private IGraph LoadSchema(string schema)
        {
            IGraph graph = new Graph();
            graph.LoadFromFile(schema);

            string queryString = "SELECT ?s WHERE { ?s a <http://www.w3.org/2002/07/owl#Ontology>. }";

            SparqlResultSet result = (SparqlResultSet)graph.ExecuteQuery(queryString);

            graph.BaseUri = (result[0]["s"] as UriNode).Uri;

            return graph;
        }

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="uri">Uri of the model which is to be removed.</param>
        public override void RemoveModel(Uri uri)
        {
            if (_store.HasGraph(uri))
                _store.Remove(uri);
        }

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()
        /// </summary>
        /// <param name="uri">Uri of the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override bool ContainsModel(Uri uri)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        {
            return _store.HasGraph(uri);
        }

        /// <summary>
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="query">The update query</param>
        /// <param name="transaction">An associated transaction</param>
        public override void ExecuteNonQuery(SparqlUpdate query, ITransaction transaction = null)
        {
            SparqlUpdateCommandSet cmds = _parser.ParseFromString(query.ToString());

            _updateProcessor.ProcessCommandSet(cmds);
        }

        /// <summary>
        /// Executes a SparqlQuery on the store.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public override ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            if (query.IsInferenceEnabled && _reasoner != null)
            {
                _store.AddInferenceEngine(_reasoner);
            }
            else
            {
                _store.ClearInferenceEngines();
            }
            object results = ExecuteQuery(query.ToString());

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
        public object ExecuteQuery(string query)
        {
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            var q = sparqlparser.ParseFromString(query.ToString());
            return _queryProcessor.ProcessQuery(q);
        }

        /// <summary>
        /// Gets a handle to a model in the store.
        /// </summary>
        /// <param name="uri">Uri of the model.</param>
        /// <returns></returns>
        public override IModel GetModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        /// <summary>
        /// Indicates if the store is ready to be queried.
        /// </summary>
        public override bool IsReady
        {
            get;
            protected set;
        } = true;

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
        public override IEnumerable<IModel> ListModels()
        {
            foreach (var g in _store.Graphs)
            {
                if( g.BaseUri != null)
                    yield return new Model(this, new UriRef(g.BaseUri));
            }
        }


        public static IRdfReader GetReader(RdfSerializationFormat format)
        {
            switch (format)
            {
                case RdfSerializationFormat.N3:
                return new Notation3Parser();

                case RdfSerializationFormat.NTriples:
                return new NTriplesParser();

                case RdfSerializationFormat.Turtle:
                return new TurtleParser();
                default:
                case RdfSerializationFormat.RdfXml:
                return new RdfXmlParser();

            }
        }

        public static IRdfWriter GetWriter(RdfSerializationFormat format)
        {
            switch (format)
            {
                case RdfSerializationFormat.N3:
                    return new Notation3Writer();

                case RdfSerializationFormat.NTriples:
                    return new NTriplesWriter();

                case RdfSerializationFormat.Turtle:
                    return new CompressingTurtleWriter();
                default:
                case RdfSerializationFormat.RdfXml:
                    return new RdfXmlWriter();

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
        public override Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            TextReader reader = new StreamReader(stream);
            IGraph graph = new Graph();
            IRdfReader parser = GetReader(format);

            parser.Load(graph, reader);
            graph.BaseUri = graphUri;
            if (!update)
                _store.Remove(graphUri);
            _store.Add(graph, update);
            return graphUri;
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

                        foreach (VDS.RDF.Graph g in s.Graphs)
                        {
                            if (!update)
                                _store.Remove(g.BaseUri);
                            _store.Add(g, update);
                        }
                    }
                    else
                    {
                        graph = new Graph();
                        graph.LoadFromFile(path, GetReader(format));
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
                if (!update)
                    _store.Remove(graph.BaseUri);
                _store.Add(graph, update);

                return graphUri;
            }

            return null;
        }

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <returns></returns>
        public override void Write(Stream stream, Uri graphUri, RdfSerializationFormat format)
        {
            if (_store.HasGraph(graphUri))
            {
                IGraph graph = _store.Graphs[graphUri];
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    graph.SaveToStream(writer, GetWriter(format));
                }
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
        /// Closes the store. It is not usable after this call.
        /// </summary>
        public override void Dispose()
        {
            this.IsReady = false;
            _updateProcessor.Discard();
            _store.Dispose();
        }

        #endregion
    }
}
