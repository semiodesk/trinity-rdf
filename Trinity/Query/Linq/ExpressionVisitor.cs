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

using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using System;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    class ExpressionVisitor : ThrowingExpressionTreeVisitor
    {
        #region Members

        protected QueryModelVisitor Visitor;

        protected MemberExpression CurrentMember;

        protected ConstantExpression CurrentConstant;

        #endregion

        #region Constructors

        public ExpressionVisitor(QueryModelVisitor visitor)
        {
            Visitor = visitor;
        }

        #endregion

        #region Methods

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            VisitExpression(expression.Left);
            VisitExpression(expression.Right);

            if (expression.Left.NodeType == ExpressionType.MemberAccess)
            {
                var property = new Property(CurrentMember.GetRdfPropertyAttribute().MappedUri);
                var value = CurrentConstant.Value;

                switch(expression.NodeType)
                {
                    case ExpressionType.Equal:
                        Visitor.Query.Where(property, value); break;
                    case ExpressionType.GreaterThan:
                        Visitor.Query.Where(property).GreaterThan(value); break;
                    case ExpressionType.GreaterThanOrEqual:
                        Visitor.Query.Where(property).GreaterOrEqual(value); break;
                    case ExpressionType.LessThan:
                        Visitor.Query.Where(property).LessThan(value); break;
                    case ExpressionType.LessThanOrEqual:
                        Visitor.Query.Where(property).LessOrEqual(value); break;
                    case ExpressionType.NotEqual:
                        Visitor.Query.Where(property).NotEqual(value); break;
                    default:
                        throw new NotSupportedException(expression.NodeType.ToString());
                }
            }

            return expression;
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
            CurrentMember = expression;

            return expression;
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            CurrentConstant = expression;

            return expression;
        }

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            return null;
        }

        #endregion
    }
}
