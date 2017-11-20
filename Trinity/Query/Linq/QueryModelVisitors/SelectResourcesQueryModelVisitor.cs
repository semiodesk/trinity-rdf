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
using System;
using System.Linq.Expressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;

namespace Semiodesk.Trinity.Query
{
    internal class SelectResourcesQueryModelVisitor<T> : SparqlQueryModelVisitorBase<T>
    {
        #region Methods

        protected override void InitializeQueryGenerator(QueryModel queryModel)
        {
            // The root query which selects triples when returning resources.
            CurrentQueryGenerator = new SparqlQueryGenerator(this, QueryBuilder.Select(new string[] {}));

            // Add the root query builder to the query tree.
            QueryGeneratorTree = new SparqlQueryGeneratorTree(CurrentQueryGenerator);
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            if (fromClause.FromExpression.NodeType == ExpressionType.Constant)
            {
                string s = fromClause.ItemName;

                // Select all triples having the resource as subject.
                CurrentQueryGenerator.SetSubjectVariable(new SparqlVariable(s));
                CurrentQueryGenerator.SelectVariable(new SparqlVariable("p_"));
                CurrentQueryGenerator.SetObjectVariable(new SparqlVariable("o_"));

                CurrentQueryGenerator.Where(e => e.Subject(s).Predicate("p_").Object("o_"));

                // Assert the resource type, if any.
                RdfClassAttribute type = fromClause.ItemType.TryGetCustomAttribute<RdfClassAttribute>();

                if (type != null)
                {
                    Uri p = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

                    CurrentQueryGenerator.Where(e => e.Subject(s).PredicateUri(p).Object(type.MappedUri));
                }
            }

            base.VisitMainFromClause(fromClause, queryModel);
        }

        #endregion
    }
}
