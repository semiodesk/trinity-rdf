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
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    /// <summary>
    /// Generates a SPARQL query from a LINQ query model by visiting all clauses and invoking 
    /// expression implementation using a <c>ExpressionTreeVisitor</c>.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    internal class SparqlQueryModelVisitor<T> : QueryModelVisitorBase, ISparqlQueryModelVisitor
    {
        #region Members

        /// <summary>
        /// Allows to access query generators and sub query generators in a tree-like fashion.
        /// </summary>
        private ISparqlQueryGeneratorTree _queryGeneratorTree;

        /// <summary>
        /// A common variable name generator for all query generators.
        /// </summary>
        private SparqlVariableGenerator _variableGenerator = new SparqlVariableGenerator();

        /// <summary>
        /// Visits all expressions in a query model and handles the query generation.
        /// </summary>
        private ExpressionTreeVisitor _expressionVisitor;

        #endregion

        #region Constructors

        public SparqlQueryModelVisitor(ISparqlQueryGenerator queryGenerator)
        {
            // Add the root query builder to the query tree.
            _queryGeneratorTree = new SparqlQueryGeneratorTree(queryGenerator, _variableGenerator);

            // The expression tree visitor needs to be initialized *after* the query builders.
            _expressionVisitor = new ExpressionTreeVisitor(this, _queryGeneratorTree, _variableGenerator);
        }

        #endregion

        #region Methods

        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
        {
            throw new NotSupportedException();
        }

        public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index)
        {
            throw new NotSupportedException();
        }

        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
        {
            throw new NotSupportedException();
        }

        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, GroupJoinClause groupJoinClause)
        {
            throw new NotSupportedException();
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            if(fromClause.FromExpression is MemberExpression)
            {
                MemberExpression memberExpression = fromClause.FromExpression as MemberExpression;

                if (memberExpression.Expression is SubQueryExpression)
                {
                    SubQueryExpression subQueryExpression = memberExpression.Expression as SubQueryExpression;

                    _expressionVisitor.VisitExpression(subQueryExpression);
                }

                _expressionVisitor.VisitExpression(fromClause.FromExpression);
            }
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            // The root query generator cannot be registered until this method is invoked.
            if(!_queryGeneratorTree.HasQueryGenerator(queryModel))
            {
                ISparqlQueryGenerator generator = _queryGeneratorTree.GetRootQueryGenerator();

                _queryGeneratorTree.RegisterQueryGenerator(generator, queryModel);
            }

            // This possibly traverses into sub-queries.
            queryModel.SelectClause.Accept(this, queryModel);
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            ISparqlQueryGenerator generator = _queryGeneratorTree.GetCurrentQueryGenerator();

            generator.SetObjectOperator(resultOperator);
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            queryModel.MainFromClause.Accept(this, queryModel);

            _expressionVisitor.VisitSelectExpression(selectClause.Selector, true);

            for (int i = 0; i < queryModel.BodyClauses.Count; i++)
            {
                IBodyClause c = queryModel.BodyClauses[i];

                c.Accept(this, queryModel, i);
            }

            for(int i = 0; i < queryModel.ResultOperators.Count; i++)
            {
                ResultOperatorBase o = queryModel.ResultOperators[i];

                o.Accept(this, queryModel, i);
            }
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            if (whereClause.Predicate is BinaryExpression)
            {
                BinaryExpression binaryExpression = whereClause.Predicate as BinaryExpression;

                if(binaryExpression.Left is SubQueryExpression)
                {
                    SubQueryExpression subQueryExpression = binaryExpression.Left as SubQueryExpression;

                    _expressionVisitor.VisitExpression(subQueryExpression);
                }

                if(binaryExpression.Right is SubQueryExpression)
                {
                    SubQueryExpression subQueryExpression = binaryExpression.Right as SubQueryExpression;

                    _expressionVisitor.VisitExpression(subQueryExpression);
                }
            }

            _expressionVisitor.VisitExpression(whereClause.Predicate);
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            base.VisitOrderByClause(orderByClause, queryModel, index);
        }

        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
        {
            base.VisitOrdering(ordering, queryModel, orderByClause, index);
        }

        public ISparqlQuery GetQuery()
        {
            string queryString = _queryGeneratorTree.GetRootQueryGenerator().BuildQuery();

            ISparqlQuery query = new SparqlQuery(queryString);

            Debug.WriteLine(query.ToString());

            return query;
        }

        #endregion
    }
}
