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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Semiodesk.Trinity.Collections
{
    public class VirtualizingCollection<T> : IList<T>, IList 
    {
        #region Members
        
        protected IModel Model;

        private readonly IItemsProvider<T> _itemsProvider;

        private readonly Dictionary<int, IList<T>> _pages = new Dictionary<int, IList<T>>();

        private readonly Dictionary<int, DateTime> _pageTouchTimes = new Dictionary<int, DateTime>();

        protected int _pageSize = 25;

        protected long _pageTimeout = 600000;

        private int _count = -1;

        public int PageSize
        {
            get { return _pageSize; }
        }

        public long PageTimeout
        {
            get { return _pageTimeout; }
        }

        public virtual int Count
        {
            get
            {
                if (_count == -1)
                {
                    LoadCount();
                }

                return _count;
            }
            protected set
            {
                _count = value;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </returns>
        public object SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>Always false.
        /// </returns>
        public bool IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>Always true.
        /// </returns>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>Always false.
        /// </returns>
        public bool IsFixedSize
        {
            get { return false; }
        }

        public T this[int index]
        {
            get
            {
                // determine which page and offset within page
                int pageIndex = index / PageSize;
                int pageOffset = index % PageSize;

                // request primary page
                RequestPage(pageIndex);

                // if accessing upper 50% then request next page
                if (pageOffset > PageSize / 2 && pageIndex < Count / PageSize)
                {
                    RequestPage(pageIndex + 1);
                }

                // if accessing lower 50% then request prev page
                if (pageOffset < PageSize / 2 && pageIndex > 0)
                {
                    RequestPage(pageIndex - 1);
                }

                // remove stale pages
                CleanUpPages();

                // defensive check in case of async load
                if (_pages[pageIndex] == null)
                {
                    return default(T);
                }

                // return requested item
                return _pages[pageIndex][pageOffset];
            }
            set { throw new NotSupportedException(); }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">Items provider</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize, int pageTimeout)
        {
            _itemsProvider = itemsProvider;
            _pageSize = pageSize;
            _pageTimeout = pageTimeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
        {
            _itemsProvider = itemsProvider;
            _pageSize = pageSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingCollection{T}"/> class.
        /// </summary>
        public VirtualizingCollection(IItemsProvider<T> itemsProvider)
        {
            _itemsProvider = itemsProvider;
        }

        #endregion

        #region Methods

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item) { throw new NotSupportedException(); }

        int IList.Add(object value) { throw new NotSupportedException(); }

        bool IList.Contains(object value) { throw new NotSupportedException(); }

        public bool Contains(T item) { throw new NotSupportedException(); }

        public void Clear() { throw new NotSupportedException(); }

        int IList.IndexOf(object value) 
        {
            return IndexOf((T)value);
        }

        public int IndexOf(T item)
        {
            int index = -1;

            foreach (var page in _pages)
            {
                index = page.Value.IndexOf(item);

                if (index != -1)
                {
                    continue;
                }
            }

            return index;
        }

        public void Insert(int index, T item) { throw new NotSupportedException(); }

        void IList.Insert(int index, object value) { throw new NotSupportedException(); }

        public void RemoveAt(int index) { throw new NotSupportedException(); }

        void IList.Remove(object value) { throw new NotSupportedException(); }

        public bool Remove(T item) { throw new NotSupportedException(); }

        public void CopyTo(T[] array, int arrayIndex) { throw new NotSupportedException(); }

        void ICollection.CopyTo(Array array, int index) { throw new NotSupportedException(); }

        public void CleanUpPages()
        {
            List<int> keys = new List<int>(_pageTouchTimes.Keys);
            foreach (int key in keys)
            {
                // page 0 is a special case, since WPF ItemsControl access the first item frequently
                if (key != 0 && (DateTime.Now - _pageTouchTimes[key]).TotalMilliseconds > PageTimeout)
                {
                    _pages.Remove(key);
                    _pageTouchTimes.Remove(key);
                    Trace.WriteLine("Removed Page: " + key);
                }
            }
        }

        protected virtual void LoadPage(int pageIndex)
        {
            PopulatePage(pageIndex, FetchPage(pageIndex));
        }

        protected virtual void PopulatePage(int pageIndex, IList<T> page)
        {
            Trace.WriteLine("Debug: populated page " + pageIndex);
            if (_pages.ContainsKey(pageIndex))
                _pages[pageIndex] = page;
        }

        protected virtual void RequestPage(int pageIndex)
        {
            if (!_pages.ContainsKey(pageIndex))
            {
                _pages.Add(pageIndex, null);
                _pageTouchTimes.Add(pageIndex, DateTime.Now);
                Trace.WriteLine("Debug: added page " + pageIndex);
                LoadPage(pageIndex);
            }
            else
            {
                _pageTouchTimes[pageIndex] = DateTime.Now;
            }
        }

        protected virtual void LoadCount()
        {
            Count = FetchCount();
        }

        protected IList<T> FetchPage(int pageIndex)
        {
            IList<T> page = new List<T>();

            foreach (T item in _itemsProvider.GetItems(pageIndex * PageSize, PageSize))
            {
                page.Add(item);
            }

            return page;
        }

        protected int FetchCount()
        {
            return _itemsProvider.Count();
        }
        #endregion
    }
}