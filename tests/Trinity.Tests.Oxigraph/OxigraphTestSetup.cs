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

using Semiodesk.Trinity.Store.Oxigraph;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Oxigraph
{
    /// <summary>
    /// Binds the shared store fixtures to the Dockerized Oxigraph server.
    /// </summary>
    /// <remarks>
    /// The server is started for this assembly by the <see cref="OxigraphContainer"/>
    /// <c>[SetUpFixture]</c> via Testcontainers (Docker). Nothing needs provisioning afterwards --
    /// an Oxigraph server holds exactly one dataset, so there is no repository or dataset to create
    /// and no <c>AfterSeed</c> hook to implement.
    /// </remarks>
    public class OxigraphTestSetup : IStoreTestSetup
    {
        #region Members

        // A fixed identifier, deliberately not tracking the mapped container port: this names
        // graphs, it does not address the server (ADR-0036).
        public UriRef BaseUri => new UriRef("http://localhost:7878/graph/trinity-rdf/");

        public string ConnectionString => OxigraphContainer.ConnectionString;

        #endregion

        #region Methods

        public void LoadProvider()
        {
            StoreFactory.LoadProvider<OxigraphStoreProvider>();
        }

        #endregion
    }
}
