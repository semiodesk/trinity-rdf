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

        private readonly ExpressionTreeVisitor _expressionVisitor;

        private readonly QueryGenerator _rootGenerator;

        private readonly Dictionary<QueryModel, QueryGenerator> _queryGenerators = new Dictionary<QueryModel, QueryGenerator>();

        private QueryGenerator _currentQueryGenerator;

        private readonly QueryGeneratorTree _queryBuilderTree;

        public VariableBuilder VariableBuilder { get; private set; }

        #endregion

        #region Constructors

        public QueryModelVisitor()
        {
            VariableBuilder = new VariableBuilder();

            // The root query which selects triples when returning resources.
            _rootGenerator = new QueryGenerator(this, QueryBuilder.Select(new string[] { }));
            _currentQueryGenerator = _rootGenerator;

            // Add the root query builder to the query tree.
            _queryBuilderTree = new QueryGeneratorTree(_rootGenerator);

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

            QueryGenerator context = GetCurrentQueryGenerator();

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

            if(_currentQueryGenerator == _rootGenerator)
            {
                // Bind the subject variable for queries which return resources.
                QuerySourceReferenceExpression sourceExpression = selectClause.Selector.TryGetQuerySource();

                if(sourceExpression != null)
                {
                    IQuerySource source = sourceExpression.ReferencedQuerySource;

                    _rootGenerator.SelectBuilder.And(source.ItemName, "p_", "o_");

                    _rootGenerator.QueryBuilder.Where(e => e.Subject(source.ItemName).Predicate("p_").Object("o_"));
                }
            }

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
            QueryGenerator currentQueryGenerator = _currentQueryGenerator;
            QueryGenerator subQueryGenerator = new QueryGenerator(this);

            _queryGenerators[expression.QueryModel] = subQueryGenerator;

            _currentQueryGenerator = subQueryGenerator;

            _expressionVisitor.VisitExpression(expression);

            _currentQueryGenerator = currentQueryGenerator;

            // Add the sub query to the query builder tree.
            _queryBuilderTree.AddQuery(currentQueryGenerator, subQueryGenerator);
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

            // Since the dotNetRdf query builder does not support sub queries,
            // we need to generate the nested query here.
            _queryBuilderTree.Traverse((builder) =>
            {
                string q = builder.BuildQuery().ToString();

                if(!string.IsNullOrEmpty(queryString))
                {
                    int n = q.IndexOf("{") + 1;

                    q = q.Insert(n, "{ " + queryString + " }");
                }

                queryString = q;
            });

            ISparqlQuery query = new SparqlQuery(queryString);

            Debug.WriteLine(query.ToString());

            return query;
        }

        internal QueryGenerator GetCurrentQueryGenerator()
        {
            return _currentQueryGenerator;
        }

        internal QueryGenerator GetQueryGenerator(QueryModel queryModel)
        {
            return _queryGenerators[queryModel];
        }

        #endregion
    }
}
