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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A simple SPARQL Update class. This class aims to ease formulating SPARQL
    /// query strings by automatically setting PREFIX declarations from a given
    /// namespace manager.
    /// </summary>
    public class SparqlUpdate
    {
        #region Properties

        /// <summary>
        /// The SPARQL Update string including all generated PREFIX declarations.
        /// </summary>
        public string Update { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public Resource Resource { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IModel Model { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Create a new SPARQL Update with an optional namespace manager instance which
        /// can be used to declare PREFIX declarations for the namespace abbreviations
        /// used in the update string.
        /// </summary>
        /// <param name="update">The u update string.</param>
        /// <param name="namespaceManager">The optional namespace manager used to declare Sparql PREFIXes.</param>
        public SparqlUpdate(string update, NamespaceManager namespaceManager = null)
        {
            if (namespaceManager != null)
            {
                Update = SparqlSerializer.GeneratePrologue(update, namespaceManager);
            }
            else
            {
                Update = update;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Update;
        }

        #endregion
    }
}
