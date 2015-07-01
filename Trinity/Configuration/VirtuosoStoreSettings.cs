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
// Copyright (c) Semiodesk GmbH 2015


using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
{
    
    public class VirtuosoStoreSettings : ConfigurationElement
    {
        [ConfigurationProperty("RuleSet", IsRequired = true)]
        public RuleSet RuleSet
        {
            get { return (RuleSet)base["RuleSet"]; }
            set { base["RuleSet"] = value; }
        }
    }

       public class RuleSet : ConfigurationElement
       {
           [ConfigurationProperty("Uri", IsRequired = true)]
           public string Uri
           {
               get { return (string)base["Uri"]; }
               set { base["Uri"] = value; }
           }

           [ConfigurationProperty("Graphs", IsDefaultCollection = true)]
           public GraphCollection Graphs
           {
               get { return (GraphCollection)base["Graphs"]; }
           }
       }

       public class Graph : ConfigurationElement
       {
           [ConfigurationProperty("Uri", IsRequired = true)]
           public string Uri
           {
               get { return (string)base["Uri"]; }
               set { base["Uri"] = value; }
           }

       }

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
