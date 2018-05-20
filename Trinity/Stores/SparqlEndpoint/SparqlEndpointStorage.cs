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

        public bool IsReady
        {
            get { throw new NotImplementedException(); }
        }

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


        public void InitializeFromConfiguration(string configPath = null, string sourceDir = null)
        {
            throw new NotSupportedException();
        }

        public void LoadOntologies(string configPath = null, string sourceDir = null)
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

        public Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update)
        {
            throw new NotSupportedException();
        }

        public Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update)
        {
            throw new NotSupportedException();
        }

        public void Write(Stream fs, Uri graphUri, RdfSerializationFormat format)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            return;
        }

        #endregion
    }
}
