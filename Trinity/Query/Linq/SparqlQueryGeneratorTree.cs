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
// Moritz Eberl <moritz@semiodesk.com>
// Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2017

using System.Collections.Generic;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryGeneratorTree
    {
        #region Members

        private ISparqlQueryGenerator _rootQuery;

        private Dictionary<ISparqlQueryGenerator, IList<ISparqlQueryGenerator>> _subQueries = new Dictionary<ISparqlQueryGenerator, IList<ISparqlQueryGenerator>>();

        #endregion

        #region Constructors

        public SparqlQueryGeneratorTree(ISparqlQueryGenerator rootQuery)
        {
            _rootQuery = rootQuery;
        }

        #endregion

        #region Methods

        public void AddQuery(ISparqlQueryGenerator query, ISparqlQueryGenerator subQuery)
        {
            if(_subQueries.ContainsKey(query))
            {
                _subQueries[query].Add(subQuery);
            }
            else
            {
                _subQueries[query] = new List<ISparqlQueryGenerator>() { subQuery };
            }
        }

        public void Traverse(QueryGeneratorTraversalDelegate callback)
        {
            Traverse(_rootQuery, callback);
        }

        private void Traverse(ISparqlQueryGenerator generator, QueryGeneratorTraversalDelegate callback)
        {
            if(_subQueries.ContainsKey(generator))
            {
                foreach(ISparqlQueryGenerator subQuery in _subQueries[generator])
                {
                    Traverse(subQuery, callback);
                }
            }

            callback(generator);
        }

        public IEnumerable<ISparqlQueryGenerator> TryGetSubQueries(ISparqlQueryGenerator query)
        {
            return _subQueries.ContainsKey(query) ? _subQueries[query] : null;
        }

        #endregion
    }

    internal delegate void QueryGeneratorTraversalDelegate(ISparqlQueryGenerator queryGenerator);
}
