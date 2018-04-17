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

using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using VDS.RDF.Query.Aggregates.Sparql;
using VDS.RDF.Query.Expressions.Primary;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class SelectQueryGenerator : SparqlQueryGenerator
    {
        #region Constructors

        public SelectQueryGenerator()
            : base(VDS.RDF.Query.Builder.QueryBuilder.Select(new string[] {}))
        {
        }

        #endregion

        #region Methods

        public override void OnSelectClauseVisited(Expression selector)
        {
            base.OnSelectClauseVisited(selector);

            // If we are in the root query generator and have not yet selected the
            // subject variable, set it from the given selector.
            if (IsRoot && SubjectVariable == null && VariableGenerator.HasExpressionVariable(selector))
            {
                SparqlVariable v = VariableGenerator.GetExpressionVariable(selector);

                if (!IsSelectedVariable(v))
                {
                    SelectVariable(v.Name);
                }
            }
        }

        public override void SetObjectOperator(ResultOperatorBase resultOperator)
        {
            base.SetObjectOperator(resultOperator);

            if (ObjectVariable != null)
            {
                if (resultOperator is AnyResultOperator)
                {
                    var aggregate = new SampleAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                }
                else if (resultOperator is AverageResultOperator)
                {
                    var aggregate = new AverageAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                }
                else if (resultOperator is CountResultOperator)
                {
                    var aggregate = new CountDistinctAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                }
                else if (resultOperator is FirstResultOperator)
                {
                    if (QueryModel.MainFromClause.FromExpression is MemberExpression)
                    {
                        throw new NotSupportedException("The First()-result operator is not supported for accessing members.");
                    }
                    else
                    {
                        Limit(1);
                    }
                }
                else if (resultOperator is LastResultOperator)
                {
                    //var aggregate = new MinAggregate(new VariableTerm(ObjectVariable.Name));
                    //SetObjectVariable(aggregate.AsSparqlVariable(), true);
                    //OrderByDescending(ObjectVariable);

                    throw new NotSupportedException();
                }
                else if (resultOperator is MaxResultOperator)
                {
                    var aggregate = new MaxAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                }
                else if (resultOperator is MinResultOperator)
                {
                    var aggregate = new MinAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                }
                else if (resultOperator is SumResultOperator)
                {
                    var aggregate = new SumAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                }
                else if (resultOperator is OfTypeResultOperator)
                {
                    OfTypeResultOperator op = resultOperator as OfTypeResultOperator;
                    RdfClassAttribute type = op.SearchedItemType.TryGetCustomAttribute<RdfClassAttribute>();

                    if (type == null)
                    {
                        throw new ArgumentException("No RdfClass attrribute declared on type: " + op.SearchedItemType);
                    }

                    WhereResource(ObjectVariable);
                    WhereResourceOfType(ObjectVariable, op.SearchedItemType);
                }
                else if (resultOperator is SkipResultOperator)
                {
                    SkipResultOperator op = resultOperator as SkipResultOperator;
                    Offset(int.Parse(op.Count.ToString()));
                }
                else if(resultOperator is TakeResultOperator)
                {
                    TakeResultOperator op = resultOperator as TakeResultOperator;
                    Limit(int.Parse(op.Count.ToString()));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override void SetSubjectOperator(ResultOperatorBase resultOperator)
        {
            base.SetSubjectOperator(resultOperator);

            if (SubjectVariable != null)
            {
                if (resultOperator is CountResultOperator)
                {
                    var aggregate = new CountDistinctAggregate(new VariableTerm(SubjectVariable.Name));
                    SetSubjectVariable(aggregate.AsSparqlVariable(), true);
                }
                else if (resultOperator is FirstResultOperator)
                {
                    if(!IsRoot)
                    {
                        // Note: We currently only support First operators on root queries.
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        #endregion
    }
}