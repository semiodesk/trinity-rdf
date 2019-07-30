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
using System.Reflection;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This static class contains a mapping of all properties and classes to its uris for discovery of the proper object and its attributes.
    /// For future reference: look into PreApplicationStartMethodAttribute Class or ModuleInitializer
    /// </summary>
    public static class OntologyDiscovery
    {
        #region Members

        /// <summary>
        /// All registered RDF ontology prefixes in the current application.
        /// </summary>
        public static readonly Dictionary<string, Uri> Namespaces = new Dictionary<string, Uri>();

        /// <summary>
        /// All registered RDF properties in the current application.
        /// </summary>
        public static readonly Dictionary<string, Property> Properties = new Dictionary<string, Property>();

        /// <summary>
        /// All registered RDF classes in the current application.
        /// </summary>
        public static readonly Dictionary<string, Class> Classes = new Dictionary<string, Class>();

        #endregion

        #region Constructors

        static OntologyDiscovery() {}

        #endregion

        #region Methods

        /// <summary>
        /// Register a namespace with a prefix.
        /// </summary>
        /// <param name="prefix">A namespace prefix.</param>
        /// <param name="uri">A uniform resource identifier.</param>
        public static void AddNamespace(string prefix, Uri uri)
        {
            Namespaces[prefix] = uri;
        }

        /// <summary>
        /// Register an assembly to search for RDF ontologies.
        /// </summary>
        /// <param name="asm"></param>
        public static void AddAssembly(Assembly asm)
        {
            var instances =
                from type in asm.GetTypes()
                where
                    type.BaseType == typeof(Ontology) && type.GetConstructor(Type.EmptyTypes) != null
                select
                    (Ontology)Activator.CreateInstance(type);

            AddOntologies(instances);
        }

        /// <summary>
        /// Register the concepts from a given set of ontologies.
        /// </summary>
        /// <param name="ontologies">An enumeration of ontologies.</param>
        private static void AddOntologies(IEnumerable<Ontology> ontologies)
        {
            foreach (Ontology ontology in ontologies)
            {
                // The namespace URI of the ontology.
                Uri uri = null;

                // The registered prefix of the ontology.
                string prefix = string.Empty;

                FieldInfo[] fields = ontology.GetType().GetFields(BindingFlags.Static | BindingFlags.Public);

                foreach (FieldInfo field in fields)
                {
                    // Register the Ontology prefix and name with the NamespaceManager.
                    if (field.Name == "Prefix")
                    {
                        prefix = field.GetValue(ontology) as string;
                    }
                    else if (field.Name == "Namespace")
                    {
                        uri = field.GetValue(ontology) as Uri;
                    }

                    if (field.FieldType == typeof(Class))
                    {
                        Class c = field.GetValue(ontology) as Class;

                        if (Classes.ContainsKey(c.Uri.OriginalString))
                            continue;

                        Classes.Add(c.Uri.OriginalString, c);
                    }
                    else if (field.FieldType == typeof(Property))
                    {
                        Property p = field.GetValue(ontology) as Property;

                        if (Properties.ContainsKey(p.Uri.OriginalString))
                            continue;

                        Properties.Add(p.Uri.OriginalString, p);
                    }
                }

                if (!string.IsNullOrEmpty(prefix) && uri != null)
                {
                    Namespaces[prefix] = uri;
                }
            }
        }

        /// <summary>
        /// Returns a a property with the given Uri. Creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public static Property GetProperty(Uri u)
        {
            return Properties.ContainsKey(u.OriginalString) ? Properties[u.OriginalString] : new Property(u);
        }

        /// <summary>
        /// Returns a a property with the given string. Creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public static Property GetProperty(string u)
        {
            return Properties.ContainsKey(u) ? Properties[u] : new Property(new UriRef(u));
        }

        #endregion
    }
}
