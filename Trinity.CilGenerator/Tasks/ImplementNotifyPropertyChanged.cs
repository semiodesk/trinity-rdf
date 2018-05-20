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

using Semiodesk.Trinity.CilGenerator.Extensions;
using Semiodesk.Trinity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NUnit.Framework;

namespace Semiodesk.Trinity.CilGenerator.Tasks
{
    public class ImplementNotifyPropertyChangedTask : GeneratorTaskBase
    {
        #region Memebers

        public bool IsMappedProperty;

        #endregion

        #region Constructors

        public ImplementNotifyPropertyChangedTask(ILGenerator generator, TypeDefinition type) : base(generator, type) { }

        #endregion

        #region Methods

        public override bool CanExecute(object parameter = null)
        {
            PropertyDefinition property = parameter as PropertyDefinition;

            return property != null && property.SetMethod != null;
        }

        public override bool Execute(object parameter = null)
        {
            PropertyDefinition property = parameter as PropertyDefinition;

            if (property == null) return false;

            if (!property.SetMethod.HasCustomAttribute<CompilerGeneratedAttribute>() && !property.SetMethod.IsCompilerControlled)
            {
                string msg = "{0}.{1}: Getter and setter of property must be compiler generated.";
                throw new Exception(string.Format(msg, property.DeclaringType.FullName, property.Name));
            }

            MethodDefinition raisePropertyChanged = Type.TryGetInheritedGenericMethod("RaisePropertyChanged", typeof(string));

            Assert.NotNull(raisePropertyChanged, "{0}: Found no suitable RaisePropertyChanged method.", Type.FullName);

            if (IsMappedProperty)
            {
                return ImplementPropertyMapping(property, raisePropertyChanged);
            }
            else
            {
                return ImplementBackingField(property, raisePropertyChanged);
            }
        }

        private bool ImplementBackingField(PropertyDefinition property, MethodDefinition raisePropertyChanged)
        {
            FieldReference backingField = property.TryGetBackingField();

            if (backingField == null)
            {
                Log.LogError("{0}.{1}: Unabled to find property backing field. Maybe the file has already been instrumented?", property.DeclaringType.FullName, property.Name);

                return false;
            }

            ILProcessor setProcessor = property.SetMethod.Body.GetILProcessor();

            // 1. Generate the instructions for setting the new property value.
            IList<Instruction> spv = GetSetBackingFieldValueInstructions(setProcessor, backingField).ToList();

            if (spv.Count == 0)
            {
                Log.LogError("{0}: Failed to generate byte code for SetValue() method.", Type.FullName);

                return false;
            }

            Instruction ret = setProcessor.Create(OpCodes.Ret);
            Instruction set = spv.First();

            // 2. Generate the instructions for raising the PropertyChanged event.
            IList<Instruction> rpc = GetRaisePropertyChangedInstructions(setProcessor, property, raisePropertyChanged).ToList();

            if (rpc.Count == 0)
            {
                Log.LogError("{0}: Failed to generate byte code for calling the RaisePropertyChanged() method.", Type.FullName);

                return false;
            }

            // 3. Generate the instructions for returning if the new value equals the old value.
            IList<Instruction> roe = GetReturnOnEqualsInstructionsForBackingField(setProcessor, backingField, set, ret).ToList();

            if (roe.Count == 0)
            {
                Log.LogError("{0}.{1}: Failed to generate byte code for return on equality.", Type.FullName, property.Name);

                return false;
            }

            // 4. Implement the property.
            setProcessor.Body.Instructions.Clear();
            setProcessor.Body.MaxStackSize = IsMappedProperty ? 4 : 2;

            // Return on equal values.
            foreach (Instruction i in roe) setProcessor.Append(i);

            // Set property value.
            foreach (Instruction i in spv) setProcessor.Append(i);

            // Raise the PropertyChanged event.
            foreach (Instruction i in rpc) setProcessor.Append(i);

            setProcessor.Append(ret);

            Log.LogMessage("{0}.{1}: Implemented NotifyPropertyChanged handler with raise method {2}.", Type.FullName, property.FullName, raisePropertyChanged.FullName);

            return true;
        }

        private bool ImplementPropertyMapping(PropertyDefinition property, MethodDefinition raisePropertyChanged)
        {
            ImplementRdfPropertyTask subtask = new ImplementRdfPropertyTask(Generator, Type);

            FieldDefinition mappingField = subtask.ImplementMappingField(property);

            PropertyGeneratorTaskHelper p = new PropertyGeneratorTaskHelper(property, mappingField);

            MethodGeneratorTask getValueGenerator = subtask.GetGetValueGenerator(p);
            MethodGeneratorTask setValueGenerator = subtask.GetSetValueGenerator(p);

            if (!getValueGenerator.CanExecute())
            {
                string msg = "{0}.{1}: Failed to implement property getter.";
                throw new Exception(string.Format(msg, property.DeclaringType.FullName, property.Name));
            }

            if (!setValueGenerator.CanExecute())
            {
                string msg = "{0}.{1}: Failed to implement property setter.";
                throw new Exception(string.Format(msg, property.DeclaringType.FullName, property.Name));
            }

            Instruction ret = setValueGenerator.Processor.Create(OpCodes.Ret);

            // Instructions for calling SetValue()
            IList<Instruction> sv = new List<Instruction>(setValueGenerator.Instructions);
            sv.RemoveAt(0); // Remove the first nop-Instruction..
            sv.RemoveAt(sv.Count - 1); // Remove the ret-Instruction..

            // Instructions for return on equal values.
            MethodReference getValue = Type.TryGetGetValueMethod(Assembly, property.PropertyType);

            IList<Instruction> roe = GetReturnOnEqualsInstructionsForMapping(setValueGenerator.Processor, mappingField, getValue, sv.First(), ret).ToList();

            Assert.Greater(roe.Count, 0, "{0}.{1}: Failed to generate byte code for return on equality.", Type.FullName, property.Name);

            // Instructions for calling RaisePropertyChanged()
            IList<Instruction> rpc = GetRaisePropertyChangedInstructions(setValueGenerator.Processor, property, raisePropertyChanged).ToList();

            Assert.Greater(rpc.Count, 0, "{0}: Failed to generate byte code for calling the RaisePropertyChanged() method.", Type.FullName);

            // Re-write the instructions for the SetValue() method generator.
            setValueGenerator.Processor.Body.MaxStackSize = IsMappedProperty ? 4 : 2;
            setValueGenerator.Instructions.Clear();
            setValueGenerator.Instructions.AddRange(roe);
            setValueGenerator.Instructions.AddRange(sv);
            setValueGenerator.Instructions.AddRange(rpc);
            setValueGenerator.Instructions.Add(ret);
            setValueGenerator.Execute();

            getValueGenerator.Execute();

            return true;
        }

        private IEnumerable<Instruction> GetReturnOnEqualsInstructionsForBackingField(ILProcessor processor, FieldReference backingField, Instruction cont, Instruction ret)
        {
            // Get a reference to the equality operator for the field type.
            TypeReference type = backingField.FieldType;

            // We need to initialize the local variables.
            processor.Body.Variables.Add(new VariableDefinition(MainModule.ImportReference(typeof(bool))));
            processor.Body.InitLocals = true;

            yield return processor.Create(OpCodes.Nop);
            yield return processor.Create(OpCodes.Ldarg_0);
            yield return processor.Create(OpCodes.Ldfld, backingField);
            yield return processor.Create(OpCodes.Ldarg_1);

            if (type.IsValueType)
            {
                yield return processor.Create(OpCodes.Ceq);
            }
            else
            {
                MethodReference equals = type.TryGetMethodReference(Assembly, "Equals", type, type);

                if (equals == null)
                {
                    MethodDefinition equalsDefinition = Assembly.GetSystemObjectEqualsMethodReference();

                    equals = MainModule.ImportReference(equalsDefinition);
                }

                yield return processor.Create(OpCodes.Call, equals);
            }

            yield return processor.Create(OpCodes.Ldc_I4_0);
            yield return processor.Create(OpCodes.Ceq);
            yield return processor.Create(OpCodes.Stloc_0);
            yield return processor.Create(OpCodes.Ldloc_0);
            yield return processor.Create(OpCodes.Brtrue_S, cont);
            yield return processor.Create(OpCodes.Br_S, ret);
        }

        private IEnumerable<Instruction> GetReturnOnEqualsInstructionsForMapping(ILProcessor processor, FieldDefinition backingField, MethodReference getValue, Instruction cont, Instruction ret)
        {
            // The PropertyMapping field type is a generic instance type.
            GenericInstanceType mappingType = backingField.FieldType as GenericInstanceType;

            // Get a reference to the equality operator for the generic instance type argument.
            TypeReference type = mappingType.GenericArguments.First();

            // We need to initialize the local variables.
            processor.Body.Variables.Add(new VariableDefinition(MainModule.ImportReference(typeof(bool))));
            processor.Body.InitLocals = true;

            yield return processor.Create(OpCodes.Nop);
            yield return processor.Create(OpCodes.Ldarg_0);
            yield return processor.Create(OpCodes.Ldarg_0);
            yield return processor.Create(OpCodes.Ldfld, backingField);
            yield return processor.Create(OpCodes.Callvirt, getValue);
            yield return processor.Create(OpCodes.Ldarg_1);

            if (type.IsValueType)
            {
                yield return processor.Create(OpCodes.Ceq);
            }
            else
            {
                MethodReference equals = type.TryGetMethodReference(Assembly, "Equals", type, type);

                if (equals == null)
                {
                    MethodDefinition equalsDefinition = Assembly.GetSystemObjectEqualsMethodReference();

                    equals = MainModule.ImportReference(equalsDefinition);
                }

                yield return processor.Create(OpCodes.Call, equals);
            }

            yield return processor.Create(OpCodes.Ldc_I4_0);
            yield return processor.Create(OpCodes.Ceq);
            yield return processor.Create(OpCodes.Stloc_0);
            yield return processor.Create(OpCodes.Ldloc_0);
            yield return processor.Create(OpCodes.Brtrue_S, cont);
            yield return processor.Create(OpCodes.Br_S, ret);
        }

        private IEnumerable<Instruction> GetSetBackingFieldValueInstructions(ILProcessor processor, FieldReference backingField)
        {
            yield return processor.Create(OpCodes.Ldarg_0);
            yield return processor.Create(OpCodes.Ldarg_1);
            yield return processor.Create(OpCodes.Stfld, backingField);
        }

        private IEnumerable<Instruction> GetCallSetValueInstructions(ILProcessor processor, FieldReference backingField, MethodReference setValue)
        {
            yield return processor.Create(OpCodes.Ldarg_0);
            yield return processor.Create(OpCodes.Ldarg_0);
            yield return processor.Create(OpCodes.Ldfld, backingField);
            yield return processor.Create(OpCodes.Ldarg_1);
            yield return processor.Create(OpCodes.Callvirt, MainModule.ImportReference(setValue));
            yield return processor.Create(OpCodes.Nop);
        }

        private IEnumerable<Instruction> GetRaisePropertyChangedInstructions(ILProcessor processor, PropertyDefinition property, MethodReference raisePropertyChanged)
        {
            yield return processor.Create(OpCodes.Ldarg_0);
            yield return processor.Create(OpCodes.Ldstr, property.Name);
            yield return processor.Create(OpCodes.Callvirt, MainModule.ImportReference(raisePropertyChanged));
            yield return processor.Create(OpCodes.Nop);
        }

        #endregion
    }
}
