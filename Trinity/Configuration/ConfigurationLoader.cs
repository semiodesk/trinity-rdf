
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Semiodesk.Trinity.Configuration
{
    class ConfigurationLoader
    {


        #region Constructor
        public static OntologyConfiguration LoadConfiguration(FileInfo configFile)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(OntologyConfiguration));

            using (var stream = configFile.OpenRead())
            {
                OntologyConfiguration result = (OntologyConfiguration)serializer.Deserialize(stream);
                return result;
            }
        }
        #endregion

    }
}
