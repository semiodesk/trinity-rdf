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
using System;
using System.Diagnostics;
using VDS.RDF.Query.Optimisation;

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
        protected readonly ISparqlQueryGeneratorTree QueryGeneratorTree;

        /// <summary>
        /// Visits all expressions in a query model and handles the query generation.
        /// </summary>
        protected readonly ExpressionTreeVisitor ExpressionVisitor;

        #endregion

        #region Constructors

        public SparqlQueryModelVisitor(ISparqlQueryGenerator queryGenerator)
        {
            // Add the root query builder to the query tree.
            QueryGeneratorTree = new SparqlQueryGeneratorTree(queryGenerator);

            // The expression tree visitor needs to be initialized *after* the query builders.
            ExpressionVisitor = new ExpressionTreeVisitor(this, QueryGeneratorTree);
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
            ISparqlQueryGenerator currentGenerator = QueryGeneratorTree.CurrentGenerator;

            currentGenerator.OnBeforeFromClauseVisited(fromClause.FromExpression);

            ExpressionVisitor.VisitFromExpression(fromClause.FromExpression, fromClause.ItemName, fromClause.ItemType);

            currentGenerator.OnFromClauseVisited(fromClause.FromExpression);
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            ISparqlQueryGenerator g = QueryGeneratorTree.CurrentGenerator;

            g.SetQueryContext(QueryGeneratorTree, queryModel);

            // Handle the main from clause before the select.
            queryModel.MainFromClause.Accept(this, queryModel);

            // This possibly traverses into sub-queries.
            queryModel.SelectClause.Accept(this, queryModel);
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            ISparqlQueryGenerator generator = QueryGeneratorTree.CurrentGenerator;

            // If we are in a sub query, apply the operator on the query object.
            if(generator.ObjectVariable != null)
            {
                generator.SetObjectOperator(resultOperator);
            }
            else if(generator.SubjectVariable != null && generator.IsRoot)
            {
                generator.SetSubjectOperator(resultOperator);
            }
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            ISparqlQueryGenerator currentGenerator = QueryGeneratorTree.CurrentGenerator;

            currentGenerator.OnBeforeSelectClauseVisited(selectClause.Selector);

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

            currentGenerator.OnSelectClauseVisited(selectClause.Selector);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            ExpressionVisitor.Visit(whereClause.Predicate);
        }

        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
        {
            base.VisitOrdering(ordering, queryModel, orderByClause, index);

            ExpressionVisitor.VisitOrdering(ordering, index);
        }

        public ISparqlQuery GetQuery()
        {
            string queryString = QueryGeneratorTree.RootGenerator.BuildQuery();

            ISparqlQuery query = new SparqlQuery(queryString);

#if DEBUG
            Debug.WriteLine(query.ToString());
#endif

            return query;
        }

        #endregion
    }
}
