
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Semiodesk.Trinity.Configuration
{
    internal class ConfigurationLoader
    {
        #region Methods

        public static IConfiguration LoadConfiguration(FileInfo configFile)
        {
            // No config file specified, loading default
            if (configFile == null)
            {
                // Loading legacy settings
                TrinitySettings settings = (TrinitySettings)ConfigurationManager.GetSection("TrinitySettings");

                if (settings != null)
                {
                    return settings;
                }

                // Legacy settings not available, set new default
                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ontologies.config");

                configFile = new FileInfo(path);
            }

            if (!configFile.Exists)
            {
                throw new FileNotFoundException(configFile.FullName);
            }

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
