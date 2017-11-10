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
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Aggregates.Sparql;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    internal class QueryModelVisitor : QueryModelVisitorBase
    {
        #region Members

        private ExpressionTreeVisitor _expressionVisitor;

        private readonly IQueryBuilder _rootQueryBuilder;

        private readonly Stack<QueryBuilderHelper> _queryBuilderHelpers = new Stack<QueryBuilderHelper>();

        public VariableBuilder VariableBuilder { get; private set; }

        #endregion

        #region Constructors

        public QueryModelVisitor()
        {
            VariableBuilder = new VariableBuilder();

            // The root query which selects triples when returning resources.
            _rootQueryBuilder = QueryBuilder
                .Select("s_", "p_", "o_")
                .Where(t => t.Subject("s_").Predicate("p_").Object("o_"));

            // The expression tree visitor needs to be initialized *after* the query builders.
            _expressionVisitor = new ExpressionTreeVisitor(this);
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
            if (fromClause.FromExpression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = fromClause.FromExpression as MemberExpression;

                if(memberExpression.Expression is SubQueryExpression)
                {
                    VisitSubQueryExpression(memberExpression.Expression as SubQueryExpression);
                }

                Debug.WriteLine(fromClause.GetType().ToString() + ": " + fromClause.ItemName);

                _expressionVisitor.VisitExpression(fromClause.FromExpression);
            }
            else
            {
                Debug.WriteLine(fromClause.GetType().ToString() + ": " + fromClause.ItemName);
            }
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            Debug.WriteLine(queryModel.GetType().ToString());

            queryModel.SelectClause.Accept(this, queryModel);
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            Debug.WriteLine(resultOperator.GetType().ToString());

            QueryBuilderHelper context = GetQueryBuilderHelper();

            if (resultOperator is AnyResultOperator)
            {
                VariableTerm o = new VariableTerm(context.ObjectVariable.Name);
                context.SetObject(o, new SampleAggregate(o));
            }
            else if (resultOperator is AverageResultOperator)
            {
                VariableTerm o = new VariableTerm(context.ObjectVariable.Name);
                context.SetObject(o, new AverageAggregate(o));
            }
            else if(resultOperator is CountResultOperator)
            {
                VariableTerm o = new VariableTerm(context.ObjectVariable.Name);
                context.SetObject(o, new CountAggregate(o));
            }
            else if(resultOperator is FirstResultOperator)
            {
                context.QueryBuilder.OrderBy(context.ObjectVariable.Name);
                context.QueryBuilder.Limit(1);
            }
            else if(resultOperator is LastResultOperator)
            {
                context.QueryBuilder.OrderByDescending(context.ObjectVariable.Name);
                context.QueryBuilder.Limit(1);
            }
            else if(resultOperator is MaxResultOperator)
            {
                VariableTerm o = new VariableTerm(context.ObjectVariable.Name);
                context.SetObject(o, new MaxAggregate(o));
            }
            else if(resultOperator is MinResultOperator)
            {
                VariableTerm o = new VariableTerm(context.ObjectVariable.Name);
                context.SetObject(o, new MinAggregate(o));
            }
            else if(resultOperator is OfTypeResultOperator)
            {
                Type itemType = queryModel.MainFromClause.ItemType;
                RdfClassAttribute itemClass = itemType.TryGetCustomAttribute<RdfClassAttribute>();

                if(itemClass == null)
                {
                    throw new ArgumentException("No RdfClass attrribute declared on type: " + itemType);
                }

                SparqlVariable s = context.SubjectVariable;
                Uri o = itemClass.MappedUri;

                context.QueryBuilder.Where(e => e.Subject(s.Name).PredicateUri("rdf:type").Object(o));
            }
            else if(resultOperator is SumResultOperator)
            {
                VariableTerm o = new VariableTerm(context.ObjectVariable.Name);
                context.SetObject(o, new SumAggregate(o));
            }
            else if(resultOperator is SkipResultOperator)
            {
                SkipResultOperator op = resultOperator as SkipResultOperator;
                context.QueryBuilder.Offset(int.Parse(op.Count.ToString()));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            Debug.WriteLine(selectClause.GetType().ToString());

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

            _expressionVisitor.VisitExpression(whereClause.Predicate);
        }

        private void VisitSubQueryExpression(SubQueryExpression expression)
        {
            QueryBuilderHelper context = new QueryBuilderHelper(this);

            _queryBuilderHelpers.Push(context);

            _expressionVisitor.VisitExpression(expression);

            Debug.WriteLine(context.BuildQuery());

            _queryBuilderHelpers.Pop();
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
            string query = _rootQueryBuilder.BuildQuery().ToString();

            Debug.WriteLine(query);

            return new SparqlQuery(query);
        }

        internal QueryBuilderHelper GetQueryBuilderHelper()
        {
            return _queryBuilderHelpers.Peek();
        }

        #endregion
    }
}
