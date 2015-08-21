using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Semiodesk.Trinity.Query
{
    internal class ResourceQueryExecutor : IQueryExecutor
    {
        // Set up a proeprty that will hold the current item being enumerated.
        public Resource Current { get; private set; }

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            QueryModelVisitor visitor = new QueryModelVisitor();
            visitor.VisitQueryModel(queryModel);

            return new T[] { };
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var sequence = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            // We'll get to this one later...
            throw new NotImplementedException();
        }
    }
}
