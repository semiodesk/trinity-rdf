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

using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using System;
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    sealed class ExpressionTreeVisitor : ThrowingExpressionTreeVisitor
    {
        #region Members

        private ISparqlQueryModelVisitor _queryModelVisitor;

        private ISparqlQueryGeneratorTree _queryGeneratorTree;

        private SparqlVariableGenerator _variableGenerator;

        #endregion

        #region Constructors

        public ExpressionTreeVisitor(ISparqlQueryModelVisitor queryModelVisitor, ISparqlQueryGeneratorTree queryGeneratorTree, SparqlVariableGenerator variableGenerator)
        {
            _queryModelVisitor = queryModelVisitor;
            _queryGeneratorTree = queryGeneratorTree;
            _variableGenerator = variableGenerator;
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
                        VisitBinaryConstantExpression(expression);
                    }
                    break;
                }
                case ExpressionType.AndAlso:
                {
                    VisitBinaryAndAlsoExpression(expression);
                    break;
                }
                case ExpressionType.OrElse:
                {
                    VisitBinaryOrElseExpression(expression);
                    break;
                }
            }

            return expression;
        }

        private void VisitBinaryAndAlsoExpression(BinaryExpression expression)
        {
            VisitExpression(expression.Left);
            VisitExpression(expression.Right);
        }

        private void VisitBinaryOrElseExpression(BinaryExpression expression)
        {
            ISparqlQueryGenerator generator = _queryGeneratorTree.GetCurrentQueryGenerator();

            // TODO
            throw new NotImplementedException();
        }

        private void VisitBinaryConstantExpression(BinaryExpression expression)
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

        private void VisitBinaryMemberExpression(ExpressionType type, MemberExpression member, ConstantExpression constant)
        {
            ISparqlQueryGenerator generator = _queryGeneratorTree.GetCurrentQueryGenerator();

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
            if(!_queryGeneratorTree.HasQueryGenerator(subQuery))
            {
                VisitSubQueryExpression(subQuery);
            }

            ISparqlQueryGenerator currentQueryGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();
            ISparqlQueryGenerator subQueryGenerator = _queryGeneratorTree.GetQueryGenerator(subQuery);

            switch (type)
            {
                case ExpressionType.Equal:
                    currentQueryGenerator.WhereEqual(subQueryGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.NotEqual:
                    currentQueryGenerator.WhereNotEqual(subQueryGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThan:
                    currentQueryGenerator.WhereGreaterThan(subQueryGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    currentQueryGenerator.WhereGreaterThanOrEqual(subQueryGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThan:
                    currentQueryGenerator.WhereLessThan(subQueryGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThanOrEqual:
                    currentQueryGenerator.WhereLessThanOrEqual(subQueryGenerator.ObjectVariable, constant);
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
            if (expression.Expression is SubQueryExpression)
            {
                SubQueryExpression subQuery = expression.Expression as SubQueryExpression;

                ISparqlQueryGenerator currentQueryGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();
                ISparqlQueryGenerator subQueryGenerator = _queryGeneratorTree.GetQueryGenerator(subQuery);

                if (subQueryGenerator.ObjectVariable != null)
                {
                    SparqlVariable o = _variableGenerator.GetObjectVariable();

                    // Make the subject variable of sub queries available for outer queries.
                    currentQueryGenerator.SelectVariable(subQueryGenerator.SubjectVariable);

                    // The object of the sub query is the subject of the enclosing query.
                    currentQueryGenerator.SetSubjectVariable(subQueryGenerator.ObjectVariable);
                    currentQueryGenerator.SetObjectVariable(o, true);

                    currentQueryGenerator.Where(expression, o);
                }
            }
            else if (expression.Expression is QuerySourceReferenceExpression)
            {
                SparqlVariable o = _variableGenerator.GetObjectVariable();

                ISparqlQueryGenerator currentQueryGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();
                ISparqlQueryGenerator rootQueryGenerator = _queryGeneratorTree.GetRootQueryGenerator();

                // Set the variable name of the query source reference as subject of the current query.
                currentQueryGenerator.SetSubjectVariable(rootQueryGenerator.SubjectVariable, true);
                currentQueryGenerator.SetObjectVariable(o, true);

                currentQueryGenerator.Where(expression, o);
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
                ISparqlQueryGenerator currentQueryGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

                ConstantExpression constant = expression.Arguments.First() as ConstantExpression;

                if (expression.Object is MemberExpression)
                {
                    MemberExpression member = expression.Object as MemberExpression;

                    currentQueryGenerator.WhereEqual(member, constant);
                }
                else if(expression.Object is SubQueryExpression)
                {
                    currentQueryGenerator.WhereEqual(currentQueryGenerator.ObjectVariable, constant);
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

        public Expression VisitSelectExpression(Expression expression, bool isRootQuery)
        {
            ISparqlQueryGenerator currentQueryGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

            // Handle the selection of bindings in the root or sub queries.
            currentQueryGenerator.Select(expression, isRootQuery);

            return expression;
        }

        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            ISparqlQueryGenerator currentQueryGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();
            ISparqlQueryGenerator subQueryGenerator = _queryGeneratorTree.CreateSubQueryGenerator(expression);

            // Descend the query tree and implement the sub query before the outer one.
            _queryGeneratorTree.SetCurrentQueryGenerator(subQueryGenerator);

            expression.QueryModel.Accept(_queryModelVisitor);

            // Reset the query generator and continue with implementing the current query.
            _queryGeneratorTree.SetCurrentQueryGenerator(currentQueryGenerator);

            // Sub queries always select the subject from the select clause of the root query.
            subQueryGenerator.SelectVariable(currentQueryGenerator.SubjectVariable);

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
