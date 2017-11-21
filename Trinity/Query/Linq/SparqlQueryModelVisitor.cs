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
// Copyright (c) Semiodesk GmbH 2015

using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Aggregates.Sparql;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Expressions.Primary;

// TODO:
// - Support scalar query forms.
// - Establish dynamic look-up of query source reference expressions and sub query expresssions to variable names.
// - Make selected variables of sub queries more flexible (currently only subject and object).

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryModelVisitor<T> : QueryModelVisitorBase, ISparqlQueryModelVisitor
    {
        #region Members

        protected bool IsInitialized;

        protected ExpressionTreeVisitor ExpressionVisitor;

        protected SparqlQueryGeneratorTree QueryGeneratorTree;

        protected readonly Dictionary<QueryModel, SparqlQueryGenerator> QueryGenerators = new Dictionary<QueryModel, SparqlQueryGenerator>();

        protected QueryModel CurrentQueryModel;

        protected SparqlQueryGenerator CurrentQueryGenerator;

        protected SparqlQueryGenerator RootQueryGenerator;

        public VariableBuilder VariableBuilder { get; private set; }

        #endregion

        #region Constructors

        public SparqlQueryModelVisitor()
        {
            VariableBuilder = new VariableBuilder();
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
            CurrentQueryGenerator.SetFromClause(fromClause);

            switch(fromClause.FromExpression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        MemberExpression memberExpression = fromClause.FromExpression as MemberExpression;

                        if (memberExpression.Expression is SubQueryExpression)
                        {
                            VisitSubQueryExpression(memberExpression.Expression as SubQueryExpression);
                        }

                        ExpressionVisitor.VisitExpression(fromClause.FromExpression);

                        break;
                    }
            }
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            if (RootQueryGenerator == null)
            {
                throw new ArgumentNullException("RootQueryGenerator");
            }

            // CurrentQueryModel is null when this method is invoked for the first time.
            QueryModel currentQueryModel = CurrentQueryModel;

            // Set the current query model which the query generators manipulate.
            CurrentQueryModel = queryModel;

            // This possibly traverses into sub-queries.
            queryModel.SelectClause.Accept(this, queryModel);

            // Restore the current query model after visiting possible sub queries in the select clause.
            CurrentQueryModel = currentQueryModel;
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            Debug.WriteLine(resultOperator.GetType().ToString());

            SparqlQueryGenerator generator = GetCurrentQueryGenerator();

            generator.SetObjectOperator(resultOperator);
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            queryModel.MainFromClause.Accept(this, queryModel);

            if(selectClause.Selector is MemberExpression)
            {
                MemberExpression expression = selectClause.Selector as MemberExpression;

                ExpressionVisitor.VisitExpression(expression);
            }

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
                    VisitSubQueryExpression(binaryExpression.Left as SubQueryExpression);
                }

                if(binaryExpression.Right is SubQueryExpression)
                {
                    VisitSubQueryExpression(binaryExpression.Right as SubQueryExpression);
                }

                Debug.WriteLine(whereClause.GetType().ToString());
            }
            else
            {
                Debug.WriteLine(whereClause.GetType().ToString());
            }

            ExpressionVisitor.VisitExpression(whereClause.Predicate);
        }

        private void VisitSubQueryExpression(SubQueryExpression expression)
        {
            SparqlQueryGenerator currentQueryGenerator = CurrentQueryGenerator;
            SparqlQueryGenerator subQueryGenerator = new SelectQueryGenerator(this);

            // Enable look-up of query generators from query models.
            QueryGenerators[expression.QueryModel] = subQueryGenerator;

            // Add the sub query to the query tree.
            QueryGeneratorTree.AddQuery(currentQueryGenerator, subQueryGenerator);

            CurrentQueryGenerator = subQueryGenerator;

            ExpressionVisitor.VisitExpression(expression);

            CurrentQueryGenerator = currentQueryGenerator;
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            Debug.WriteLine(orderByClause.GetType().ToString());

            base.VisitOrderByClause(orderByClause, queryModel, index);
        }

        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
        {
            Debug.WriteLine(ordering.GetType().ToString());

            base.VisitOrdering(ordering, queryModel, orderByClause, index);
        }

        public ISparqlQuery GetQuery()
        {
            string queryString = "";

            // Since the dotNetRdf QueryBuilder does not support building sub queries,
            // we need to generate the nested queries here.
            QueryGeneratorTree.Traverse((builder) =>
            {
                string q = builder.BuildQuery().ToString();

                if(!string.IsNullOrEmpty(queryString))
                {
                    int n = q.IndexOf("{") + 1;

                    if(n > 0)
                    {
                        q = q.Insert(n, "{ " + queryString + " }");
                    }
                }

                queryString = q;
            });

            ISparqlQuery query = new SparqlQuery(queryString);

            Debug.WriteLine(query.ToString());

            return query;
        }

        public QueryModel GetCurrentQueryModel()
        {
            return CurrentQueryModel;
        }

        public SparqlQueryGenerator GetCurrentQueryGenerator()
        {
            return CurrentQueryGenerator;
        }

        public SparqlQueryGenerator GetQueryGenerator(QueryModel queryModel)
        {
            return QueryGenerators[queryModel];
        }

        public void SetRootQueryGenerator(SparqlQueryGenerator generator)
        {
            // The current (sub-)query generator.
            CurrentQueryGenerator = generator;

            // The query generator of the outermost query.
            RootQueryGenerator = generator;

            // Add the root query builder to the query tree.
            QueryGeneratorTree = new SparqlQueryGeneratorTree(RootQueryGenerator);

            // The expression tree visitor needs to be initialized *after* the query builders.
            ExpressionVisitor = new ExpressionTreeVisitor(this);
        }

        #endregion
    }
}
