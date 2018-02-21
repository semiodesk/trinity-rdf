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
using System.Reflection;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Inference;
using VDS.RDF.Update;
using VDS.RDF.Writing;

using TrinitySettings = Semiodesk.Trinity.Configuration.TrinitySettings;

namespace Semiodesk.Trinity.Store
{
    public class dotNetRDFStore : IStore
    {
        #region Members

        TripleStore _store;

        ISparqlUpdateProcessor _updateProcessor;

        ISparqlQueryProcessor _queryProcessor;

        SparqlUpdateParser _parser;

        RdfsReasoner _reasoner;

        #endregion

        #region Constructors

        public dotNetRDFStore(string[] schemes)
        {
            _store = new TripleStore();
            _updateProcessor = new LeviathanUpdateProcessor(_store);
            _queryProcessor = new LeviathanQueryProcessor(_store);
            _parser = new SparqlUpdateParser();

            if (schemes != null)
            {
                _reasoner = new RdfsReasoner();
                _store.AddInferenceEngine(_reasoner);

                foreach (string s in schemes)
                {
                    var directory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
                    var file = new FileInfo(Path.Combine(directory.FullName, s));

                    IGraph schemaGraph = LoadSchema(file.FullName);

                    _store.Add(schemaGraph);
                    _reasoner.Initialise(schemaGraph);
                }
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

        #region IStore implementation

        public IModel CreateModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        public bool ContainsModel(IModel model)
        {
            return ContainsModel(model.Uri);
        }

        public bool ContainsModel(Uri uri)
        {
            return _store.HasGraph(uri);
        }

        public void ExecuteNonQuery(SparqlUpdate query, ITransaction transaction = null)
        {
            SparqlUpdateCommandSet cmds = _parser.ParseFromString(query.ToString());

            _updateProcessor.ProcessCommandSet(cmds);
        }

        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            if (query.IsInferenceEnabled && _reasoner != null)
            {
                _store.AddInferenceEngine(_reasoner);
            }
            else
            {
                _store.ClearInferenceEngines();
            }

            object results = _store.ExecuteQuery(query.ToString());

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

        public object ExecuteQuery(string query)
        {
            SparqlQueryParser parser = new SparqlQueryParser();

            var q = parser.ParseFromString(query);

            return _queryProcessor.ProcessQuery(q);
        }

        public IModel GetModel(Uri uri)
        {
            return new Model(this, uri.ToUriRef());
        }

        public bool IsReady
        {
            get { return true; }
        }

        public IEnumerable<IModel> ListModels()
        {
            foreach (var graph in _store.Graphs)
            {
                if (graph.BaseUri != null)
                {
                    yield return new Model(this, new UriRef(graph.BaseUri));
                }
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


        public Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            IRdfReader parser = GetReader(format);
            TextReader reader = new StreamReader(stream);

            IGraph graph = new Graph();

            parser.Load(graph, reader);

            graph.BaseUri = graphUri;

            if (!update)
            {
                _store.Remove(graphUri);
            }

            _store.Add(graph, update);

            return graphUri;
        }

        public Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update)
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

                        foreach (Graph g in s.Graphs)
                        {
                            if (!update)
                            {
                                _store.Remove(g.BaseUri);
                            }

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
                {
                    _store.Remove(graph.BaseUri);
                }

                _store.Add(graph, update);

                return graphUri;
            }

            return null;
        }

        public void RemoveModel(IModel model)
        {
            RemoveModel(model.Uri);
        }

        public void RemoveModel(Uri uri)
        {
            if (_store.HasGraph(uri))
            {
                _store.Remove(uri);
            }
        }

        public void Write(Stream stream, Uri graphUri, RdfSerializationFormat format)
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

        public ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return null;
        }

        public IModelGroup CreateModelGroup(params Uri[] models)
        {
            List<IModel> modelList = new List<IModel>();

            foreach (var x in models)
            {
                modelList.Add(GetModel(x));
            }

            return new ModelGroup(this, modelList);
        }

        public void Dispose()
        {
            _updateProcessor.Discard();
            _store.Dispose();
        }

        public void LoadOntologySettings(string configPath = null, string sourceDir = "")
        {
            TrinitySettings settings;

            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();

                configMap.ExeConfigFilename = configPath;

                var configuration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

                try
                {
                    settings = configuration.GetSection("TrinitySettings") as TrinitySettings;
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Could not read config file from {0}. Reason: {1}", configPath, e.Message));
                }
            }
            else
            {
                settings = ConfigurationManager.GetSection("TrinitySettings") as TrinitySettings;
            }

            DirectoryInfo d;

            if (string.IsNullOrEmpty(sourceDir))
            {
                d = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }
            else
            {
                d = new DirectoryInfo(sourceDir);
            }

            StoreUpdater updater = new StoreUpdater(this, d);
            updater.UpdateOntologies(settings.Ontologies);
        }

        #endregion
        #endregion
    }
}
