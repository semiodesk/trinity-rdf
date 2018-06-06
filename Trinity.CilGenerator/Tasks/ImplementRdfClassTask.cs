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

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Semiodesk.Trinity.CilGenerator.Extensions;
using NUnit.Framework;
using System;

namespace Semiodesk.Trinity.CilGenerator.Tasks
{
    /// <summary>
    /// A task which implements the .GetTypes override which returns all annotated RDF classes
    /// for any class derived from Resource.
    /// </summary>
    internal class ImplementRdfClassTask : GeneratorTaskBase
    {
        #region Constructors

        public ImplementRdfClassTask(ILGenerator generator, TypeDefinition type) : base(generator, type) { }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates if the .GetTypes method can be implemented for a given type.
        /// </summary>
        /// <param name="parameter">A type definition.</param>
        /// <returns><c>true</c> if the task can be executed, <c>false</c> otherwise.</returns>
        public override bool CanExecute(object parameter = null)
        {
            bool canExecute = Type.TryGetCustomAttribute("RdfClassAttribute").Any() && !Type.Methods.Any( x=> x.Name == "GetTypes");
            return canExecute;
        }

        /// <summary>
        /// Implements the .GetTypes method which returns a list of all annotated RDF classes for a given type.
        /// </summary>
        /// <param name="parameter">A type defintion.</param>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        public override bool Execute(object parameter = null)
        {
            List<string> uris = new List<string>();

            // Accumulate the annotated RDF classes.
            foreach (CustomAttribute attribute in Type.TryGetCustomAttribute("RdfClassAttribute"))
            {
                uris.Insert(0, attribute.ConstructorArguments.First().Value.ToString());
            }

            if (uris.Count == 0)
            {
                if (!Type.Methods.Any(m => m.Name == "GetTypes"))
                {
                    string msg = "{0}: Mapped type has no annotated RDF classes and no overridden GetTypes() method.";

                    throw new Exception(string.Format(msg, Type.FullName));
                }

                return false;
            }

            TypeDefinition[] types = { Type };
            MethodDefinition getTypes = null;
            MethodReference getTypeBase = null;

            // We iterate through the type hierarchy from top to bottom and find the first GetTypes 
            // method. That one serves as a template for the override method of the current type in 
            // which the instructions are being replaced.
            foreach (TypeDefinition t in types.Union(Type.GetBaseTypes()).Reverse())
            {
                getTypes = t.TryGetInheritedMethod("GetTypes");

                if (getTypes != null && getTypeBase == null)
                {
                    getTypeBase = getTypes;

                    break;
                }
            }

            Assert.NotNull(getTypes, "{0}: Found no GetTypes() method for type.", Type.FullName);
            Assert.NotNull(getTypeBase, "{0}: Failed to find virtual base method of {1}", Type.FullName, getTypes.FullName);

            // The override method must be created in the module where the type is defined.
            getTypes = getTypeBase.GetOverrideMethod(MainModule);

            MethodGeneratorTask generator = new MethodGeneratorTask(getTypes);
            generator.Instructions.AddRange(GenerateGetTypesInstructions(generator.Processor, uris));

            if (generator.CanExecute())
            {
                generator.Method.Body.MaxStackSize = 3;
                generator.Method.Body.InitLocals = true;
                generator.Method.Body.Variables.Add(new VariableDefinition(MainModule.ImportReference(getTypeBase.ReturnType)));
                generator.Method.Body.Variables.Add(new VariableDefinition(ClassArrayType));
                generator.Execute();
                MainModule.ImportReference(ClassType);
            }

            Type.Methods.Add(getTypes);

            foreach (string uri in uris)
            {
                Log.LogMessage("{0} -> <{1}>", Type.FullName, uri);
            }

            return true;
        }

        /// <summary>
        /// Generates the MSIL byte code for the .GetTypes() method override which returns a list of given URIs.
        /// </summary>
        /// <param name="processor">A IL processor.</param>
        /// <param name="uris">List of URIs to be returned.</param>
        /// <returns>On success, a non-empty enumeration of instructions.</returns>
        private IEnumerable<Instruction> GenerateGetTypesInstructions(ILProcessor processor, IList<string> uris)
        {
            if (uris.Count == 0) yield break;

            // A reference to the item type constructor, however not yet imported from the assembly where it is defined.
            MethodReference ctorref = ClassType.Resolve().TryGetConstructor(typeof(string).FullName);

            if (ctorref == null) yield break;

            // A reference to the imported item type constructor.
            MethodReference ctor = Generator.Assembly.MainModule.ImportReference(ctorref);

            if (ctor == null) yield break;

            // IL_0000: nop
            // IL_0001: ldc.i4.1
            // IL_0002: newarr [Platform]Platform.Class
            // IL_0007: stloc.1
            // IL_0008: ldloc.1
            // IL_0009: ldc.i4.0
            // IL_000a: ldstr "http://schema.org/Thing"
            // IL_000f: newobj instance void [Platform]Platform.Class::.ctor(string)
            // IL_0014: stelem.ref
            // IL_0015: ldloc.1
            // IL_0016: stloc.0
            // IL_0017: br.s IL_0019 <-- NOTE: references to the next instruction!?
            // IL_0019: ldloc.0
            // IL_001a: ret

            Instruction ldloc = processor.Create(OpCodes.Ldloc_0);

            yield return processor.Create(OpCodes.Nop);

            // Define the a new array on the stack.
            yield return processor.CreateLdc_I4(uris.Count);
            yield return processor.Create(OpCodes.Newarr, ClassType);
            yield return processor.Create(OpCodes.Stloc_1);

            // Define the array items.
            for (int i = 0; i < uris.Count; i++)
            {
                string uri = uris[i];

                yield return processor.Create(OpCodes.Ldloc_1);
                yield return processor.CreateLdc_I4(i);
                yield return processor.Create(OpCodes.Ldstr, uri);
                yield return processor.Create(OpCodes.Newobj, ctor);
                yield return processor.Create(OpCodes.Stelem_Ref);
            }

            // Return a reference to the array.
            yield return processor.Create(OpCodes.Ldloc_1);
            yield return processor.Create(OpCodes.Stloc_0);
            yield return processor.Create(OpCodes.Br_S, ldloc);

            yield return ldloc;
            yield return processor.Create(OpCodes.Ret);
        }

        #endregion
    }
}
