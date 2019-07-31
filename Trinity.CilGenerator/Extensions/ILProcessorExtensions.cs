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
// Copyright (c) Semiodesk GmbH 2015-2019

using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Semiodesk.Trinity.CilGenerator.Extensions
{
    /// <summary>
    /// Extensions for the Mono.Cecil.Cil.ILProcessor class.
    /// </summary>
    public static class ILProcessorExtensions
    {
        /// <summary>
        /// Gets the instruction to push an integer value onto the stack.
        /// </summary>
        /// <param name="processor">A IL processor.</param>
        /// <param name="i">An integer value.</param>
        /// <returns>A MSIL instruction.</returns>
        public static Instruction CreateLdc_I4(this ILProcessor processor, int i)
        {
            if (0 <= i && i <= 8)
            {
                return CreateLdc_I4_X(processor, i);
            }

            return processor.Create(OpCodes.Ldc_I4_S, i);
        }

        /// <summary>
        /// Gets the instruction to push an integer value onto the stack.
        /// </summary>
        /// <param name="processor">A IL processor.</param>
        /// <param name="i">An integer value.</param>
        /// <returns>A MSIL instruction.</returns>
        public static Instruction CreateLdc_I4(this ILProcessor processor, uint i)
        {
            if (0 <= i && i <= 8)
            {
                return CreateLdc_I4_X(processor, (int)i);
            }

            return processor.Create(OpCodes.Ldc_I4_S, i);
        }

        /// <summary>
        /// Gets the insction to push an integer value small than 9 onto the stack.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static Instruction CreateLdc_I4_X(ILProcessor processor, int i)
        {
            // NOTE: Theare are special constants for referencing integers up to value 8.
            switch (i)
            {
                case 0: return processor.Create(OpCodes.Ldc_I4_0);
                case 1: return processor.Create(OpCodes.Ldc_I4_1);
                case 2: return processor.Create(OpCodes.Ldc_I4_2);
                case 3: return processor.Create(OpCodes.Ldc_I4_3);
                case 4: return processor.Create(OpCodes.Ldc_I4_4);
                case 5: return processor.Create(OpCodes.Ldc_I4_5);
                case 6: return processor.Create(OpCodes.Ldc_I4_6);
                case 7: return processor.Create(OpCodes.Ldc_I4_7);
                case 8: return processor.Create(OpCodes.Ldc_I4_8);
                default: throw new ArgumentException();
            }
        }
    }
}
