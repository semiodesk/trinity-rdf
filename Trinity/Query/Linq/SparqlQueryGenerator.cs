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
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Patterns;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryGenerator : ISparqlQueryGenerator
    {
        #region Members

        public bool IsRoot
        {
            get { return this == QueryGeneratorTree.GetRootQueryGenerator(); }
        }

        public bool IsBound { get; private set; }

        protected ISelectBuilder SelectBuilder;

        protected IQueryBuilder QueryBuilder;

        protected IGraphPatternBuilder PatternBuilder { get; private set; }

        public QueryModel QueryModel { get; private set; }

        public SparqlVariable SubjectVariable { get; private set; }

        public SparqlVariable ObjectVariable { get; private set; }

        public IList<SparqlVariable> SelectedVariables { get; private set; }

        protected SparqlVariableGenerator VariableGenerator;

        protected ISparqlQueryGeneratorTree QueryGeneratorTree;

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

        public bool HasResultOperator<T>()
        {
            return QueryModel.ResultOperators.Any(op => op is T);
        }

        public bool HasNumericResultOperator()
        {
            return QueryModel.ResultOperators.Any(op => IsNumericResultOperator(op));
        }

        private bool IsNumericResultOperator(ResultOperatorBase op)
        {
            if (op is SumResultOperator
                || op is CountResultOperator
                || op is LongCountResultOperator
                || op is AverageResultOperator
                || op is MinResultOperator
                || op is MaxResultOperator)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    
        public string BuildQuery()
        {
            if(!IsBound)
            {
                BindSelectVariables();
            }

            return QueryBuilder.BuildQuery().ToString();
        }

        public void BindSelectVariables()
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

                if(hasAggregate && !IsRoot)
                {
                    QueryBuilder.Distinct();
                }
            }
        }

        protected SparqlVariable BuildMemberAccessOptional(MemberExpression member, SparqlVariable o)
        {
            var optionalBuilder = new GraphPatternBuilder(GraphPatternType.Optional);
            var currentBuilder = PatternBuilder;

            Child(optionalBuilder);

            PatternBuilder = optionalBuilder;

            var v = BuildMemberAccess(member, o);

            PatternBuilder = currentBuilder;

            return v;
        }

        protected SparqlVariable BuildMemberAccess(MemberExpression member)
        {
            return BuildMemberAccess(member, (s, p, o) =>
            {
                PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o));
            });
        }

        protected SparqlVariable BuildMemberAccess(MemberExpression member, INode o)
        {
            return BuildMemberAccess(member, (s, p, x) =>
            {
                PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o));
            });
        }

        protected SparqlVariable BuildMemberAccess(MemberExpression member, SparqlVariable o)
        {
            return BuildMemberAccess(member, (s, p, x) =>
            {
                PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o.Name));
            });
        }

        /// <todo>
        /// Refactor: The method does not call buildMemberAccessTriple in any case. Therefore when calling
        /// BuildMemberAccess with a node or sparql variable the passed parameter is not always used as expected.
        /// </todo>
        private SparqlVariable BuildMemberAccess(Expression expression, Action<SparqlVariable, Uri, SparqlVariable> buildMemberAccessTriple)
        {
            if (expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;
                MemberInfo member = memberExpression.Member;

                if(member.IsSystemType())
                {
                    return VariableGenerator.GetVariable(memberExpression.Expression);
                }
                else
                {
                    // Recursively generate the property path which leads to the query source (buildMemberAccessTriple = null).
                    var s = BuildMemberAccess(memberExpression.Expression, null);
                    var p = memberExpression.TryGetRdfPropertyAttribute();
                    var o = new SparqlVariable(member.Name.ToLowerInvariant());

                    if (buildMemberAccessTriple == null)
                    {
                        // We are recursively building the property path. We need a property mapping for _each_ node.
                        ThrowOnUndefinedPropertyAttribute(p, member);

                        PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p.MappedUri).Object(o.Name));

                        return o;
                    }
                    else if (member.IsUriType())
                    {
                        // We access the .Uri member of a resource. We do not need a property mapping and return the subject.

                        // Make the variable known for building filters on it later.
                        VariableGenerator.RegisterExpression(memberExpression.Expression, s);

                        return s;
                    }

                    if(buildMemberAccessTriple != null)
                    {
                        // In all other cases we require a property mapping.
                        ThrowOnUndefinedPropertyAttribute(p, member);

                        // Invoke the final user-handled member access triple builder callback.
                        buildMemberAccessTriple(s, p.MappedUri, o);
                    }

                    return o;
                }
            }
            else
            {
                return VariableGenerator.GetVariable(expression);
            }
        }

        private void ThrowOnUndefinedPropertyAttribute(RdfPropertyAttribute p, MemberInfo member)
        {
            if(p == null)
            {
                string msg = string.Format("Property mapping not found for member: {0}", member.Name);
                throw new Exception(msg);
            }
        }

        protected void BuildFilterOnSystemType(MemberExpression expression, Func<NumericExpression, BooleanExpression> buildFilter)
        {
            SparqlVariable o = VariableGenerator.GetVariable(expression.Expression);

            MemberInfo member = expression.Member;

            if (member.DeclaringType == typeof(String))
            {
                switch (member.Name)
                {
                    case "Length":
                        PatternBuilder.Filter(e => buildFilter(e.StrLen(e.Variable(o.Name))));
                        break;
                    default:
                        throw new NotSupportedException(expression.ToString());
                }
            }
            else if (member.DeclaringType == typeof(DateTime))
            {
                throw new NotImplementedException(member.DeclaringType.ToString());
            }
        }

        public virtual void SetObjectOperator(ResultOperatorBase resultOperator)
        {
        }

        public void SetObjectVariable(SparqlVariable variable, bool select = false)
        {
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
            if (variable != null)
            {
                if (select)
                {
                    DeselectVariable(SubjectVariable);
                    SelectVariable(variable);
                }

                SubjectVariable = variable;
            }
        }

        public virtual void SetSubjectOperator(ResultOperatorBase resultOperator)
        {
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

        public void SelectVariable(string variable)
        {
            if (SelectBuilder != null)
            {
                if (!string.IsNullOrEmpty(variable) && !SelectedVariables.Any(v => v.Name == variable))
                {
                    SelectedVariables.Add(new SparqlVariable(variable, true));
                }
            }
            else
            {
                string msg = "Cannot select variables with non-SELECT query type.";
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

        public bool IsSelectedVariable(SparqlVariable variable)
        {
            return variable != null && SelectBuilder != null && SelectedVariables.Contains(variable);
        }

        public void Where(MemberExpression member, SparqlVariable variable)
        {
            RdfPropertyAttribute attribute = member.Member.TryGetCustomAttribute<RdfPropertyAttribute>();

            PatternBuilder.Where(t => t.Subject(SubjectVariable.Name).PredicateUri(attribute.MappedUri).Object(variable.Name));

            VariableGenerator.RegisterExpression(member, variable);
        }

        public void WhereEqual(SparqlVariable variable, ConstantExpression constant)
        {
            PatternBuilder.Filter(e => e.Variable(variable.Name) == constant.AsLiteralExpression());
        }

        public void WhereEqual(MemberExpression expression, ConstantExpression constant)
        {
            if(constant.Value != null)
            {
                SparqlVariable so = BuildMemberAccess(expression, constant.AsNode());

                if (expression.Member.IsSystemType())
                {
                    BuildFilterOnSystemType(expression, e => e == constant.AsNumericExpression());
                }
                else if (expression.Member.IsUriType())
                {
                    PatternBuilder.Filter(e => e.Variable(so.Name) == constant.AsIriExpression());
                }
            }
            else
            {
                SparqlVariable o = VariableGenerator.GetObjectVariable();

                // If we want to filter for non-bound values we need to mark the properties as optional.
                BuildMemberAccessOptional(expression, o);

                // Comparing with null means the variable is not bound.
                PatternBuilder.Filter(e => !e.Bound(o.Name));
            }
        }

        public void WhereNotEqual(SparqlVariable variable, ConstantExpression constant)
        {
            PatternBuilder.Filter(e => e.Variable(variable.Name) != new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereNotEqual(MemberExpression expression, ConstantExpression constant)
        {
            // TODO: We do not need this object variable in any cases. Let BuildMemberAccess return the variable it generated or referenced.
            SparqlVariable o = VariableGenerator.GetObjectVariable();

            if (expression.Member.IsSystemType())
            {
                // Literal value types do not have an undefined state 'null' value.
                // Therefore, the member access must not be optional.
                BuildMemberAccess(expression, o);

                BuildFilterOnSystemType(expression, e => e != constant.AsNumericExpression());
            }
            else if (expression.Member.IsUriType())
            {
                if(expression.Expression is QuerySourceReferenceExpression)
                {
                    // Here we compare the URI property of the subject.
                    SparqlVariable s = BuildMemberAccess(expression, o);

                    PatternBuilder.Filter(e => e.Variable(s.Name) != constant.AsIriExpression());
                }
                else
                {
                    // If we compare URIs of query source members, then we compare reference values. 
                    // In this case we also need to include resources in the result where the member
                    // value is 'null'. Therefore, we make the member access optional.
                    SparqlVariable so = BuildMemberAccessOptional(expression, o);

                    PatternBuilder.Filter(e => e.Bound(so.Name) && e.Variable(so.Name) != constant.AsIriExpression() || !e.Bound(so.Name));
                }
            }
            else if (constant.Value == null)
            {
                // The member access must not be optional.
                BuildMemberAccess(expression, o);

                // The value must be bound.
                PatternBuilder.Filter(e => e.Bound(o.Name));
            }
            else
            {
                BuildMemberAccess(expression, o);

                PatternBuilder.Filter(e => e.Variable(o.Name) != constant.AsNumericExpression());
            }
        }

        public void WhereGreaterThan(SparqlVariable variable, ConstantExpression constant)
        {
            PatternBuilder.Filter(e => e.Variable(variable.Name) > constant.AsNumericExpression());
        }

        public void WhereGreaterThan(MemberExpression expression, ConstantExpression constant)
        {
            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(expression, o);

            if (expression.Member.IsSystemType())
            {
                BuildFilterOnSystemType(expression, e => e > constant.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) > constant.AsNumericExpression());
            }
        }

        public void WhereGreaterThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            PatternBuilder.Filter(e => e.Variable(variable.Name) >= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereGreaterThanOrEqual(MemberExpression expression, ConstantExpression constant)
        {
            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(expression, o);

            if (expression.Member.IsSystemType())
            {
                BuildFilterOnSystemType(expression, e => e >= constant.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) >= constant.AsNumericExpression());
            }
        }

        public void WhereLessThan(SparqlVariable variable, ConstantExpression constant)
        {
            PatternBuilder.Filter(e => e.Variable(variable.Name) < new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThan(MemberExpression expression, ConstantExpression constant)
        {
            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(expression, o);

            if (expression.Member.IsSystemType())
            {
                BuildFilterOnSystemType(expression, e => e < constant.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) < constant.AsNumericExpression());
            }
        }

        public void WhereLessThanOrEqual(SparqlVariable variable, ConstantExpression constant)
        {
            PatternBuilder.Filter(e => e.Variable(variable.Name) <= new LiteralExpression(constant.AsSparqlExpression()));
        }

        public void WhereLessThanOrEqual(MemberExpression expression, ConstantExpression constant)
        {
            SparqlVariable o = VariableGenerator.GetObjectVariable();

            BuildMemberAccess(expression, o);

            if (expression.Member.IsSystemType())
            {
                BuildFilterOnSystemType(expression, e => e <= constant.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) <= constant.AsNumericExpression());
            }
        }

        public void FilterRegex(SparqlVariable variable, string pattern, bool ignoreCase)
        {
            if (ignoreCase)
            {
                PatternBuilder.Filter(e => e.Regex(e.Variable(variable.Name), pattern, "i"));
            }
            else
            {
                PatternBuilder.Filter(e => e.Regex(e.Variable(variable.Name), pattern));
            }
        }

        public void FilterRegex(MemberExpression expression, string pattern, bool ignoreCase)
        {
            SparqlVariable s = VariableGenerator.GetVariable(expression);
            SparqlVariable o = BuildMemberAccess(expression);

            FilterRegex(o, pattern, ignoreCase);
        }

        public void WhereResource(SparqlVariable subject)
        {
            SparqlVariable p = VariableGenerator.GetPredicateVariable();
            SparqlVariable o = VariableGenerator.GetObjectVariable();

            PatternBuilder.Where(t => t.Subject(subject.Name).Predicate(p.Name).Object(o.Name));
        }

        public void WhereResourceOfType(SparqlVariable subject, Type type)
        {
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

        public IGraphPatternBuilder Child(ISparqlQueryGenerator queryGenerator)
        {
            queryGenerator.BindSelectVariables();

            var subquery = queryGenerator.GetQueryBuilder().BuildQuery();

            var childBuilder = new GraphPatternBuilder();
            childBuilder.Where(new SubQueryPattern(subquery));

            // Note: This sets the enclosing pattern builder as the current pattern
            // builder in order to build subsequent FILTERs on the selected variables
            // into the enclosing block, rather than the parent query. This is because
            // for OpenLink Virtuoso the FILTERs need to be inside the enclosing group
            // of the subquery..
            queryGenerator.SetPatternBuilder(childBuilder);

            return PatternBuilder.Child(childBuilder);
        }

        public IGraphPatternBuilder Child(GraphPatternBuilder patternBuilder)
        {
            return PatternBuilder.Child(patternBuilder);
        }

        public IQueryBuilder GetQueryBuilder()
        {
            return QueryBuilder;
        }

        public IGraphPatternBuilder GetPatternBuilder()
        {
            return PatternBuilder;
        }

        public IGraphPatternBuilder GetRootPatternBuilder()
        {
            return QueryBuilder.RootGraphPatternBuilder;
        }

        public void SetPatternBuilder(IGraphPatternBuilder patternBuilder)
        {
            PatternBuilder = patternBuilder;
        }

        public void SetQueryContext(QueryModel queryModel, ISparqlQueryGeneratorTree generatorTree, SparqlVariableGenerator variableGenerator)
        {
            QueryModel = queryModel;
            QueryGeneratorTree = generatorTree;
            VariableGenerator = variableGenerator;
        }

        public virtual void OnBeforeFromClauseVisited(Expression expression)
        {
            // This is a workaround for a bug in OpenLink Virtuoso where it throws an exception
            // when it receives a SPARQL query with a OFFSET but not LIMIT clause.
            if (HasResultOperator<SkipResultOperator>() && !HasResultOperator<TakeResultOperator>())
            {
                SkipResultOperator op = QueryModel.ResultOperators.OfType<SkipResultOperator>().First();

                if (int.Parse(op.Count.ToString()) > 0)
                {
                    QueryModel.ResultOperators.Insert(0, new TakeResultOperator(Expression.Constant(int.MaxValue)));
                }
            }
        }

        public virtual void OnFromClauseVisited(Expression expression)
        { }

        public virtual void OnBeforeSelectClauseVisited(Expression selector)
        {
        }

        public virtual void OnSelectClauseVisited(Expression selector)
        {
        }

        #endregion
    }
}
