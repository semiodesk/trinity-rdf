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
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections.Generic;
using System.Linq;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// The backends a benchmark runs against.
    /// </summary>
    /// <remarks>
    /// <see cref="InMemory"/> is not a peer of the others and is included deliberately: it removes
    /// the network and the server, so its column is the floor. Whatever it costs is Trinity, and the
    /// gap between it and a real backend is what the backend costs.
    /// </remarks>
    public enum StoreBackend
    {
        InMemory,
        Oxigraph,
        Fuseki,
        GraphDB,
        Virtuoso
    }

    /// <summary>
    /// Which backends this run covers, from the <c>TRINITY_BENCH_BACKENDS</c> environment variable.
    /// </summary>
    /// <remarks>
    /// A comma-separated list of <see cref="StoreBackend"/> names, case-insensitive, e.g.
    /// <c>TRINITY_BENCH_BACKENDS=InMemory,Oxigraph</c>. Unset or empty means all of them. An environment
    /// variable rather than a command-line switch because the benchmarks run in-process, so it reaches
    /// the <c>ParamsSource</c> without BenchmarkDotNet having to know about it, and because the
    /// <c>profile</c> mode and a normal run then read the same setting.
    ///
    /// An unknown name throws rather than being skipped: a typo that silently ran the whole matrix
    /// would start four containers nobody asked for.
    /// </remarks>
    public static class BenchmarkBackends
    {
        /// <summary>
        /// The environment variable read by <see cref="Selected"/>.
        /// </summary>
        public const string Variable = "TRINITY_BENCH_BACKENDS";

        /// <summary>
        /// The backends selected for this run, in declaration order.
        /// </summary>
        public static IReadOnlyList<StoreBackend> Selected => Parse(Environment.GetEnvironmentVariable(Variable));

        /// <summary>
        /// Parses a backend list in the format <see cref="Variable"/> takes.
        /// </summary>
        public static IReadOnlyList<StoreBackend> Parse(string value)
        {
            var all = (StoreBackend[])Enum.GetValues(typeof(StoreBackend));

            if (string.IsNullOrWhiteSpace(value))
            {
                return all;
            }

            var selected = new HashSet<StoreBackend>();

            foreach (var name in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Enum.TryParse(name.Trim(), ignoreCase: true, out StoreBackend backend)
                    || !Enum.IsDefined(typeof(StoreBackend), backend))
                {
                    throw new ArgumentException(
                        $"{Variable} names an unknown backend '{name.Trim()}'. Known: "
                        + string.Join(", ", all) + ".");
                }

                selected.Add(backend);
            }

            return all.Where(selected.Contains).ToList();
        }
    }
}
