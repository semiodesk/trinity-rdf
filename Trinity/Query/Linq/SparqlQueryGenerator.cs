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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryGenerator
    {
        #region Members

        public bool IsBound { get; private set; }

        protected ISelectBuilder SelectBuilder;

        protected IQueryBuilder QueryBuilder;

        public IQueryModelVisitor ModelVisitor { get; private set; }

        public SparqlVariable SubjectVariable { get; private set; }

        public SparqlVariable ObjectVariable { get; private set; }

        public IList<SparqlVariable> SelectedVariables { get; private set; }

        #endregion

        #region Constructors

        public SparqlQueryGenerator(IQueryModelVisitor modelVisitor, IQueryBuilder queryBuilder)
        {
            SelectedVariables = new List<SparqlVariable>();
            QueryBuilder = queryBuilder;
            ModelVisitor = modelVisitor;
        }

        public SparqlQueryGenerator(IQueryModelVisitor modelVisitor, ISelectBuilder selectBuilder)
        {
            SelectedVariables = new List<SparqlVariable>();
            SelectBuilder = selectBuilder;
            QueryBuilder = selectBuilder.GetQueryBuilder();
            ModelVisitor = modelVisitor;
        }

        #endregion

        #region Methods

        private void BindVariables()
        {
            IsBound = true;

            if(SelectBuilder != null)
            {
                bool hasAggregate = SelectedVariables.Any(v => v.IsAggregate);

                foreach (SparqlVariable variable in SelectedVariables)
                {
                    SelectBuilder.And(variable);

                    if (hasAggregate && !variable.IsAggregate)
                    {
                        QueryBuilder.GroupBy(variable.Name);
                    }
                }
            }
        }

        public void SetSubjectVariable(SparqlVariable variable)
        {
            ThrowOnBound();

            if (variable != null && !variable.IsAggregate)
            {
                DeselectVariable(SubjectVariable);

                SubjectVariable = variable;

                SelectVariable(variable);
            }
        }

        public bool SetSubjectVariableFromExpression(Expression expression)
        {
            ThrowOnBound();

            if(expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;
                
                if(memberExpression.Expression is SubQueryExpression)
                {
                    SubQueryExpression subQueryExpression = memberExpression.Expression as SubQueryExpression;

                    SparqlQueryGenerator subQueryGenerator = ModelVisitor.GetQueryGenerator(subQueryExpression.QueryModel);

                    if(subQueryGenerator.ObjectVariable != null)
                    {
                        // Make the subject variable of sub queries available for outer queries.
                        SelectVariable(subQueryGenerator.SubjectVariable);

                        // The object of the sub query is the subject of the outer query.
                        SetSubjectVariable(new SparqlVariable(subQueryGenerator.ObjectVariable.Name));

                        return true;
                    }
                }
                else if(memberExpression.Expression is QuerySourceReferenceExpression)
                {
                    // Set the variable name of the query source reference as subject of the current query.
                    QuerySourceReferenceExpression source = memberExpression.Expression as QuerySourceReferenceExpression;

                    SetSubjectVariable(new SparqlVariable(source.ReferencedQuerySource.ItemName, true));

                    return true;
                }
            }

            return false;
        }

        public void SetObjectVariable(SparqlVariable variable)
        {
            ThrowOnBound();

            if(variable != null)
            {
                DeselectVariable(ObjectVariable);

                ObjectVariable = variable;

                SelectVariable(ObjectVariable);
            }
        }

        public void DeselectVariable(SparqlVariable variable)
        {
            if (SelectBuilder != null)
            {
                if (variable != null && SelectedVariables.Contains(variable))
                {
                    SelectedVariables.Remove(variable);
                }
            }
            else
            {
                string msg = "Cannot deselect variables with non-SELECT query type.";
                throw new Exception(msg);
            }
        }

        public void SelectVariable(SparqlVariable variable)
        {
            if(SelectBuilder != null)
            {
                if (variable != null && !SelectedVariables.Contains(variable))
                {
                    SelectedVariables.Add(variable);
                }
            }
            else
            {
                string msg = "Cannot select variables with non-SELECT query type.";
                throw new Exception(msg);
            }
        }

        public void Where(Action<ITriplePatternBuilder> buildTriplePatterns)
        {
            QueryBuilder.Where(buildTriplePatterns);
        }

        public void WhereEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) == new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            BuildMemberAccess(member, constant.AsNode());
        }

        public void WhereNotEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereNotEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            BuildMemberAccess(member, o);

            QueryBuilder.Filter(e => e.Variable(o.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThan(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThan(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            BuildMemberAccess(member, o);

            QueryBuilder.Filter(e => e.Variable(o.Name) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            BuildMemberAccess(member, o);

            QueryBuilder.Filter(e => e.Variable(o.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThan(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThan(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            BuildMemberAccess(member, o);

            QueryBuilder.Filter(e => e.Variable(o.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            QueryBuilder.Filter(e => e.Variable(variable.Name) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = ModelVisitor.VariableBuilder.GenerateObjectVariable();

            BuildMemberAccess(member, o);

            QueryBuilder.Filter(e => e.Variable(o.Name) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        private void BuildMemberAccess(MemberExpression member, INode o)
        {
            BuildMemberAccess(member, (s, p) =>
            {
                QueryBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o));
            });            
        }

        private void BuildMemberAccess(MemberExpression member, SparqlVariable o)
        {
            BuildMemberAccess(member, (s, p) =>
            {
                QueryBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o.Name));
            });
        }

        private SparqlVariable BuildMemberAccess(Expression expression, Action<SparqlVariable, Uri> buildMemberAccessTriple)
        {
            // Recursively generate the property path which leads to the query source.
            if (expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;
                MemberInfo member = memberExpression.Member;

                var s = BuildMemberAccess(memberExpression.Expression, null);
                var p = member.TryGetRdfPropertyAttribute();
                var o = new SparqlVariable(member.Name.ToLowerInvariant());

                if (buildMemberAccessTriple == null)
                {
                    QueryBuilder.Where(t => t.Subject(s.Name).PredicateUri(p.MappedUri).Object(o.Name));
                }
                else
                {
                    buildMemberAccessTriple(s, p.MappedUri);
                }

                return o;
            }
            else
            {
                return SubjectVariable;
            }
        }

        public void OrderBy(SparqlVariable variable)
        {
            QueryBuilder.OrderBy(variable.Name);
        }

        public void OrderByDescending(SparqlVariable variable)
        {
            QueryBuilder.OrderByDescending(variable.Name);
        }

        public void Optional(Action<IGraphPatternBuilder> buildGraphPattern)
        {
            QueryBuilder.Optional(buildGraphPattern);
        }

        public void Offset(int offset)
        {
            QueryBuilder.Offset(offset);
        }
        
        public void Limit(int limit)
        {
            QueryBuilder.Limit(limit);
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
