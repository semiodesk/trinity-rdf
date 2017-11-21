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

        protected readonly Dictionary<QueryModel, ISparqlQueryGenerator> QueryGenerators = new Dictionary<QueryModel, ISparqlQueryGenerator>();

        // TODO: Implement the 'current' accessors using a stack. Then we can count to determine if we are in a sub query.
        protected QueryModel CurrentQueryModel;

        protected ISparqlQueryGenerator CurrentQueryGenerator;

        protected QueryModel RootQueryModel;

        protected ISparqlQueryGenerator RootQueryGenerator;

        public SparqlVariableGenerator VariableGenerator { get; private set; }

        #endregion

        #region Constructors

        public SparqlQueryModelVisitor(ISparqlQueryGenerator queryGenerator)
        {
            // Generated variables need to be managed accross sub-queries,
            // so that no duplicate variable names are generated.
            VariableGenerator = new SparqlVariableGenerator();

            // The root quer generator builds the outer-most query which returns the actual results.
            queryGenerator.SetQueryModelVisitor(this);

            // The current (sub-)query generator.
            CurrentQueryGenerator = queryGenerator;

            // The query generator of the outermost query.
            RootQueryGenerator = queryGenerator;

            // Add the root query builder to the query tree.
            QueryGeneratorTree = new SparqlQueryGeneratorTree(queryGenerator);

            // The expression tree visitor needs to be initialized *after* the query builders.
            ExpressionVisitor = new ExpressionTreeVisitor(this);
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
                    VisitSubQueryExpression(memberExpression.Expression as SubQueryExpression);
                }

                ExpressionVisitor.VisitExpression(fromClause.FromExpression);
            }
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            // Store the root query model for reliably detecting if we are in a sub query.
            if (CurrentQueryModel == null)
            {
                RootQueryModel = queryModel;
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

            ISparqlQueryGenerator generator = GetCurrentQueryGenerator();

            generator.SetObjectOperator(resultOperator);
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            queryModel.MainFromClause.Accept(this, queryModel);

            bool isRootQuery = RootQueryModel == CurrentQueryModel;

            CurrentQueryGenerator.Select(selectClause, isRootQuery);

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
            }

            ExpressionVisitor.VisitExpression(whereClause.Predicate);
        }

        public void VisitSubQueryExpression(SubQueryExpression expression)
        {
            ISparqlQueryGenerator currentQueryGenerator = CurrentQueryGenerator;
            ISparqlQueryGenerator subQueryGenerator = new SelectQueryGenerator();
            subQueryGenerator.SetQueryModelVisitor(this);

            // Enable look-up of query generators from query models.
            QueryGenerators[expression.QueryModel] = subQueryGenerator;

            // Add the sub query to the query tree.
            QueryGeneratorTree.AddQuery(currentQueryGenerator, subQueryGenerator);

            // Descend the query tree and implement the sub query before the outer one.
            CurrentQueryGenerator = subQueryGenerator;

            ExpressionVisitor.VisitExpression(expression);

            CurrentQueryGenerator = currentQueryGenerator;

            // Sub queries always select the subject from the select clause of the root query.
            subQueryGenerator.SelectVariable(subQueryGenerator.SubjectVariable);
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

        public ISparqlQueryGenerator GetRootQueryGenerator()
        {
            return RootQueryGenerator;
        }

        public ISparqlQueryGenerator GetCurrentQueryGenerator()
        {
            return CurrentQueryGenerator;
        }

        public ISparqlQueryGenerator GetQueryGenerator(QueryModel queryModel)
        {
            return QueryGenerators[queryModel];
        }

        public bool HasQueryGenerator(QueryModel queryModel)
        {
            return QueryGenerators.ContainsKey(queryModel);
        }

        #endregion
    }
}
