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
using System.IO;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Convenience extension methods for <see cref="IStore"/>.
    /// </summary>
    public static class StoreExtensions
    {
        /// <summary>
        /// Creates a read-only view over three named graphs with working-copy semantics: reads see
        /// <c>(baseline - removals) union additions</c> while the baseline graph itself stays
        /// untouched.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is an extension method rather than a member of <see cref="IStore"/> deliberately. A
        /// layered model needs nothing store-specific beyond <c>ExecuteQuery</c> and
        /// <c>GetModel</c>, so putting it here avoids a breaking change to the interface every
        /// custom backend implements - and avoids reproducing the empty-group bug that the
        /// per-store <c>CreateModelGroup(params IModel[])</c> overrides still carry.
        /// </para>
        /// <para>
        /// The three graphs need not exist yet; an absent graph simply contributes nothing, so a
        /// view with empty additions and removals reads exactly like the baseline.
        /// </para>
        /// </remarks>
        /// <param name="store">The store holding all three graphs.</param>
        /// <param name="baseline">URI of the unchanged graph read through the view.</param>
        /// <param name="additions">URI of the graph holding triples staged for addition.</param>
        /// <param name="removals">URI of the graph holding triples staged for removal.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown if the store cannot honour the overlay - see <see cref="CanHostLayeredModel"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown if any two URIs name the same graph.</exception>
        public static ILayeredModel CreateLayeredModel(this IStore store, Uri baseline, Uri additions, Uri removals)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            if (additions == null) throw new ArgumentNullException(nameof(additions));
            if (removals == null) throw new ArgumentNullException(nameof(removals));

            if (!CanHostLayeredModel(store))
            {
                throw new NotSupportedException(
                    store.GetType().Name + " cannot host a layered model: it discards the dataset clause of " +
                    "every query it runs, so the overlay's graph scoping would be lost and reads would " +
                    "silently include triples staged for removal.");
            }

            return new LayeredModel(store, store.GetModel(baseline), store.GetModel(additions), store.GetModel(removals));
        }

        /// <summary>
        /// Creates a read-only layered view over three existing models of the same store.
        /// </summary>
        /// <inheritdoc cref="CreateLayeredModel(IStore, Uri, Uri, Uri)" path="/remarks" />
        public static ILayeredModel CreateLayeredModel(this IStore store, IModel baseline, IModel additions, IModel removals)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            if (additions == null) throw new ArgumentNullException(nameof(additions));
            if (removals == null) throw new ArgumentNullException(nameof(removals));

            RequireSameStore(store, baseline, nameof(baseline));
            RequireSameStore(store, additions, nameof(additions));
            RequireSameStore(store, removals, nameof(removals));

            return store.CreateLayeredModel(baseline.Uri, additions.Uri, removals.Uri);
        }

        /// <summary>
        /// Rejects a model that belongs to a different store.
        /// </summary>
        /// <remarks>
        /// The overlay is a single SPARQL query over a single dataset, so a graph in another store is
        /// simply not in scope: <c>GRAPH &lt;g&gt;</c> would match nothing there. Without this check the
        /// view answers from whichever graphs the executing store happens to have and silently ignores
        /// the rest - returning triples staged for removal, or dropping the baseline entirely,
        /// depending on which store executes. That is the one failure this design must not have, so it
        /// is refused at construction.
        /// </remarks>
        private static void RequireSameStore(IStore store, IModel model, string parameterName)
        {
            // Every IStore.GetModel implementation returns the concrete Model, so this covers every
            // model a caller can actually obtain.
            if (model is Model concrete && !ReferenceEquals(concrete.Store, store))
            {
                throw new NotSupportedException(
                    "The " + parameterName + " model <" + model.Uri + "> belongs to a different store. A layered " +
                    "model is evaluated as one SPARQL query over one dataset, so all three graphs must live in the " +
                    "same store. To overlay a change held elsewhere - for example an in-memory pending change over a " +
                    "Virtuoso baseline - copy the additions and removals graphs into the store that holds the " +
                    "baseline, or merge the layers client-side. See doc/adr/0041-layered-read-views.md.");
            }
        }

        /// <summary>
        /// Indicates whether a store can answer layered reads correctly.
        /// </summary>
        /// <remarks>
        /// Only <c>SparqlEndpointStore</c> cannot: it parses every query and calls
        /// <c>ClearDefaultGraphs()</c>/<c>ClearNamedGraphs()</c> before sending it, which strips the
        /// <c>FROM NAMED</c> clause the overlay's <c>GRAPH</c> blocks rely on. It also has no
        /// <c>ExecuteNonQuery</c>, so a change could not be staged there in the first place. Rather
        /// than add a capability model (ADR-0022 declines one), this is a single negative check.
        /// </remarks>
        internal static bool CanHostLayeredModel(IStore store)
        {
            return !(store is Store.SparqlEndpointStore);
        }

        /// <summary>
        /// Reads one or more RDF files into named graphs of the store, inferring the serialization
        /// format from each file's extension (<c>.n3</c>, <c>.trig</c>, <c>.ttl</c>, <c>.nt</c>,
        /// otherwise RDF/XML).
        /// <para>
        /// This is the recommended way to seed a store with schema / background graphs (ontologies).
        /// It replaces the retired declarative <c>ontologies.config</c> auto-loading: call it once at
        /// startup instead of <c>InitializeFromConfiguration()</c>. Register ontology prefixes for
        /// SPARQL/mapping separately via <see cref="OntologyDiscovery"/>.
        /// </para>
        /// </summary>
        /// <param name="store">The store the graphs are loaded into.</param>
        /// <param name="graphs">Pairs of (graph URI, RDF file path) to read.</param>
        /// <param name="update">
        /// If <c>true</c>, the file is merged into an existing graph of the same URI; otherwise the
        /// graph is replaced. Defaults to <c>false</c>.
        /// </param>
        /// <param name="baseDirectory">
        /// Base directory used to resolve relative file paths. Defaults to
        /// <see cref="AppContext.BaseDirectory"/> (the application's base directory).
        /// </param>
        public static void LoadGraphs(this IStore store, IEnumerable<(Uri Graph, string Path)> graphs, bool update = false, string baseDirectory = null)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (graphs == null) throw new ArgumentNullException(nameof(graphs));

            string root = string.IsNullOrEmpty(baseDirectory) ? AppContext.BaseDirectory : baseDirectory;

            foreach (var (graph, path) in graphs)
            {
                if (graph == null)
                {
                    throw new ArgumentException("The graph URI must not be null.", nameof(graphs));
                }

                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentException($"The file path for graph <{graph}> must not be null or empty.", nameof(graphs));
                }

                string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(root, path);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"The RDF file for graph <{graph}> was not found.", fullPath);
                }

                store.Read(graph, new Uri(fullPath), GetSerializationFormat(fullPath), update);
            }
        }

        /// <summary>
        /// Infers the <see cref="RdfSerializationFormat"/> from a file extension.
        /// </summary>
        private static RdfSerializationFormat GetSerializationFormat(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".n3": return RdfSerializationFormat.N3;
                case ".trig": return RdfSerializationFormat.Trig;
                case ".ttl": return RdfSerializationFormat.Turtle;
                case ".nt": return RdfSerializationFormat.NTriples;
                default: return RdfSerializationFormat.RdfXml;
            }
        }
    }
}
