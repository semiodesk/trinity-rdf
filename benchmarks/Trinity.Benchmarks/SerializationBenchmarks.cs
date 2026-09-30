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

using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Loading and exporting whole graphs: <c>IStore.Read</c> and <c>IStore.Write</c> (ADR-0034).
    /// </summary>
    /// <remarks>
    /// The bulk path around the object mapper: seeding a store from a file, exporting one. Each
    /// backend has its own implementation of both (<c>StoreBase.TryParse</c> is shared, the transport
    /// is not), which is where ADR-0047 found dotNetRDF emitting BOM-prefixed Turtle and invalid
    /// RDF/XML, so a regression here is as likely to be in an adapter as in the engine.
    ///
    /// <b>Read</b>: parse a document and load it, against the same triples as one
    /// <c>INSERT DATA</c> -- so the Ratio is parsing plus whatever transport the adapter chose.
    /// <b>Write</b>: export a graph, against fetching the same triples as bindings -- the part a
    /// serializer cannot avoid -- so the Ratio is serialization plus the adapter's own fetch.
    /// </remarks>
    public class SerializationBenchmarks : StoreBenchmarkBase
    {
        /// <summary>
        /// Resources in the document, two triples each.
        /// </summary>
        [Params(1000, 10_000)]
        public int People { get; set; }

        /// <summary>
        /// The formats a document is read in.
        /// </summary>
        [Params(RdfSerializationFormat.Turtle, RdfSerializationFormat.NTriples, RdfSerializationFormat.JsonLd)]
        public RdfSerializationFormat Format { get; set; }

        private string _document;

        private string _insert;

        private IModel _exportModel;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            var ntriples = new StringBuilder();

            for (var i = 0; i < People; i++)
            {
                BenchmarkData.AppendPerson(ntriples, PersonUri(i), $"Person {i}");
            }

            _insert = $"INSERT DATA {{ GRAPH <{Model.Uri}> {{ {ntriples} }} }}";

            _exportModel = Store.GetModel(BaseUri.GetUriRef("export"));
            _exportModel.Clear();

            // As Turtle, like BenchmarkData: this is fixture setup, and Virtuoso cannot read N-Triples
            // from a string. The timed ReadDocument row does use Format, so that cell fails there.
            Store.Read(ntriples.ToString(), _exportModel.Uri, RdfSerializationFormat.Turtle, update: true);

            AssertSeeded(People * 2, _exportModel.Uri);

            // N-Triples is valid Turtle, so one document serves both; the parser Format selects is what
            // differs. JSON-LD is not a superset of anything, so its document is this store's own export
            // of the same graph -- which also proves the export is readable back, before anything is timed.
            _document = Format == RdfSerializationFormat.JsonLd ? Export(RdfSerializationFormat.JsonLd) : ntriples.ToString();
        }

        public override void GlobalCleanup()
        {
            _exportModel?.Clear();

            base.GlobalCleanup();
        }

        [IterationSetup(Targets = new[] { nameof(ReadDocument), nameof(ReadRaw) })]
        public void IterationSetup()
        {
            Model.Clear();
        }

        [IterationCleanup(Targets = new[] { nameof(ReadDocument), nameof(ReadRaw) })]
        public void IterationCleanup()
        {
            AssertWrote(People * 2);
        }

        [Benchmark(Description = "IStore.Read (parse + load)")]
        [BenchmarkCategory("Read")]
        public void ReadDocument()
        {
            Store.Read(_document, Model.Uri, Format, update: true);
        }

        [Benchmark(Description = "INSERT DATA the same triples (raw)", Baseline = true)]
        [BenchmarkCategory("Read")]
        public void ReadRaw()
        {
            Store.ExecuteNonQuery(new SparqlUpdate(_insert));
        }

        [Benchmark(Description = "IStore.Write (fetch + serialize)")]
        [BenchmarkCategory("Write")]
        public int WriteDocument()
        {
            return Export(Format).Length;
        }

        private string Export(RdfSerializationFormat format)
        {
            using (var stream = new MemoryStream())
            {
                Store.Write(stream, _exportModel.Uri, format, leaveOpen: true);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        [Benchmark(Description = "SELECT ?s ?p ?o (raw fetch)", Baseline = true)]
        [BenchmarkCategory("Write")]
        public int WriteRaw()
        {
            var query = new SparqlQuery($"SELECT ?s ?p ?o FROM <{_exportModel.Uri}> WHERE {{ ?s ?p ?o }}",
                declarePrefixes: false);

            return Store.ExecuteQuery(query).GetBindings().Count();
        }
    }
}
