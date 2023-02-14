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
// Copyright (c) Semiodesk GmbH 2018

using OpenLink.Data.Virtuoso;
using Semiodesk.Trinity.Extensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System;
using VDS.RDF.Parsing;
using VDS.RDF;

namespace Semiodesk.Trinity.Store.Virtuoso
{
    /// <summary>
    /// This class is the implementation of the IStorage inteface for the Virtuoso Database.
    /// It provides the backend specific implementation of the storage management functions.
    /// </summary>
    internal class VirtuosoStore : StoreBase
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
        public override bool IsReady
        {
            get
            {
                return Connection.State == ConnectionState.Open;
            }
        }

        private string _defaultInferenceRule = "urn:semiodesk/ruleset";

        private bool _isDisposed = false;

        /// <summary>
        /// This property is being used to identify newly created resources 
        /// that have a blank node instead of a URI.
        /// </summary>
        /// <remarks>
        /// Virtuoso has no support for SPARQL BNODE().
        /// </remarks>
        private readonly Property _idProperty = new Property(new UriRef("http://trinity-rdf.net/id", UriKind.RelativeOrAbsolute));

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
            return "Server=" + Hostname + ":" + Port + ";uid=" + Username + ";pwd=" + Password + ";Charset=utf-8;CONNECTIONLIFETIME=30";
        }

        [Obsolete("It is not necessary to create models explicitly. Use GetModel() instead, if the model does not exist, it will be created implicitly.")]
        public override IModel CreateModel(Uri uri)
        {
            return GetModel(uri);
        }

        public override void RemoveModel(Uri uri)
        {
            using (ITransaction transaction = this.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var delete = new SparqlUpdate("DELETE FROM @graph WHERE { ?s ?p ?o . }").Bind("@graph", uri);
                    ExecuteNonQuery(delete, transaction);
                    
                    var clear = new SparqlUpdate("CLEAR GRAPH @graph").Bind("@graph", uri);
                    ExecuteNonQuery(clear, transaction);

                    var drop = new SparqlUpdate("DROP GRAPH @graph").Bind("@graph", uri);
                    ExecuteNonQuery(drop, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                }
            }
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public override bool ContainsModel(Uri uri)
        {
            if (uri != null)
            {
                using (ITransaction transaction = this.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    string query = string.Format("SPARQL ASK {{ GRAPH <{0}> {{ ?s ?p ?o . }} }}", uri.AbsoluteUri);

                    using (var result = ExecuteQuery(query, transaction))
                    {
                        return result.Rows.Count > 0;
                    }
                }
            }

            return false;
        }

        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty()")]
        public override bool ContainsModel(IModel model)
        {
            return ContainsModel(model.Uri);
        }

        public override IModel GetModel(Uri uri)
        {
            return new Model(this, uri.ToUriRef());
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

        public ITransaction BeginTransaction()
        {
            VirtuosoTransaction transaction = new VirtuosoTransaction(this);
            transaction.Transaction = Connection.BeginTransaction();

            return transaction;
        }

        public override ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            VirtuosoTransaction transaction = new VirtuosoTransaction(this);
            transaction.Transaction = Connection.BeginTransaction(isolationLevel);

            return transaction;
        }

        public override ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            return new VirtuosoSparqlQueryResult(query.Model, query, this, transaction);
        }

        public string CreateQuery(ISparqlQuery query)
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

                queryBuilder.Append("DEFINE input:inference '" + _defaultInferenceRule + "' \n");
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
                if (Connection.State != ConnectionState.Open)
                {
                    throw new Exception("Lost database connection to Virtuoso store.");
                }

                Log?.Invoke(queryString);

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
            }
            // This seems to be different in 7.x version of Openlink.Virtuoso.dll
            //catch (VirtuosoException e)
            //{

            //    if (e.ErrorCode == 40001)
            //        throw new ResourceLockedException(e);
            //    else

            //        throw;
            //}
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

                Log?.Invoke(queryString);

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
                    {
                        throw new ResourceLockedException(e);
                    }
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }
            }
        }

        public override void ExecuteNonQuery(ISparqlUpdate update, ITransaction transaction = null)
        {
            string queryString = string.Format("SPARQL {{ {0} }}", update.ToString());

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

        public override Uri Read(Uri graph, Uri url, RdfSerializationFormat format, bool update)
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

        public override Uri Read(Stream stream, Uri graph, RdfSerializationFormat format, bool update, bool leaveOpen = false)
        {
            using (TextReader reader = new StreamReader(stream))
            {
#if !NET35
                if (format == RdfSerializationFormat.Trig || format == RdfSerializationFormat.NQuads || format == RdfSerializationFormat.JsonLd)
#else
                if (format == RdfSerializationFormat.Trig)
#endif
                {
                    return ReadQuadFormat(reader, graph, format, update);
                }
                else
                {
                    return ReadTripleFormat(reader, graph, format, update);
                }
            }
        }

        public override Uri Read(string content, Uri graph, RdfSerializationFormat format, bool update)
        {
            
#if !NET35
                if (format == RdfSerializationFormat.Trig || format == RdfSerializationFormat.NQuads || format == RdfSerializationFormat.JsonLd)
#else
                if (format == RdfSerializationFormat.Trig)
#endif
                {
                    return ReadQuadFormat(content, graph, format, update);
                }
                else
                {
                    return ReadTripleFormat(content, graph, format, update);
                }
            
        }

        private IStoreReader GetStoreReader(RdfSerializationFormat format)
        {
            if (format == RdfSerializationFormat.JsonLd)
                return new JsonLdParser();
            if (format == RdfSerializationFormat.GZippedJsonLd)
                return new GZippedJsonLdParser();
            if (format == RdfSerializationFormat.NQuads)
                return new NQuadsParser();
            if (format == RdfSerializationFormat.GZippedNQuads)
                return new GZippedNQuadsParser();
            if (format == RdfSerializationFormat.Trig)
                return new TriGParser();
            if (format == RdfSerializationFormat.GZippedTrig)
                return new GZippedTriGParser();

            return null;
        }

        private IRdfReader GetParser(RdfSerializationFormat format)
        {
            if (format == RdfSerializationFormat.Turtle)
                return new TurtleParser();
            if (format == RdfSerializationFormat.GZippedTurtle)
                return new GZippedTurtleParser();
            if (format == RdfSerializationFormat.N3)
                return new Notation3Parser();
            if (format == RdfSerializationFormat.GZippedN3)
                return new GZippedNotation3Parser();
            if (format == RdfSerializationFormat.GZippedRdfXml)
                return new GZippedRdfXmlParser();
            if (format == RdfSerializationFormat.RdfXml)
                return new RdfXmlParser();
            return null;
        }

        private Uri ReadQuadFormat(TextReader reader, Uri graph, RdfSerializationFormat format, bool update)
        {
            using (VirtuosoManager manager = new VirtuosoManager(CreateConnectionString()))
            {
                using (ThreadSafeTripleStore store = new VDS.RDF.ThreadSafeTripleStore())
                {
                    IStoreReader parser = GetStoreReader(format);
                    if (parser == null)
                        throw new NotSupportedException();

                    parser.Load(store, reader);

                    foreach (var g in store.Graphs)
                    {
                        if (update)
                        {
                            manager.UpdateGraph(g.BaseUri, g.Triples, new Triple[] { });
                        }
                        else
                        {
                            manager.SaveGraph(g);
                        }
                    }
                }
            }

            return graph;
        }

        private Uri ReadQuadFormat(string content, Uri graph, RdfSerializationFormat format, bool update)
        {
            using (VirtuosoManager manager = new VirtuosoManager(CreateConnectionString()))
            {
                using (ThreadSafeTripleStore store = new VDS.RDF.ThreadSafeTripleStore())
                {
                    IStoreReader parser = GetStoreReader(format);
                    if (parser == null)
                        throw new NotSupportedException();

                    parser.Load(store, content);

                    var g = store.Graphs.Where(x => x.BaseUri == graph).FirstOrDefault();

                    if( g != null )
                    {
                        if (update)
                        {
                            manager.UpdateGraph(g.BaseUri, g.Triples, new Triple[] { });
                        }
                        else
                        {
                            manager.SaveGraph(g);
                        }
                    }
                }
            }

            return graph;
        }

        private Uri ReadTripleFormat(TextReader reader, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            using (VirtuosoManager manager = new VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph graph = new VDS.RDF.Graph())
                {
                    dotNetRDFStore.TryParse(reader, graph, format);

                    graph.BaseUri = graphUri;

                    if (update)
                    {
                        manager.UpdateGraph(graphUri, graph.Triples, new Triple[] { });
                    }
                    else
                    {
                        manager.SaveGraph(graph);
                    }
                }
            }

            return graphUri;
        }

        private Uri ReadTripleFormat(string content, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            using (VirtuosoManager manager = new VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph graph = new VDS.RDF.Graph())
                {
                    var parser = GetParser(format);
                    if (parser == null)
                        throw new NotSupportedException();

                    graph.LoadFromString(content, parser);

                    graph.BaseUri = graphUri;

                    if (update)
                    {
                        manager.UpdateGraph(graphUri, graph.Triples, new Triple[] { });
                    }
                    else
                    {
                        manager.SaveGraph(graph);
                    }
                }
            }

            return graphUri;
        }

        private Uri ReadRemoteTripleFormat(Uri graph, Uri location, RdfSerializationFormat format)
        {
            using (VirtuosoManager manager = new VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph g = new VDS.RDF.Graph())
                {
                    UriLoader.Load(g, location);

                    g.BaseUri = graph;

                    manager.SaveGraph(g);
                }
            }

            return graph;
        }

        public override void Write(Stream stream, Uri graph, RdfSerializationFormat format, INamespaceMap namespaces, Uri baseUri, bool leaveOpen = false)
        {
            using (VirtuosoManager manager = new VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph g = new VDS.RDF.Graph())
                {
                    if (namespaces != null)
                    {
                        g.NamespaceMap.ImportNamespaces(namespaces);
                    }

                    manager.LoadGraph(g, graph);

                    if (baseUri != null)
                    {
                        g.BaseUri = baseUri;
                    }

                    Write(stream, g, format, leaveOpen);
                }
            }
        }

        public override void Write(Stream stream, Uri graph, IRdfWriter formatWriter, bool leaveOpen = false)
        {
            using (VirtuosoManager manager = new VirtuosoManager(CreateConnectionString()))
            {
                using (VDS.RDF.Graph g = new VDS.RDF.Graph())
                {
                    manager.LoadGraph(g, graph);

                    Write(stream, g, formatWriter, leaveOpen);
                }
            }
        }

        public override IModelGroup CreateModelGroup(params Uri[] models)
        {
            List<IModel> result = new List<IModel>();

            foreach (var model in models)
            {
                result.Add(GetModel(model));
            }

            return new ModelGroup(this, result);
        }

        public override void InitializeFromConfiguration(string configPath = null, string sourceDir = "")
        {
            var config = LoadConfiguration(configPath);

            LoadOntologies(config, sourceDir);

            var settings = from x in config.ListStoreConfigurations() where x.Type == "virtuoso" select x;

            if (settings.Any())
            {
                VirtuosoSettings s = new VirtuosoSettings(settings.First());
                s.Update(this);
            }
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of model handles that should be grouped together.</param>
        /// <returns></returns>
        public override IModelGroup CreateModelGroup(params IModel[] models)
        {
            List<IModel> result = new List<IModel>();

            foreach (var model in models)
            {
                result.Add(GetModel(model.Uri));
            }

            return new ModelGroup(this, result);
        }

        public override void UpdateResource(Resource resource, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            string guid = null;
            string updateString;

            if (resource.IsNew)
            {
                if (resource.Uri.IsBlankId)
                {
                    // Virtuoso does not support the SPARQL 1.1 BNODE constructor. Therefore,
                    // we add a special id to the resource upon creation which we later
                    // use to retrieve the blank node ID the server as assigned to the resource..
                    guid = Guid.NewGuid().ToString();

                    resource.AddProperty(_idProperty, guid);
                }

                updateString = string.Format(@"
                    SPARQL
                    WITH <{0}>
                    INSERT {{ {1} }} ",
                    modelUri.OriginalString,
                    SparqlSerializer.SerializeResource(resource, ignoreUnmappedProperties));
            }
            else
            {
                updateString = string.Format(@"
                    SPARQL
                    WITH <{0}>
                    DELETE {{ {1} ?p ?o. }}
                    WHERE {{ OPTIONAL {{ {1} ?p ?o. }} }}
                    INSERT {{ {2} }} ",
                    modelUri.OriginalString,
                    SparqlSerializer.SerializeUri(resource.Uri),
                    SparqlSerializer.SerializeResource(resource, ignoreUnmappedProperties));
            }

            ExecuteDirectQuery(updateString, transaction);

            resource.IsNew = false;
            resource.IsSynchronized = true;

            if (!string.IsNullOrEmpty(guid))
            {
                // Retrieve the blank node id from the id property value.
                string queryString = string.Format(@"SPARQL SELECT ?x FROM <{0}> WHERE {{ ?x <http://trinity-rdf.net/id> '{1}' . }}",
                    modelUri.OriginalString,
                    guid);

                var result = ExecuteQuery(queryString, transaction);

                resource.Uri = new UriRef(result.Rows[0]["x"].ToString(), true);

                // Remove the id property from the resource *and* the model.
                resource.RemoveProperty(_idProperty, guid);

                updateString = string.Format(@"SPARQL DELETE FROM <{0}> {{ <{1}> <http://trinity-rdf.net/id> '{2}'. }}",
                    modelUri.OriginalString,
                    resource.Uri.OriginalString,
                    guid);

                ExecuteDirectQuery(updateString, transaction);
            }
        }

        public override void UpdateResources(IEnumerable<Resource> resources, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            string WITH = $"{SparqlSerializer.SerializeUri(modelUri)} ";
            StringBuilder INSERT = new StringBuilder();
            StringBuilder DELETE = new StringBuilder();
            StringBuilder OPTIONAL = new StringBuilder();

            int count = 0;
            foreach (var res in resources)
            {
                DELETE.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p{count} ?o{count}. ");
                OPTIONAL.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p{count} ?o{count}. ");
                INSERT.Append($" {SparqlSerializer.SerializeResource(res, ignoreUnmappedProperties)} ");
                count++;
            }
            string updateString = $"WITH {WITH} DELETE {{ {DELETE} }}  WHERE {{ OPTIONAL {{ {OPTIONAL} }} }} INSERT {{ {INSERT} }}";
            SparqlUpdate update = new SparqlUpdate(updateString);

            ExecuteNonQuery(update, transaction);

            foreach (var resource in resources)
            {
                resource.IsNew = false;
                resource.IsSynchronized = true;
            }
        }

        public override void DeleteResource(Uri modelUri, Uri resourceUri, ITransaction transaction = null)
        {
            SparqlUpdate delete = new SparqlUpdate(@"WITH @graph DELETE WHERE { ?s ?p ?o. FILTER( ?s = @subject || ?o = @object ) }");
            delete.Bind("@graph", modelUri);
            delete.Bind("@subject", resourceUri);
            delete.Bind("@object", resourceUri);

            ExecuteNonQuery(delete, transaction);
        }

        public override void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            DeleteResource(resource.Model.Uri, resource.Uri, transaction);
        }

        public override void DeleteResources(Uri modelUri, IEnumerable<Uri> resources, ITransaction transaction = null)
        {

            var template = "WITH @graph DELETE WHERE { ?s ?p ?o. FILTER( _filter_ )}";

            List<string> filters = new List<string>();

            int n = 0;

            foreach (var x in resources)
            {
                filters.Add($"?s = @subject{n} || ?o = @object{n}");
                n++;
            }

            SparqlUpdate c = new SparqlUpdate(template.Replace("_filter_", string.Join(" || ", filters)));
            c.Bind("@graph", modelUri);

            n = 0;

            foreach (var x in resources)
            {
                c.Bind("@subject" + n, x);
                c.Bind("@object" + n, x);
                n++;
            }

            ExecuteNonQuery(c, transaction);
        }

        public override void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null)
        {
            IModel model = resources.First().Model;

            if (resources.Any(x => x.Model.Uri != model.Uri))
            {
                throw new NotSupportedException();
            }

            var template = "WITH @graph DELETE WHERE { ?s ?p ?o. FILTER( _filter_ )}";

            List<string> filters = new List<string>();

            int n = 0;

            foreach ( var x in resources)
            {
                filters.Add($"?s = @subject{n} || ?o = @object{n}");
                n++;
            }
            
            SparqlUpdate c = new SparqlUpdate(template.Replace("_filter_", string.Join(" || ", filters)));
            c.Bind("@graph", model.Uri);
            
            n = 0;

            foreach (var x in resources)
            {
                c.Bind("@subject" + n, x.Uri);
                c.Bind("@object" + n, x.Uri);
                n++;
            }

            ExecuteNonQuery(c, transaction);
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

        ~VirtuosoStore()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                if (Connection != null)
                {
                    Connection.Close();
                    Connection.Dispose();
                }
            }

            _isDisposed = true;
        }

        #endregion
    }
}
