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
// Copyright (c) Semiodesk GmbH 2015-2019

using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Inference;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Functions.Sparql.Boolean;
using VDS.RDF.Query.Patterns;

// Semiodesk.Trinity has its own SparqlQuery; this file means dotNetRDF's parsed one throughout.
using ParsedQuery = VDS.RDF.Query.SparqlQuery;

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// Computes RDFS entailments for the in-memory store and hands back a dataset that contains them,
    /// so that <c>inferenceEnabled</c> can be honoured per query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The obvious approach — dotNetRDF's <c>AddInferenceEngine</c> — writes entailments back into the
    /// graph they came from, which makes them visible to <i>every</i> query. A store doing that cannot
    /// answer <c>inferenceEnabled: false</c> correctly, which is why Fuseki cannot honour the flag at
    /// all (ADR-0043). Entailments therefore live in a graph of their own.
    /// </para>
    /// <para>
    /// <b>That graph is never added to the store.</b> It is built per query, into a throwaway dataset
    /// that merely references the store's existing graphs, and discarded when the query returns. An
    /// earlier version cached entailment graphs inside the store, which cost three defects and bought
    /// only speed: the graphs showed up in any query enumerating <c>GRAPH ?g</c>, a failure midway
    /// through materialization latched permanently, and a read mutated shared state under concurrent
    /// queries. Recomputing is the cheaper trade for an in-memory store used in tests and development.
    /// </para>
    /// </remarks>
    internal sealed class RdfsEntailment
    {
        private readonly IInMemoryQueryableStore _store;

        internal RdfsEntailment(IInMemoryQueryableStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Builds a dataset in which the given query's default graphs also contain their RDFS
        /// entailments, and widens the query to select them.
        /// </summary>
        /// <remarks>
        /// Returns <c>null</c> when the query names no default graph. Inference then does nothing
        /// rather than something arbitrary: adding a graph would <i>narrow</i> a query that currently
        /// reads the whole store, which is the opposite of what enabling inference should do.
        /// </remarks>
        /// <param name="query">The parsed query; its dataset is widened in place.</param>
        /// <returns>A dataset to run the query against, or <c>null</c> to run it unchanged.</returns>
        internal IInMemoryQueryableStore Apply(ParsedQuery query)
        {
            RequireNoNamedGraphAccess(query);

            // DefaultGraphNames, not Trinity's own record of the FROM operands: these are resolved
            // against the query's BASE, so `BASE <http://ex.org/> ... FROM <m1>` yields an absolute
            // IRI. Taking the raw token text turned a working query into a UriFormatException the
            // moment inference was switched on.
            var targets = query.DefaultGraphNames.OfType<IUriNode>().Select(n => n.Uri).ToList();

            if (targets.Count == 0)
            {
                return null;
            }

            var reasoner = CreateReasoner();
            var dataset = new TripleStore();

            // By reference: the store's graphs are shared, not copied, and nothing here writes to them.
            foreach (var graph in _store.Graphs)
            {
                dataset.Add(graph, true);
            }

            foreach (var target in targets)
            {
                var name = new UriNode(target);

                if (!_store.HasGraph(name))
                {
                    continue;
                }

                var entailed = new Graph(new UriNode(EntailmentGraphUri(target)));

                // Apply(input, output) writes the entailed triples somewhere other than the input --
                // the whole reason a per-query graph is possible.
                reasoner.Apply(_store[name], entailed);

                if (entailed.Triples.Count == 0)
                {
                    continue;
                }

                dataset.Add(entailed, true);
                query.AddDefaultGraph(entailed.Name);
            }

            return dataset;
        }

        /// <summary>
        /// Refuses a query that reaches a graph by name while inference is requested.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Entailments are added to the query's <i>default</i> graph. A pattern inside
        /// <c>GRAPH &lt;g&gt;</c> reads <c>g</c> itself, which holds only asserted triples, so such a
        /// query would come back non-inferred while reporting success — the silent wrong answer this
        /// whole design exists to remove. Refusing is the same choice a layered view makes for a query
        /// it cannot rewrite faithfully (ADR-0041).
        /// </para>
        /// <para>
        /// <b>Whitelist, not blacklist</b> — the discipline <c>OverlayQueryRewriter</c> arrived at for
        /// the same reason. The first version of this walker recursed only through child graph
        /// patterns, which reaches <c>OPTIONAL</c>, <c>MINUS</c> and <c>UNION</c> but not a subquery
        /// (whose pattern hangs off a <c>SubQueryPattern</c> in <c>TriplePatterns</c>) nor a
        /// <c>FILTER EXISTS</c> (whose pattern hangs off the filter expression). A <c>GRAPH</c> in
        /// either position was accepted and answered non-inferred. Enumerating the places a
        /// <c>GRAPH</c> can hide is a game that loses to the next SPARQL feature, so anything the
        /// walker does not positively recognise is refused instead.
        /// </para>
        /// </remarks>
        private static void RequireNoNamedGraphAccess(ParsedQuery query)
        {
            if (query.NamedGraphNames.Any())
            {
                Refuse("the query declares FROM NAMED");
            }

            RequireNoNamedGraphAccess(query.RootGraphPattern);
        }

        private static void RequireNoNamedGraphAccess(GraphPattern pattern)
        {
            if (pattern == null)
            {
                return;
            }

            if (pattern.IsGraph)
            {
                Refuse("the query reads a named graph with GRAPH");
            }

            if (pattern.IsService)
            {
                Refuse("the query delegates to a remote endpoint with SERVICE");
            }

            foreach (var child in pattern.ChildGraphPatterns)
            {
                RequireNoNamedGraphAccess(child);
            }

            foreach (var triplePattern in pattern.TriplePatterns)
            {
                RequireNoNamedGraphAccess(triplePattern);
            }

            RequireNoNamedGraphAccess(pattern.Filter?.Expression);

            foreach (var filter in pattern.UnplacedFilters)
            {
                RequireNoNamedGraphAccess(filter.Expression);
            }
        }

        /// <summary>
        /// Descends the pattern kinds that can contain another pattern, and refuses any kind this
        /// walker has not been taught about.
        /// </summary>
        private static void RequireNoNamedGraphAccess(ITriplePattern triplePattern)
        {
            switch (triplePattern)
            {
                case ISubQueryPattern subQuery:
                    // A subquery carries a whole query, dataset clause included.
                    RequireNoNamedGraphAccess(subQuery.SubQuery);
                    break;

                case IFilterPattern filter:
                    RequireNoNamedGraphAccess(filter.Filter?.Expression);
                    break;

                case GraphPattern nested:
                    RequireNoNamedGraphAccess(nested);
                    break;

                // Kinds that match, bind or supply values, and cannot contain a graph pattern.
                case IMatchTriplePattern _:
                case IPropertyPathPattern _:
                case IPropertyFunctionPattern _:
                case IAssignmentPattern _:
                case BindingsPattern _:
                    break;

                default:
                    Refuse(
                        $"the query uses a pattern this store cannot check for named-graph access "
                        + $"({triplePattern.GetType().Name})");
                    break;
            }
        }

        /// <summary>
        /// Walks a filter expression for a pattern hiding inside it.
        /// </summary>
        /// <remarks>
        /// <c>EXISTS</c> / <c>NOT EXISTS</c> is the only SPARQL 1.1 expression form that carries a
        /// group graph pattern; everything else is reached by recursing through
        /// <see cref="ISparqlExpression.Arguments"/>, which covers it however deeply it is nested.
        /// </remarks>
        private static void RequireNoNamedGraphAccess(ISparqlExpression expression)
        {
            if (expression == null)
            {
                return;
            }

            if (expression is ExistsFunction exists)
            {
                RequireNoNamedGraphAccess(exists.Pattern);
            }

            foreach (var argument in expression.Arguments)
            {
                RequireNoNamedGraphAccess(argument);
            }
        }

        private static void Refuse(string what)
        {
            throw new NotSupportedException(
                $"Inferencing is not supported here: {what}. Entailments are added to the query's "
                + "default graph, so a pattern reading a named graph would silently return "
                + "non-inferred results. Query the model through its default graph instead, or run "
                + "without inferenceEnabled.");
        }

        /// <summary>
        /// Builds a reasoner over the schema currently in the store.
        /// </summary>
        /// <remarks>
        /// The schema is taken from the graphs actually present rather than only from the files named
        /// by the connection string's <c>schema=</c> key: callers seed their vocabularies with
        /// <c>store.Read</c> / <c>LoadGraphs</c> (ADR-0011), and a reasoner that could not see those
        /// would have no axioms to work from. <see cref="StaticRdfsReasoner"/> rather than
        /// <c>RdfsReasoner</c>, so applying data cannot silently redefine the ontology.
        /// </remarks>
        private StaticRdfsReasoner CreateReasoner()
        {
            var reasoner = new StaticRdfsReasoner();

            foreach (var graph in _store.Graphs)
            {
                reasoner.Initialise(graph);
            }

            return reasoner;
        }

        private static Uri EntailmentGraphUri(Uri modelGraph)
        {
            return new Uri("urn:semiodesk:trinity:entailed:" + Uri.EscapeDataString(modelGraph.OriginalString));
        }
    }
}
