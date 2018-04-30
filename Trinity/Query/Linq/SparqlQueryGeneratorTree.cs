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
using System.Collections.Generic;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryGeneratorTree : ISparqlQueryGeneratorTree
    {
        #region Members

        private ISparqlQueryGenerator _currentQueryGenerator;

        private ISparqlQueryGenerator _rootQueryGenerator;

        private readonly Dictionary<ISparqlQueryGenerator, IList<ISparqlQueryGenerator>> _generatorTree = new Dictionary<ISparqlQueryGenerator, IList<ISparqlQueryGenerator>>();

        private readonly Dictionary<SubQueryExpression, ISparqlQueryGenerator> _expressionGenerators = new Dictionary<SubQueryExpression, ISparqlQueryGenerator>();

        private readonly Dictionary<QueryModel, ISparqlQueryGenerator> _queryModelGenerators = new Dictionary<QueryModel, ISparqlQueryGenerator>();

        private readonly SparqlVariableGenerator _variableGenerator;

        #endregion

        #region Constructors

        public SparqlQueryGeneratorTree(ISparqlQueryGenerator queryGenerator, SparqlVariableGenerator variableGenerator)
        {
            // The query generator of the outermost query.
            _rootQueryGenerator = queryGenerator;

            // The current (sub-)query generator.
            _currentQueryGenerator = queryGenerator;

            // Generates unique variable names for the query generators.
            _variableGenerator = variableGenerator;
        }

        #endregion

        #region Methods

        public void Bind()
        {
            Bind(_rootQueryGenerator);
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

        public ISparqlQueryGenerator CreateSubQueryGenerator<T>(SubQueryExpression expression = null) where T : SelectQueryGenerator, new()
        {
            ISparqlQueryGenerator generator = new T();

            if(expression != null)
            {
                RegisterQueryExpression(generator, expression);
            }

            if (_currentQueryGenerator != null)
            {
                AddSubQueryGenerator(_currentQueryGenerator, generator);
            }
            else if (_rootQueryGenerator != null)
            {
                AddSubQueryGenerator(_rootQueryGenerator, generator);
            }

            return generator;
        }

        private void AddSubQueryGenerator(ISparqlQueryGenerator generator, ISparqlQueryGenerator subQueryGenerator)
        {
            // Add the sub query to the query tree.
            if (_generatorTree.ContainsKey(generator))
            {
                _generatorTree[generator].Add(subQueryGenerator);
            }
            else
            {
                _generatorTree[generator] = new List<ISparqlQueryGenerator>() { subQueryGenerator };
            }

            subQueryGenerator.ParentGenerator = generator;
        }

        public void RegisterQueryModel(ISparqlQueryGenerator generator, QueryModel queryModel)
        {
            if(!_queryModelGenerators.ContainsKey(queryModel))
            {
                _queryModelGenerators[queryModel] = generator;
            }
        }

        public void RegisterQueryExpression(ISparqlQueryGenerator generator, SubQueryExpression expression)
        {
            if (!_expressionGenerators.ContainsKey(expression))
            {
                _expressionGenerators[expression] = generator;
            }
        }

        public bool IsRootQueryGenerator()
        {
            return _rootQueryGenerator == _currentQueryGenerator;
        }

        public ISparqlQueryGenerator GetRootQueryGenerator()
        {
            return _rootQueryGenerator;
        }

        public ISparqlQueryGenerator GetCurrentQueryGenerator()
        {
            return _currentQueryGenerator;
        }

        public void SetCurrentQueryGenerator(ISparqlQueryGenerator queryGenerator)
        {
            _currentQueryGenerator = queryGenerator;
        }

        public bool HasQueryGenerator(QueryModel queryModel)
        {
            return _queryModelGenerators.ContainsKey(queryModel);
        }

        public ISparqlQueryGenerator GetQueryGenerator(QueryModel queryModel)
        {
            return _queryModelGenerators[queryModel];
        }

        public bool HasQueryGenerator(SubQueryExpression subQuery)
        {
            return _expressionGenerators.ContainsKey(subQuery);
        }

        public ISparqlQueryGenerator GetQueryGenerator(SubQueryExpression subQuery)
        {
            return _expressionGenerators[subQuery];
        }

        public IEnumerable<ISparqlQueryGenerator> TryGetSubQueries(ISparqlQueryGenerator query)
        {
            return _generatorTree.ContainsKey(query) ? _generatorTree[query] : null;
        }

        #endregion
    }
}
