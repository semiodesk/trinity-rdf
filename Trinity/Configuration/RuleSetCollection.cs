using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
{
    public sealed class RuleSetCollection : ConfigurationElementCollection, IEnumerable<RuleSet>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new RuleSet();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RuleSet)element).Uri;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "RuleSet"; }
        }

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

        new public RuleSet this[string employeeID]
        {
            get { return (RuleSet)BaseGet(employeeID); }
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
