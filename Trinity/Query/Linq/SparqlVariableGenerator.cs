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

        #region Methods

        public void RegisterExpression(Expression expression, SparqlVariable variable)
        {
            _expressionVariables[expression.ToString()] = variable;
        }

        public bool HasVariable(Expression expression)
        {
            return _expressionVariables.ContainsKey(expression.ToString());
        }

        /// <summary>
        /// Removes special characters from the variable name and returns a valid SPARQL variable name.
        /// </summary>
        /// <param name="name">A variable name.</param>
        /// <returns>A variable name in canonical form.</returns>
        private string GetCanonicalVariableName(string name)
        {
            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < name.Length; i++)
            {
                var c = name[i];

                // Note: SPARQL supports more characters in variable names.
                // We reduce it to letters, digits and '_' as a separator.
                if(char.IsLetterOrDigit(c))
                {
                    if (i == 0)
                    {
                        builder.Append(char.ToLowerInvariant(c));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
                else if(c == '_')
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public SparqlVariable GetVariable(Expression expression)
        {
            QuerySourceReferenceExpression querySource = expression.TryGetQuerySourceReference();

            if (querySource != null && querySource.ReferencedQuerySource is MainFromClause)
            {
                MainFromClause fromClause = querySource.ReferencedQuerySource as MainFromClause;

                if(fromClause.FromExpression is MemberExpression)
                {
                    return _expressionVariables[fromClause.FromExpression.ToString()];
                }
            }

            return _expressionVariables[expression.ToString()];
        }

        public SparqlVariable GetVariable(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            name = GetCanonicalVariableName(name);

            int n = 0;

            if(_variableCounters.ContainsKey(name))
            {
                n = _variableCounters[name] + 1;

                _variableCounters[name] = n;
            }

            _variableCounters[name] = n;

            return new SparqlVariable(name + n);
        }

        public SparqlVariable GetVariable(QuerySourceReferenceExpression expression)
        {
            SparqlVariable v;

            string e = expression.ToString();

            if (!_expressionVariables.ContainsKey(e))
            {
                IQuerySource querySource = expression.ReferencedQuerySource;

                v = GetVariable(querySource.ItemName);

                _expressionVariables[e] = v;
            }
            else
            {
                v = _expressionVariables[e];
            }

            return v;
        }

        public SparqlVariable GetGlobalVariable(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            return new SparqlVariable(GetCanonicalVariableName(name) + "_");
        }

        public SparqlVariable GetGlobalVariable(QuerySourceReferenceExpression expression)
        {
            SparqlVariable v;

            string e = expression.ToString();

            if (!_expressionVariables.ContainsKey(e))
            {
                IQuerySource querySource = expression.ReferencedQuerySource;

                v = GetGlobalVariable(querySource.ItemName);

                _expressionVariables[e] = v;
            }
            else
            {
                v = _expressionVariables[e];
            }

            return v;
        }

        public SparqlVariable GetMemberVariable(MemberInfo member)
        {
            return GetVariable(GetCanonicalVariableName(member.Name));
        }

        public SparqlVariable GetPredicateVariable(string name = "p")
        {
            return GetVariable(name);
        }

        public SparqlVariable GetObjectVariable(string name = "o")
        {
            return GetVariable(name);
        }

        #endregion
    }
}
