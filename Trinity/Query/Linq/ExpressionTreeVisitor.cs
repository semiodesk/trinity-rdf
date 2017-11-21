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
// Moritz Eberl <moritz@semiodesk.com>
// Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2017

using Remotion.Linq;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    // TODO: Each query generator should have its own ExpressionVisitor instance.
    sealed class ExpressionTreeVisitor : ThrowingExpressionTreeVisitor
    {
        #region Members

        private ISparqlQueryModelVisitor _queryModelVisitor;

        #endregion

        #region Constructors

        public ExpressionTreeVisitor(ISparqlQueryModelVisitor queryModelVisitor)
        {
            _queryModelVisitor = queryModelVisitor;
        }

        #endregion

        #region Methods

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            switch(expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                {
                    if (expression.HasExpressionOfType<ConstantExpression>())
                    {
                        ConstantExpression constant = expression.TryGetExpressionOfType<ConstantExpression>();

                        if (expression.HasExpressionOfType<MemberExpression>())
                        {
                            MemberExpression member = expression.TryGetExpressionOfType<MemberExpression>();

                            VisitBinaryMemberExpression(expression.NodeType, member, constant);
                        }
                        else if (expression.HasExpressionOfType<SubQueryExpression>())
                        {
                            SubQueryExpression subQuery = expression.TryGetExpressionOfType<SubQueryExpression>();

                            VisitBinarySubQueryExpression(expression.NodeType, subQuery, constant);
                        }
                    }

                    break;
                }
                case ExpressionType.AndAlso:
                {
                    VisitExpression(expression.Left);
                    VisitExpression(expression.Right);

                    break;
                }
            }

            return expression;
        }

        private void VisitBinaryMemberExpression(ExpressionType type, MemberExpression member, ConstantExpression constant)
        {
            ISparqlQueryGenerator generator = _queryModelVisitor.GetCurrentQueryGenerator();

            switch (type)
            {
                case ExpressionType.Equal:
                    generator.WhereEqual(member, constant);
                    break;
                case ExpressionType.NotEqual:
                    generator.WhereNotEqual(member, constant);
                    break;
                case ExpressionType.GreaterThan:
                    generator.WhereGreaterThan(member, constant);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    generator.WhereGreaterThanOrEqual(member, constant);
                    break;
                case ExpressionType.LessThan:
                    generator.WhereLessThan(member, constant);
                    break;
                case ExpressionType.LessThanOrEqual:
                    generator.WhereLessThanOrEqual(member, constant);
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        private void VisitBinarySubQueryExpression(ExpressionType type, SubQueryExpression subQuery, ConstantExpression constant)
        {
            if(!_queryModelVisitor.HasQueryGenerator(subQuery.QueryModel))
            {
                VisitSubQueryExpression(subQuery);
            }

            ISparqlQueryGenerator generator = _queryModelVisitor.GetCurrentQueryGenerator();
            ISparqlQueryGenerator subGenerator = _queryModelVisitor.GetQueryGenerator(subQuery.QueryModel);

            switch (type)
            {
                case ExpressionType.Equal:
                    generator.WhereEqual(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.NotEqual:
                    generator.WhereNotEqual(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThan:
                    generator.WhereGreaterThan(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    generator.WhereGreaterThanOrEqual(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThan:
                    generator.WhereLessThan(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThanOrEqual:
                    generator.WhereLessThanOrEqual(subGenerator.ObjectVariable, constant);
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        protected override Expression VisitConditionalExpression(ConditionalExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            return expression;
        }
        
        protected override Expression VisitInvocationExpression(InvocationExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitLambdaExpression(LambdaExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitListInitExpression(ListInitExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            ISparqlQueryGenerator generator = _queryModelVisitor.GetCurrentQueryGenerator();

            if (generator.SetSubjectVariableFromExpression(expression))
            {
                generator.Where(expression);
            }

            return expression;
        }

        protected override Expression VisitMemberInitExpression(MemberInitExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            string method = expression.Method.Name;

            if(method == "Equals")
            {
                ISparqlQueryGenerator generator = _queryModelVisitor.GetCurrentQueryGenerator();

                ConstantExpression constant = expression.Arguments.First() as ConstantExpression;

                if (expression.Object is MemberExpression)
                {
                    MemberExpression member = expression.Object as MemberExpression;

                    generator.WhereEqual(member, constant);
                }
                else if(expression.Object is SubQueryExpression)
                {
                    generator.WhereEqual(generator.ObjectVariable, constant);
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            return expression;
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
            throw new NotImplementedException();
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            _queryModelVisitor.VisitQueryModel(expression.QueryModel);

            return expression;
        }

        protected override Expression VisitTypeBinaryExpression(TypeBinaryExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            return null;
        }

        #endregion
    }
}
