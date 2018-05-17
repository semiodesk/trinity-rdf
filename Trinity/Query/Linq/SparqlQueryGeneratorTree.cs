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

        #endregion

        #region Constructors

        public SparqlQueryGeneratorTree(ISparqlQueryGenerator root)
        {
            // The query generator of the outermost query.
            RootGenerator = root;

            // The current (sub-)query generator.
            CurrentGenerator = root;
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

        public ISparqlQueryGenerator CreateSubQueryGenerator(ISparqlQueryGenerator parent, Expression expression)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (expression == null) throw new ArgumentNullException("expression");

            ISparqlQueryGenerator g = new SubSelectQueryGenerator(parent);
            g.SetQueryContext(this, parent.QueryModel);

            RegisterQueryExpression(g, expression);

            if (CurrentGenerator != null)
            {
                AddSubQueryGenerator(CurrentGenerator, g);
            }
            else if (RootGenerator != null)
            {
                AddSubQueryGenerator(RootGenerator, g);
            }

            return g;
        }

        private void AddSubQueryGenerator(ISparqlQueryGenerator parent, ISparqlQueryGenerator child)
        {
            // Add the sub query to the query tree.
            if (_generatorTree.ContainsKey(parent))
            {
                _generatorTree[parent].Add(child);
            }
            else
            {
                _generatorTree[parent] = new List<ISparqlQueryGenerator>() { child };
            }

            child.ParentGenerator = parent;
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

        #endregion
    }
}
