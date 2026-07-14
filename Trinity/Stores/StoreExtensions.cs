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
