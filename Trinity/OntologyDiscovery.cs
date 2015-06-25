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
using System.Reflection;
using System.Diagnostics;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This static class contains a mapping of all properties and classes to its uris for discovery of the proper object and its attributes.
    /// </summary>
    public static class OntologyDiscovery
    {
        #region Fields

        public static Dictionary<string, Property> Properties = new Dictionary<string, Property>();
        public static Dictionary<string, Class> Classes = new Dictionary<string, Class>();

        #endregion

        #region Constructors

        static OntologyDiscovery()
        {
        }

        #endregion

        #region Methods

        private static void AddOntologies(IEnumerable<Ontology> list)
        {
            foreach (Ontology o in list)
            {
                FieldInfo[] fieldList = o.GetType().GetFields(BindingFlags.Static | BindingFlags.Public);

                foreach (FieldInfo info in fieldList)
                {
                    if (info.FieldType == typeof(Class))
                    {
                        Class c = (Class)info.GetValue(o);
                        if (Classes.ContainsKey(c.Uri.OriginalString))
                            continue;

                        Classes.Add(c.Uri.OriginalString, c);
                    }
                    else if (info.FieldType == typeof(Property))
                    {
                        Property p = (Property)info.GetValue(o);
                        if (Properties.ContainsKey(p.Uri.OriginalString))
                            continue;

                        Properties.Add(p.Uri.OriginalString, p);
                    }
                }
            }
        }

        public static void AddAssembly(Assembly asm)
        {
            AddOntologies(GetInstances<Ontology>(asm));
        }

        private static IList<T> GetInstances<T>(Assembly asm)
        {
            return (from t in asm.GetTypes()
                    where t.BaseType == (typeof(T)) && t.GetConstructor(Type.EmptyTypes) != null
                    select (T)Activator.CreateInstance(t)).ToList();
        }

        public static Property GetProperty(Uri u)
        {
            if (Properties.ContainsKey(u.OriginalString))
                return Properties[u.OriginalString];
            return new Property(u);
        }

        public static Property GetProperty(string u)
        {
            if (Properties.ContainsKey(u))
                return Properties[u];
            return new Property(new UriRef(u));
        }
        #endregion
    }

}
