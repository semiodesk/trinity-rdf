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
using NUnit.Framework;
using Semiodesk.Trinity.Store.Fuseki;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Fuseki
{
    /// <summary>
    /// Store-level tests for the Fuseki adapter -- the members the shared fixtures do not reach
    /// because they take the store as given.
    /// </summary>
    [TestFixture]
    public class FusekiStoreTest : StoreTest<FusekiTestSetup>
    {
        /// <summary>
        /// <c>ContainsModel</c> had no test on any store but the in-memory one, which is how it
        /// went unnoticed that the Fuseki override asked the server and then threw the answer away,
        /// returning <c>false</c> unconditionally.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        [Test]
        public void ContainsModelTest()
        {
            var absent = BaseUri.GetUriRef("no-such-model");

            Assert.IsFalse(Store.ContainsModel(absent));

            var present = BaseUri.GetUriRef("contains-model-test");
            var model = Store.GetModel(present);

            model.Clear();

            var resource = model.CreateResource(BaseUri.GetUriRef("r1"));
            resource.AddProperty(new Property(BaseUri.GetUriRef("p1")), "a value");
            resource.Commit();

            Assert.IsTrue(Store.ContainsModel(present),
                "a graph that holds a triple must be reported as present");
            Assert.IsFalse(Store.ContainsModel(absent),
                "an unrelated graph must still be reported as absent");

            model.Clear();
        }

        /// <summary>
        /// The obsolete overload has to agree with the one it delegates to, and must not throw on
        /// null the way the deleted Fuseki override would have.
        /// </summary>
        [Test]
        public void ContainsModelRejectsNullRatherThanThrowing()
        {
            Assert.IsFalse(Store.ContainsModel((Uri)null));
            Assert.IsFalse(Store.ContainsModel((IModel)null));
        }
#pragma warning restore CS0618 // Type or member is obsolete

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
