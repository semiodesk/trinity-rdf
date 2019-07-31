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

using Remotion.Linq.Clauses.Expressions;
using System;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class AskQueryGenerator : SparqlQueryGenerator
    {
        #region Constructors

        public AskQueryGenerator()
            : base(VDS.RDF.Query.Builder.QueryBuilder.Ask())
        {
            IsRoot = true;
            VariableGenerator = new SparqlVariableGenerator(null);
        }

        #endregion

        #region Methods

        public override void OnBeforeSelectClauseVisited(Expression selector)
        {
            base.OnBeforeSelectClauseVisited(selector);

            // TODO: Add support for selecting literal types as a query result.

            if (selector is QuerySourceReferenceExpression)
            {
                SparqlVariable s_ = VariableGenerator.GlobalSubject;
                SparqlVariable p_ = VariableGenerator.GlobalPredicate;
                SparqlVariable o_ = VariableGenerator.GlobalObject;

                // Select all triples having the resource as subject.
                SetSubjectVariable(s_);
                SetObjectVariable(o_);

                // Add the type constraint on the referenced query source.
                WhereResource(s_, p_, o_);

                // Constrain the type of resource, if it is a subclass of Resource.
                QuerySourceReferenceExpression sourceExpression = selector as QuerySourceReferenceExpression;

                Type type = sourceExpression.ReferencedQuerySource.ItemType;

                if (type.IsSubclassOf(typeof(Resource)))
                {
                    WhereResourceOfType(s_, type);
                }
            }
            else
            {
                // TODO: Create unit tests an implement for ConstantExpression, MemberExpression and SubQueryExpression.
                throw new NotImplementedException(selector.GetType().ToString());
            }
        }

        #endregion
    }
}