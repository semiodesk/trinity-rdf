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

using System.Collections.Generic;

namespace Semiodesk.Trinity.Query.Sparql
{
    // The SPARQL expression tree used inside FILTER, BIND, HAVING and ORDER BY. Node types are
    // prefixed "Sparql..." so they never collide with the identically-named System.Linq.Expressions
    // types (BinaryExpression, UnaryExpression, ConstantExpression) that the translator imports.

    /// <summary>Operators for a <see cref="SparqlBinaryExpression"/>.</summary>
    internal enum SparqlBinaryOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        And,
        Or,
        Add,
        Subtract,
        Multiply,
        Divide
    }

    /// <summary>Operators for a <see cref="SparqlUnaryExpression"/>.</summary>
    internal enum SparqlUnaryOperator
    {
        Not,
        Negate
    }

    /// <summary>SPARQL set/aggregate function kinds.</summary>
    internal enum SparqlAggregateKind
    {
        Count,
        Sum,
        Min,
        Max,
        Average,
        Sample,
        GroupConcat
    }

    /// <summary>Base class for a SPARQL expression (appears in FILTER / BIND / HAVING / ORDER BY).</summary>
    internal abstract class SparqlExpression
    {
    }

    /// <summary>A binary operation, e.g. <c>?a = ?b</c> or <c>?a &amp;&amp; ?b</c>.</summary>
    internal sealed class SparqlBinaryExpression : SparqlExpression
    {
        public SparqlBinaryOperator Operator { get; set; }

        public SparqlExpression Left { get; set; }

        public SparqlExpression Right { get; set; }

        public SparqlBinaryExpression(SparqlBinaryOperator op, SparqlExpression left, SparqlExpression right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }
    }

    /// <summary>A unary operation, e.g. <c>!?a</c> or <c>-?a</c>.</summary>
    internal sealed class SparqlUnaryExpression : SparqlExpression
    {
        public SparqlUnaryOperator Operator { get; set; }

        public SparqlExpression Operand { get; set; }

        public SparqlUnaryExpression(SparqlUnaryOperator op, SparqlExpression operand)
        {
            Operator = op;
            Operand = operand;
        }
    }

    /// <summary>A reference to a query variable within an expression (<c>?name</c>).</summary>
    internal sealed class SparqlVariableExpression : SparqlExpression
    {
        public string Name { get; }

        public SparqlVariableExpression(string name) => Name = name;
    }

    /// <summary>A constant term (literal or IRI) used within an expression.</summary>
    internal sealed class SparqlConstantExpression : SparqlExpression
    {
        public SparqlTerm Term { get; }

        public SparqlConstantExpression(SparqlTerm term) => Term = term;
    }

    /// <summary>A built-in or IRI function call, e.g. <c>REGEX(...)</c>, <c>CONTAINS(...)</c>, <c>BOUND(...)</c>.</summary>
    internal sealed class SparqlFunctionExpression : SparqlExpression
    {
        public string Name { get; }

        public List<SparqlExpression> Arguments { get; } = new List<SparqlExpression>();

        public SparqlFunctionExpression(string name, params SparqlExpression[] arguments)
        {
            Name = name;

            if (arguments != null)
            {
                Arguments.AddRange(arguments);
            }
        }
    }

    /// <summary>A <c>FILTER (NOT) EXISTS { ... }</c> expression.</summary>
    internal sealed class SparqlExistsExpression : SparqlExpression
    {
        public bool Negate { get; set; }

        public GroupGraphPattern Pattern { get; set; }

        public SparqlExistsExpression(GroupGraphPattern pattern, bool negate = false)
        {
            Pattern = pattern;
            Negate = negate;
        }
    }

    /// <summary>An <c>?x IN (...)</c> / <c>?x NOT IN (...)</c> expression.</summary>
    internal sealed class SparqlInExpression : SparqlExpression
    {
        public SparqlExpression Value { get; set; }

        public List<SparqlExpression> Set { get; } = new List<SparqlExpression>();

        public bool Negate { get; set; }

        public SparqlInExpression(SparqlExpression value, bool negate = false)
        {
            Value = value;
            Negate = negate;
        }
    }

    /// <summary>An aggregate expression, e.g. <c>COUNT(?x)</c>, <c>SUM(?x)</c>.</summary>
    internal sealed class SparqlAggregateExpression : SparqlExpression
    {
        public SparqlAggregateKind Kind { get; set; }

        /// <summary>The aggregated expression; <c>null</c> means <c>COUNT(*)</c>.</summary>
        public SparqlExpression Argument { get; set; }

        public bool Distinct { get; set; }

        public SparqlAggregateExpression(SparqlAggregateKind kind, SparqlExpression argument = null, bool distinct = false)
        {
            Kind = kind;
            Argument = argument;
            Distinct = distinct;
        }
    }
}
