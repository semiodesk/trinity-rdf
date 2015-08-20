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

            // Create an expression that returns the current item when invoked.
            Expression currentItemExpression = Expression.Property(Expression.Constant(this), "Current");

            // Now replace references like the "i" in "select i" that refers to the "i" in "from i in items"
            var mapping = new QuerySourceMapping();
            mapping.AddMapping(queryModel.MainFromClause, currentItemExpression);
            queryModel.TransformExpressions(e =>
                ReferenceReplacingExpressionTreeVisitor.ReplaceClauseReferences(e, mapping, true));

            // Create a lambda that takes our SampleDataSourceItem and passes it through the select clause
            // to produce a type of T.  (T may be SampleDataSourceItem, in which case this is an identity function.)
            var currentItemProperty = Expression.Parameter(typeof(Resource));
            var projection = Expression.Lambda<Func<Resource, T>>(queryModel.SelectClause.Selector, currentItemProperty);
            var projector = projection.Compile();

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
