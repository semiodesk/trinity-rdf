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
//  Mark Stemmler <mark.stemmler@schneider-electric.com>
//
// Copyright (c) Semiodesk GmbH 2015-2019

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
    /// <summary>
    /// A store adapter for Stardog databases.
    /// </summary>
    public class StardogStore : StoreBase
    {
        #region Members

        private StardogConnector _connector;

        private StardogRdfHandler _rdfHandler;

        private StardogTransaction _stardogTransaction;

        /// <summary>
        /// Indicates if the store is connected and awaiting queries.
        /// </summary>
        public new bool IsReady
        {
            get { return true; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new instance of the <c>StardogStore</c> class.
        /// </summary>
        /// <param name="host">URL of the host to connect to.</param>
        /// <param name="username">Username to be used when connecting.</param>
        /// <param name="password">Password to be used when connecting.</param>
        /// <param name="storeId">Knowledge base / database identifier.</param>
        public StardogStore(string host, string username, string password, string storeId)
        {
            _connector = new StardogConnector(host, storeId, username, password);
            _rdfHandler = new StardogRdfHandler();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a new model with the given uri to the storage. 
        /// </summary>
        /// <param name="uri">Uri of the model</param>
        /// <returns>Handle to the model</returns>
        [Obsolete]
        public override IModel CreateModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty
        /// </summary>
        /// <param name="uri">Uri of the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete]
        public override bool ContainsModel(Uri uri)
        {
            string query = string.Format("ASK {{ GRAPH <{0}> {{ ?s ?p ?o . }} }}", uri.AbsoluteUri);

            var result = ExecuteQuery(query);
            {
                return result.GetAnwser();
            }
        }

        /// <summary>
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="update">SPARQL Update to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        public override void ExecuteNonQuery(SparqlUpdate update, ITransaction transaction = null)
        {
            if (!_connector.UpdateSupported)
            {
                throw new Exception("This store does not support SPARQL update.");
            }

            string q = update.ToString();

            if (_stardogTransaction?.IsActive ?? false)
            {
                Log?.Invoke($"**{q}");

                var converter = new StardogUpdateSparqlConverter(this);
                converter.ParseQuery(q);

                Log?.Invoke($"UpdateGraph,{converter.Additions.Count},{converter.Removals.Count},{JsonConvert.SerializeObject(converter.Additions)},{JsonConvert.SerializeObject(converter.Removals)}");

                _stardogTransaction.AddTripleCount += converter.Additions.Count;
                _stardogTransaction.RemoveTripleCount += converter.Removals.Count;

                _connector.UpdateGraph(converter.GraphUri, converter.Additions, converter.Removals);
            }
            else
            {
                // No transaction so just call update with the query
                Log?.Invoke(q);

                _connector.Update(q);
            }
        }

        /// <summary>
        /// Executes a <c>SparqlQuery</c> on the store.
        /// </summary>
        /// <param name="query">SPARQL query string to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        /// <returns></returns>
        public StardogResultHandler ExecuteQuery(string query, ITransaction transaction = null)
        {
            Log?.Invoke(query);

            StardogResultHandler resultHandler = new StardogResultHandler();

            _connector.Query(_rdfHandler, resultHandler, query);

            return resultHandler;
        }

        /// <summary>
        /// Executes a <c>SparqlQuery</c> on the store.
        /// </summary>
        /// <param name="query">SPARQL query to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        /// <returns></returns>
        public override ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            string q = query.ToString();

            Log?.Invoke(q);

            StardogResultHandler resultHandler = new StardogResultHandler();
            _connector.Query(_rdfHandler, resultHandler, q, query.IsInferenceEnabled);

            return new StardogQueryResult(this, query, resultHandler);
        }

        /// <summary>
        /// Gets a handle to a model in the store.
        /// </summary>
        /// <param name="uri">Model URI.</param>
        /// <returns></returns>
        public override IModel GetModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads a serialized graph from the given location into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="modelUri">Uri of the graph in this store</param>
        /// <param name="url">Location</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public override Uri Read(Uri modelUri, Uri url, RdfSerializationFormat format, bool update)
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

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="uri">Uri of the model which is to be removed.</param>
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

        /// <summary>
        /// Starts a transaction. The resulting transaction handle can be used to chain operations together.
        /// </summary>
        /// <param name="isolationLevel">Isolation level of the operations executed in the transaction.</param>
        /// <returns></returns>
        public override ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            if (_stardogTransaction != null) throw new ApplicationException("Only one transaction is supported at a time.  Dispose and rollback changes or use the open transaction.");
            _stardogTransaction = new StardogTransaction(_connector);
            _stardogTransaction.OnFinishedTransaction += OnTransactionCompleted;
            return _stardogTransaction;
        }

        /// <summary>
        /// Invoked when a transaction is completed.
        /// </summary>
        /// <param name="sender">Object which invoked the event.</param>
        /// <param name="e">Event arguments.</param>
        protected void OnTransactionCompleted(object sender, TransactionEventArgs e)
        {
            _stardogTransaction = null;
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of uris of the models that should be grouped together.</param>
        /// <returns></returns>
        public override IModelGroup CreateModelGroup(params Uri[] models)
        {
            List<IModel> modelList = new List<IModel>();

            foreach (var model in models)
            {
                modelList.Add(GetModel(model));
            }

            return new ModelGroup(this, modelList);
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of model handles that should be grouped together.</param>
        /// <returns></returns>
        public override IModelGroup CreateModelGroup(params IModel[] models)
        {
            List<IModel> modelList = new List<IModel>();

            // This approach might seem a bit redundant, but we want to make sure to get the model from the right store.
            foreach (var model in models)
            {
                GetModel(model.Uri);
            }

            return new ModelGroup(this, modelList);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _stardogTransaction?.Dispose();
            _connector.Dispose();
        }

        #endregion
    }
}
