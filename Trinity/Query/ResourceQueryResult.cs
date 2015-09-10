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

namespace Semiodesk.Trinity
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceQueryResult : IResourceQueryResult
    {
        #region Members

        IModel _model;

        ResourceQuery _query;

        ITransaction _transaction;

        bool _inferenceEnabled;

        bool IsSorted
        {
            get
            {
                foreach( var statement in _query.WhereStatements)
                {
                    if (statement.SortDirection != SortDirection.None)
                        return true;
                }
                return false;
            }
        }

        #endregion

        #region Constructors

        public ResourceQueryResult(IModel model, ResourceQuery query, bool inferenceEnabled, ITransaction transaction)
        {
            _model = model;
            _query = query;
            _inferenceEnabled = inferenceEnabled;
            _transaction = transaction;
        }

        #endregion

        #region Methods

        public int Count()
        {
            SparqlQuery query = new SparqlQuery(SparqlQueryType.Select, SparqlSerializer.SerializeCount(_model, _query), null);

            ISparqlQueryResult result = _model.ExecuteQuery(query, _inferenceEnabled);

            IEnumerable<BindingSet> bindings = result.GetBindings();

            if (bindings != null)
            {
                foreach (BindingSet b in bindings)
                {
                    if( b.ContainsKey("count"))
                        return (int)b["count"];
                }
            }

            return -1;
        }

        public IEnumerable<Resource> GetResources(int offset = -1, int limit = -1)
        {
            return GetResources<Resource>(offset, limit);
        }

        IEnumerable<Uri> FetchUris(SparqlQuery query)
        {
            List<Uri> result = new List<Uri>();

            IEnumerable<BindingSet> bindings = _model.ExecuteQuery(query, _inferenceEnabled).GetBindings();
            Uri uri = null;

            foreach (BindingSet binding in bindings)
            {
                Uri u = binding["s0"] as Uri;

                if (u != uri)
                {
                    result.Add(binding["s0"] as Uri);
                }

                uri = u;
            }

            return result;
        }

        public IEnumerable<T> GetResources<T>(int offset = -1, int limit = -1) where T : Resource
        {
            _query.Offset = offset;
            _query.Limit = limit;

            if (_inferenceEnabled)
            {
                SparqlQuery uriQuery = new SparqlQuery(SparqlSerializer.Serialize(_model, _query, true));
                uriQuery.SetModel(_model);

                StringBuilder uris = new StringBuilder();
                var uriList = FetchUris(uriQuery).ToList();

                foreach (Uri u in uriList)
                {
                    uris.Append(SparqlSerializer.SerializeUri(u));
                }

                if (!uriList.Any())
                {
                    return new List<T>();
                }

                SparqlQuery query = new SparqlQuery(string.Format("DESCRIBE {0} {1}", uris, SparqlSerializer.GenerateDatasetClause(_model)));
                ISparqlQueryResult res = _model.ExecuteQuery(query);

                if (IsSorted)
                {
                    return res.GetResources<T>().OrderBy(o => { return (o != null) ? uriList.IndexOf(o.Uri) : -1; });
                }
                else
                {
                    return res.GetResources<T>();
                }
            }
            else
            {
                SparqlQuery query = new SparqlQuery(SparqlSerializer.Serialize(_model, _query));
                query.SetModel(_model);

                return _model.ExecuteQuery(query, _inferenceEnabled).GetResources<T>();
            }
        }

        public override string ToString()
        {
            SparqlQuery query = new SparqlQuery(SparqlSerializer.Serialize(_model, _query));
            query.SetModel(_model);
            return query.Query;
        }

        #endregion
    }
}
