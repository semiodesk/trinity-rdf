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

using System.Configuration;
using System.Text;
using System.Xml.Linq;

namespace Semiodesk.Trinity.Configuration.Legacy
{
    /// <summary>
    /// Constains Virtuoso specific settings.
    /// </summary>
    public class VirtuosoStoreSettings : ConfigurationElement, IStoreConfiguration
    {
        /// <summary>
        /// A collection of inference rule sets.
        /// </summary>
        [ConfigurationProperty("RuleSets", IsDefaultCollection = true)]
        public RuleSetCollection RuleSets
        {
            get { return (RuleSetCollection)base["RuleSets"]; }
        }

        /// <summary>
        /// Get the triple store type identifier.
        /// </summary>
        public string Type
        {
            get { return "virtuoso"; }
        }

        /// <summary>
        /// Get store specific XML configuration data.
        /// </summary>
        public XElement Data
        {
            get 
            {
                StringBuilder content = new StringBuilder();
                content.Append("<rulesets>");
                foreach (var set in RuleSets)
                {
                    content.AppendFormat("<ruleset uri=\"{0}\">", set.Uri);
                    foreach( var graph in set.Graphs )
                        content.AppendFormat("<graph uri=\"{0}\"/>", graph.Uri);

                    content.Append("</ruleset>");
                }
                content.Append("</rulesets>");
                return XElement.Parse(content.ToString());
            }
        }
    }
}
