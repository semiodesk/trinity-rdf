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
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class SelectBindingsQueryGenerator : SelectQueryGenerator
    {
        #region Constructors

        public SelectBindingsQueryGenerator()
        {
        }

        #endregion

        #region Methods

        public override void OnBeforeSelectClauseVisited(Expression selector)
        {
            base.OnBeforeSelectClauseVisited(selector);

            QuerySourceReferenceExpression sourceExpression = selector.TryGetQuerySourceReference();

            if (sourceExpression != null)
            {
                // Register the query source with the global variable for sub-queries.
                SparqlVariable s = VariableGenerator.GetGlobalSubjectVariable(sourceExpression);

                // Assert the object type.
                if (sourceExpression.Type.IsSubclassOf(typeof(Resource)))
                {
                    WhereResourceOfType(s, sourceExpression.Type);
                }

                if (selector is MemberExpression)
                {
                    SparqlVariable o = VariableGenerator.CreateLocalObjectVariable();

                    // Select all triples having the resource as subject.
                    SetSubjectVariable(s);
                    SetObjectVariable(o, true);

                    // Assert the triples which select the binding value.
                    MemberExpression member = selector as MemberExpression;

                    Where(member, o);
                }
                else if(HasNumericResultOperator())
                {
                    // If we have a numeric result operator on the root query, make the
                    // subject variable known so that the model visitor can handle it.
                    SetSubjectVariable(s);
                }
            }
        }

        #endregion
    }
}