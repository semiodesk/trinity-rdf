using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Semiodesk.Trinity.Query
{
    class ExpressionVisitor : ThrowingExpressionTreeVisitor
    {
        #region Members
        private QueryModelVisitor _visitor;
        #endregion

        #region Constructor
        public ExpressionVisitor(QueryModelVisitor visitor)
        {
            _visitor = visitor;
        }
        #endregion


        protected override System.Linq.Expressions.Expression VisitBinaryExpression(System.Linq.Expressions.BinaryExpression expression)
        {
            VisitExpression(expression.Left);
            VisitExpression(expression.Right);
            return null;
        }

        protected override Expression VisitConditionalExpression(ConditionalExpression expression)
        {
            return base.VisitConditionalExpression(expression);
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            return base.VisitMethodCallExpression(expression);
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            return base.VisitUnaryExpression(expression);
        }

        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            return base.VisitSubQueryExpression(expression);
        }

        protected override Expression VisitTypeBinaryExpression(TypeBinaryExpression expression)
        {
            return base.VisitTypeBinaryExpression(expression);
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            //_visitor.GetResourceQuery(expression.)
            ResourceQuery q = _visitor.GetResourceQuery(expression.Expression);

            IPropertyMapping mapping = _visitor.GetMapping(expression.Expression.Type, expression.Member.Name);
            return null;
            //return base.VisitMemberExpression(expression);
        }

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            return null;
        }
    }
}
