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

using Semiodesk.Trinity;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
    /// <summary>
    /// Extensions for the Mono.Cecil.TypeDefinition class.
    /// </summary>
    internal static class TypeDefinitionExtensions
    {
        #region Methods

        /// <summary>
        /// Indicates if the type is inherited from a type other than System.Object.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <returns><c>true</c> if the type has a base class. <c>false</c> otherwise.</returns>
        public static bool HasBaseType(this TypeDefinition type)
        {
            return type != null && type.BaseType != null;
        }

        /// <summary>
        /// Enumerates all the base types from top to bottom.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <returns>An enumeration of types.</returns>
        public static IEnumerable<TypeDefinition> GetBaseTypes(this TypeDefinition type)
        {
            while (type != null)
            {
                if (!HasBaseType(type)) break;

                type = type.BaseType.Resolve();

                yield return type;
            }
        }

        /// <summary>
        /// Indicates if the type has a (inherited) method with the given name and attributes.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="arguments">Argument signature of the method (a list of types).</param>
        /// <returns><c>true</c> if the type has a matching method, <c>false</c> otherwise.</returns>
        public static bool HasMethod(this TypeDefinition type, string name, params object[] arguments)
        {
            return type.TryGetInheritedMethod(name, arguments) != null;
        }

        /// <summary>
        /// Indicates if the type has a field with the given name.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">Name of the field.</param>
        /// <returns><c>true</c> if the type has a matching field, <c>false</c> otherwise.</returns>
        public static bool HasField(this TypeDefinition type, string name)
        {
            return type.TryGetField(name) != null;
        }

        /// <summary>
        /// Get a (inherited) constructor with a given list of arguments.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="arguments">Constructor signature (list of argument types).</param>
        /// <returns>A method definition on success, <c>null</c> otherwise.</returns>
        public static MethodDefinition TryGetConstructor(this TypeDefinition type, params object[] arguments)
        {
            TypeDefinition[] types = { type };

            foreach (TypeDefinition t in types.Union(GetBaseTypes(type)))
            {
                foreach (MethodDefinition m in t.GetConstructors())
                {
                    if (m.Parameters.Count != arguments.Count()) continue;

                    bool match = !m.Parameters.Where((t1, i) => !t1.ParameterType.FullName.Equals(arguments[i])).Any();

                    if (match) return m;
                }
            }

            return null;
        }

        /// <summary>
        /// Get a property with a given name.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">Name of the property.</param>
        /// <returns>A property definition on success, <c>null</c> otherwise.</returns>
        public static PropertyDefinition TryGetProperty(this TypeDefinition type, string name)
        {
            IEnumerable<PropertyDefinition> properties = GetBaseTypes(type).SelectMany(t => t.Properties);

            return properties.FirstOrDefault(p => p.Name.Equals(name));
        }


        /// <summary>
        /// Get a field with a given name.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">Name of the property.</param>
        /// <returns>A field definition on success, <c>null</c> otherwise.</returns>
        public static FieldDefinition TryGetField(this TypeDefinition type, string name)
        {
            IEnumerable<FieldDefinition> fields = GetBaseTypes(type).SelectMany(t => t.Fields).Union(type.Fields);
            fields = fields.Where(p=>p.Name == name);

            return fields.FirstOrDefault();
        }

        /// <summary>
        /// Enumerate all properties of the type which have a given attribute.
        /// </summary>
        /// <typeparam name="T">A property attribute.</typeparam>
        /// <param name="type">A type definition.</param>
        /// <returns>An enumeration of properties.</returns>
        public static IEnumerable<PropertyDefinition> GetPropertiesWithAttribute<T>(this TypeDefinition type) where T : Attribute
        {
            return type.Properties.Where(p => p.CustomAttributes.Any(a => a.Is(typeof(T))));
        }

        /// <summary>
        /// Enumerate all properties of the type which have a given attribute.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">An attribute name.</param>
        /// <returns>An enumeration of properties.</returns>
        public static IEnumerable<PropertyDefinition> GetPropertiesWithAttribute(this TypeDefinition type, string attributeName)
        {
            return type.Properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == attributeName));
        }

        /// <summary>
        /// Get the first method with a given name and signature in the line of inheritance, starting from the type itself.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="arguments">List of method arguments.</param>
        /// <returns>A method definition on success, <c>null</c> otherwise.</returns>
        public static MethodDefinition TryGetInheritedMethod(this TypeDefinition type, string name, params object[] arguments)
        {
            TypeDefinition[] types = { type };

            foreach (TypeDefinition t in types.Union(GetBaseTypes(type)))
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    if (!m.Name.Equals(name) || m.Parameters.Count != arguments.Count()) continue;

                    bool match = !m.Parameters.Where((t1, i) => !t1.ParameterType.FullName.Equals(arguments[i])).Any();

                    if (match) return m;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the first (inherited) generic method definition with the given set of generic arguments.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="genericArguments">List of generic arguments (types).</param>
        /// <returns>A method definition on success, <c>false</c> otherwise.</returns>
        public static MethodDefinition TryGetInheritedGenericMethod(this TypeDefinition type, string name, params object[] genericArguments)
        {
            MethodDefinition m = TryGetGenericMethod(type, name, genericArguments);

            if (m == null)
            {
                foreach (TypeDefinition t in GetBaseTypes(type))
                {
                    m = TryGetGenericMethod(t, name, genericArguments);

                    if (m != null) break;
                }
            }

            return m;
        }

        /// <summary>
        /// Get a generic method definition with the given set of generic arguments.
        /// </summary>
        /// <param name="type">A type definition.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="genericArguments">List of generic arguments (types).</param>
        /// <returns>A method definition on success, <c>false</c> otherwise.</returns>
        private static MethodDefinition TryGetGenericMethod(TypeDefinition type, string name, params object[] genericArguments)
        {
            foreach (MethodDefinition m in type.Methods.Where(m => m.Name == name))
            {
                if (m.Parameters.Count != genericArguments.Count()) continue;

                for (int i = 0; i < m.Parameters.Count; i++)
                {
                    if (genericArguments[i] is GenericParameter)
                    {
                        GenericParameter genericParameter = genericArguments[i] as GenericParameter;

                        if (genericParameter.FullName == m.Parameters[i].ParameterType.FullName)
                        {
                            return m;
                        }
                    }
                    else
                    {
                        var signatureType = genericArguments[i] as TypeReference;
                        TypeReference parameterType = m.Parameters[i].ParameterType.GetElementType();

                        if (parameterType != null && signatureType != null && parameterType.FullName.Equals(signatureType.FullName))
                        {
                            return m;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Enumerate all custom attributes of a given type.
        /// </summary>
        /// <typeparam name="T">Attribute type.</typeparam>
        /// <param name="type">A type definition.</param>
        /// <returns>An enumeration of custom attributes.</returns>
        public static IEnumerable<CustomAttribute> TryGetCustomAttribute<T>(this TypeDefinition type) where T : Attribute
        {
            return type.CustomAttributes.Where(a => a.Is(typeof(T)));
        }

        /// <summary>
        /// Enumerate all custom attributes of a given type.
        /// </summary>
        /// <typeparam name="T">Attribute type.</typeparam>
        /// <param name="type">A type definition.</param>
        /// <returns>An enumeration of custom attributes.</returns>
        public static IEnumerable<CustomAttribute> TryGetCustomAttribute(this TypeDefinition type, string AttributeName)
        {
            return type.CustomAttributes.Where(a => a.AttributeType.Name == AttributeName);
        }

        /// <summary>
        /// Get the SetValue(PropertyMapping<T>) definition for a given type.
        /// </summary>
        /// <param name="type">A type defintion.</param>
        /// <param name="assembly">The assembly from which to import the method.</param>
        /// <param name="genericArguments">Generic type arguments to the SetValue method.</param>
        /// <returns>A method reference on success, <c>null</c> otherwise.</returns>
        public static MethodReference TryGetSetValueMethod(this TypeDefinition type, AssemblyDefinition assembly, TypeReference mappingType, params TypeReference[] genericArguments)
        {
            assembly.MainModule.ImportReference(mappingType);
            GenericParameter valueType = mappingType.GenericParameters[0];

            if (mappingType == null) return null;

            MethodDefinition method = type.TryGetInheritedGenericMethod("SetValue", mappingType, valueType);

            if (method == null) return null;

            return method.GetGenericInstance(assembly, genericArguments);
            
        }

        /// <summary>
        /// Get the SetValue(PropertyMapping<T>) definition for a given type.
        /// </summary>
        /// <param name="type">A type defintion.</param>
        /// <param name="assembly">The assembly from which to import the method.</param>
        /// <param name="genericArguments">Generic type arguments to the SetValue method.</param>
        /// <returns>A method reference on success, <c>null</c> otherwise.</returns>
        public static MethodReference TryGetGetValueMethod(this TypeDefinition type, AssemblyDefinition assembly, params TypeReference[] genericArguments)
        {

            TypeReference mappingType =ILGenerator.PropertyMapping;
            assembly.MainModule.ImportReference(mappingType);
            GenericParameter valueType = mappingType.GenericParameters[0];

            if (mappingType == null) return null;

            MethodDefinition method = type.TryGetInheritedGenericMethod("GetValue",ILGenerator.PropertyMapping);

            if (method == null) return null;

            return method.GetGenericInstance(assembly, genericArguments);
        }

        #endregion
    }
}
