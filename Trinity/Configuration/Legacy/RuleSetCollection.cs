using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration.Legacy
{
    /// <summary>
    /// Collection of rule sets
    /// </summary>
    public sealed class RuleSetCollection : ConfigurationElementCollection, IEnumerable<RuleSet>
    {
        /// <summary>
        /// Creates a new RuleSet element
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
        /// The collection type
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        /// <summary>
        /// Contains the name of the element
        /// </summary>
        protected override string ElementName
        {
            get { return "RuleSet"; }
        }

        /// <summary>
        /// The index operator
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
        /// Index operator with key name
        /// </summary>
        /// <param name="employeeID"></param>
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
