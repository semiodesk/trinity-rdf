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

using System.Linq;
using Mono.Cecil.Cil;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
    /// <summary>
    /// Extensions for the Mono.Cecil.Instruction class.
    /// </summary>
    public static class InstructionExtensions
    {
        /// <summary>
        /// Indicates if an instruction has a given opcode.
        /// </summary>
        /// <param name="instruction">A CIL instruction.</param>
        /// <param name="opcode">A CIL opcode.</param>
        /// <returns><c>true</c> if the opcode of the instruction matches the given value, <c>false</c> otherwise.</returns>
        public static bool Is(this Instruction instruction, OpCode opcode)
        {
            return instruction.OpCode == opcode;
        }

        /// <summary>
        /// Indicates if an instruction loads a 4 byte integer value onto the stack.
        /// </summary>
        /// <param name="instruction">A CIL instruction.</param>
        /// <returns><c>true</c> if the opcode of the instruction is one of the 4 byte integer opcodes, <c>false</c> otherwise.</returns>
        public static bool IsLdc_I4(this Instruction instruction)
        {
            OpCode[] opcodes =
            {
                OpCodes.Ldc_I4,
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_3,
                OpCodes.Ldc_I4_4,
                OpCodes.Ldc_I4_5,
                OpCodes.Ldc_I4_6,
                OpCodes.Ldc_I4_7,
                OpCodes.Ldc_I4_8,
                OpCodes.Ldc_I4_S,
            };

            return opcodes.Contains(instruction.OpCode);
        }
    }
}
