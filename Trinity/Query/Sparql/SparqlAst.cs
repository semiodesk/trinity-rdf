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

namespace Semiodesk.Trinity.Query.Sparql
{
    // A minimal, owned SPARQL abstract syntax tree. The LINQ provider folds LINQ operators into
    // this AST and a serializer (SparqlQueryWriter) emits the query text. Keeping our own AST — as
    // EF Core does with SelectExpression — decouples the LINQ layer from dotNetRDF's Query Builder
    // and gives us room for property paths, custom functions, and SPARQL* (ADR: LINQ provider).
    //
    // SPARQL is smaller than SQL here: joins are implicit (shared variables in a Basic Graph
    // Pattern), and UNION/OPTIONAL are graph-pattern nodes rather than set operations over SELECTs.
    // Node variants are enum-discriminated to keep the type count low.

    #region Query roots

    /// <summary>Base class for a SPARQL query (SELECT / ASK).</summary>
    internal abstract class SparqlQueryModel
    {
        /// <summary>The top-level graph pattern (the WHERE clause body).</summary>
        public GroupGraphPattern Where { get; set; } = new GroupGraphPattern();
    }

    /// <summary>A SPARQL <c>SELECT</c> query.</summary>
    internal sealed class SelectQuery : SparqlQueryModel
    {
        /// <summary>Projected items (variables or expressions bound to a variable). Empty = <c>SELECT *</c>.</summary>
        public List<Projection> Projections { get; } = new List<Projection>();

        public bool IsDistinct { get; set; }

        public List<OrderCondition> OrderBy { get; } = new List<OrderCondition>();

        public List<SparqlExpression> GroupBy { get; } = new List<SparqlExpression>();

        public SparqlExpression Having { get; set; }

        public int? Limit { get; set; }

        public int? Offset { get; set; }
    }

    /// <summary>A SPARQL <c>ASK</c> query.</summary>
    internal sealed class AskQuery : SparqlQueryModel
    {
    }

    /// <summary>A projected item: a bare variable, or an expression aliased to a variable (<c>(expr AS ?v)</c>).</summary>
    internal sealed class Projection
    {
        public VariableTerm Variable { get; set; }

        /// <summary>Optional expression; when set the projection is <c>(Expression AS Variable)</c>.</summary>
        public SparqlExpression Expression { get; set; }

        public Projection(VariableTerm variable, SparqlExpression expression = null)
        {
            Variable = variable;
            Expression = expression;
        }
    }

    #endregion

    #region Graph patterns

    /// <summary>Base class for a graph pattern (an element of a WHERE body).</summary>
    internal abstract class GraphPattern
    {
    }

    /// <summary>A group graph pattern: a brace-delimited block of patterns plus FILTERs.</summary>
    internal sealed class GroupGraphPattern : GraphPattern
    {
        public List<GraphPattern> Patterns { get; } = new List<GraphPattern>();

        public List<SparqlExpression> Filters { get; } = new List<SparqlExpression>();

        public void Add(GraphPattern pattern) => Patterns.Add(pattern);

        public void AddFilter(SparqlExpression filter) => Filters.Add(filter);
    }

    /// <summary>A single triple pattern <c>s p o</c>.</summary>
    internal sealed class TriplePattern : GraphPattern
    {
        public SparqlTerm Subject { get; set; }

        public SparqlTerm Predicate { get; set; }

        public SparqlTerm Object { get; set; }

        public TriplePattern(SparqlTerm subject, SparqlTerm predicate, SparqlTerm @object)
        {
            Subject = subject;
            Predicate = predicate;
            Object = @object;
        }
    }

    /// <summary>An <c>OPTIONAL { ... }</c> pattern (SPARQL's left-outer join).</summary>
    internal sealed class OptionalPattern : GraphPattern
    {
        public GroupGraphPattern Pattern { get; set; }

        public OptionalPattern(GroupGraphPattern pattern) => Pattern = pattern;
    }

    /// <summary>A <c>{ ... } UNION { ... }</c> pattern.</summary>
    internal sealed class UnionPattern : GraphPattern
    {
        public GroupGraphPattern Left { get; set; }

        public GroupGraphPattern Right { get; set; }

        public UnionPattern(GroupGraphPattern left, GroupGraphPattern right)
        {
            Left = left;
            Right = right;
        }
    }

    /// <summary>A <c>MINUS { ... }</c> pattern.</summary>
    internal sealed class MinusPattern : GraphPattern
    {
        public GroupGraphPattern Pattern { get; set; }

        public MinusPattern(GroupGraphPattern pattern) => Pattern = pattern;
    }

    /// <summary>A nested <c>{ SELECT ... }</c> sub-query used as a pattern.</summary>
    internal sealed class SubSelectPattern : GraphPattern
    {
        public SelectQuery Query { get; set; }

        public SubSelectPattern(SelectQuery query) => Query = query;
    }

    /// <summary>A <c>BIND(expr AS ?v)</c> pattern.</summary>
    internal sealed class BindPattern : GraphPattern
    {
        public SparqlExpression Expression { get; set; }

        public VariableTerm Variable { get; set; }

        public BindPattern(SparqlExpression expression, VariableTerm variable)
        {
            Expression = expression;
            Variable = variable;
        }
    }

    /// <summary>An inline <c>VALUES ?v { ... }</c> pattern (used for <c>Contains</c> on a collection).</summary>
    internal sealed class ValuesPattern : GraphPattern
    {
        public VariableTerm Variable { get; set; }

        public List<SparqlTerm> Values { get; } = new List<SparqlTerm>();

        public ValuesPattern(VariableTerm variable) => Variable = variable;
    }

    #endregion

    #region Terms

    /// <summary>Base class for an RDF term or variable appearing in a pattern.</summary>
    internal abstract class SparqlTerm
    {
    }

    /// <summary>A query variable (<c>?name</c>), without the leading '?'.</summary>
    internal sealed class VariableTerm : SparqlTerm
    {
        public string Name { get; }

        public VariableTerm(string name) => Name = name;
    }

    /// <summary>An IRI term, serialized as <c>&lt;absolute-uri&gt;</c>.</summary>
    internal sealed class IriTerm : SparqlTerm
    {
        public Uri Value { get; }

        public IriTerm(Uri value) => Value = value;
    }

    /// <summary>An RDF literal, optionally typed (<c>^^&lt;datatype&gt;</c>) or language-tagged (<c>@lang</c>).</summary>
    internal sealed class LiteralTerm : SparqlTerm
    {
        public string Value { get; }

        public Uri Datatype { get; }

        public string Language { get; }

        public LiteralTerm(string value, Uri datatype = null, string language = null)
        {
            Value = value;
            Datatype = datatype;
            Language = language;
        }
    }

    /// <summary>The <c>a</c> keyword (shorthand for <c>rdf:type</c>) as a predicate term.</summary>
    internal sealed class RdfTypeTerm : SparqlTerm
    {
        public static readonly RdfTypeTerm Instance = new RdfTypeTerm();

        private RdfTypeTerm() { }
    }

    #endregion

    #region Modifiers

    /// <summary>An <c>ORDER BY</c> condition.</summary>
    internal sealed class OrderCondition
    {
        public SparqlExpression Expression { get; set; }

        public bool Descending { get; set; }

        public OrderCondition(SparqlExpression expression, bool descending = false)
        {
            Expression = expression;
            Descending = descending;
        }
    }

    #endregion
}
