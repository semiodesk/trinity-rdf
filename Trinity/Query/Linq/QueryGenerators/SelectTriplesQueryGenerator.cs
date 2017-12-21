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
using System;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class SelectTriplesQueryGenerator : SelectQueryGenerator
    {
        #region Constructors

        public SelectTriplesQueryGenerator()
        {
            IsRoot = true;
        }

        #endregion

        #region Methods

        public override void OnBeforeSelectVisited(Expression selector)
        {
            base.OnBeforeSelectVisited(selector);

            // In any case, we need to describe the queried object provided by the from expression.
            QuerySourceReferenceExpression sourceExpression = selector.TryGetQuerySourceReference();

            if (sourceExpression != null)
            {
                SparqlVariable s = null;
                SparqlVariable p = VariableGenerator.GetGlobalVariable("p");
                SparqlVariable o = VariableGenerator.GetGlobalVariable("o");

                if (selector is MemberExpression)
                {
                    s = VariableGenerator.GetVariable(sourceExpression);

                    // We set the query subject, just in case there are any sub queries.
                    SetSubjectVariable(s);

                    // If we are selecting to return a member of an object, we select 
                    // the triples of the resource and generate the required member access triples.
                    MemberExpression memberExpression = selector as MemberExpression;

                    SparqlVariable m = VariableGenerator.GetGlobalVariable(memberExpression.Member.Name);

                    SelectVariable(m);
                    SelectVariable(p);
                    SelectVariable(o);

                    PatternBuilder.Where(t => t.Subject(m.Name).Predicate(p.Name).Object(o.Name));

                    Type type = memberExpression.Member.GetMemberType();

                    if (type != null)
                    {
                        WhereResourceOfType(m, type);
                    }

                    // The member can be accessed via chained properties (?s ex:prop1 / ex:prop2 ?m).
                    BuildMemberAccess(memberExpression, m);
                }
                else if (selector is QuerySourceReferenceExpression)
                {
                    s = VariableGenerator.GetGlobalVariable(sourceExpression);

                    // We set the query subject, just in case there are any sub queries.
                    SetSubjectVariable(s);
                    SetObjectVariable(o);

                    // Only select the query source variables if we directly return the object.
                    SelectVariable(s);
                    SelectVariable(p);
                    SelectVariable(o);

                    PatternBuilder.Where(t => t.Subject(s.Name).Predicate(p.Name).Object(o.Name));

                    // Add the type constraint on the referenced query source.
                    Type type = sourceExpression.ReferencedQuerySource.ItemType;

                    if (typeof(Resource).IsAssignableFrom(type))
                    {
                        WhereResourceOfType(s, type);
                    }
                }
            }
        }

        #endregion
    }
}