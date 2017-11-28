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
using VDS.RDF.Query;
using VDS.RDF.Query.Aggregates.Sparql;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Expressions.Primary;

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
                    var aggregate = new MinAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                    OrderBy(ObjectVariable);
                }
                else if (resultOperator is LastResultOperator)
                {
                    var aggregate = new MinAggregate(new VariableTerm(ObjectVariable.Name));
                    SetObjectVariable(aggregate.AsSparqlVariable(), true);
                    OrderByDescending(ObjectVariable);
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

                    SparqlVariable s = SubjectVariable;
                    Uri p = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
                    Uri o = type.MappedUri;

                    PatternBuilder.Where(t => t.Subject(s.Name).PredicateUri(p).Object(o));
                }
                else if (resultOperator is SkipResultOperator)
                {
                    SkipResultOperator op = resultOperator as SkipResultOperator;
                    Offset(int.Parse(op.Count.ToString()));
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