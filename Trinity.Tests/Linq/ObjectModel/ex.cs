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
// Copyright (c) Semiodesk GmbH 2018

using System;

namespace Semiodesk.Trinity.Tests.Linq
{
    ///<summary>
    ///Example vocabulary.
    ///</summary>
    public class ex : Ontology
    {
        public static readonly Uri Namespace = new Uri("http://example.org/test");
        public static Uri GetNamespace() { return Namespace; }

        public static readonly string Prefix = "ex";
        public static string GetPrefix() { return Prefix; }

        public static readonly UriRef Alice = new UriRef("http://example.org/test/Alice");

        public static readonly UriRef Bob = new UriRef("http://example.org/test/Bob");

        public static readonly UriRef Eve = new UriRef("http://example.org/test/Eve");

        public static readonly UriRef John = new UriRef("http://example.org/test/John");

        public static readonly UriRef TheSpiders = new UriRef("http://example.org/test/TheSpiders");

        public static readonly UriRef AlicaKeys = new UriRef("http://example.org/test/AlicaKeys");

        public static readonly UriRef JohnLennon = new UriRef("http://example.org/test/JohnLennon");

        public static readonly UriRef PaulMcCartney = new UriRef("http://example.org/test/PaulMcCartney");

        public static readonly UriRef TheBeatles = new UriRef("http://example.org/test/TheBeatles");

        public static readonly UriRef TheBeatlesAlbum = new UriRef("http://example.org/test/TheBeatlesAlbum");

        public static readonly UriRef BackInTheUSSR = new UriRef("http://example.org/test/BackInTheUSSR");
    }
}
