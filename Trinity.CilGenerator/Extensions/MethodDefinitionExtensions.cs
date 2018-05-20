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

using Mono.Cecil;
using System;
using System.Linq;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
    /// <summary>
    /// Extensions fo the Mono.Cecil.MethodDefinition class.
    /// </summary>
    public static class MethodDefinitionExtensions
    {
        #region Methods

        /// <summary>
        /// Indicates if the method defintion has a custom attribute of a given type.
        /// </summary>
        /// <param name="method">A method defition.</param>
        /// <returns><c>true</c> if the method has the given attribute, <c>false</c> if not.</returns>
        public static bool HasCustomAttribute<T>(this MethodDefinition method) where T : Attribute
        {
            return method.CustomAttributes.Any(a => a.Is(typeof(T)));
        }

        /// <summary>
        /// Gets a reference to the instance of a generic method with the given generic arguments.
        /// </summary>
        /// <param name="method">A method definition.</param>
        /// <param name="assembly">Assembly from which to import the method from.</param>
        /// <param name="genericArguments">Enumeration of generic arguments (a list of types).</param>
        /// <returns>A reference to the instance of the generic method.</returns>
        public static MethodReference GetGenericInstance(this MethodDefinition method, AssemblyDefinition assembly, params TypeReference[] genericArguments)
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

            return assembly.MainModule.ImportReference(instance);
        }

        #endregion
    }
}
