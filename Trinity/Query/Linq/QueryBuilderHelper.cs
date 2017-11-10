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
using VDS.RDF.Query.Aggregates.Sparql;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    internal class QueryBuilderHelper
    {
        #region Members

        public bool IsBound { get; private set; }

        public ISelectBuilder SelectBuilder { get; private set; }

        public SparqlVariable SubjectVariable { get; private set; }

        public SparqlVariable ObjectVariable { get; private set; }

        public IQueryBuilder QueryBuilder { get; private set; }

        public QueryModelVisitor QueryModelVisitor { get; private set; }

        #endregion

        #region Constructors

        public QueryBuilderHelper(QueryModelVisitor modelVisitor)
        {
            SelectBuilder = VDS.RDF.Query.Builder.QueryBuilder.Select(new string[] {});
            QueryBuilder = SelectBuilder.GetQueryBuilder();
            QueryModelVisitor = modelVisitor;
        }

        public QueryBuilderHelper(QueryModelVisitor modelVisitor, ISelectBuilder selectBuilder)
        {
            SelectBuilder = selectBuilder;
            QueryBuilder = SelectBuilder.GetQueryBuilder();
            QueryModelVisitor = modelVisitor;
        }

        #endregion

        #region Methods

        public void BindVariables()
        {
            IsBound = true;

            SelectBuilder.And(SubjectVariable);

            if(ObjectVariable.IsAggregate)
            {
                string name = ObjectVariable.Name;
                string functor = ObjectVariable.Aggregate.Functor.ToLowerInvariant();

                SelectBuilder.And(e => e.Count(name)).As(string.Format("{0}_{1}", name, functor));

                QueryBuilder.GroupBy(SubjectVariable.Name);
            }
            else
            {
                SelectBuilder.And(ObjectVariable);
            }
        }

        public bool SetSubjectFromExpression(Expression expression)
        {
            ThrowOnBound();

            QuerySourceReferenceExpression source = expression.TryGetQuerySource();

            if (source != null)
            {
                SubjectVariable = new SparqlVariable(source.ReferencedQuerySource.ItemName, true);

                return true;
            }

            return false;
        }

        public void SetObject(VariableTerm variable, ISparqlAggregate aggregate = null)
        {
            ThrowOnBound();

            string variableName = variable.Variables.First();

            if (aggregate == null)
            {
                ObjectVariable = new SparqlVariable(variableName, true);
            }
            else
            {
                ObjectVariable = new SparqlVariable(variableName, aggregate);
            }
        }

        public void SetObject(SparqlVariable variable)
        {
            ThrowOnBound();

            if (variable.IsResultVariable)
            {
                ObjectVariable = variable;
            }
            else
            {
                ObjectVariable = new SparqlVariable(variable.Name, true);
            }
        }

        public void Equal(VariableExpression variable, ConstantExpression constant)
        {
            string v = variable.Expression.Variables.First();

            QueryBuilder.Filter(e => e.Variable(v) == new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void Equal(MemberExpression member, ConstantExpression constant)
        {
            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            INode o = constant.AsNode();

            QueryBuilder.Where(t => t.Subject(SubjectVariable.Name).PredicateUri(p.MappedUri).Object(o));
        }

        public void NotEqual(VariableExpression variable, ConstantExpression constant)
        {
            string v = variable.Expression.Variables.First();

            QueryBuilder.Filter(e => e.Variable(v) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void NotEqual(MemberExpression member, ConstantExpression constant)
        {
            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = QueryModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(SubjectVariable.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThan(VariableExpression variable, ConstantExpression constant)
        {
            string v = variable.Expression.Variables.First();

            QueryBuilder.Filter(e => e.Variable(v) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThan(MemberExpression member, ConstantExpression constant)
        {
            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = QueryModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(SubjectVariable.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThanOrEqual(VariableExpression variable, ConstantExpression constant)
        {
            string v = variable.Expression.Variables.First();

            QueryBuilder.Filter(e => e.Variable(v) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void GreaterThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = QueryModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(SubjectVariable.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThan(VariableExpression variable, ConstantExpression constant)
        {
            string v = variable.Expression.Variables.First();

            QueryBuilder.Filter(e => e.Variable(v) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThan(MemberExpression member, ConstantExpression constant)
        {
            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = QueryModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(SubjectVariable.Name).PredicateUri(p.MappedUri).Object(o.Name));
            QueryBuilder.Filter(e => e.Variable(o.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThanOrEqual(VariableExpression variable, ConstantExpression constant)
        {
            string v = variable.Expression.Variables.First();

            QueryBuilder.Filter(e => e.Variable(v) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void LessThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            RdfPropertyAttribute p = member.GetRdfPropertyAttribute();
            SparqlVariable o = QueryModelVisitor.VariableBuilder.GenerateObjectVariable();

            QueryBuilder.Where(t => t.Subject(SubjectVariable.Name).PredicateUri(p.MappedUri).Object(o.Name));
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
