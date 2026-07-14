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
// Copyright (c) Semiodesk GmbH 2015-2020

using System;

namespace Semiodesk.Trinity.Tests
{
    /// <summary>
    /// Seeds a Fuseki store with the schema graphs used by the integration tests. Replaces the
    /// retired <c>ontologies.config</c> auto-loading (<c>InitializeFromConfiguration()</c>,
    /// ADR-0011) with an explicit <see cref="StoreExtensions.LoadGraphs"/> call.
    /// </summary>
    internal static class TestOntologies
    {
        /// <summary>
        /// Reads the standard test ontologies into their named graphs of the given store.
        /// </summary>
        /// <param name="store">The store to seed.</param>
        public static void LoadInto(IStore store)
        {
            store.LoadGraphs(new (Uri, string)[]
            {
                (new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"),                    "ontologies/rdf.rdf"),
                (new Uri("http://www.w3.org/2000/01/rdf-schema#"),                          "ontologies/rdfs.n3"),
                (new Uri("http://www.w3.org/2002/07/owl#"),                                 "ontologies/owl.n3"),
                (new Uri("http://xmlns.com/foaf/0.1/"),                                     "ontologies/foaf.rdf"),
                (new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#"),      "ontologies/nco.trig"),
            });
        }
    }
}
