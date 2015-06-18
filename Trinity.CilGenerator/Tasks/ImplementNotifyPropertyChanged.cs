/* Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 * Copyright (c) Semiodesk GmbH 2015
 * 
 * AUTHORS
 * - Moritz Eberl <moritz@semiodesk.com>
 * - Sebastian Faubel <sebastian@semiodesk.com>
 */

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Semiodesk.Trinity;
using Semiodesk.Trinity.CilGenerator.Extensions;

namespace Semiodesk.Trinity.CilGenerator.Tasks
{
  public class ImplementNotifyPropertyChanged : GeneratorTaskBase
  {
    #region Memebers

    public bool CallSetValue;

    #endregion

    #region Constructors

    public ImplementNotifyPropertyChanged(ILGenerator generator, TypeDefinition type) : base(generator, type) { }

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

      ILProcessor processor = property.SetMethod.Body.GetILProcessor();

      FieldReference backingField = property.TryGetBackingField();

      if (backingField == null)
      {
        Log.LogError("{0}.{1}: Unabled to find property backing field. May be the file has already been instrumented?", property.DeclaringType.FullName, property.Name);

        return false;
      }

      MethodDefinition raisePropertyChanged = TryGetRaisePropertyChanged(Type);

      if (raisePropertyChanged == null)
      {
        Log.LogError("{0}: Unable to find RaisePropertyChanged method.", property.DeclaringType.FullName);

        return false;
      }

      // 1. Generate the instructions for setting the new property value.
      IList<Instruction> spv;

      if (CallSetValue)
      {
        MethodReference setValue = Assembly.MainModule.Import(Type.TryGetSetValueMethod(backingField.FieldType));

        if (setValue == null)
        {
          Log.LogError("{0}: Found no suitable SetValue method.", Type.FullName);

          return false;
        }

        string uri = property.TryGetAttributeParameter(typeof(RdfPropertyAttribute));

        if (string.IsNullOrEmpty(uri))
        {
          Log.LogError("{0}.{1}: Unabled to resolve URI of property.", property.DeclaringType.FullName, property.Name);

          return false;
        }

        spv = GetCallSetValueInstructions(processor, backingField, setValue, uri).ToList();
      }
      else
      {
        spv = GetSetBackingFieldValueInstructions(processor, backingField).ToList();
      }

      if (spv.Count == 0)
      {
        Log.LogError("{0}: Failed to generate byte code for SetValue() method.", Type.FullName);

        return false;
      }

      // 2. Generate the instructions for raising the PropertyChanged event.
      IList<Instruction> rpc = GetRaisePropertyChangedInstructions(processor, property, raisePropertyChanged).ToList();

      if (rpc.Count == 0)
      {
        Log.LogError("{0}: Failed to generate byte code for calling the RaisePropertyChanged() method.", Type.FullName);

        return false;
      }

      Instruction ret = processor.Create(OpCodes.Ret);
      Instruction set = spv.First();

      // 3. Generate the instructions for returning if the new value equals the old value.
      IList<Instruction> roe = GetReturnOnEqualsInstructions(processor, backingField, set, ret).ToList();

      if (roe.Count == 0)
      {
        Log.LogError("{0}: Failed to generate byte code for return on equality.", Type.FullName);

        return false;
      }

      // 4. Implement the property.
      processor.Body.Instructions.Clear();
      processor.Body.MaxStackSize = CallSetValue ? 4 : 2;

      // Return on equal values.
      foreach (Instruction i in roe) processor.Append(i);

      // Set property value.
      foreach (Instruction i in spv) processor.Append(i);

      // Raise the PropertyChanged event.
      foreach (Instruction i in rpc) processor.Append(i);

      processor.Append(ret);

      Log.LogMessage("{0}.{1}: Implemented NotifyPropertyChanged handler with raise method {2}.", Type.FullName, property.FullName, raisePropertyChanged.FullName);

      return true;
    }

    private static IEnumerable<Instruction> GetSetBackingFieldValueInstructions(ILProcessor processor, FieldReference backingField)
    {
      yield return processor.Create(OpCodes.Ldarg_0);
      yield return processor.Create(OpCodes.Ldarg_1);
      yield return processor.Create(OpCodes.Stfld, backingField);
    }

    private IEnumerable<Instruction> GetCallSetValueInstructions(ILProcessor processor, FieldReference backingField, MethodReference setValue, string uri)
    {
      yield return processor.Create(OpCodes.Ldarg_0);
      yield return processor.Create(OpCodes.Ldstr, uri);
      yield return processor.Create(OpCodes.Ldarg_0);
      yield return processor.Create(OpCodes.Ldflda, backingField);
      yield return processor.Create(OpCodes.Ldarg_1);
      yield return processor.Create(OpCodes.Call, Assembly.MainModule.Import(setValue));
      yield return processor.Create(OpCodes.Nop);
    }

    private IEnumerable<Instruction> GetReturnOnEqualsInstructions(ILProcessor processor, FieldReference backingField, Instruction cont, Instruction ret)
    {
        // Get a reference to the equality operator for the field type.
        TypeReference type = backingField.FieldType;

        MethodReference equals = null;

        if (type.IsValueType)
        {
            equals = type.TryGetMethodReference(Assembly, "op_Equality", type, type);
        }
        else
        {
            equals = type.TryGetMethodReference(Assembly, "Equals", type, type);

            if (equals == null)
            {
                MethodDefinition equalsDefinition = Assembly.GetSystemObjectEqualsMethodReference();

                equals = Assembly.MainModule.Import(equalsDefinition);
            }
        }

        if (equals == null) yield break;

        // We need to initialize the local variables.
        processor.Body.Variables.Add(new VariableDefinition(MainModule.Import(typeof(bool))));
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
            yield return processor.Create(OpCodes.Call, equals);
        }

        yield return processor.Create(OpCodes.Ldc_I4_0);
        yield return processor.Create(OpCodes.Ceq);
        yield return processor.Create(OpCodes.Stloc_0);
        yield return processor.Create(OpCodes.Ldloc_0);
        yield return processor.Create(OpCodes.Brtrue_S, cont);
        yield return processor.Create(OpCodes.Br_S, ret);
    }

    private IEnumerable<Instruction> GetRaisePropertyChangedInstructions(ILProcessor processor, PropertyDefinition property, MethodReference raisePropertyChanged)
    {
      yield return processor.Create(OpCodes.Ldarg_0);
      yield return processor.Create(OpCodes.Ldstr, property.Name);
      yield return processor.Create(OpCodes.Call, Assembly.MainModule.Import(raisePropertyChanged));
      yield return processor.Create(OpCodes.Nop);
    }

    private MethodDefinition TryGetRaisePropertyChanged(TypeDefinition type)
    {
      MethodDefinition result = type.TryGetGenericMethod("RaisePropertyChanged", typeof(string));

      if (result != null) return result;

      Log.LogError("{0}: Found no suitable RaisePropertyChanged method.", type.FullName);

      return null;
    }

    #endregion
  }
}
