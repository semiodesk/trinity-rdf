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
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Data.Odbc;
using System.Data;
using System.Globalization;
using System.Diagnostics;
using System.ComponentModel;
using OpenLink.Data.Virtuoso;
using VDS.RDF.Parsing;
using VDS.RDF;
using Semiodesk.Trinity.Configuration;
using System.Configuration;
using System.Reflection;

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// This class is the implementation of the IStorage inteface for the Virtuoso Database.
    /// It provides the backend specific implementation of the storage management functions.
    /// </summary>
    internal class VirtuosoStore : IStore
    {
        #region Members

        /// <summary>
        ///  Handle to the Virtuoso connection.
        /// </summary>
        protected VirtuosoConnection Connection;

        /// <summary>
        /// The host of the storage service.
        /// </summary>
        public string Hostname { get; protected set; }

        /// <summary>
        /// The service port on the storage service host.
        /// </summary>
        public string Port { get; protected set; }

        /// <summary>
        /// The username used for establishing the connection.
        /// </summary>
        private string Username { get; set; }

        /// <summary>
        /// The password used for establishing the connection.
        /// </summary>
        private string Password { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public bool IsReady
        {
            get
            {
                return Connection.State == ConnectionState.Open;
            }
        }

        public bool ConnectUbiquity
        {
            get;
            set;
        }

        private string _defaultInferenceRule = "urn:semiodesk/ruleset";
        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new connection to the Virtuoso storage. 
        /// </summary>
        /// <param name="hostname">The host of the storage service.</param>
        /// <param name="port">The service port on the storage service host.</param>
        /// <param name="username">Username used to connect to storage.</param>
        /// <param name="password">Password needed to connect to storage.</param>
        /// <param name="defaultInferenceRule"></param>
        public VirtuosoStore(string hostname, int port, string username, string password, string defaultInferenceRule) : this(hostname, port, username, password)
        {
            _defaultInferenceRule = defaultInferenceRule;
        }

        /// <summary>
        /// Creates a new connection to the Virtuoso storage. 
        /// </summary>
        /// <param name="hostname">The host of the storage service.</param>
        /// <param name="port">The service port on the storage service host.</param>
        /// <param name="username">Username used to connect to storage.</param>
        /// <param name="password">Password needed to connect to storage.</param>
        public VirtuosoStore(string hostname, int port, string username, string password)
        {
            ConnectUbiquity = true;
            Hostname = hostname;
            Port = port.ToString();
            Username = username;
            Password = password;

            Connection = new VirtuosoConnection();
            Connection.ConnectionString = CreateConnectionString();
            
            Connection.Open();
        }

        /// <summary>
        /// Alternative constructor to create a Virtuoso storage connection.
        /// It automatically connectso to the local virtuoso store with the default port.
        /// </summary>
        /// <param name="username">Username used to connect to storage.</param>
        /// <param name="password">Password needed to connect to storage.</param>
        public VirtuosoStore(string username, string password)
            : this("localhost", 1111, username, password) { }

        #endregion

        #region Methods

        private string CreateConnectionString()
        {
            return "Server=" + Hostname + ":" + Port + ";uid=" + Username + ";pwd=" + Password + ";Charset=utf-8";
        }

        [Obsolete("It is not necessary to create models explicitly. Use GetModel() instead, if the model does not exist, it will be created implicitly.")]
        public IModel CreateModel(Uri uri)
        {
            //Model model = null;

            //using (ITransaction transaction = this.BeginTransaction(IsolationLevel.ReadCommitted))
            //{
            //    model = new Model(this, uri.ToUriRef());

            //    SparqlUpdate update = new SparqlUpdate(string.Format("CREATE GRAPH <{0}>", uri.AbsoluteUri));

            //    ExecuteNonQuery(update, transaction);

            //    transaction.Commit();
            //}

            //return model;
            return GetModel(uri);
        }

        public void RemoveModel(Uri uri)
        {
            using (ITransaction transaction = this.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    SparqlUpdate update = new SparqlUpdate(string.Format("CLEAR GRAPH <{0}>", uri.AbsoluteUri));
                    ExecuteNonQuery(update, transaction);
                    update = new SparqlUpdate(string.Format("DROP GRAPH <{0}>", uri.AbsoluteUri));
                    ExecuteNonQuery(update, transaction);
                    transaction.Commit();
                }
                catch (Exception)
                {
                }
            }
        }

        public void RemoveModel(IModel model)
        {
            RemoveModel(model.Uri);
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public bool ContainsModel(Uri uri)
        {
          if (uri == null)
            return false;

            bool result = false;

            using (ITransaction transaction = this.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                string query = string.Format("SPARQL ASK {{ GRAPH <{0}> {{ ?s ?p ?o . }} }}", uri.AbsoluteUri);

                var res = ExecuteQuery(query, transaction);
                result = res.Rows.Count > 0;
                res.Dispose();
            }

            return result;
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public bool ContainsModel(IModel model)
        {
            return ContainsModel(model.Uri);
        }

        public IModel GetModel(Uri uri)
        {
            return new Model(this, uri.ToUriRef());
        }

        public IEnumerable<IModel> ListModels()
        {
            SparqlQuery update = new SparqlQuery("SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } }");

            var result = ExecuteQuery(update);

            foreach(var bindingSet in result.GetBindings())
            {
                IModel m = null;

                try
                {
                    var x = bindingSet["g"];
                    m = new Model(this, new UriRef(x.ToString()));
                }
                catch (Exception)
                {
                    continue;
                }

                if (m != null)
                {
                    yield return m;
                }
            }
        }

        public ITransaction BeginTransaction()
        {
            VirtuosoTransaction transaction = new VirtuosoTransaction(this);
            transaction.Transaction = Connection.BeginTransaction();
            return transaction;
        }

        public ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            VirtuosoTransaction transaction = new VirtuosoTransaction(this);
            transaction.Transaction = Connection.BeginTransaction(isolationLevel);
            return transaction;
        }

        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            return new VirtuosoSparqlQueryResult(query.Model, query, this, transaction);
        }

        internal string CreateQuery(ISparqlQuery query)
        {
            StringBuilder queryBuilder = new StringBuilder();

            // Add Virtuoso specific describe mode for Describe queries.
            if (query.QueryType == SparqlQueryType.Describe)
            {
                // http://docs.openlinksw.com/virtuoso/rdfsparql.html: sql:describe-mode "SPO". 
                // This pair of procedures searches for all triples where the input IRIs are used 
                // as subjects; they are faster than the default routine which searches for all 
                // triples where the input IRIs are used as subjects or objects.
                queryBuilder.Append("DEFINE sql:describe-mode \"SPO\" \n");
            }

            // Add Virtuoso specific inferencing DEFINEs.
            // The models which can be used for inferencing is Virtuoso specific and
            // are therefore added here in the store.
            if (query.IsInferenceEnabled)
            {
                if (string.IsNullOrEmpty(_defaultInferenceRule))
                {
                    throw new Exception("You tried to query with inferencing but the inference rule is empty or not set.");
                }
                
                queryBuilder.Append("DEFINE input:inference '"+_defaultInferenceRule+"' \n");
            }

            queryBuilder.Append(query.ToString());

            return string.Format("SPARQL {0}", queryBuilder);
        }

        public DataTable ExecuteQuery(string queryString, ITransaction transaction = null)
        {
            DataTable result = new DataTable();

            VirtuosoDataAdapter adapter = null;
            VirtuosoCommand command = null;

            try
            {
                command = Connection.CreateCommand();
                command.CommandText = queryString;

                if (transaction != null && transaction is VirtuosoTransaction)
                {
                    command.Transaction = (transaction as VirtuosoTransaction).Transaction;
                }

                result.Columns.CollectionChanged += OnColumnsCollectionChanged;

                adapter = new VirtuosoDataAdapter(command);
                adapter.Fill(result);

                result.Columns.CollectionChanged -= OnColumnsCollectionChanged;
            }
            catch (InvalidOperationException ex)
            {
                string msg = string.Format("Error: Caught {0} exception.", ex.GetType());
                Debug.WriteLine(msg);
            } /* This seems to be different in 7.x version of Openlink.Virtuoso.dll
            catch (VirtuosoException e)
            {
               
                if (e.ErrorCode == 40001)
                    throw new ResourceLockedException(e);
                else
                
                    throw;
            } */
            finally
            {
                if (adapter != null)
                {
                    adapter.Dispose();
                }

                if (command != null)
                {
                    command.Dispose();
                }
            }

            return result;
        }

        protected void ExecuteDirectQuery(string queryString, ITransaction transaction = null)
        {
            VirtuosoCommand command = null;

            try
            {
                command = Connection.CreateCommand();
                command.CommandText = queryString;

                if (transaction is VirtuosoTransaction)
                {
                    command.Transaction = (transaction as VirtuosoTransaction).Transaction;
                }

                command.ExecuteNonQuery();
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Caught InvalidOperationExcetion.");
            }
            catch (VirtuosoException e)
            {
                if (e.Errors.Count > 0)
                {
                    var er = e.Errors[0];
                    if (er.SQLState == "40001")
                        throw new ResourceLockedException(e);
                }
               
                else
                    throw;
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }
            }
        }

        public void ExecuteNonQuery(SparqlUpdate query, ITransaction transaction = null)
        {
            string queryString = string.Format("SPARQL {0}", query.ToString());

            ExecuteDirectQuery(queryString, transaction);
        }

        private string GetLocalPathFromUrl(Uri url)
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
            return path;
        }

        public Uri Read(Uri graph, Uri url, RdfSerializationFormat format, bool update)
        {
            // Note: Accessing the file scheme here throws an exception in case the URL is relative..
            if (url.IsFile)
            {
                string path = GetLocalPathFromUrl(url);

                using (TextReader reader = File.OpenText(path))
                {
                    if (format == RdfSerializationFormat.Trig)
                    {
                        return ReadQuadFormat(reader, graph, format, update);
                    }
                    else
                    {
                        return ReadTripleFormat(reader, graph, format, update);
                    }
                } 
            }
            else if (url.Scheme == "http")
            {
                if (format == RdfSerializationFormat.Trig)
                {
                    throw new Exception("Loading of remote trig files is not supported yet.");
                }
                else
                {
                    return ReadRemoteTripleFormat(graph, url, format);
                }
            }
            else
            {
                string msg = string.Format("Unkown URL scheme {0}", url.Scheme);
                throw new ArgumentException(msg);
            }
        }

        public Uri Read(Stream stream, Uri graph, RdfSerializationFormat format, bool update)
        {
            using (TextReader reader = new StreamReader(stream))
            {
                if (format == RdfSerializationFormat.Trig)
                {
                    return ReadQuadFormat(reader, graph, format, update);
                }
                else
                {
                    return ReadTripleFormat(reader, graph, format, update);
                }
            }
        }

        private Uri ReadQuadFormat(TextReader reader, Uri graph, RdfSerializationFormat format, bool update)
        {
            using (VDS.RDF.Storage.VirtuosoManager m = new VDS.RDF.Storage.VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.ThreadSafeTripleStore store = new VDS.RDF.ThreadSafeTripleStore())
                {
                    VDS.RDF.Parsing.TriGParser p = new TriGParser();
                    p.Load(store, reader);
                    foreach (var x in store.Graphs)
                    {
                        if (update)
                            m.UpdateGraph(x.BaseUri, x.Triples, new Triple[] { });
                        else
                            m.SaveGraph(x);
                        
                    }
                }
            }

            return graph;
        }

        private Uri ReadTripleFormat(TextReader reader, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            using (VDS.RDF.Storage.VirtuosoManager m = new VDS.RDF.Storage.VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph graph = new VDS.RDF.Graph())
                {
                    IRdfReader parser = dotNetRDFStore.GetReader(format);
                    parser.Load(graph, reader);
                    graph.BaseUri = graphUri;
                    if (update)
                        m.UpdateGraph(graphUri, graph.Triples, new Triple[]{});
                    else
                        m.SaveGraph(graph);
                }
            }

            return graphUri;
        }

        private Uri ReadRemoteTripleFormat(Uri graph, Uri location, RdfSerializationFormat format)
        {
            using (VDS.RDF.Storage.VirtuosoManager m = new VDS.RDF.Storage.VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph g = new VDS.RDF.Graph())
                {
                    UriLoader.Load(g, location);
                    g.BaseUri = graph;
                    m.SaveGraph(g);
                }
            }

            return graph;
        }

        public void Write(Stream fs, Uri graph, RdfSerializationFormat format)
        {
            using (VDS.RDF.Storage.VirtuosoManager m = new VDS.RDF.Storage.VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph g = new VDS.RDF.Graph())
                {
                    m.LoadGraph(g, graph);

                    StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                    switch (format)
                    {
                        case RdfSerializationFormat.RdfXml:
                            {
                                VDS.RDF.Writing.RdfXmlWriter wr = new VDS.RDF.Writing.RdfXmlWriter();
                                wr.Save(g, sw);
                                break;
                            }
                    }
                }
            }
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

                if( settings == null )
                    throw new Exception(string.Format("TrinitySettings sections could not be found. Make sure the configSections element is at the top of the file {0}.", configPath));
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

            if (settings.VirtuosoStoreSettings != null)
            {
                IStorageSpecific spec = new VirtuosoSettings(settings.VirtuosoStoreSettings);
                updater.UpdateStorageSpecifics(spec);
            }
            
        }
        #endregion

        #region Event Handlers

        private void OnColumnsCollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            DataColumnCollection columns = sender as DataColumnCollection;

            if (columns != null)
            {
                columns[columns.Count - 1].DataType = typeof(Object);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }

        #endregion
    }



}
