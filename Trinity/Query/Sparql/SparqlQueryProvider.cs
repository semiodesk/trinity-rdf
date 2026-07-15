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
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015-2020

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Semiodesk.Trinity.Query.Sparql
{
    /// <summary>
    /// A LINQ query provider that translates expression trees to SPARQL (via the owned AST) and
    /// executes them through the model's existing <c>GetResources</c>/<c>ExecuteQuery</c> path.
    /// This is the replacement for the re-linq-based provider (ADR: LINQ provider).
    /// </summary>
    internal sealed class SparqlQueryProvider : IQueryProvider
    {
        private static MethodInfo _getResourcesMethod;

        private readonly IModel _model;

        private readonly bool _inferenceEnabled;

        public SparqlQueryProvider(IModel model, bool inferenceEnabled)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _inferenceEnabled = inferenceEnabled;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TrinityQueryable<TElement>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = GetSequenceElementType(expression.Type);
            Type queryableType = typeof(TrinityQueryable<>).MakeGenericType(elementType);

            return (IQueryable)Activator.CreateInstance(queryableType, this, expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            QueryTranslation translation = new SparqlQueryTranslator().Translate(expression);

            var query = new SparqlQuery(SparqlQueryWriter.Write(translation.Query)) { Model = _model };

            switch (translation.Kind)
            {
                case QueryExecutionKind.Ask:
                    bool answer = _model.ExecuteQuery(query, _inferenceEnabled).GetAnwser();
                    return (TResult)(object)answer;

                case QueryExecutionKind.Count:
                    return (TResult)Convert.ChangeType(ExecuteCount(query), typeof(TResult));

                default:
                    IEnumerable resources = InvokeGetResources(translation.ElementType, query);
                    return ShapeResult<TResult>(resources, translation.Terminal);
            }
        }

        public object Execute(Expression expression)
        {
            return Execute<object>(expression);
        }

        private long ExecuteCount(ISparqlQuery query)
        {
            BindingSet bindings = _model.ExecuteQuery(query, _inferenceEnabled).GetBindings().FirstOrDefault();

            if (bindings == null || !bindings.Any())
            {
                return 0;
            }

            return Convert.ToInt64(bindings.First().Value);
        }

        private IEnumerable InvokeGetResources(Type elementType, ISparqlQuery query)
        {
            if (_getResourcesMethod == null)
            {
                _getResourcesMethod = typeof(IModel).GetMethods().First(m =>
                    m.Name == "GetResources" &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 3 &&
                    m.GetParameters()[0].ParameterType == typeof(ISparqlQuery));
            }

            MethodInfo method = _getResourcesMethod.MakeGenericMethod(elementType);

            return (IEnumerable)method.Invoke(_model, new object[] { query, _inferenceEnabled, null });
        }

        private static TResult ShapeResult<TResult>(IEnumerable resources, TerminalKind terminal)
        {
            if (terminal == TerminalKind.Enumerate)
            {
                return (TResult)resources;
            }

            List<object> items = resources.Cast<object>().ToList();

            object result;

            switch (terminal)
            {
                case TerminalKind.First:
                    result = items.First();
                    break;
                case TerminalKind.FirstOrDefault:
                    result = items.FirstOrDefault();
                    break;
                case TerminalKind.Single:
                    result = items.Single();
                    break;
                case TerminalKind.SingleOrDefault:
                    result = items.SingleOrDefault();
                    break;
                default:
                    throw new NotSupportedException($"Unsupported terminal operator: {terminal}.");
            }

            return (TResult)result;
        }

        private static Type GetSequenceElementType(Type type)
        {
            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[0];
            }

            Type enumerable = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerable != null ? enumerable.GetGenericArguments()[0] : type;
        }
    }

    /// <summary>An <see cref="IQueryable{T}"/> backed by <see cref="SparqlQueryProvider"/>.</summary>
    internal sealed class TrinityQueryable<T> : IOrderedQueryable<T>
    {
        public TrinityQueryable(SparqlQueryProvider provider)
        {
            Provider = provider;
            Expression = Expression.Constant(this);
        }

        public TrinityQueryable(IQueryProvider provider, Expression expression)
        {
            Provider = provider;
            Expression = expression;
        }

        public Type ElementType => typeof(T);

        public Expression Expression { get; }

        public IQueryProvider Provider { get; }

        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Entry point for the SPARQL LINQ provider. Kept separate from the public
    /// <c>IModel.AsQueryable&lt;T&gt;</c> (still on re-linq) until the provider swap (ADR-A4).
    /// </summary>
    internal static class SparqlQueryableExtensions
    {
        public static IQueryable<T> AsSparqlQueryable<T>(this IModel model, bool inferenceEnabled = false) where T : Resource
        {
            return new TrinityQueryable<T>(new SparqlQueryProvider(model, inferenceEnabled));
        }
    }
}
