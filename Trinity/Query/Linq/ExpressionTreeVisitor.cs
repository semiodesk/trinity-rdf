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
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;

namespace Semiodesk.Trinity.Query
{
    class ExpressionTreeVisitor : ThrowingExpressionVisitor
    {
        #region Members

        protected ISparqlQueryModelVisitor QueryModelVisitor;

        protected ISparqlQueryGeneratorTree QueryGeneratorTree;

        #endregion

        #region Constructors

        public ExpressionTreeVisitor(ISparqlQueryModelVisitor queryModelVisitor, ISparqlQueryGeneratorTree queryGeneratorTree)
        {
            QueryModelVisitor = queryModelVisitor;
            QueryGeneratorTree = queryGeneratorTree;
        }

        #endregion

        #region Methods

        private void HandleRegexMethodCallExpression(Expression expression, string regex, bool ignoreCase = false)
        {
            ISparqlQueryGenerator currentGenerator = QueryGeneratorTree.CurrentGenerator;

            if (expression is MemberExpression)
            {
                MemberExpression member = expression as MemberExpression;

                currentGenerator.FilterRegex(member, regex, ignoreCase);
            }
            else if (expression is SubQueryExpression)
            {
                currentGenerator.FilterRegex(currentGenerator.ObjectVariable, regex, ignoreCase);
            }
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
            else if(expression.HasExpressionOfType<QuerySourceReferenceExpression>())
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

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            switch (expression.NodeType)
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

        protected override Expression VisitConditional(ConditionalExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            return expression;
        }

        protected override Expression VisitInvocation(InvocationExpression expression)
        {
            throw new NotSupportedException();
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment memberAssigment)
        {
            return base.VisitMemberAssignment(memberAssigment);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding expression)
        {
            return base.VisitMemberBinding(expression);
        }

        protected override Expression VisitListInit(ListInitExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            SparqlVariable o = g.VariableGenerator.TryGetObjectVariable(expression);

            if(o == null)
            {
                // We have not visited the member before. It might be accessed in an order by clause..
                if(g.QueryModel.HasOrdering(expression))
                {
                    o = g.VariableGenerator.CreateObjectVariable(expression);

                    g.Where(expression, o);
                }
                else if(expression.Type == typeof(bool))
                {
                    ConstantExpression constantExpression = Expression.Constant(true);

                    g.WhereEqual(expression, constantExpression);
                }
            }
            else
            {
                // We have visited the member before, either in the FromExpression or a SubQueryExpression.
                g.Where(expression, o);
            }

            return expression;
        }

        protected override Expression VisitMemberInit(MemberInitExpression expression)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            string method = expression.Method.Name;

            switch(method)
            {
                case "Equals":
                    {
                        ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

                        ConstantExpression arg0 = expression.Arguments[0] as ConstantExpression;

                        if (expression.Object is MemberExpression)
                        {
                            MemberExpression member = expression.Object as MemberExpression;

                            g.WhereEqual(member, arg0);
                        }
                        else
                        {
                            g.WhereEqual(g.ObjectVariable, arg0);
                        }

                        return expression;
                    }
                case "Contains":
                    {
                        Expression o = expression.Object;
                        string pattern = expression.GetArgumentValue<string>(0);

                        HandleRegexMethodCallExpression(o, pattern);

                        return expression;
                    }
                case "StartsWith":
                    {
                        object[] args = new object[]
                        {
                            true,
                            StringComparison.CurrentCultureIgnoreCase,
                            StringComparison.InvariantCultureIgnoreCase
                        };

                        Expression o = expression.Object;
                        string pattern = "^" + expression.GetArgumentValue<string>(0);
                        bool ignoreCase = expression.HasArgumentValueFromAlternatives(1, args);

                        HandleRegexMethodCallExpression(o, pattern, ignoreCase);

                        return expression;
                    }
                case "EndsWith":
                    {
                        object[] args = new object[]
                        {
                            true,
                            StringComparison.CurrentCultureIgnoreCase,
                            StringComparison.InvariantCultureIgnoreCase
                        };

                        Expression o = expression.Object;
                        string pattern = expression.GetArgumentValue<string>(0) + "$";
                        bool ignoreCase = expression.HasArgumentValueFromAlternatives(1, args);

                        HandleRegexMethodCallExpression(o, pattern, ignoreCase);

                        return expression;
                    }
                case "IsMatch":
                    {
                        if (expression.Method.DeclaringType == typeof(Regex))
                        {
                            Expression o = expression.Arguments[0];
                            string pattern = expression.GetArgumentValue<string>(1) + "$";
                            RegexOptions options = expression.GetArgumentValue(2, RegexOptions.None);

                            HandleRegexMethodCallExpression(o, pattern, options.HasFlag(RegexOptions.IgnoreCase));

                            return expression;
                        }

                        break;
                    }
            }

            throw new NotSupportedException();
        }

        protected override Expression VisitNew(NewExpression expression)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitNewArray(NewArrayExpression expression)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitParameter(ParameterExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitSubQuery(SubQueryExpression expression)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;
            ISparqlQueryGenerator sg = QueryGeneratorTree.CreateSubQueryGenerator(g, expression);

            // Set the sub query generator as the current query generator to implement the sub query.
            QueryGeneratorTree.CurrentGenerator = sg;

            // Descend the query tree and implement the sub query.
            expression.QueryModel.Accept(QueryModelVisitor);

            // Register the sub query expression with the variable generator (used for ORDER BYs in the outer query).
            if(sg.ObjectVariable != null && sg.ObjectVariable.IsResultVariable)
            {
                // Note: We make a copy of the variable here so that aggregate variables are selected by their names only.
                SparqlVariable o = new SparqlVariable(sg.ObjectVariable.Name);

                g.VariableGenerator.SetObjectVariable(expression, o);
            }

            // Reset the query generator and continue with implementing the outer query.
            QueryGeneratorTree.CurrentGenerator = g;

            // Note: This will set pattern builder of the sub generator to the enclosing graph group builder.
            g.Child(sg);

            return expression;
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression expression)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            g.WhereResourceOfType(g.SubjectVariable, expression.TypeOperand);

            return expression;
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            if(expression.NodeType == ExpressionType.Not)
            {
                if (expression.Operand is MemberExpression)
                {
                    MemberExpression memberExpression = expression.Operand as MemberExpression;

                    if(memberExpression.Type == typeof(bool))
                    {
                        ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

                        ConstantExpression constantExpression = Expression.Constant(false);

                        g.WhereEqual(memberExpression, constantExpression);

                        return expression;
                    }
                }
                else if(expression.Operand is MethodCallExpression)
                {
                    // Equals, Contains, StartsWith, EndsWith, IsMatch (see <c>VisitMethodCall</c>).
                    throw new NotImplementedException();
                }
                else if(expression.Operand is SubQueryExpression)
                {
                    // Any.
                    throw new NotImplementedException();
                }
            }

            throw new NotSupportedException(expression.Operand.ToString());
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

            if(o != null)
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
