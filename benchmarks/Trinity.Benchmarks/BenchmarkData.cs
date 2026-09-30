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
using System.Text;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Bulk fixture loading, for workloads whose setup is not what they measure.
    /// </summary>
    /// <remarks>
    /// Seeding through the mapper costs at least one request per resource -- <c>CreateResource</c>
    /// adds an ASK on top of the commit -- which at the layered workloads' million triples would take
    /// longer than every measurement in the run together. Serialized triples through
    /// <see cref="IStore.Read(string, Uri, RdfSerializationFormat, bool)"/> is one request per chunk on
    /// every backend.
    ///
    /// Chunked because Virtuoso refuses one statement that touches more than 10,000 entries
    /// ("D1CTX: Hash dictionary is full, exceeded 10000 entries"). Through <c>Read</c> it fails loudly;
    /// through an update it fails <b>silently</b>, because <c>ExecuteDirectQuery</c> swallows the error
    /// (#50). So an update past the limit "succeeds" having written nothing. The chunk sits well under
    /// the limit. Callers still verify the count afterwards with
    /// <see cref="StoreBenchmarkBase.AssertSeeded"/>; the chunking makes the load succeed, the check
    /// proves it did.
    /// </remarks>
    public static class BenchmarkData
    {
        /// <summary>
        /// Triples per write: half of Virtuoso's 10,000-entry statement limit. It used to be 250,000,
        /// which was half of what ADR-0042 measured Virtuoso accepting in one update. But that update
        /// was reporting success while writing nothing (#50), and past 10,000 Virtuoso's <c>Read</c>
        /// fails outright, so no Virtuoso case above the smallest size had ever run.
        /// </summary>
        public const int ChunkSize = 5_000;

        /// <summary>
        /// Appends the triples <paramref name="write"/> produces to <paramref name="graph"/>.
        /// </summary>
        /// <param name="store">The store to load into.</param>
        /// <param name="graph">The graph to append to. Existing triples are kept.</param>
        /// <param name="count">How many items <paramref name="write"/> is called for.</param>
        /// <param name="write">
        /// Appends the N-Triples for item <c>i</c> to the builder and returns how many triples it wrote.
        /// </param>
        /// <returns>The number of triples written.</returns>
        public static int Seed(IStore store, Uri graph, int count, Func<StringBuilder, int, int> write)
        {
            var total = 0;
            var pending = 0;
            var buffer = new StringBuilder();

            for (var i = 0; i < count; i++)
            {
                var written = write(buffer, i);

                total += written;
                pending += written;

                if (pending >= ChunkSize)
                {
                    Flush(store, graph, buffer);
                    pending = 0;
                }
            }

            if (buffer.Length > 0)
            {
                Flush(store, graph, buffer);
            }

            return total;
        }

        /// <summary>
        /// Appends the two triples of a <see cref="BenchmarkPerson"/> with no links:
        /// <c>rdf:type</c> and <c>foaf:firstName</c>.
        /// </summary>
        public static int AppendPerson(StringBuilder buffer, Uri subject, string firstName)
        {
            buffer.Append('<').Append(subject.OriginalString).Append("> <")
                .Append("http://www.w3.org/1999/02/22-rdf-syntax-ns#type").Append("> <")
                .Append(Vocabulary.PersonClass).Append("> .\n");

            buffer.Append('<').Append(subject.OriginalString).Append("> <")
                .Append(Vocabulary.FirstNameProperty).Append("> \"")
                .Append(firstName).Append("\" .\n");

            return 2;
        }

        /// <summary>
        /// Appends one <c>foaf:knows</c> triple.
        /// </summary>
        public static int AppendKnows(StringBuilder buffer, Uri subject, Uri @object)
        {
            buffer.Append('<').Append(subject.OriginalString).Append("> <")
                .Append(Vocabulary.KnowsProperty).Append("> <")
                .Append(@object.OriginalString).Append("> .\n");

            return 1;
        }

        /// <summary>
        /// Seeds <paramref name="people"/> persons in a ring, each knowing the next
        /// <paramref name="links"/>, so every resource is both a subject and an object.
        /// </summary>
        /// <param name="store">The store to load into.</param>
        /// <param name="graph">The graph to append to.</param>
        /// <param name="people">Persons to write.</param>
        /// <param name="links">Outgoing <c>foaf:knows</c> links per person.</param>
        /// <param name="subject">The URI of the <c>i</c>th person.</param>
        /// <returns>The number of triples written: <c>people * (2 + links)</c>.</returns>
        public static int SeedRing(IStore store, Uri graph, int people, int links, Func<int, Uri> subject)
        {
            return Seed(store, graph, people, (buffer, i) =>
            {
                var written = AppendPerson(buffer, subject(i), $"Person {i}");

                for (var k = 1; k <= links; k++)
                {
                    written += AppendKnows(buffer, subject(i), subject((i + k) % people));
                }

                return written;
            });
        }

        private static void Flush(IStore store, Uri graph, StringBuilder buffer)
        {
            // Written as N-Triples, read as Turtle, of which N-Triples is a subset: Virtuoso's
            // Read(string, ...) has its own parser switch with no N-Triples case and throws
            // NotSupportedException, while every backend reads Turtle. SerializationBenchmarks reads
            // N-Triples on purpose, so that defect stays visible there.
            store.Read(buffer.ToString(), graph, RdfSerializationFormat.Turtle, update: true);
            buffer.Clear();
        }
    }
}
