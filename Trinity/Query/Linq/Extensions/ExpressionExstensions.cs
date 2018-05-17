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
// Copyright (c) Semiodesk GmbH 2017

using Remotion.Linq.Clauses.Expressions;
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    internal static class ExpressionExstensions
    {
        public static QuerySourceReferenceExpression TryGetQuerySourceReference(this Expression expression)
        {
            if (expression is QuerySourceReferenceExpression)
            {
                QuerySourceReferenceExpression sourceExpression = expression as QuerySourceReferenceExpression;

                return sourceExpression;
            }
            else if (expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;

                return TryGetQuerySourceReference(memberExpression.Expression);
            }
            else if (expression is SubQueryExpression)
            {
                SubQueryExpression subQueryExpression = expression as SubQueryExpression;

                return TryGetQuerySourceReference(subQueryExpression.QueryModel.MainFromClause.FromExpression);
            }

            return null;
        }

        /// <summary>
        /// Indicate if an expression contains antoher or is equal to it.
        /// </summary>
        /// <param name="expression">An expression.</param>
        /// <param name="e">Expression to be evaluated.</param>
        /// <returns><c>true</c> if <c>e</c> is equal to the given expression or one of its query sources, <c>false</c> otherwise.</returns>
        public static bool ContainsOrEquals(this Expression expression, Expression e)
        {
            if (expression != null)
            {
                if (expression.Equals(e))
                {
                    return true;
                }
                else
                {
                    QuerySourceReferenceExpression sourceExpression = expression.TryGetQuerySourceReference();

                    if (sourceExpression != null && sourceExpression != expression)
                    {
                        return sourceExpression.ContainsOrEquals(e);
                    }
                }
            }

            return false;
        }

        public static string GetKey(this Expression expression)
        {
            string key = expression.ToString().Trim();

            if (key.EndsWith(".Uri"))
            {
                key = key.Substring(0, key.LastIndexOf(".Uri"));
            }

            return key;
        }
    }
}
