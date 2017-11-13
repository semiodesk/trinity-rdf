using System;
using System.Collections.Generic;
using VDS.RDF.Query.Builder;

namespace Semiodesk.Trinity.Query
{
    internal class QueryGeneratorTree
    {
        #region Members

        private QueryGenerator _rootQuery;

        private Dictionary<QueryGenerator, List<QueryGenerator>> _subQueries = new Dictionary<QueryGenerator, List<QueryGenerator>>();

        #endregion

        #region Constructors

        public QueryGeneratorTree(QueryGenerator rootQuery)
        {
            _rootQuery = rootQuery;
        }

        #endregion

        #region Methods

        public void AddQuery(QueryGenerator query, QueryGenerator subQuery)
        {
            if(_subQueries.ContainsKey(query))
            {
                _subQueries[query].Add(subQuery);
            }
            else
            {
                _subQueries[query] = new List<QueryGenerator>() { subQuery };
            }
        }

        public void Traverse(QueryGeneratorTraversalDelegate visitQuery)
        {
            Traverse(_rootQuery, visitQuery);
        }

        private void Traverse(QueryGenerator generator, QueryGeneratorTraversalDelegate visitQuery)
        {
            if(_subQueries.ContainsKey(generator))
            {
                foreach(QueryGenerator subQuery in _subQueries[generator])
                {
                    Traverse(subQuery, visitQuery);
                }
            }

            visitQuery(generator);
        }

        #endregion
    }

    internal delegate void QueryGeneratorTraversalDelegate(QueryGenerator queryGenerator);
}
