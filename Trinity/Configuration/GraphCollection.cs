using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
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
