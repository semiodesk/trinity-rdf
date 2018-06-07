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

using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;
#if NET35
using Semiodesk.Trinity.Utility;
#endif

namespace Semiodesk.Trinity.Query
{
    class ExpressionTreeVisitor : ThrowingExpressionVisitor
    {
        #region Members

        protected ISparqlQueryModelVisitor QueryModelVisitor;

        protected ISparqlQueryGeneratorTree QueryGeneratorTree;

        protected List<Expression> Trace = new List<Expression>();

        #endregion

        #region Constructors

        public ExpressionTreeVisitor(ISparqlQueryModelVisitor queryModelVisitor, ISparqlQueryGeneratorTree queryGeneratorTree)
        {
            QueryModelVisitor = queryModelVisitor;
            QueryGeneratorTree = queryGeneratorTree;
        }

        #endregion

        #region Methods

        private void HandleRegexMethodCallExpression(Expression expression, string regex, bool ignoreCase)
        {
            ISparqlQueryGenerator currentGenerator = QueryGeneratorTree.CurrentGenerator;

            // For method calls like !x.Name.Contains(x) we need to implement the operator on the FILTER.
            bool unaryNot = Trace.Any(e => e.NodeType == ExpressionType.Not);

            if (expression is MemberExpression)
            {
                MemberExpression member = expression as MemberExpression;

                if (unaryNot)
                {
                    currentGenerator.FilterNotRegex(member, regex, ignoreCase);
                }
                else
                {
                    currentGenerator.FilterRegex(member, regex, ignoreCase);
                }
            }
            else if (expression is SubQueryExpression)
            {
                if (unaryNot)
                {
                    currentGenerator.FilterNotRegex(currentGenerator.ObjectVariable, regex, ignoreCase);
                }
                else
                {
                    currentGenerator.FilterRegex(currentGenerator.ObjectVariable, regex, ignoreCase);
                }
            }
        }

        public override Expression Visit(Expression expression)
        {
            // Some expressions such as a call to the .Equals() method can be more easily implemented as a binary expression.
            // Therefore, we can apply transformations to the expressions before actually handling them.
            Expression e = ExpressionTransformer.Transform(expression);

            Trace.Insert(0, e);

            base.Visit(e);

            Trace.RemoveAt(0);

            return e;
        }

        private void VisitBinaryAndAlsoExpression(BinaryExpression expression)
        {
            Visit(expression.Left);
            Visit(expression.Right);
        }

        private void VisitBinaryOrElseExpression(BinaryExpression expression)
        {
            ISparqlQueryGenerator currentGenerator = QueryGeneratorTree.CurrentGenerator;

            // Get the currently active pattern builder so that we can reset it after we're done.
            // This will build nested UNIONS ({{x} UNION {y}} UNION {z}) for multiple alternative 
            // OR expressions. While this is not elegant, it is logically correct and can be optimized 
            // by the storage backend.
            IGraphPatternBuilder patternBuilder = currentGenerator.PatternBuilder;

            currentGenerator.Union(
                (left) =>
                {
                    currentGenerator.PatternBuilder = left;
                    Visit(expression.Left);
                },
                (right) =>
                {
                    currentGenerator.PatternBuilder = right;
                    Visit(expression.Right);
                }
            );

            // Reset the pattern builder that was used before implementing the unions.
            currentGenerator.PatternBuilder = patternBuilder;
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
            else if (expression.HasExpressionOfType<QuerySourceReferenceExpression>())
            {
                QuerySourceReferenceExpression querySource = expression.TryGetExpressionOfType<QuerySourceReferenceExpression>();

                VisitBinaryQuerySourceReferenceExpression(expression.NodeType, querySource, constant);
            }
        }

        private void VisitBinaryQuerySourceReferenceExpression(ExpressionType type, QuerySourceReferenceExpression sourceExpression, ConstantExpression constant)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            SparqlVariable s = g.VariableGenerator.TryGetSubjectVariable(sourceExpression) ?? g.VariableGenerator.GlobalSubject;

            switch (type)
            {
                case ExpressionType.Equal:
                    g.WhereEqual(s, constant);
                    break;
                case ExpressionType.NotEqual:
                    g.WhereNotEqual(s, constant);
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        private void VisitBinaryMemberExpression(ExpressionType type, MemberExpression member, ConstantExpression constant)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            switch (type)
            {
                case ExpressionType.Equal:
                    g.WhereEqual(member, constant);
                    break;
                case ExpressionType.NotEqual:
                    g.WhereNotEqual(member, constant);
                    break;
                case ExpressionType.GreaterThan:
                    g.WhereGreaterThan(member, constant);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    g.WhereGreaterThanOrEqual(member, constant);
                    break;
                case ExpressionType.LessThan:
                    g.WhereLessThan(member, constant);
                    break;
                case ExpressionType.LessThanOrEqual:
                    g.WhereLessThanOrEqual(member, constant);
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        private void VisitBinarySubQueryExpression(ExpressionType type, SubQueryExpression subQuery, ConstantExpression constant)
        {
            if (!QueryGeneratorTree.HasQueryGenerator(subQuery))
            {
                VisitSubQuery(subQuery);
            }

            ISparqlQueryGenerator g = QueryGeneratorTree.GetQueryGenerator(subQuery);

            // Note: We write the filter into the sub query generator which is writing into it's
            // enclosing graph group pattern rather than the query itself. This is required for
            // supporting OpenLink Virtuoso (see SparqlQueryGenerator.Child()).
            switch (type)
            {
                case ExpressionType.Equal:
                    g.WhereEqual(g.ObjectVariable, constant);
                    break;
                case ExpressionType.NotEqual:
                    g.WhereNotEqual(g.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThan:
                    g.WhereGreaterThan(g.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    g.WhereGreaterThanOrEqual(g.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThan:
                    g.WhereLessThan(g.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThanOrEqual:
                    g.WhereLessThanOrEqual(g.ObjectVariable, constant);
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            switch (binary.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    {
                        if (binary.HasExpressionOfType<ConstantExpression>())
                        {
                            VisitBinaryConstantExpression(binary);
                        }
                        break;
                    }
                case ExpressionType.AndAlso:
                    {
                        VisitBinaryAndAlsoExpression(binary);
                        break;
                    }
                case ExpressionType.OrElse:
                    {
                        VisitBinaryOrElseExpression(binary);
                        break;
                    }
            }

            return binary;
        }

        protected override Expression VisitConditional(ConditionalExpression conditional)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitConstant(ConstantExpression constant)
        {
            return constant;
        }

        protected override Expression VisitInvocation(InvocationExpression invocation)
        {
            throw new NotSupportedException();
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment memberAssigment)
        {
            return base.VisitMemberAssignment(memberAssigment);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding memberBinding)
        {
            return base.VisitMemberBinding(memberBinding);
        }

        protected override Expression VisitListInit(ListInitExpression listInit)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitMember(MemberExpression member)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            SparqlVariable o = g.VariableGenerator.TryGetObjectVariable(member);

            if (o == null)
            {
                // We have not visited the member before. It might be accessed in an order by clause..
                if (g.QueryModel.HasOrdering(member))
                {
                    o = g.VariableGenerator.CreateObjectVariable(member);

                    g.Where(member, o);
                }
                else if (member.Type == typeof(bool))
                {
                    ConstantExpression constantExpression = Expression.Constant(true);

                    g.WhereEqual(member, constantExpression);
                }
            }
            else
            {
                // We have visited the member before, either in the FromExpression or a SubQueryExpression.
                g.Where(member, o);
            }

            return member;
        }

        protected override Expression VisitMemberInit(MemberInitExpression memberInit)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            string method = methodCall.Method.Name;

            switch (method)
            {
                case "Contains":
                    {
                        Expression o = methodCall.Object;
                        string pattern = methodCall.GetArgumentValue<string>(0);

                        HandleRegexMethodCallExpression(o, pattern, false);

                        return methodCall;
                    }
                case "StartsWith":
                    {
                        object[] args = new object[]
                        {
                            true,
                            StringComparison.CurrentCultureIgnoreCase,
                            StringComparison.InvariantCultureIgnoreCase
                        };

                        Expression o = methodCall.Object;
                        string pattern = "^" + methodCall.GetArgumentValue<string>(0);
                        bool ignoreCase = methodCall.HasArgumentValueFromAlternatives(1, args);

                        HandleRegexMethodCallExpression(o, pattern, ignoreCase);

                        return methodCall;
                    }
                case "EndsWith":
                    {
                        object[] args = new object[]
                        {
                            true,
                            StringComparison.CurrentCultureIgnoreCase,
                            StringComparison.InvariantCultureIgnoreCase
                        };

                        Expression o = methodCall.Object;
                        string pattern = methodCall.GetArgumentValue<string>(0) + "$";
                        bool ignoreCase = methodCall.HasArgumentValueFromAlternatives(1, args);

                        HandleRegexMethodCallExpression(o, pattern, ignoreCase);

                        return methodCall;
                    }
                case "IsMatch":
                    {
                        if (methodCall.Method.DeclaringType == typeof(Regex))
                        {
                            Expression o = methodCall.Arguments[0];
                            string pattern = methodCall.GetArgumentValue<string>(1) + "$";
                            RegexOptions options = methodCall.GetArgumentValue(2, RegexOptions.None);

                            HandleRegexMethodCallExpression(o, pattern, options.HasFlag(RegexOptions.IgnoreCase));

                            return methodCall;
                        }

                        break;
                    }
            }

            throw new NotSupportedException();
        }

        protected override Expression VisitNew(NewExpression @new)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitNewArray(NewArrayExpression newArray)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression querySource)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitSubQuery(SubQueryExpression subQuery)
        {
            if (subQuery.QueryModel.ResultOperators.OfType<FirstResultOperator>().Any()
                || subQuery.QueryModel.ResultOperators.OfType<LastResultOperator>().Any())
            {
                // We currently do not support First, FirstOrDefault, Last and LastOrDefault on sub queries.
                // There are no fundamental issues, but needs extensive testing.
                string msg = "First, FirstOrDefault, Last and LastOrDfault are not supported in sub-queries.";
                throw new NotSupportedException(msg);
            }

            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;
            ISparqlQueryGenerator sg = QueryGeneratorTree.CreateSubQueryGenerator(g, subQuery);

            // Set the sub query generator as the current query generator to implement the sub query.
            QueryGeneratorTree.CurrentGenerator = sg;

            // Descend the query tree and implement the sub query.
            subQuery.QueryModel.Accept(QueryModelVisitor);

            // Register the sub query expression with the variable generator (used for ORDER BYs in the outer query).
            if (sg.ObjectVariable != null && sg.ObjectVariable.IsResultVariable)
            {
                // Note: We make a copy of the variable here so that aggregate variables are selected by their names only.
                SparqlVariable o = new SparqlVariable(sg.ObjectVariable.Name);

                g.VariableGenerator.SetObjectVariable(subQuery, o);
            }

            // Reset the query generator and continue with implementing the outer query.
            QueryGeneratorTree.CurrentGenerator = g;

            // Note: This will set pattern builder of the sub generator to the enclosing graph group builder.
            g.Child(sg);

            return subQuery;
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression typeBinary)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            g.WhereResourceOfType(g.SubjectVariable, typeBinary.TypeOperand);

            return typeBinary;
        }

        protected override Expression VisitUnary(UnaryExpression unary)
        {
            if (unary.NodeType == ExpressionType.Not)
            {
                if (unary.Operand is MemberExpression)
                {
                    // This has already been handled with ExpressionTransformer.Transform().
                    return unary;
                }
                else if (unary.Operand is MethodCallExpression)
                {
                    // Let VisitMethodCall decide if the operand is supported.
                    MethodCallExpression methodCall = unary.Operand as MethodCallExpression;

                    return Visit(methodCall);
                }
                else
                {
                    throw new NotSupportedException(unary.Operand.ToString());
                }
            }
            else
            {
                throw new NotSupportedException(unary.NodeType.ToString());
            }
        }

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            return null;
        }

        public Expression VisitOrdering(Ordering ordering, int index)
        {
            Visit(ordering.Expression);

            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            // Either the member or aggregate variable has already been created previously by a SubQuery or a SelectClause..
            SparqlVariable o = g.VariableGenerator.TryGetObjectVariable(ordering.Expression);

            if (o != null)
            {
                // In case the query has a LastResultOperator, we invert the direction of the first
                // ordering to retrieve the last element of the result set.
                // See: SelectQueryGenerator.SetObjectOperator()
                if (g.QueryModel.HasResultOperator<LastResultOperator>() && index == 0)
                {
                    if (ordering.OrderingDirection == OrderingDirection.Asc) g.OrderByDescending(o); else g.OrderBy(o);
                }
                else
                {
                    if (ordering.OrderingDirection == OrderingDirection.Asc) g.OrderBy(o); else g.OrderByDescending(o);
                }

                return ordering.Expression;
            }
            else
            {
                throw new ArgumentException(ordering.Expression.ToString());
            }
        }

        public Expression VisitFromExpression(Expression expression, string itemName, Type itemType)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            g.VariableGenerator.AddVariableMapping(expression, itemName);

            if (expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;

                if (memberExpression.Expression is SubQueryExpression)
                {
                    // First, implement the subquery..
                    SubQueryExpression subQueryExpression = memberExpression.Expression as SubQueryExpression;

                    Visit(subQueryExpression);
                }

                // ..then implement the member expression.
                Visit(memberExpression);
            }

            return expression;
        }

        #endregion
    }
}
