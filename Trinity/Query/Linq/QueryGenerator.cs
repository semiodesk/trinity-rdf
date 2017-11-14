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

using Remotion.Linq.Clauses.Expressions;
using System;
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Aggregates;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    internal class QueryGenerator
    {
        #region Members

        public bool IsBound { get; private set; }

        public SparqlVariable Subject { get; private set; }

        public SparqlVariable Object { get; private set; }

        public ISelectBuilder SelectBuilder { get; private set; }

        public IQueryBuilder QueryBuilder { get; private set; }

        public QueryModelVisitor ModelVisitor { get; private set; }

        #endregion

        #region Constructors

        public QueryGenerator(QueryModelVisitor modelVisitor)
        {
            SelectBuilder = VDS.RDF.Query.Builder.QueryBuilder.Select(new string[] {});
            QueryBuilder = SelectBuilder.GetQueryBuilder();
            ModelVisitor = modelVisitor;
        }

        public QueryGenerator(QueryModelVisitor modelVisitor, ISelectBuilder selectBuilder)
        {
            SelectBuilder = selectBuilder;
            QueryBuilder = SelectBuilder.GetQueryBuilder();
            ModelVisitor = modelVisitor;
        }

        #endregion

        #region Methods

        public void BindVariables()
        {
            IsBound = true;

            if(Subject != null)
            {
                SelectBuilder.And(Subject);
            }

            if(Object != null)
            {
                SelectBuilder.And(Object);

                if (Object.IsAggregate && Subject != null)
                {
                    QueryBuilder.GroupBy(Subject.Name);
                }
            }
        }

        public void SetSubject(SparqlVariable variable)
        {
            if (variable != null && !variable.IsAggregate)
            {
                Subject = variable;
            }
        }

        public bool SetSubjectFromExpression(Expression expression)
        {
            ThrowOnBound();

            if(expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;
                
                if(memberExpression.Expression is SubQueryExpression)
                {
                    // When accessing members of sub queries, then the subject of the 
                    // current query is the 'result' of the sub query.
                    SubQueryExpression subQueryExpression = memberExpression.Expression as SubQueryExpression;

                    QueryGenerator subQueryGenerator = ModelVisitor.GetQueryGenerator(subQueryExpression.QueryModel);

                    if(subQueryGenerator.Object != null)
                    {
                        Subject = new SparqlVariable(subQueryGenerator.Object.Name);

                        // TODO: Also add the inner subject here to the selected variables of the current query.
                        SelectBuilder.And(subQueryGenerator.Subject);

                        QueryBuilder.GroupBy(subQueryGenerator.Subject.Name);

                        return true;
                    }
                }
                else if(memberExpression.Expression is QuerySourceReferenceExpression)
                {
                    // Set the variable name of the query source reference as subject of the current query.
                    QuerySourceReferenceExpression source = memberExpression.Expression as QuerySourceReferenceExpression;

                    Subject = new SparqlVariable(source.ReferencedQuerySource.ItemName, true);

                    return true;
                }
            }

            return false;
        }

        public void SetObject(VariableTerm variable, ISparqlAggregate aggregate = null)
        {
            ThrowOnBound();

            string variableName = variable.Variables.First();

            if (aggregate == null)
            {
                Object = new SparqlVariable(variableName, true);
            }
            else
            {
                Object = new SparqlVariable(aggregate.GetProjectedName(variableName), aggregate);
            }
        }

        public void SetObject(SparqlVariable variable)
        {
            ThrowOnBound();

            if (variable.IsResultVariable)
            {
                Object = variable;
            }
            else
            {
                Object = new SparqlVariable(variable.Name, true);
            }
        }

        public void Equal(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) == new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void Equal(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            INode o = constant.AsNode();

            QueryBuilder.Where(t => t.Subject(Subject.Name).PredicateUri(p.MappedUri).Object(o));
        }

        public void NotEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void NotEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(Subject.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThan(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThan(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(Subject.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(Subject.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThan(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThan(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(Subject.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(Subject.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        private void ThrowOnBound()
        {
            if (IsBound)
            {
                throw new Exception("Cannot modify a bound query.");
            }
        }

        public string BuildQuery()
        {
            if(!IsBound)
            {
                BindVariables();
            }

            return QueryBuilder.BuildQuery().ToString();
        }

        #endregion
    }
}
