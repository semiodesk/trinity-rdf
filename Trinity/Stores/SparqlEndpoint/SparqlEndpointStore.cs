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
// Copyright (c) Semiodesk GmbH 2015-2019

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.Web;
using VDS.RDF.Query;
using System.IO;
using VDS.RDF.Parsing;

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// Storage that can can connect to Sparql Endpoints 
    /// 
    /// </summary>
    /// <see ref="http://www.w3.org/TR/rdf-sparql-protocol/#SparqlQuery"/>
    internal class SparqlEndpointStore : IStore
    {
        #region Members

        SparqlRemoteEndpoint _endpoint;

        /// <summary>
        /// Indicates if the store is ready to be queried.
        /// </summary>
        public bool IsReady
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Set this property to log the SPARQL queries which are executed on this store.
        /// For example, to log to the console, set this property to System.Console.Write(System.String).
        /// </summary>
        public Action<string> Log { get; set; }

        #endregion

        #region Constructor

        public SparqlEndpointStore(Uri endpointUri, IWebProxy proxy = null, ICredentials credentials = null)
        {
            _endpoint = new SparqlRemoteEndpoint(endpointUri);
            
            //_endpoint.Proxy = proxy;
        }

        #endregion

        #region Methods

        public ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
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

        public IModelGroup CreateModelGroup(params IModel[] models)
        {
            List<IModel> modelList = new List<IModel>();

            // This approach might seem a bit redundant, but we want to make sure to get the model from the right store.
            foreach (var x in models)
            {
                this.GetModel(x.Uri);
            }

            return new ModelGroup(this, modelList);
        }

        public void InitializeFromConfiguration(string configPath = null, string sourceDir = null)
        {
            throw new NotSupportedException();
        }

        public void LoadOntologySettings(string configPath = null, string sourceDir = null)
        {
            throw new NotSupportedException();
        }

        public IModel CreateModel(Uri uri)
        {
            throw new NotSupportedException();
        }

        public void RemoveModel(Uri uri)
        {
            throw new NotSupportedException();
        }

        public void RemoveModel(IModel model)
        {
            throw new NotSupportedException();
        }

        public bool ContainsModel(Uri uri)
        {
            throw new NotSupportedException();
        }

        public bool ContainsModel(IModel model)
        {
            throw new NotSupportedException();
        }

        public IModel GetModel(Uri uri)
        {
            return new Model(this, uri.ToUriRef());
        }

        public IModel GetOrCreateModel(Uri uri)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<IModel> ListModels()
        {
            throw new NotSupportedException();
        }

        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            string q = query.ToString();

            Log?.Invoke(q);

            SparqlQueryParser p = new SparqlQueryParser();

            var x = p.ParseFromString(q);
            x.ClearNamedGraphs();
            x.ClearDefaultGraphs();

            SparqlEndpointQueryResult result = null;

            if (query.QueryType == SparqlQueryType.Describe || query.QueryType == SparqlQueryType.Construct)
            {
                var r = _endpoint.QueryWithResultGraph(x.ToString());
                result = new SparqlEndpointQueryResult(r, query); 
            }
            else
            {
                var r = _endpoint.QueryWithResultSet(x.ToString());
                result = new SparqlEndpointQueryResult(r,  query);
            }

            return result;
        }

        public void ExecuteNonQuery(SparqlUpdate queryString, ITransaction transaction = null)
        {
            return;
        }
        public Uri Read(string content, Uri url, RdfSerializationFormat format, bool update)
        {
            throw new NotSupportedException();
        }

        public Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update)
        {
            throw new NotSupportedException();
        }

        public Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            throw new NotSupportedException();
        }

        public void Write(Stream fs, Uri graphUri, RdfSerializationFormat format, INamespaceMap namespaces = null)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            return;
        }

        public void UpdateResource(Resource resource, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            throw new NotSupportedException();
        }

        public void UpdateResources(IEnumerable<Resource> resources, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            throw new NotSupportedException();
        }

        public void DeleteResource(Uri modelUri, Uri resourceUri, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        public void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        public void DeleteResources(Uri modelUri, IEnumerable<Uri> resources, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        public void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
