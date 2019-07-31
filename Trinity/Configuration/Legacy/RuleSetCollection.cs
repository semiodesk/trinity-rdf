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
    /// Collection of rule sets.
    /// </summary>
    public sealed class RuleSetCollection : ConfigurationElementCollection, IEnumerable<RuleSet>
    {
        /// <summary>
        /// Create a new rule set element.
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new RuleSet();
        }

        /// <summary>
        /// Gets the key (the uri) of a RuleSet element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RuleSet)element).Uri;
        }

        /// <summary>
        /// The collection type.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        /// <summary>
        /// Contains the name of the element.
        /// </summary>
        protected override string ElementName
        {
            get { return "RuleSet"; }
        }

        /// <summary>
        /// The index operator.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public RuleSet this[int index]
        {
            get { return (RuleSet)BaseGet(index); }
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
        /// Index operator with key name.
        /// </summary>
        /// <param name="key">The key of the ruleset.</param>
        /// <returns></returns>
        new public RuleSet this[string key]
        {
            get { return (RuleSet)BaseGet(key); }
        }

        /// <summary>
        /// Can be used to test if the key exists.
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
        /// Enumerator for the collection.
        /// </summary>
        /// <returns></returns>
        public new IEnumerator<RuleSet> GetEnumerator()
        {
            foreach (var k in BaseGetAllKeys())
            {
                yield return (RuleSet)BaseGet(k);
            }
        }

        #endregion
    }
}
