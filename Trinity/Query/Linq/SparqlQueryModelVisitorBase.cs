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
    internal abstract class SparqlQueryModelVisitorBase<T> : QueryModelVisitorBase, IQueryModelVisitor
    {
        #region Members

        protected bool IsInitialized;

        protected ExpressionTreeVisitor ExpressionVisitor;

        protected SparqlQueryGeneratorTree QueryGeneratorTree;

        protected readonly Dictionary<QueryModel, SparqlQueryGenerator> QueryGenerators = new Dictionary<QueryModel, SparqlQueryGenerator>();

        protected QueryModel CurrentQueryModel;

        protected SparqlQueryGenerator CurrentQueryGenerator;

        public VariableBuilder VariableBuilder { get; private set; }

        #endregion

        #region Constructors

        public SparqlQueryModelVisitorBase()
        {
            VariableBuilder = new VariableBuilder();
        }

        #endregion

        #region Methods

        protected abstract void InitializeQueryGenerator(QueryModel queryModel);

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
            if (fromClause.FromExpression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = fromClause.FromExpression as MemberExpression;

                if(memberExpression.Expression is SubQueryExpression)
                {
                    VisitSubQueryExpression(memberExpression.Expression as SubQueryExpression);
                }

                ExpressionVisitor.VisitExpression(fromClause.FromExpression);
            }
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            if (!IsInitialized)
            {
                // Initialize a query generator suitable for the current result type.
                InitializeQueryGenerator(queryModel);

                // The expression tree visitor needs to be initialized *after* the query builders.
                ExpressionVisitor = new ExpressionTreeVisitor(this);

                IsInitialized = true;
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

            // TODO: Improve abstraction of the query types: introduce sub classes of SparqlQueryGenerator.
            if (generator.ObjectVariable != null)
            {
                if (resultOperator is AnyResultOperator)
                {
                    var aggregate = new SampleAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                }
                else if (resultOperator is AverageResultOperator)
                {
                    var aggregate = new AverageAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                }
                else if (resultOperator is CountResultOperator)
                {
                    var aggregate = new CountDistinctAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                }
                else if (resultOperator is FirstResultOperator)
                {
                    var aggregate = new MinAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                    generator.OrderBy(generator.ObjectVariable);
                }
                else if (resultOperator is LastResultOperator)
                {
                    var aggregate = new MinAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                    generator.OrderByDescending(generator.ObjectVariable);
                }
                else if (resultOperator is MaxResultOperator)
                {
                    var aggregate = new MaxAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                }
                else if (resultOperator is MinResultOperator)
                {
                    var aggregate = new MinAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                }
                else if (resultOperator is SumResultOperator)
                {
                    var aggregate = new SumAggregate(new VariableTerm(generator.ObjectVariable.Name));
                    generator.SetObjectVariable(aggregate.AsSparqlVariable());
                }
                else if (resultOperator is OfTypeResultOperator)
                {
                    Type itemType = queryModel.MainFromClause.ItemType;
                    RdfClassAttribute itemClass = itemType.TryGetCustomAttribute<RdfClassAttribute>();

                    if (itemClass == null)
                    {
                        throw new ArgumentException("No RdfClass attrribute declared on type: " + itemType);
                    }

                    SparqlVariable s = generator.SubjectVariable;
                    Uri p = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
                    Uri o = itemClass.MappedUri;

                    generator.Where(e => e.Subject(s.Name).PredicateUri(p).Object(o));
                }
                else if (resultOperator is SkipResultOperator)
                {
                    SkipResultOperator op = resultOperator as SkipResultOperator;
                    generator.Offset(int.Parse(op.Count.ToString()));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            queryModel.MainFromClause.Accept(this, queryModel);

            for(int i = 0; i < queryModel.BodyClauses.Count; i++)
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
            SparqlQueryGenerator subQueryGenerator = new SparqlQueryGenerator(this, QueryBuilder.Select(new string[] {}));

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

        #endregion
    }
}
