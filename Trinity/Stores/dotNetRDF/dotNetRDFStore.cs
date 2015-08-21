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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Inference;
using VDS.RDF.Update;
using VDS.RDF.Writing;


namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// </summary>

    public class dotNetRDFStore : IStore
    {
        #region Members

        TripleStore _store;
        LeviathanUpdateProcessor _updateProcessor;
        ISparqlQueryProcessor _queryProcessor;
        SparqlUpdateParser _parser;
        RdfsReasoner _reasoner;

        #endregion

        #region Constructor


        public dotNetRDFStore(string[] schema)
        {
            _store = new TripleStore();
            _updateProcessor = new LeviathanUpdateProcessor(_store);
            _queryProcessor = new LeviathanQueryProcessor(_store);
            _parser = new SparqlUpdateParser();
            if (schema != null)
            {
                _reasoner = new RdfsReasoner();
                _store.AddInferenceEngine(_reasoner);

                foreach (string m in schema)
                {
                    IGraph schemaGraph = LoadSchema(m);
                    _store.Add(schemaGraph);
                    _reasoner.Initialise(schemaGraph);
                }
            }

        }

        #endregion

        #region Methods

        private IGraph LoadSchema(string schema)
        {
            IGraph g = new Graph();
            g.LoadFromFile(schema);
            SparqlResultSet res = (SparqlResultSet)g.ExecuteQuery("select ?s where { ?s <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.w3.org/2002/07/owl#Ontology>. }");
            g.BaseUri = (res[0]["s"] as UriNode).Uri;

            return g;
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

        public ISparqlQueryResult ExecuteQuery(SparqlQuery query, ITransaction transaction = null)
        {
            if (query.InferenceEnabled && _reasoner != null)
            {

                _store.AddInferenceEngine(_reasoner);

            }
            else
            {
                //_store.RemoveInferenceEngine(_reasoner);
                _store.ClearInferenceEngines();
            }

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            var q = sparqlparser.ParseFromString(query.ToString());

            object results = _store.ExecuteQuery(q);
            if (results is IGraph)
                return new dotNetRDFQueryResult(this, query, results as IGraph);
            else if (results is SparqlResultSet)
                return new dotNetRDFQueryResult(this, query, results as SparqlResultSet);
            return null;
        }

        public object ExecuteQuery(string query)
        {
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            var q = sparqlparser.ParseFromString(query.ToString());
            return _queryProcessor.ProcessQuery(q);
        }

        public IModel GetModel(Uri uri)
        {
            if (ContainsModel(uri))
                return new Model(this, new UriRef(uri));
            else
                return null;
        }

        public bool IsReady
        {
            get { return true; }
        }

        public IEnumerable<IModel> ListModels()
        {
            foreach (var g in _store.Graphs)
            {
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


        public Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format)
        {
            TextReader reader = new StreamReader(stream);
            IGraph graph = new Graph();
            IRdfReader parser = GetReader(format);

            parser.Load(graph, reader);
            graph.BaseUri = graphUri;
            _store.Add(graph, true);
            return graphUri;
        }

        public Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format)
        {
            IGraph graph = null;

            if (url.AbsoluteUri.StartsWith("file:"))
            {
                string path;

                if (url.IsAbsoluteUri)
                {
                    path = url.AbsolutePath;
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
                            _store.Add(g, true);
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
                _store.Add(graph, true);

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
                _store.Remove(uri);
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

        #endregion
        #endregion
    }
}
