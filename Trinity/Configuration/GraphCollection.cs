using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
{
    public sealed class GraphCollection : ConfigurationElementCollection, IEnumerable<Graph>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Graph();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Graph)element).Uri;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "Graph"; }
        }

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

        new public Graph this[string employeeID]
        {
            get { return (Graph)BaseGet(employeeID); }
        }

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
