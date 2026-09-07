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
using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.DotNetRDF
{
    /// <summary>
    /// Runs the materialization suite against the dotNetRDF in-memory store.
    /// </summary>
    [TestFixture]
    public class DotNetRDFLayeredModelMaterializationTest : LayeredModelMaterializationTest<DotNetRDFTestSetup>
    {
        /// <summary>
        /// Entailments over a materialized view are computed from the <i>effective</i> triples, so a
        /// staged removal withdraws what it entailed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Here rather than in the shared fixture because this is the one backend where a reasoner is
        /// guaranteed: ADR-0045 gave the in-memory store per-query RDFS entailment, and ADR-0022 leaves
        /// every other store free to ignore the flag (Fuseki has no per-query switch at all). The shared
        /// fixture therefore asserts only that the read is <i>accepted</i>.
        /// </para>
        /// <para>
        /// This is the combination the merge of ADR-0042 and ADR-0045 created and neither ADR tested:
        /// before in-memory inferencing existed, <c>inferenceEnabled: true</c> on a materialized view
        /// was accepted and then quietly did nothing, so "materialization lifts inferencing" could not
        /// be checked on this store at all.
        /// </para>
        /// </remarks>
        [Test]
        public void EntailmentsOverAMaterializedViewHonourTheOverlay()
        {
            // nco:PersonContact rdfs:subClassOf nco:Contact, seeded by TestOntologies for every store
            // test. Stated in the baseline; entailed only with the flag.
            var stated = new Uri("http://example.org/mat/contact-stated");
            var withdrawn = new Uri("http://example.org/mat/contact-withdrawn");

            foreach (Uri uri in new[] { stated, withdrawn })
            {
                var resource = Baseline.CreateResource(uri);
                resource.AddProperty(rdf.type, nco.PersonContact);
                resource.Commit();
            }

            // Stage the removal of one resource's stated type.
            Removals.ExecuteUpdate(new SparqlUpdate(
                "INSERT DATA { GRAPH @removals { @s @p @t } }")
                .Bind("@removals", Removals)
                .Bind("@s", withdrawn)
                .Bind("@p", rdf.type.Uri)
                .Bind("@t", nco.PersonContact.Uri));

            Materialized.Refresh();

            var query = new SparqlQuery($"SELECT ?s WHERE {{ ?s a <{nco.Contact.Uri}> }}", declarePrefixes: false);

            Assert.IsEmpty(Materialized.GetBindings(query).ToList(),
                "precondition: nco:Contact is entailed, not stated, so it is invisible without the flag");

            var inferred = Materialized.GetBindings(query, inferenceEnabled: true)
                .Select(b => b["s"].ToString()).ToList();

            Assert.Contains(stated.ToString(), inferred,
                "the effective graph states nco:PersonContact for this one, so nco:Contact is entailed");

            Assert.IsFalse(inferred.Contains(withdrawn.ToString()),
                "and its type is staged for removal, so the entailment must be withdrawn with it - " +
                "reasoning over the effective graph rather than over the baseline is the whole point");

            // Control: the same query over the baseline entails it for both, so the exclusion above is
            // the overlay doing its job and not an accident of how the reasoner was seeded.
            var fromBaseline = Baseline.GetBindings(
                new SparqlQuery($"SELECT ?s WHERE {{ ?s a <{nco.Contact.Uri}> }}", declarePrefixes: false),
                inferenceEnabled: true).Select(b => b["s"].ToString()).ToList();

            Assert.Contains(withdrawn.ToString(), fromBaseline,
                "the baseline still states the type, so reasoning over it entails nco:Contact - which " +
                "is exactly what the materialized view must not report");
        }
    }
}
