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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Semiodesk.Trinity.Query.Sparql
{
    /// <summary>How the translated query must be executed against the model.</summary>
    internal enum QueryExecutionKind
    {
        /// <summary>Materialize resources via <c>Model.GetResources&lt;T&gt;</c>.</summary>
        ResourceList,

        /// <summary>Boolean via an <c>ASK</c> query.</summary>
        Ask,

        /// <summary>Scalar count via a <c>COUNT</c> query.</summary>
        Count
    }

    /// <summary>The terminal LINQ operator that shapes the result.</summary>
    internal enum TerminalKind
    {
        Enumerate,
        First,
        FirstOrDefault,
        Single,
        SingleOrDefault
    }

    /// <summary>The result of translating a LINQ expression tree.</summary>
    internal sealed class QueryTranslation
    {
        public SparqlQueryModel Query { get; set; }

        public QueryExecutionKind Kind { get; set; }

        public Type ElementType { get; set; }

        public TerminalKind Terminal { get; set; }
    }

    /// <summary>
    /// Translates a LINQ <see cref="Expression"/> tree (a chain of <see cref="Queryable"/> operator
    /// calls over an <c>AsSparqlQueryable&lt;T&gt;()</c> source) into the owned SPARQL AST plus an
    /// execution kind. Folds operators into a query state, EF-Core-style — no intermediate query model.
    ///
    /// Scope note: this is the vertical-slice exemplar (Where / OfType / OrderBy / Skip / Take /
    /// Any / Count / First / Single over resource queries). Unsupported operators throw
    /// <see cref="NotSupportedException"/>; extending the coverage is the pattern to follow.
    /// </summary>
    internal sealed class SparqlQueryTranslator
    {
        private static readonly VariableTerm Subject = new VariableTerm("s");

        private int _variableCounter;

        private Type _elementType;

        private readonly List<Uri> _typeConstraints = new List<Uri>();

        // Patterns that constrain the selected subject ?s (property bindings introduced by filters/orderings).
        private readonly GroupGraphPattern _subjectPatterns = new GroupGraphPattern();

        private readonly List<SparqlExpression> _filters = new List<SparqlExpression>();

        private readonly List<OrderCondition> _orderings = new List<OrderCondition>();

        private int? _limit;

        private int? _offset;

        private QueryExecutionKind _kind = QueryExecutionKind.ResourceList;

        private TerminalKind _terminal = TerminalKind.Enumerate;

        public QueryTranslation Translate(Expression expression)
        {
            expression = PartialEvaluator.Evaluate(expression);

            VisitChain(expression);

            return Build();
        }

        #region Operator chain

        private void VisitChain(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constant when constant.Value is IQueryable queryable:
                    _elementType = queryable.ElementType;
                    AddTypeConstraints(_elementType);
                    break;

                case MethodCallExpression call when call.Method.DeclaringType == typeof(Queryable):
                    VisitChain(call.Arguments[0]);
                    ApplyOperator(call);
                    break;

                default:
                    throw new NotSupportedException($"Unsupported query expression: {expression.NodeType} ({expression.Type}).");
            }
        }

        private void ApplyOperator(MethodCallExpression call)
        {
            switch (call.Method.Name)
            {
                case "Where":
                    _filters.Add(TranslatePredicate(GetLambda(call.Arguments[1]).Body));
                    break;

                case "OfType":
                    _elementType = call.Method.GetGenericArguments()[0];
                    _typeConstraints.Clear();
                    AddTypeConstraints(_elementType);
                    break;

                case "OrderBy":
                case "ThenBy":
                    _orderings.Add(new OrderCondition(TranslateOrderKey(GetLambda(call.Arguments[1]).Body), false));
                    break;

                case "OrderByDescending":
                case "ThenByDescending":
                    _orderings.Add(new OrderCondition(TranslateOrderKey(GetLambda(call.Arguments[1]).Body), true));
                    break;

                case "Take":
                    _limit = Convert.ToInt32(GetConstant(call.Arguments[1]));
                    break;

                case "Skip":
                    _offset = Convert.ToInt32(GetConstant(call.Arguments[1]));
                    break;

                case "Any":
                    _kind = QueryExecutionKind.Ask;
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(GetLambda(call.Arguments[1]).Body));
                    }
                    break;

                case "Count":
                case "LongCount":
                    _kind = QueryExecutionKind.Count;
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(GetLambda(call.Arguments[1]).Body));
                    }
                    break;

                case "First":
                case "FirstOrDefault":
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(GetLambda(call.Arguments[1]).Body));
                    }
                    _limit = 1;
                    _terminal = call.Method.Name == "First" ? TerminalKind.First : TerminalKind.FirstOrDefault;
                    break;

                case "Single":
                case "SingleOrDefault":
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(GetLambda(call.Arguments[1]).Body));
                    }
                    // Fetch two so the executor can detect a cardinality violation.
                    _limit = 2;
                    _terminal = call.Method.Name == "Single" ? TerminalKind.Single : TerminalKind.SingleOrDefault;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported query operator: {call.Method.Name}.");
            }
        }

        #endregion

        #region Predicate translation

        private SparqlExpression TranslatePredicate(Expression expression)
        {
            expression = Unwrap(expression);

            switch (expression)
            {
                case BinaryExpression binary when binary.NodeType == ExpressionType.AndAlso:
                    return new SparqlBinaryExpression(SparqlBinaryOperator.And, TranslatePredicate(binary.Left), TranslatePredicate(binary.Right));

                case BinaryExpression binary when binary.NodeType == ExpressionType.OrElse:
                    return new SparqlBinaryExpression(SparqlBinaryOperator.Or, TranslatePredicate(binary.Left), TranslatePredicate(binary.Right));

                case BinaryExpression binary:
                    return new SparqlBinaryExpression(MapComparison(binary.NodeType), TranslateOperand(binary.Left), TranslateOperand(binary.Right));

                case UnaryExpression unary when unary.NodeType == ExpressionType.Not:
                    return new SparqlUnaryExpression(SparqlUnaryOperator.Not, TranslatePredicate(unary.Operand));

                case MethodCallExpression call when call.Method.Name == "Equals" && call.Object != null:
                    return new SparqlBinaryExpression(SparqlBinaryOperator.Equal, TranslateOperand(call.Object), TranslateOperand(call.Arguments[0]));

                case MethodCallExpression call:
                    return TranslateStringFunction(call);

                case MemberExpression member:
                    // A bare boolean member used as a predicate, e.g. `where person.Status`.
                    return TranslateOperand(member);

                default:
                    throw new NotSupportedException($"Unsupported predicate expression: {expression.NodeType}.");
            }
        }

        private SparqlExpression TranslateStringFunction(MethodCallExpression call)
        {
            switch (call.Method.Name)
            {
                case "Contains":
                    return new SparqlFunctionExpression("CONTAINS", TranslateOperand(call.Object), TranslateOperand(call.Arguments[0]));
                case "StartsWith":
                    return new SparqlFunctionExpression("STRSTARTS", TranslateOperand(call.Object), TranslateOperand(call.Arguments[0]));
                case "EndsWith":
                    return new SparqlFunctionExpression("STRENDS", TranslateOperand(call.Object), TranslateOperand(call.Arguments[0]));
                default:
                    throw new NotSupportedException($"Unsupported method call in predicate: {call.Method.Name}.");
            }
        }

        private SparqlExpression TranslateOperand(Expression expression)
        {
            expression = Unwrap(expression);

            switch (expression)
            {
                case ConstantExpression constant:
                    return new SparqlConstantExpression(ToTerm(constant.Value));

                case MemberExpression member when IsMappedChain(member):
                    return new SparqlVariableExpression(BindMember(member).Name);

                case MethodCallExpression call when call.Method.Name == "ToLower":
                    return new SparqlFunctionExpression("LCASE", TranslateOperand(call.Object));

                case MethodCallExpression call when call.Method.Name == "ToUpper":
                    return new SparqlFunctionExpression("UCASE", TranslateOperand(call.Object));

                default:
                    throw new NotSupportedException($"Unsupported operand expression: {expression.NodeType}.");
            }
        }

        private SparqlExpression TranslateOrderKey(Expression expression)
        {
            return new SparqlVariableExpression(BindMember((MemberExpression)Unwrap(expression)).Name);
        }

        #endregion

        #region Member binding

        /// <summary>
        /// Binds a mapped-property member access to a fresh variable, emitting the triple pattern(s)
        /// that reach it from the subject. Handles nested access such as <c>person.Group.Name</c>.
        /// </summary>
        private VariableTerm BindMember(MemberExpression member)
        {
            SparqlTerm parent;

            switch (member.Expression)
            {
                case ParameterExpression _:
                    parent = Subject;
                    break;
                case MemberExpression inner:
                    parent = BindMember(inner);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member access root: {member.Expression?.NodeType}.");
            }

            Uri predicate = GetPredicate(member.Member);
            VariableTerm variable = FreshVariable();

            _subjectPatterns.Add(new TriplePattern(parent, new IriTerm(predicate), variable));

            return variable;
        }

        private static bool IsMappedChain(MemberExpression member)
        {
            Expression current = member;

            while (current is MemberExpression m)
            {
                if (GetPredicate(m.Member) == null)
                {
                    return false;
                }

                current = m.Expression;
            }

            return current is ParameterExpression;
        }

        #endregion

        #region Build

        private QueryTranslation Build()
        {
            GroupGraphPattern selection = BuildSubjectSelection();

            SparqlQueryModel query;

            switch (_kind)
            {
                case QueryExecutionKind.Ask:
                    query = new AskQuery { Where = selection };
                    break;

                case QueryExecutionKind.Count:
                    var count = new SelectQuery { Where = selection };
                    count.Projections.Add(new Projection(
                        new VariableTerm("count"),
                        new SparqlAggregateExpression(SparqlAggregateKind.Count, new SparqlVariableExpression(Subject.Name), true)));
                    query = count;
                    break;

                default:
                    query = BuildResourceQuery(selection);
                    break;
            }

            return new QueryTranslation
            {
                Query = query,
                Kind = _kind,
                ElementType = _elementType,
                Terminal = _terminal
            };
        }

        private GroupGraphPattern BuildSubjectSelection()
        {
            var selection = new GroupGraphPattern();

            foreach (Uri type in _typeConstraints)
            {
                selection.Add(new TriplePattern(Subject, RdfTypeTerm.Instance, new IriTerm(type)));
            }

            foreach (GraphPattern pattern in _subjectPatterns.Patterns)
            {
                selection.Add(pattern);
            }

            foreach (SparqlExpression filter in _filters)
            {
                selection.AddFilter(filter);
            }

            return selection;
        }

        /// <summary>
        /// Builds the <c>SELECT ?s ?p ?o WHERE { ?s ?p ?o . { SELECT DISTINCT ?s WHERE { ... } ... } }</c>
        /// shape that <c>Model.GetResources&lt;T&gt;</c> materializes. The subject selection is nested so
        /// ordering and paging apply per resource, not per triple.
        /// </summary>
        private SparqlQueryModel BuildResourceQuery(GroupGraphPattern selection)
        {
            var inner = new SelectQuery { IsDistinct = true, Limit = _limit, Offset = _offset, Where = selection };
            inner.Projections.Add(new Projection(Subject));
            inner.OrderBy.AddRange(_orderings);

            var outer = new SelectQuery();
            outer.Projections.Add(new Projection(Subject));
            outer.Projections.Add(new Projection(new VariableTerm("p")));
            outer.Projections.Add(new Projection(new VariableTerm("o")));
            outer.Where.Add(new TriplePattern(Subject, new VariableTerm("p"), new VariableTerm("o")));
            outer.Where.Add(new SubSelectPattern(inner));

            return outer;
        }

        #endregion

        #region Helpers

        private void AddTypeConstraints(Type type)
        {
            foreach (Uri uri in GetTypeConstraints(type))
            {
                if (!_typeConstraints.Contains(uri))
                {
                    _typeConstraints.Add(uri);
                }
            }
        }

        private static IEnumerable<Uri> GetTypeConstraints(Type type)
        {
            object[] attributes = type.GetCustomAttributes(typeof(RdfClassAttribute), false);

            if (attributes.Length == 0)
            {
                attributes = type.GetCustomAttributes(typeof(RdfClassAttribute), true);
            }

            return attributes.Cast<RdfClassAttribute>().Select(a => a.MappedUri);
        }

        private static Uri GetPredicate(MemberInfo member)
        {
            return (member.GetCustomAttributes(typeof(RdfPropertyAttribute), true).FirstOrDefault() as RdfPropertyAttribute)?.MappedUri;
        }

        private static SparqlTerm ToTerm(object value)
        {
            switch (value)
            {
                case null:
                    throw new NotSupportedException("Null literals are not yet supported.");
                case Uri uri:
                    return new IriTerm(uri);
                case IResource resource:
                    return new IriTerm(resource.Uri);
                case string text:
                    // Stored as a plain literal; emit plain so term equality matches.
                    return new LiteralTerm(text);
                default:
                    return new LiteralTerm(XsdTypeMapper.SerializeObject(value), XsdTypeMapper.GetXsdTypeUri(value.GetType()));
            }
        }

        private VariableTerm FreshVariable()
        {
            return new VariableTerm("v" + _variableCounter++);
        }

        private static SparqlBinaryOperator MapComparison(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal: return SparqlBinaryOperator.Equal;
                case ExpressionType.NotEqual: return SparqlBinaryOperator.NotEqual;
                case ExpressionType.LessThan: return SparqlBinaryOperator.LessThan;
                case ExpressionType.LessThanOrEqual: return SparqlBinaryOperator.LessThanOrEqual;
                case ExpressionType.GreaterThan: return SparqlBinaryOperator.GreaterThan;
                case ExpressionType.GreaterThanOrEqual: return SparqlBinaryOperator.GreaterThanOrEqual;
                default: throw new NotSupportedException($"Unsupported comparison: {type}.");
            }
        }

        private static LambdaExpression GetLambda(Expression expression)
        {
            return (LambdaExpression)Unwrap(expression);
        }

        private static object GetConstant(Expression expression)
        {
            return ((ConstantExpression)Unwrap(expression)).Value;
        }

        /// <summary>Strips <c>Quote</c> and identity <c>Convert</c> wrappers.</summary>
        private static Expression Unwrap(Expression expression)
        {
            while (expression is UnaryExpression unary &&
                   (unary.NodeType == ExpressionType.Quote || unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unary.Operand;
            }

            return expression;
        }

        #endregion
    }
}
