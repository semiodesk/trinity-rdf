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

Copyright (c) 2015 Semiodesk GmbH

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Semiodesk.Trinity
{
    public class VirtualizingResourceQueryCollection<T> : VirtualizingCollectionBase<T> where T : Resource
    {
        #region Members

        private readonly IResourceQueryResult _result;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingResourceQueryCollectionResourceQueryCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public VirtualizingResourceQueryCollection(IModel model, ResourceQuery query, int pageSize, int pageTimeout)
        {
            Model = model;
            _result = model.ExecuteQuery(query, true);
            _pageSize = pageSize;
            _pageTimeout = pageTimeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingResourceQueryCollectionResourceQueryCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        public VirtualizingResourceQueryCollection(IModel model, ResourceQuery query, int pageSize)
        {
            Model = model;
            _result = model.ExecuteQuery(query, true);
            _pageSize = pageSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingResourceQueryCollectionResourceQueryCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        public VirtualizingResourceQueryCollection(IModel model, ResourceQuery query, bool inferenceEnabled = false)
        {
            Model = model;
            _result = model.ExecuteQuery(query, inferenceEnabled);
        }

        #endregion

        #region Methods

        protected override IList<T> FetchPage(int pageIndex)
        {
            IList<T> page = new List<T>();
            
            foreach(T item in _result.GetResources<T>(pageIndex * PageSize, PageSize))
            {
                page.Add(item);
            }

            return page;
        }

        protected override int FetchCount()
        {
            return _result.Count();
        }

        #endregion
    }
}
