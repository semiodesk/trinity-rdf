using System.Collections.Generic;

namespace Semiodesk.Trinity.Query
{
    internal class QueryGeneratorTree
    {
        #region Members

        private QueryGenerator _rootQuery;

        private Dictionary<QueryGenerator, IList<QueryGenerator>> _subQueries = new Dictionary<QueryGenerator, IList<QueryGenerator>>();

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

        public void Traverse(QueryGeneratorTraversalDelegate callback)
        {
            Traverse(_rootQuery, callback);
        }

        private void Traverse(QueryGenerator generator, QueryGeneratorTraversalDelegate callback)
        {
            if(_subQueries.ContainsKey(generator))
            {
                foreach(QueryGenerator subQuery in _subQueries[generator])
                {
                    Traverse(subQuery, callback);
                }
            }

            callback(generator);
        }

        public IEnumerable<QueryGenerator> TryGetSubQueries(QueryGenerator query)
        {
            return _subQueries.ContainsKey(query) ? _subQueries[query] : null;
        }

        #endregion
    }

    internal delegate void QueryGeneratorTraversalDelegate(QueryGenerator queryGenerator);
}
