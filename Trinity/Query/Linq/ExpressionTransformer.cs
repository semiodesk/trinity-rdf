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
// Copyright (c) Semiodesk GmbH 2015-2019

using Remotion.Linq;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    internal class ExpressionTransformer
    {
        public static Expression Transform(Expression expression)
        {
            if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCall = expression as MethodCallExpression;

                return TransformMethodCall(methodCall);
            }
            else if(expression is UnaryExpression)
            {
                UnaryExpression unary = expression as UnaryExpression;

                return TransformUnary(unary);
            }

            return expression;
        }

        private static Expression TransformMethodCall(MethodCallExpression methodCall)
        {
            if (methodCall.Method.Name == "Equals")
            {
                // We currently only support comparing constants using .Equals().
                if (methodCall.Arguments[0] is ConstantExpression)
                {
                    // We transform calls to .Equals into an equivalent binary expression.
                    ConstantExpression arg0 = methodCall.Arguments[0] as ConstantExpression;

                    return Expression.Equal(methodCall.Object, arg0);
                }
                else
                {
                    throw new NotSupportedException(methodCall.Arguments[0].GetType().ToString());
                }
            }

            return methodCall;
        }

        private static Expression TransformUnary(UnaryExpression unary)
        {
            if (unary.NodeType == ExpressionType.Not)
            {
                if (unary.Operand is MemberExpression)
                {
                    // Handle expression like !x.IsEnabled
                    MemberExpression member = unary.Operand as MemberExpression;

                    if (member.Type.IsValueType)
                    {
                        // TODO: The default value for a property may be overridden with the DefaultValue attribute.
                        object defaultValue = TypeHelper.GetDefaultValue(member.Type);

                        // Note that a BinaryEqualsExpression with the default value will generate SPARQL that includes unbound values.
                        return Expression.Equal(unary.Operand, Expression.Constant(defaultValue));
                    }
                    else
                    {
                        throw new NotSupportedException(member.Type.ToString());
                    }
                }
                else if(unary.Operand is MethodCallExpression)
                {
                    MethodCallExpression methodCall = unary.Operand as MethodCallExpression;

                    if (methodCall.Method.Name == "Equals")
                    {
                        // We currently only support comparing constants using .Equals().
                        if (methodCall.Arguments[0] is ConstantExpression)
                        {
                            // We transform calls to .Equals into an equivalent binary expression.
                            ConstantExpression arg0 = methodCall.Arguments[0] as ConstantExpression;

                            return Expression.NotEqual(methodCall.Object, arg0);
                        }
                        else
                        {
                            throw new NotSupportedException(methodCall.Arguments[0].GetType().ToString());
                        }
                    }
                }
                else if (unary.Operand is SubQueryExpression)
                {
                    SubQueryExpression subQuery = unary.Operand as SubQueryExpression;

                    QueryModel queryModel = subQuery.QueryModel.Clone();

                    // Transform sub-query calls to .Any() into .Count(x) = 0
                    if (queryModel.ResultOperators.Count == 1 && subQuery.QueryModel.HasResultOperator<AnyResultOperator>())
                    {
                        // Remove .Any() with boolean result type.
                        queryModel.ResultOperators.RemoveAt(0);

                        // Insert .Count() with an integer result type.
                        queryModel.ResultOperators.Insert(0, new CountResultOperator());
                        queryModel.ResultTypeOverride = typeof(Int32);

                        return Expression.Equal(new SubQueryExpression(queryModel), Expression.Constant(0));
                    }

                    // Generic sub-queries are not supported with unary expressions.
                    throw new NotSupportedException(unary.Operand.ToString());
                }
            }

            return unary;
        }
    }
}
