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
    /// The FOAF terms the benchmarks use, as constants an attribute can take.
    /// </summary>
    public static class Vocabulary
    {
        public const string PersonClass = "http://xmlns.com/foaf/0.1/Person";

        public const string FirstNameProperty = "http://xmlns.com/foaf/0.1/firstName";

        public const string KnowsProperty = "http://xmlns.com/foaf/0.1/knows";
    }
}
