/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) Semiodesk GmbH 2015

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Collections
{
    #if !NET_3_5
    public class AsyncVirtualizingResourceCollection<T> : AsyncVirtualizingCollection<T> where T : Resource
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingSparqlCollection{T}"/> class.
        /// </summary>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public AsyncVirtualizingResourceCollection(IModel model, ResourceQuery query, int pageSize, int pageTimeout, bool inferenceEnabled = true) 
            : base(new ResourceQueryItemsProvider<T>(model, query, inferenceEnabled), pageSize, pageTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingSparqlCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        public AsyncVirtualizingResourceCollection(IModel model, ResourceQuery query, int pageSize, bool inferenceEnabled = true)
            : base(new ResourceQueryItemsProvider<T>(model, query, inferenceEnabled), pageSize)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingSparqlCollection{T}"/> class.
        /// </summary>
        public AsyncVirtualizingResourceCollection(IModel model, ResourceQuery query, bool inferenceEnabled = false)
            : base(new ResourceQueryItemsProvider<T>(model, query, inferenceEnabled))
        {
        }

        #endregion
    }
#endif
}
