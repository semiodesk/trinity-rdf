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
// Copyright (c) Semiodesk GmbH 2018

using Remotion.Linq.Clauses.Expressions;
using System.Linq.Expressions;
using VDS.RDF.Query;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Linq;

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

        private Type TryGetSelectedType(Expression selector)
        {
            if (selector is ConstantExpression)
            {
                // We can either select a resource as a constant.
                IQueryable queryable = (selector as ConstantExpression).Value as IQueryable;

                return (queryable != null) ? queryable.ElementType : null;
            }
            else if (selector is MemberExpression)
            {
                // Or we can select a resource from a member variable.
                return (selector as MemberExpression).Type;
            }
            else if (selector is QuerySourceReferenceExpression)
            {
                // Or we can select resources from a query source reference.
                return (selector as QuerySourceReferenceExpression).Type;
            }
            else
            {
                // TODO: Create unit test for handling SubQueryExpression.
                throw new NotImplementedException();
            }
        }

        public override void OnBeforeSelectClauseVisited(Expression selector)
        {
            base.OnBeforeSelectClauseVisited(selector);

            Type selectedType = TryGetSelectedType(selector);

            if (selectedType == null || !typeof(Resource).IsAssignableFrom(selectedType))
            {
                throw new NotSupportedException(selectedType.ToString());
            }

            // 1. We always create an outer query which selects all triples that describe our resources.
            SparqlVariable s_ = VariableGenerator.GlobalSubject;
            SparqlVariable p_ = VariableGenerator.GlobalPredicate;
            SparqlVariable o_ = VariableGenerator.GlobalObject;

            VariableGenerator.SetSubjectVariable(selector, s_);
            VariableGenerator.SetPredicateVariable(selector, p_);
            VariableGenerator.SetObjectVariable(selector, o_);

            SetSubjectVariable(s_);
            SetObjectVariable(o_);

            SelectVariable(s_);
            SelectVariable(p_);
            SelectVariable(o_);

            WhereResource(s_, p_, o_);

            // If we are describing resources using a SKIP or TAKE operator, we need to make sure that
            // these operations are on a per-resource basis and all triples for the described resources
            // are contained in the result.
            if (QueryModel.HasResultOperator<SkipResultOperator>()
                || QueryModel.HasResultOperator<TakeResultOperator>()
                || QueryModel.HasResultOperator<FirstResultOperator>()
                || QueryModel.HasResultOperator<LastResultOperator>())
            {
                // ..which are described in an inner query on which the LIMIT and OFFSET operators are set.
                // This results in a SELECT query that acts like a DESCRIBE but ist faster on most triple 
                // stores as the triples can be returend via bindings and must not be parsed.
                ISparqlQueryGenerator subGenerator = QueryGeneratorTree.CreateSubQueryGenerator(this, selector);

                subGenerator.SetSubjectVariable(s_, true);
                subGenerator.SetObjectVariable(o_);

                GenerateTypeConstraintOnSubject(subGenerator, selector);

                QueryGeneratorTree.CurrentGenerator = subGenerator;

                // NOTE: We set the subGenerator as a child *AFTER* the select clause and body clauses
                // have been processed (see <c>OnSelectClauseVisited</c>). This is because the dotNetRDF 
                // query generator does not correctly handle result operators when it is already set as a child.
            }
            else
            {
                GenerateTypeConstraintOnSubject(this, selector);
            }
        }

        private void GenerateTypeConstraintOnSubject(ISparqlQueryGenerator generator, Expression selector)
        {
            Type type = null;

            if(selector is ConstantExpression)
            {
                type = (selector as ConstantExpression).Value.GetType();
            }
            else if(selector is MemberExpression)
            {
                type = (selector as MemberExpression).Member.DeclaringType;
            }
            else if(selector is QuerySourceReferenceExpression)
            {
                type = (selector as QuerySourceReferenceExpression).ReferencedQuerySource.ItemType;
            }

            if(type != null && type.IsSubclassOf(typeof(Resource)))
            {
                generator.WhereResourceOfType(VariableGenerator.GlobalSubject, type);
            }
        }

        public override void OnSelectClauseVisited(Expression selector)
        {
            if (QueryModel.HasResultOperator<SkipResultOperator>()
                || QueryModel.HasResultOperator<TakeResultOperator>()
                || QueryModel.HasResultOperator<FirstResultOperator>()
                || QueryModel.HasResultOperator<LastResultOperator>())
            {
                // Finally, if we have a SKIP or TAKE operator on the root query, set the current 
                // query generator as a child.
                ISparqlQueryGenerator subGenerator = QueryGeneratorTree.GetQueryGenerator(selector);

                Child(subGenerator);
            }
        }

        #endregion
    }
}