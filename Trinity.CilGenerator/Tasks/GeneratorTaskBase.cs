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

namespace Semiodesk.Trinity.CilGenerator.Tasks
{
    /// <summary>
    /// Base class for all CIL generator tasks.
    /// </summary>
    public class GeneratorTaskBase : IGeneratorTask
    {
        #region Members

        /// <summary>
        /// Currently modified type definition.
        /// </summary>
        /// 
        protected TypeDefinition Type { get; private set; }

        /// <summary>
        /// Current CIL generator instance.
        /// </summary>
        protected ILGenerator Generator { get; private set; }

        /// <summary>
        /// Currently modified assembly.
        /// </summary>
        protected AssemblyDefinition Assembly
        {
            get { return Generator.Assembly; }
        }

        /// <summary>
        /// Main module of the currently modified assembly.
        /// </summary>
        protected ModuleDefinition MainModule
        {
            get { return Generator.Assembly.MainModule; }
        }

        /// <summary>
        /// Logger of the CIL generator instance.
        /// </summary>
        protected ILogger Log
        {
            get { return Generator.Log; }
        }

        protected AssemblyDefinition Trinity;

        protected TypeReference ClassType = null;
        protected TypeReference ClassArrayType = null;
        protected TypeReference PropertyMappingType = null;
        

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new generator task for a given type definition.
        /// </summary>
        /// <param name="generator">CIL generator.</param>
        /// <param name="type">The type to be modified.</param>
        public GeneratorTaskBase(ILGenerator generator, TypeDefinition type)
        {
            Generator = generator;
            Type = type;

            LoadTrinityTypes();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates if the task can be executed.
        /// </summary>
        /// <param name="parameter">A task specific parameter.</param>
        /// <returns><c>true</c> if the task can be executed.</returns>
        public virtual bool CanExecute(object parameter = null)
        {
            return false;
        }

        /// <summary>
        /// Execute the task.
        /// </summary>
        /// <param name="parameter">A task specific parameter.</param>
        /// <returns><c>true</c> if the task has modified the assembly, <c>false</c> if not.</returns>
        public virtual bool Execute(object parameter = null)
        {
            return false;
        }

        protected virtual void LoadTrinityTypes()
        {
            LoadType("Semiodesk.Trinity.Class", out ClassType);
            ClassArrayType= new ArrayType(ClassType);

            PropertyMappingType = ILGenerator.propertyMapping;
            


        }

        private void LoadType(string name, out TypeReference typeRef)
        {
            Assembly.MainModule.TryGetTypeReference(name, out typeRef);
        }
        #endregion
    }
}
