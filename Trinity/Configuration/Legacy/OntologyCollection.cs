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
    /// A collection of ontology settings.
    /// </summary>
    public sealed class OntologyCollection : ConfigurationElementCollection, IEnumerable<Ontology>
    {
        /// <summary>
        /// Create a new ontology configuration element.
        /// </summary>
        /// <returns>A new configuration element.</returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new Ontology();
        }

        /// <summary>
        /// Get the key associated with a configuration element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>URI of the configuration element.</returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Ontology)element).UriString;
        }

        /// <summary>
        /// Get the configuration element collection type.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        /// <summary>
        /// XML element tag name.
        /// </summary>
        protected override string ElementName
        {
            get { return "Ontology"; }
        }

        /// <summary>
        /// Gets the configuration element at the specified index location.
        /// </summary>
        /// <param name="index">The index location of the System.Configuration.ConfigurationElement to return.</param>
        /// <returns>The <c>System.Configuration.ConfigurationElement</c> at the specified index.</returns>
        public Ontology this[int index]
        {
            get { return (Ontology)BaseGet(index); }
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
        /// Gets the configuration element with the specified identifier.
        /// </summary>
        /// <param name="key">The identifier of the System.Configuration.ConfigurationElement to return.</param>
        /// <returns>The <c>System.Configuration.ConfigurationElement</c> at the specified index.</returns>
        new public Ontology this[string key]
        {
            get { return (Ontology)BaseGet(key); }
        }

        /// <summary>
        /// Indicates if an element with the given key exists in this collection.
        /// </summary>
        /// <param name="key">The identifier to be checked.</param>
        /// <returns><c>true</c> if an element with the given key exists, <c>false</c> otherwise.</returns>
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

        #region IEnumerable<Ontology> Members

        /// <summary>
        /// Get an enumerator for iterating over the items in this collection.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public new IEnumerator<Ontology> GetEnumerator()
        {
            foreach (var k in BaseGetAllKeys())
            {
                yield return (Ontology)BaseGet(k);
            }
        }

        #endregion
    }
}
