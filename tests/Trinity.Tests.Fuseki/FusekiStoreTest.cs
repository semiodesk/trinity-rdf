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
using Semiodesk.Trinity.Store.Fuseki;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Fuseki
{
    /// <summary>
    /// Store-level tests for the Fuseki adapter -- the members the shared fixtures do not reach
    /// because they take the store as given. The model-catalog behaviour that used to live here is
    /// now shared across every backend in <see cref="Semiodesk.Trinity.Tests.Store.StoreCatalogTest{T}"/>.
    /// </summary>
    [TestFixture]
    public class FusekiStoreTest : StoreTest<FusekiTestSetup>
    {
        /// <summary>
        /// A dataset may hold blank-node-named graphs. They arrive from <c>ListGraphNames()</c> as
        /// bare labels, not IRIs, so constructing a <c>UriRef</c> from one throws and kills the whole
        /// enumeration. The obsolete <c>ListGraphs()</c> omitted them, so moving to
        /// <c>ListGraphNames()</c> (ADR-0038) is what exposed this.
        /// </summary>
        [Test]
        public void ListModelsSkipsBlankNodeNamedGraphsRatherThanThrowing()
        {
            var named = BaseUri.GetUriRef("list-models-named");

            Store.ExecuteNonQuery(new SparqlUpdate(
                $"INSERT DATA {{ GRAPH <{named}> {{ <urn:a> <urn:b> <urn:c> }} }}"));
            Store.ExecuteNonQuery(new SparqlUpdate(
                "INSERT DATA { GRAPH _:listModelsBlank { <urn:d> <urn:e> <urn:f> } }"));

            try
            {
                var models = Store.ListModels().ToList();

                Assert.IsTrue(models.Any(m => m.Uri.ToString() == named.ToString()),
                    "the IRI-named graph must still be listed");
                Assert.IsTrue(models.All(m => Uri.IsWellFormedUriString(m.Uri.ToString(), UriKind.Absolute)),
                    "every listed model must be addressable by IRI");
            }
            finally
            {
                Store.GetModel(named).Clear();
            }
        }

        /// <summary>
        /// <c>IsReady</c> used to be a hardcoded <c>true</c>, so it stayed true after Dispose.
        /// </summary>
        [Test]
        public void IsReadyReflectsTheConnectionRatherThanBeingHardcoded()
        {
            var store = StoreFactory.CreateStore(ConnectionString);

            Assert.IsTrue(store.IsReady);

            store.Dispose();

            Assert.IsFalse(store.IsReady, "a disposed store is not ready");
        }

        /// <summary>
        /// Dispose has to be safe to call twice; NUnit teardown and a using block together manage it.
        /// </summary>
        [Test]
        public void DisposeIsIdempotent()
        {
            var store = StoreFactory.CreateStore(ConnectionString);

            store.Dispose();

            Assert.DoesNotThrow(() => store.Dispose());
        }

        /// <summary>
        /// A store with no dataset connects, reports ready, and 404s on every query. Rejecting it at
        /// construction is the difference between one clear error and a store that looks healthy.
        /// </summary>
        [Test]
        public void ConstructionRequiresAHostAndADataset()
        {
            Assert.Throws<ArgumentException>(() => new FusekiStore(null, "ds"));
            Assert.Throws<ArgumentException>(() => new FusekiStore("http://localhost:3030", null));
            Assert.Throws<ArgumentException>(() => new FusekiStore("http://localhost:3030", ""));
        }

        /// <summary>
        /// The composed connector URL must not grow a double slash from a host that ends in one --
        /// the provider's default host does, a container-mapped host does not.
        /// </summary>
        [Test]
        public void TrailingSlashInTheHostIsNormalized()
        {
            using (var store = new FusekiStore("http://localhost:3030/", "ds"))
            {
                Assert.AreEqual("http://localhost:3030", ((FusekiStore)store).Hostname);
                Assert.AreEqual("ds", ((FusekiStore)store).Dataset);
            }
        }
    }
}
