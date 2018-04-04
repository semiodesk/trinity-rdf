
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Semiodesk.Trinity.Configuration
{
    internal class ConfigurationLoader
    {
        #region Constructor
        public static IConfiguration LoadConfiguration(FileInfo configFile)
        {
            // No config file specified, loading default
            if( configFile == null )
            {
                // Loading legacy settings
                TrinitySettings settings = (TrinitySettings)ConfigurationManager.GetSection("TrinitySettings");
                if (settings != null)
                    return settings;

                // Legacy settings not available, set new default
                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ontologies.config");
                configFile = new FileInfo(path);
            }
            
            if( !configFile.Exists )
                return null;

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

            using (var stream = configFile.OpenRead())
            {
                Configuration result = (Configuration)serializer.Deserialize(stream);
                return result;
            }
            

        }
        #endregion

    }
}
