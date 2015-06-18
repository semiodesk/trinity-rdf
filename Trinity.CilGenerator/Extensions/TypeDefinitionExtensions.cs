/* Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 * Copyright (c) Semiodesk GmbH 2015
 * 
 * AUTHORS
 * - Moritz Eberl <moritz@semiodesk.com>
 * - Sebastian Faubel <sebastian@semiodesk.com>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
  internal static class TypeDefinitionExtensions
  {
    #region Methods

    public static bool HasBaseType(this TypeDefinition type)
    {
      return type != null && type.BaseType != null;
    }

    public static IEnumerable<TypeDefinition> GetBaseTypes(this TypeDefinition type)
    {
      while (type != null)
      {
        if (!HasBaseType(type)) break;

        type = type.BaseType.Resolve();

        yield return type;
      }
    }

    public static bool HasMethod(this TypeDefinition type, string name, params object[] arguments)
    {
      foreach (TypeDefinition t in GetBaseTypes(type))
      {
        foreach (MethodDefinition m in t.Methods)
        {
          if (!m.Name.Equals(name) || m.Parameters.Count != arguments.Count()) continue;

          bool match = !m.Parameters.Where((t1, i) => !t1.ParameterType.FullName.Equals(arguments[i])).Any();

          if (match) return true;
        }
      }

      return false;
    }

    public static MethodDefinition TryGetMethodWithArguments(this TypeDefinition type, string name, params object[] arguments)
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

    public static PropertyDefinition TryGetProperty(this TypeDefinition type, string name)
    {
      IEnumerable<PropertyDefinition> properties = GetBaseTypes(type).SelectMany(t => t.Properties);

      return properties.FirstOrDefault(p => p.Name.Equals(name));
    }

    public static MethodDefinition TryGetGenericMethod(this TypeDefinition type, string name, params object[] arguments)
    {
      foreach (TypeDefinition t in GetBaseTypes(type))
      {
        foreach (MethodDefinition m in t.Methods)
        {
          if (!m.Name.Equals(name) || m.Parameters.Count != arguments.Count()) continue;

          for (int i = 0; i < m.Parameters.Count; i++)
          {
            if (arguments[i] is GenericParameter)
            {
              GenericParameter genericParameter = arguments[i] as GenericParameter;

              if (genericParameter.FullName != m.Parameters[i].ParameterType.FullName) return null;
            }
            else
            {
              Type signatureType = arguments[i] as Type;
              TypeReference parameterType = m.Parameters[i].ParameterType;

              if (parameterType == null || signatureType == null) return null;

              if (!parameterType.FullName.Equals(signatureType.FullName)) return null;
            }
          }

          return m;
        }
      }

      return null;
    }

    public static IEnumerable<PropertyDefinition> GetPropertiesWithAttribute<T>(this TypeDefinition type) where T : Attribute
    {
      return type.Properties.Where(p => p.CustomAttributes.Any(a => a.Is(typeof(T))));
    }

    public static MethodReference TryGetMethodReference(this TypeReference type, AssemblyDefinition assembly, string name, params TypeReference[] arguments)
    {
      TypeDefinition t = type.Resolve();

      foreach (MethodDefinition m in t.Methods)
      {
        if (!m.Name.Equals(name) || m.Parameters.Count != arguments.Count()) continue;

        bool match = !m.Parameters.Where((t1, i) => !t1.ParameterType.FullName.Equals(arguments[i].FullName)).Any();

        // Return a reference to the method in the correct module.
        if (match) return assembly.MainModule.Import(m);
      }

      return null;
    }

    public static IEnumerable<CustomAttribute> TryGetCustomAttribute<T>(this TypeDefinition type) where T : Attribute
    {
      return type.CustomAttributes.Where(a => a.Is(typeof (T)));
    }

    public static MethodReference TryGetSetValueMethod(this TypeDefinition type, params TypeReference[] genericArguments)
    {
      GenericParameter memberType = new GenericParameter(type) { Name = "T&" };
      GenericParameter valueType = new GenericParameter(type) { Name = "T" };

      MethodDefinition result = type.TryGetGenericMethod("SetValue", typeof(string), memberType, valueType);

      if (result != null) return GetGenericInstanceMethod(result, genericArguments);



      return null;
    }

    private static MethodReference GetGenericInstanceMethod(MethodDefinition method, params TypeReference[] genericArguments)
    {
      if (method == null) throw new ArgumentException("method");
      if (method.GenericParameters.Count != genericArguments.Length) throw new ArgumentException("genericArguments");

      GenericInstanceMethod instance = new GenericInstanceMethod(method);

      foreach (GenericParameter parameter in method.GenericParameters)
      {
        instance.GenericParameters.Add(new GenericParameter(parameter.Name, method));
      }

      foreach (TypeReference argument in genericArguments)
      {
        instance.GenericArguments.Add(argument);
      }

      return instance;
    }

    #endregion
  }
}
