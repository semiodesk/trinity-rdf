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
using Remotion.Linq.Clauses.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryGeneratorTree : ISparqlQueryGeneratorTree
    {
        #region Members

        public ISparqlQueryGenerator CurrentGenerator { get; set; }

        public ISparqlQueryGenerator RootGenerator { get; private set; }

        private readonly Dictionary<ISparqlQueryGenerator, IList<ISparqlQueryGenerator>> _generatorTree = new Dictionary<ISparqlQueryGenerator, IList<ISparqlQueryGenerator>>();

        private readonly Dictionary<string, ISparqlQueryGenerator> _expressionGenerators = new Dictionary<string, ISparqlQueryGenerator>();

        private readonly SparqlVariableGenerator _variableGenerator;

        #endregion

        #region Constructors

        public SparqlQueryGeneratorTree(ISparqlQueryGenerator rootGenerator, SparqlVariableGenerator variableGenerator)
        {
            // The query generator of the outermost query.
            RootGenerator = rootGenerator;

            // The current (sub-)query generator.
            CurrentGenerator = rootGenerator;

            // Generates unique variable names for the query generators.
            _variableGenerator = variableGenerator;
        }

        #endregion

        #region Methods

        private string GetKey(Expression expression)
        {
            return expression.ToString();
        }

        public void Bind()
        {
            Bind(RootGenerator);
        }

        private void Bind(ISparqlQueryGenerator generator)
        {
            generator.BindSelectVariables();

            if (_generatorTree.ContainsKey(generator))
            {
                foreach (ISparqlQueryGenerator g in _generatorTree[generator])
                {
                    Bind(g);
                }
            }
        }

        public ISparqlQueryGenerator CreateSubQueryGenerator(ISparqlQueryGenerator parentGenerator, Expression expression)
        {
            if (parentGenerator == null) throw new ArgumentNullException("parentGenerator");
            if (expression == null) throw new ArgumentNullException("expression");

            ISparqlQueryGenerator generator = new SubSelectQueryGenerator();
            generator.SetQueryContext(this, _variableGenerator, parentGenerator.QueryModel);

            RegisterQueryExpression(generator, expression);

            if (CurrentGenerator != null)
            {
                AddSubQueryGenerator(CurrentGenerator, generator);
            }
            else if (RootGenerator != null)
            {
                AddSubQueryGenerator(RootGenerator, generator);
            }

            return generator;
        }

        private void AddSubQueryGenerator(ISparqlQueryGenerator parentGenerator, ISparqlQueryGenerator subQueryGenerator)
        {
            // Add the sub query to the query tree.
            if (_generatorTree.ContainsKey(parentGenerator))
            {
                _generatorTree[parentGenerator].Add(subQueryGenerator);
            }
            else
            {
                _generatorTree[parentGenerator] = new List<ISparqlQueryGenerator>() { subQueryGenerator };
            }

            subQueryGenerator.ParentGenerator = parentGenerator;
        }

        public void RegisterQueryExpression(ISparqlQueryGenerator generator, Expression expression)
        {
            string key = GetKey(expression);

            if (!_expressionGenerators.ContainsKey(key))
            {
                _expressionGenerators[key] = generator;
            }
        }

        public bool HasQueryGenerator(Expression expression)
        {
            string key = GetKey(expression);

            return _expressionGenerators.ContainsKey(key);
        }

        public ISparqlQueryGenerator GetQueryGenerator(Expression expression)
        {
            string key = GetKey(expression);

            return _expressionGenerators[key];
        }

        public IEnumerable<ISparqlQueryGenerator> GetChildren(ISparqlQueryGenerator query)
        {
            return _generatorTree.ContainsKey(query) ? _generatorTree[query] : null;
        }

        #endregion
    }
}
