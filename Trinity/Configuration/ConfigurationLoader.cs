
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
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Semiodesk.Trinity.Configuration
{
    /// <summary>
    /// Loads Trinity RDF settings from a XML configuration file.
    /// </summary>
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
