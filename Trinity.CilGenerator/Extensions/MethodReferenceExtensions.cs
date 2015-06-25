﻿// LICENSE:
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
    /// Extensions fo the Mono.Cecil.MethodReference class.
    /// </summary>
    public static class MethodReferenceExtensions
    {
        #region Methods

        /// <summary>
        /// Get a new override method definition for the given method reference.
        /// </summary>
        /// <param name="method">A method reference.</param>
        /// <param name="module">A module definition.</param>
        /// <returns>A new method defition which overrides a the given method reference.</returns>
        public static MethodDefinition GetOverrideMethod(this MethodReference method, ModuleDefinition module)
        {
            const MethodAttributes attributes = MethodAttributes.Public
                                    | MethodAttributes.Virtual
                                    | MethodAttributes.ReuseSlot
                                    | MethodAttributes.HideBySig;

            return new MethodDefinition(method.Name, attributes, module.Import(method.ReturnType));
        }

        #endregion
    }
}
