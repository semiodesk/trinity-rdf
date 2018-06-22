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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Enumerates all supported RDF serialization formats.
    /// </summary>
    public enum RdfSerializationFormat
    {
        /// <summary>
        /// RDF/XML <see href="http://www.w3.org/TR/REC-rdf-syntax/">http://www.w3.org/TR/REC-rdf-syntax/</see>
        /// </summary>
        RdfXml,
        /// <summary>
        /// N3 <see href="http://www.w3.org/TeamSubmission/n3/">http://www.w3.org/TeamSubmission/n3/</see>
        /// </summary>
        N3,
        /// <summary>
        /// NTriples <see href="http://www.w3.org/2001/sw/RDFCore/ntriples/">http://www.w3.org/2001/sw/RDFCore/ntriples/</see>
        /// </summary>
        NTriples,
#if !NET35
        /// <summary>
        /// NTriples <see href="https://www.w3.org/TR/2014/REC-n-quads-20140225/">https://www.w3.org/TR/2014/REC-n-quads-20140225/</see>
        /// </summary>
        NQuads,
#endif
        /// <summary>
        /// TriG <see href="http://www.w3.org/TR/trig/">http://www.w3.org/TR/trig/</see>
        /// </summary>
        Trig,
        /// <summary>
        /// Turtle <see href="http://www.w3.org/TR/turtle/">http://www.w3.org/TR/turtle/</see>
        /// </summary>
        Turtle,
        /// <summary>
        /// JSON
        /// </summary>
        Json,
#if !NET35
        /// <summary>
        /// JSON-LD <see href="https://www.w3.org/TR/json-ld/">https://www.w3.org/TR/json-ld/</see>
        /// </summary>
        JsonLd
#endif
    };
}
