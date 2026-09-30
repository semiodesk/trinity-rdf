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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Writing and reading resources with twenty mapped values each: the per-property cost of mapping.
    /// </summary>
    /// <remarks>
    /// Both directions go through the batch paths -- <c>UpdateResources</c> out, <c>GetResources</c>
    /// back -- so the round trips are few and fixed, and what grows with the width is serialization
    /// (<c>XsdTypeMapper</c>, <c>SparqlSerializer</c>) and conversion on read (ADR-0040). Against
    /// <see cref="WriteBenchmarks"/> and <see cref="ReadBenchmarks"/>, whose resource carries one value,
    /// the difference is what each further property costs.
    ///
    /// The raw baselines carry the same typed literals, written as the store would receive them, so the
    /// Ratio is mapping and conversion and not the size of the payload.
    /// </remarks>
    public class WideResourceBenchmarks : StoreBenchmarkBase
    {
        private const string Xsd = "http://www.w3.org/2001/XMLSchema#";

        private static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Resources written or read per invocation.
        /// </summary>
        [Params(100, 1000)]
        public int Count { get; set; }

        /// <summary>
        /// rdf:type plus every mapped property.
        /// </summary>
        private int ExpectedTriples => Count * (1 + BenchmarkWideResource.MappedProperties);

        private IModel _readModel;

        private List<BenchmarkWideResource> _pending;

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            // Reads come from their own graph, seeded once, because the write rows clear Model every
            // iteration.
            _readModel = Store.GetModel(BaseUri.GetUriRef("wide-read"));
            _readModel.Clear();

            Store.UpdateResources(Build(), _readModel.Uri);

            AssertSeeded(ExpectedTriples, _readModel.Uri);

            // One conversion check, outside the timed region: a read that silently fell back to
            // defaults would be timed as a cheap one.
            var sum = _readModel.GetResources<BenchmarkWideResource>().Sum(r => (long)r.I1);
            var expected = (long)Count * (Count - 1) / 2;

            if (sum != expected)
            {
                throw new InvalidOperationException(
                    $"{Backend}: mapped int values read back summing to {sum}, expected {expected}.");
            }
        }

        public override void GlobalCleanup()
        {
            _readModel?.Clear();

            base.GlobalCleanup();
        }

        [IterationSetup(Targets = new[] { nameof(WriteMapped), nameof(WriteRaw) })]
        public void IterationSetup()
        {
            Model.Clear();

            // Built outside the timed region, so the row is the write and not the object construction.
            _pending = Build();
        }

        [IterationCleanup(Targets = new[] { nameof(WriteMapped), nameof(WriteRaw) })]
        public void IterationCleanup()
        {
            AssertWrote(ExpectedTriples);
        }

        [Benchmark(Description = "UpdateResources (mapped, 20 values each)")]
        [BenchmarkCategory("Write")]
        public void WriteMapped()
        {
            Store.UpdateResources(_pending, Model.Uri);
        }

        [Benchmark(Description = "INSERT DATA typed literals (raw)", Baseline = true)]
        [BenchmarkCategory("Write")]
        public void WriteRaw()
        {
            var triples = new StringBuilder();

            for (var i = 0; i < Count; i++)
            {
                AppendRaw(triples, i);
            }

            Store.ExecuteNonQuery(new SparqlUpdate($"INSERT DATA {{ GRAPH <{Model.Uri}> {{ {triples} }} }}"));
        }

        [Benchmark(Description = "GetResources<T>() (mapped, 20 values each)")]
        [BenchmarkCategory("Read")]
        public int ReadMapped()
        {
            return Expect(_readModel.GetResources<BenchmarkWideResource>().Count(), Count, "GetResources<T>()");
        }

        [Benchmark(Description = "SELECT ?s ?p ?o (raw)", Baseline = true)]
        [BenchmarkCategory("Read")]
        public int ReadRaw()
        {
            var query = new SparqlQuery(
                $"SELECT ?s ?p ?o FROM <{_readModel.Uri}> WHERE {{ ?s ?p ?o . ?s a <{Vocabulary.WideClass}> . }}",
                declarePrefixes: false);

            return Expect(Store.ExecuteQuery(query).GetBindings().Count(), ExpectedTriples, "SELECT ?s ?p ?o");
        }

        private List<BenchmarkWideResource> Build()
        {
            var result = new List<BenchmarkWideResource>(Count);

            for (var i = 0; i < Count; i++)
            {
                var r = new BenchmarkWideResource(BaseUri.GetUriRef($"wide-{i}"));

                r.SetModel(Model);
                r.IsNew = true;

                r.S1 = $"Value 1 of {i}";
                r.S2 = $"Value 2 of {i}";
                r.S3 = $"Value 3 of {i}";
                r.S4 = $"Value 4 of {i}";
                r.S5 = $"Value 5 of {i}";
                r.I1 = i;
                r.I2 = i * 2;
                r.I3 = -i;
                r.L1 = i * 1_000_000_000L;
                r.L2 = -i;
                r.D1 = i * 0.5;
                r.D2 = i / 4.0;
                r.F1 = i * 0.25f;
                r.M1 = i * 1.5m;
                r.M2 = i / 8m;
                r.B1 = i % 2 == 0;
                r.B2 = true;
                r.T1 = Epoch.AddMinutes(i);
                r.T2 = Epoch.AddDays(i);
                r.Ref = new BenchmarkPerson(PersonUri(i % 10));

                result.Add(r);
            }

            return result;
        }

        private void AppendRaw(StringBuilder triples, int i)
        {
            var s = $"<{BaseUri.GetUriRef($"wide-{i}")}>";

            void Literal(string property, string value, string datatype)
            {
                triples.Append(s).Append(" <").Append(Vocabulary.Wide).Append(property).Append("> \"")
                    .Append(value).Append('"');

                if (datatype != null)
                {
                    triples.Append("^^<").Append(Xsd).Append(datatype).Append('>');
                }

                triples.Append(" . ");
            }

            triples.Append(s).Append(" a <").Append(Vocabulary.WideClass).Append("> . ");

            for (var k = 1; k <= 5; k++)
            {
                Literal($"s{k}", $"Value {k} of {i}", null);
            }

            Literal("i1", XmlConvert.ToString(i), "int");
            Literal("i2", XmlConvert.ToString(i * 2), "int");
            Literal("i3", XmlConvert.ToString(-i), "int");
            Literal("l1", XmlConvert.ToString(i * 1_000_000_000L), "long");
            Literal("l2", XmlConvert.ToString((long)-i), "long");
            Literal("d1", XmlConvert.ToString(i * 0.5), "double");
            Literal("d2", XmlConvert.ToString(i / 4.0), "double");
            Literal("f1", XmlConvert.ToString(i * 0.25f), "float");
            Literal("m1", (i * 1.5m).ToString(CultureInfo.InvariantCulture), "decimal");
            Literal("m2", (i / 8m).ToString(CultureInfo.InvariantCulture), "decimal");
            Literal("b1", i % 2 == 0 ? "true" : "false", "boolean");
            Literal("b2", "true", "boolean");
            Literal("t1", XmlConvert.ToString(Epoch.AddMinutes(i), XmlDateTimeSerializationMode.Utc), "dateTime");
            Literal("t2", XmlConvert.ToString(Epoch.AddDays(i), XmlDateTimeSerializationMode.Utc), "dateTime");

            triples.Append(s).Append(" <").Append(Vocabulary.Wide).Append("ref> <").Append(PersonUri(i % 10)).Append("> . ");
        }
    }
}
