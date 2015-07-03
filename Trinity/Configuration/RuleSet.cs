using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
{
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
}
