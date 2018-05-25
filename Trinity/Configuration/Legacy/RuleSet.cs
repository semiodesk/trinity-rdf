using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration.Legacy
{
    /// <summary>
    /// A ruleset element
    /// </summary>
    public class RuleSet : ConfigurationElement
    {
        /// <summary>
        /// The uri of the rule set.
        /// </summary>
        [ConfigurationProperty("Uri", IsRequired = true)]
        public string Uri
        {
            get { return (string)base["Uri"]; }
            set { base["Uri"] = value; }
        }

        /// <summary>
        /// A collection of graphs contained in this rule set.
        /// </summary>
        [ConfigurationProperty("Graphs", IsDefaultCollection = true)]
        public GraphCollection Graphs
        {
            get { return (GraphCollection)base["Graphs"]; }
        }
    }
}
