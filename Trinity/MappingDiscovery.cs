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

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This static class is responsible for discovering mapped classes.
    /// Every assembly that defines mapping classes needs to register them with this service.
    /// </summary>
    public static class MappingDiscovery
    {
        #region MappingClass Definition
        /// <summary>
        /// A class containing information about a RDF class mapped to c#.
        /// </summary>
        public class MappingClass
        {
            private static Class ResourceClass = new Class(new UriRef("http://www.w3.org/2000/01/rdf-schema#Class"));

            /// <summary>
            /// The c# type of the class.
            /// </summary>
            public readonly Type MappingClassType;

            /// <summary>
            /// The RDF classes that are mapped to this class
            /// </summary>
            public readonly List<Class> RdfClasses = new List<Class>();

            /// <summary>
            /// A list of inferenced RDF classes mapped to this class. Currently not used.
            /// </summary>
            public readonly List<Class> InferencedRdfClasses = new List<Class>();

            /// <summary>
            /// Constructor to create a new MappingClass
            /// </summary>
            /// <param name="mappingClassType">The c# type</param>
            /// <param name="rdfClasses">The</param>
            public MappingClass(Type mappingClassType, IEnumerable<Class> rdfClasses)
            {
                MappingClassType = mappingClassType;
                RdfClasses.AddRange(rdfClasses);
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The list of all registered assemblies
        /// </summary>
        public static List<string> RegisteredAssemblies = new List<string>();

        /// <summary>
        /// The list of all registered mapped classes.
        /// </summary>
        public static List<MappingClass> MappingClasses = new List<MappingClass>();

        #endregion

        #region Constructor

        static MappingDiscovery()
        {
            AddMappingClasses(new List<Type> { typeof(Resource) });
            RegisteredAssemblies.Add(Assembly.GetExecutingAssembly().GetName().FullName);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a collection of mapped classes to the registration.
        /// </summary>
        /// <param name="list"></param>
        public static void AddMappingClasses(IList<Type> list)
        {
            foreach (Type o in list)
            {
                AddMappingClass(o);
            }
        }

        /// <summary>
        /// Adds a mapped class to the registration.
        /// </summary>
        /// <param name="_class"></param>
        public static void AddMappingClass(Type _class)
        {
            try
            {
                Resource r = (Resource)Activator.CreateInstance(_class, new UriRef("semio:empty"));

                MappingClass c = new MappingClass(_class, r.GetTypes());

                if (MappingClasses.Contains(c)) return;

                MappingClasses.Add(c);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Initialisation of mapping class {0} failed. For the reason please consult the inner exception.", _class.ToString()), e);
            }
        }

        internal static IEnumerable<IPropertyMapping> ListMappings(Type _class)
        {
            Type propertyMappingType = typeof(IPropertyMapping);
            Resource resource;
            try
            {
                resource = (Resource)Activator.CreateInstance(_class, new UriRef("semio:empty"));
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Initialisation of mapping class {0} failed. For the reason please consult the inner exception.", _class.ToString()), e);
            }
            foreach (var x in _class.GetFields())
            {
                if (propertyMappingType.IsAssignableFrom(x.FieldType))
                    yield return x.GetValue(resource) as IPropertyMapping;
            }

        }

        /// <summary>
        /// Loads all mapped classes from the assembly calling this method.
        /// </summary>
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
        /// from all assemblies currently loaded.
        /// </summary>
        public static void RegisterAllCurrentAssemblies()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!RegisteredAssemblies.Contains(a.GetName().FullName))
                    RegisterAssembly(a);
            }
        }

        /// <summary>
        /// Load all mapped classes from the given assembly.
        /// </summary>
        /// <param name="asm"></param>
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

        /// <summary>
        /// Returns all types which match the given restrictions.
        /// </summary>
        /// <param name="classes">List of RDF classes</param>
        /// <param name="type">A c# type in a inheritence tree. Give Resource if you don't know what to do.</param>
        /// <param name="inferencingEnabled">Should inferencing be factored in.</param>
        public static IList<Type> GetMatchingTypes(IList<Class> classes, Type type, bool inferencingEnabled = false)
        {
            if( !inferencingEnabled )
                return (IList<Type>)(from t in MappingClasses
                                     where t.RdfClasses.Count > 0 && t.RdfClasses.Intersect(classes).Count() == t.RdfClasses.Count && type.IsAssignableFrom(t.MappingClassType)
                                     select t.MappingClassType).ToList();
            else
                return (IList<Type>)(from t in MappingClasses
                                     where t.InferencedRdfClasses.Intersect(classes).Count() == t.RdfClasses.Count && type.IsAssignableFrom(t.MappingClassType)
                                     select t.MappingClassType).ToList();
        }

        /// <summary>
        /// The the RDF class of a C# type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IList<Class> GetRdfClasses(Type type)
        {
            return (from t in MappingClasses where t.MappingClassType == type select t.RdfClasses).First();
        }

        #endregion
    }
}
