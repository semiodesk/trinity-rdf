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
    internal class SparqlQueryGenerator : ISparqlQueryGenerator
    {
        #region Members

        public bool IsBound { get; private set; }

        protected ISelectBuilder SelectBuilder;

        protected IQueryBuilder QueryBuilder;

        protected IGraphPatternBuilder PatternBuilder { get; private set; }

        public QueryModel QueryModel { get; private set; }

        public SparqlVariable SubjectVariable { get; private set; }

        public SparqlVariable ObjectVariable { get; private set; }

        public IList<SparqlVariable> SelectedVariables { get; private set; }

        protected SparqlVariableGenerator VariableGenerator;

        #endregion

        #region Constructors

        public SparqlQueryGenerator(IQueryBuilder queryBuilder)
        {
            SelectedVariables = new List<SparqlVariable>();
            QueryBuilder = queryBuilder;
            PatternBuilder = QueryBuilder.RootGraphPatternBuilder;
        }

        public SparqlQueryGenerator(ISelectBuilder selectBuilder)
        {
            SelectedVariables = new List<SparqlVariable>();
            SelectBuilder = selectBuilder;
            QueryBuilder = selectBuilder.GetQueryBuilder();
            PatternBuilder = QueryBuilder.RootGraphPatternBuilder;
        }

        #endregion

        #region Methods

        public string BuildQuery()
        {
            if (!IsBound)
            {
                BindVariables();
            }

            return QueryBuilder.BuildQuery().ToString();
        }

        private void BindVariables()
        {
            IsBound = true;

            if (SelectBuilder != null)
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

        protected void BuildMemberAccess(MemberExpression member, INode o)
        {
            BuildMemberAccess(member, (s, p) =>
            {
                PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o));
            });
        }

        protected void BuildMemberAccess(MemberExpression member, SparqlVariable o)
        {
            BuildMemberAccess(member, (s, p) =>
            {
                PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o.Name));
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
                    PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p.MappedUri).Object(o.Name));
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

        public void SetQueryModel(QueryModel queryModel)
        {
            QueryModel = queryModel;
        }

        public void SetVariableGenerator(SparqlVariableGenerator variableGenerator)
        {
            VariableGenerator = variableGenerator;
        }

        public virtual void SetObjectOperator(ResultOperatorBase resultOperator)
        {
            ThrowOnBound();
        }

        public void SetObjectVariable(SparqlVariable variable, bool select = false)
        {
            ThrowOnBound();

            if (variable != null)
            {
                if(select)
                {
                    DeselectVariable(ObjectVariable);
                    SelectVariable(variable);
                }

                ObjectVariable = variable;
            }
        }

        public void SetSubjectVariable(SparqlVariable variable, bool select = false)
        {
            ThrowOnBound();

            if (variable != null && !variable.IsAggregate)
            {
                if (select)
                {
                    DeselectVariable(SubjectVariable);
                    SelectVariable(variable);
                }

                SubjectVariable = variable;
            }
        }

        public void DeselectVariable(SparqlVariable variable)
        {
            ThrowOnBound();

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
            ThrowOnBound();

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

        public virtual void Select(Expression selector, bool isRootQuery)
        {
            ThrowOnBound();
        }

        public void Where(MemberExpression member, SparqlVariable variable)
        {
            SparqlVariable s = SubjectVariable;

            RdfPropertyAttribute attribute = member.Member.TryGetCustomAttribute<RdfPropertyAttribute>();

            if (QueryModel.HasNumericResultOperator())
            {
                SparqlVariable p2 = VariableGenerator.GetPredicateVariable();
                SparqlVariable o2 = VariableGenerator.GetObjectVariable();

                PatternBuilder.Where(t => t.Subject(s.Name).Predicate(p2.Name).Object(o2.Name));

                // For numeric results, allow to select non existing triple patterns as zero values.
                PatternBuilder.Optional(g => g.Where(t => t.Subject(s.Name).PredicateUri(attribute.MappedUri).Object(variable.Name)));
            }
            else
            {
                // Otherwise make the pattern non-optional.
                PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(attribute.MappedUri).Object(variable.Name));
            }
        }

        public void WhereEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            PatternBuilder.Filter(e => e.Variable(variable.Name) == new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            BuildMemberAccess(member, constant.AsNode());
        }

        public void WhereNotEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            PatternBuilder.Filter(e => e.Variable(variable.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereNotEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(member, o);

            PatternBuilder.Filter(e => e.Variable(o.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThan(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            PatternBuilder.Filter(e => e.Variable(variable.Name) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThan(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(member, o);

            PatternBuilder.Filter(e => e.Variable(o.Name) > new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            PatternBuilder.Filter(e => e.Variable(variable.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(member, o);

            PatternBuilder.Filter(e => e.Variable(o.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThan(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            PatternBuilder.Filter(e => e.Variable(variable.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThan(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(member, o);

            PatternBuilder.Filter(e => e.Variable(o.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            ThrowOnBound();

            PatternBuilder.Filter(e => e.Variable(variable.Name) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThanOrEqual(MemberExpression member, ConstantExpression constant)
        {
            ThrowOnBound();

            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(member, o);

            PatternBuilder.Filter(e => e.Variable(o.Name) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereOfType(SparqlVariable subject, Type type)
        {
            ThrowOnBound();

            if (typeof(Resource).IsAssignableFrom(type))
            {
                // Assert the resource type, if any.
                RdfClassAttribute t = type.TryGetCustomAttribute<RdfClassAttribute>();

                if (t != null)
                {
                    Uri a = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

                    PatternBuilder.Where(e => e.Subject(subject.Name).PredicateUri(a).Object(t.MappedUri));
                }
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

        public void Offset(int offset)
        {
            QueryBuilder.Offset(offset);
        }
        
        public void Limit(int limit)
        {
            QueryBuilder.Limit(limit);
        }

        public void Union(GraphPatternBuilder firstBuilder, params GraphPatternBuilder[] otherBuilders)
        {
            PatternBuilder.Union(firstBuilder, otherBuilders);
        }

        public void Union(Action<IGraphPatternBuilder> buildFirstPattern, params Action<IGraphPatternBuilder>[] buildOtherPatterns)
        {
            PatternBuilder.Union(buildFirstPattern, buildOtherPatterns);
        }

        public void SetPatternBuilder(IGraphPatternBuilder patternBuilder)
        {
            PatternBuilder = patternBuilder;
        }

        public void ResetPatternBuilder()
        {
            PatternBuilder = QueryBuilder.RootGraphPatternBuilder;
        }

        private void ThrowOnBound()
        {
            if (IsBound)
            {
                throw new Exception("Cannot modify a bound query.");
            }
        }

        #endregion
    }
}
