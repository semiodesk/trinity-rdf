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
using System.Diagnostics;
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

        private readonly Dictionary<string, SparqlVariable> _subjectVariables = new Dictionary<string, SparqlVariable>();

        private readonly Dictionary<string, SparqlVariable> _predicateVariables = new Dictionary<string, SparqlVariable>();

        private readonly Dictionary<string, SparqlVariable> _objectVariables = new Dictionary<string, SparqlVariable>();

        private readonly Dictionary<string, string> _expressionMappings = new Dictionary<string, string>();

        public readonly SparqlVariable GlobalSubject = new SparqlVariable("s_");

        public readonly SparqlVariable GlobalPredicate = new SparqlVariable("p_");

        public readonly SparqlVariable GlobalObject = new SparqlVariable("o_");

        #endregion

        #region Constructors

        public SparqlVariableGenerator() {}

        #endregion

        #region Methods

        private string GetKey(Expression expression)
        {
            //return expression.ToString().Trim();

            string key = expression.ToString().Trim();

            if (key.EndsWith(".Uri"))
            {
                key = key.Substring(0, key.LastIndexOf(".Uri"));
            }

            return key;
        }

        public void AddMapping(Expression expression, string itemName)
        {
            if(!string.IsNullOrEmpty(itemName))
            {
                string key = string.Format("[{0}]", itemName);

                _expressionMappings[key] = expression.ToString();
            }
            else
            {
                throw new ArgumentNullException("itemName");
            }
        }

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

        private SparqlVariable TryGetVariable(Dictionary<string, SparqlVariable> source, params Expression[] expressions)
        {
            foreach(Expression expression in expressions)
            {
                string primaryKey = GetKey(expression);

                if (source.ContainsKey(primaryKey))
                {
                    return source[primaryKey];
                }
                else if(_expressionMappings.ContainsKey(primaryKey))
                {
                    string mappedKey = _expressionMappings[primaryKey];

                    if(source.ContainsKey(mappedKey))
                    {
                        return source[mappedKey];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get a variable from an expression that can be used as a subject in triple patterns and represents resources.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public SparqlVariable TryGetSubjectVariable(Expression expression)
        {
            if (expression is MemberExpression)
            {
                QuerySourceReferenceExpression sourceExpression = expression.TryGetQuerySourceReference();

                return TryGetVariable(_subjectVariables, expression, sourceExpression);
            }
            else
            {
                // For instances of ConstantExpression, QuerySourceReferenceExpression and SubQueryExpression there must be a direct mapping.
                return TryGetVariable(_subjectVariables, expression);
            }
        }

        public SparqlVariable TryGetPredicateVariable(Expression expression)
        {
            return TryGetVariable(_predicateVariables, expression);
        }

        public SparqlVariable TryGetObjectVariable(Expression expression)
        {
            return TryGetVariable(_objectVariables, expression);
        }

        public void SetSubjectVariable(Expression expression, SparqlVariable s)
        {
            string key = GetKey(expression);

            if (!_subjectVariables.ContainsKey(key) || _subjectVariables[key] != s)
            {
                _subjectVariables[key] = s;
            }
        }

        public void SetPredicateVariable(Expression expression, SparqlVariable p)
        {
            string key = GetKey(expression);

            if (!_predicateVariables.ContainsKey(key) || _predicateVariables[key] != p)
            {
                _predicateVariables[key] = p;
            }
        }

        public void SetObjectVariable(Expression expression, SparqlVariable o)
        {
            string key = GetKey(expression);

            if(!_objectVariables.ContainsKey(key))
            {
                _objectVariables[key] = o;
            }
        }

        public SparqlVariable CreateSubjectVariable(Expression expression)
        {
            SparqlVariable s = new SparqlVariable(GetNextAvailableVariableName("s"));

            SetSubjectVariable(expression, s);

            return s;
        }

        // TODO: Should take a MemberExpression as argument.
        public SparqlVariable CreatePredicateVariable()
        {
            return new SparqlVariable(GetNextAvailableVariableName("p"));
        }

        // TODO: Deprecated.
        public SparqlVariable CreateObjectVariable()
        {
            return new SparqlVariable(GetNextAvailableVariableName("o"));
        }

        public SparqlVariable CreateObjectVariable(Expression expression)
        {
            SparqlVariable o = new SparqlVariable(GetNextAvailableVariableName("o"));

            SetObjectVariable(expression, o);

            return o;
        }

        #endregion
    }
}
