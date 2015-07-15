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
using System.Text;

namespace Semiodesk.Trinity.CilGenerator.Tasks
{
    /// <summary>
    /// A CIL generator task specialized on implementing method instructions.
    /// </summary>
    public class MethodGeneratorTask : IGeneratorTask
    {
        #region Members

        /// <summary>
        /// Method the task is modifying.
        /// </summary>
        public MethodDefinition Method { get; private set; }

        /// <summary>
        /// IL Processor for the method body.
        /// </summary>
        public ILProcessor Processor { get; private set; }

        /// <summary>
        /// List of instructions the task will be implementing.
        /// </summary>
        public readonly List<Instruction> Instructions = new List<Instruction>();

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new method implementation task.
        /// </summary>
        /// <param name="method">A method definition.</param>
        public MethodGeneratorTask(MethodDefinition method)
        {
            Method = method;

            if (method == null) return;

            Processor = Method.Body.GetILProcessor();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates if the task can be executed.
        /// </summary>
        /// <param name="parameter">This method takes no parameters.</param>
        /// <returns><c>true</c> if the task can execute, <c>false</c> otherwise.</returns>
        public bool CanExecute(object parameter = null)
        {
            return Method != null && Instructions.Any();
        }

        /// <summary>
        /// Implement the instructions in the method body.
        /// </summary>
        /// <param name="parameter">This method takes no parameters.</param>
        /// <returns><c>true</c></returns>
        public bool Execute(object parameter = null)
        {
            Processor.Body.Instructions.Clear();

            foreach (Instruction i in Instructions)
            {
                Processor.Append(i);
            }

            return true;
        }

        #endregion
    }
}
