using Remotion.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Query
{
    class QueryModelVisitor : QueryModelVisitorBase
    {
        #region Members

        #endregion

        #region Constructors
       
        #endregion

        #region Methods

        protected override void VisitBodyClauses(Remotion.Linq.Collections.ObservableCollection<Remotion.Linq.Clauses.IBodyClause> bodyClauses, QueryModel queryModel)
        {
            base.VisitBodyClauses(bodyClauses, queryModel);
        }

        public override void VisitWhereClause(Remotion.Linq.Clauses.WhereClause whereClause, QueryModel queryModel, int index)
        {
            ExpressionVisitor v = new ExpressionVisitor();
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
