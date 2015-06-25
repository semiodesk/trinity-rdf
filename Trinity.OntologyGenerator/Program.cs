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

namespace Semiodesk.Trinity.OntologyGenerator
{
    class Program
    {
        #region Members
        string _generatePath = null;
        int _verbosity = 0;
        private string _configPath = null;
        Configuration _config = null;
        private DirectoryInfo _sourceDir;
        #endregion

        [STAThread]
        static int Main(string[] args)
        {
            Program p = new Program(args);

            return 0;
        }

        #region Constructors
        Program(string[] args)
        {
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
                if (_config == null)
                    showHelp = true;
                else
                    Run();

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

        }

        #endregion

        #region Methods
        public void SetConfig(string config)
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
                    reader.Close();

                    //DataContractSerializer serializer = new DataContractSerializer(typeof(Configuration));
                    //_config = (Configuration)serializer.ReadObject(reader.BaseStream);
                    //reader.Close();
                }


            }
        }

        public void SetGenerate(string generate)
        {
            Debug("Generating to {0}", generate);
            _generatePath = generate;
        }

        void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: OntologyGenerator.exe [OPTIONS]");
            Console.WriteLine("Tool for generating and updating ontologies.");
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

        protected UriRef GetPathFromSource(Source source)
        {
            UriRef result = null;
            if (source != null && source is FileSource)
            {
                string sourcePath = (source as FileSource).Path;

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
            else if (source != null && source is WebSource)
            {
                result = (source as WebSource).FileUrl;
            }
            return result;
        }

        public int Run()
        {
            FileInfo configFile = new FileInfo(_configPath);
            _sourceDir = configFile.Directory;


            if (!string.IsNullOrEmpty(_generatePath))
            {
                FileInfo fileInfo = new FileInfo(_generatePath);
                OntologyGenerator generator = new OntologyGenerator(_config.Namespace);
                foreach (var ontology in _config.OntologyCollection)
                {
                    UriRef t = GetPathFromSource(ontology.Source);
                    generator.ImportOntology(ontology.Uri, t);

                    if (!generator.AddOntology(ontology.Uri, ontology.MetadataUri, ontology.Prefix))
                    {
                        Debug("Ontology with uri <{0}> or uri <{1}> could not be found in store.", ontology.Uri, ontology.MetadataUri);
                    }

                }
                generator.GenerateFile(fileInfo);
            }
            return 0;
        }

        public void UpdateOntologies(string hostname, int port, string username, string password)
        {

        }

        public static void CopyOntologies(DirectoryInfo target)
        {
            DirectoryInfo source = new DirectoryInfo("Ontologies");
            if (!source.Exists)
            {
                Console.WriteLine(string.Format("Error: The ontology source folder {0} does not exist.", source.FullName));
                return;
            }


            CopyAll(source, target);


        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            if (source.FullName.ToLower() == target.FullName.ToLower())
            {
                return;
            }

            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                if (Encoding.ASCII == GetEncoding(fi))
                {
                    Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                    fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
                }
                else
                {
                    Console.WriteLine(@"Omitting the file {0} is not ASCII encoded. Please re-encode manually.", fi.Name);
                }
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static Encoding GetEncoding(FileInfo file)
        {
            Encoding enc = null;
            FileStream fileStream = new System.IO.FileStream(file.FullName,
                FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fileStream.CanSeek)
            {
                byte[] bom = new byte[4]; // Get the byte-order mark, if there is one 
                fileStream.Read(bom, 0, 4);
                if ((bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) || // utf-8 
                    (bom[0] == 0xff && bom[1] == 0xfe) || // ucs-2le, ucs-4le, and ucs-16le 
                    (bom[0] == 0xfe && bom[1] == 0xff) || // utf-16 and ucs-2 
                    (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)) // ucs-4 
                {
                    enc = System.Text.Encoding.Unicode;
                }
                else
                {
                    enc = System.Text.Encoding.ASCII;
                }

                // Now reposition the file cursor back to the start of the file 
                fileStream.Seek(0, System.IO.SeekOrigin.Begin);
            }
            else
            {
                // The file cannot be randomly accessed, so you need to decide what to set the default to 
                // based on the data provided. If you're expecting data from a lot of older applications, 
                // default your encoding to Encoding.ASCII. If you're expecting data from a lot of newer 
                // applications, default your encoding to Encoding.Unicode. Also, since binary files are 
                // single byte-based, so you will want to use Encoding.ASCII, even though you'll probably 
                // never need to use the encoding then since the Encoding classes are really meant to get 
                // strings from the byte array that is the file. 

                enc = System.Text.Encoding.ASCII;
            }

            return enc;

        }

        #endregion
    }
}
