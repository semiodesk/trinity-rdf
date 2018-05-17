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
using System;
using System.Linq.Expressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;

namespace Semiodesk.Trinity.Query
{
    internal class SubSelectQueryGenerator : SelectQueryGenerator
    {
        #region Constructors

        public SubSelectQueryGenerator(ISparqlQueryGenerator parent)
        {
            IsRoot = false;
            VariableGenerator = new SparqlVariableGenerator(parent.VariableGenerator);
        }

        #endregion

        #region Methods

        public override void OnBeforeFromClauseVisited(Expression expression)
        {
            SparqlVariable s = null;
            SparqlVariable o = null;

            if(expression is MemberExpression)
            {
                QuerySourceReferenceExpression sourceExpression = expression.TryGetQuerySourceReference();

                s = VariableGenerator.TryGetSubjectVariable(sourceExpression) ?? VariableGenerator.TryGetObjectVariable(sourceExpression);
                o = VariableGenerator.CreateObjectVariable(expression);

                // The from clause is parsed first when handling a query. This allows us to detect if the
                // query source is a subquery and proceed with implementing it _before_ hanlding its results.
                MemberExpression memberExpression = expression as MemberExpression;

                if (s.IsGlobal())
                {
                    Type type = memberExpression.Member.DeclaringType;

                    if(type.IsSubclassOf(typeof(Resource)))
                    {
                        WhereResourceOfType(s, type);
                    }
                }

                // If the query model has a numeric result operator, we make all the following
                // expressions optional in order to also allow to count zero occurences.
                if (QueryModel.HasNumericResultOperator())
                {
                    GraphPatternBuilder optionalBuilder = new GraphPatternBuilder(GraphPatternType.Optional);

                    Child(optionalBuilder);
                    
                    PatternBuilder = optionalBuilder;
                }
            }
            else
            {
                s = VariableGenerator.TryGetSubjectVariable(expression);
                o = VariableGenerator.TryGetObjectVariable(expression);
            }

            if (s != null && o != null)
            {
                // Set the variable name of the query source reference as subject of the current query.
                SetSubjectVariable(s, true);
                SetObjectVariable(o, true);
            }
        }

        #endregion
    }
}