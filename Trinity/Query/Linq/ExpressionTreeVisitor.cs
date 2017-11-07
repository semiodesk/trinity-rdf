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

using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    sealed class ExpressionTreeVisitor : ThrowingExpressionTreeVisitor
    {
        #region Members

        private QueryModelVisitor _queryModelVisitor;

        private IQueryBuilder _queryBuilder;

        private ExpressionTreeVisitorNodeContext _subject;

        private ExpressionTreeVisitorNodeContext _predicate;

        private ExpressionTreeVisitorNodeContext _object;

        #endregion

        #region Constructors

        public ExpressionTreeVisitor(QueryModelVisitor queryModelVisitor)
        {
            _queryModelVisitor = queryModelVisitor;
            _queryBuilder = queryModelVisitor.GetQueryBuilder();
            _subject = ExpressionTreeVisitorNodeContext.FromVariableName("s");
            _predicate = null;
            _object = null;
        }

        #endregion

        #region Methods

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            QuerySourceReferenceExpression leftSource = expression.Left.TryGetQuerySource();

            if (leftSource != null)
            {
                string id = leftSource.ReferencedQuerySource.ItemName;

                Console.WriteLine(expression.Left.GetType().ToString() + ": " + id);
            }

            QuerySourceReferenceExpression rightSource = expression.Right.TryGetQuerySource();

            if (rightSource != null)
            {
                string id = rightSource.ReferencedQuerySource.ItemName;

                Console.WriteLine(expression.Right.GetType().ToString() + ": " + id);
            }

            if (expression.Left is MemberExpression && expression.Right is ConstantExpression)
            {
                MemberExpression member = expression.Left as MemberExpression;
                ConstantExpression constant = expression.Right as ConstantExpression;

                var property = GetPropertyAttribute(member);
                var node = _object.Node;
                var term = new LiteralExpression(_object.Expression);

                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        _queryBuilder.Where(t => t.Subject("s").PredicateUri(property.MappedUri).Object(node));
                        break;
                    case ExpressionType.GreaterThan:
                        _queryBuilder.Where(t => t.Subject("s").PredicateUri(property.MappedUri).Object("l"));
                        _queryBuilder.Filter(e => e.Variable("l") > term);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        _queryBuilder.Where(t => t.Subject("s").PredicateUri(property.MappedUri).Object("l"));
                        _queryBuilder.Filter(e => e.Variable("l") >= term);
                        break;
                    case ExpressionType.LessThan:
                        _queryBuilder.Where(t => t.Subject("s").PredicateUri(property.MappedUri).Object("l"));
                        _queryBuilder.Filter(e => e.Variable("l") < term);
                        break;
                    case ExpressionType.LessThanOrEqual:
                        _queryBuilder.Where(t => t.Subject("s").PredicateUri(property.MappedUri).Object("l"));
                        _queryBuilder.Filter(e => e.Variable("l") <= term);
                        break;
                    case ExpressionType.NotEqual:
                        _queryBuilder.Where(t => t.Subject("s").PredicateUri(property.MappedUri).Object("l"));
                        _queryBuilder.Filter(e => e.Variable("l") != term);
                        break;
                    default:
                        throw new NotSupportedException(expression.NodeType.ToString());
                }
            }

            return expression;
        }

        protected override Expression VisitConditionalExpression(ConditionalExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            _object = ExpressionTreeVisitorNodeContext.FromConstantExpression(expression);

            Debug.WriteLine(expression.GetType().ToString() + ": " + _object.Expression.ToString());

            return expression;
        }

        protected override Expression VisitInvocationExpression(InvocationExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitLambdaExpression(LambdaExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitListInitExpression(ListInitExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            _predicate = ExpressionTreeVisitorNodeContext.FromMemberExpression(expression);

            Debug.WriteLine(expression.GetType().ToString() + ": " + _predicate.Node.ToString());

            return expression;
        }

        protected override Expression VisitMemberInitExpression(MemberInitExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitNewExpression(NewExpression expression)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitNewArrayExpression(NewArrayExpression expression)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitParameterExpression(ParameterExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            QuerySourceReferenceExpression source = expression.TryGetQuerySource();

            if(source != null)
            {
                string id = source.ReferencedQuerySource.ItemName;

                Debug.WriteLine(expression.GetType().ToString() + ": " + id);
            }
            else
            {
                Debug.WriteLine(expression.GetType().ToString());
            }

            expression.QueryModel.Accept(_queryModelVisitor);

            return expression;
        }

        protected override Expression VisitTypeBinaryExpression(TypeBinaryExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            return null;
        }

        private RdfPropertyAttribute GetPropertyAttribute(MemberExpression expression)
        {
            Type attributeType = typeof(RdfPropertyAttribute);

            return expression.Member.GetCustomAttributes(attributeType, true).First() as RdfPropertyAttribute;
        }

        #endregion
    }
}
