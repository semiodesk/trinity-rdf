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
// Copyright (c) Semiodesk GmbH 2015-2019

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This is the abstract store provider class. Implement it if you want to write your own store provider.
    /// </summary>
    public abstract class StoreProvider
    {
        /// <summary>
        /// The name of the store.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// All valid configuration options
        /// </summary>
        protected string[] ConfigurationOptions { get; set; }

        /// <summary>
        /// The constructor of the store provider
        /// </summary>
        public StoreProvider()
        {

        }

        /// <summary>
        /// The GetStore method which will be called with the parsed configuration string.
        /// </summary>
        /// <param name="configurationDictionary">Store specific configuation parameters.</param>
        /// <returns>An instance of <c>IStore</c>.</returns>
        public abstract IStore GetStore(Dictionary<string, string> configurationDictionary );


    }
}
