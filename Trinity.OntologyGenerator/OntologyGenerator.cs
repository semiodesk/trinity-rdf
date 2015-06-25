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
    internal class OntologyGenerator
    {
        #region Members

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
            "enum", "namespace", "string"
        };
        private string _namespace;

        #endregion

        #region Constructors

        public OntologyGenerator(string ns)
        {
            _namespace = ns;
            Console.WriteLine();
            Console.WriteLine(string.Format("Starting OntologyGenerator in {0}", Directory.GetCurrentDirectory()));
            Console.WriteLine();


            _store = Stores.CreateStore("provider=dotnetrdf");

            Initialize();
        }

        #endregion

        #region Methods

        public void ImportOntology(Uri graphUri, Uri location)
        {
            FileInfo ontologyFile = new FileInfo(location.AbsolutePath);
            RdfSerializationFormat format;
            if (ontologyFile.Extension == ".trig")
                format = RdfSerializationFormat.Trig;
            else if (ontologyFile.Extension == ".n3")
                format = RdfSerializationFormat.N3;
            else if (ontologyFile.Extension == ".ttl")
                format = RdfSerializationFormat.Turtle;
            else
                format = RdfSerializationFormat.RdfXml;

            try
            {
                if (!_store.ContainsModel(graphUri))
                    _store.Read(graphUri, location, format);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error reading ontology {0}: {1}", location.AbsolutePath, e));
            }
        }



        public bool AddOntology(Uri graphUri, Uri metadataUri, string prefix)
        {
            if (graphUri != null && _store.ContainsModel(graphUri))
            {
                IModel graphModel = _store.GetModel(graphUri);
                IModel metaModel = null;
                if (metadataUri != null && _store.ContainsModel(metadataUri))
                    metaModel = _store.GetModel(metadataUri);

                _models.Add(new Tuple<IModel, IModel, string, string>(graphModel, metaModel, prefix, graphUri.AbsoluteUri));
                return true;
            }
            return false;
        }

        private void Initialize()
        {
        }

        public void GenerateFile(FileInfo target)
        {
            StringBuilder ontologies = new StringBuilder();

            foreach (Tuple<IModel, IModel, string, string> model in _models)
            {
                _globalSymbols.Clear();
                Console.WriteLine(string.Format("Generating <{0}>", model.Item1.Uri.OriginalString));

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

            string content = string.Format(Properties.Resources.FileTemplate, DateTime.Now, WindowsIdentity.GetCurrent().Name, ontologies.ToString(), _namespace);
            if (string.IsNullOrEmpty(content))
                throw new Exception(string.Format("Content of file {0} should not be empty", target.FullName));
            using (StreamWriter writer = new StreamWriter(target.FullName, false))
            {
                writer.Write(content.ToString());
            }

            Console.WriteLine();
        }

        private string GetOntologyTitle(IModel model)
        {
            string title = "";
            try
            {
                ResourceQuery query = new ResourceQuery();
                query.Where(rdf.type, owl.Ontology);

                var res = model.ExecuteQuery(query);
                if (res.Count() > 0)
                {
                    IResource ontology = res.GetResources().First();
                    foreach (var t in ontology.ListValues(dces.Title))
                    {
                        if (t is string)
                        {
                            title = (t as string).Replace("\r\n", "///\r\n");
                        }
                        else if (t is Tuple<string, string>)
                        {
                            Tuple<string, string> val = (Tuple<string, string>)t;

                            //title = t.Replace("\r\n", "///\r\n");
                        }
                    }
                }
            }
            catch
            {
                string msg = "Warning: Could not retrieve <dc:title> of ontology <{0}>";
                Debug.WriteLine(string.Format(msg, model.Uri.ToString()));

            }

            return title;
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
                string msg = "Warning: Could not retrieve <dc:title> of ontology <{0}>";
                Debug.WriteLine(string.Format(msg, ontology.Uri.ToString()));
            }


            try
            {
                string desc = ontology.ListValues(dces.Description).First().ToString();
                desc = NormalizeLineBreaks(desc);
                description = desc.Replace("\r\n", "///\r\n");
            }
            catch
            {
                string msg = "Warning: Could not retrieve <dc:description> of ontology <{0}>";
                Debug.WriteLine(string.Format(msg, ontology.Uri.ToString()));
            }
            return GenerateOntology(model, title, description, ns, nsPrefix, stringOnly);
        }

        private string GenerateOntology(IModel model, string title, string description, string ns, string nsPrefix, bool stringOnly = false)
        {
            StringBuilder result = new StringBuilder();

            string queryString = string.Format("select * where {{ ?s ?p ?o }}", model.Uri);
            SparqlQuery query = new SparqlQuery(queryString);

            List<string> localSymbols = new List<string>();

            foreach (IResource resource in model.GetResources(query))
            {
                try
                {
                    result.Append(GenerateResource(resource, localSymbols, stringOnly));
                }
                catch (Exception)
                {
                    Console.WriteLine(string.Format("Error: Could not write <{0}>.", resource.Uri.OriginalString));
                }
            }
            if (stringOnly)
                nsPrefix = nsPrefix.ToUpper();
            return string.Format(Properties.Resources.OntologyTemplate, nsPrefix, ns, result.ToString(), title, description);
        }

        private string NormalizeLineBreaks(string s)
        {
            return Regex.Replace(s, @"\r\n|\n\r|\n|\r", "\r\n");
        }

        /// <summary>
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private string GenerateResource(IResource resource, List<string> localSymbols, bool stringOnly = false)
        {
            string name = GetName(resource);
            if (string.IsNullOrEmpty(name))
                return "";

            string comment = "";
            string type = "Resource";

            if (_globalSymbols.Contains(name) || localSymbols.Contains(name))
            {
                int i = 0;

                while (_globalSymbols.Contains(string.Format("{0}_{1}", name, i)))
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
                resource.HasProperty(rdf.type, owl.ReflexiveProperty) ||
                resource.HasProperty(rdf.type, owl.SymmetricProperty) || 
                resource.HasProperty(rdf.type, owl.TransitiveProperty) ||
                resource.HasProperty(rdf.type, owl.OntologyProperty)
                )
            {
                type = "Property";
            }
            else if (resource.HasProperty(rdf.type, rdfs.Class) || resource.HasProperty(rdf.type, owl.Class))
            {
                type = "Class";
            }

            if( stringOnly )
                return string.Format(Properties.Resources.StringTemplate, type, name, resource.Uri.OriginalString, comment);
            else
                return string.Format(Properties.Resources.ResourceTemplate, type, name, resource.Uri.OriginalString, comment);
        }

        private string GetName(IResource resource)
        {
            string result;

            if (!string.IsNullOrEmpty(resource.Uri.Fragment) && resource.Uri.Fragment.Length > 1)
            {
                result = resource.Uri.Fragment.Substring(1);
            }
            else if (!string.IsNullOrEmpty(resource.Uri.Segments.Last()))
            {
                result = resource.Uri.Segments.Last();
            }
            else
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

            if (result.Contains("/"))
            {
                result = result.Replace("/", "");
            }

            if (result.Contains("."))
            {
                result = result.Replace(".", "_");
            }

            if (result.Contains("-"))
            {
                result = result.Replace("-", "_");
            }


            return result;
        }

        #endregion
    }
}
