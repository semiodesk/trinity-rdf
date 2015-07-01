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
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using Mono.Options;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using System.Reflection;
using Semiodesk.Trinity.Configuration;

namespace Semiodesk.Trinity.OntologyGenerator
{
    class Program
    {
        #region Members
        string _generatePath = null;
        int _verbosity = 0;
        private string _configPath = null;
        System.Configuration.Configuration _configuration = null;
        TrinitySettings _config = null;
        private DirectoryInfo _sourceDir;
        ILogger Logger { get; set; }
        #endregion

        [STAThread]
        static int Main(string[] args)
        {

            Program p = new Program(args, new ConsoleLogger());

            return 0;
        }

        #region Constructors
        Program(string[] args, ILogger logger)
        {
            Logger = logger;
            bool showHelp = false;

            OptionSet o = new OptionSet()
          {
            { "c|config=", "Path of the config file.", v =>SetConfig(v)  },
            { "g|generate=", "Path of the ontologies.cs file to generate.", v =>SetGenerate(v)  },
            { "v", "increase debug message verbosity", v => { if (v != null) ++_verbosity; } },
            { "h|help",  "Show this message and exit", v => showHelp = v != null }
          };

            try
            {
                o.Parse(args);
                if (string.IsNullOrEmpty(_configPath))
                    showHelp = true;
                else
                {
                    LoadConfigFile(_configPath);
                    Run();
                }

            }
            catch (OptionException e)
            {
                Logger.LogMessage(e.ToString());
                showHelp = true;
                return;
            }

            if (showHelp)
            {
                ShowHelp(o);
                return;
            }

        }

        public Program(ILogger logger)
        {
            Logger = logger;
        }

        #endregion

        #region Methods
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
                }
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

            }
            else
            {
                Logger.LogError("Could not read config file from {0}. Reason: File does not exist.", configPath);
            }
            return _config == null;
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name == "Semiodesk.Trinity")
                return Assembly.GetAssembly(typeof(Resource));
            return null;
        }

        public void SetGenerate(string generate)
        {
            Logger.LogMessage("Generating to {0}", generate);
            _generatePath = generate;
        }

        void ShowHelp(OptionSet p)
        {
            Logger.LogMessage("Usage: OntologyGenerator.exe [OPTIONS]");
            Logger.LogMessage("Tool for generating and updating ontologies.");
            Logger.LogMessage("");
            Logger.LogMessage("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        protected UriRef GetPathFromSource(FileSource source)
        {
            UriRef result = null;
            if (source != null )
            {
                string sourcePath = (source as FileSource).Location;

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

        public int Run()
        {
            if (_config == null)
                throw new Exception("Config file not loaded.");

            FileInfo configFile = new FileInfo(_configPath);
            _sourceDir = configFile.Directory;


            if (!string.IsNullOrEmpty(_generatePath))
            {
                FileInfo fileInfo = new FileInfo(_generatePath);
                OntologyGenerator generator = new OntologyGenerator(_config.Namespace);
                generator.Logger = Logger;
                if (_config.Ontologies != null)
                {
                    foreach (var ontology in _config.Ontologies)
                    {
                        UriRef t = GetPathFromSource(ontology.FileSource);
                        if ( !generator.ImportOntology(ontology.Uri, t) )
                        {
                            FileInfo ontologyFile = new FileInfo(t.AbsolutePath);
                            var info = ontology.ElementInformation;
                            Logger.LogWarning(string.Format("Could not read ontology <{0}> from path {1}.", ontology.Uri, ontologyFile.FullName), info);
                        }

                        

                        if (!generator.AddOntology(ontology.Uri, ontology.MetadataUri, ontology.Prefix))
                        {
                            Logger.LogMessage("Ontology with uri <{0}> or uri <{1}> could not be found in store.", ontology.Uri, ontology.MetadataUri);
                        }

                    }
                }
                generator.GenerateFile(fileInfo);
                generator.Dispose();
                return 0;
            }
            else
            {
                return -1;
            }
        }


        #endregion
    }
}
