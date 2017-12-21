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
using Remotion.Linq.Parsing;
using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;

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

        private void VisitBinaryAndAlsoExpression(BinaryExpression expression)
        {
            VisitExpression(expression.Left);
            VisitExpression(expression.Right);
        }

        private void VisitBinaryOrElseExpression(BinaryExpression expression)
        {
            ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

            // Get the currently active pattern builder so that we can reset it after we're done.
            // This will build nested UNIONS ({{x} UNION {y}} UNION {z}) for multiple alternative 
            // OR expressions. While this is not elegant, it is logically correct and can be optimized 
            // by the storage backend.
            IGraphPatternBuilder patternBuilder = currentGenerator.GetPatternBuilder();

            currentGenerator.Union(
                (left) =>
                {
                    currentGenerator.SetPatternBuilder(left);
                    VisitExpression(expression.Left);
                },
                (right) =>
                {
                    currentGenerator.SetPatternBuilder(right);
                    VisitExpression(expression.Right);
                }
            );

            // Reset the pattern builder that was used before implementing the unions.
            currentGenerator.SetPatternBuilder(patternBuilder);
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
            ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

            switch (type)
            {
                case ExpressionType.Equal:
                    currentGenerator.WhereEqual(member, constant);
                    break;
                case ExpressionType.NotEqual:
                    currentGenerator.WhereNotEqual(member, constant);
                    break;
                case ExpressionType.GreaterThan:
                    currentGenerator.WhereGreaterThan(member, constant);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    currentGenerator.WhereGreaterThanOrEqual(member, constant);
                    break;
                case ExpressionType.LessThan:
                    currentGenerator.WhereLessThan(member, constant);
                    break;
                case ExpressionType.LessThanOrEqual:
                    currentGenerator.WhereLessThanOrEqual(member, constant);
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        private void VisitBinarySubQueryExpression(ExpressionType type, SubQueryExpression subQuery, ConstantExpression constant)
        {
            if (!_queryGeneratorTree.HasQueryGenerator(subQuery))
            {
                VisitSubQueryExpression(subQuery);
            }

            ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();
            ISparqlQueryGenerator subGenerator = _queryGeneratorTree.GetQueryGenerator(subQuery);

            // Note: We write the filter into the sub query generator which is writing into it's
            // enclosing graph group pattern rather than the query itself. This is required for
            // supporting OpenLink Virtuoso (see SparqlQueryGenerator.Child()).
            switch (type)
            {
                case ExpressionType.Equal:
                    subGenerator.WhereEqual(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.NotEqual:
                    subGenerator.WhereNotEqual(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThan:
                    subGenerator.WhereGreaterThan(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    subGenerator.WhereGreaterThanOrEqual(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThan:
                    subGenerator.WhereLessThan(subGenerator.ObjectVariable, constant);
                    break;
                case ExpressionType.LessThanOrEqual:
                    subGenerator.WhereLessThanOrEqual(subGenerator.ObjectVariable, constant);
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

        public Expression VisitFromExpression(Expression expression, string itemName, Type itemType)
        {
            ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

            if (expression is SubQueryExpression)
            {
                SubQueryExpression subQuery = expression as SubQueryExpression;

                ISparqlQueryGenerator subGenerator = _queryGeneratorTree.GetQueryGenerator(subQuery);

                if (subGenerator.ObjectVariable != null)
                {
                    SparqlVariable o = _variableGenerator.GetObjectVariable();

                    // The object of the sub query is the subject of the enclosing query.
                    currentGenerator.SetSubjectVariable(subGenerator.ObjectVariable);
                    currentGenerator.SetObjectVariable(o);

                    // Make the subject variable of sub queries available for outer queries.
                    currentGenerator.SelectVariable(subGenerator.SubjectVariable);
                    currentGenerator.SelectVariable(o);

                    _variableGenerator.RegisterExpression(expression, o);
                }
            }
            else
            {
                QuerySourceReferenceExpression querySource = expression.TryGetQuerySourceReference();

                if (querySource != null)
                {
                    SparqlVariable s = _variableGenerator.GetVariable(querySource);
                    SparqlVariable o = _variableGenerator.GetObjectVariable();

                    // Set the variable name of the query source reference as subject of the current query.
                    currentGenerator.SetSubjectVariable(s);
                    currentGenerator.SetObjectVariable(o);

                    // Make the subject variable of sub queries available for outer queries.
                    currentGenerator.SelectVariable(s);
                    currentGenerator.SelectVariable(o);

                    if (expression != querySource)
                    {
                        _variableGenerator.RegisterExpression(expression, o);
                    }
                }
            }

            if (expression is MemberExpression)
            {

                // The from clause is parsed first when handling a query. This allows us to detect if the
                // query source is a subquery and proceed with implementing it _before_ hanlding its results.
                MemberExpression memberExpression = expression as MemberExpression;

                // Handle numeric result operators.
                if (currentGenerator.HasNumericResultOperator())
                {
                    SparqlVariable s = currentGenerator.SubjectVariable;

                    if (s != null && s.IsGlobal())
                    {
                        currentGenerator.WhereResource(s);

                        Type t = memberExpression.Member.DeclaringType;

                        if (typeof(Resource).IsAssignableFrom(t))
                        {
                            currentGenerator.WhereResourceOfType(s, t);
                        }
                    }

                    // If the query model has a numeric result operator, we make all the following
                    // expressions optional in order to also allow to count zero occurences.
                    var optionalBuilder = new GraphPatternBuilder(GraphPatternType.Optional);

                    currentGenerator.Child(optionalBuilder);

                    currentGenerator.SetPatternBuilder(optionalBuilder);
                }

                if (memberExpression.Expression is SubQueryExpression)
                {
                    // First, implement the subquery..
                    SubQueryExpression subQueryExpression = memberExpression.Expression as SubQueryExpression;

                    VisitExpression(subQueryExpression);
                }

                // Handle the results of the subquery.
                VisitExpression(expression);
            }

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
            Expression e = expression.Expression;

            if (e is SubQueryExpression || e is QuerySourceReferenceExpression)
            {
                ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

                currentGenerator.Where(expression, currentGenerator.ObjectVariable);
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

            switch(method)
            {
                case "Equals":
                    {
                        ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

                        ConstantExpression arg0 = expression.Arguments[0] as ConstantExpression;

                        if (expression.Object is MemberExpression)
                        {
                            MemberExpression member = expression.Object as MemberExpression;

                            currentGenerator.WhereEqual(member, arg0);
                        }
                        else if (expression.Object is SubQueryExpression)
                        {
                            currentGenerator.WhereEqual(currentGenerator.ObjectVariable, arg0);
                        }

                        break;
                    }
                case "Contains":
                    {
                        Expression o = expression.Object;
                        string pattern = expression.GetArgumentValue<string>(0);

                        HandleRegexMethodCallExpression(o, pattern);

                        break;
                    }
                case "StartsWith":
                    {
                        Expression o = expression.Object;
                        string pattern = "^" + expression.GetArgumentValue<string>(0);
                        bool ignoreCase = expression.GetArgumentValue(1, false);

                        HandleRegexMethodCallExpression(o, pattern, ignoreCase);

                        break;
                    }
                case "EndsWith":
                    {
                        Expression o = expression.Object;
                        string pattern = expression.GetArgumentValue<string>(0) + "$";
                        bool ignoreCase = expression.GetArgumentValue(1, false);

                        HandleRegexMethodCallExpression(o, pattern, ignoreCase);

                        break;
                    }
                case "IsMatch":
                    {
                        if(expression.Method.DeclaringType == typeof(Regex))
                        {
                            Expression o = expression.Arguments[0];
                            string pattern = expression.GetArgumentValue<string>(1) + "$";
                            RegexOptions options = expression.GetArgumentValue(2, RegexOptions.None);

                            HandleRegexMethodCallExpression(o, pattern, options.HasFlag(RegexOptions.IgnoreCase));
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        break;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }

            return expression;
        }

        private void HandleRegexMethodCallExpression(Expression expression, string regex, bool ignoreCase = false)
        {
            ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

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
            ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();
            ISparqlQueryGenerator subGenerator = _queryGeneratorTree.CreateSubQueryGenerator(expression);

            // Sub queries always select the subject from the select clause of the root query.
            subGenerator.SelectVariable(currentGenerator.SubjectVariable);

            // Set the sub query generator as the current query generator to implement the sub query.
            _queryGeneratorTree.SetCurrentQueryGenerator(subGenerator);

            // Descend the query tree and implement the sub query.
            expression.QueryModel.Accept(_queryModelVisitor);

            // Register the sub query expression with the variable generator.
            if(!_variableGenerator.HasVariable(expression))
            {
                _variableGenerator.RegisterExpression(expression, subGenerator.ObjectVariable);
            }

            // Reset the query generator and continue with implementing the outer query.
            _queryGeneratorTree.SetCurrentQueryGenerator(currentGenerator);

            // Note: This will set pattern builder of the sub generator to the enclosing graph group builder.
            currentGenerator.Child(subGenerator);

            return expression;
        }

        protected override Expression VisitTypeBinaryExpression(TypeBinaryExpression expression)
        {
            ISparqlQueryGenerator generator = _queryGeneratorTree.GetCurrentQueryGenerator();

            generator.WhereResourceOfType(generator.SubjectVariable, expression.TypeOperand);

            return expression;
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            throw new NotImplementedException();
        }

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            return null;
        }

        public Expression VisitOrdering(Ordering ordering)
        {
            Expression expression = ordering.Expression;

            VisitExpression(expression);

            if (_variableGenerator.HasVariable(expression))
            {
                SparqlVariable v = _variableGenerator.GetVariable(expression);

                ISparqlQueryGenerator currentGenerator = _queryGeneratorTree.GetCurrentQueryGenerator();

                if(ordering.OrderingDirection == OrderingDirection.Asc)
                {
                    currentGenerator.OrderBy(v);
                }
                else
                {
                    currentGenerator.OrderByDescending(v);
                }
            }

            return ordering.Expression;
        }

        #endregion
    }
}
