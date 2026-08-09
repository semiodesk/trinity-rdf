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
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Query.Sparql
{
    /// <summary>
    /// Serializes a <see cref="SparqlQueryModel"/> (our owned SPARQL AST) to a SPARQL query string.
    /// IRIs are always written in full (<c>&lt;...&gt;</c>) so the output is independent of any
    /// registered prefixes.
    /// </summary>
    internal sealed class SparqlQueryWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public static string Write(SparqlQueryModel query)
        {
            var writer = new SparqlQueryWriter();
            writer.WriteQuery(query);
            return writer._builder.ToString().Trim();
        }

        private void WriteQuery(SparqlQueryModel query)
        {
            switch (query)
            {
                case SelectQuery select:
                    WriteSelect(select);
                    break;
                case AskQuery ask:
                    _builder.Append("ASK ");
                    WriteGroup(ask.Where);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported query root: {query.GetType().Name}");
            }
        }

        private void WriteSelect(SelectQuery select)
        {
            _builder.Append("SELECT ");

            if (select.IsDistinct)
            {
                _builder.Append("DISTINCT ");
            }

            if (select.Projections.Count == 0)
            {
                _builder.Append("* ");
            }
            else
            {
                foreach (var projection in select.Projections)
                {
                    WriteProjection(projection);
                    _builder.Append(' ');
                }
            }

            _builder.Append("WHERE ");
            WriteGroup(select.Where);

            if (select.GroupBy.Count > 0)
            {
                _builder.Append(" GROUP BY");
                foreach (var expression in select.GroupBy)
                {
                    _builder.Append(' ');
                    WriteExpression(expression);
                }
            }

            if (select.Having != null)
            {
                _builder.Append(" HAVING (");
                WriteExpression(select.Having);
                _builder.Append(')');
            }

            if (select.OrderBy.Count > 0)
            {
                _builder.Append(" ORDER BY");
                foreach (var ordering in select.OrderBy)
                {
                    _builder.Append(' ');
                    WriteOrdering(ordering);
                }
            }

            if (select.Limit.HasValue)
            {
                _builder.Append(" LIMIT ").Append(select.Limit.Value);
            }

            if (select.Offset.HasValue)
            {
                _builder.Append(" OFFSET ").Append(select.Offset.Value);
            }
        }

        private void WriteProjection(Projection projection)
        {
            if (projection.Expression == null)
            {
                WriteTerm(projection.Variable);
            }
            else
            {
                _builder.Append('(');
                WriteExpression(projection.Expression);
                _builder.Append(" AS ");
                WriteTerm(projection.Variable);
                _builder.Append(')');
            }
        }

        private void WriteOrdering(OrderCondition ordering)
        {
            if (ordering.Descending)
            {
                _builder.Append("DESC(");
                WriteExpression(ordering.Expression);
                _builder.Append(')');
            }
            else
            {
                WriteExpression(ordering.Expression);
            }
        }

        private void WriteGroup(GroupGraphPattern group)
        {
            _builder.Append("{ ");

            foreach (var pattern in group.Patterns)
            {
                WritePattern(pattern);
                _builder.Append(' ');
            }

            foreach (var filter in group.Filters)
            {
                _builder.Append("FILTER(");
                WriteExpression(filter);
                _builder.Append(") ");
            }

            _builder.Append('}');
        }

        private void WritePattern(GraphPattern pattern)
        {
            switch (pattern)
            {
                case TriplePattern triple:
                    WriteTerm(triple.Subject);
                    _builder.Append(' ');
                    WriteTerm(triple.Predicate);
                    _builder.Append(' ');
                    WriteTerm(triple.Object);
                    _builder.Append(" .");
                    break;
                case OptionalPattern optional:
                    _builder.Append("OPTIONAL ");
                    WriteGroup(optional.Pattern);
                    break;
                case UnionPattern union:
                    WriteGroup(union.Left);
                    _builder.Append(" UNION ");
                    WriteGroup(union.Right);
                    break;
                case MinusPattern minus:
                    _builder.Append("MINUS ");
                    WriteGroup(minus.Pattern);
                    break;
                case SubSelectPattern subSelect:
                    _builder.Append("{ ");
                    WriteSelect(subSelect.Query);
                    _builder.Append(" }");
                    break;
                case BindPattern bind:
                    _builder.Append("BIND(");
                    WriteExpression(bind.Expression);
                    _builder.Append(" AS ");
                    WriteTerm(bind.Variable);
                    _builder.Append(')');
                    break;
                case ValuesPattern values:
                    _builder.Append("VALUES ");
                    WriteTerm(values.Variable);
                    _builder.Append(" { ");
                    foreach (var value in values.Values)
                    {
                        WriteTerm(value);
                        _builder.Append(' ');
                    }
                    _builder.Append('}');
                    break;
                default:
                    throw new NotSupportedException($"Unsupported graph pattern: {pattern.GetType().Name}");
            }
        }

        private void WriteTerm(SparqlTerm term)
        {
            switch (term)
            {
                case VariableTerm variable:
                    _builder.Append('?').Append(variable.Name);
                    break;
                case IriTerm iri:
                    _builder.Append('<').Append(iri.Value.AbsoluteUri).Append('>');
                    break;
                case RdfTypeTerm _:
                    _builder.Append('a');
                    break;
                case LiteralTerm literal:
                    WriteLiteral(literal);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported term: {term.GetType().Name}");
            }
        }

        private void WriteLiteral(LiteralTerm literal)
        {
            _builder.Append('"').Append(Escape(literal.Value)).Append('"');

            if (!string.IsNullOrEmpty(literal.Language))
            {
                _builder.Append('@').Append(literal.Language);
            }
            else if (literal.Datatype != null)
            {
                _builder.Append("^^<").Append(literal.Datatype.AbsoluteUri).Append('>');
            }
        }

        private void WriteExpression(SparqlExpression expression)
        {
            switch (expression)
            {
                case SparqlVariableExpression variable:
                    _builder.Append('?').Append(variable.Name);
                    break;
                case SparqlConstantExpression constant:
                    WriteTerm(constant.Term);
                    break;
                case SparqlBinaryExpression binary:
                    _builder.Append('(');
                    WriteExpression(binary.Left);
                    _builder.Append(' ').Append(BinaryOperator(binary.Operator)).Append(' ');
                    WriteExpression(binary.Right);
                    _builder.Append(')');
                    break;
                case SparqlUnaryExpression unary:
                    if (unary.Operator == SparqlUnaryOperator.Not)
                    {
                        _builder.Append("!(");
                    }
                    else
                    {
                        _builder.Append("-(");
                    }
                    WriteExpression(unary.Operand);
                    _builder.Append(')');
                    break;
                case SparqlFunctionExpression function:
                    _builder.Append(function.Name).Append('(');
                    for (int i = 0; i < function.Arguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            _builder.Append(", ");
                        }
                        WriteExpression(function.Arguments[i]);
                    }
                    _builder.Append(')');
                    break;
                case SparqlExistsExpression exists:
                    _builder.Append(exists.Negate ? "NOT EXISTS " : "EXISTS ");
                    WriteGroup(exists.Pattern);
                    break;
                case SparqlInExpression inExpression:
                    WriteExpression(inExpression.Value);
                    _builder.Append(inExpression.Negate ? " NOT IN (" : " IN (");
                    for (int i = 0; i < inExpression.Set.Count; i++)
                    {
                        if (i > 0)
                        {
                            _builder.Append(", ");
                        }
                        WriteExpression(inExpression.Set[i]);
                    }
                    _builder.Append(')');
                    break;
                case SparqlAggregateExpression aggregate:
                    WriteAggregate(aggregate);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported expression: {expression.GetType().Name}");
            }
        }

        private void WriteAggregate(SparqlAggregateExpression aggregate)
        {
            _builder.Append(AggregateName(aggregate.Kind)).Append('(');

            if (aggregate.Distinct)
            {
                _builder.Append("DISTINCT ");
            }

            if (aggregate.Argument == null)
            {
                _builder.Append('*');
            }
            else
            {
                WriteExpression(aggregate.Argument);
            }

            _builder.Append(')');
        }

        private static string BinaryOperator(SparqlBinaryOperator op)
        {
            switch (op)
            {
                case SparqlBinaryOperator.Equal: return "=";
                case SparqlBinaryOperator.NotEqual: return "!=";
                case SparqlBinaryOperator.LessThan: return "<";
                case SparqlBinaryOperator.LessThanOrEqual: return "<=";
                case SparqlBinaryOperator.GreaterThan: return ">";
                case SparqlBinaryOperator.GreaterThanOrEqual: return ">=";
                case SparqlBinaryOperator.And: return "&&";
                case SparqlBinaryOperator.Or: return "||";
                case SparqlBinaryOperator.Add: return "+";
                case SparqlBinaryOperator.Subtract: return "-";
                case SparqlBinaryOperator.Multiply: return "*";
                case SparqlBinaryOperator.Divide: return "/";
                default: throw new NotSupportedException($"Unsupported binary operator: {op}");
            }
        }

        private static string AggregateName(SparqlAggregateKind kind)
        {
            switch (kind)
            {
                case SparqlAggregateKind.Count: return "COUNT";
                case SparqlAggregateKind.Sum: return "SUM";
                case SparqlAggregateKind.Min: return "MIN";
                case SparqlAggregateKind.Max: return "MAX";
                case SparqlAggregateKind.Average: return "AVG";
                case SparqlAggregateKind.Sample: return "SAMPLE";
                case SparqlAggregateKind.GroupConcat: return "GROUP_CONCAT";
                default: throw new NotSupportedException($"Unsupported aggregate: {kind}");
            }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
