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
using Semiodesk.Trinity.Store.Oxigraph;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Oxigraph
{
    /// <summary>
    /// Store-level tests for the Oxigraph adapter -- the members the shared fixtures do not reach
    /// because they take the store as given.
    /// </summary>
    [TestFixture]
    public class OxigraphStoreTest : StoreTest<OxigraphTestSetup>
    {
        /// <summary>
        /// Oxigraph refuses a blank node as a graph <i>name</i>, so the blank-node-named graph that
        /// <c>ListModels</c> has to skip on the other backends cannot be created here at all.
        /// </summary>
        /// <remarks>
        /// This is Oxigraph being right and the others being lenient: SPARQL's GRAPH clause takes an
        /// IRI, and <c>INSERT DATA { GRAPH _:b { ... } }</c> is not legal. Fuseki, GraphDB and
        /// Virtuoso accept it as an extension, which is why <c>ListModels</c> needs the well-formed
        /// IRI filter at all -- and that filter stays, because the store layer is shared and a graph
        /// name it cannot turn into a UriRef must be skipped rather than kill the enumeration.
        ///
        /// So this asserts the two halves that are reachable: an IRI-named graph is listed, and every
        /// listed model is addressable.
        /// </remarks>
        [Test]
        public void ListModelsReturnsAddressableGraphsAndRefusesABlankGraphName()
        {
            var named = BaseUri.GetUriRef("list-models-named");

            Store.ExecuteNonQuery(new SparqlUpdate(
                $"INSERT DATA {{ GRAPH <{named}> {{ <urn:a> <urn:b> <urn:c> }} }}"));

            try
            {
                var models = Store.ListModels().ToList();

                Assert.IsTrue(models.Any(m => m.Uri.ToString() == named.ToString()),
                    "the IRI-named graph must be listed");
                Assert.IsTrue(models.All(m => Uri.IsWellFormedUriString(m.Uri.ToString(), UriKind.Absolute)),
                    "every listed model must be addressable by IRI");

                Assert.Catch(
                    () => Store.ExecuteNonQuery(new SparqlUpdate(
                        "INSERT DATA { GRAPH _:listModelsBlank { <urn:d> <urn:e> <urn:f> } }")),
                    "a blank node is not a legal graph name, and Oxigraph says so rather than "
                    + "inventing a graph nothing can address");
            }
            finally
            {
                Store.GetModel(named).Clear();
            }
        }

        /// <summary>
        /// <c>IsReady</c> must reflect the connection rather than being a hardcoded <c>true</c>,
        /// which is what it was on Fuseki until ADR-0043.
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
        /// There is no dataset or repository to name -- an Oxigraph server holds one dataset -- so
        /// the host is the whole of the configuration and an empty one is refused.
        /// </summary>
        [Test]
        public void ConstructionRequiresAHost()
        {
            Assert.Throws<ArgumentException>(() => new OxigraphStore(null));
            Assert.Throws<ArgumentException>(() => new OxigraphStore(""));
        }

        /// <summary>
        /// The composed endpoint URLs must not grow a double slash from a host that ends in one --
        /// the provider's default host does, a container-mapped host does not.
        /// </summary>
        [Test]
        public void TrailingSlashInTheHostIsNormalized()
        {
            using (var store = new OxigraphStore("http://localhost:7878/"))
            {
                Assert.AreEqual("http://localhost:7878", ((OxigraphStore)store).Hostname);
            }
        }

        /// <summary>
        /// A server behind a path prefix must keep it. Uri resolution drops the last segment of a
        /// base that has no trailing slash, so composing naively would turn
        /// <c>http://host/oxigraph</c> + <c>query</c> into <c>http://host/query</c> -- talking to the
        /// wrong path, or to nothing, without saying so.
        /// </summary>
        [Test]
        public void APathPrefixInTheHostIsPreserved()
        {
            using (var store = new OxigraphStore("http://localhost:7878/oxigraph"))
            {
                Assert.AreEqual("http://localhost:7878/oxigraph", ((OxigraphStore)store).Hostname);
            }
        }
    }
}
