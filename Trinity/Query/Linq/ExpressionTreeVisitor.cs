﻿// LICENSE:
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

using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    sealed class ExpressionTreeVisitor : ThrowingExpressionTreeVisitor
    {
        #region Members

        private QueryModelVisitor _queryModelVisitor;

        #endregion

        #region Constructors

        public ExpressionTreeVisitor(QueryModelVisitor queryModelVisitor)
        {
            _queryModelVisitor = queryModelVisitor;
        }

        #endregion

        #region Methods

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            QueryGenerator generator = _queryModelVisitor.CurrentQueryGenerator;
            ConstantExpression constant = expression.Right as ConstantExpression;

            if (expression.Left is MemberExpression && expression.Right is ConstantExpression)
            {
                MemberExpression member = expression.Left as MemberExpression;

                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        generator.WhereEqual(member, constant);
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
                    case ExpressionType.NotEqual:
                        generator.WhereNotEqual(member, constant);
                        break;
                    default:
                        throw new NotSupportedException(expression.NodeType.ToString());
                }
            }
            else if (expression.Left is SubQueryExpression && expression.Right is ConstantExpression)
            {
                SubQueryExpression subQuery = expression.Left as SubQueryExpression;

                QueryGenerator subGenerator = _queryModelVisitor.GetQueryGenerator(subQuery.QueryModel);

                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        generator.WhereEqual(subGenerator.ObjectVariable, constant);
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
                    case ExpressionType.NotEqual:
                        generator.WhereNotEqual(subGenerator.ObjectVariable, constant);
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
            Debug.WriteLine(expression.GetType().ToString() + ": " + expression.AsSparqlExpression());

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
            RdfPropertyAttribute attribute = expression.Member.TryGetCustomAttribute<RdfPropertyAttribute>();

            if(attribute != null)
            {
                INode node = new NodeFactory().CreateUriNode(attribute.MappedUri);

                Debug.WriteLine(expression.GetType().ToString() + ": " + node.ToString());

                QueryGenerator generator = _queryModelVisitor.CurrentQueryGenerator;

                if (generator.SetSubjectVariableFromExpression(expression))
                {
                    SparqlVariable s = generator.SubjectVariable;
                    SparqlVariable o = _queryModelVisitor.VariableBuilder.GenerateObjectVariable();

                    generator.SetObjectVariable(o);

                    if(_queryModelVisitor.CurrentQueryModel.HasNumericResultOperator())
                    {
                        SparqlVariable p = _queryModelVisitor.VariableBuilder.GeneratePredicateVariable();
                        SparqlVariable o2 = _queryModelVisitor.VariableBuilder.GenerateObjectVariable();

                        generator.Where(t => t.Subject(s.Name).Predicate(p.Name).Object(o2.Name));

                        // For numeric results, allow to select non existing triple patterns as zero values.
                        generator.Optional(g => g.Where(t => t.Subject(s.Name).PredicateUri(attribute.MappedUri).Object(o.Name)));
                    }
                    else
                    {
                        // Otherwise make the pattern non-optional.
                        generator.Where(t => t.Subject(s.Name).PredicateUri(attribute.MappedUri).Object(o.Name));
                    }
                }
            }

            return expression;
        }

        protected override Expression VisitMemberInitExpression(MemberInitExpression expression)
        {
            Debug.WriteLine(expression.GetType().ToString());

            return expression;
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            string method = expression.Method.Name;

            if(method == "Equals")
            {
                QueryGenerator generator = _queryModelVisitor.GetCurrentQueryGenerator();
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

        #endregion
    }
}
