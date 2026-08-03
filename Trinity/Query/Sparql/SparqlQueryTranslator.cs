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
using System.Text.RegularExpressions;

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
        Count,

        /// <summary>Literal values via <c>GetBindings</c> (member value projections).</summary>
        Bindings,

        /// <summary>A single aggregate value (SUM/MIN/MAX/AVG) read from one binding row.</summary>
        Scalar
    }

    /// <summary>The terminal LINQ operator that shapes the result.</summary>
    internal enum TerminalKind
    {
        Enumerate,
        First,
        FirstOrDefault,
        Single,
        SingleOrDefault,
        Last,
        LastOrDefault
    }

    /// <summary>The result of translating a LINQ expression tree.</summary>
    internal sealed class QueryTranslation
    {
        public SparqlQueryModel Query { get; set; }

        public QueryExecutionKind Kind { get; set; }

        public Type ElementType { get; set; }

        public TerminalKind Terminal { get; set; }

        /// <summary>For <see cref="QueryExecutionKind.Bindings"/>: the SELECT variable holding the projected value.</summary>
        public string ProjectedVariable { get; set; }

        /// <summary>For multi-column (client-shaped) projections: the SELECT variable of each column, in order.</summary>
        public string[] ColumnVariables { get; set; }

        /// <summary>For multi-column projections: the .NET type each column value is coerced to.</summary>
        public Type[] ColumnTypes { get; set; }

        /// <summary>For multi-column projections: shapes one row of column values into a result element.</summary>
        public Func<object[], object> RowProjector { get; set; }

        /// <summary>For <see cref="QueryExecutionKind.Scalar"/>: the aggregate kind (decides the empty-sequence behavior).</summary>
        public SparqlAggregateKind? Aggregate { get; set; }

        /// <summary>For <see cref="QueryExecutionKind.Ask"/>: invert the answer (used by <c>All</c> = !Any(!pred)).</summary>
        public bool NegateResult { get; set; }

        /// <summary>
        /// For resource queries whose result is a projected variable (not the query source): a SELECT
        /// returning that variable once per source row, used to restore result multiplicity after
        /// <c>GetResources</c> (which materializes each resource once).
        /// </summary>
        public SparqlQueryModel MultiplicityQuery { get; set; }

        /// <summary>The variable projected by <see cref="MultiplicityQuery"/>.</summary>
        public string SubjectVariable { get; set; }
    }

    /// <summary>
    /// Translates a LINQ <see cref="Expression"/> tree (a chain of <see cref="Queryable"/> operator
    /// calls over an <c>AsSparqlQueryable&lt;T&gt;()</c> source) into the owned SPARQL AST plus an
    /// execution kind. Folds operators into a query state, EF-Core-style — no intermediate query model.
    ///
    /// Unbound-member semantics (parity with the re-linq provider): mapped members without a triple in
    /// the store behave as holding <c>default(T)</c>. Equality comparisons that would match the default
    /// bind the member with <c>OPTIONAL</c> and include unbound values via <c>!BOUND</c>; value
    /// projections <c>COALESCE</c> the missing binding to the default. Negations are normalized
    /// (<c>!(a == b)</c> → <c>a != b</c>) so the correct bound/unbound branch is chosen.
    /// Unsupported operators throw <see cref="NotSupportedException"/>.
    /// </summary>
    internal sealed class SparqlQueryTranslator
    {
        private static readonly VariableTerm Subject = new VariableTerm("s");

        private int _variableCounter;

        // Prefix for generated variable names; set on sub-translators (set-operation operands) so
        // both operands can be merged into one query without variable collisions.
        private readonly string _variablePrefix;

        private int _setOperandCounter;

        private Type _elementType;

        private readonly List<Uri> _typeConstraints = new List<Uri>();

        // Patterns that constrain the selected subject ?s (property bindings introduced by filters/orderings).
        private readonly GroupGraphPattern _subjectPatterns = new GroupGraphPattern();

        private readonly List<SparqlExpression> _filters = new List<SparqlExpression>();

        private readonly List<OrderCondition> _orderings = new List<OrderCondition>();

        private int? _limit;

        private int? _offset;

        private bool _distinct;

        private QueryExecutionKind _kind = QueryExecutionKind.ResourceList;

        private TerminalKind _terminal = TerminalKind.Enumerate;

        // The lambda-parameter binding scope for the query source; child scopes are created for
        // sub-query lambdas (e.g. the parameter of an Any predicate inside an EXISTS group).
        // Replaced (subject swap) when a projection or SelectMany makes another variable the result:
        // subsequent operators then bind against the projected variable.
        private QueryScope _rootScope;

        // Scopes for synthetic parameters introduced by a transparent identifier (see
        // ApplySelectMany): they are not part of the _rootScope parent chain.
        private readonly Dictionary<ParameterExpression, QueryScope> _extraScopes =
            new Dictionary<ParameterExpression, QueryScope>();

        // Transparent identifiers by anonymous type: member name -> synthetic parameter.
        private readonly Dictionary<Type, Dictionary<string, ParameterExpression>> _transparentIdentifiers =
            new Dictionary<Type, Dictionary<string, ParameterExpression>>();

        // Set by a SelectMany with a result selector: the result is the source, but one row per
        // (source, element) pair, so per-row multiplicity must be restored.
        private bool _forceMultiplicity;

        // For value projections (Bindings): the member binding whose variable is projected.
        private MemberBinding _projection;

        // For aggregate terminals (Sum/Min/Max/Average) over the value projection.
        private SparqlAggregateKind? _aggregate;

        // For client-shaped projections (computed/anonymous/grouped): columns + row projector.
        private ClientProjection _clientProjection;

        // For All(pred): the ASK looks for a violating resource and the provider inverts the answer.
        private bool _negateResult;

        // GroupBy state: the (coalesced) grouping key variable and its .NET type.
        private bool _isGrouped;

        private VariableTerm _groupKeyVariable;

        private Type _groupKeyType;

        public SparqlQueryTranslator() : this(string.Empty)
        {
        }

        private SparqlQueryTranslator(string variablePrefix)
        {
            _variablePrefix = variablePrefix;
            _rootScope = new QueryScope(null, Subject, _subjectPatterns, null);
        }

        public QueryTranslation Translate(Expression expression)
        {
            expression = PartialEvaluator.Evaluate(expression);

            VisitChain(expression);

            return Build();
        }

        #region Scopes and member bindings

        /// <summary>A lambda-parameter scope mapping a parameter to a subject term and a pattern sink.</summary>
        private sealed class QueryScope
        {
            /// <summary>The lambda parameter owning this scope; <c>null</c> for the root scope, which matches any parameter.</summary>
            public ParameterExpression Parameter { get; }

            public VariableTerm Subject { get; }

            public GroupGraphPattern Patterns { get; }

            public QueryScope Parent { get; }

            /// <summary>Member bindings keyed by predicate-URI path, so filters/orderings/projections share variables.</summary>
            public Dictionary<string, MemberBinding> Bindings { get; } = new Dictionary<string, MemberBinding>();

            public QueryScope(ParameterExpression parameter, VariableTerm subject, GroupGraphPattern patterns, QueryScope parent)
            {
                Parameter = parameter;
                Subject = subject;
                Patterns = patterns;
                Parent = parent;
            }
        }

        /// <summary>A member access bound to a variable via triple patterns in a scope.</summary>
        private sealed class MemberBinding
        {
            public VariableTerm Variable { get; set; }

            public bool IsOptional { get; set; }

            public Type MemberType { get; set; }

            /// <summary>The final-step triple pattern (null for computed bindings such as counts).</summary>
            public TriplePattern Triple { get; set; }

            /// <summary>The group the triple was added to (for upgrading to OPTIONAL).</summary>
            public GroupGraphPattern Container { get; set; }
        }

        private QueryScope ResolveScope(QueryScope scope, ParameterExpression parameter)
        {
            for (QueryScope current = scope; current != null; current = current.Parent)
            {
                if (current.Parameter == parameter)
                {
                    return current;
                }
            }

            QueryScope extra;

            if (parameter != null && _extraScopes.TryGetValue(parameter, out extra))
            {
                return extra;
            }

            return _rootScope;
        }

        /// <summary>
        /// Binds a mapped-property member chain to a variable, emitting the triple pattern(s) that
        /// reach it from the scope subject. Parent steps of a chain are always mandatory; only the
        /// final step is wrapped in <c>OPTIONAL</c> when requested. Bindings are cached per scope so
        /// filters, orderings and projections on the same member share one variable.
        /// </summary>
        private MemberBinding BindChain(QueryScope scope, MemberExpression member, bool optional)
        {
            ParameterExpression root = GetRootParameter(member);

            if (root == null)
            {
                throw new NotSupportedException($"Unsupported member access root: {member}.");
            }

            QueryScope owner = ResolveScope(scope, root);
            string key = PathKey(member);

            MemberBinding binding;

            if (owner.Bindings.TryGetValue(key, out binding))
            {
                if (optional && !binding.IsOptional)
                {
                    UpgradeToOptional(binding);
                }

                return binding;
            }

            SparqlTerm parent;
            Expression inner = Unwrap(member.Expression);

            switch (inner)
            {
                case ParameterExpression _:
                    parent = owner.Subject;
                    break;
                case MemberExpression innerMember:
                    parent = BindChain(scope, innerMember, false).Variable;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member access root: {inner?.NodeType}.");
            }

            Uri predicate = GetPredicate(member);

            if (predicate == null)
            {
                throw new NotSupportedException($"Member is not mapped to an RDF property: {member.Member.Name}.");
            }

            VariableTerm variable = FreshVariable();
            var triple = new TriplePattern(parent, new IriTerm(predicate), variable);

            binding = new MemberBinding
            {
                Variable = variable,
                MemberType = GetMemberType(member.Member),
                Triple = triple,
                Container = owner.Patterns
            };

            if (optional)
            {
                var group = new GroupGraphPattern();
                group.Add(triple);

                binding.IsOptional = true;
                owner.Patterns.Add(new OptionalPattern(group));
            }
            else
            {
                owner.Patterns.Add(triple);
            }

            owner.Bindings[key] = binding;

            return binding;
        }

        /// <summary>
        /// Re-wraps an already-emitted mandatory binding in <c>OPTIONAL</c>. Safe for existing plain
        /// filters: they evaluate to an error (= false) on unbound values, which matches the join.
        /// </summary>
        private static void UpgradeToOptional(MemberBinding binding)
        {
            var group = new GroupGraphPattern();
            group.Add(binding.Triple);

            int index = binding.Container.Patterns.IndexOf(binding.Triple);
            var optional = new OptionalPattern(group);

            if (index >= 0)
            {
                binding.Container.Patterns[index] = optional;
            }
            else
            {
                binding.Container.Add(optional);
            }

            binding.IsOptional = true;
        }

        /// <summary>
        /// Binds a mapped collection member's element count to a variable via a correlated sub-select:
        /// <c>{ SELECT ?s (COUNT(?x) AS ?c) WHERE { ?s a &lt;Type&gt; . OPTIONAL { ?s &lt;p&gt; ?x } } GROUP BY ?s }</c>.
        /// The member is OPTIONAL and the type pattern is inside, so resources without elements count as zero.
        /// </summary>
        private MemberBinding BindCount(QueryScope scope, MemberExpression member, Type elementType = null)
        {
            if (!(Unwrap(member.Expression) is ParameterExpression parameter))
            {
                throw new NotSupportedException($"Unsupported collection count: {member}.");
            }

            QueryScope owner = ResolveScope(scope, parameter);

            if (owner != _rootScope)
            {
                throw new NotSupportedException("Collection counts are only supported on the query source.");
            }

            string key = PathKey(member) + "#count" + (elementType != null ? ":" + elementType.FullName : "");

            MemberBinding binding;

            if (owner.Bindings.TryGetValue(key, out binding))
            {
                return binding;
            }

            VariableTerm item = FreshVariable();
            VariableTerm count = FreshVariable();

            var inner = new SelectQuery();
            inner.Projections.Add(new Projection(owner.Subject));
            inner.Projections.Add(new Projection(count, new SparqlAggregateExpression(SparqlAggregateKind.Count, new SparqlVariableExpression(item.Name))));

            foreach (Uri type in _typeConstraints)
            {
                inner.Where.Add(new TriplePattern(owner.Subject, RdfTypeTerm.Instance, new IriTerm(type)));
            }

            var optional = new GroupGraphPattern();
            optional.Add(new TriplePattern(owner.Subject, new IriTerm(GetPredicate(member)), item));

            if (elementType != null)
            {
                // OfType<T> over the collection: only elements carrying T's class(es) are counted.
                foreach (Uri type in GetTypeConstraints(elementType))
                {
                    optional.Add(new TriplePattern(item, RdfTypeTerm.Instance, new IriTerm(type)));
                }
            }

            inner.Where.Add(new OptionalPattern(optional));
            inner.GroupBy.Add(new SparqlVariableExpression(owner.Subject.Name));

            owner.Patterns.Add(new SubSelectPattern(inner));

            binding = new MemberBinding { Variable = count, MemberType = typeof(int) };
            owner.Bindings[key] = binding;

            return binding;
        }

        #endregion

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
            if (_isGrouped && !(call.Method.Name == "Select" && _clientProjection == null))
            {
                throw new NotSupportedException($"Unsupported query operator after GroupBy: {call.Method.Name}.");
            }

            switch (call.Method.Name)
            {
                case "Where":
                    if (_kind == QueryExecutionKind.Bindings)
                    {
                        throw new NotSupportedException("Filtering after a value projection is not supported.");
                    }
                    _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, false));
                    break;

                case "OfType":
                    _elementType = call.Method.GetGenericArguments()[0];
                    _typeConstraints.Clear();
                    AddTypeConstraints(_elementType);
                    break;

                case "Select":
                    ApplySelect(call);
                    break;

                case "SelectMany":
                    ApplySelectMany(call);
                    break;

                case "Distinct":
                    if (call.Arguments.Count != 1)
                    {
                        throw new NotSupportedException("Distinct with a custom comparer is not supported.");
                    }
                    if (_clientProjection != null)
                    {
                        // A server-side DISTINCT over the raw columns is not equivalent to a client-side
                        // Distinct over the computed results (the projector may collapse distinct rows).
                        throw new NotSupportedException("Distinct over a computed projection is not supported.");
                    }
                    _distinct = true;
                    break;

                case "GroupBy":
                    ApplyGroupBy(call);
                    break;

                case "Union":
                case "Concat":
                case "Intersect":
                case "Except":
                    ApplySetOperation(call);
                    break;

                case "OrderBy":
                case "ThenBy":
                    _orderings.Add(new OrderCondition(TranslateOrderKey(GetLambda(call.Arguments[1])), false));
                    break;

                case "OrderByDescending":
                case "ThenByDescending":
                    _orderings.Add(new OrderCondition(TranslateOrderKey(GetLambda(call.Arguments[1])), true));
                    break;

                case "Take":
                    // Take(n).Take(m) narrows; Skip(n).Take(m) maps to OFFSET n LIMIT m.
                    int take = Convert.ToInt32(GetConstant(call.Arguments[1]));
                    _limit = _limit.HasValue ? Math.Min(_limit.Value, take) : take;
                    break;

                case "Skip":
                    if (_limit.HasValue)
                    {
                        // Take(n).Skip(m) skips within the taken window — needs a nested sub-select.
                        throw new NotSupportedException("Skip after Take is not supported.");
                    }
                    _offset = (_offset ?? 0) + Convert.ToInt32(GetConstant(call.Arguments[1]));
                    break;

                case "Sum":
                case "Min":
                case "Max":
                case "Average":
                    ApplyAggregate(call);
                    break;

                case "All":
                    ApplyAll(call);
                    break;

                case "Any":
                    _kind = QueryExecutionKind.Ask;
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, false));
                    }
                    break;

                case "Count":
                case "LongCount":
                    if (_rootScope.Subject != Subject)
                    {
                        // The result is a projected variable; COUNT(DISTINCT) would drop the duplicates
                        // LINQ counts.
                        throw new NotSupportedException("Count over a projected resource sequence is not supported.");
                    }
                    _kind = QueryExecutionKind.Count;
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, false));
                    }
                    break;

                case "First":
                case "FirstOrDefault":
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, false));
                    }
                    _limit = 1;
                    _terminal = call.Method.Name == "First" ? TerminalKind.First : TerminalKind.FirstOrDefault;
                    break;

                case "Last":
                case "LastOrDefault":
                    // Shaped like First over inverted orderings (flipped in Build).
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, false));
                    }
                    _limit = 1;
                    _terminal = call.Method.Name == "Last" ? TerminalKind.Last : TerminalKind.LastOrDefault;
                    break;

                case "Single":
                case "SingleOrDefault":
                    if (call.Arguments.Count == 2)
                    {
                        _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, false));
                    }
                    // Fetch two so the executor can detect a cardinality violation.
                    _limit = 2;
                    _terminal = call.Method.Name == "Single" ? TerminalKind.Single : TerminalKind.SingleOrDefault;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported query operator: {call.Method.Name}.");
            }
        }

        private void ApplySelect(MethodCallExpression call)
        {
            LambdaExpression lambda = GetLambda(call.Arguments[1]);

            if (_isGrouped)
            {
                ApplyGroupSelect(lambda);
                return;
            }

            Expression body = Unwrap(lambda.Body);

            if (body == lambda.Parameters[0]
                || (body is ParameterExpression alias && ResolveScope(_rootScope, alias).Subject == _rootScope.Subject))
            {
                // Identity projection: the query still yields the source resources. The second form is
                // a transparent-identifier alias for the source (`select user` after a `from ... from`).
                return;
            }

            ChainInfo chain = TryGetChain(body);

            if (chain == null)
            {
                // Not a plain member chain: computed or composite (anonymous) projection.
                ApplyClientSelect(lambda);
                return;
            }

            switch (chain.Kind)
            {
                case ChainKind.Count:
                    _projection = BindCount(_rootScope, chain.Chain, chain.ElementType);
                    _kind = QueryExecutionKind.Bindings;
                    _elementType = typeof(int);
                    break;

                case ChainKind.Value when typeof(IResource).IsAssignableFrom(chain.MemberType):
                {
                    // Resource-valued member: still a resource query, but the member variable becomes
                    // the result subject. The root scope is swapped so later operators bind against it.
                    MemberBinding member = BindChain(_rootScope, chain.Chain, false);

                    _rootScope = new QueryScope(null, member.Variable, _subjectPatterns, null);
                    _elementType = chain.MemberType;
                    break;
                }

                case ChainKind.Value when IsScalarType(chain.MemberType):
                {
                    string key = PathKey(chain.Chain);
                    MemberBinding binding;

                    if (!_rootScope.Bindings.TryGetValue(key, out binding))
                    {
                        // An unconstrained value-type projection must also yield rows for resources
                        // without the property (as default(T)), so it binds optionally; strings bind
                        // mandatorily (parity with the re-linq provider).
                        bool optional = chain.MemberType.IsValueType && chain.MemberType != typeof(string);

                        binding = BindChain(_rootScope, chain.Chain, optional);
                    }

                    _projection = binding;
                    _kind = QueryExecutionKind.Bindings;
                    _elementType = chain.MemberType;
                    break;
                }

                default:
                    // Uri / Length / other decorated chains are handled as one-column client projections.
                    ApplyClientSelect(lambda);
                    break;
            }
        }

        /// <summary>
        /// Translates a computed or composite projection (e.g. <c>p.FirstName + "!"</c>,
        /// <c>new { p.FirstName, p.Age }</c>): every mapped member chain in the body becomes a
        /// projected column, and the remaining expression is compiled into a client-side row
        /// projector evaluated over the column values.
        /// </summary>
        private void ApplyClientSelect(LambdaExpression lambda)
        {
            if (_kind == QueryExecutionKind.Bindings)
            {
                throw new NotSupportedException($"Unsupported projection: {lambda.Body}.");
            }

            var projection = new ClientProjection();
            ParameterExpression row = Expression.Parameter(typeof(object[]), "row");

            Expression rewritten = new ClientProjectionRewriter(this, row, projection).Visit(lambda.Body);

            if (projection.Columns.Count == 0 || ReferencesParameter(rewritten, lambda.Parameters[0]))
            {
                // The body uses the resource itself (or nothing bindable) — cannot be shaped from rows.
                throw new NotSupportedException($"Unsupported projection: {lambda.Body}.");
            }

            projection.Projector = Expression.Lambda<Func<object[], object>>(
                Expression.Convert(rewritten, typeof(object)), row).Compile();

            _clientProjection = projection;
            _kind = QueryExecutionKind.Bindings;
            _elementType = lambda.Body.Type;
        }

        /// <summary>
        /// Translates the projection following a <c>GroupBy</c>. The selector may use the grouping
        /// parameter only as <c>g.Key</c> or <c>g.Count()</c>; those become the grouped SELECT's
        /// columns (key / COUNT aggregate) and the rest of the body runs client-side per row.
        /// </summary>
        private void ApplyGroupSelect(LambdaExpression lambda)
        {
            var projection = new ClientProjection();
            ParameterExpression row = Expression.Parameter(typeof(object[]), "row");

            Expression rewritten = new GroupSelectRewriter(lambda.Parameters[0], row, projection, _groupKeyType).Visit(lambda.Body);

            if (projection.Columns.Count == 0 || ReferencesParameter(rewritten, lambda.Parameters[0]))
            {
                throw new NotSupportedException(
                    $"Unsupported group projection: {lambda.Body}. Only g.Key and g.Count() are supported.");
            }

            projection.Projector = Expression.Lambda<Func<object[], object>>(
                Expression.Convert(rewritten, typeof(object)), row).Compile();

            _clientProjection = projection;
            _kind = QueryExecutionKind.Bindings;
            _elementType = lambda.Body.Type;
        }

        /// <summary>
        /// Translates <c>SelectMany(p => p.Collection)</c>: the collection elements become the query
        /// result — a fresh variable bound via the collection predicate replaces the root subject.
        /// Duplicates are preserved for value projections; a resource-valued result is restored to
        /// source-row multiplicity by the provider (see <see cref="QueryTranslation.MultiplicityQuery"/>).
        /// </summary>
        private void ApplySelectMany(MethodCallExpression call)
        {
            if (call.Arguments.Count != 2 && call.Arguments.Count != 3)
            {
                throw new NotSupportedException($"Unsupported SelectMany overload ({call.Arguments.Count} arguments).");
            }

            if (_kind != QueryExecutionKind.ResourceList || _distinct || _limit.HasValue || _offset.HasValue || _orderings.Count > 0)
            {
                throw new NotSupportedException("SelectMany is only supported directly on a resource query.");
            }

            LambdaExpression lambda = GetLambda(call.Arguments[1]);

            if (!(Unwrap(lambda.Body) is MemberExpression member) || !IsMappedChain(member))
            {
                throw new NotSupportedException($"Unsupported SelectMany source: {lambda.Body}.");
            }

            SparqlTerm parent;
            Expression inner = Unwrap(member.Expression);

            switch (inner)
            {
                case ParameterExpression parameter:
                    parent = ResolveScope(_rootScope, parameter).Subject;
                    break;
                case MemberExpression innerMember:
                    parent = BindChain(_rootScope, innerMember, false).Variable;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member access root: {inner?.NodeType}.");
            }

            VariableTerm element = FreshVariable();

            _subjectPatterns.Add(new TriplePattern(parent, new IriTerm(GetPredicate(member)), element));

            if (call.Arguments.Count == 3)
            {
                // `from a in source from b in a.Collection ...` adds a result selector over both. The
                // compiler emits the selected parameter directly when the query ends in `select a`/
                // `select b`, and an anonymous "transparent identifier" when more clauses follow.
                LambdaExpression resultSelector = GetLambda(call.Arguments[2]);
                Expression result = Unwrap(resultSelector.Body);

                if (result == resultSelector.Parameters[1])
                {
                    // The element is the result — same shape as the two-argument overload.
                    _rootScope = new QueryScope(null, element, _subjectPatterns, null);
                    _elementType = resultSelector.Parameters[1].Type;
                    return;
                }

                if (result != resultSelector.Parameters[0])
                {
                    RegisterTransparentIdentifier(resultSelector, element);
                }

                // The source stays the result, but there is one row per (source, element) pair, so
                // per-row multiplicity has to be restored after materialization.
                _forceMultiplicity = true;
                return;
            }

            // The collection element replaces the source as the root subject.
            _rootScope = new QueryScope(null, element, _subjectPatterns, null);
            _elementType = call.Method.GetGenericArguments()[1];
        }

        /// <summary>
        /// Registers the transparent identifier a <c>SelectMany</c> result selector introduces: the
        /// anonymous type's members alias the source and the collection element. Each member gets a
        /// synthetic parameter — the element's is scoped to the element variable, the source's falls
        /// back to the root scope — and later lambdas are rewritten onto them.
        /// </summary>
        private void RegisterTransparentIdentifier(LambdaExpression resultSelector, VariableTerm element)
        {
            if (!(Unwrap(resultSelector.Body) is NewExpression projection)
                || projection.Members == null
                || projection.Members.Count != projection.Arguments.Count
                || resultSelector.Parameters.Count != 2)
            {
                throw new NotSupportedException(
                    "Only the compiler-generated SelectMany result selector (an anonymous type over the source and the element) is supported.");
            }

            var members = new Dictionary<string, ParameterExpression>();

            for (int i = 0; i < projection.Arguments.Count; i++)
            {
                if (!(Unwrap(projection.Arguments[i]) is ParameterExpression argument))
                {
                    throw new NotSupportedException("Only a SelectMany result selector that packs its parameters unchanged is supported.");
                }

                ParameterExpression alias = Expression.Parameter(argument.Type, projection.Members[i].Name);

                members[projection.Members[i].Name] = alias;

                if (argument == resultSelector.Parameters[1])
                {
                    _extraScopes[alias] = new QueryScope(alias, element, _subjectPatterns, null);
                }
            }

            _transparentIdentifiers[resultSelector.Body.Type] = members;
        }

        /// <summary>
        /// Translates an aggregate terminal (<c>Sum</c>/<c>Min</c>/<c>Max</c>/<c>Average</c>) over a
        /// value projection into a single-row aggregate SELECT. An optionally-bound member is
        /// aggregated over its COALESCEd value so the result agrees with the projected sequence
        /// (unbound members count as default(T)).
        /// </summary>
        private void ApplyAggregate(MethodCallExpression call)
        {
            if (call.Arguments.Count == 2)
            {
                // The selector overload folds like a preceding Select.
                ApplySelect(call);
            }

            if (_kind != QueryExecutionKind.Bindings || _projection == null || _clientProjection != null)
            {
                throw new NotSupportedException($"{call.Method.Name} is only supported over a projected member value.");
            }

            switch (call.Method.Name)
            {
                case "Sum": _aggregate = SparqlAggregateKind.Sum; break;
                case "Min": _aggregate = SparqlAggregateKind.Min; break;
                case "Max": _aggregate = SparqlAggregateKind.Max; break;
                default: _aggregate = SparqlAggregateKind.Average; break;
            }

            _kind = QueryExecutionKind.Scalar;
            _elementType = call.Method.ReturnType;
        }

        /// <summary>
        /// Translates <c>All(pred)</c> as the negation of <c>Any(!pred)</c>: an ASK for a violating
        /// resource whose answer the provider inverts. This keeps the ASK group non-empty (engines
        /// disagree on filters over an empty group) and an empty selection correctly yields true.
        /// </summary>
        private void ApplyAll(MethodCallExpression call)
        {
            if (_kind != QueryExecutionKind.ResourceList)
            {
                throw new NotSupportedException("All is only supported on a resource query.");
            }

            _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, true));

            _kind = QueryExecutionKind.Ask;
            _negateResult = true;
        }

        /// <summary>
        /// Translates <c>GroupBy(keySelector)</c>: binds the key member (optionally for value types,
        /// with a COALESCE-to-default BIND, so unbound members group under default(T)) and records the
        /// grouping state. The following Select shapes the result; enumerating groups is unsupported.
        /// </summary>
        private void ApplyGroupBy(MethodCallExpression call)
        {
            if (call.Arguments.Count != 2)
            {
                throw new NotSupportedException("GroupBy is only supported with a single key selector.");
            }

            if (_kind != QueryExecutionKind.ResourceList || _distinct || _limit.HasValue || _offset.HasValue || _orderings.Count > 0)
            {
                throw new NotSupportedException("GroupBy is only supported directly on a resource query.");
            }

            ChainInfo chain = TryGetChain(GetLambda(call.Arguments[1]).Body);

            if (chain == null || chain.Kind != ChainKind.Value || !IsScalarType(chain.MemberType))
            {
                throw new NotSupportedException("GroupBy is only supported on a mapped literal-valued member.");
            }

            bool optional = chain.MemberType.IsValueType && chain.MemberType != typeof(string);
            MemberBinding binding = BindChain(_rootScope, chain.Chain, optional);

            if (optional)
            {
                // Group under the coalesced value so resources without the member fall into default(T).
                VariableTerm key = FreshVariable();

                _subjectPatterns.Add(new BindPattern(
                    Coalesce(binding.Variable, chain.MemberType), key));

                _groupKeyVariable = key;
            }
            else
            {
                _groupKeyVariable = binding.Variable;
            }

            _groupKeyType = chain.MemberType;
            _isGrouped = true;
        }

        /// <summary>
        /// Translates a set operation (<c>Union</c>/<c>Concat</c>/<c>Intersect</c>/<c>Except</c>).
        /// The right operand is translated by a sub-translator with a distinct variable namespace but
        /// the same subject variable, so both selections correlate on <c>?s</c>: Concat/Union become a
        /// UNION (Union additionally DISTINCT), Intersect/Except become FILTER (NOT) EXISTS.
        /// </summary>
        private void ApplySetOperation(MethodCallExpression call)
        {
            if (call.Arguments.Count != 2)
            {
                throw new NotSupportedException($"{call.Method.Name} with a custom comparer is not supported.");
            }

            if (_kind != QueryExecutionKind.ResourceList || _limit.HasValue || _offset.HasValue
                || _orderings.Count > 0 || _rootScope.Subject != Subject)
            {
                throw new NotSupportedException($"{call.Method.Name} is only supported between two resource queries.");
            }

            if (_distinct && call.Method.Name == "Concat")
            {
                throw new NotSupportedException("Concat after Distinct is not supported.");
            }

            GroupGraphPattern right = TranslateSetOperand(call.Arguments[1]);

            switch (call.Method.Name)
            {
                case "Intersect":
                    _filters.Add(new SparqlExistsExpression(right, false));
                    _distinct = true;
                    break;

                case "Except":
                    _filters.Add(new SparqlExistsExpression(right, true));
                    _distinct = true;
                    break;

                default: // Union, Concat
                {
                    GroupGraphPattern left = BuildSubjectSelection();

                    ResetSelection();

                    _subjectPatterns.Add(new UnionPattern(left, right));

                    if (call.Method.Name == "Union")
                    {
                        _distinct = true;
                    }
                    break;
                }
            }
        }

        /// <summary>Translates the second queryable of a set operation into its subject selection.</summary>
        private GroupGraphPattern TranslateSetOperand(Expression expression)
        {
            var sub = new SparqlQueryTranslator(_variablePrefix + "r" + _setOperandCounter++ + "_");

            sub.VisitChain(expression);

            if (sub._kind != QueryExecutionKind.ResourceList || sub._distinct || sub._limit.HasValue
                || sub._offset.HasValue || sub._orderings.Count > 0 || sub._rootScope.Subject != Subject
                || sub._isGrouped)
            {
                throw new NotSupportedException("Set operations are only supported between plain resource queries.");
            }

            return sub.BuildSubjectSelection();
        }

        /// <summary>Clears the accumulated selection state after it has been folded into a nested pattern.</summary>
        private void ResetSelection()
        {
            _typeConstraints.Clear();
            _subjectPatterns.Patterns.Clear();
            _filters.Clear();
            _rootScope.Bindings.Clear();
        }

        #endregion

        #region Predicate translation

        /// <summary>
        /// Translates a boolean predicate expression, normalizing negation: <c>Not</c> flips
        /// <paramref name="negated"/>, equality operators are inverted rather than wrapped in
        /// <c>!(...)</c>, so the unbound-member semantics pick the correct branch.
        /// <paramref name="inDisjunction"/> marks operands of a (possibly negation-induced) <c>||</c>:
        /// their member bindings must be OPTIONAL so a resource missing the member can still match
        /// through the other branch (the comparison itself errors to false on unbound values).
        /// </summary>
        private SparqlExpression TranslatePredicate(QueryScope scope, Expression expression, bool negated, bool inDisjunction = false)
        {
            expression = Unwrap(expression);

            switch (expression)
            {
                case TypeBinaryExpression typeBinary when typeBinary.NodeType == ExpressionType.TypeIs:
                    return TranslateTypeIs(scope, typeBinary, negated);

                case BinaryExpression binary when binary.NodeType == ExpressionType.AndAlso:
                {
                    bool disjunctive = inDisjunction || negated;

                    return new SparqlBinaryExpression(
                        negated ? SparqlBinaryOperator.Or : SparqlBinaryOperator.And,
                        TranslatePredicate(scope, binary.Left, negated, disjunctive),
                        TranslatePredicate(scope, binary.Right, negated, disjunctive));
                }

                case BinaryExpression binary when binary.NodeType == ExpressionType.OrElse:
                {
                    bool disjunctive = inDisjunction || !negated;

                    return new SparqlBinaryExpression(
                        negated ? SparqlBinaryOperator.And : SparqlBinaryOperator.Or,
                        TranslatePredicate(scope, binary.Left, negated, disjunctive),
                        TranslatePredicate(scope, binary.Right, negated, disjunctive));
                }

                case BinaryExpression binary when binary.NodeType == ExpressionType.Equal || binary.NodeType == ExpressionType.NotEqual:
                {
                    ExpressionType op = binary.NodeType;

                    if (negated)
                    {
                        op = op == ExpressionType.Equal ? ExpressionType.NotEqual : ExpressionType.Equal;
                    }

                    return TranslateComparison(scope, op, binary.Left, binary.Right, inDisjunction);
                }

                case BinaryExpression binary when IsComparison(binary.NodeType):
                {
                    SparqlExpression comparison = TranslateComparison(scope, binary.NodeType, binary.Left, binary.Right, inDisjunction);

                    return negated ? new SparqlUnaryExpression(SparqlUnaryOperator.Not, comparison) : comparison;
                }

                case UnaryExpression unary when unary.NodeType == ExpressionType.Not:
                    return TranslatePredicate(scope, unary.Operand, !negated, inDisjunction);

                case MethodCallExpression callExpression when callExpression.Method.Name == "Equals" && callExpression.Object != null && callExpression.Arguments.Count == 1:
                    return TranslateComparison(
                        scope,
                        negated ? ExpressionType.NotEqual : ExpressionType.Equal,
                        callExpression.Object,
                        callExpression.Arguments[0],
                        inDisjunction);

                case MethodCallExpression callExpression when callExpression.Method.Name == "Any" && callExpression.Method.DeclaringType == typeof(Enumerable):
                    return TranslateAny(scope, callExpression, negated);

                case MethodCallExpression callExpression when IsCollectionContains(callExpression):
                    return TranslateCollectionContains(scope, callExpression, negated);

                case MethodCallExpression callExpression:
                {
                    SparqlExpression function = TranslateStringFunction(scope, callExpression);

                    return negated ? new SparqlUnaryExpression(SparqlUnaryOperator.Not, function) : function;
                }

                case MemberExpression member:
                    // A bare boolean member used as a predicate, e.g. `where person.Status`.
                    return TranslateComparison(
                        scope,
                        negated ? ExpressionType.NotEqual : ExpressionType.Equal,
                        member,
                        Expression.Constant(true),
                        inDisjunction);

                default:
                    throw new NotSupportedException($"Unsupported predicate expression: {expression.NodeType}.");
            }
        }

        private SparqlExpression TranslateComparison(QueryScope scope, ExpressionType op, Expression left, Expression right, bool inDisjunction = false)
        {
            SparqlExpression typeCheck;

            if (TryTranslateRuntimeTypeCheck(scope, op, left, right, out typeCheck))
            {
                return typeCheck;
            }

            left = Unwrap(left);
            right = Unwrap(right);

            // Normalize `constant op member` to `member op' constant`.
            if (left is ConstantExpression && !(right is ConstantExpression))
            {
                Expression swap = left;
                left = right;
                right = swap;
                op = Mirror(op);
            }

            ChainInfo chain = TryGetChain(left);

            if (chain != null && right is ConstantExpression constant)
            {
                return TranslateChainComparison(scope, op, chain, constant, inDisjunction);
            }

            return new SparqlBinaryExpression(MapComparison(op), TranslateOperand(scope, left), TranslateOperand(scope, right));
        }

        private SparqlExpression TranslateChainComparison(QueryScope scope, ExpressionType op, ChainInfo chain, ConstantExpression constant, bool inDisjunction = false)
        {
            object value = constant.Value;

            switch (chain.Kind)
            {
                case ChainKind.Subject:
                {
                    if (value == null)
                    {
                        throw new NotSupportedException("Comparing the query source against null is not supported.");
                    }

                    VariableTerm subject = ResolveScope(scope, chain.RootParameter).Subject;

                    return new SparqlBinaryExpression(MapComparison(op), new SparqlVariableExpression(subject.Name), new SparqlConstantExpression(ToTerm(value)));
                }

                case ChainKind.Length:
                {
                    MemberBinding binding = BindChain(scope, chain.Chain, false);

                    return new SparqlBinaryExpression(
                        MapComparison(op),
                        new SparqlFunctionExpression("STRLEN", new SparqlVariableExpression(binding.Variable.Name)),
                        new SparqlConstantExpression(ToTerm(value)));
                }

                case ChainKind.Count:
                {
                    MemberBinding binding = BindCount(scope, chain.Chain, chain.ElementType);

                    // A correlated count is always bound (zero included), so plain comparisons suffice.
                    return new SparqlBinaryExpression(
                        MapComparison(op),
                        new SparqlVariableExpression(binding.Variable.Name),
                        new SparqlConstantExpression(ToTerm(value)));
                }

                case ChainKind.Uri:
                    return TranslateReferenceComparison(scope, op, chain.Chain, value, inDisjunction);

                default:
                {
                    if (value == null)
                    {
                        if (op != ExpressionType.Equal && op != ExpressionType.NotEqual)
                        {
                            throw new NotSupportedException($"Unsupported null comparison: {op}.");
                        }

                        // `member == null` means the property is absent; the chain pattern lives
                        // inside the (NOT) EXISTS group so it doesn't constrain the main solution.
                        GroupGraphPattern group = BuildChainExistsGroup(scope, chain.Chain);

                        return new SparqlExistsExpression(group, op == ExpressionType.Equal);
                    }

                    if (typeof(IResource).IsAssignableFrom(chain.MemberType) || value is IResource || value is Uri)
                    {
                        return TranslateReferenceComparison(scope, op, chain.Chain, value, inDisjunction);
                    }

                    if (chain.MemberType == typeof(string))
                    {
                        // Strings have no default-value semantics (parity with the re-linq provider).
                        MemberBinding text = BindChain(scope, chain.Chain, false);

                        return BindingComparison(op, text, value);
                    }

                    if (op == ExpressionType.Equal || op == ExpressionType.NotEqual)
                    {
                        // Unbound members hold default(T): an equality that would match the default
                        // must bind optionally and include unbound values.
                        object defaultValue = TypeHelper.GetDefaultValue(constant.Type);
                        bool matchesUnbound = op == ExpressionType.Equal ? value.Equals(defaultValue) : !value.Equals(defaultValue);

                        if (matchesUnbound)
                        {
                            MemberBinding optional = BindChain(scope, chain.Chain, true);

                            return new SparqlBinaryExpression(
                                SparqlBinaryOperator.Or,
                                BindingComparison(op, optional, value),
                                NotBound(optional));
                        }
                    }

                    // Unbound members hold default(T), so a value-type member binds optionally and the
                    // comparison runs against its COALESCEd value. Without this an inequality would
                    // silently drop resources that lack the property, disagreeing with the projected
                    // sequence (`Select(p => p.Age)` already yields 0 for them).
                    bool defaultable = chain.MemberType.IsValueType;

                    MemberBinding binding = BindChain(scope, chain.Chain, defaultable || inDisjunction);

                    if (defaultable)
                    {
                        return new SparqlBinaryExpression(
                            MapComparison(op),
                            Coalesce(binding.Variable, chain.MemberType),
                            new SparqlConstantExpression(ToTerm(value)));
                    }

                    return BindingComparison(op, binding, value);
                }
            }
        }

        /// <summary>
        /// Translates <c>x is T</c> into a type check on the resource: <c>(NOT) EXISTS { ?x a &lt;T&gt; }</c>.
        /// Expressing it as a filter (rather than adding the pattern to the main solution) keeps it
        /// composable with <c>&amp;&amp;</c>, <c>||</c> and negation like any other predicate.
        /// </summary>
        private SparqlExpression TranslateTypeIs(QueryScope scope, TypeBinaryExpression expression, bool negated)
        {
            return TranslateTypeCheck(
                ResolveSubjectTerm(scope, expression.Expression), expression.TypeOperand, negated, "is");
        }

        /// <summary>
        /// Recognizes <c>x.GetType() == typeof(T)</c> (and <c>!=</c>) and translates it as the same
        /// type check as <c>x is T</c>. Note the member chain binds mandatorily, so a resource that
        /// lacks the member matches neither form.
        /// </summary>
        private bool TryTranslateRuntimeTypeCheck(QueryScope scope, ExpressionType op, Expression left, Expression right, out SparqlExpression result)
        {
            result = null;

            if (op != ExpressionType.Equal && op != ExpressionType.NotEqual)
            {
                return false;
            }

            Expression operand = GetTypeCallTarget(left) ?? GetTypeCallTarget(right);
            Type type = GetTypeConstant(left) ?? GetTypeConstant(right);

            if (operand == null || type == null)
            {
                return false;
            }

            result = TranslateTypeCheck(
                ResolveSubjectTerm(scope, operand), type, op == ExpressionType.NotEqual, "GetType() ==");

            return true;
        }

        /// <summary>Builds the <c>(NOT) EXISTS { ?x a &lt;T&gt; }</c> type check for a mapped type.</summary>
        private SparqlExpression TranslateTypeCheck(SparqlTerm subject, Type type, bool negate, string syntax)
        {
            List<Uri> types = GetTypeConstraints(type).ToList();

            if (types.Count == 0)
            {
                throw new NotSupportedException(
                    $"`{syntax} {type.Name}` cannot be translated: the type has no [RdfClass] mapping.");
            }

            var group = new GroupGraphPattern();

            foreach (Uri uri in types)
            {
                group.Add(new TriplePattern(subject, RdfTypeTerm.Instance, new IriTerm(uri)));
            }

            return new SparqlExistsExpression(group, negate);
        }

        /// <summary>The receiver of a parameterless <c>GetType()</c> call, or <c>null</c>.</summary>
        private static Expression GetTypeCallTarget(Expression expression)
        {
            return Unwrap(expression) is MethodCallExpression call
                   && call.Method.Name == "GetType"
                   && call.Object != null
                   && call.Arguments.Count == 0
                ? call.Object
                : null;
        }

        /// <summary>The type denoted by a <c>typeof(T)</c> constant, or <c>null</c>.</summary>
        private static Type GetTypeConstant(Expression expression)
        {
            return (Unwrap(expression) as ConstantExpression)?.Value as Type;
        }

        /// <summary>Resolves an expression denoting a resource to the term that stands for it.</summary>
        private SparqlTerm ResolveSubjectTerm(QueryScope scope, Expression expression)
        {
            ChainInfo chain = TryGetChain(expression);

            if (chain == null)
            {
                throw new NotSupportedException($"Unsupported resource expression: {expression.NodeType}.");
            }

            switch (chain.Kind)
            {
                case ChainKind.Subject:
                    return ResolveScope(scope, chain.RootParameter).Subject;

                case ChainKind.Value:
                case ChainKind.Uri:
                    return BindChain(scope, chain.Chain, false).Variable;

                default:
                    throw new NotSupportedException($"Unsupported resource expression: {chain.Kind}.");
            }
        }

        /// <summary>
        /// Compares a resource- or URI-valued member against an IRI. Inequality must also match
        /// resources where the member is absent, so it binds optionally and includes unbound values.
        /// </summary>
        private SparqlExpression TranslateReferenceComparison(QueryScope scope, ExpressionType op, MemberExpression chain, object value, bool inDisjunction = false)
        {
            if (value == null)
            {
                GroupGraphPattern group = BuildChainExistsGroup(scope, chain);

                return new SparqlExistsExpression(group, op == ExpressionType.Equal);
            }

            if (op == ExpressionType.NotEqual)
            {
                MemberBinding optional = BindChain(scope, chain, true);

                return new SparqlBinaryExpression(
                    SparqlBinaryOperator.Or,
                    BindingComparison(op, optional, value),
                    NotBound(optional));
            }

            MemberBinding binding = BindChain(scope, chain, inDisjunction);

            return BindingComparison(op, binding, value);
        }

        private GroupGraphPattern BuildChainExistsGroup(QueryScope scope, MemberExpression member)
        {
            SparqlTerm parent;
            Expression inner = Unwrap(member.Expression);

            switch (inner)
            {
                case ParameterExpression parameter:
                    parent = ResolveScope(scope, parameter).Subject;
                    break;
                case MemberExpression innerMember:
                    parent = BindChain(scope, innerMember, false).Variable;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member access root: {inner?.NodeType}.");
            }

            var group = new GroupGraphPattern();
            group.Add(new TriplePattern(parent, new IriTerm(GetPredicate(member)), FreshVariable()));

            return group;
        }

        /// <summary>Translates an <c>Enumerable.Any</c> over a mapped collection into a <c>FILTER (NOT) EXISTS</c>.</summary>
        private SparqlExpression TranslateAny(QueryScope scope, MethodCallExpression call, bool negated)
        {
            if (!(Unwrap(call.Arguments[0]) is MemberExpression member) || !IsMappedChain(member))
            {
                throw new NotSupportedException($"Unsupported sub-query source: {call.Arguments[0]}.");
            }

            SparqlTerm parent;
            Expression inner = Unwrap(member.Expression);

            switch (inner)
            {
                case ParameterExpression parameter:
                    parent = ResolveScope(scope, parameter).Subject;
                    break;
                case MemberExpression innerMember:
                    parent = BindChain(scope, innerMember, false).Variable;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member access root: {inner?.NodeType}.");
            }

            var group = new GroupGraphPattern();
            VariableTerm subject = FreshVariable();

            group.Add(new TriplePattern(parent, new IriTerm(GetPredicate(member)), subject));

            if (call.Arguments.Count == 2)
            {
                LambdaExpression lambda = GetLambda(call.Arguments[1]);
                var child = new QueryScope(lambda.Parameters[0], subject, group, scope);

                group.AddFilter(TranslatePredicate(child, lambda.Body, false));
            }

            return new SparqlExistsExpression(group, negated);
        }

        /// <summary>Matches <c>Contains</c> over an in-memory (constant) collection — not a string instance call.</summary>
        private static bool IsCollectionContains(MethodCallExpression call)
        {
            if (call.Method.Name != "Contains")
            {
                return false;
            }

            if (call.Object == null)
            {
                // Static form: Enumerable.Contains(collection, item) — or, under C# 13+ first-class
                // spans, MemoryExtensions.Contains(span, item) over an implicitly converted array.
                return call.Arguments.Count == 2 && UnwrapCollectionConversion(call.Arguments[0]) is ConstantExpression;
            }

            // Instance form: List<T>.Contains(item) etc. — string.Contains stays a string function.
            return call.Object.Type != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(call.Object.Type)
                && call.Arguments.Count == 1
                && Unwrap(call.Object) is ConstantExpression;
        }

        /// <summary>
        /// Strips the implicit array → (ReadOnly)Span conversion (an <c>op_Implicit</c> call or
        /// Convert node) that C# 13+ overload resolution inserts for <c>array.Contains(...)</c>.
        /// </summary>
        private static Expression UnwrapCollectionConversion(Expression expression)
        {
            expression = Unwrap(expression);

            while (expression is MethodCallExpression conversion
                && conversion.Object == null
                && conversion.Method.Name == "op_Implicit"
                && conversion.Arguments.Count == 1)
            {
                expression = Unwrap(conversion.Arguments[0]);
            }

            return expression;
        }

        /// <summary>
        /// Translates <c>collection.Contains(x.Member)</c> into <c>FILTER(?v (NOT) IN (...))</c>.
        /// An empty collection matches nothing (negated: everything) — <c>?v IN ()</c> is not valid
        /// SPARQL, so a boolean constant is emitted. Unbound members hold default(T): when the
        /// (negated) membership test would accept the default, the member binds optionally and
        /// unbound values are included via <c>!BOUND</c>.
        /// </summary>
        private SparqlExpression TranslateCollectionContains(QueryScope scope, MethodCallExpression call, bool negated)
        {
            Expression collectionExpression = UnwrapCollectionConversion(call.Object ?? call.Arguments[0]);
            Expression itemExpression = call.Object != null ? call.Arguments[0] : call.Arguments[1];

            var collection = (IEnumerable)((ConstantExpression)collectionExpression).Value;

            if (collection == null)
            {
                throw new NotSupportedException("Contains on a null collection is not supported.");
            }

            List<object> values = collection.Cast<object>().ToList();

            if (values.Count == 0)
            {
                return BooleanConstant(negated);
            }

            ChainInfo chain = TryGetChain(itemExpression);

            SparqlExpression value;
            MemberBinding binding = null;
            bool matchesUnbound = false;

            if (chain != null && chain.Kind == ChainKind.Value)
            {
                Type type = chain.MemberType;

                if (type.IsValueType && type != typeof(string))
                {
                    bool containsDefault = values.Contains(TypeHelper.GetDefaultValue(type));

                    matchesUnbound = negated ? !containsDefault : containsDefault;
                }

                binding = BindChain(scope, chain.Chain, matchesUnbound);
                value = new SparqlVariableExpression(binding.Variable.Name);
            }
            else
            {
                value = TranslateOperand(scope, itemExpression);
            }

            var membership = new SparqlInExpression(value, negated);

            foreach (object item in values)
            {
                membership.Set.Add(new SparqlConstantExpression(ToTerm(item)));
            }

            if (matchesUnbound)
            {
                return new SparqlBinaryExpression(SparqlBinaryOperator.Or, membership, NotBound(binding));
            }

            return membership;
        }

        private static SparqlExpression BooleanConstant(bool value)
        {
            return new SparqlConstantExpression(new LiteralTerm(
                XsdTypeMapper.SerializeObject(value),
                XsdTypeMapper.GetXsdTypeUri(typeof(bool))));
        }

        private SparqlExpression TranslateStringFunction(QueryScope scope, MethodCallExpression call)
        {
            switch (call.Method.Name)
            {
                case "Contains" when IsStringInstanceCall(call):
                    return TranslateStringMatch(scope, call, "CONTAINS");

                case "StartsWith" when IsStringInstanceCall(call):
                    return TranslateStringMatch(scope, call, "STRSTARTS");

                case "EndsWith" when IsStringInstanceCall(call):
                    return TranslateStringMatch(scope, call, "STRENDS");

                case "IsMatch" when call.Object == null && call.Method.DeclaringType == typeof(Regex):
                    return TranslateRegexMatch(scope, call);

                default:
                    throw new NotSupportedException($"Unsupported method call in predicate: {call.Method.Name}.");
            }
        }

        private SparqlExpression TranslateStringMatch(QueryScope scope, MethodCallExpression call, string function)
        {
            bool ignoreCase = false;

            if (call.Arguments.Count == 2 && Unwrap(call.Arguments[1]) is ConstantExpression comparison && comparison.Value is StringComparison mode)
            {
                ignoreCase = mode == StringComparison.CurrentCultureIgnoreCase
                    || mode == StringComparison.InvariantCultureIgnoreCase
                    || mode == StringComparison.OrdinalIgnoreCase;
            }
            else if (call.Arguments.Count == 3 && Unwrap(call.Arguments[1]) is ConstantExpression flag && flag.Value is bool caseInsensitive)
            {
                // The (string, bool ignoreCase, CultureInfo) overload.
                ignoreCase = caseInsensitive;
            }
            else if (call.Arguments.Count > 1)
            {
                throw new NotSupportedException($"Unsupported {call.Method.Name} overload.");
            }

            SparqlExpression target = TranslateOperand(scope, call.Object);
            SparqlExpression pattern = TranslateOperand(scope, call.Arguments[0]);

            if (ignoreCase)
            {
                target = new SparqlFunctionExpression("LCASE", target);
                pattern = new SparqlFunctionExpression("LCASE", pattern);
            }

            return new SparqlFunctionExpression(function, target, pattern);
        }

        private SparqlExpression TranslateRegexMatch(QueryScope scope, MethodCallExpression call)
        {
            SparqlExpression target = TranslateOperand(scope, call.Arguments[0]);
            SparqlExpression pattern = TranslateOperand(scope, call.Arguments[1]);

            bool ignoreCase = call.Arguments.Count == 3
                && Unwrap(call.Arguments[2]) is ConstantExpression options
                && options.Value is RegexOptions regexOptions
                && (regexOptions & RegexOptions.IgnoreCase) != 0;

            return ignoreCase
                ? new SparqlFunctionExpression("REGEX", target, pattern, new SparqlConstantExpression(new LiteralTerm("i")))
                : new SparqlFunctionExpression("REGEX", target, pattern);
        }

        private SparqlExpression TranslateOperand(QueryScope scope, Expression expression)
        {
            expression = Unwrap(expression);

            switch (expression)
            {
                case ConstantExpression constant:
                    return new SparqlConstantExpression(ToTerm(constant.Value));

                case MethodCallExpression call when call.Method.Name == "ToLower" && IsStringInstanceCall(call):
                    return new SparqlFunctionExpression("LCASE", TranslateOperand(scope, call.Object));

                case MethodCallExpression call when call.Method.Name == "ToUpper" && IsStringInstanceCall(call):
                    return new SparqlFunctionExpression("UCASE", TranslateOperand(scope, call.Object));

                default:
                {
                    ChainInfo chain = TryGetChain(expression);

                    if (chain == null)
                    {
                        throw new NotSupportedException($"Unsupported operand expression: {expression.NodeType}.");
                    }

                    switch (chain.Kind)
                    {
                        case ChainKind.Subject:
                            return new SparqlVariableExpression(ResolveScope(scope, chain.RootParameter).Subject.Name);

                        case ChainKind.Length:
                            return new SparqlFunctionExpression("STRLEN", new SparqlVariableExpression(BindChain(scope, chain.Chain, false).Variable.Name));

                        case ChainKind.Count:
                            return new SparqlVariableExpression(BindCount(scope, chain.Chain, chain.ElementType).Variable.Name);

                        default:
                            return new SparqlVariableExpression(BindChain(scope, chain.Chain, false).Variable.Name);
                    }
                }
            }
        }

        private SparqlExpression TranslateOrderKey(LambdaExpression lambda)
        {
            Expression body = Unwrap(lambda.Body);

            if (body == lambda.Parameters[0])
            {
                // Identity ordering over a value projection, e.g. `.Select(x => x.Age).OrderBy(i => i)`.
                if (_kind == QueryExecutionKind.Bindings && _projection != null)
                {
                    return new SparqlVariableExpression(_projection.Variable.Name);
                }

                throw new NotSupportedException("Identity ordering is only supported after a value projection.");
            }

            ChainInfo chain = TryGetChain(body);

            if (chain != null && chain.Kind == ChainKind.Value && chain.MemberType.IsValueType)
            {
                // Unbound members hold default(T): order by the COALESCEd value so a resource without
                // the property sorts as default(T) rather than dropping out of the result entirely.
                MemberBinding binding = BindChain(_rootScope, chain.Chain, true);

                return Coalesce(binding.Variable, chain.MemberType);
            }

            return TranslateOperand(_rootScope, body);
        }

        #endregion

        #region Member chains

        /// <summary>How a member-access chain is interpreted.</summary>
        private enum ChainKind
        {
            /// <summary>The lambda parameter itself (or its <c>.Uri</c>): the scope subject.</summary>
            Subject,

            /// <summary>A mapped member chain yielding a value (literal or resource).</summary>
            Value,

            /// <summary>A mapped resource chain accessed via <c>.Uri</c>.</summary>
            Uri,

            /// <summary><c>string.Length</c> over a mapped chain (<c>STRLEN</c>).</summary>
            Length,

            /// <summary>The element count of a mapped collection member.</summary>
            Count
        }

        private sealed class ChainInfo
        {
            public ChainKind Kind { get; set; }

            /// <summary>The mapped member chain (decorations stripped); <c>null</c> for <see cref="ChainKind.Subject"/>.</summary>
            public MemberExpression Chain { get; set; }

            public Type MemberType { get; set; }

            /// <summary>
            /// For <see cref="ChainKind.Count"/>: restricts the counted elements to this mapped type
            /// (<c>collection.OfType&lt;T&gt;().Count()</c>); <c>null</c> counts every element.
            /// </summary>
            public Type ElementType { get; set; }

            public ParameterExpression RootParameter { get; set; }
        }

        /// <summary>
        /// Analyzes an expression as a mapped member chain, recognizing the <c>.Uri</c>,
        /// <c>string.Length</c> and collection <c>Count</c> decorations. Returns <c>null</c>
        /// if the expression is not rooted in a lambda parameter over mapped members.
        /// </summary>
        private ChainInfo TryGetChain(Expression expression)
        {
            expression = Unwrap(expression);

            if (expression is ParameterExpression parameter)
            {
                return new ChainInfo { Kind = ChainKind.Subject, RootParameter = parameter };
            }

            if (expression is MethodCallExpression call
                && call.Method.DeclaringType == typeof(Enumerable)
                && call.Method.Name == "Count"
                && call.Arguments.Count == 1)
            {
                Expression counted = Unwrap(call.Arguments[0]);
                Type elementType = null;

                // `collection.OfType<T>().Count()` counts only the elements typed T.
                if (counted is MethodCallExpression ofType
                    && ofType.Method.DeclaringType == typeof(Enumerable)
                    && ofType.Method.Name == "OfType"
                    && ofType.Arguments.Count == 1)
                {
                    elementType = ofType.Method.GetGenericArguments()[0];
                    counted = Unwrap(ofType.Arguments[0]);
                }

                if (counted is MemberExpression source && IsMappedChain(source))
                {
                    return new ChainInfo
                    {
                        Kind = ChainKind.Count,
                        Chain = source,
                        ElementType = elementType,
                        MemberType = typeof(int),
                        RootParameter = GetRootParameter(source)
                    };
                }
            }

            if (!(expression is MemberExpression member))
            {
                return null;
            }

            if (GetPredicate(member) == null)
            {
                Expression inner = Unwrap(member.Expression);

                switch (member.Member.Name)
                {
                    case "Uri":
                        if (inner is ParameterExpression root)
                        {
                            return new ChainInfo { Kind = ChainKind.Subject, RootParameter = root };
                        }
                        if (inner is MemberExpression resource && IsMappedChain(resource))
                        {
                            return new ChainInfo { Kind = ChainKind.Uri, Chain = resource, MemberType = GetMemberType(resource.Member), RootParameter = GetRootParameter(resource) };
                        }
                        return null;

                    case "Length":
                        if (member.Member.DeclaringType == typeof(string) && inner is MemberExpression text && IsMappedChain(text))
                        {
                            return new ChainInfo { Kind = ChainKind.Length, Chain = text, MemberType = typeof(int), RootParameter = GetRootParameter(text) };
                        }
                        return null;

                    case "Count":
                        if (inner is MemberExpression collection && IsMappedChain(collection))
                        {
                            return new ChainInfo { Kind = ChainKind.Count, Chain = collection, MemberType = typeof(int), RootParameter = GetRootParameter(collection) };
                        }
                        return null;

                    default:
                        return null;
                }
            }

            if (IsMappedChain(member))
            {
                return new ChainInfo { Kind = ChainKind.Value, Chain = member, MemberType = GetMemberType(member.Member), RootParameter = GetRootParameter(member) };
            }

            return null;
        }

        private static bool IsMappedChain(MemberExpression member)
        {
            Expression current = member;

            while (current is MemberExpression m)
            {
                if (GetPredicate(m) == null)
                {
                    return false;
                }

                current = Unwrap(m.Expression);
            }

            return current is ParameterExpression;
        }

        private static ParameterExpression GetRootParameter(MemberExpression member)
        {
            Expression current = member;

            while (current is MemberExpression m)
            {
                current = Unwrap(m.Expression);
            }

            return current as ParameterExpression;
        }

        private static string PathKey(MemberExpression member)
        {
            var segments = new List<string>();
            Expression current = member;

            while (current is MemberExpression m)
            {
                Uri predicate = GetPredicate(m);

                if (predicate == null)
                {
                    throw new NotSupportedException($"Member is not mapped to an RDF property: {m.Member.Name}.");
                }

                segments.Add(predicate.AbsoluteUri);
                current = Unwrap(m.Expression);
            }

            segments.Reverse();

            return string.Join("|", segments);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            switch (member)
            {
                case PropertyInfo property: return property.PropertyType;
                case FieldInfo field: return field.FieldType;
                default: return null;
            }
        }

        #endregion

        #region Client projections

        /// <summary>How a client-projection column is produced by the SELECT.</summary>
        private enum ClientColumnKind
        {
            /// <summary>A bound member (or subject/Uri) variable.</summary>
            Member,

            /// <summary>The grouping key variable of a grouped query.</summary>
            GroupKey,

            /// <summary>A <c>COUNT(DISTINCT ?s)</c> aggregate of a grouped query.</summary>
            GroupCount
        }

        /// <summary>One projected column of a client-shaped projection.</summary>
        private sealed class ClientColumn
        {
            public ClientColumnKind Kind { get; set; }

            /// <summary>The SELECT variable (null for <see cref="ClientColumnKind.GroupCount"/>, named at build time).</summary>
            public VariableTerm Variable { get; set; }

            /// <summary>Whether the variable is optionally bound and needs a COALESCE alias.</summary>
            public bool IsOptional { get; set; }

            /// <summary>The .NET type the column value is coerced to before projection.</summary>
            public Type Type { get; set; }
        }

        /// <summary>A projection shaped client-side from projected column values.</summary>
        private sealed class ClientProjection
        {
            public List<ClientColumn> Columns { get; } = new List<ClientColumn>();

            public Func<object[], object> Projector { get; set; }
        }

        /// <summary>
        /// Rewrites a projection body: every mapped member chain (and subject/.Uri access) becomes a
        /// typed read from the row array; everything else is left for client-side evaluation.
        /// </summary>
        private sealed class ClientProjectionRewriter : ExpressionVisitor
        {
            private readonly SparqlQueryTranslator _translator;

            private readonly ParameterExpression _row;

            private readonly ClientProjection _projection;

            private readonly Dictionary<string, int> _columnsByVariable = new Dictionary<string, int>();

            public ClientProjectionRewriter(SparqlQueryTranslator translator, ParameterExpression row, ClientProjection projection)
            {
                _translator = translator;
                _row = row;
                _projection = projection;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                Expression column = TryRewriteChain(node);

                return column ?? base.VisitMember(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                Expression column = TryRewriteChain(node);

                return column ?? base.VisitMethodCall(node);
            }

            private Expression TryRewriteChain(Expression node)
            {
                ChainInfo chain = _translator.TryGetChain(node);

                if (chain == null)
                {
                    return null;
                }

                switch (chain.Kind)
                {
                    case ChainKind.Value when IsScalarType(chain.MemberType):
                    {
                        // Mirror the single-member projection: reuse an existing binding as-is,
                        // otherwise bind value types optionally (default(T) semantics), strings mandatorily.
                        string key = PathKey(chain.Chain);
                        MemberBinding binding;

                        if (!_translator._rootScope.Bindings.TryGetValue(key, out binding))
                        {
                            bool optional = chain.MemberType.IsValueType && chain.MemberType != typeof(string);

                            binding = _translator.BindChain(_translator._rootScope, chain.Chain, optional);
                        }

                        return Column(binding.Variable, binding.IsOptional, chain.MemberType, node.Type);
                    }

                    case ChainKind.Count:
                        return Column(_translator.BindCount(_translator._rootScope, chain.Chain, chain.ElementType).Variable, false, typeof(int), node.Type);

                    case ChainKind.Subject when node.Type == typeof(Uri):
                        return Column(_translator._rootScope.Subject, false, typeof(Uri), node.Type);

                    case ChainKind.Uri:
                        return Column(_translator.BindChain(_translator._rootScope, chain.Chain, false).Variable, false, typeof(Uri), node.Type);

                    default:
                        return null;
                }
            }

            private Expression Column(VariableTerm variable, bool optional, Type columnType, Type nodeType)
            {
                int index;

                if (!_columnsByVariable.TryGetValue(variable.Name, out index))
                {
                    index = _projection.Columns.Count;

                    _projection.Columns.Add(new ClientColumn
                    {
                        Kind = ClientColumnKind.Member,
                        Variable = variable,
                        IsOptional = optional,
                        Type = columnType
                    });

                    _columnsByVariable[variable.Name] = index;
                }

                return Expression.Convert(
                    Expression.ArrayIndex(_row, Expression.Constant(index)),
                    nodeType);
            }
        }

        /// <summary>
        /// Rewrites a group-projection body: <c>g.Key</c> and <c>g.Count()</c> become typed reads from
        /// the row array; any other use of the grouping parameter is left in place (and rejected).
        /// </summary>
        private sealed class GroupSelectRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression _group;

            private readonly ParameterExpression _row;

            private readonly ClientProjection _projection;

            private readonly Type _keyType;

            private int _keyColumn = -1;

            private int _countColumn = -1;

            public GroupSelectRewriter(ParameterExpression group, ParameterExpression row, ClientProjection projection, Type keyType)
            {
                _group = group;
                _row = row;
                _projection = projection;
                _keyType = keyType;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == _group && node.Member.Name == "Key")
                {
                    if (_keyColumn < 0)
                    {
                        _keyColumn = _projection.Columns.Count;

                        _projection.Columns.Add(new ClientColumn { Kind = ClientColumnKind.GroupKey, Type = _keyType });
                    }

                    return Expression.Convert(Expression.ArrayIndex(_row, Expression.Constant(_keyColumn)), node.Type);
                }

                return base.VisitMember(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(Enumerable)
                    && (node.Method.Name == "Count" || node.Method.Name == "LongCount")
                    && node.Arguments.Count == 1
                    && Unwrap(node.Arguments[0]) == _group)
                {
                    if (_countColumn < 0)
                    {
                        _countColumn = _projection.Columns.Count;

                        _projection.Columns.Add(new ClientColumn { Kind = ClientColumnKind.GroupCount, Type = node.Type });
                    }

                    return Expression.Convert(Expression.ArrayIndex(_row, Expression.Constant(_countColumn)), node.Type);
                }

                return base.VisitMethodCall(node);
            }
        }

        /// <summary>Detects whether an expression tree still references a lambda parameter.</summary>
        private sealed class ParameterDetector : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;

            public bool Found { get; private set; }

            public ParameterDetector(ParameterExpression parameter) => _parameter = parameter;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == _parameter)
                {
                    Found = true;
                }

                return base.VisitParameter(node);
            }
        }

        private static bool ReferencesParameter(Expression expression, ParameterExpression parameter)
        {
            var detector = new ParameterDetector(parameter);

            detector.Visit(expression);

            return detector.Found;
        }

        /// <summary>A member type that maps to an RDF literal (projectable as a column value).</summary>
        private static bool IsScalarType(Type type)
        {
            return type == typeof(string) || (type.IsValueType && XsdTypeMapper.HasXsdTypeUri(type));
        }

        /// <summary>Builds <c>COALESCE(?v, default(T))</c> for an optionally bound value-type member.</summary>
        private static SparqlFunctionExpression Coalesce(VariableTerm variable, Type type)
        {
            object defaultValue = TypeHelper.GetDefaultValue(type);
            var defaultTerm = new LiteralTerm(XsdTypeMapper.SerializeObject(defaultValue), XsdTypeMapper.GetXsdTypeUri(type));

            return new SparqlFunctionExpression(
                "COALESCE",
                new SparqlVariableExpression(variable.Name),
                new SparqlConstantExpression(defaultTerm));
        }

        #endregion

        #region Build

        private QueryTranslation Build()
        {
            if (_terminal == TerminalKind.Last || _terminal == TerminalKind.LastOrDefault)
            {
                if (_orderings.Count == 0)
                {
                    throw new NotSupportedException("Last requires an ordering (append OrderBy before Last).");
                }

                // Last is First over inverted orderings.
                foreach (OrderCondition ordering in _orderings)
                {
                    ordering.Descending = !ordering.Descending;
                }
            }

            if (_isGrouped && _clientProjection == null)
            {
                throw new NotSupportedException("Materializing the elements of a group is not supported; project g.Key or g.Count().");
            }

            GroupGraphPattern selection = BuildSubjectSelection();

            var translation = new QueryTranslation
            {
                Kind = _kind,
                ElementType = _elementType,
                Terminal = _terminal,
                Aggregate = _aggregate,
                NegateResult = _negateResult
            };

            switch (_kind)
            {
                case QueryExecutionKind.Ask:
                    translation.Query = new AskQuery { Where = selection };
                    break;

                case QueryExecutionKind.Count:
                    var count = new SelectQuery { Where = selection };
                    count.Projections.Add(new Projection(
                        new VariableTerm("count"),
                        new SparqlAggregateExpression(SparqlAggregateKind.Count, new SparqlVariableExpression(_rootScope.Subject.Name), true)));
                    translation.Query = count;
                    break;

                case QueryExecutionKind.Scalar:
                    translation.Query = BuildScalarQuery(selection);
                    translation.ProjectedVariable = "agg";
                    break;

                case QueryExecutionKind.Bindings when _clientProjection != null:
                    translation.Query = BuildRowsQuery(selection, translation);
                    break;

                case QueryExecutionKind.Bindings:
                {
                    string projectedVariable;
                    translation.Query = BuildBindingsQuery(selection, out projectedVariable);
                    translation.ProjectedVariable = projectedVariable;
                    break;
                }

                default:
                    translation.Query = BuildResourceQuery(selection);

                    if ((_rootScope.Subject != Subject || _forceMultiplicity) && _terminal == TerminalKind.Enumerate)
                    {
                        // The result is a projected variable: GetResources materializes each resource
                        // once, so a companion multiset query restores per-source-row multiplicity.
                        var multiset = new SelectQuery { Where = selection, Limit = _limit, Offset = _offset, IsDistinct = _distinct };
                        multiset.Projections.Add(new Projection(_rootScope.Subject));
                        multiset.OrderBy.AddRange(_orderings);

                        translation.MultiplicityQuery = multiset;
                        translation.SubjectVariable = _rootScope.Subject.Name;
                    }
                    break;
            }

            return translation;
        }

        /// <summary>
        /// Builds the single-row aggregate query: <c>SELECT (AGG(?v) AS ?agg) WHERE { ... }</c>. An
        /// optionally-bound member aggregates over <c>COALESCE(?v, default)</c> so the result matches
        /// aggregating the projected value sequence.
        /// </summary>
        private SparqlQueryModel BuildScalarQuery(GroupGraphPattern selection)
        {
            var select = new SelectQuery { Where = selection };

            Type type = _projection.MemberType;
            SparqlExpression argument;

            if (_projection.IsOptional && type != null && type.IsValueType && type != typeof(string))
            {
                argument = Coalesce(_projection.Variable, type);
            }
            else
            {
                argument = new SparqlVariableExpression(_projection.Variable.Name);
            }

            select.Projections.Add(new Projection(
                new VariableTerm("agg"),
                new SparqlAggregateExpression(_aggregate.Value, argument)));

            return select;
        }

        /// <summary>
        /// Builds the multi-column SELECT for a client-shaped projection. Grouped queries project the
        /// key and/or a COUNT aggregate and add the GROUP BY clause; plain projections project each
        /// member column (COALESCEd when optionally bound).
        /// </summary>
        private SparqlQueryModel BuildRowsQuery(GroupGraphPattern selection, QueryTranslation translation)
        {
            var select = new SelectQuery { Where = selection, Limit = _limit, Offset = _offset, IsDistinct = _distinct };

            var names = new List<string>();
            var types = new List<Type>();
            int aggregateIndex = 0;

            foreach (ClientColumn column in _clientProjection.Columns)
            {
                switch (column.Kind)
                {
                    case ClientColumnKind.GroupKey:
                        select.Projections.Add(new Projection(_groupKeyVariable));
                        names.Add(_groupKeyVariable.Name);
                        break;

                    case ClientColumnKind.GroupCount:
                    {
                        var variable = new VariableTerm("c" + aggregateIndex++);

                        select.Projections.Add(new Projection(variable, new SparqlAggregateExpression(
                            SparqlAggregateKind.Count,
                            new SparqlVariableExpression(_rootScope.Subject.Name),
                            true)));
                        names.Add(variable.Name);
                        break;
                    }

                    default:
                    {
                        if (column.IsOptional && column.Type.IsValueType && column.Type != typeof(string))
                        {
                            var alias = new VariableTerm(column.Variable.Name + "_");

                            select.Projections.Add(new Projection(alias, Coalesce(column.Variable, column.Type)));
                            names.Add(alias.Name);
                        }
                        else
                        {
                            select.Projections.Add(new Projection(column.Variable));
                            names.Add(column.Variable.Name);
                        }
                        break;
                    }
                }
            }

            if (_isGrouped)
            {
                select.GroupBy.Add(new SparqlVariableExpression(_groupKeyVariable.Name));
            }

            select.OrderBy.AddRange(_orderings);

            translation.ColumnVariables = names.ToArray();
            translation.ColumnTypes = _clientProjection.Columns.Select(c => c.Type).ToArray();
            translation.RowProjector = _clientProjection.Projector;

            return select;
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
        /// Builds the <c>SELECT ?s ?p ?o WHERE { ?s ?p ?o . ... }</c> shape that
        /// <c>Model.GetResources&lt;T&gt;</c> materializes. With paging (limit/offset), Distinct, or a
        /// projected subject (which may match several source rows) the subject selection is nested as
        /// <c>{ SELECT DISTINCT ?s WHERE { ... } ... }</c> so the operators apply per resource, not
        /// per triple; otherwise the selection stays in the top-level group and the ordering is
        /// applied to the triple rows directly — engines are not required to preserve a sub-select's
        /// order through the outer join.
        /// </summary>
        private SparqlQueryModel BuildResourceQuery(GroupGraphPattern selection)
        {
            VariableTerm subject = _rootScope.Subject;

            var outer = new SelectQuery();
            outer.Projections.Add(new Projection(subject));
            outer.Projections.Add(new Projection(new VariableTerm("p")));
            outer.Projections.Add(new Projection(new VariableTerm("o")));
            outer.Where.Add(new TriplePattern(subject, new VariableTerm("p"), new VariableTerm("o")));

            if (_limit.HasValue || _offset.HasValue || _distinct || subject != Subject)
            {
                var inner = new SelectQuery { IsDistinct = true, Limit = _limit, Offset = _offset, Where = selection };
                inner.Projections.Add(new Projection(subject));
                inner.OrderBy.AddRange(_orderings);

                outer.Where.Add(new SubSelectPattern(inner));
            }
            else
            {
                foreach (GraphPattern pattern in selection.Patterns)
                {
                    outer.Where.Add(pattern);
                }

                foreach (SparqlExpression filter in selection.Filters)
                {
                    outer.Where.AddFilter(filter);
                }

                outer.OrderBy.AddRange(_orderings);
            }

            return outer;
        }

        /// <summary>
        /// Builds the value-projection query. An optionally-bound value-type member is projected as
        /// <c>(COALESCE(?v, default) AS ?v_)</c> so resources without the property yield default(T).
        /// </summary>
        private SparqlQueryModel BuildBindingsQuery(GroupGraphPattern selection, out string projectedVariable)
        {
            var select = new SelectQuery { Where = selection, Limit = _limit, Offset = _offset, IsDistinct = _distinct };

            Type type = _projection.MemberType;

            if (_projection.IsOptional && type != null && type.IsValueType && type != typeof(string))
            {
                var alias = new VariableTerm(_projection.Variable.Name + "_");

                select.Projections.Add(new Projection(alias, Coalesce(_projection.Variable, type)));

                projectedVariable = alias.Name;
            }
            else
            {
                select.Projections.Add(new Projection(_projection.Variable));

                projectedVariable = _projection.Variable.Name;
            }

            select.OrderBy.AddRange(_orderings);

            return select;
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

        private static Uri GetPredicate(MemberExpression member)
        {
            MemberInfo info = member.Member;

            // Interface-declared members (e.g. IImage.DepictedAgent) carry no [RdfProperty]; resolve the
            // same-named member on the concrete expression type, which does.
            if (info.DeclaringType != null && info.DeclaringType.IsInterface && member.Expression != null)
            {
                info = member.Expression.Type.GetMember(info.Name).FirstOrDefault() ?? info;
            }

            return (info.GetCustomAttributes(typeof(RdfPropertyAttribute), true).FirstOrDefault() as RdfPropertyAttribute)?.MappedUri;
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

        private static SparqlExpression BindingComparison(ExpressionType op, MemberBinding binding, object value)
        {
            return new SparqlBinaryExpression(
                MapComparison(op),
                new SparqlVariableExpression(binding.Variable.Name),
                new SparqlConstantExpression(ToTerm(value)));
        }

        private static SparqlExpression NotBound(MemberBinding binding)
        {
            return new SparqlUnaryExpression(
                SparqlUnaryOperator.Not,
                new SparqlFunctionExpression("BOUND", new SparqlVariableExpression(binding.Variable.Name)));
        }

        private static bool IsStringInstanceCall(MethodCallExpression call)
        {
            return call.Object != null && call.Object.Type == typeof(string);
        }

        private VariableTerm FreshVariable()
        {
            return new VariableTerm("v" + _variablePrefix + _variableCounter++);
        }

        private static bool IsComparison(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    return true;
                default:
                    return false;
            }
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

        private static ExpressionType Mirror(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.LessThan: return ExpressionType.GreaterThan;
                case ExpressionType.LessThanOrEqual: return ExpressionType.GreaterThanOrEqual;
                case ExpressionType.GreaterThan: return ExpressionType.LessThan;
                case ExpressionType.GreaterThanOrEqual: return ExpressionType.LessThanOrEqual;
                default: return type;
            }
        }

        private LambdaExpression GetLambda(Expression expression)
        {
            var lambda = (LambdaExpression)Unwrap(expression);

            if (_transparentIdentifiers.Count == 0)
            {
                return lambda;
            }

            Expression body = new TransparentIdentifierRewriter(_transparentIdentifiers).Visit(lambda.Body);

            return body == lambda.Body ? lambda : Expression.Lambda(body, lambda.Parameters);
        }

        /// <summary>
        /// Removes transparent identifiers: rewrites <c>t.member</c> on the anonymous type a
        /// <c>SelectMany</c> result selector introduced into the synthetic parameter that member
        /// aliases, so downstream lambdas look like plain <c>p.Member</c> chains again.
        /// </summary>
        private sealed class TransparentIdentifierRewriter : ExpressionVisitor
        {
            private readonly Dictionary<Type, Dictionary<string, ParameterExpression>> _identifiers;

            public TransparentIdentifierRewriter(Dictionary<Type, Dictionary<string, ParameterExpression>> identifiers)
            {
                _identifiers = identifiers;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                Dictionary<string, ParameterExpression> members;
                ParameterExpression alias;

                if (node.Expression is ParameterExpression parameter
                    && _identifiers.TryGetValue(parameter.Type, out members)
                    && members.TryGetValue(node.Member.Name, out alias))
                {
                    return alias;
                }

                return base.VisitMember(node);
            }
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
