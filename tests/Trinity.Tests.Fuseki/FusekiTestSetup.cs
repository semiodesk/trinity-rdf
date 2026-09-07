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

using Semiodesk.Trinity.Store.Fuseki;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Fuseki
{
    // These tests were created with Apache Jena Fuseki 4.0.0.

    // The Fuseki server is started automatically for this assembly by the FusekiContainer
    // [SetUpFixture] via Testcontainers (Docker), which also creates the 'ds' in-memory dataset.
    // No manually-provisioned server is required — just a running Docker daemon (ADR-0036).
    public class FusekiTestSetup : IStoreTestSetup
    {
        #region Members

        public UriRef BaseUri => new UriRef("http://localhost:3030/ds/");

        // Points at the Dockerized Fuseki started by the FusekiContainer [SetUpFixture] on a
        // random host port, with the 'ds' dataset already created (ADR-0036).
        public string ConnectionString => FusekiContainer.ConnectionString;

        #endregion

        #region Methods

        public void LoadProvider()
        {
            StoreFactory.LoadProvider<FusekiStoreProvider>();
        }

        #endregion
    }
}
