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
using System.Linq.Expressions;
using VDS.RDF.Query;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using System;

namespace Semiodesk.Trinity.Query
{
    internal class SelectTriplesQueryGenerator : SelectQueryGenerator
    {
        #region Constructors

        public SelectTriplesQueryGenerator()
        {
        }

        #endregion

        #region Methods

        public override void OnBeforeFromClauseVisited(Expression expression)
        {
            base.OnBeforeFromClauseVisited(expression);

            // If we are describing resources using a skip or take operator, we need to make sure that
            // these operations are on a per-resource basis and all triples for the described resources
            // are contained in the result.
            if(HasResultOperator<SkipResultOperator>()
                || HasResultOperator<TakeResultOperator>()
                || HasResultOperator<FirstResultOperator>()
                || HasResultOperator<LastResultOperator>())
            {
                // We create an outer query which selects all triples for the resources..
                SparqlVariable s = VariableGenerator.GetGlobalSubjectVariable(); // TODO: Always create the subject variable here and register the expression later..
                SparqlVariable p = VariableGenerator.GetGlobalPredicateVariable();
                SparqlVariable o = VariableGenerator.GetGlobalObjectVariable();

                if(expression is ConstantExpression)
                {
                    VariableGenerator.SetExpressionVariable(expression, s);
                }
                else
                {
                    QuerySourceReferenceExpression sourceExpression = QueryModel.MainFromClause.FromExpression.TryGetQuerySourceReference();

                    if(sourceExpression != null)
                    {
                        VariableGenerator.SetExpressionVariable(sourceExpression, s);
                    }
                    else
                    {
                        throw new Exception("Unable to determine query subject from the given from-expression.");
                    }
                }

                SetSubjectVariable(s);

                SelectVariable(s);
                SelectVariable(p);
                SelectVariable(o);

                PatternBuilder.Where(t => t.Subject(s).Predicate(p).Object(o));

                // ..which are described in an inner query on which the LIMIT and OFFSET operators are set.
                // This results in a SELECT query that acts like a DESCRIBE but ist faster on most triple 
                // stores as the triples can be returend via bindings and must not be parsed.
                ISparqlQueryGenerator subGenerator = QueryGeneratorTree.CreateSubQueryGenerator<SelectTriplesQueryGenerator>();
                subGenerator.SetQueryContext(QueryModel, QueryGeneratorTree, VariableGenerator);

                QueryGeneratorTree.SetCurrentQueryGenerator(subGenerator);
            }
        }

        public override void OnBeforeSelectClauseVisited(Expression selector)
        {
            base.OnBeforeSelectClauseVisited(selector);

            // In any case, we need to describe the queried object provided by the from expression.
            QuerySourceReferenceExpression sourceExpression = selector.TryGetQuerySourceReference();

            if (sourceExpression != null)
            {
                SparqlVariable s = null;
                SparqlVariable p = VariableGenerator.GetGlobalPredicateVariable();
                SparqlVariable o = VariableGenerator.GetGlobalObjectVariable();

                if (selector is MemberExpression)
                {
                    s = VariableGenerator.CreateLocalSubjectVariable(sourceExpression);

                    // We set the query subject, just in case there are any sub queries.
                    SetSubjectVariable(s);

                    // If we are selecting to return a member of an object, we select 
                    // the triples of the resource and generate the required member access triples.
                    MemberExpression memberExpression = selector as MemberExpression;

                    SparqlVariable m = VariableGenerator.GetGlobalSubjectVariable(memberExpression);

                    SelectVariable(m);
                    SelectVariable(p);
                    SelectVariable(o);

                    PatternBuilder.Where(t => t.Subject(m).Predicate(p).Object(o));

                    WhereResourceOfType(m, memberExpression.Member.GetMemberType());

                    // The member can be accessed via chained properties (?s ex:prop1 / ex:prop2 ?m).
                    BuildMemberAccess(memberExpression, m);
                }
                else if (selector is QuerySourceReferenceExpression)
                {
                    s = VariableGenerator.GetGlobalSubjectVariable(sourceExpression);

                    // We set the query subject, just in case there are any sub queries.
                    SetSubjectVariable(s);
                    SetObjectVariable(o);

                    // Only select the query source variables if we directly return the object.
                    SelectVariable(s);

                    if (IsRoot)
                    {
                        SelectVariable(p);
                        SelectVariable(o);

                        PatternBuilder.Where(t => t.Subject(s).Predicate(p).Object(o));
                    }

                    // Add the type constraint on the referenced query source.
                    WhereResourceOfType(s, sourceExpression.ReferencedQuerySource.ItemType);
                }
            }
        }

        public override void OnSelectClauseVisited(Expression selector)
        {
            if(!IsRoot)
            {
                // Finally, if we have a skip- or take operator on the root query, set the current 
                // query generator as a child.
                ISparqlQueryGenerator rootGenerator = QueryGeneratorTree.GetRootQueryGenerator();

                if(rootGenerator.HasResultOperator<SkipResultOperator>()
                    || rootGenerator.HasResultOperator<TakeResultOperator>()
                    || rootGenerator.HasResultOperator<FirstResultOperator>()
                    || rootGenerator.HasResultOperator<LastResultOperator>())
                {
                    rootGenerator.Child(this);
                }
            }
        }

        #endregion
    }
}