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
        Dictionary<Type, ResourceQuery> _queryType = new Dictionary<Type, ResourceQuery>();
        Dictionary<Type, Resource> _instances = new Dictionary<Type, Resource>();
        public List<IPropertyMapping> PropertyMappings;
        #endregion

        #region Constructors
       
        #endregion

        #region Methods

        public override void VisitMainFromClause(Remotion.Linq.Clauses.MainFromClause fromClause, QueryModel queryModel)
        {
            IList<Class> classes = MappingDiscovery.GetRdfClasses(fromClause.ItemType);
            var q = new ResourceQuery(classes);
            var x = (Resource)Activator.CreateInstance(fromClause.ItemType, new UriRef("semio:empty"));
            _instances.Add(fromClause.ItemType, x);
            _queryType.Add(fromClause.ItemType, q);
            
            
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

        //((Remotion.Linq.Clauses.Expressions.QuerySourceReferenceExpression)(exp))
        public ResourceQuery GetResourceQuery(Expression exp)
        {
            ResourceQuery q = null;
            _queryType.TryGetValue(exp.Type, out q);
            return q;
        }

        public IPropertyMapping GetMapping(Type type, string propertyName)
        {
            Resource res = null;
            _instances.TryGetValue(type, out res);
            return res.GetPropertyMapping(propertyName);
        }
        

        #endregion
    }
}
