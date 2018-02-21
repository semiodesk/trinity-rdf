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
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Inference;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;
using VDS.RDF.Update;
using VDS.RDF.Writing;
using TrinitySettings = Semiodesk.Trinity.Configuration.TrinitySettings;

namespace Semiodesk.Trinity.Store.Stardog
{
    /// <summary>
    /// </summary>

    class StardogStore : IStore
    {
        #region Members

        StardogConnector _connector;
        StardogRdfHandler _rdfHandler;
        

        #endregion

        #region Constructors

        public StardogStore(string host, string username, string password, string storeId)
        {
            _connector = new StardogConnector(host, storeId, username, password);
            _rdfHandler = new StardogRdfHandler();
        }

        #endregion

        #region Methods

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
            if (uri != null)
            {

                string query = string.Format("ASK {{ GRAPH <{0}> {{ ?s ?p ?o . }} }}", uri.AbsoluteUri);

                var result = ExecuteQuery(query);
                {
                    return result.BoolResult;
                }
                
            }

            return false;
        }

        public void ExecuteNonQuery(SparqlUpdate query, ITransaction transaction = null)
        {
            if (!_connector.UpdateSupported)
                throw new Exception("This store does not support SPARQL update.");
            this._connector.Update(query.ToString());
        }

        public StardogResultHandler ExecuteQuery(string query, ITransaction transaction = null)
        {
            StardogResultHandler resultHandler = new StardogResultHandler();
            this._connector.Query(_rdfHandler, resultHandler, query);

            return resultHandler;
        }

        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            bool reasoning = query.IsInferenceEnabled;
            StardogResultHandler resultHandler = new StardogResultHandler();
            this._connector.Query(_rdfHandler, resultHandler, query.ToString(), reasoning);

            return new StardogQueryResult(this, query, resultHandler);
        }

        public IModel GetModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        public bool IsReady
        {
            get { return true; }
        }

        public IEnumerable<IModel> ListModels()
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
            return null;
        }

        public Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update)
        {
           
            return null;
        }

        public void RemoveModel(IModel model)
        {
            RemoveModel(model.Uri);
        }

        public void RemoveModel(Uri uri)
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

        public void Write(Stream stream, Uri graphUri, RdfSerializationFormat format)
        {
            return;
        }

        public ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return null;
        }

        public IModelGroup CreateModelGroup(params Uri[] models)
        {
            return null;
        }

        public void Dispose()
        {
            _connector.Dispose();
        }

        public void LoadOntologySettings(string configPath = null, string sourceDir = "")
        {
            Trinity.Configuration.TrinitySettings settings;
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();

                configMap.ExeConfigFilename = configPath;

                var configuration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                try
                {
                    settings = (TrinitySettings)configuration.GetSection("TrinitySettings");
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Could not read config file from {0}. Reason: {1}", configPath, e.Message));
                }

            }
            else
            {
                settings = (TrinitySettings)ConfigurationManager.GetSection("TrinitySettings");
            }

            DirectoryInfo srcDir;
            if (string.IsNullOrEmpty(sourceDir))
            {
                srcDir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }
            else
            {
                srcDir = new DirectoryInfo(sourceDir);
            }
            StoreUpdater updater = new StoreUpdater(this, srcDir);
            updater.UpdateOntologies(settings.Ontologies);
        }
        #endregion
        #endregion
    }
}
