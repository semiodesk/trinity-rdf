
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

using System.Collections.Generic;
using System.Configuration;

namespace Semiodesk.Trinity.Configuration.Legacy
{
    /// <summary>
    /// A collection containing all graphs in the configuration
    /// </summary>
    public sealed class GraphCollection : ConfigurationElementCollection, IEnumerable<Graph>
    {
        /// <summary>
        /// Create a new graph element
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new Graph();
        }

        /// <summary>
        /// Gets the key of the given graph element. Uri is used.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Graph)element).Uri;
        }

        /// <summary>
        /// The type of the collection
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        /// <summary>
        /// The name of the element
        /// </summary>
        protected override string ElementName
        {
            get { return "Graph"; }
        }

        /// <summary>
        /// The index operator
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Graph this[int index]
        {
            get { return (Graph)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        /// <summary>
        /// Get the element by the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        new public Graph this[string key]
        {
            get { return (Graph)BaseGet(key); }
        }

        /// <summary>
        /// Test if key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            bool result = false;
            object[] keys = BaseGetAllKeys();
            foreach (object obj in keys)
            {
                if ((string)obj == key)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        #region IEnumerable<Graph> Members

        /// <summary>
        /// Get enumerator of collection
        /// </summary>
        /// <returns></returns>
        public new IEnumerator<Graph> GetEnumerator()
        {
            foreach (var k in BaseGetAllKeys())
            {
                yield return (Graph)BaseGet(k);
            }
        }

        #endregion
    }
}
