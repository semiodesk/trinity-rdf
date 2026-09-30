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

        private string _ntriples;

        private string _jsonLd;

        private string _exported;

        private RdfSerializationFormat _exportedFormat;

        private string _insert;

        private IModel _exportModel;

        // One method per format rather than a Format parameter. As a parameter, the raw baselines --
        // which do not depend on the format -- ran once per format value and reported the same cell
        // three times. As methods, each category has one baseline and a row per format against it.

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            var ntriples = new StringBuilder();

            for (var i = 0; i < People; i++)
            {
                BenchmarkData.AppendPerson(ntriples, PersonUri(i), $"Person {i}");
            }

            // N-Triples is valid Turtle, so one document serves both; the parser each row selects is
            // what differs.
            _ntriples = ntriples.ToString();
            _insert = $"INSERT DATA {{ GRAPH <{Model.Uri}> {{ {_ntriples} }} }}";

            _exportModel = Store.GetModel(BaseUri.GetUriRef("export"));
            _exportModel.Clear();

            // Through BenchmarkData, in chunks and as Turtle: this is fixture, not what is measured.
            // One Read of 20,000 triples exceeds Virtuoso's 10,000-entry statement limit (#70), and
            // Virtuoso cannot read N-Triples from a string (#55). The timed Read rows use neither
            // workaround, so they fail there and the defects stay visible.
            BenchmarkData.Seed(Store, _exportModel.Uri, People,
                (buffer, i) => BenchmarkData.AppendPerson(buffer, PersonUri(i), $"Person {i}"));

            AssertSeeded(People * 2, _exportModel.Uri);
        }

        /// <summary>
        /// The JSON-LD row's setup. JSON-LD is not a superset of anything, so its document is this
        /// store's own export of the same graph -- built only for that row, so an export that fails on
        /// some store fails one cell rather than every case in the class.
        /// </summary>
        [GlobalSetup(Target = nameof(ReadJsonLd))]
        public void GlobalSetupJsonLd()
        {
            GlobalSetup();

            _jsonLd = Export(RdfSerializationFormat.JsonLd);
        }

        public override void GlobalCleanup()
        {
            _exportModel?.Clear();

            base.GlobalCleanup();
        }

        [IterationSetup(Targets = new[] { nameof(ReadTurtle), nameof(ReadNTriples), nameof(ReadJsonLd), nameof(ReadRaw) })]
        public void IterationSetup()
        {
            Model.Clear();
        }

        [IterationCleanup(Targets = new[] { nameof(ReadTurtle), nameof(ReadNTriples), nameof(ReadJsonLd), nameof(ReadRaw) })]
        public void IterationCleanup()
        {
            AssertWrote(People * 2);
        }

        [Benchmark(Description = "IStore.Read Turtle (parse + load)")]
        [BenchmarkCategory("Read")]
        public void ReadTurtle()
        {
            Store.Read(_ntriples, Model.Uri, RdfSerializationFormat.Turtle, update: true);
        }

        [Benchmark(Description = "IStore.Read N-Triples (parse + load)")]
        [BenchmarkCategory("Read")]
        public void ReadNTriples()
        {
            Store.Read(_ntriples, Model.Uri, RdfSerializationFormat.NTriples, update: true);
        }

        [Benchmark(Description = "IStore.Read JSON-LD (parse + load)")]
        [BenchmarkCategory("Read")]
        public void ReadJsonLd()
        {
            Store.Read(_jsonLd, Model.Uri, RdfSerializationFormat.JsonLd, update: true);
        }

        [Benchmark(Description = "INSERT DATA the same triples (raw)", Baseline = true)]
        [BenchmarkCategory("Read")]
        public void ReadRaw()
        {
            Store.ExecuteNonQuery(new SparqlUpdate(_insert));
        }

        [Benchmark(Description = "IStore.Write Turtle (fetch + serialize)")]
        [BenchmarkCategory("Write")]
        public int WriteTurtle() => Exported(RdfSerializationFormat.Turtle);

        [Benchmark(Description = "IStore.Write N-Triples (fetch + serialize)")]
        [BenchmarkCategory("Write")]
        public int WriteNTriples() => Exported(RdfSerializationFormat.NTriples);

        [Benchmark(Description = "IStore.Write JSON-LD (fetch + serialize)")]
        [BenchmarkCategory("Write")]
        public int WriteJsonLd() => Exported(RdfSerializationFormat.JsonLd);

        [Benchmark(Description = "SELECT ?s ?p ?o (raw fetch)", Baseline = true)]
        [BenchmarkCategory("Write")]
        public int WriteRaw()
        {
            var query = new SparqlQuery($"SELECT ?s ?p ?o FROM <{_exportModel.Uri}> WHERE {{ ?s ?p ?o }}",
                declarePrefixes: false);

            return Expect(Store.ExecuteQuery(query).GetBindings().Count(), People * 2, "SELECT ?s ?p ?o");
        }

        /// <summary>
        /// Exports the graph and keeps the document for <see cref="VerifyExport"/>.
        /// </summary>
        private int Exported(RdfSerializationFormat format)
        {
            _exported = Export(format);
            _exportedFormat = format;

            return _exported.Length;
        }

        /// <summary>
        /// Parses the exported document back and requires every triple of the graph in it.
        /// </summary>
        /// <remarks>
        /// By content, outside the timed region. Looking for one IRI in the text proves little: the
        /// order of an export is up to the store, so a document that lost most of the graph could still
        /// name any given resource. Parsed locally, with dotNetRDF's own parser rather than
        /// <c>Store.Read</c>, so a store's read defect (#55 on Virtuoso) cannot fail an export that is
        /// fine.
        /// </remarks>
        [IterationCleanup(Targets = new[] { nameof(WriteTurtle), nameof(WriteNTriples), nameof(WriteJsonLd) })]
        public void VerifyExport()
        {
            int parsed;

            using (var reader = new StringReader(_exported))
            {
                if (_exportedFormat == RdfSerializationFormat.JsonLd)
                {
                    var store = new VDS.RDF.TripleStore();

                    new VDS.RDF.Parsing.JsonLdParser().Load(store, reader);
                    parsed = store.Graphs.Sum(g => g.Triples.Count);
                }
                else
                {
                    var graph = new VDS.RDF.Graph();
                    VDS.RDF.IRdfReader parser = _exportedFormat == RdfSerializationFormat.NTriples
                        ? new VDS.RDF.Parsing.NTriplesParser()
                        : (VDS.RDF.IRdfReader)new VDS.RDF.Parsing.TurtleParser();

                    parser.Load(graph, reader);
                    parsed = graph.Triples.Count;
                }
            }

            Expect(parsed, People * 2, $"{_exportedFormat} export, parsed back");
        }

        private string Export(RdfSerializationFormat format)
        {
            using (var stream = new MemoryStream())
            {
                Store.Write(stream, _exportModel.Uri, format, leaveOpen: true);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
