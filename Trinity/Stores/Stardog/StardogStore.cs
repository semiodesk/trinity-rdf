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
using Newtonsoft.Json;
using Semiodesk.Trinity.Stores.Stardog;
using VDS.RDF;
using VDS.RDF.Storage;
using VDS.RDF.Writing;

namespace Semiodesk.Trinity.Store.Stardog
{
    public class StardogStore : StoreBase
    {
        #region Members

        private StardogConnector _connector;

        private StardogRdfHandler _rdfHandler;

        public bool IsReady
        {
            get { return true; }
        }

        #endregion

        #region Constructors

        public StardogStore(string host, string username, string password, string storeId)
        {
            _connector = new StardogConnector(host, storeId, username, password);
            _rdfHandler = new StardogRdfHandler();
        }

        #endregion

        #region Methods

        [Obsolete]
        public override IModel CreateModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        [Obsolete]
        public override bool ContainsModel(Uri uri)
        {
            string query = string.Format("ASK {{ GRAPH <{0}> {{ ?s ?p ?o . }} }}", uri.AbsoluteUri);

            var result = ExecuteQuery(query);
            {
                return result.BoolResult;
            }
        }

        public override void ExecuteNonQuery(SparqlUpdate query, ITransaction transaction = null)
        {
            if (!_connector.UpdateSupported)
            {
                throw new Exception("This store does not support SPARQL update.");
            }

            string q = query.ToString();

            if (_stardogTransaction?.IsActive ?? false)
            {
                Log?.Invoke($"**{q}");
                var convertor = new StardogUpdateSparqlConvertor(this);

                convertor.ParseQuery(q);
                Log?.Invoke($"UpdateGraph,{convertor.Additions.Count},{convertor.Removals.Count},{JsonConvert.SerializeObject(convertor.Additions)},{JsonConvert.SerializeObject(convertor.Removals)}");
                _stardogTransaction.AddTripleCount += convertor.Additions.Count;
                _stardogTransaction.RemoveTripleCount += convertor.Removals.Count;

                _connector.UpdateGraph(convertor.GraphUri, convertor.Additions, convertor.Removals);
            }
            else
            {
                // No transaction so just call update with the query
                Log?.Invoke(q);
                _connector.Update(q);
            }
        }

        public StardogResultHandler ExecuteQuery(string query, ITransaction transaction = null)
        {
            Log?.Invoke(query);

            StardogResultHandler resultHandler = new StardogResultHandler();

            _connector.Query(_rdfHandler, resultHandler, query);

            return resultHandler;
        }

        public override ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            string q = query.ToString();

            Log?.Invoke(q);

            StardogResultHandler resultHandler = new StardogResultHandler();
            _connector.Query(_rdfHandler, resultHandler, q, query.IsInferenceEnabled);

            return new StardogQueryResult(this, query, resultHandler);
        }

        public override IModel GetModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        public override IEnumerable<IModel> ListModels()
        {
            ISparqlQuery query = new SparqlQuery("SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } }");

            ISparqlQueryResult result = ExecuteQuery(query);

            foreach (BindingSet b in result.GetBindings())
            {
                IModel model = null;

                try
                {
                    var x = b["g"];

                    model = new Model(this, new UriRef(x.ToString()));
                }
                catch (Exception)
                {
                    continue;
                }

                if (model != null)
                {
                    yield return model;
                }
            }
        }

        public override Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            throw new NotImplementedException();
        }

        public override Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update)
        {
            throw new NotImplementedException();
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
            var query = $"SELECT * {{ GRAPH <{graphUri.AbsoluteUri}> {{ ?s ?p ?o . }} }}";

            var result = ExecuteQuery(query);

            if (result.SparqlResultSet.Result)
            {
                using (var graphEmpty = new Graph())
                using (var writer = new StreamWriter(stream))
                {
                    var triples = result.SparqlResultSet.ToTripleCollection(graphEmpty);

                    using (var graph = new Graph(triples))
                    {
                        graph.BaseUri = graphUri;

                        switch (format)
                        {
                            case RdfSerializationFormat.N3:
                                graph.SaveToStream(writer, new Notation3Writer());
                                break;

                            case RdfSerializationFormat.NTriples:
                                graph.SaveToStream(writer, new NTriplesWriter());
                                break;

#if !NET35
                            case RdfSerializationFormat.NQuads:
                                graph.SaveToStream(writer, new NQuadsWriter());
                                break;
#endif

                            case RdfSerializationFormat.Turtle:
                                graph.SaveToStream(writer, new CompressingTurtleWriter());
                                break;

                            case RdfSerializationFormat.Json:
                                graph.SaveToStream(writer, new RdfJsonWriter());
                                break;

#if !NET35
                            case RdfSerializationFormat.JsonLd:
                                graph.SaveToStream(writer, new JsonLdWriter());
                                break;
#endif
                            case RdfSerializationFormat.RdfXml:
                                graph.SaveToStream(writer, new RdfXmlWriter());
                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(format), format, null);
                        }
                    }
                }
            }
        }

        public override void RemoveModel(Uri uri)
        {
            try
            {
                SparqlUpdate clear = new SparqlUpdate(string.Format("CLEAR GRAPH <{0}>", uri.AbsoluteUri));

                ExecuteNonQuery(clear);
            }
            catch (Exception)
            {
            }
        }

        private StardogTransaction _stardogTransaction;
        public override ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            if (_stardogTransaction != null) throw new ApplicationException("Only one transaction is supported at a time.  Dispose and rollback changes or use the open transaction.");
            _stardogTransaction = new StardogTransaction(_connector);
            _stardogTransaction.OnFinishedTransaction += OnTransactionCompleted;
            return _stardogTransaction;
        }
        protected void OnTransactionCompleted(object sender, TransactionEventArgs e)
        {
            _stardogTransaction = null;
        }

        public IModelGroup CreateModelGroup(params Uri[] models)
        {
            List<IModel> modelList = new List<IModel>();

            foreach (var model in models)
            {
                modelList.Add(GetModel(model));
            }

            return new ModelGroup(this, modelList);
        }

        public IModelGroup CreateModelGroup(params IModel[] models)
        {
            List<IModel> modelList = new List<IModel>();

            // This approach might seem a bit redundant, but we want to make sure to get the model from the right store.
            foreach (var model in models)
            {
                GetModel(model.Uri);
            }

            return new ModelGroup(this, modelList);
        }

        public override void Dispose()
        {
            _stardogTransaction?.Dispose();
            _connector.Dispose();
        }

        #endregion
    }
}
