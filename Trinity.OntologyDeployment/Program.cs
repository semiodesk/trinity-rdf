/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) Semiodesk GmbH 2015

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mono.Options;

namespace Semiodesk.Trinity.OntologyDeployment
{
    class Program
    {

        #region Members
        int _verbosity = 0;
        private string _configPath = null;
        Configuration _config = null;
        string _connectionString = null;
        string[] _args;
        #endregion

        [STAThread]
        static int Main(string[] args)
        {
            Program p = new Program(args);
            return p.Run();
        }

        #region Constructors
        Program(string[] args)
        {
            _args = args;

        }
        #endregion
        #region Methods

        public int Run()
        {
            bool showHelp = false;

            OptionSet o = new OptionSet()
          {
            { "c|config=", "Path of the config file.", v =>SetConfig(v)  },
            { "o|connection=", "Connectionstring for the database.", v=>SetConnection(v) },
            { "v", "increase debug message verbosity", v => { if (v != null) ++_verbosity; } },
            { "h|help",  "Show this message and exit", v => showHelp = v != null }
          };

            try
            {
                o.Parse(_args);
                if (_config == null)
                    showHelp = true;
                else if (string.IsNullOrEmpty(_connectionString))
                {
                    Debug("No connection string given. Nothing to do!");
                    return 0;
                }
                else
                {
                    DirectoryInfo currentDir = null;
                    if (!string.IsNullOrEmpty(_config.BaseDirectory))
                    {
                        currentDir = new DirectoryInfo(_config.BaseDirectory);
                    }
                    else
                    {
                        currentDir = _config.ConfigFile.Directory;
                    }

                    IStore store = Stores.CreateStore(_connectionString);
                    if (store == null)
                    {
                        Debug(string.Format("Could not create store with connectionstring '{0}'", _connectionString));
                        return -1;
                    }

                    OntologyUpdater update = new OntologyUpdater(store, currentDir);
                    update.UpdateOntologies(_config.OntologyCollection);

                    if (_config.VirtuosoSpecific != null)
                    {
                        update.UpdateStorageSpecifics(_config.VirtuosoSpecific);
                    }
                    return 0;
                }
               
            }
            catch (OptionException e)
            {
                Debug(e.ToString());
                showHelp = true;
                return 0;
            }

            if (showHelp)
            {
                ShowHelp(o);
                return 0;
            }
            return -1;
        }

        void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: OntologyDeployment.exe [OPTIONS]");
            Console.WriteLine("Tool for updating ontologies in a triple store.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        void Debug(string format, params object[] args)
        {
            if (_verbosity > 0)
            {
                Console.Write("# ");
                Console.WriteLine(format, args);
            }
        }

        private void SetConfig(string config)
        {
            Debug("Reading config from {0}", config);

            if (string.IsNullOrEmpty(config))
                return;

            FileInfo configFile = new FileInfo(config);
            _configPath = configFile.FullName;
            if (configFile.Exists)
            {

                using (StreamReader reader = new StreamReader(configFile.FullName))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

                    _config = (Configuration)serializer.Deserialize(reader);
                    _config.ConfigFile = configFile;
                    reader.Close();
                }


            }
        }

        private void SetConnection(string connectionString)
        {
            Debug("Setting connection string");

            if (string.IsNullOrEmpty(connectionString))
                return;

            _connectionString = connectionString;
        }

        #endregion
    }
}
