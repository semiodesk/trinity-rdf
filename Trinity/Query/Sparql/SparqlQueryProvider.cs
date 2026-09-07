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
using System.Globalization;
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

        /// <summary>
        /// Non-null when querying a layered model, in which case every emitted triple pattern is
        /// wrapped in its overlay.
        /// </summary>
        private readonly ILayeredModel _layered;

        public SparqlQueryProvider(IModel model, bool inferenceEnabled)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _inferenceEnabled = inferenceEnabled;
            // Only a rewriting view needs the overlay woven into each pattern. A materialized one is
            // an ordinary graph as far as the writer is concerned, selected by the dataset clause.
            var layered = model as ILayeredModel;

            _layered = layered != null && !layered.IsMaterialized ? layered : null;

            // Interface-level on purpose. Consulting IsMaterialized does not require the concrete
            // class, and testing for it would let a third-party ILayeredModel through with inferencing
            // over a rewritten overlay - the one case ADR-0041 says cannot be approximated.
            if (layered != null)
            {
                LayeredModel.RequireNoInferencing(layered, inferenceEnabled);
            }
        }

        /// <summary>
        /// Serializes a translated query, applying the layered overlay when there is one, and marks
        /// it so a layered model will accept it.
        /// </summary>
        private ISparqlQuery CreateQuery(SparqlQueryModel translated)
        {
            var query = new SparqlQuery(SparqlQueryWriter.Write(translated, _layered)) { Model = _model };

            if (_layered != null)
            {
                query.IsOverlayApplied = true;
            }

            return query;
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

            ISparqlQuery query = CreateQuery(translation.Query);

            switch (translation.Kind)
            {
                case QueryExecutionKind.Ask:
                    bool answer = _model.ExecuteQuery(query, _inferenceEnabled).GetAnwser();
                    return (TResult)(object)(translation.NegateResult ? !answer : answer);

                case QueryExecutionKind.Count:
                    return (TResult)Convert.ChangeType(ExecuteCount(query), typeof(TResult));

                case QueryExecutionKind.Scalar:
                    return (TResult)ExecuteScalar(translation, query, typeof(TResult));

                case QueryExecutionKind.Bindings:
                    IEnumerable values = translation.RowProjector != null
                        ? ExecuteRows(translation, query)
                        : ExecuteBindings(translation, query);
                    return ShapeResult<TResult>(values, translation.Terminal);

                default:
                    IEnumerable resources = InvokeGetResources(translation.ElementType, query);

                    if (translation.MultiplicityQuery != null)
                    {
                        resources = RestoreMultiplicity(translation, resources);
                    }

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

        /// <summary>
        /// Executes a value-projection query and materializes the projected variable of each row
        /// into a typed list. Values are typed by <c>XsdTypeMapper</c> on the way in; mismatches
        /// (e.g. numeric widening) fall back to an invariant-culture conversion.
        /// </summary>
        private IEnumerable ExecuteBindings(QueryTranslation translation, ISparqlQuery query)
        {
            Type elementType = translation.ElementType;

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            foreach (BindingSet bindings in _model.ExecuteQuery(query, _inferenceEnabled).GetBindings())
            {
                object value;

                if (!bindings.TryGetValue(translation.ProjectedVariable, out value))
                {
                    continue;
                }

                if (!elementType.IsInstanceOfType(value))
                {
                    value = Convert.ChangeType(value, elementType, CultureInfo.InvariantCulture);
                }

                list.Add(value);
            }

            return list;
        }

        /// <summary>
        /// Executes an aggregate (SUM/MIN/MAX/AVG) query and converts the single binding value to the
        /// expected result type. An empty sequence yields 0 for SUM/COUNT and throws for the others,
        /// matching LINQ to Objects.
        /// </summary>
        private object ExecuteScalar(QueryTranslation translation, ISparqlQuery query, Type resultType)
        {
            BindingSet bindings = _model.ExecuteQuery(query, _inferenceEnabled).GetBindings().FirstOrDefault();

            object value = null;

            if (bindings != null)
            {
                bindings.TryGetValue(translation.ProjectedVariable, out value);
            }

            if (value == null)
            {
                if (translation.Aggregate == SparqlAggregateKind.Sum || translation.Aggregate == SparqlAggregateKind.Count)
                {
                    value = 0;
                }
                else
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
            }

            return CoerceValue(value, resultType);
        }

        /// <summary>
        /// Executes a multi-column projection query: each row's column values are coerced to their
        /// declared types and shaped into a result element by the translation's row projector.
        /// </summary>
        private IEnumerable ExecuteRows(QueryTranslation translation, ISparqlQuery query)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(translation.ElementType));

            foreach (BindingSet bindings in _model.ExecuteQuery(query, _inferenceEnabled).GetBindings())
            {
                object[] row = new object[translation.ColumnVariables.Length];

                for (int i = 0; i < row.Length; i++)
                {
                    object value;

                    bindings.TryGetValue(translation.ColumnVariables[i], out value);

                    row[i] = value == null
                        ? TypeHelper.GetDefaultValue(translation.ColumnTypes[i])
                        : CoerceValue(value, translation.ColumnTypes[i]);
                }

                list.Add(translation.RowProjector(row));
            }

            return list;
        }

        /// <summary>
        /// Re-expands a materialized (per-resource-unique) result to the multiplicity of the source
        /// rows: the multiplicity query returns the projected variable once per source row, and each
        /// value is mapped back to its materialized resource.
        /// </summary>
        private IEnumerable RestoreMultiplicity(QueryTranslation translation, IEnumerable resources)
        {
            var byUri = new Dictionary<string, object>();

            foreach (object resource in resources)
            {
                byUri[((IResource)resource).Uri.OriginalString] = resource;
            }

            ISparqlQuery query = CreateQuery(translation.MultiplicityQuery);
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(translation.ElementType));

            foreach (BindingSet bindings in _model.ExecuteQuery(query, _inferenceEnabled).GetBindings())
            {
                object value;

                if (!bindings.TryGetValue(translation.SubjectVariable, out value) || !(value is Uri uri))
                {
                    continue;
                }

                object resource;

                if (byUri.TryGetValue(uri.OriginalString, out resource))
                {
                    list.Add(resource);
                }
            }

            return list;
        }

        /// <summary>Coerces a binding value to the expected .NET type (invariant-culture conversion).</summary>
        private static object CoerceValue(object value, Type type)
        {
            if (type.IsInstanceOfType(value))
            {
                return value;
            }

            // A binding carries a plain Uri, but resource identity is UriRef (ADR-0025). Uri is not
            // IConvertible, so ChangeType cannot bridge the two and 'select x.Uri' would fail here.
            if (type == typeof(UriRef) && value is Uri uri)
            {
                return uri.ToUriRef();
            }

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
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
                case TerminalKind.Last:
                    // The translator inverted the orderings, so the last item arrives first.
                    result = items.First();
                    break;
                case TerminalKind.LastOrDefault:
                    result = items.FirstOrDefault();
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
