/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) 2015 Semiodesk GmbH

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace Semiodesk.Trinity.Stores
{
    class FusekiStore : IStore
    {
        #region Members

        private FusekiConnector _connector;

        public bool IsReady { get; private set; }
        #endregion

        #region Constructor

        public FusekiStore(Uri location)
        {
            _connector = new FusekiConnector(location);
        }

        #endregion

        #region Methods
        #region IStore Members


        public Model AddModel(Uri uri)
        {
            //"SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } }"
            return null;
        }

        public void RemoveModel(Uri uri)
        {
            throw new NotImplementedException();
        }

        public void RemoveModel(Model model)
        {
            throw new NotImplementedException();
        }

        public bool ContainsModel(Uri uri)
        {
            throw new NotImplementedException();
        }

        public bool ContainsModel(Model model)
        {
            throw new NotImplementedException();
        }

        public Model GetModel(Uri uri)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IModel> ListModels()
        {
            throw new NotImplementedException();
        }

        public ISparqlQueryResult ExecuteQuery(SparqlQuery query, ITransaction transaction = null)
        {
            object result = _connector.Query(query.ToString());
            //return new SparqlEndpointQueryResult(result, query);
            return null;
        }

        public void ExecuteNonQuery(string queryString, ITransaction transaction = null)
        {
            throw new NotImplementedException();
        }

        public Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format)
        {
            throw new NotImplementedException();
        }

        public System.IO.MemoryStream Write(Uri graphUri, RdfSerializationFormat format)
        {
            throw new NotImplementedException();
        }

        #endregion
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
