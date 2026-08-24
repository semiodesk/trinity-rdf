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
// Copyright (c) Semiodesk GmbH 2023

using System;
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Reading a TriG file that declares more than one named graph.
    /// </summary>
    /// <remarks>
    /// <c>nco.trig</c> holds two — <c>nco#</c> (542 triples, including the class hierarchy the
    /// inferencing tests reason over) and <c>nco_metadata#</c> (12). Two defects lived in the read
    /// path and only showed up once something seeded such a file: the delete targeted the caller's
    /// <c>graphUri</c> once per iteration rather than the graph being written, and <c>BaseUri</c> was
    /// never set, so the connector sent every graph to the <i>default</i> graph where the last one
    /// silently overwrote the rest. Measured before the fix: Fuseki ended up with 12 triples in the
    /// default graph and nothing in <c>nco#</c> at all.
    /// </remarks>
    [TestFixture]
    public abstract class MultiGraphTrigTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        private static readonly Uri Nco =
            new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#");

        private static readonly Uri NcoMetadata =
            new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco_metadata#");

        /// <summary>
        /// Every graph in the file lands under its own name, and re-reading does not wipe the ones
        /// already written. Seeding runs once per fixture, so "survives a re-read" is the case that
        /// actually occurs.
        /// </summary>
        [Test]
        public virtual void EveryGraphInAMultiGraphTrigSurvivesAReRead()
        {
            Assert.IsTrue(HasAxiom(), "the seeded nco class hierarchy must be queryable in <nco#>");
            Assert.Greater(Count(NcoMetadata), 0, "nco_metadata# must be populated too");

            // Re-read exactly as a second fixture's seeding would.
            Store.Read(Nco, TestOntologies.PathOf(Nco), RdfSerializationFormat.Trig, false);

            Assert.IsTrue(HasAxiom(),
                "re-reading the file must not delete the graph an earlier iteration just wrote");
            Assert.Greater(Count(NcoMetadata), 0, "...nor the other way round");
        }

        private bool HasAxiom()
        {
            var query = new SparqlQuery(
                $"ASK FROM <{Nco}> WHERE {{ <{Nco}PersonContact> "
                + "<http://www.w3.org/2000/01/rdf-schema#subClassOf> ?o }");

            return Store.ExecuteQuery(query).GetAnwser();
        }

        private int Count(Uri graph)
        {
            var query = new SparqlQuery($"SELECT ?s ?p ?o FROM <{graph}> WHERE {{ ?s ?p ?o }}");

            return Store.ExecuteQuery(query).GetBindings().Count();
        }
    }
}
