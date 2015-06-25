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
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Semiodesk.Trinity;
using Semiodesk.Trinity.Store;

namespace Semiodesk.Trinity.OntologyDeployment
{
    public class VirtuosoSpecific : IStorageSpecific
    {
        [XmlElement(ElementName = "RuleSet")]
        public List<RuleSet> RuleSets { get; set; }



        #region IStorageSpecific Members

        public void Update(IStore store)
        {
            if (store is VirtuosoStore)
            {
                VirtuosoStore virtuosoStore = (store as VirtuosoStore);
                
                foreach (RuleSet set in RuleSets)
                {
                    ClearRuleSet(set.RuleSetUri, virtuosoStore);
                    foreach (var item in set.Graphs)
                    {
                        AddGraphToRuleSet(set.RuleSetUri, item, virtuosoStore);
                    }
                }
            }
        }

        private void ClearRuleSet(Uri ruleSet, VirtuosoStore store)
        {
            try
            {
                string query = string.Format("delete * from DB.DBA.SYS_RDF_SCHEMA where RS_NAME='{0}';", ruleSet.OriginalString);
                store.ExecuteQuery(query);
            }catch(Exception)
            {
            }
        }

        private void RemoveGraphFromRuleSet(Uri ruleSet, Uri graph, VirtuosoStore store)
        {
            try
            {
                string query = string.Format("rdfs_rule_set ('{0}', '{1}', 1)", ruleSet, graph);
                store.ExecuteQuery(query);
            }
            catch (Exception)
            {

            }
        }

        private void AddGraphToRuleSet(Uri ruleSet, Uri graph, VirtuosoStore store)
        {

            string query = string.Format("rdfs_rule_set ('{0}', '{1}')", ruleSet, graph);
            store.ExecuteQuery(query);
        }

        #endregion
    }

    public class RuleSet
    {

        [XmlIgnore]
        public List<UriRef> Graphs
        {
            get
            {
                return graphs.Select<string, UriRef>(x => new UriRef(x)).ToList();
            }
            set
            {
                graphs = value.Select<UriRef, string>(x => x.OriginalString).ToList();
            }
        }

        [XmlElement(ElementName = "Graph")]
        public List<string> graphs { get; set; }

        [XmlIgnore]
        public UriRef RuleSetUri
        {
            get
            {
                if (string.IsNullOrEmpty(ruleSet))
                    return null;
                return new UriRef(ruleSet);
            }
            set
            {
                ruleSet = value.OriginalString;
            }
        }

        [XmlAttribute("Uri")]
        public string ruleSet { get; set; }

        public RuleSet()
        {
            graphs = new List<string>();
        }
    }
}
