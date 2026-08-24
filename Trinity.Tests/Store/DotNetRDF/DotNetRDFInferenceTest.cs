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
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.DotNetRDF
{
    /// <summary>
    /// The in-memory store's RDFS entailment, from the angles the shared inferencing tests do not
    /// cover: that entailments stay <b>out</b> of non-inferred queries, that they survive a write, and
    /// that the graphs holding them stay invisible.
    /// </summary>
    [TestFixture]
    public class DotNetRDFInferenceTest : StoreTest<DotNetRDFTestSetup>
    {
        /// <summary>
        /// The whole point of materializing into a side graph. If entailments ever leak into the model
        /// graph, this is what catches it -- and every other inferencing test would still pass.
        /// </summary>
        [Test]
        public void InferredTriplesAreInvisibleWithoutTheFlag()
        {
            var uri = BaseUri.GetUriRef("inference-leak");
            var resource = Model1.CreateResource(uri);
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery("ASK WHERE { @s a @t . }")
                .Bind("@s", uri)
                .Bind("@t", nco.Contact);

            Assert.IsFalse(Model1.ExecuteQuery(query).GetAnwser(),
                "nco:Contact is entailed, not stated -- it must not be visible without inference");

            Assert.IsTrue(Model1.ExecuteQuery(query, true).GetAnwser(),
                "...and it must be visible with inference");
        }

        /// <summary>
        /// Entailments are cached, and every write throws the cache away. A stale cache would answer
        /// the second query from the first query's entailments and miss the new resource.
        /// </summary>
        [Test]
        public void EntailmentsAreRecomputedAfterAWrite()
        {
            var first = Model1.CreateResource(BaseUri.GetUriRef("inference-first"));
            first.AddProperty(rdf.type, nco.PersonContact);
            first.Commit();

            Assert.AreEqual(1, Model1.GetResources<Contact>(true).Count(),
                "the first resource is entailed to be a Contact");

            var second = Model1.CreateResource(BaseUri.GetUriRef("inference-second"));
            second.AddProperty(rdf.type, nco.PersonContact);
            second.Commit();

            Assert.AreEqual(2, Model1.GetResources<Contact>(true).Count(),
                "a write after the entailments were materialized must invalidate them");
        }

        /// <summary>
        /// Deleting the data has to withdraw the entailment too -- the inverse staleness bug.
        /// </summary>
        [Test]
        public void EntailmentsAreWithdrawnWhenTheDataGoes()
        {
            var uri = BaseUri.GetUriRef("inference-withdrawn");
            var resource = Model1.CreateResource(uri);
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            Assert.AreEqual(1, Model1.GetResources<Contact>(true).Count());

            Model1.DeleteResource(uri);

            Assert.AreEqual(0, Model1.GetResources<Contact>(true).Count(),
                "the entailment must go when the triple it was derived from does");
        }

        /// <summary>
        /// The graphs holding entailments belong to the store, not the caller, so they must not appear
        /// as models.
        /// </summary>
        [Test]
        public void InferenceGraphsAreNotModels()
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-hidden"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            // Force the entailments to be materialized.
            Model1.GetResources<Contact>(true).ToList();

            var models = Store.ListModels().Select(m => m.Uri.ToString()).ToList();

            CollectionAssert.IsEmpty(
                models.Where(m => m.StartsWith("urn:semiodesk:trinity:inferred:", StringComparison.Ordinal)).ToList(),
                "inference graphs must not be listed as models:\n" + string.Join("\n", models));
        }

        /// <summary>
        /// Inference must not widen a query that named no graph into one that names several.
        /// </summary>
        [Test]
        public void QueryWithoutADatasetIsUnaffectedByTheFlag()
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("inference-nodataset"));
            resource.AddProperty(rdf.type, nco.PersonContact);
            resource.Commit();

            var query = new SparqlQuery("SELECT ?s WHERE { ?s ?p ?o . }");

            Assert.DoesNotThrow(() => Model1.ExecuteQuery(query, true).GetBindings().ToList());
        }
    }
}
