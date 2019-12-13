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

using Mono.Options;
using Semiodesk.Trinity.Configuration;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Semiodesk.Trinity.OntologyGenerator
{
    class Program
    {
        #region Members

        private int _verbosity = 0;

        private IConfiguration _configuration = null;

        private string _generatePath = null;

        private string _configPath = null;

        private FileInfo _configFile = null;

        private DirectoryInfo _sourceDir;

        protected ILogger Logger { get; set; }

        #endregion

        #region Constructors

        private Program(string[] args, ILogger logger)
        {
            Logger = logger;

            bool showHelp = false;

            OptionSet optionSet = new OptionSet()
            {
                { "c|config=", "Path of the config file.", v =>SetConfig(v)  },
                { "g|generate=", "Path of the ontologies.cs file to generate.", v =>SetTarget(v)  },
                { "v", "increase debug message verbosity", v => { if (v != null) ++_verbosity; } },
                { "h|help",  "Show this message and exit", v => showHelp = v != null }
            };

            try
            {
                optionSet.Parse(args);

                if (string.IsNullOrEmpty(_configPath))
                {
                    showHelp = true;
                }
                else
                {
                    LoadLegacyConfigFile();
                    if (_configuration == null)
                        LoadConfigFile();
                    Run();
                }
            }
            catch (OptionException e)
            {
                Logger.LogMessage(e.ToString());

                showHelp = true;
            }

            if (showHelp)
            {
                ShowHelp(optionSet);
            }
        }

        public Program(ILogger logger)
        {
            Logger = logger;
        }

        #endregion

        #region Methods

        [STAThread]
        static int Main(string[] args)
        {
            Program p = new Program(args, new ConsoleLogger());
      
            return 0;
        }

        public int Run()
        {

            if (_configuration == null)
            {
                throw new Exception("Trinity config file not fount. The project does neither have a new style (ontologies.config) or legacy (app.config or web.config) configuration file available.");
            }

            _sourceDir = new FileInfo(_configPath).Directory;

            if (!string.IsNullOrEmpty(_generatePath))
            {
                FileInfo fileInfo = new FileInfo(_generatePath);

                using (OntologyGenerator generator = new OntologyGenerator(_configuration.Namespace))
                {
                    generator.Logger = Logger;


                    foreach (var ontology in _configuration.ListOntologies())
                    {
                        UriRef t = GetPathFromLocation(ontology.Location);

                        if (!generator.ImportOntology(ontology.Uri, t))
                        {
                            FileInfo ontologyFile = new FileInfo(t.LocalPath);

                           
                            Logger.LogWarning(string.Format("Could not read ontology <{0}> from path {1}.", ontology.Uri, ontologyFile.FullName));
                        }

                        if (!generator.AddOntology(ontology.Uri, null, ontology.Prefix))
                        {
                            Logger.LogMessage("Ontology with uri <{0}> or uri <{1}> could not be found in store.", ontology.Uri, null);
                        }
                    }

                    generator.GenerateFile(fileInfo);
      
                }

                return 0;
            }
            else
            {
                return -1;
            }
        }

        public void SetConfig(string configPath)
        {
            Logger.LogMessage("Reading config from {0}", configPath);

            if (string.IsNullOrEmpty(configPath))
                throw new ArgumentException("Configuration path was null or empty.");

            _configPath = configPath;
            _configFile = new FileInfo(configPath);
            if (!_configFile.Exists)
                throw new ArgumentException("Could not read config file from {0}. Reason: File does not exist.", _configPath);

            _sourceDir = _configFile.Directory;
        }

        public void SetConfig(FileInfo configFile)
        {
            Logger.LogMessage("Reading config from {0}", configFile.FullName);
            _configFile = configFile;
            _sourceDir = _configFile.Directory;
            _configPath = configFile.FullName;
        }

        /// <summary>
        /// This method loads the new configuration format (ontologies.config)
        /// </summary>
        /// <returns></returns>
        public bool LoadConfigFile()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration.Configuration));

            using (var stream = _configFile.OpenRead())
            {
                IConfiguration result = (Configuration.Configuration)serializer.Deserialize(stream);
                _configuration = result;
            }

            return true;
        }

        /// <summary>
        /// This method loads the legacy configuration from app.config/web.config
        /// </summary>
        /// <returns></returns>
        public bool LoadLegacyConfigFile()
        {
            if (_configFile.Exists)
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();

                configMap.ExeConfigFilename = _configFile.FullName;

                var cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                try
                {
                    _configuration = (IConfiguration)cfg.GetSection("TrinitySettings");
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not read config file from {0}. Reason: {1}", _configPath, e.Message);
                }

                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
            else
            {
                Logger.LogError("Could not read config file from {0}. Reason: File does not exist.", _configPath);
            }

            return _configuration == null;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return args.Name == "Semiodesk.Trinity" ? Assembly.GetAssembly(typeof(Resource)) : null;
        }

        public void SetTarget(string generate)
        {
            Logger.LogMessage("Generating to {0}", generate);

            _generatePath = generate;
        }

        private void ShowHelp(OptionSet p)
        {
            Logger.LogMessage("Usage: OntologyGenerator.exe [OPTIONS]");
            Logger.LogMessage("Tool for generating and updating ontologies.");
            Logger.LogMessage("");
            Logger.LogMessage("Options:");

            p.WriteOptionDescriptions(Console.Out);
        }

        protected UriRef GetPathFromLocation(string location)
        {
            UriRef result = null;
            if (!string.IsNullOrEmpty(location))
            {
                string sourcePath = location;

                if (Path.IsPathRooted(sourcePath))
                {
                    result = new UriRef(sourcePath);
                }
                else
                {
                    string fullPath = Path.Combine(_sourceDir.FullName, sourcePath);
                    result = new UriRef(fullPath);
                }
            }
            return result;
        }

        #endregion
    }
}
