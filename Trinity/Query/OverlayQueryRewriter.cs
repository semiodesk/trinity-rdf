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
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Functions.Sparql.Boolean;
using VDS.RDF.Query.Expressions.Primary;
using VDS.RDF.Query.Patterns;
using DnrParsing = VDS.RDF.Parsing;
using DnrQuery = VDS.RDF.Query;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Rewrites a caller-supplied SPARQL query so that every triple pattern resolves against an
    /// <see cref="ILayeredModel"/>'s effective graph instead of a single named graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subtraction cannot live in a dataset clause, so a query run unchanged against a layered view
    /// gives a wrong answer with no error — see <c>doc/adr/0041-layered-read-views.md</c>. This class
    /// makes the honest version possible for the query forms it can prove it handles, and refuses
    /// every other form with a reason.
    /// </para>
    /// <para>
    /// It works on the query's <b>parse tree</b>, not its text. That matters: the abbreviations that
    /// make text-level rewriting impractical — predicate-object lists (<c>;</c>), object lists
    /// (<c>,</c>) and blank-node property lists (<c>[ ]</c>) — are already expanded into plain triple
    /// patterns by the time the parser is done, so there is nothing left to disentangle.
    /// </para>
    /// <para>
    /// <b>Whitelist, not blacklist.</b> Every pattern kind, and every filter expression, must be
    /// recognised as safe; anything else throws. That is what keeps an unhandled form from silently
    /// returning triples staged for removal.
    /// </para>
    /// </remarks>
    internal static class OverlayQueryRewriter
    {
        /// <summary>
        /// Rewrites <paramref name="queryString"/> against <paramref name="model"/>'s three graphs.
        /// </summary>
        /// <remarks>
        /// The input must already have its Trinity <c>@parameters</c> substituted: the strict SPARQL
        /// parser used here rejects them outright. Callers therefore pass
        /// <c>ISparqlQuery.ToString()</c>, which performs the substitution.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        /// Thrown, with the specific reason, if the query cannot be rewritten faithfully.
        /// </exception>
        internal static string Rewrite(ILayeredModel model, string queryString)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            DnrQuery.SparqlQuery parsed = Parse(queryString);

            string rewritten = new Walker(model).RewriteQuery(parsed, outermost: true);

            // Re-parse the result and compare the parts this class did not intend to change. A
            // serialization slip would otherwise be indistinguishable from a correct rewrite, and
            // the whole point of the overlay is that wrong answers are never silent.
            VerifyRoundTrip(parsed, rewritten, queryString);

            return rewritten;
        }

        /// <summary>
        /// Refuses a caller query that selects graphs of its own, without otherwise constraining it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used by a <b>materialized</b> view, which runs caller SPARQL natively and so lifts every
        /// refusal that exists for want of a faithful rewrite - property paths, <c>CONSTRUCT</c>,
        /// <c>DESCRIBE</c>, inferencing. Graph selection is not in that category. The effective triples
        /// live in one ordinary graph and the view names it in the dataset clause; a caller
        /// <c>FROM</c> is merely appended beside it, giving a union of the two, and a caller
        /// <c>GRAPH</c> block reaches for graphs the view exists to combine. Either way the answer can
        /// contain triples staged for removal, which is the one failure this design must not have.
        /// </para>
        /// <para>
        /// <paramref name="effectiveGraph"/> is therefore required, not optional: the check is
        /// "no graph but this one", not "no graph at all". Assigning
        /// <see cref="ISparqlQuery.Model"/> injects <c>FROM &lt;effectiveGraph&gt;</c> into the query,
        /// so by the time it is serialized for parsing here the view's own clause is present and
        /// indistinguishable in kind from a caller's.
        /// </para>
        /// <para>
        /// The cost is that a materialized view still parses each caller query once per execution, even
        /// though it does not rewrite it - see ADR-0041 for the placeholder-IRI caching that would
        /// remove it if it ever shows up in a profile.
        /// </para>
        /// </remarks>
        internal static void RequireNoGraphSelection(string queryString, Uri effectiveGraph)
        {
            if (effectiveGraph == null)
            {
                throw new ArgumentNullException(nameof(effectiveGraph));
            }

            RequireNoGraphSelection(Parse(queryString), effectiveGraph, outermost: true);
        }

        private static void RequireNoGraphSelection(DnrQuery.SparqlQuery query, Uri effectiveGraph, bool outermost)
        {
            // The view's own clause has to be accepted, not just any clause refused. Assigning
            // ISparqlQuery.Model injects FROM <effective>, and ToString() - which is what gets parsed
            // here - therefore already carries it. The LINQ provider assigns Model at construction, and
            // executing a query object mutates it, so a blanket refusal broke every LINQ query and made
            // re-executing any query object fail on the second call.
            foreach (IRefNode name in query.DefaultGraphNames)
            {
                if (!IsGraph(name, effectiveGraph))
                {
                    throw GraphSelectionUnsupported($"a dataset clause of its own (FROM <{Name(name)}>)");
                }
            }

            // Materialized mode never injects a named graph, so any of these is the caller's. A
            // sub-SELECT may not carry a dataset clause at all.
            if (query.NamedGraphNames.Any())
            {
                throw GraphSelectionUnsupported(
                    $"a dataset clause of its own (FROM NAMED <{Name(query.NamedGraphNames.First())}>)");
            }

            if (!outermost && query.DefaultGraphNames.Any())
            {
                throw GraphSelectionUnsupported("a dataset clause on a sub-SELECT");
            }

            // Null for a query with no WHERE clause - a bare DESCRIBE <iri> is the common case. No
            // pattern means there is no graph selection to find.
            if (query.RootGraphPattern != null)
            {
                RequireNoGraphSelection(query.RootGraphPattern, effectiveGraph);
            }
        }

        private static void RequireNoGraphSelection(DnrQuery.Patterns.GraphPattern pattern, Uri effectiveGraph)
        {
            if (pattern.IsGraph)
            {
                throw GraphSelectionUnsupported($"an explicit GRAPH block (GRAPH {pattern.GraphSpecifier?.Value})");
            }

            // A sub-SELECT carries its own pattern tree, and may not declare a dataset at all.
            foreach (DnrQuery.Patterns.ITriplePattern triplePattern in pattern.TriplePatterns)
            {
                if (triplePattern is DnrQuery.Patterns.SubQueryPattern subQuery)
                {
                    RequireNoGraphSelection(subQuery.SubQuery, effectiveGraph, outermost: false);
                }
            }

            // FILTER EXISTS / NOT EXISTS holds its pattern in the filter's *expression* tree, not in
            // ChildGraphPatterns, so a GRAPH block nested in one is reached by neither loop. Left
            // unchecked it was a silent wrong answer rather than a refusal: the dataset clause is a
            // bare FROM, so the named-graph set is empty and the nested GRAPH matched nothing - the
            // caller asked about a named graph and was told it was empty.
            foreach (VDS.RDF.Query.Filters.ISparqlFilter filter in Filters(pattern))
            {
                RequireNoGraphSelectionInExpression(filter.Expression, effectiveGraph);
            }

            foreach (DnrQuery.Patterns.IAssignmentPattern assignment in pattern.UnplacedAssignments)
            {
                RequireNoGraphSelectionInExpression(assignment.AssignExpression, effectiveGraph);
            }

            foreach (DnrQuery.Patterns.ITriplePattern triplePattern in pattern.TriplePatterns)
            {
                if (triplePattern is DnrQuery.Patterns.IAssignmentPattern inlineAssignment)
                {
                    RequireNoGraphSelectionInExpression(inlineAssignment.AssignExpression, effectiveGraph);
                }
                else if (triplePattern is DnrQuery.Patterns.FilterPattern inlineFilter)
                {
                    RequireNoGraphSelectionInExpression(inlineFilter.Filter.Expression, effectiveGraph);
                }
            }

            foreach (DnrQuery.Patterns.GraphPattern child in pattern.ChildGraphPatterns)
            {
                RequireNoGraphSelection(child, effectiveGraph);
            }
        }

        /// <summary>
        /// Walks an expression for the graph patterns <c>EXISTS</c> / <c>NOT EXISTS</c> carry.
        /// </summary>
        /// <remarks>
        /// Unlike rewriting mode, which refuses these outright because it cannot weave the overlay into
        /// them, a materialized view is happy to evaluate one - it is an ordinary graph. Only the graph
        /// selection inside has to be refused, so this recurses into the pattern rather than rejecting
        /// the expression.
        /// </remarks>
        private static void RequireNoGraphSelectionInExpression(ISparqlExpression expression, Uri effectiveGraph)
        {
            if (expression == null)
            {
                return;
            }

            if (expression is ExistsFunction exists && exists.Pattern != null)
            {
                RequireNoGraphSelection(exists.Pattern, effectiveGraph);
            }
            else if (expression is GraphPatternTerm term && term.Pattern != null)
            {
                RequireNoGraphSelection(term.Pattern, effectiveGraph);
            }

            foreach (ISparqlExpression argument in expression.Arguments)
            {
                RequireNoGraphSelectionInExpression(argument, effectiveGraph);
            }
        }

        private static bool IsGraph(IRefNode name, Uri expected)
        {
            return name is IUriNode uri && uri.Uri.Equals(expected);
        }

        private static string Name(IRefNode name)
        {
            return name is IUriNode uri ? uri.Uri.ToString() : name?.ToString() ?? "?";
        }

        private static NotSupportedException GraphSelectionUnsupported(string what)
        {
            return new NotSupportedException(
                "This query cannot be run against a layered model because it contains " + what + ". " +
                "A layered view defines its own dataset - the effective triples of the baseline, additions and " +
                "removals - and names it itself, so a query cannot also choose one. This is refused even on a " +
                "materialized view, where the rewrite-shape restrictions do not apply: naming another graph reads " +
                "past the view rather than through it, and the answer could include triples staged for removal. " +
                "Query the Baseline, Additions or Removals models directly if that is what you want. " +
                "See doc/adr/0041-layered-read-views.md.");
        }

        private static DnrQuery.SparqlQuery Parse(string queryString)
        {
            try
            {
                return new DnrParsing.SparqlQueryParser().ParseFromString(queryString);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(
                    "The query could not be parsed, so it cannot be checked against the layered view's " +
                    "baseline/additions/removals overlay - which means either rewriting it to honour the overlay, " +
                    "or, on a materialized view, confirming it selects no graph of its own. Note that Trinity's " +
                    "own tokenizer accepts a wider, extended syntax than the strict SPARQL parser used here, so a " +
                    "query may be accepted by a plain model and refused by a layered one. Parser error: " +
                    ex.Message, ex);
            }
        }

        /// <summary>
        /// Confirms the rewrite preserved everything outside the WHERE clause.
        /// </summary>
        private static void VerifyRoundTrip(DnrQuery.SparqlQuery original, string rewritten, string queryString)
        {
            DnrQuery.SparqlQuery check;

            try
            {
                check = new DnrParsing.SparqlQueryParser().ParseFromString(rewritten);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(
                    "Rewriting the query for the layered overlay produced SPARQL that does not parse. This is a " +
                    "defect in the rewriter rather than a problem with the query; it is reported instead of " +
                    "executed because an unparseable rewrite is preferable to a silently wrong answer.\n" +
                    "Original: " + queryString + "\nRewritten: " + rewritten, ex);
            }

            var differences = new List<string>();

            if (check.QueryType != original.QueryType)
            {
                differences.Add($"query form {original.QueryType} became {check.QueryType}");
            }

            if (check.Limit != original.Limit)
            {
                differences.Add($"LIMIT {original.Limit} became {check.Limit}");
            }

            if (check.Offset != original.Offset)
            {
                differences.Add($"OFFSET {original.Offset} became {check.Offset}");
            }

            string before = string.Join(",", original.Variables.Where(v => v.IsResultVariable).Select(v => v.Name));
            string after = string.Join(",", check.Variables.Where(v => v.IsResultVariable).Select(v => v.Name));

            if (before != after)
            {
                differences.Add($"projected variables [{before}] became [{after}]");
            }

            // Filters are re-emitted through dotNetRDF's own serialization, which is not always
            // faithful: FILTER(!(?r < 3)) comes back as FILTER(!?r < 3), i.e. (!?r) < 3, and it
            // reparses cleanly. Comparing the expression trees structurally is what turns that
            // upstream defect into a refusal instead of a quietly different answer.
            List<string> filtersBefore = FilterSignatures(original.RootGraphPattern);

            // The overlay adds its own FILTER NOT EXISTS guard for every fully ground pattern, so
            // those have to come out of the comparison. It is safe to drop all of them because a
            // caller's own EXISTS / NOT EXISTS is refused outright - anything matching here is ours.
            List<string> filtersAfter = FilterSignatures(check.RootGraphPattern)
                .Where(signature => !signature.StartsWith("NOT EXISTS(", StringComparison.Ordinal))
                .ToList();

            if (!filtersBefore.SequenceEqual(filtersAfter))
            {
                differences.Add(
                    "a FILTER expression did not survive re-serialization: [" +
                    string.Join(" | ", filtersBefore) + "] became [" + string.Join(" | ", filtersAfter) + "]");
            }

            // HAVING and the projection expressions are not re-emitted by this class - they come from
            // dotNetRDF's serialization of the head and the solution modifiers, which is reused
            // verbatim. So they cannot be repaired here, only checked: a negation inside one is
            // mangled exactly as it is inside a filter, and would otherwise pass unnoticed.
            string havingBefore = SparqlExpressionWriter.Signature(original.Having?.Expression);
            string havingAfter = SparqlExpressionWriter.Signature(check.Having?.Expression);

            if (havingBefore != havingAfter)
            {
                differences.Add($"the HAVING expression {havingBefore} became {havingAfter}");
            }

            string projectionsBefore = ProjectionSignatures(original);
            string projectionsAfter = ProjectionSignatures(check);

            if (projectionsBefore != projectionsAfter)
            {
                differences.Add($"a projected expression changed: [{projectionsBefore}] became [{projectionsAfter}]");
            }

            // GROUP BY and ORDER BY carry expressions too, and reach the output through the same
            // reused serialization as HAVING and the projection - so they are exposed to the same
            // negation defect and need the same check.
            string groupByBefore = GroupBySignature(original.GroupBy);
            string groupByAfter = GroupBySignature(check.GroupBy);

            if (groupByBefore != groupByAfter)
            {
                differences.Add($"the GROUP BY expression {groupByBefore} became {groupByAfter}");
            }

            string orderByBefore = OrderBySignature(original.OrderBy);
            string orderByAfter = OrderBySignature(check.OrderBy);

            if (orderByBefore != orderByAfter)
            {
                differences.Add($"the ORDER BY expression {orderByBefore} became {orderByAfter}");
            }

            if (differences.Count > 0)
            {
                throw new NotSupportedException(
                    "This query cannot be run against a layered model: rewriting it to honour the " +
                    "baseline/additions/removals overlay changed part of it that had to be preserved (" +
                    string.Join("; ", differences) + "). Re-serializing a query relies on dotNetRDF, which is not " +
                    "faithful in every case - notably a negation wrapped around a comparison, where " +
                    "!(?x < 1) comes back as !?x < 1 and still parses. Trinity serializes FILTER and BIND " +
                    "expressions itself to avoid that, but a projection, GROUP BY, HAVING or ORDER BY clause " +
                    "is reused from dotNetRDF's own output and cannot be repaired here, only detected. " +
                    "Rephrasing the expression avoids it (here, ?x >= 1).\nOriginal: " + queryString + "\nRewritten: " + rewritten);
            }
        }

        /// <summary>
        /// Signature of a GROUP BY chain, following <c>Child</c> to the end.
        /// </summary>
        private static string GroupBySignature(VDS.RDF.Query.Grouping.ISparqlGroupBy groupBy)
        {
            var parts = new List<string>();

            for (var current = groupBy; current != null; current = current.Child)
            {
                parts.Add(SparqlExpressionWriter.Signature(current.Expression) +
                          (current.AssignVariable == null ? "" : " AS ?" + current.AssignVariable));
            }

            return parts.Count == 0 ? "(none)" : string.Join(" , ", parts);
        }

        /// <summary>
        /// Signature of an ORDER BY chain, following <c>Child</c> to the end. Direction is included
        /// because reversing it changes the answer just as surely as changing the expression.
        /// </summary>
        private static string OrderBySignature(VDS.RDF.Query.Ordering.ISparqlOrderBy orderBy)
        {
            var parts = new List<string>();

            for (var current = orderBy; current != null; current = current.Child)
            {
                parts.Add((current.Descending ? "DESC" : "ASC") + "(" +
                          SparqlExpressionWriter.Signature(current.Expression) + ")");
            }

            return parts.Count == 0 ? "(none)" : string.Join(" , ", parts);
        }

        /// <summary>
        /// Every filter attached to a group, from both places dotNetRDF can hold one.
        /// </summary>
        private static IEnumerable<VDS.RDF.Query.Filters.ISparqlFilter> Filters(DnrQuery.Patterns.GraphPattern pattern)
        {
            if (pattern.IsFiltered && pattern.Filter != null)
            {
                yield return pattern.Filter;
            }

            foreach (VDS.RDF.Query.Filters.ISparqlFilter filter in pattern.UnplacedFilters)
            {
                yield return filter;
            }
        }

        /// <summary>
        /// Signatures of the projected expressions and aggregates, in projection order.
        /// </summary>
        private static string ProjectionSignatures(DnrQuery.SparqlQuery query)
        {
            var parts = new List<string>();

            foreach (DnrQuery.SparqlVariable variable in query.Variables.Where(v => v.IsResultVariable))
            {
                if (variable.IsAggregate && variable.Aggregate != null)
                {
                    parts.Add(variable.Name + "=" + variable.Aggregate.Functor + "(" +
                              SparqlExpressionWriter.Signature(variable.Aggregate.Expression) + ")");
                }
                else if (variable.IsProjection && variable.Projection != null)
                {
                    parts.Add(variable.Name + "=" + SparqlExpressionWriter.Signature(variable.Projection));
                }
                else
                {
                    parts.Add(variable.Name);
                }
            }

            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Collects a structural signature of every FILTER expression in a pattern tree, so a
        /// re-serialization that changed one can be detected.
        /// </summary>
        /// <remarks>
        /// Sorted rather than compared in tree order: the rewrite moves filters to the end of their
        /// group, which is semantically irrelevant but would otherwise look like a difference.
        /// </remarks>
        private static List<string> FilterSignatures(DnrQuery.Patterns.GraphPattern pattern)
        {
            var signatures = new HashSet<string>(StringComparer.Ordinal);

            Collect(pattern, signatures);

            // Distinct rather than a multiset: dotNetRDF can expose one filter through both
            // GraphPattern.Filter and GraphPattern.UnplacedFilters, and a repeated filter is
            // idempotent anyway, so a count difference carries no meaning.
            var sorted = signatures.ToList();
            sorted.Sort(StringComparer.Ordinal);

            return sorted;
        }

        private static void Collect(DnrQuery.Patterns.GraphPattern pattern, HashSet<string> signatures)
        {
            if (pattern == null)
            {
                return;
            }


            foreach (ITriplePattern triplePattern in pattern.TriplePatterns)
            {
                switch (triplePattern)
                {
                    case IFilterPattern filter:
                        signatures.Add(SparqlExpressionWriter.Signature(filter.Filter.Expression));
                        break;
                    case IAssignmentPattern assignment:
                        signatures.Add(SparqlExpressionWriter.Signature(assignment.AssignExpression));
                        break;
                    case ISubQueryPattern subQuery:
                        Collect(subQuery.SubQuery.RootGraphPattern, signatures);
                        break;
                }
            }

            foreach (IAssignmentPattern assignment in pattern.UnplacedAssignments)
            {
                signatures.Add(SparqlExpressionWriter.Signature(assignment.AssignExpression));
            }

            foreach (VDS.RDF.Query.Filters.ISparqlFilter filter in Filters(pattern))
            {
                signatures.Add(SparqlExpressionWriter.Signature(filter.Expression));
            }

            foreach (DnrQuery.Patterns.GraphPattern child in pattern.ChildGraphPatterns)
            {
                Collect(child, signatures);
            }
        }

        /// <summary>
        /// Walks one query's pattern tree and emits the rewritten SPARQL.
        /// </summary>
        private sealed class Walker
        {
            private readonly ILayeredModel _model;

            private readonly StringBuilder _body = new StringBuilder();

            internal Walker(ILayeredModel model)
            {
                _model = model;
            }

            /// <summary>
            /// Rewrites a whole query. Only the outermost query may carry a dataset clause; a
            /// sub-<c>SELECT</c> must not.
            /// </summary>
            internal string RewriteQuery(DnrQuery.SparqlQuery query, bool outermost)
            {
                RequireSupportedForm(query);

                if (query.DefaultGraphNames.Any() || query.NamedGraphNames.Any())
                {
                    throw Unsupported(
                        "the query declares its own dataset clause (FROM / FROM NAMED). A layered view defines the " +
                        "dataset itself, so a query cannot also choose one");
                }

                // Serialize the pattern tree first; the head and solution modifiers are then taken
                // from dotNetRDF's own serialization of the same query, which is authoritative for
                // its own AST and saves re-implementing projection, GROUP BY, HAVING and ORDER BY.
                var walker = new Walker(_model);
                walker.WritePattern(query.RootGraphPattern);
                string body = walker._body.ToString();

                string normalized = query.ToString();
                SplitAroundRootPattern(normalized, out string head, out string tail);

                string dataset = outermost ? LayeredModelSparql.NamedDatasetClause(_model) : string.Empty;

                var result = new StringBuilder();

                result.Append(head);

                if (head.Length > 0 && !head.EndsWith(" ", StringComparison.Ordinal))
                {
                    result.Append(' ');
                }

                result.Append(dataset).Append("WHERE ").Append(body);

                if (tail.Length > 0)
                {
                    result.Append(' ').Append(tail);
                }

                return result.ToString().Trim();
            }

            private static void RequireSupportedForm(DnrQuery.SparqlQuery query)
            {
                switch (query.QueryType)
                {
                    case DnrQuery.SparqlQueryType.Ask:
                    case DnrQuery.SparqlQueryType.Select:
                    case DnrQuery.SparqlQueryType.SelectAll:
                    case DnrQuery.SparqlQueryType.SelectAllDistinct:
                    case DnrQuery.SparqlQueryType.SelectAllReduced:
                    case DnrQuery.SparqlQueryType.SelectDistinct:
                    case DnrQuery.SparqlQueryType.SelectReduced:
                        return;

                    case DnrQuery.SparqlQueryType.Construct:
                        throw Unsupported(
                            "CONSTRUCT: its template describes triples to build rather than to match, so the overlay " +
                            "must not be applied there, and distinguishing the two reliably is not supported");

                    case DnrQuery.SparqlQueryType.Describe:
                    case DnrQuery.SparqlQueryType.DescribeAll:
                        throw Unsupported(
                            "DESCRIBE: the store decides which triples to return, so there is no graph pattern to " +
                            "rewrite. Read whole resources with GetResource instead, which does honour the overlay");

                    default:
                        throw Unsupported($"query form {query.QueryType}");
                }
            }

            private void WritePattern(DnrQuery.Patterns.GraphPattern pattern)
            {
                if (pattern.IsGraph)
                {
                    throw Unsupported(
                        $"an explicit GRAPH block (GRAPH {pattern.GraphSpecifier?.Value}). A layered view is itself a " +
                        "graph-level construct built from three graphs, so naming one of them would read past the " +
                        "overlay - and GRAPH ?g would expose the removals graph as ordinary data");
                }

                if (pattern.IsService)
                {
                    throw Unsupported("a SERVICE clause: the remote endpoint knows nothing of the overlay");
                }

                if (pattern.HasInlineData && pattern.InlineData == null)
                {
                    throw Unsupported("an inline data block that could not be read");
                }

                // A union has to be recognised here, not only when it is reached as a child: an
                // alternative of a union can itself be a union, and emitting that as a group would
                // silently turn the inner disjunction into a join.
                if (pattern.IsUnion)
                {
                    WriteUnion(pattern);
                    return;
                }

                _body.Append("{ ");

                WriteTriplePatterns(pattern);
                WriteChildPatterns(pattern);
                WriteAssignments(pattern);
                WriteFilters(pattern);

                if (pattern.HasInlineData)
                {
                    // VALUES binds solutions directly and never touches a graph.
                    _body.Append(pattern.InlineData.ToString()).Append(' ');
                }

                _body.Append('}');
            }

            /// <summary>
            /// Emits a union as <c>{ alt UNION alt ... }</c>.
            /// </summary>
            /// <remarks>
            /// dotNetRDF models a union as a pattern whose own children are the alternatives, and an
            /// alternative may itself be a union - <c>{A} UNION {B} UNION {C}</c> parses left-nested.
            /// Each alternative therefore goes through <see cref="WritePattern"/>, which recognises a
            /// nested union rather than flattening it into a join.
            /// </remarks>
            private void WriteUnion(DnrQuery.Patterns.GraphPattern pattern)
            {
                if (pattern.TriplePatterns.Count > 0 || pattern.IsFiltered || pattern.HasInlineData)
                {
                    throw Unsupported(
                        "a UNION that also carries patterns of its own, which this rewriter does not model");
                }

                if (pattern.ChildGraphPatterns.Count == 0)
                {
                    throw Unsupported("a UNION with no alternatives");
                }

                _body.Append("{ ");

                for (int i = 0; i < pattern.ChildGraphPatterns.Count; i++)
                {
                    if (i > 0)
                    {
                        _body.Append("UNION ");
                    }

                    WritePattern(pattern.ChildGraphPatterns[i]);
                    _body.Append(' ');
                }

                _body.Append('}');
            }

            /// <summary>
            /// Emits a group's triple patterns, preserving the order of anything order-sensitive.
            /// </summary>
            /// <remarks>
            /// Match patterns are reordered so more-bound ones come first: order inside a basic graph
            /// pattern is semantically irrelevant, but the overlay turns each pattern into a UNION
            /// containing an anti-join, and an engine that cannot reorder joins across that evaluates
            /// them as written - so a leading unbound pattern makes it materialize the whole effective
            /// graph before applying any constraint.
            /// <para>
            /// That reordering is confined to contiguous <b>runs</b> of match patterns, because BIND is
            /// not order-insensitive: its variable must not already be in scope where it appears, so
            /// hoisting patterns across a BIND - or moving every BIND to the end - produces SPARQL that
            /// is either invalid or means something else.
            /// </para>
            /// </remarks>
            private void WriteTriplePatterns(DnrQuery.Patterns.GraphPattern pattern)
            {
                var run = new List<IMatchTriplePattern>();

                foreach (ITriplePattern triplePattern in pattern.TriplePatterns)
                {
                    if (triplePattern is IMatchTriplePattern match)
                    {
                        run.Add(match);

                        continue;
                    }

                    FlushRun(run);
                    WriteNonMatchPattern(triplePattern);
                }

                FlushRun(run);
            }

            /// <summary>
            /// Writes the accumulated run of match patterns, most-bound first, and clears it.
            /// </summary>
            private void FlushRun(List<IMatchTriplePattern> run)
            {
                foreach (IMatchTriplePattern match in run.OrderByDescending(BoundTermCount))
                {
                    _body.Append(LayeredModelSparql.Overlay(
                        _model, Term(match.Subject), Term(match.Predicate), Term(match.Object)));

                    _body.Append(' ');
                }

                run.Clear();
            }

            private void WriteNonMatchPattern(ITriplePattern triplePattern)
            {
                switch (triplePattern)
                {
                    case ISubQueryPattern subQuery:
                        _body.Append("{ ")
                             .Append(new Walker(_model).RewriteQuery(subQuery.SubQuery, outermost: false))
                             .Append(" } ");
                        break;

                    case IAssignmentPattern assignment:
                        // BIND / LET: computes a value, never reads a graph. Written through our own
                        // serializer because dotNetRDF mangles a negation here too.
                        RequireNoGraphAccess(assignment.AssignExpression);
                        _body.Append("BIND(")
                             .Append(SparqlExpressionWriter.Write(assignment.AssignExpression))
                             .Append(" AS ?").Append(assignment.VariableName).Append(") ");
                        break;

                    case BindingsPattern inlineData:
                        _body.Append(inlineData.ToString()).Append(' ');
                        break;

                    case IFilterPattern filter:
                        RequireNoGraphAccess(filter.Filter.Expression);
                        _body.Append("FILTER(")
                             .Append(SparqlExpressionWriter.Write(filter.Filter.Expression))
                             .Append(") ");
                        break;

                    case IPropertyPathPattern path:
                        throw UnsupportedPath(path);

                    case IPropertyFunctionPattern _:
                        throw Unsupported("a property function");

                    default:
                        throw Unsupported($"a pattern of type {triplePattern.GetType().Name}");
                }
            }

            private void WriteChildPatterns(DnrQuery.Patterns.GraphPattern pattern)
            {
                foreach (DnrQuery.Patterns.GraphPattern child in pattern.ChildGraphPatterns)
                {
                    if (child.IsExists || child.IsNotExists)
                    {
                        throw Unsupported("an EXISTS / NOT EXISTS graph pattern");
                    }

                    if (child.IsOptional)
                    {
                        _body.Append("OPTIONAL ");
                    }
                    else if (child.IsMinus)
                    {
                        _body.Append("MINUS ");
                    }

                    WritePattern(child);
                    _body.Append(' ');
                }
            }

            /// <summary>
            /// Emits the BIND clauses dotNetRDF parked as "unplaced".
            /// </summary>
            /// <remarks>
            /// A parser may hold an assignment in <c>UnplacedAssignments</c> rather than among the
            /// group's triple patterns, to be positioned later during optimisation. Iterating only
            /// <c>TriplePatterns</c> would drop it from the rewritten query - losing a binding
            /// silently, which is worse than refusing.
            /// </remarks>
            private void WriteAssignments(DnrQuery.Patterns.GraphPattern pattern)
            {
                foreach (IAssignmentPattern assignment in pattern.UnplacedAssignments)
                {
                    RequireNoGraphAccess(assignment.AssignExpression);

                    _body.Append("BIND(")
                         .Append(SparqlExpressionWriter.Write(assignment.AssignExpression))
                         .Append(" AS ?").Append(assignment.VariableName).Append(") ");
                }
            }

            /// <summary>
            /// Emits every filter that applies to a group, each exactly once.
            /// </summary>
            /// <remarks>
            /// A filter can be reachable through both <c>Filter</c> and <c>UnplacedFilters</c>, so the
            /// two are merged and de-duplicated by structure. Emitting one twice would be harmless -
            /// a filter is idempotent - but omitting one would not, and de-duplicating here keeps the
            /// round-trip comparison honest.
            /// </remarks>
            private void WriteFilters(DnrQuery.Patterns.GraphPattern pattern)
            {
                var written = new HashSet<string>();

                foreach (VDS.RDF.Query.Filters.ISparqlFilter filter in Filters(pattern))
                {
                    RequireNoGraphAccess(filter.Expression);

                    string text = SparqlExpressionWriter.Write(filter.Expression);

                    if (written.Add(SparqlExpressionWriter.Signature(filter.Expression)))
                    {
                        _body.Append("FILTER(").Append(text).Append(") ");
                    }
                }
            }

            /// <summary>
            /// Refuses a filter expression that reads a graph of its own.
            /// </summary>
            /// <remarks>
            /// <c>FILTER EXISTS { ... }</c> and <c>FILTER NOT EXISTS { ... }</c> do not appear as child
            /// graph patterns; they are an <c>ExistsFunction</c> wrapping a <c>GraphPatternTerm</c>
            /// inside the filter expression. Filters are re-emitted verbatim, so without this check
            /// that nested pattern would bypass the overlay entirely and match against the raw
            /// baseline — the removals graph would be ignored and nothing would say so.
            /// </remarks>
            private static void RequireNoGraphAccess(ISparqlExpression expression)
            {
                if (expression == null)
                {
                    return;
                }

                if (expression is ExistsFunction || expression is GraphPatternTerm ||
                    expression.Type == SparqlExpressionType.GraphOperator)
                {
                    throw Unsupported(
                        "a filter that matches a graph pattern of its own (EXISTS / NOT EXISTS). Filters are " +
                        "re-emitted unchanged, so its nested pattern would read the baseline directly and ignore " +
                        "the triples staged for removal");
                }

                foreach (ISparqlExpression argument in expression.Arguments)
                {
                    RequireNoGraphAccess(argument);
                }
            }

            private static int BoundTermCount(IMatchTriplePattern match)
            {
                int bound = 0;

                if (!(match.Subject is VariablePattern)) bound++;
                if (!(match.Predicate is VariablePattern)) bound++;
                if (!(match.Object is VariablePattern)) bound++;

                return bound;
            }

            private static string Term(PatternItem item)
            {
                switch (item)
                {
                    case VariablePattern _:
                    case NodeMatchPattern _:
                        return item.ToString();

                    case BlankNodePattern _:
                    case FixedBlankNodePattern _:
                        // The overlay repeats each triple pattern across three basic graph patterns,
                        // and SPARQL forbids a blank-node label appearing in more than one BGP of a
                        // query - so the rewrite would not be legal SPARQL. This also covers the
                        // property-list syntax [ ... ], which introduces a blank node.
                        throw Unsupported(
                            "a blank node in a triple pattern, including the [ ... ] property-list form. " +
                            "The overlay repeats every pattern across three basic graph patterns, and SPARQL " +
                            "does not allow a blank-node label to appear in more than one of them");

                    default:
                        throw Unsupported($"a term of type {item.GetType().Name}");
                }
            }

            /// <summary>
            /// Refuses a property path, distinguishing the forms that are impossible from the forms
            /// that are merely not implemented yet.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The <b>unbounded</b> forms - <c>p+</c>, <c>p*</c>, <c>p{n,}</c> - cannot be supported at
            /// all, and the reason is sharper than "paths are hard": <b>transitive closure does not
            /// distribute over the union of the layers</b>. With <c>a p b</c> in the baseline and
            /// <c>b p c</c> in the additions, the effective graph contains the chain, so
            /// <c>a p+ c</c> must hold - but evaluating the closure inside each graph and unioning the
            /// results yields only <c>b</c>, because neither graph contains the whole chain. So the
            /// path cannot be pushed inside the overlay's GRAPH blocks, and there is no finite
            /// expansion into triple patterns to push the overlay into instead. The only way to
            /// evaluate one correctly would be to materialize the effective graph first, which is a
            /// write, and O(baseline) per query.
            /// </para>
            /// <para>
            /// The <b>bounded</b> forms are a different matter and are simply not implemented:
            /// <c>^p</c> is a subject/object swap, <c>p1/p2</c> expands into two patterns joined by a
            /// fresh variable, <c>p1|p2</c> into a UNION, and <c>!p</c> into a variable predicate with
            /// a filter - each of which the overlay then handles one pattern at a time. They are
            /// refused only because the expansion has not been written, not because it cannot be.
            /// </para>
            /// </remarks>
            private static NotSupportedException UnsupportedPath(IPropertyPathPattern path)
            {
                string kind = path.Path == null ? "a property path" : PathKind(path.Path);

                return Unsupported(kind);
            }

            private static string PathKind(VDS.RDF.Query.Paths.ISparqlPath path)
            {
                switch (path)
                {
                    case VDS.RDF.Query.Paths.OneOrMore _:
                    case VDS.RDF.Query.Paths.ZeroOrMore _:
                    case VDS.RDF.Query.Paths.NOrMore _:
                        return "an unbounded property path (+, * or {n,}). This one cannot be supported: " +
                               "transitive closure does not distribute over the union of the three layers - a chain " +
                               "whose hops come from different layers exists in the effective graph but in none of " +
                               "them alone - and there is no finite expansion into triple patterns to apply the " +
                               "overlay to instead";

                    default:
                        return "a property path. The unbounded forms (+, *) cannot be supported at all, because " +
                               "transitive closure does not distribute over the union of the layers; the bounded " +
                               "forms (^, /, |, !) could be expanded into ordinary triple patterns but that is not " +
                               "implemented yet";
                }
            }

            private static NotSupportedException Unsupported(string what)
            {
                return new NotSupportedException(
                    "This query cannot be run against a layered model because it contains " + what + ". " +
                    "The baseline/additions/removals overlay has to be applied inside every graph pattern, and this " +
                    "form cannot be rewritten faithfully - so it is refused rather than answered with triples that " +
                    "are staged for removal. Query the Baseline, Additions or Removals models directly, or use " +
                    "GetResource / GetResources / ContainsResource / AsQueryable<T>(). " +
                    "See doc/adr/0041-layered-read-views.md.");
            }

            /// <summary>
            /// Splits dotNetRDF's serialization of a query around its root group graph pattern, so the
            /// projection and the solution modifiers can be reused verbatim.
            /// </summary>
            /// <remarks>
            /// Brace matching is literal-aware: a query may legitimately contain <c>"}"</c> inside a
            /// string, in the projection as well as in the pattern.
            /// </remarks>
            private static void SplitAroundRootPattern(string query, out string head, out string tail)
            {
                int open = IndexOfRootBrace(query);

                if (open < 0)
                {
                    throw Unsupported("no group graph pattern that could be rewritten");
                }

                int close = IndexOfMatchingBrace(query, open);

                if (close < 0)
                {
                    throw Unsupported("an unbalanced group graph pattern");
                }

                head = query.Substring(0, open).TrimEnd();

                // Drop the WHERE keyword; it is re-emitted with the rewritten body.
                if (head.EndsWith("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    head = head.Substring(0, head.Length - "WHERE".Length).TrimEnd();
                }

                tail = query.Substring(close + 1).Trim();
            }

            private static int IndexOfRootBrace(string query)
            {
                for (int i = 0; i < query.Length; i++)
                {
                    i = SkipLiteralOrIri(query, i);

                    if (i < query.Length && query[i] == '{')
                    {
                        return i;
                    }
                }

                return -1;
            }

            private static int IndexOfMatchingBrace(string query, int open)
            {
                int depth = 0;

                for (int i = open; i < query.Length; i++)
                {
                    i = SkipLiteralOrIri(query, i);

                    if (i >= query.Length)
                    {
                        break;
                    }

                    if (query[i] == '{')
                    {
                        depth++;
                    }
                    else if (query[i] == '}')
                    {
                        depth--;

                        if (depth == 0)
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }

            /// <summary>
            /// Indicates whether an IRI reference starts at <paramref name="start"/>, and if so where
            /// its closing angle bracket is.
            /// </summary>
            private static bool IsIriRef(string query, int start, out int end)
            {
                end = start;

                for (int i = start + 1; i < query.Length; i++)
                {
                    char c = query[i];

                    if (c == '>')
                    {
                        end = i;
                        return true;
                    }

                    if (char.IsWhiteSpace(c) || c == '<' || c == '"' || c == '{' ||
                        c == '}' || c == '|' || c == '^' || c == '`')
                    {
                        return false;
                    }
                }

                return false;
            }

            /// <summary>
            /// If a literal or IRI starts at <paramref name="i"/>, returns the index of its last
            /// character; otherwise returns <paramref name="i"/> unchanged.
            /// </summary>
            private static int SkipLiteralOrIri(string query, int i)
            {
                if (i >= query.Length)
                {
                    return i;
                }

                char c = query[i];

                if (c == '<')
                {
                    // '<' is also the less-than operator, so only skip when this really is an IRI.
                    // Per the SPARQL grammar an IRIREF runs to the next '>' and may not contain
                    // whitespace or any of <>"{}|^`. Treating `?r < 3` as an IRI would swallow the
                    // rest of the query, closing braces included.
                    return IsIriRef(query, i, out int end) ? end : i;
                }

                if (c != '"' && c != '\'')
                {
                    return i;
                }

                string longQuote = new string(c, 3);
                bool isLong = i + 2 < query.Length && query.Substring(i, 3) == longQuote;
                int start = isLong ? i + 3 : i + 1;

                for (int j = start; j < query.Length; j++)
                {
                    if (query[j] == '\\')
                    {
                        j++;
                        continue;
                    }

                    if (isLong)
                    {
                        if (j + 2 < query.Length && query.Substring(j, 3) == longQuote)
                        {
                            return j + 2;
                        }
                    }
                    else if (query[j] == c)
                    {
                        return j;
                    }
                }

                return query.Length;
            }
        }
    }
}
