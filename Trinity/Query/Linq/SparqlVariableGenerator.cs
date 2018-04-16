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
using Remotion.Linq.Clauses.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlVariableGenerator
    {
        #region Members

        private readonly Dictionary<string, int> _variableCounters = new Dictionary<string, int>();

        private readonly Dictionary<string, SparqlVariable> _expressionVariables = new Dictionary<string, SparqlVariable>();

        #endregion

        #region Constructors

        public SparqlVariableGenerator()
        {
        }

        #endregion

        #region Methods

        private string GetNextAvailableVariableName(string name)
        {
            int n = 0;

            if (_variableCounters.ContainsKey(name))
            {
                n = _variableCounters[name] + 1;
            }

            _variableCounters[name] = n;

            return name + n;
        }

        public bool HasExpressionVariable(Expression expression)
        {
            string key = expression.ToString();

            return _expressionVariables.ContainsKey(key);
        }

        public SparqlVariable GetExpressionVariable(Expression expression)
        {
            QuerySourceReferenceExpression querySource = expression.TryGetQuerySourceReference();

            if (querySource != null && querySource.ReferencedQuerySource is MainFromClause)
            {
                MainFromClause fromClause = querySource.ReferencedQuerySource as MainFromClause;

                if (fromClause.FromExpression is MemberExpression)
                {
                    return _expressionVariables[fromClause.FromExpression.ToString()];
                }
            }

            return _expressionVariables[expression.ToString()];
        }

        public void SetExpressionVariable(Expression expression, SparqlVariable variable)
        {
            string key = expression.ToString();

            _expressionVariables[key] = variable;
        }

        public SparqlVariable CreateLocalSubjectVariable(Expression expression)
        {
            string key = expression.ToString();

            SparqlVariable s = new SparqlVariable(GetNextAvailableVariableName("s"));

            _expressionVariables[key] = s;

            return s;
        }

        public SparqlVariable CreateLocalPredicateVariable()
        {
            return new SparqlVariable(GetNextAvailableVariableName("p"));
        }

        public SparqlVariable CreateLocalObjectVariable()
        {
            return new SparqlVariable(GetNextAvailableVariableName("o"));
        }

        public SparqlVariable GetGlobalSubjectVariable()
        {
            return new SparqlVariable("s_");
        }

        public SparqlVariable GetGlobalSubjectVariable(Expression expression)
        {
            string key = expression.ToString();

            SparqlVariable s = GetGlobalSubjectVariable();

            _expressionVariables[key] = s;

            return s;
        }

        public SparqlVariable GetGlobalPredicateVariable()
        {
            return new SparqlVariable("p_");
        }

        public SparqlVariable GetGlobalObjectVariable()
        {
            return new SparqlVariable("o_");
        }

        #endregion
    }
}
