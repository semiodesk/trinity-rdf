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
using System.Collections.Generic;
using System.Linq.Expressions;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlVariableGenerator : ISparqlVariableGenerator
    {
        #region Members

        public Dictionary<string, int> VariableCounters { get; private set; }

        public SparqlVariable GlobalSubject { get; } = new SparqlVariable("s_");

        public SparqlVariable GlobalPredicate { get; } = new SparqlVariable("p_");

        public SparqlVariable GlobalObject { get; } = new SparqlVariable("o_");

        private readonly Dictionary<string, SparqlVariable> _subjectVariables = new Dictionary<string, SparqlVariable>();

        private readonly Dictionary<string, SparqlVariable> _predicateVariables = new Dictionary<string, SparqlVariable>();

        private readonly Dictionary<string, SparqlVariable> _objectVariables = new Dictionary<string, SparqlVariable>();

        private readonly Dictionary<string, string> _expressionMappings = new Dictionary<string, string>();

        private readonly ISparqlVariableGenerator _parentGenerator;

        #endregion

        #region Constructors

        public SparqlVariableGenerator(ISparqlVariableGenerator parent)
        {
            _parentGenerator = parent;

            if(_parentGenerator != null)
            {
                VariableCounters = _parentGenerator.VariableCounters;
            }
            else
            {
                VariableCounters = new Dictionary<string, int>();
            }
        }

        #endregion

        #region Methods

        private string GetNextAvailableVariableName(string name)
        {
            int n = 0;

            if (VariableCounters.ContainsKey(name))
            {
                n = VariableCounters[name] + 1;
            }

            VariableCounters[name] = n;

            return name + n;
        }

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

        public void AddVariableMapping(Expression expression, string alias)
        {
            if(!string.IsNullOrEmpty(alias))
            {
                string sourceKey = string.Format("[{0}]", alias);
                string targetKey = GetKey(expression);

                _expressionMappings[sourceKey] = targetKey;

                if (_subjectVariables.ContainsKey(targetKey))
                {
                    _objectVariables[sourceKey] = _subjectVariables[targetKey];
                }

                if (_objectVariables.ContainsKey(targetKey))
                {
                    _subjectVariables[sourceKey] = _objectVariables[targetKey];
                }
            }
            else
            {
                throw new ArgumentNullException("alias");
            }
        }

        public bool HasSubjectVariable(Expression expression)
        {
            return TryGetSubjectVariable(expression) != null;
        }

        public bool HasPredicateVariable(Expression expression)
        {
            return TryGetPredicateVariable(expression) != null;
        }

        public bool HasObjectVariable(Expression expression)
        {
            return TryGetObjectVariable(expression) != null;
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
            //if (expression is MemberExpression)
            //{
            //    QuerySourceReferenceExpression sourceExpression = expression.TryGetQuerySourceReference();

            //    return TryGetVariable(_subjectVariables, expression, sourceExpression) ?? _parentGenerator?.TryGetSubjectVariable(expression);
            //}
            //else
            {
                // For instances of ConstantExpression, QuerySourceReferenceExpression and SubQueryExpression there must be a direct mapping.
                return TryGetVariable(_subjectVariables, expression) ?? _parentGenerator?.TryGetSubjectVariable(expression);
            }
        }

        public SparqlVariable TryGetPredicateVariable(Expression expression)
        {
            return TryGetVariable(_predicateVariables, expression) ?? _parentGenerator?.TryGetPredicateVariable(expression);
        }

        public SparqlVariable TryGetObjectVariable(Expression expression)
        {
            return TryGetVariable(_objectVariables, expression) ?? _parentGenerator?.TryGetObjectVariable(expression);
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
