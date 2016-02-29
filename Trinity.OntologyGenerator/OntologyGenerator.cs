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
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using Semiodesk.Trinity;
using System.Security.Principal;
using System.Diagnostics;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif

namespace Semiodesk.Trinity.OntologyGenerator
{
    internal class OntologyGenerator : IDisposable
    {
        #region Members

        public ILogger Logger
        {
            get;
            set;
        }

        /// <summary>
        /// A reference to the store
        /// </summary>
        private IStore _store;

        /// <summary>
        /// Holds a list of all registered ontology models.
        /// </summary>
        private List<Tuple<IModel, IModel, string, string>> _models = new List<Tuple<IModel, IModel, string, string>>();

        /// <summary>
        /// Holds the symbols already in use.
        /// </summary>
        private List<string> _globalSymbols = new List<string>();

        /// <summary>
        /// A list of keywords which may not be used for generated names of resources.
        /// </summary>
        string[] _keywords =
        {
            "abstract", "event", "new", "struct",
            "as", "explicit", "null", "switch", 
            "base", "extern",  "object", "this", 
            "bool", "false", "operator", "throw",
            "break", "finally", "out", "true",
            "byte", "fixed", "override", "try",
            "case", "float", "params", "typeof",
            "catch", "for", "private", "uint",
            "char", "foreach", "protected", "ulong",
            "checked", "goto", "public", "unchecked",
            "class", "if", "readonly", "unsafe",
            "const", "implicit", "ref", "ushort",
            "continue", "in", "return", "using",
            "decimal", "int", "sbyte", "virtual",
            "default", "interface", "sealed", "volatile",
            "delegate", "internal", "short", "void",
            "do", "is", "sizeof", "while",
            "double", "lock", "stackalloc",	 
            "else", "long", "static",
            "enum", "namespace", "string", 
            "Namespace", "Prefix"
        };
        private string _namespace;

        #endregion

        #region Constructors

        public OntologyGenerator(string ns)
        {
            Logger = new ConsoleLogger();
            _namespace = ns;

            Console.WriteLine();
            Console.WriteLine(string.Format("Starting OntologyGenerator in {0}", Directory.GetCurrentDirectory()));
            Console.WriteLine();

            _store = StoreFactory.CreateStore("provider=dotnetrdf");
        }

        #endregion

        #region Methods

        public bool ImportOntology(Uri graphUri, Uri location)
        {
            FileInfo ontologyFile = new FileInfo(location.LocalPath);

            RdfSerializationFormat format;

            switch (ontologyFile.Extension)
            {
                case ".trig": format = RdfSerializationFormat.Trig; break;
                case ".n3": format = RdfSerializationFormat.N3; break;
                case ".nt": format = RdfSerializationFormat.NTriples; break;
                case ".ttl": format = RdfSerializationFormat.Turtle; break;
                default: format = RdfSerializationFormat.RdfXml; break;
            }

            try
            {
                _store.Read(graphUri, location, format, false);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool AddOntology(Uri graphUri, Uri metadataUri, string prefix)
        {
            if (graphUri == null) return false;

            IModel graphModel = _store.ContainsModel(graphUri) ? _store.GetModel(graphUri) : null;

            if (graphModel == null) return false;

            IModel metadataModel = null;

            if (metadataUri != null)
            {
                metadataModel = _store.ContainsModel(metadataUri) ? _store.GetModel(metadataUri) : null;
            }

            _models.Add(new Tuple<IModel, IModel, string, string>(graphModel, metadataModel, prefix, graphUri.AbsoluteUri));

            return true;
        }

        public void GenerateFile(FileInfo target)
        {
            StringBuilder ontologies = new StringBuilder();

            foreach (Tuple<IModel, IModel, string, string> model in _models)
            {
                _globalSymbols.Clear();

                Logger.LogMessage("Generating ontology <{0}>", model.Item1.Uri.OriginalString);

                if (model.Item2 == null)
                {
                    ontologies.Append(GenerateOntology(model.Item1, model.Item3, model.Item4));
                    ontologies.Append(GenerateOntology(model.Item1, model.Item3, model.Item4, true));
                }
                else
                {
                    ontologies.Append(GenerateOntology(model.Item1, model.Item2));
                    ontologies.Append(GenerateOntology(model.Item1, model.Item2, true));
                }
            }

            string content = string.Format(Properties.Resources.FileTemplate, DateTime.Now, ontologies.ToString(), _namespace);

            if (string.IsNullOrEmpty(content))
            {
                throw new Exception(string.Format("Content of file {0} should not be empty", target.FullName));
            }

            using (StreamWriter writer = new StreamWriter(target.FullName, false))
            {
                writer.Write(content.ToString());
            }
        }

        private string GetOntologyTitle(IModel model)
        {
            try
            {
                ResourceQuery query = new ResourceQuery();
                query.Where(rdf.type, owl.Ontology);

                IResourceQueryResult result = model.ExecuteQuery(query);

                if (result.Count() == 0) return "";

                IResource ontology = result.GetResources().First();

                return ontology.ListValues(dces.Title).OfType<string>().FirstOrDefault();
            }
            catch
            {
                string msg = "Could not retrieve title of ontology <{0}>";
                Logger.LogWarning(string.Format(msg, model.Uri.ToString()));
                return "";
            }
        }

        private string GenerateOntology(IModel model, string prefix, string ns, bool stringOnly = false)
        {
            string title = GetOntologyTitle(model);

            _globalSymbols.Add(prefix);

            return GenerateOntology(model, title, "", ns, prefix, stringOnly);
        }

        private string GenerateOntology(IModel model, IModel metadata, bool stringOnly = false)
        {
            IResource ontology = metadata.GetResource(model.Uri);

            string ns = ontology.GetValue(nao.hasdefaultnamespace).ToString();
            string nsPrefix = ontology.GetValue(nao.hasdefaultnamespaceabbreviation).ToString().ToLower();

            _globalSymbols.Add(nsPrefix);

            string title = "";
            string description = "";

            try
            {
                title = ontology.ListValues(dces.Title).First().ToString().Replace("\r\n", "///\r\n");
            }
            catch
            {
                string msg = "Could not retrieve title of ontology <{0}>";
                Logger.LogWarning(string.Format(msg, model.Uri.ToString()));
            }

            try
            {
                string desc = ontology.ListValues(dces.Description).First().ToString();
                desc = NormalizeLineBreaks(desc);
                description = desc.Replace("\r\n", "///\r\n");
            }
            catch
            {
                string msg = "Could not retrieve description of ontology <{0}>";
                Logger.LogWarning(string.Format(msg, model.Uri.ToString()));
               
            }

            return GenerateOntology(model, title, description, ns, nsPrefix, stringOnly);
        }

        private string GenerateOntology(IModel model, string title, string description, string ns, string nsPrefix, bool stringOnly = false)
        {
            StringBuilder result = new StringBuilder();

            SparqlQuery query = new SparqlQuery("select * where { ?s ?p ?o. FILTER isIRI(?s) }");

            List<string> localSymbols = new List<string>();

            foreach (IResource resource in model.GetResources(query))
            {
                try
                {
                    result.Append(GenerateResource(resource, model.Uri, localSymbols, stringOnly));
                }
                catch (Exception)
                {
                    Logger.LogWarning("Could not write resource <{0}>.", resource.Uri.OriginalString);   
                }
            }

            if (stringOnly)
            {
                nsPrefix = nsPrefix.ToUpper();

                return string.Format(Properties.Resources.StringOntologyTemplate, nsPrefix, ns, result.ToString(), title, description);
            }
            else
            {
                return string.Format(Properties.Resources.OntologyTemplate, nsPrefix, ns, result.ToString(), title, description);
            }
        }

        private string NormalizeLineBreaks(string s)
        {
            return Regex.Replace(s, @"\r\n|\n\r|\n|\r", "\r\n");
        }

        /// <summary>
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private string GenerateResource(IResource resource, Uri ontology, List<string> localSymbols, bool stringOnly = false)
        {
            string name = GetName(resource);

            if (string.IsNullOrEmpty(name)) return "";

            string comment = "";
            string type = "Resource";

            if (_globalSymbols.Contains(name) || localSymbols.Contains(name))
                name = GetName(resource, ontology);

            if (string.IsNullOrEmpty(name)) return "";

            if (_globalSymbols.Contains(name) || localSymbols.Contains(name))
            {
                int i = 0;

                while (_globalSymbols.Contains(string.Format("{0}_{1}", name, i)) || localSymbols.Contains(string.Format("{0}_{1}", name, i)))
                {
                    i++;
                }

                name = string.Format("{0}_{1}", name, i);
            }

            localSymbols.Add(name);

            if (resource.HasProperty(rdfs.comment))
            {
                string c = resource.ListValues(rdfs.comment).First().ToString();
                c = NormalizeLineBreaks(c);
                comment = c.Replace("\r\n", "\r\n    ///");
            }

            comment = string.Format("{0}\r\n    ///<see cref=\"{1}\"/>", comment, resource.Uri.OriginalString);

            if (resource.HasProperty(rdf.type, rdf.Property) ||
                resource.HasProperty(rdf.type, owl.DatatypeProperty) || 
                resource.HasProperty(rdf.type, owl.ObjectProperty) || 
                resource.HasProperty(rdf.type, owl.AnnotationProperty) || 
                resource.HasProperty(rdf.type, owl.AsymmetricProperty) ||
                resource.HasProperty(rdf.type, owl.DeprecatedProperty) ||
                resource.HasProperty(rdf.type, owl.FunctionalProperty) ||
                resource.HasProperty(rdf.type, owl.InverseFunctionalProperty) ||
                resource.HasProperty(rdf.type, owl.IrreflexiveProperty) ||
                resource.HasProperty(rdf.type, owl.ReflexiveProperty) |
                resource.HasProperty(rdf.type, owl.SymmetricProperty) || 
                resource.HasProperty(rdf.type, owl.TransitiveProperty) ||
                resource.HasProperty(rdf.type, owl.OntologyProperty))
            {
                type = "Property";
            }
            else if (resource.HasProperty(rdf.type, rdfs.Class) ||
                resource.HasProperty(rdf.type, owl.Class))
            {
                type = "Class";
            }

            if (stringOnly)
            {
                return string.Format(Properties.Resources.StringTemplate, type, name, resource.Uri.OriginalString, comment);
            }
            else
            {
                return string.Format(Properties.Resources.ResourceTemplate, type, name, resource.Uri.OriginalString, comment);
            }
        }

        private string GetName(IResource resource, Uri ontology = null)
        {
            string result = null;

            if( ontology == null )
            {
                if (!string.IsNullOrEmpty(resource.Uri.Fragment) && resource.Uri.Fragment.Length > 1)
                {
                    result = resource.Uri.Fragment.Substring(1);
                }
                else if (!string.IsNullOrEmpty(resource.Uri.Segments.Last()))
                {
                    result = resource.Uri.Segments.Last();
                }
            }
            else if (ontology == resource.Uri)
            {
                return null;
            }
            else
            {
                result = ontology.MakeRelativeUri(resource.Uri).ToString();
            }

            if( string.IsNullOrEmpty(result))
            {
                string msg = "Could not retrieve a name for resource <{0}>";
                throw new Exception(string.Format(msg, resource.Uri.OriginalString));
            }

            if (_keywords.Contains(result))
            {
                result = "_" + result;
            }

            if (Regex.Match(result, "^[0-9]").Success)
            {
                result = "_" + result;
            }

            result = result.Trim('/');

            if (result.Contains("/"))
            {
                result = result.Replace("/", "_");
            }

            if (result.Contains("."))
            {
                result = result.Replace(".", "_");
            }

            if (result.Contains("-"))
            {
                result = result.Replace("-", "_");
            }

            if (result.Contains("#"))
            {
                result = result.Replace("#", "_");
            }

            if (result.Contains(":"))
            {
                result = result.Replace(":", "_");
            }

            return result;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_store != null)
                _store.Dispose();
        }

        #endregion
    }
}
