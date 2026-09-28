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

using NUnit.Framework;
using Semiodesk.Trinity.Store.Oxigraph;

namespace Semiodesk.Trinity.Tests.Oxigraph
{
    /// <summary>
    /// The connection string resolves to the Oxigraph store -- the one thing the shared store
    /// fixtures cannot cover, because they take the store as given.
    /// </summary>
    [TestFixture]
    public class StoreProviderTest
    {
        [Test]
        public void ConnectionStringResolvesToAnOxigraphStore()
        {
            StoreFactory.LoadProvider<OxigraphStoreProvider>();

            using (var store = StoreFactory.CreateStore(OxigraphContainer.ConnectionString))
            {
                Assert.IsNotNull(store);
                Assert.IsInstanceOf<OxigraphStore>(store);
                Assert.IsTrue(store.IsReady);
            }
        }
    }
}
