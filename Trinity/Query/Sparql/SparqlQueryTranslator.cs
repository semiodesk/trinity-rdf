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
        Bindings
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

        // The lambda-parameter binding scope for the query source; child scopes are created for
        // sub-query lambdas (e.g. the parameter of an Any predicate inside an EXISTS group).
        private readonly QueryScope _rootScope;

        // For value projections (Bindings): the member binding whose variable is projected.
        private MemberBinding _projection;

        // For resource-member projections (select x.Member of a Resource type): the variable that
        // becomes the statement subject of the outer ?v ?p ?o query instead of ?s.
        private VariableTerm _resultTerm;

        public SparqlQueryTranslator()
        {
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

            Uri predicate = GetPredicate(member.Member);

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
        private MemberBinding BindCount(QueryScope scope, MemberExpression member)
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

            string key = PathKey(member) + "#count";

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
            optional.Add(new TriplePattern(owner.Subject, new IriTerm(GetPredicate(member.Member)), item));
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

                case "OrderBy":
                case "ThenBy":
                    _orderings.Add(new OrderCondition(TranslateOrderKey(GetLambda(call.Arguments[1])), false));
                    break;

                case "OrderByDescending":
                case "ThenByDescending":
                    _orderings.Add(new OrderCondition(TranslateOrderKey(GetLambda(call.Arguments[1])), true));
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
                        _filters.Add(TranslatePredicate(_rootScope, GetLambda(call.Arguments[1]).Body, false));
                    }
                    break;

                case "Count":
                case "LongCount":
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
            Expression body = Unwrap(lambda.Body);

            if (body == lambda.Parameters[0])
            {
                // Identity projection: the query still yields the source resources.
                return;
            }

            ChainInfo chain = TryGetChain(body);

            if (chain == null)
            {
                throw new NotSupportedException($"Unsupported projection: {body}.");
            }

            switch (chain.Kind)
            {
                case ChainKind.Count:
                    _projection = BindCount(_rootScope, chain.Chain);
                    _kind = QueryExecutionKind.Bindings;
                    _elementType = typeof(int);
                    break;

                case ChainKind.Value when typeof(IResource).IsAssignableFrom(chain.MemberType):
                    // Resource-valued member: still a resource query, but the member variable becomes
                    // the statement subject of the outer ?v ?p ?o query.
                    _resultTerm = BindChain(_rootScope, chain.Chain, false).Variable;
                    _elementType = chain.MemberType;
                    break;

                case ChainKind.Value:
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
                    throw new NotSupportedException($"Unsupported projection: {body}.");
            }
        }

        #endregion

        #region Predicate translation

        /// <summary>
        /// Translates a boolean predicate expression, normalizing negation: <c>Not</c> flips
        /// <paramref name="negated"/>, equality operators are inverted rather than wrapped in
        /// <c>!(...)</c>, so the unbound-member semantics pick the correct branch.
        /// </summary>
        private SparqlExpression TranslatePredicate(QueryScope scope, Expression expression, bool negated)
        {
            expression = Unwrap(expression);

            switch (expression)
            {
                case BinaryExpression binary when binary.NodeType == ExpressionType.AndAlso:
                    return new SparqlBinaryExpression(
                        negated ? SparqlBinaryOperator.Or : SparqlBinaryOperator.And,
                        TranslatePredicate(scope, binary.Left, negated),
                        TranslatePredicate(scope, binary.Right, negated));

                case BinaryExpression binary when binary.NodeType == ExpressionType.OrElse:
                    return new SparqlBinaryExpression(
                        negated ? SparqlBinaryOperator.And : SparqlBinaryOperator.Or,
                        TranslatePredicate(scope, binary.Left, negated),
                        TranslatePredicate(scope, binary.Right, negated));

                case BinaryExpression binary when binary.NodeType == ExpressionType.Equal || binary.NodeType == ExpressionType.NotEqual:
                {
                    ExpressionType op = binary.NodeType;

                    if (negated)
                    {
                        op = op == ExpressionType.Equal ? ExpressionType.NotEqual : ExpressionType.Equal;
                    }

                    return TranslateComparison(scope, op, binary.Left, binary.Right);
                }

                case BinaryExpression binary when IsComparison(binary.NodeType):
                {
                    SparqlExpression comparison = TranslateComparison(scope, binary.NodeType, binary.Left, binary.Right);

                    return negated ? new SparqlUnaryExpression(SparqlUnaryOperator.Not, comparison) : comparison;
                }

                case UnaryExpression unary when unary.NodeType == ExpressionType.Not:
                    return TranslatePredicate(scope, unary.Operand, !negated);

                case MethodCallExpression callExpression when callExpression.Method.Name == "Equals" && callExpression.Object != null && callExpression.Arguments.Count == 1:
                    return TranslateComparison(
                        scope,
                        negated ? ExpressionType.NotEqual : ExpressionType.Equal,
                        callExpression.Object,
                        callExpression.Arguments[0]);

                case MethodCallExpression callExpression when callExpression.Method.Name == "Any" && callExpression.Method.DeclaringType == typeof(Enumerable):
                    return TranslateAny(scope, callExpression, negated);

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
                        Expression.Constant(true));

                default:
                    throw new NotSupportedException($"Unsupported predicate expression: {expression.NodeType}.");
            }
        }

        private SparqlExpression TranslateComparison(QueryScope scope, ExpressionType op, Expression left, Expression right)
        {
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
                return TranslateChainComparison(scope, op, chain, constant);
            }

            return new SparqlBinaryExpression(MapComparison(op), TranslateOperand(scope, left), TranslateOperand(scope, right));
        }

        private SparqlExpression TranslateChainComparison(QueryScope scope, ExpressionType op, ChainInfo chain, ConstantExpression constant)
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
                    MemberBinding binding = BindCount(scope, chain.Chain);

                    // A correlated count is always bound (zero included), so plain comparisons suffice.
                    return new SparqlBinaryExpression(
                        MapComparison(op),
                        new SparqlVariableExpression(binding.Variable.Name),
                        new SparqlConstantExpression(ToTerm(value)));
                }

                case ChainKind.Uri:
                    return TranslateReferenceComparison(scope, op, chain.Chain, value);

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
                        return TranslateReferenceComparison(scope, op, chain.Chain, value);
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

                    MemberBinding binding = BindChain(scope, chain.Chain, false);

                    return BindingComparison(op, binding, value);
                }
            }
        }

        /// <summary>
        /// Compares a resource- or URI-valued member against an IRI. Inequality must also match
        /// resources where the member is absent, so it binds optionally and includes unbound values.
        /// </summary>
        private SparqlExpression TranslateReferenceComparison(QueryScope scope, ExpressionType op, MemberExpression chain, object value)
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

            MemberBinding binding = BindChain(scope, chain, false);

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
            group.Add(new TriplePattern(parent, new IriTerm(GetPredicate(member.Member)), FreshVariable()));

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

            group.Add(new TriplePattern(parent, new IriTerm(GetPredicate(member.Member)), subject));

            if (call.Arguments.Count == 2)
            {
                LambdaExpression lambda = GetLambda(call.Arguments[1]);
                var child = new QueryScope(lambda.Parameters[0], subject, group, scope);

                group.AddFilter(TranslatePredicate(child, lambda.Body, false));
            }

            return new SparqlExistsExpression(group, negated);
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
                            return new SparqlVariableExpression(BindCount(scope, chain.Chain).Variable.Name);

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
                && call.Arguments.Count == 1
                && Unwrap(call.Arguments[0]) is MemberExpression source
                && IsMappedChain(source))
            {
                return new ChainInfo { Kind = ChainKind.Count, Chain = source, MemberType = typeof(int), RootParameter = GetRootParameter(source) };
            }

            if (!(expression is MemberExpression member))
            {
                return null;
            }

            if (GetPredicate(member.Member) == null)
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
                if (GetPredicate(m.Member) == null)
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
                Uri predicate = GetPredicate(m.Member);

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

        #region Build

        private QueryTranslation Build()
        {
            if (_terminal == TerminalKind.Last || _terminal == TerminalKind.LastOrDefault)
            {
                // Last is First over inverted orderings.
                foreach (OrderCondition ordering in _orderings)
                {
                    ordering.Descending = !ordering.Descending;
                }
            }

            GroupGraphPattern selection = BuildSubjectSelection();

            SparqlQueryModel query;
            string projectedVariable = null;

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

                case QueryExecutionKind.Bindings:
                    query = BuildBindingsQuery(selection, out projectedVariable);
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
                Terminal = _terminal,
                ProjectedVariable = projectedVariable
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
        /// Builds the <c>SELECT ?s ?p ?o WHERE { ?s ?p ?o . ... }</c> shape that
        /// <c>Model.GetResources&lt;T&gt;</c> materializes. With paging (limit/offset) the subject
        /// selection is nested as <c>{ SELECT DISTINCT ?s WHERE { ... } ... }</c> so the operators
        /// apply per resource, not per triple; without paging the selection stays in the top-level
        /// group and the ordering is applied to the triple rows directly — engines are not required
        /// to preserve a sub-select's order through the outer join. For resource-member projections
        /// the projected member variable takes the place of ?s.
        /// </summary>
        private SparqlQueryModel BuildResourceQuery(GroupGraphPattern selection)
        {
            VariableTerm subject = _resultTerm ?? Subject;

            var outer = new SelectQuery();
            outer.Projections.Add(new Projection(subject));
            outer.Projections.Add(new Projection(new VariableTerm("p")));
            outer.Projections.Add(new Projection(new VariableTerm("o")));
            outer.Where.Add(new TriplePattern(subject, new VariableTerm("p"), new VariableTerm("o")));

            if (_limit.HasValue || _offset.HasValue)
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
            var select = new SelectQuery { Where = selection, Limit = _limit, Offset = _offset };

            Type type = _projection.MemberType;

            if (_projection.IsOptional && type != null && type.IsValueType && type != typeof(string))
            {
                object defaultValue = TypeHelper.GetDefaultValue(type);
                var defaultTerm = new LiteralTerm(XsdTypeMapper.SerializeObject(defaultValue), XsdTypeMapper.GetXsdTypeUri(type));
                var alias = new VariableTerm(_projection.Variable.Name + "_");

                select.Projections.Add(new Projection(alias, new SparqlFunctionExpression(
                    "COALESCE",
                    new SparqlVariableExpression(_projection.Variable.Name),
                    new SparqlConstantExpression(defaultTerm))));

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
            return new VariableTerm("v" + _variableCounter++);
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
