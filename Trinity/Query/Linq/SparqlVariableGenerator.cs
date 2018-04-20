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
using System.Linq;
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

        private readonly Dictionary<Expression, SparqlVariable> _expressionVariables = new Dictionary<Expression, SparqlVariable>();

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
            return _expressionVariables.ContainsKey(expression);
        }

        public SparqlVariable GetExpressionVariable(Expression expression)
        {
            if(_expressionVariables.ContainsKey(expression))
            {
                return _expressionVariables[expression];
            }
            else
            {
                QuerySourceReferenceExpression querySource = expression.TryGetQuerySourceReference();

                if (querySource != null && querySource.ReferencedQuerySource is MainFromClause)
                {
                    MainFromClause fromClause = querySource.ReferencedQuerySource as MainFromClause;

                    if (fromClause.FromExpression is MemberExpression)
                    {
                        return _expressionVariables[fromClause.FromExpression];
                    }
                }

                string key = expression.ToString();

                SparqlVariable v = _expressionVariables.FirstOrDefault(p => p.Key.ToString() == key).Value;

                if(v != null)
                {
                    return v;
                }
                else
                {
                    throw new KeyNotFoundException(key);
                }
            }
        }

        public void SetExpressionVariable(Expression expression, SparqlVariable variable)
        {
            if(!_expressionVariables.ContainsKey(expression))
            {
                _expressionVariables[expression] = variable;
            }
            else if(_expressionVariables[expression] != variable)
            {
                string msg = "Variable mapping for expression '{0}' already exists: '{1}'. Cannot set value: '{2}'.";
                throw new InvalidOperationException(string.Format(msg, expression.ToString(), _expressionVariables[expression].Name, variable.Name));
            }
        }

        public SparqlVariable CreateLocalSubjectVariable(Expression expression)
        {
            SparqlVariable s = new SparqlVariable(GetNextAvailableVariableName("s"));

            _expressionVariables[expression] = s;

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
            SparqlVariable s = GetGlobalSubjectVariable();

            _expressionVariables[expression] = s;

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
