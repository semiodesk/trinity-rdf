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
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
    /// <summary>
    /// Extensions for the Mono.Cecil.PropertyDefinition class.
    /// </summary>
    public static class PropertyDefinitionExtensions
    {
        /// <summary>
        /// Gets the first constructor argument for a given attribute type, if there are any.
        /// </summary>
        /// <param name="property">A property defition.</param>
        /// <param name="attributeType">An attribute type.</param>
        /// <returns>On success, the first attribute constructor argument as a string. An empty string otherwise.</returns>
        public static string TryGetAttributeParameter(this PropertyDefinition property, Type attributeType)
        {
            string attributeName = attributeType.FullName;

            foreach (CustomAttribute a in property.CustomAttributes.Where(a => a.AttributeType.FullName == attributeName))
            {
                return a.ConstructorArguments.First().Value.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the second constructor argument for a given attribute type, if there are any.
        /// </summary>
        /// <param name="property">A property defition.</param>
        /// <param name="attributeType">An attribute type.</param>
        /// <returns>On success, the first attribute constructor argument as a string. An empty string otherwise.</returns>
        public static bool TryGetSecondAttributeParameter(this PropertyDefinition property, Type attributeType, bool fallback)
        {
            string attributeName = attributeType.FullName;

            foreach (CustomAttribute a in property.CustomAttributes.Where(a => a.AttributeType.FullName == attributeName))
            {
                if( a.ConstructorArguments.Count >= 2)
                return (bool)a.ConstructorArguments[1].Value;
            }

            return fallback;
        }

        /// <summary>
        /// Get the backing field for a given property, if there is one.
        /// </summary>
        /// <param name="property">A property definition.</param>
        /// <returns>A field reference to the backing field on success, <c>null</c> otherwise.</returns>
        public static FieldReference TryGetBackingField(this PropertyDefinition property)
        {
            FieldReference result = property.SetMethod.Body.Instructions
              .Where(x => x.OpCode == OpCodes.Stfld)
              .Select(x => x.Operand as FieldReference).FirstOrDefault();

            return result;
        }
    }
}
