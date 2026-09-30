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

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// The mapped type the benchmarks write and read.
    /// </summary>
    /// <remarks>
    /// Declared here rather than reusing the LINQ test object model, for two reasons. Most of that
    /// model is <c>internal</c> to the test assembly. More importantly, a benchmark that depends on
    /// a test fixture's shape changes its numbers whenever someone adds a property to that fixture
    /// for an unrelated test -- and the change would look like a performance result. Owning the
    /// model keeps the workload fixed.
    ///
    /// Two triples per instance: <c>rdf:type</c> and one literal. Deliberately minimal, so the
    /// measurement is dominated by the round trip rather than by serializing a wide resource.
    /// </remarks>
    [RdfClass(Vocabulary.PersonClass)]
    public partial class BenchmarkPerson : Resource
    {
        public BenchmarkPerson(Uri uri) : base(uri) { }

        [RdfProperty(Vocabulary.FirstNameProperty)]
        public partial string FirstName { get; set; }

        /// <summary>
        /// A resource-valued collection, so the lazy-loading cost has something to load.
        /// </summary>
        /// <remarks>
        /// Lazy loading is always on and not disablable (ADR-0023). Reading a resource does not fetch
        /// what it points at; touching this property does, through <c>ResourceCache</c>. That is the
        /// cost <see cref="LazyLoadBenchmarks"/> exists to put a number on.
        /// </remarks>
        [RdfProperty(Vocabulary.KnowsProperty)]
        public partial List<BenchmarkPerson> Knows { get; set; }
    }

    /// <summary>
    /// A resource with twenty mapped properties of mixed types, for the per-property cost of mapping.
    /// </summary>
    /// <remarks>
    /// <see cref="BenchmarkPerson"/> is deliberately minimal, so its numbers are dominated by the round
    /// trip. This is its opposite: the same number of requests, but twenty values each to serialize on
    /// the way out and to convert on the way back -- <c>XsdTypeMapper</c>, <c>PropertyMapping&lt;T&gt;</c>
    /// and the numeric conversion on read (ADR-0040). A separate type rather than new properties on
    /// <see cref="BenchmarkPerson"/>, whose remarks explain why its shape must not move.
    ///
    /// No language-tagged string: localized strings are represented inconsistently (ADR-0026/0027),
    /// so a benchmark of them would measure whichever representation happened to be chosen.
    /// </remarks>
    [RdfClass(Vocabulary.WideClass)]
    public partial class BenchmarkWideResource : Resource
    {
        public BenchmarkWideResource(Uri uri) : base(uri) { }

        [RdfProperty(Vocabulary.Wide + "s1")] public partial string S1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "s2")] public partial string S2 { get; set; }
        [RdfProperty(Vocabulary.Wide + "s3")] public partial string S3 { get; set; }
        [RdfProperty(Vocabulary.Wide + "s4")] public partial string S4 { get; set; }
        [RdfProperty(Vocabulary.Wide + "s5")] public partial string S5 { get; set; }
        [RdfProperty(Vocabulary.Wide + "i1")] public partial int I1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "i2")] public partial int I2 { get; set; }
        [RdfProperty(Vocabulary.Wide + "i3")] public partial int I3 { get; set; }
        [RdfProperty(Vocabulary.Wide + "l1")] public partial long L1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "l2")] public partial long L2 { get; set; }
        [RdfProperty(Vocabulary.Wide + "d1")] public partial double D1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "d2")] public partial double D2 { get; set; }
        [RdfProperty(Vocabulary.Wide + "f1")] public partial float F1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "m1")] public partial decimal M1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "m2")] public partial decimal M2 { get; set; }
        [RdfProperty(Vocabulary.Wide + "b1")] public partial bool B1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "b2")] public partial bool B2 { get; set; }
        [RdfProperty(Vocabulary.Wide + "t1")] public partial DateTime T1 { get; set; }
        [RdfProperty(Vocabulary.Wide + "t2")] public partial DateTime T2 { get; set; }
        [RdfProperty(Vocabulary.Wide + "ref")] public partial BenchmarkPerson Ref { get; set; }

        /// <summary>
        /// Mapped properties, so a guard can count what a write should have produced.
        /// </summary>
        public const int MappedProperties = 20;
    }

    /// <summary>
    /// The FOAF terms the benchmarks use, as constants an attribute can take.
    /// </summary>
    public static class Vocabulary
    {
        public const string PersonClass = "http://xmlns.com/foaf/0.1/Person";

        public const string FirstNameProperty = "http://xmlns.com/foaf/0.1/firstName";

        public const string KnowsProperty = "http://xmlns.com/foaf/0.1/knows";

        /// <summary>
        /// The namespace of <see cref="BenchmarkWideResource"/>'s properties. Not a real vocabulary,
        /// and not one Trinity has generated, so no TRIN006 check applies to it.
        /// </summary>
        public const string Wide = "http://localhost/benchmark/wide#";

        public const string WideClass = Wide + "WideResource";
    }
}
