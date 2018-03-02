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
using Semiodesk.Trinity.Configuration.Legacy;

namespace Semiodesk.Trinity.OntologyGenerator
{
    class Program
    {
        #region Members
        string _generatePath = null;
        int _verbosity = 0;
        private string _configPath = null;
        private FileInfo _configFile = null;
        IConfiguration _config = null;
        private DirectoryInfo _sourceDir = null;
        private OntologyGenerator _generator;

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
            { "g|generate=", "Path of the ontologies.cs file to generate.", v =>SetTarget(v)  },
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

        private bool LoadConfigFile()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration.Configuration));

            using (var stream = _configFile.OpenRead())
            {
                IConfiguration result = (Configuration.Configuration)serializer.Deserialize(stream);
                _config = result;
            }

            return true;
        }

        private bool LoadLegacyConfigFile()
        {
            bool res = false;
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();

            configMap.ExeConfigFilename = _configFile.FullName;

            var configuration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try
            {
                _config = (TrinitySettings)configuration.GetSection("TrinitySettings");
                res = true;
            }
            catch (Exception e)
            {
                Logger.LogError("Could not read config file from {0}. Reason: {1}", _configFile.FullName, e.Message);
            }
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

            return res;
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name == "Semiodesk.Trinity")
                return Assembly.GetAssembly(typeof(Resource));
            return null;
        }

        public void SetTarget(string targetPath)
        {
            Logger.LogMessage("Generating to {0}", targetPath);
            _generatePath = targetPath;
        }

        void ShowHelp(OptionSet p)
        {
            Logger.LogMessage("Usage: OntologyGenerator.exe [OPTIONS]");
            Logger.LogMessage("Tool for generating and updating ontologies.");
            Logger.LogMessage("");
            Logger.LogMessage("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        protected UriRef GetPathFromSource(string location)
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

        public int Run()
        {
            LoadLegacyConfigFile();
            if( _config == null)
                LoadConfigFile();

            if (!string.IsNullOrEmpty(_generatePath))
            {
                FileInfo fileInfo = new FileInfo(_generatePath);
                try
                {
                    _generator = new OntologyGenerator(_config.Namespace);

                    _generator.Logger = Logger;
    
                    foreach (var ontology in _config.ListOntologies())
                    {
                        Uri ontologyUri = null;
                        if(! ValidateOntology(ontology, out ontologyUri))
                            continue;

                        if( !string.IsNullOrEmpty(ontology.Location) )
                        { 
                            HandleFileSource(ontology.Location, ontologyUri);
                        }
                        /*// Handle Websource here
                        else if (ontology.WebSource != null)
                        {
                            HandleWebSource(ontology.WebSource);
                        }*/

                            
                        if (!_generator.AddOntology(ontology.Uri, null, ontology.Prefix))
                        {
                            Logger.LogMessage("Ontology with uri <{0}> or uri <{1}> could not be found in store.", ontology.Uri, null);
                        }
                            

                        
                    }
                    _generator.GenerateFile(fileInfo);
                    _generator.Dispose();

                    return 0;
                }catch(Exception e)
                {
                    Logger.LogError(string.Format("Error while generating {0}.\nException:\n{1}", fileInfo.FullName, e.ToString()));
                }
            }

                return -1;
            
        }

        public bool ValidateOntology(Semiodesk.Trinity.Configuration.IOntologyConfiguration ontology, out Uri uri)
        {
            uri = null;
            try
            {
                uri = ontology.Uri;
            }catch(Exception)
            {
                Logger.LogError(string.Format("Uri \"{1}\" of ontology {0} does not conform to RFC 3986.", ontology.Prefix, ontology.Uri));
                return false;
            }
            return true;
        }

        private void HandleWebSource(WebSource webSource)
        {
 	        // TODO: Cache ontology
            /*
            UriRef t = GetPathFromSource(ontology.WebSource);
            if (!generator.ImportOntology(new Uri(ontology.Uri), t))
            {
                FileInfo ontologyFile = new FileInfo(t.LocalPath);

                Logger.LogWarning(string.Format("Could not read ontology <{0}> from path {1}.", ontology.Uri, ontologyFile.FullName));
            }
            */
        }

        /// <summary>
        /// Handles a file source.
        /// </summary>
        /// <param name="source">The FileSource object.</param>
        /// <param name="uri">The uri of the ontology.</param>
        private void HandleFileSource(string location, Uri uri)
        {
            UriRef t = GetPathFromSource(location);
            if (!_generator.ImportOntology(uri, t))
            {
                FileInfo ontologyFile = new FileInfo(t.LocalPath);

                Logger.LogWarning(string.Format("Could not read ontology <{0}> from path {1}.", uri, ontologyFile.FullName));
            }
        }

        #endregion
    }
}
