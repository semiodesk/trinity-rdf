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
// Copyright (c) Semiodesk GmbH 2015-2020

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query.Sparql
{
    /// <summary>
    /// Partial evaluation ("funcletization") of a LINQ expression tree: every maximal sub-tree that
    /// does not depend on a query parameter is evaluated and replaced with a <see cref="ConstantExpression"/>.
    /// This turns captured locals, field/property reads on closures, <c>new DateTime(...)</c>, static
    /// members like <c>DateTime.MaxValue</c>, etc. into constants before translation — the same job
    /// EF Core's parameter-extraction stage performs. Classic nominate-then-evaluate approach.
    /// </summary>
    internal static class PartialEvaluator
    {
        public static Expression Evaluate(Expression expression)
        {
            HashSet<Expression> candidates = new Nominator().Nominate(expression);

            return new SubtreeEvaluator(candidates).Visit(expression);
        }

        /// <summary>Evaluates the nominated maximal sub-trees, replacing them with constants.</summary>
        private sealed class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates;

            public SubtreeEvaluator(HashSet<Expression> candidates) => _candidates = candidates;

            public override Expression Visit(Expression node)
            {
                if (node == null)
                {
                    return null;
                }

                return _candidates.Contains(node) ? EvaluateNode(node) : base.Visit(node);
            }

            private static Expression EvaluateNode(Expression node)
            {
                if (node.NodeType == ExpressionType.Constant)
                {
                    return node;
                }

                LambdaExpression lambda = Expression.Lambda(node);
                object value = lambda.Compile().DynamicInvoke(null);

                return Expression.Constant(value, node.Type);
            }
        }

        /// <summary>Marks the maximal sub-trees that can be evaluated locally (no parameter dependency).</summary>
        private sealed class Nominator : ExpressionVisitor
        {
            private HashSet<Expression> _candidates;

            private bool _cannotBeEvaluated;

            public HashSet<Expression> Nominate(Expression expression)
            {
                _candidates = new HashSet<Expression>();

                Visit(expression);

                return _candidates;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                // The query source (and anything containing it) must never be evaluated locally —
                // doing so would compile and *run* the query, re-entering the provider (stack overflow).
                if (node.Value is IQueryable)
                {
                    _cannotBeEvaluated = true;
                }

                return base.VisitConstant(node);
            }

            public override Expression Visit(Expression node)
            {
                if (node != null)
                {
                    bool saved = _cannotBeEvaluated;

                    _cannotBeEvaluated = false;

                    base.Visit(node);

                    if (!_cannotBeEvaluated)
                    {
                        if (node.NodeType == ExpressionType.Parameter)
                        {
                            _cannotBeEvaluated = true;
                        }
                        else
                        {
                            _candidates.Add(node);
                        }
                    }

                    _cannotBeEvaluated |= saved;
                }

                return node;
            }
        }
    }
}
