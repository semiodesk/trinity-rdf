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
using System.Linq;
using System.Text;
using System.Reflection;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This static class is responsible for discovering mapped classes.
    /// Every assembly that defines mapping classes needs to register them with this service.
    /// </summary>
    public static class MappingDiscovery
    {
        #region MappingClass Definition

        public class MappingClass
        {
            private static Class ResourceClass = new Class(new UriRef("http://www.w3.org/2000/01/rdf-schema#Class"));
            public readonly Type MappingClassType;
            public readonly List<Class> RdfClasses = new List<Class>();
            public readonly List<Class> InferencedRdfClasses = new List<Class>();

            public MappingClass(Type mappingClassType, IList<Class> rdfClasses)
            {
                MappingClassType = mappingClassType;
                RdfClasses.AddRange(rdfClasses);
            }
        }

        #endregion

        #region Fields

        public static List<string> RegisteredAssemblies = new List<string>();

        public static List<MappingClass> MappingClasses = new List<MappingClass>();

        //public static Dictionary<Type, Dictionary<string, Property>> MappedProperties = new Dictionary<Type, Dictionary<string, Property>>();

        #endregion

        #region Constructor

        static MappingDiscovery()
        {
            AddMappingClasses(new List<Type> { typeof(Resource) });
            RegisteredAssemblies.Add(Assembly.GetExecutingAssembly().GetName().FullName);
        }

        #endregion

        #region Methods

        public static void AddMappingClasses(IList<Type> list)
        {
            foreach (Type o in list)
            {
                AddMappingClass(o);
            }
        }

        public static void AddMappingClass(Type _class)
        {
            try
            {
                Resource r = (Resource)Activator.CreateInstance(_class, new UriRef("semio:empty"));

                MappingClass c = new MappingClass(_class, r.Classes);

                if (MappingClasses.Contains(c)) return;

                MappingClasses.Add(c);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Initialisation of mapping class {0} failed. For the reason please consult the inner exception.", _class.ToString()), e);
            }
        }

        public static void RegisterCallingAssembly()
        {
            Assembly asm = Assembly.GetCallingAssembly();

            if (!RegisteredAssemblies.Contains(asm.GetName().FullName))
            {
                MappingDiscovery.RegisterAssembly(asm);
            }
        }

        /// <summary>
        /// Register ALL THE THINGS!!
        /// </summary>
        public static void RegisterAllCurrentAssemblies()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!RegisteredAssemblies.Contains(a.GetName().FullName))
                    RegisterAssembly(a);
            }
        }

        public static void RegisterAssembly(Assembly asm)
        {
            RegisteredAssemblies.Add(asm.GetName().FullName);
            IList<Type> l = GetMappingClasses(asm);
            AddMappingClasses(l);
        }

        private static IList<Type> GetMappingClasses(Assembly asm)
        {
            try
            {
                return (IList<Type>)(from t in asm.GetTypes()
                                     where typeof(Resource).IsAssignableFrom(t)
                                     select t).ToList();
            }
            catch
            {
                return new List<Type>();
            }
        }

        public static IList<Type> GetMatchingTypes(IList<Class> classes, Type type, bool inferencingEnabled = false)
        {
            if( !inferencingEnabled )
                return (IList<Type>)(from t in MappingClasses
                                     where t.RdfClasses.Intersect(classes).Count() == classes.Count && type.IsAssignableFrom(t.MappingClassType)
                                     select t.MappingClassType).ToList();
            else
                return (IList<Type>)(from t in MappingClasses
                                     where t.InferencedRdfClasses.Intersect(classes).Count() == classes.Count && type.IsAssignableFrom(t.MappingClassType)
                                     select t.MappingClassType).ToList();
        }

        public static IList<Class> GetRdfClasses(Type type)
        {
            return (from t in MappingClasses where t.MappingClassType == type select t.RdfClasses).First();
        }

        #endregion
    }
}
