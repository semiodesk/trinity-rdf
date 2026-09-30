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

namespace Semiodesk.Trinity.Tests
{
    /// <summary>
    /// A generator-authored mapped class using the localized-text containers, in the shape ADR-0048
    /// recommends: <c>partial</c>, attribute-mapped, and <b>get-only</b>.
    /// </summary>
    /// <remarks>
    /// The hand-written counterpart is <c>LocalizedMappingTestClass</c>. Both routes have to stay
    /// first-class (ADR-0018), and the generator route is the one that was briefly impossible: emitting
    /// <c>get</c>+<c>set</c> unconditionally made a get-only mapped property CS9253, so a container
    /// could only be declared in the shape that TRIN009 now warns about. This class compiling at all is
    /// the regression test for that.
    /// </remarks>
    [RdfClass("semio:test:LocalizedDocument")]
    public partial class LocalizedDocument : Resource
    {
        public LocalizedDocument(Uri uri) : base(uri) { }

        /// <summary>One value per language.</summary>
        [RdfProperty("semio:test:documentTitle")]
        public partial LocalizedString Title { get; }

        /// <summary>Any number of values per language.</summary>
        [RdfProperty("semio:test:documentKeyword")]
        public partial LocalizedStringCollection Keywords { get; }

        /// <summary>Untagged by construction, beside the containers on the same class.</summary>
        [RdfProperty("semio:test:documentCode")]
        public partial string Code { get; set; }
    }
}
