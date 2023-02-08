﻿// LICENSE:
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
// Copyright (c) Semiodesk GmbH 2015-2019

using System.Collections.Generic;
#if NETSTANDARD2_0
using System.Composition;
#elif !NET35
using System.ComponentModel.Composition;
#endif

namespace Semiodesk.Trinity.Store
{
    /// <summary>
    /// A store provider for dotNetRDF triple store adapters.
    /// </summary>
#if !NET35
    [Export(typeof(StoreProvider))]
#endif
    public class dotNetRDFStoreProvider : StoreProvider
    {
        #region Constructor

        /// <summary>
        /// Create a new instance of the <c>dotNetRDFStoreProvider</c> class.
        /// </summary>
        public dotNetRDFStoreProvider()
        {
            Name = "dotNetRDF";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a new triple store with the given settings.
        /// </summary>
        /// <param name="config">Triple store setting variables.</param>
        /// <returns></returns>
        public override IStore GetStore(Dictionary<string, string> config)
        {
            string schemaKey = "schema";
            string[] schema = null;

            if (config.ContainsKey(schemaKey))
            {
                schema = config[schemaKey].Split(',');
            }

            return new dotNetRDFStore(schema);
        }

        #endregion
    }
}
