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

using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using System;
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    /// <summary>
    /// Generates SELECT queries which return binding sets.
    /// </summary>
    /// <remarks>
    /// This class is intended to be used as a root query generator. For generating SELECT queries
    /// for sub-queries, refer to <c>SubSelectQueryGenerator</c>.
    /// </remarks>
    internal class SelectBindingsQueryGenerator : SelectQueryGenerator
    {
        #region Constructors

        public SelectBindingsQueryGenerator()
        {
            IsRoot = true;
            VariableGenerator = new SparqlVariableGenerator(null);
        }

        #endregion

        #region Methods

        public override void OnBeforeFromClauseVisited(Expression expression)
        {
            base.OnBeforeFromClauseVisited(expression);

            // TODO: Move into OnBeforeSelectClauseVisited.
            if (expression is ConstantExpression)
            {
                ConstantExpression constantExpression = expression as ConstantExpression;

                IQueryable queryable = constantExpression.Value as IQueryable;

                if (queryable != null && typeof(Resource).IsAssignableFrom(queryable.ElementType))
                {
                    SparqlVariable s = VariableGenerator.GlobalSubject;

                    SetSubjectVariable(s);

                    VariableGenerator.SetSubjectVariable(expression, s);
                }
                else
                {
                    throw new NotSupportedException(constantExpression.Value.GetType().ToString());
                }
            }
            else
            {
                // TODO: Create unit test for QuerySourceReferenceExpression, SubQueryExpression
                throw new NotImplementedException(expression.GetType().ToString());
            }
        }

        public override void OnBeforeSelectClauseVisited(Expression selector)
        {
            base.OnBeforeSelectClauseVisited(selector);

            QuerySourceReferenceExpression sourceExpression = selector.TryGetQuerySourceReference();

            if (sourceExpression != null)
            {
                // Register the query source with the global variable for sub-queries.
                SparqlVariable s = VariableGenerator.TryGetSubjectVariable(sourceExpression) ?? VariableGenerator.GlobalSubject;

                // Assert the object type.
                if (sourceExpression.Type.IsSubclassOf(typeof(Resource)))
                {
                    WhereResourceOfType(s, sourceExpression.Type);
                }

                if (selector is MemberExpression)
                {
                    MemberExpression memberExpression = selector as MemberExpression;

                    SparqlVariable o = VariableGenerator.CreateObjectVariable(memberExpression);

                    // Select all triples having the resource as subject.
                    SetSubjectVariable(s);
                    SetObjectVariable(o, true);

                    // If the member expression is not selected in the WHERE block, we add it here.
                    // Scenarios:
                    // - from x in Model.AsQueryable<X>() select x.B
                    // - from x in Model.AsQueryable<X>() where x.A select x.B
                    string e = memberExpression.ToString();

                    if (!QueryModel.BodyClauses.OfType<WhereClause>().Any(c => c.Predicate.ToString().Contains(e)))
                    {
                        // We select the member without a constraint on its value.
                        QueryModel.BodyClauses.Add(new WhereClause(memberExpression));

                        // Since there is no constraint on the member, we also need to select the ones that are not bound.
                        Type memberType = memberExpression.Member.GetMemberType();

                        // TODO: There might be a different default value on the member using the DefaultValue() attribute.
                        object defaultValue = TypeHelper.GetDefaultValue(memberType);

                        if(defaultValue != null && memberType != typeof(string))
                        {
                            ConstantExpression coalescedValue = Expression.Constant(defaultValue);

                            // Mark the variable to be coalesced with the default value when selected.
                            CoalescedVariables[o] = coalescedValue.AsLiteralExpression();
                        }
                    }
                }
                else if(QueryModel.HasNumericResultOperator())
                {
                    // If we have a numeric result operator on the root query, make the
                    // subject variable known so that the model visitor can handle it.
                    SetSubjectVariable(s);
                }
            }
        }

        public override void OnSelectClauseVisited(Expression selector)
        {
            base.OnSelectClauseVisited(selector);

            // If we are in the root query generator and have not yet selected the
            // subject variable, set it from the given selector.
            if(IsRoot && !SelectedVariables.Any())
            {
                SparqlVariable o = VariableGenerator.TryGetObjectVariable(selector);

                if (o != null && !IsSelectedVariable(o))
                {
                    SelectVariable(o);
                }
            }
        }

        #endregion
    }
}