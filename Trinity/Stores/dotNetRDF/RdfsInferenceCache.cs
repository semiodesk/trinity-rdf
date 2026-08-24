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
using VDS.RDF.Query.Inference;

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// Materializes RDFS entailments for the in-memory store into a <b>separate</b> graph per model,
    /// so that <c>inferenceEnabled</c> can be honoured per query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The obvious approach — dotNetRDF's <c>AddInferenceEngine</c> — writes entailments back into the
    /// graph they came from, which makes them visible to <i>every</i> query. A store doing that cannot
    /// answer <c>inferenceEnabled: false</c> correctly, which is exactly why Fuseki cannot honour the
    /// flag at all (ADR-0043). Keeping entailments in a side graph is what makes the flag per-query:
    /// a query that asks for inference gets the side graph added to its dataset, and one that does not
    /// is left untouched.
    /// </para>
    /// <para>
    /// <b>Invalidation is deliberately coarse.</b> Any write drops every materialized graph. An
    /// arbitrary SPARQL UPDATE does not say which graph it touched, and guessing wrong would serve
    /// stale entailments — a silent wrong answer, which is the thing this class exists to avoid. The
    /// store is in-memory and used for tests and development, so recomputing is cheap; making the
    /// invalidation finer is an optimisation, not a correctness fix.
    /// </para>
    /// </remarks>
    internal sealed class RdfsInferenceCache
    {
        /// <summary>
        /// Prefix of the graphs this class owns. Chosen so it cannot collide with a caller's model URI,
        /// and recognisable so those graphs can be hidden from <c>ListModels</c>.
        /// </summary>
        private const string InferenceGraphPrefix = "urn:semiodesk:trinity:inferred:";

        private readonly ITripleStore _store;

        private readonly HashSet<string> _materialized = new HashSet<string>();

        private StaticRdfsReasoner _reasoner;

        private bool _isStale = true;

        internal RdfsInferenceCache(ITripleStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Indicates whether a graph is one this class materialized rather than one a caller created.
        /// </summary>
        internal static bool IsInferenceGraph(IRefNode name)
        {
            return name is IUriNode uriNode && IsInferenceGraph(uriNode.Uri);
        }

        /// <summary>
        /// Indicates whether a URI addresses a materialized graph rather than a caller's model.
        /// </summary>
        internal static bool IsInferenceGraph(Uri uri)
        {
            return uri != null && uri.OriginalString.StartsWith(InferenceGraphPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Marks every materialized graph out of date. Called from each write path.
        /// </summary>
        internal void Invalidate()
        {
            _isStale = true;
        }

        /// <summary>
        /// Ensures the entailments of <paramref name="modelGraph"/> are materialized, and returns the
        /// name of the graph holding them.
        /// </summary>
        /// <param name="modelGraph">URI of the model whose entailments are wanted.</param>
        /// <returns>
        /// The graph name to add to a query's dataset, or <c>null</c> when there is nothing to add —
        /// an unknown model, or one that entails nothing.
        /// </returns>
        internal IRefNode EnsureCurrent(Uri modelGraph)
        {
            if (modelGraph == null || IsInferenceGraph(modelGraph))
            {
                return null;
            }

            RefreshIfStale();

            var source = new UriNode(modelGraph);

            if (!_store.HasGraph(source))
            {
                return null;
            }

            var target = new UriNode(InferenceGraphUri(modelGraph));

            if (_materialized.Add(modelGraph.OriginalString))
            {
                var inferred = new Graph(target);

                // Apply(input, output) writes the entailed triples somewhere other than the input --
                // the whole reason this design is possible.
                _reasoner.Apply(_store[source], inferred);

                if (_store.HasGraph(target))
                {
                    _store.Remove(target);
                }

                _store.Add(inferred);
            }

            return _store.HasGraph(target) ? target : null;
        }

        /// <summary>
        /// Discards every materialized graph and rebuilds the reasoner's schema from the store.
        /// </summary>
        /// <remarks>
        /// The schema is taken from the graphs actually present rather than only from the files named
        /// by the connection string's <c>schema=</c> key: callers seed their vocabularies with
        /// <c>store.Read</c> / <c>LoadGraphs</c> (ADR-0011), and a reasoner that could not see those
        /// would have no axioms to work from. <see cref="StaticRdfsReasoner"/> rather than
        /// <c>RdfsReasoner</c> so the schema is fixed at this point and data cannot silently redefine
        /// the ontology as it is applied.
        /// </remarks>
        private void RefreshIfStale()
        {
            if (!_isStale)
            {
                return;
            }

            // ToList first: removing while enumerating _store.Graphs would invalidate the enumerator.
            foreach (var name in _store.Graphs.Select(g => g.Name).Where(IsInferenceGraph).ToList())
            {
                _store.Remove(name);
            }

            _materialized.Clear();

            _reasoner = new StaticRdfsReasoner();

            foreach (var graph in _store.Graphs.Where(g => !IsInferenceGraph(g.Name)).ToList())
            {
                _reasoner.Initialise(graph);
            }

            _isStale = false;
        }

        private static Uri InferenceGraphUri(Uri modelGraph)
        {
            return new Uri(InferenceGraphPrefix + Uri.EscapeDataString(modelGraph.OriginalString));
        }
    }
}
