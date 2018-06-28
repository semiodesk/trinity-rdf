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
// Copyright (c) Semiodesk GmbH 2017

using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    internal static class QueryModelExtensions
    {
        public static bool HasTypeConstraintOnExpression(this QueryModel queryModel, Expression expression)
        {
            foreach (WhereClause clause in queryModel.BodyClauses.OfType<WhereClause>().Where(c => c.Predicate is BinaryExpression))
            {
                BinaryExpression binary = clause.Predicate as BinaryExpression;

                ConstantExpression constant = binary.TryGetExpressionOfType<ConstantExpression>();

                if (constant != null && constant.Type == typeof(Type))
                {
                    MethodCallExpression methodCall = binary.TryGetExpressionOfType<MethodCallExpression>();

                    return methodCall != null && methodCall.Object.ToString() == expression.ToString();
                }
            }

            return false;
        }

        public static bool HasOrdering(this QueryModel queryModel)
        {
            return queryModel.BodyClauses.OfType<OrderByClause>().Any();
        }

        public static bool HasOrdering(this QueryModel queryModel, Expression expression)
        {
            return queryModel.BodyClauses.OfType<OrderByClause>().Any(c => c.Orderings.Any(o => o.Expression == expression));
        }

        public static bool HasResultOperator<T>(this QueryModel queryModel)
        {
            return queryModel.ResultOperators.Any(op => op is T);
        }

        public static bool HasNumericResultOperator(this QueryModel queryModel)
        {
            return queryModel.ResultOperators.Any(op => IsNumericResultOperator(op));
        }

        private static bool IsNumericResultOperator(ResultOperatorBase op)
        {
            if (op is SumResultOperator
                || op is CountResultOperator
                || op is LongCountResultOperator
                || op is AverageResultOperator
                || op is MinResultOperator
                || op is MaxResultOperator)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool HasSelect(this QueryModel queryModel, Expression expression)
        {
            return queryModel.BodyClauses.OfType<SelectClause>().Any(c => c.Selector == expression);
        }
    }
}
