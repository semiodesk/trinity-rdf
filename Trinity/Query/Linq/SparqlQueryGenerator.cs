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
// Copyright (c) Semiodesk GmbH 2015-2019

using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Patterns;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryGenerator : ISparqlQueryGenerator
    {
        #region Members

        public bool IsRoot { get; protected set; }

        public bool IsBound { get; private set; }

        public QueryModel QueryModel { get; private set; }

        public SparqlVariable SubjectVariable { get; private set; }

        public SparqlVariable ObjectVariable { get; private set; }

        public IList<SparqlVariable> SelectedVariables { get; private set; }

        protected Dictionary<SparqlVariable, SparqlExpression> CoalescedVariables { get; private set; }

        public ISparqlVariableGenerator VariableGenerator { get; protected set; }

        protected ISparqlQueryGeneratorTree QueryGeneratorTree;

        public IQueryBuilder QueryBuilder { get; set; }

        protected ISelectBuilder SelectBuilder;

        public IGraphPatternBuilder PatternBuilder { get; set; }

        public ISparqlQueryGenerator ParentGenerator { get; set; }

        #endregion

        #region Constructors

        public SparqlQueryGenerator(IQueryBuilder queryBuilder)
        {
            SelectedVariables = new List<SparqlVariable>();
            CoalescedVariables = new Dictionary<SparqlVariable, SparqlExpression>();
            QueryBuilder = queryBuilder;
#if NET35
            PatternBuilder = QueryBuilder.RootGraphPatternBuilder;
#else
            PatternBuilder = QueryBuilder.Root;
#endif
        }

        public SparqlQueryGenerator(ISelectBuilder selectBuilder)
        {
            SelectedVariables = new List<SparqlVariable>();
            CoalescedVariables = new Dictionary<SparqlVariable, SparqlExpression>();
            SelectBuilder = selectBuilder;
#if NET35
            QueryBuilder = selectBuilder.GetQueryBuilder();
            PatternBuilder = QueryBuilder.RootGraphPatternBuilder;
#else
            QueryBuilder = SelectBuilder;
            PatternBuilder = QueryBuilder.Root;
#endif
        }

#endregion

        #region Methods

        public string BuildQuery()
        {
            if (!IsBound)
            {
                BindSelectVariables();
            }

            var query = QueryBuilder.BuildQuery();

            return query.ToString();
        }

        public void BindSelectVariables()
        {
            IsBound = true;

            if (SelectBuilder != null)
            {
                bool hasAggregate = SelectedVariables.Any(v => v.IsAggregate);

                foreach (SparqlVariable v in SelectedVariables)
                {
                    if (CoalescedVariables.ContainsKey(v))
                    {
                        SparqlExpression defaultValue = CoalescedVariables[v];

                        SelectBuilder.And(e => e.Coalesce(e.Variable(v.Name), defaultValue)).As(v.Name + '_');
                    }
                    else
                    {
                        SelectBuilder.And(v);
                    }

                    if (hasAggregate && !v.IsAggregate)
                    {
#if !NET35
                        SelectBuilder.GroupBy(v.Name);
#else
                        QueryBuilder.GroupBy(v.Name);
#endif
                    }
                }

                if (hasAggregate && !IsRoot)
                {
#if !NET35
                    SelectBuilder.Distinct();
#else
                        QueryBuilder.Distinct();
#endif
                }
            }
        }

        /// <summary>
        /// Builds the triples required to access a given member and accociates its value with a variable.
        /// </summary>
        /// <param name="memberExpression">The member to be accessed.</param>
        /// <returns>The object variable associated with the member value.</returns>
        protected SparqlVariable BuildMemberAccess(MemberExpression memberExpression)
        {
            var requiredBuilder = PatternBuilder;

            var so = BuildMemberAccess(memberExpression, requiredBuilder);

            return so;
        }

        /// <summary>
        /// Builds the triples required to access a given member and accociates its value with a variable.
        /// </summary>
        /// <param name="memberExpression">The member to be accessed.</param>
        /// <returns>The object variable associated with the member value.</returns>
        protected SparqlVariable BuildMemberAccessOptional(MemberExpression memberExpression)
        {
            var optionalBuilder = new GraphPatternBuilder(GraphPatternType.Optional);

            var so = BuildMemberAccess(memberExpression, optionalBuilder);

            Child(optionalBuilder);

            return so;
        }

        private SparqlVariable BuildMemberAccess(MemberExpression memberExpression, IGraphPatternBuilder patternBuilder)
        {
            MemberInfo member = memberExpression.Member;

            // If we do access a member of a system type, like string.Length we actually select the
            // the declaring member and invoke a SPARQL built in call to get the value.
            if (member.IsBuiltInCall())
            {
                MemberExpression parentMember = memberExpression.Expression as MemberExpression;

                return BuildMemberAccess(parentMember, patternBuilder);
            }
            else if (memberExpression.Expression is MemberExpression)
            {
                MemberExpression parentMember = memberExpression.Expression as MemberExpression;

                // Note: When we build an optional property path, we consider the relation to the 
                // parent properties of the accessed property to be non-optional.
                IGraphPatternBuilder builder = member.IsUriType() ? patternBuilder : PatternBuilder;

                // We might encounter property paths (i.e. contact.Organization.Name). Therefore,
                // implement the parent expression of the current member recursively..
                SparqlVariable po = BuildMemberAccess(parentMember, builder);

                // If we are building a node on a property path (parentExpression != null), we associate 
                // the object variable with the parent expression so that it becomes the subject of the parent.
                VariableGenerator.SetSubjectVariable(memberExpression, po);
            }

            if (member.IsUriType())
            {
                // When we access the .Uri member of a resource we do not need a property mapping and return the subject as the bound variable.

                // We create a triple pattern describing the resource in the local scope just in case it has not been described yet.
                // Todo: Improve. Check if triples actually need to be asserted.
                SparqlVariable s = VariableGenerator.TryGetSubjectVariable(memberExpression) ?? SubjectVariable;
                SparqlVariable p = VariableGenerator.CreatePredicateVariable();
                SparqlVariable o = VariableGenerator.CreateObjectVariable(memberExpression);

                patternBuilder.Where(t => t.Subject(s).Predicate(p).Object(o));

                VariableGenerator.SetSubjectVariable(memberExpression, s);

                return s;
            }
            else if (memberExpression.Expression is QuerySourceReferenceExpression)
            {
                QuerySourceReferenceExpression querySource = memberExpression.Expression as QuerySourceReferenceExpression;

                if (VariableGenerator.TryGetSubjectVariable(memberExpression) == VariableGenerator.GlobalSubject)
                {
                    // In case the accessed member is the global query subject (i.e. from x select x.Y)..
                    SparqlVariable s = VariableGenerator.TryGetSubjectVariable(querySource);
                    SparqlVariable o = VariableGenerator.GlobalSubject;

                    if (s == null)
                    {
                        s = VariableGenerator.CreateSubjectVariable(querySource);

                        BuildMemberAccess(memberExpression, patternBuilder, member, s, o);
                    }

                    return o;
                }
                else
                {
                    // Otherwise we are accessing a member of the globale query subject (i.e. from x where x.Y select x)
                    SparqlVariable s = VariableGenerator.TryGetSubjectVariable(querySource) ?? VariableGenerator.GlobalSubject;
                    SparqlVariable o = VariableGenerator.TryGetObjectVariable(memberExpression) ?? VariableGenerator.CreateObjectVariable(memberExpression);

                    BuildMemberAccess(memberExpression, patternBuilder, member, s, o);

                    return o;
                }
            }
            else
            {
                SparqlVariable s = VariableGenerator.TryGetSubjectVariable(memberExpression) ?? VariableGenerator.CreateSubjectVariable(memberExpression);
                SparqlVariable o = VariableGenerator.TryGetObjectVariable(memberExpression) ?? VariableGenerator.CreateObjectVariable(memberExpression);

                BuildMemberAccess(memberExpression, patternBuilder, member, s, o);

                return o;
            }
        }

        private void BuildMemberAccess(MemberExpression memberExpression, IGraphPatternBuilder patternBuilder, MemberInfo member, SparqlVariable s, SparqlVariable o)
        {
            RdfPropertyAttribute p = memberExpression.TryGetRdfPropertyAttribute();

            if (p == null)
            {
                throw new Exception(string.Format("No RdfPropertyAttribute found for member: {0}", member.Name));
            }

            // Invoke the final user-handled member access triple builder callback.
            patternBuilder.Where(t => t.Subject(s).PredicateUri(p.MappedUri).Object(o));
        }

        protected void BuildBuiltInCall(MemberExpression memberExpression, Func<NumericExpression, BooleanExpression> buildFilter)
        {
            SparqlVariable o = VariableGenerator.TryGetObjectVariable(memberExpression.Expression) ?? ObjectVariable;

            MemberInfo member = memberExpression.Member;

            if (member.DeclaringType == typeof(String))
            {
                switch (member.Name)
                {
                    case "Length":
                        PatternBuilder.Filter(e => buildFilter(e.StrLen(e.Variable(o.Name))));
                        break;
                    default:
                        throw new NotSupportedException(memberExpression.ToString());
                }
            }
            else if (member.DeclaringType == typeof(DateTime))
            {
                // TODO: YEAR, MONTH, DAY, HOURS, MINUTES, SECONDS, TIMEZONE, TZ
                throw new NotImplementedException(member.DeclaringType.ToString());
            }
        }

        public virtual void SetObjectOperator(ResultOperatorBase resultOperator) { }

        public void SetObjectVariable(SparqlVariable v, bool select = false)
        {
            if (v == null) return;

            // If the new variable is to be selected, deselect the previous variable first.
            if (select)
            {
                DeselectVariable(ObjectVariable);
                SelectVariable(v);
            }

            ObjectVariable = v;
        }

        public virtual void SetSubjectOperator(ResultOperatorBase resultOperator) { }

        public void SetSubjectVariable(SparqlVariable v, bool select = false)
        {
            if (v == null) return;

            // If the new variable is to be selected, deselect the previous variable first.
            if (select)
            {
                DeselectVariable(SubjectVariable);
                SelectVariable(v);
            }

            SubjectVariable = v;
        }

        public void DeselectVariable(SparqlVariable v)
        {
            if (SelectBuilder != null)
            {
                if (v != null && SelectedVariables.Contains(v))
                {
                    SelectedVariables.Remove(v);
                }
            }
            else
            {
                string msg = "Cannot deselect variables with non-SELECT query type.";
                throw new Exception(msg);
            }
        }

        public void SelectVariable(SparqlVariable v)
        {
            if (SelectBuilder != null)
            {
                if (v != null && !SelectedVariables.Any(x => x.Name == v.Name))
                {
                    SelectedVariables.Add(v);
                }
            }
            else
            {
                string msg = "Cannot select variables with non-SELECT query type.";
                throw new Exception(msg);
            }
        }

        public bool IsSelectedVariable(SparqlVariable v)
        {
            return v != null && SelectBuilder != null && SelectedVariables.Contains(v);
        }

        public void Where(MemberExpression member, SparqlVariable v)
        {
            if (CoalescedVariables.ContainsKey(v))
            {
                // If the member may be unbound, we create an optional binding.
                BuildMemberAccessOptional(member);
            }
            else
            {
                // Otherwise we create a normal binding.
                BuildMemberAccess(member);
            }
        }

        public void WhereEqual(SparqlVariable v, ConstantExpression c)
        {
            if (c.Value == null)
            {
                PatternBuilder.Filter(e => !e.Bound(v.Name));
            }
            else if (c.Type.IsValueType || c.Type == typeof(string))
            {
                PatternBuilder.Filter(e => e.Variable(v.Name) == c.AsLiteralExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(v.Name) == c.AsIriExpression());
            }
        }

        public void WhereEqual(MemberExpression expression, ConstantExpression c)
        {
            if (c.Value == null)
            {
                // If we want to filter for non-bound values we need to mark the properties as optional.
                SparqlVariable so = BuildMemberAccessOptional(expression);

                // TODO: If we filter a resource, make sure it has been described with variables in the local scope.

                // Comparing with null means the variable is not bound.
                PatternBuilder.Filter(e => !e.Bound(so.Name));
            }
            else if (c.Type.IsValueType || c.Type == typeof(string))
            {
                if (expression.Member.DeclaringType == typeof(string))
                {
                    BuildMemberAccess(expression);

                    // If we are comparing a property of string we need to implement SPARQL built-in call on the variable such as STRLEN..
                    BuildBuiltInCall(expression, e => e == c.AsNumericExpression());
                }
                else
                {
                    // TODO: The default value for a property may be overridden with the DefaultValue attribute.
                    object defaultValue = TypeHelper.GetDefaultValue(c.Type);

                    // If the value IS the default value, WhereEquals includes the default value and therefore includes non-bound values..
                    if (c.Value.Equals(defaultValue))
                    {
                        // If we want to filter for non-bound values we need to mark the properties as optional.
                        SparqlVariable o = BuildMemberAccessOptional(expression);

                        // Mark the variable to be coalesced with the default value when selected.
                        CoalescedVariables[o] = Expression.Constant(defaultValue).AsLiteralExpression();

                        // Comparing with null means the variable is not bound.
                        PatternBuilder.Filter(e => e.Variable(o.Name) == c.AsLiteralExpression() || !e.Bound(o.Name));
                    }
                    else
                    {
                        // If we want to filter bound literal values, we still write them into a variable so they can be selected.
                        SparqlVariable o = BuildMemberAccess(expression);

                        PatternBuilder.Filter(e => e.Variable(o.Name) == c.AsLiteralExpression());
                    }
                }
            }
            else
            {
                // We are comparing reference types / resources against a bound value here.
                SparqlVariable so = BuildMemberAccess(expression);

                // TODO: If we filter a resource, make sure it has been described with variables in the local scope.

                PatternBuilder.Filter(e => e.Variable(so.Name) == c.AsIriExpression());
            }
        }

        public void WhereNotEqual(SparqlVariable v, ConstantExpression c)
        {
            if (c.Value == null)
            {
                PatternBuilder.Filter(e => e.Bound(v.Name));
            }
            else if (c.Type.IsValueType || c.Type == typeof(string))
            {
                PatternBuilder.Filter(e => e.Variable(v.Name) != c.AsLiteralExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(v.Name) != c.AsIriExpression());
            }
        }

        public void WhereNotEqual(MemberExpression expression, ConstantExpression c)
        {
            if (c.Value == null)
            {
                // If we want to filter for non-bound values we need to mark the properties as optional.
                SparqlVariable o = BuildMemberAccessOptional(expression);

                // Comparing with null means the variable is not bound.
                PatternBuilder.Filter(e => e.Bound(o.Name));
            }
            else if (c.Type.IsValueType || c.Type == typeof(string))
            {
                if (expression.Member.DeclaringType == typeof(string))
                {
                    BuildMemberAccess(expression);

                    // If we are comparing a property of string we need to implement SPARQL built-in call on the variable such as STRLEN..
                    BuildBuiltInCall(expression, e => e != c.AsNumericExpression());
                }
                else
                {
                    // TODO: The default value for a property may be overridden with the DefaultValue attribute.
                    object defaultValue = TypeHelper.GetDefaultValue(c.Type);

                    // If the value is NOT the default value, WhereNotEquals includes the default value and therefore includes non-bound values..
                    if (!c.Value.Equals(defaultValue))
                    {
                        // If we want to filter for non-bound values we need to mark the properties as optional.
                        SparqlVariable o = BuildMemberAccessOptional(expression);

                        // Mark the variable to be coalesced with the default value when selected.
                        CoalescedVariables[o] = Expression.Constant(defaultValue).AsLiteralExpression();

                        // Comparing with null means the variable is not bound.
                        PatternBuilder.Filter(e => e.Variable(o.Name) != c.AsLiteralExpression() || !e.Bound(o.Name));
                    }
                    else
                    {
                        // If we want to filter bound literal values, we still write them into a variable so they can be selected.
                        SparqlVariable o = BuildMemberAccess(expression);

                        PatternBuilder.Filter(e => e.Variable(o.Name) != c.AsLiteralExpression());
                    }
                }
            }
            else
            {
                // We are comparing reference types /resource against a bound value here.
                // Note: If the compared values must not be equal, then the comapred value might also be not bound (optional).
                SparqlVariable o = BuildMemberAccessOptional(expression);

                // Unbound variables are explicitly included in the result.
                PatternBuilder.Filter(e => e.Variable(o.Name) != c.AsIriExpression() || !e.Bound(o.Name));
            }
        }

        public void WhereGreaterThan(SparqlVariable v, ConstantExpression c)
        {
            PatternBuilder.Filter(e => e.Variable(v.Name) > c.AsNumericExpression());
        }

        public void WhereGreaterThan(MemberExpression expression, ConstantExpression c)
        {
            SparqlVariable o = BuildMemberAccess(expression);

            if (expression.Member.IsBuiltInCall())
            {
                BuildBuiltInCall(expression, e => e > c.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) > c.AsNumericExpression());
            }
        }

        public void WhereGreaterThanOrEqual(SparqlVariable v, ConstantExpression c)
        {
            PatternBuilder.Filter(e => e.Variable(v.Name) >= new LiteralExpression(c.AsSparqlExpression()));
        }

        public void WhereGreaterThanOrEqual(MemberExpression expression, ConstantExpression c)
        {
            SparqlVariable o = BuildMemberAccess(expression);

            if (expression.Member.IsBuiltInCall())
            {
                BuildBuiltInCall(expression, e => e >= c.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) >= c.AsNumericExpression());
            }
        }

        public void WhereLessThan(SparqlVariable v, ConstantExpression c)
        {
            PatternBuilder.Filter(e => e.Variable(v.Name) < new LiteralExpression(c.AsSparqlExpression()));
        }

        public void WhereLessThan(MemberExpression expression, ConstantExpression c)
        {
            SparqlVariable o = BuildMemberAccess(expression);

            if (expression.Member.IsBuiltInCall())
            {
                BuildBuiltInCall(expression, e => e < c.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) < c.AsNumericExpression());
            }
        }

        public void WhereLessThanOrEqual(SparqlVariable v, ConstantExpression c)
        {
            PatternBuilder.Filter(e => e.Variable(v.Name) <= new LiteralExpression(c.AsSparqlExpression()));
        }

        public void WhereLessThanOrEqual(MemberExpression expression, ConstantExpression c)
        {
            SparqlVariable o = BuildMemberAccess(expression);

            if (expression.Member.IsBuiltInCall())
            {
                BuildBuiltInCall(expression, e => e <= c.AsNumericExpression());
            }
            else
            {
                PatternBuilder.Filter(e => e.Variable(o.Name) <= c.AsNumericExpression());
            }
        }

        public void FilterRegex(SparqlVariable v, string pattern, bool ignoreCase)
        {
            if (ignoreCase)
            {
                PatternBuilder.Filter(e => e.Regex(e.Variable(v.Name), pattern, "i"));
            }
            else
            {
                PatternBuilder.Filter(e => e.Regex(e.Variable(v.Name), pattern));
            }
        }

        public void FilterRegex(MemberExpression expression, string pattern, bool ignoreCase)
        {
            SparqlVariable s = VariableGenerator.TryGetSubjectVariable(expression) ?? SubjectVariable;
            SparqlVariable o = VariableGenerator.TryGetObjectVariable(expression) ?? VariableGenerator.CreateObjectVariable(expression);

            BuildMemberAccess(expression);

            FilterRegex(o, pattern, ignoreCase);
        }

        public void FilterNotRegex(SparqlVariable v, string pattern, bool ignoreCase)
        {
            if (ignoreCase)
            {
                PatternBuilder.Filter(e => !e.Regex(e.Variable(v.Name), pattern, "i"));
            }
            else
            {
                PatternBuilder.Filter(e => !e.Regex(e.Variable(v.Name), pattern));
            }
        }

        public void FilterNotRegex(MemberExpression expression, string pattern, bool ignoreCase)
        {
            SparqlVariable s = VariableGenerator.TryGetSubjectVariable(expression) ?? SubjectVariable;
            SparqlVariable o = VariableGenerator.TryGetObjectVariable(expression) ?? VariableGenerator.CreateObjectVariable(expression);

            BuildMemberAccess(expression);

            FilterNotRegex(o, pattern, ignoreCase);
        }

        public void WhereResource(SparqlVariable s, SparqlVariable p, SparqlVariable o)
        {
            PatternBuilder.Where(t => t.Subject(s).Predicate(p).Object(o));
        }

        public void WhereResource(Expression expression, SparqlVariable p, SparqlVariable o)
        {
            SparqlVariable s = VariableGenerator.TryGetSubjectVariable(expression) ?? SubjectVariable;

            PatternBuilder.Where(t => t.Subject(s).Predicate(p).Object(o));
        }

        public void WhereResourceOfType(Expression expression, Type type)
        {
            SparqlVariable so = VariableGenerator.TryGetSubjectVariable(expression) ?? VariableGenerator.TryGetObjectVariable(expression);

            WhereResourceOfType(so, type);
        }

        public void WhereResourceOfType(SparqlVariable s, Type type)
        {
            RdfClassAttribute t = type.TryGetCustomAttribute<RdfClassAttribute>();

            if (t != null)
            {
                Uri a = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

                PatternBuilder.Where(e => e.Subject(s).PredicateUri(a).Object(t.MappedUri));
            }
        }

        public void WhereResourceNotOfType(Expression expression, Type type)
        {
            SparqlVariable so = VariableGenerator.TryGetSubjectVariable(expression) ?? VariableGenerator.TryGetObjectVariable(expression);

            WhereResourceNotOfType(so, type);
        }

        public void WhereResourceNotOfType(SparqlVariable s, Type type)
        {
            RdfClassAttribute t = type.TryGetCustomAttribute<RdfClassAttribute>();

            if (t != null)
            {
                Uri a = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

                SparqlVariable o = VariableGenerator.CreateObjectVariable();

                ConstantExpression c = Expression.Constant(t.MappedUri);

                PatternBuilder.Where(e => e.Subject(s).PredicateUri(a).Object(o));
                PatternBuilder.Filter(e => e.Variable(o.Name) != c.AsIriExpression() || !e.Bound(o.Name));
            }
        }

        public void OrderBy(SparqlVariable v)
        {
            QueryBuilder.OrderBy(v.Name);
        }

        public void OrderByDescending(SparqlVariable v)
        {
            QueryBuilder.OrderByDescending(v.Name);
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

        public IGraphPatternBuilder Child(ISparqlQueryGenerator generator)
        {
            generator.BindSelectVariables();

            var subQuery = generator.QueryBuilder.BuildQuery();

            var childBuilder = new GraphPatternBuilder();
            childBuilder.Where(new SubQueryPattern(subQuery));

            // Note: This sets the enclosing pattern builder as the current pattern
            // builder in order to build subsequent FILTERs on the selected variables
            // into the enclosing block, rather than the parent query. This is because
            // for OpenLink Virtuoso the FILTERs need to be inside the enclosing group
            // of the subquery..
            generator.PatternBuilder = childBuilder;

            return PatternBuilder.Child(childBuilder);
        }

        public IGraphPatternBuilder Child(GraphPatternBuilder patternBuilder)
        {
            return PatternBuilder.Child(patternBuilder);
        }

        public void SetQueryContext(ISparqlQueryGeneratorTree generatorTree, QueryModel queryModel)
        {
            QueryModel = queryModel;
            QueryGeneratorTree = generatorTree;
        }

        public virtual void OnBeforeFromClauseVisited(Expression expression)
        {
            // This is a workaround for a bug in OpenLink Virtuoso where it throws an exception
            // when it receives a SPARQL query with a OFFSET but not LIMIT clause.
            if (QueryModel.HasResultOperator<SkipResultOperator>() && !QueryModel.HasResultOperator<TakeResultOperator>())
            {
                SkipResultOperator op = QueryModel.ResultOperators.OfType<SkipResultOperator>().First();

                int skipCount = int.Parse(op.Count.ToString());

                if (skipCount > 0)
                {
                    // OpenLink Virtuoso does not support returning more than 10000 results in an ordered query.
                    int limit = QueryModel.HasOrdering() ? 10000 - skipCount : int.MaxValue;

                    QueryModel.ResultOperators.Insert(0, new TakeResultOperator(Expression.Constant(limit)));
                }
            }
        }

        public virtual void OnFromClauseVisited(Expression expression)
        {
        }

        public virtual void OnBeforeSelectClauseVisited(Expression selector)
        {
        }

        public virtual void OnSelectClauseVisited(Expression selector)
        {
        }

        #endregion
    }
}
