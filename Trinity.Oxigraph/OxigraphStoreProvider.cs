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

using System.Collections.Generic;
using System.Composition;

namespace Semiodesk.Trinity.Store.Oxigraph
{
    /// <summary>
    /// Creates an <see cref="OxigraphStore"/> from a connection string.
    /// </summary>
    /// <remarks>
    /// Registered by the caller with <c>StoreFactory.LoadProvider&lt;OxigraphStoreProvider&gt;()</c>;
    /// the <c>[Export]</c> attribute is vestigial, as nothing in the solution composes MEF.
    ///
    /// Connection string: <c>provider=oxigraph;host=http://localhost:7878/</c>, with optional
    /// <c>uid</c> and <c>pw</c>. There is no dataset or repository key -- an Oxigraph server holds
    /// one dataset, so the host names the store completely.
    /// </remarks>
    [Export(typeof(StoreProvider))]
    public class OxigraphStoreProvider : StoreProvider
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of the provider.
        /// </summary>
        public OxigraphStoreProvider()
        {
            // Lower case, and matched ordinally by StoreFactory's registry: dotNetRDFStoreProvider
            // registers under "dotnetrdf" while naming itself "dotNetRDF", so provider=dotNetRDF
            // does not resolve. The other backends all use a lower-case name for that reason.
            Name = "oxigraph";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a store from the given connection string parameters.
        /// </summary>
        /// <param name="config">Parsed connection string.</param>
        /// <returns>A store handle, or <c>null</c> if no host was given.</returns>
        public override IStore GetStore(Dictionary<string, string> config)
        {
            var host = GetValue(config, "host", "http://localhost:7878/");

            if (string.IsNullOrEmpty(host))
            {
                return null;
            }

            var username = GetValue(config, "uid");
            var password = GetValue(config, "pw");

            return new OxigraphStore(host, username, password);
        }

        private string GetValue(Dictionary<string, string> config, string key, string defaultValue = null)
        {
            return config.ContainsKey(key) ? config[key] : defaultValue;
        }

        #endregion
    }
}
