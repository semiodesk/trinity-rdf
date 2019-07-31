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
using Remotion.Linq.Clauses.ResultOperators;
using VDS.RDF.Query.Aggregates.Sparql;
using VDS.RDF.Query.Expressions.Primary;
using VDS.RDF.Query;
using System;
using System.Linq;

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

        public override void SetObjectOperator(ResultOperatorBase resultOperator)
        {
            base.SetObjectOperator(resultOperator);

            if (ObjectVariable != null)
            {
                if (resultOperator is AnyResultOperator)
                {
                    // When using x.Any(), we add a LIMIT 1 the query results in the SparqlQueryGenerator.

                    // When using .Any(x => ..), the variable x is locally scoped and cannot be
                    // used in outer queries. Therefore do not need to actually select it.

                    // This avoids issues with Stardog:
                    // https://community.stardog.com/t/sparql-union-only-working-with-inferencing-enabled/1040/9
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
                    // "Using LIMIT and OFFSET to select different subsets of the query solutions 
                    // will not be useful unless the order is made predictable by using ORDER BY."
                    // Source: https://www.w3.org/TR/2013/REC-sparql11-query-20130321/#modOffset

                    // Therefore, if no ordering exists we add an ordering on the current subject 
                    // to make the query result predictable.
                    if (!QueryModel.BodyClauses.OfType<OrderByClause>().Any())
                    {
                        OrderBy(SubjectVariable);
                    }
                    else
                    {
                        // In case the order was make explicit, we have to do nothing.
                    }

                    Limit(1);
                }
                else if (resultOperator is LastResultOperator)
                {
                    // "Using LIMIT and OFFSET to select different subsets of the query solutions 
                    // will not be useful unless the order is made predictable by using ORDER BY."
                    // Source: https://www.w3.org/TR/2013/REC-sparql11-query-20130321/#modOffset

                    // Therefore, if no ordering exists we add an ordering on the current subject 
                    // to make the query result predictable.
                    if (!QueryModel.BodyClauses.OfType<OrderByClause>().Any())
                    {
                        OrderByDescending(SubjectVariable);
                    }
                    else
                    {
                        // Inverting the direction of the first ordering is handled in SparqlQueryModelVisitor.VisitOrdering().
                        // This is because the orderings are not necessarily processed *before* the result operators..
                    }

                    Limit(1);
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
                    OfTypeResultOperator ofType = resultOperator as OfTypeResultOperator;
                    RdfClassAttribute type = ofType.SearchedItemType.TryGetCustomAttribute<RdfClassAttribute>();

                    if (type == null)
                    {
                        throw new ArgumentException("No RdfClass attrribute declared on type: " + ofType.SearchedItemType);
                    }

                    SparqlVariable s = ObjectVariable;
                    SparqlVariable p = VariableGenerator.CreatePredicateVariable();
                    SparqlVariable o = VariableGenerator.CreateObjectVariable();

                    WhereResource(s, p, o);
                    WhereResourceOfType(o, ofType.SearchedItemType);
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
                    throw new NotImplementedException(resultOperator.ToString());
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
                    throw new NotImplementedException(resultOperator.ToString());
                }
            }
        }

        #endregion
    }
}