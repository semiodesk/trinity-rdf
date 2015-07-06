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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mono.Options;
using Semiodesk.Trinity.Configuration;

namespace Semiodesk.Trinity.OntologyDeployment
{
    class Program
    {

        #region Members
        int _verbosity = 0;
        private string _configPath = null;
        System.Configuration.Configuration _configuration = null;
        TrinitySettings _config = null;
        string _connectionString = null;
        string[] _args;
        ILogger Logger { get; set; }
        bool _initalized = false;

        public bool Initialized { get { return _initalized; } }

        DirectoryInfo BaseDirectory { get; set; }
        #endregion

        [STAThread]
        static int Main(string[] args)
        {
            Program p = new Program(args);
            return p.Run();
        }

        #region Constructors

        public Program(ILogger logger)
        {
            Logger = logger;
        }

        Program(string[] args)
        {
            Logger = new ConsoleLogger();

            _args = args;
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

                if (!LoadConfigFile(_configPath))
                {
                    ShowHelp(o);
                    return;
                }

            }
            catch (OptionException e)
            {
                Debug(e.ToString());
                showHelp = true;
                return;
            }

            if (showHelp)
            {
                ShowHelp(o);
                return;
            }
            return;

        }
        #endregion

        #region Methods

        public int Run()
        {
            if( !_initalized )
                return -1;


            IStore store = StoreFactory.CreateStore(_connectionString);
            if (store == null)
            {
                Logger.LogError(string.Format("Could not create store with connectionstring '{0}'", _connectionString));
                return -1;
            }

            OntologyUpdater update = new OntologyUpdater(store, BaseDirectory);
            update.Logger = Logger;
            update.UpdateOntologies(_config.Ontologies);

            if (_config.VirtuosoStoreSettings != null)
            {
                IStorageSpecific spec = new VirtuosoSettings(_config.VirtuosoStoreSettings);
                update.UpdateStorageSpecifics(spec);
            }
            return 0;
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

        protected void SetConfig(string config)
        {
            Logger.LogMessage("Reading config from {0}", config);

            if (string.IsNullOrEmpty(config))
                return;

            FileInfo configFile = new FileInfo(config);
            _configPath = configFile.FullName;

        }

        public bool LoadConfigFile(string configPath)
        {
            _configPath = configPath;
            FileInfo configFile = new FileInfo(configPath);
            BaseDirectory = configFile.Directory;
            if (configFile.Exists)
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();

                configMap.ExeConfigFilename = configFile.FullName;

                _configuration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                try
                {
                    _config = (TrinitySettings)_configuration.GetSection("TrinitySettings");
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not read config file from {0}. Reason: {1}", configPath, e.Message);
                    return false;
                }
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

                // Test if connection string was set before
                if( string.IsNullOrEmpty(_connectionString ))
                {
                    // It was not, so we set it from the configuration
                    var conStrings = _config.CurrentConfiguration.ConnectionStrings.ConnectionStrings;
                    foreach( ConnectionStringSettings setting in conStrings)
                    {
                        string conString = setting.ConnectionString;
                        if (setting.ProviderName == "Semiodesk.Trinity" && StoreFactory.TestConnectionString(conString))
                            _connectionString = conString;
                    }
                }

                if (string.IsNullOrEmpty(_connectionString))
                {
                    Logger.LogWarning("No connection string given. Nothing to do!");
                    return true;
                }
            }
            else
            {
                Logger.LogError("Could not read config file from {0}. Reason: File does not exist.", configPath);
            }

            
            _initalized = (_config != null);
            return _initalized;
        }


        private void SetConnection(string connectionString)
        {
            Debug("Setting connection string");

            if (string.IsNullOrEmpty(connectionString))
                return;

            _connectionString = connectionString;
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name == "Semiodesk.Trinity")
                return Assembly.GetAssembly(typeof(Resource));
            return null;
        }

        #endregion
    }
}
