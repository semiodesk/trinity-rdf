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
using System.Linq;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
    /// <summary>
    /// Extensions for the Mono.Cecil.TypeReference class.
    /// </summary>
    public static class TypeReferenceExtensions
    {
        /// <summary>
        /// Get a method reference with the given set of arguments (type references).
        /// </summary>
        /// <param name="type">A type reference.</param>
        /// <param name="assembly">Assembly from which to import the type reference.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="arguments">List of method arguments.</param>
        /// <returns>A method reference on success, <c>null</c> otherwise.</returns>
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
    }
}
