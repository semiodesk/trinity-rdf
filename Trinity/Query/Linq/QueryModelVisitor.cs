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
    class QueryModelVisitor : QueryModelVisitorBase
    {
        #region Members
        Dictionary<Expression, ResourceQuery> _queries = new Dictionary<Expression, ResourceQuery>();
        public QuerySourceMapping Mapping { get; private set; }
        public Dictionary<Expression, ResourceQuery> Queries { get { return _queries; } }
        public List<IPropertyMapping> PropertyMappings;
        #endregion

        #region Constructors
       
        #endregion

        #region Methods

        public override void VisitMainFromClause(Remotion.Linq.Clauses.MainFromClause fromClause, QueryModel queryModel)
        {
            Mapping = new QuerySourceMapping();
            IList<Class> classes = MappingDiscovery.GetRdfClasses(fromClause.ItemType);
            var q = new ResourceQuery(classes);
            _queries.Add(fromClause.FromExpression, q);
            
            //PropertyMappings = MappingDiscovery.ListMappings()

        }

        protected override void VisitBodyClauses(Remotion.Linq.Collections.ObservableCollection<Remotion.Linq.Clauses.IBodyClause> bodyClauses, QueryModel queryModel)
        {
            base.VisitBodyClauses(bodyClauses, queryModel);
        }

        public override void VisitWhereClause(Remotion.Linq.Clauses.WhereClause whereClause, QueryModel queryModel, int index)
        {
            ExpressionVisitor v = new ExpressionVisitor(this);
            v.VisitExpression(whereClause.Predicate);
            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitSelectClause(Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel)
        {
            base.VisitSelectClause(selectClause, queryModel);
        }

        

        #endregion
    }
}
