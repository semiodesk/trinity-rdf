using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration.Legacy
{
    /// <summary>
    /// A graph element in the configuration.
    /// </summary>
    public class Graph : ConfigurationElement
    {
        /// <summary>
        /// The Uri of the graph element
        /// </summary>
        [ConfigurationProperty("Uri", IsRequired = true)]
        public string Uri
        {
            get { return (string)base["Uri"]; }
            set { base["Uri"] = value; }
        }

    }

       
}
