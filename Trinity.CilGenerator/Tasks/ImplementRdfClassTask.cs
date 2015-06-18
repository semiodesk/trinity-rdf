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
      return Type.TryGetCustomAttribute<RdfClassAttribute>().Any();
    }

    /// <summary>
    /// Implements the .GetTypes method which returns a list of all annotated RDF classes for a given type.
    /// </summary>
    /// <param name="parameter">A type defintion.</param>
    /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
    public override bool Execute(object parameter = null)
    {
      List<string> uris = new List<string>();

      TypeDefinition[] types = { Type };

      MethodDefinition getTypes = null;

      MethodReference getTypeBase = null;

      // We iterate through the type hierarchy from top to bottom and find the first GetTypes 
      // method. That one serves as a template for the override method of the current type in 
      // which the instructions are being replaced.
      foreach (TypeDefinition t in types.Union(Type.GetBaseTypes()).Reverse())
      {
        getTypes = t.TryGetMethodWithArguments("GetTypes");

        if (getTypes == null) continue;

        if (getTypeBase == null)
        {
          // We have found the GetTypes of the base type.
          getTypeBase = getTypes;
        }
        else
        {
          // Accumulate the annotated RDF classes of the base types up to the current type.
          foreach (CustomAttribute attribute in t.TryGetCustomAttribute<RdfClassAttribute>())
          {
            uris.Insert(0, attribute.ConstructorArguments.First().Value.ToString());
          }
        }
      }

      if (getTypes == null)
      {
        Log.LogError("{0}: Found no GetTypes() method for type.", Type.FullName);

        return false;
      }

      if (uris.Count == 0)
      {
        Log.LogError("{0}: Found no RDF class URIs for type.", Type.FullName);

        return false;
      }

      // The override method must be created in the module where the type is defined.
      getTypes = getTypeBase.GetOverrideMethod(MainModule);

      ILProcessor processor = getTypes.Body.GetILProcessor();

      // The instructions which create the array containing the classes which are returned by the GetTypes method.
      List<Instruction> instructions = GenerateGetTypesInstructions(processor, uris).ToList();

      if (instructions.Count == 0)
      {
        Log.LogError("{0}: Failed to generate byte code for GetTypes() method.", Type.FullName);

        return false;
      }

      // Now that we're all set up we can start actually messing with the byte code.
      getTypes.Body.Instructions.Clear();

      foreach (Instruction instruction in instructions)
      {
        getTypes.Body.Instructions.Add(instruction);
      }

      getTypes.Body.Variables.Add(new VariableDefinition(GetReturnType(getTypeBase)));
      getTypes.Body.Variables.Add(new VariableDefinition(GetTypeReference<Class>()));
      getTypes.Body.InitLocals = true;
      getTypes.Body.MaxStackSize = 3;

      Type.Methods.Add(getTypes);

      foreach (string uri in uris)
      {
        Log.LogMessage("{0}: Implemented type mapping to RDF class <{1}>", Type.FullName, uri); 
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

      // A reference to the item type of the 'type'-array.
      TypeReference classType = MainModule.Import(typeof(Class));

      if (classType == null) yield break;

      // A reference to the item type constructor, however not yet imported from the assembly where it is defined.
      MethodReference ctorref = classType.Resolve().TryGetConstructor(typeof(string).FullName);

      if (ctorref == null) yield break;

      // A reference to the imported item type constructor.
      MethodReference ctor = Generator.Assembly.MainModule.Import(ctorref);

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
      yield return GetLdc_I4_X(processor, uris.Count);
      yield return processor.Create(OpCodes.Newarr, classType);
      yield return processor.Create(OpCodes.Stloc_1);

      // Define the array items.
      for (int i = 0; i < uris.Count; i++)
      {
        string uri = uris[i];

        yield return processor.Create(OpCodes.Ldloc_1);
        yield return GetLdc_I4_X(processor, i);
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

    /// <summary>
    /// Gets the instruction to push an integer value onto the stack.
    /// </summary>
    /// <param name="processor">A IL processor.</param>
    /// <param name="i">An integer value.</param>
    /// <returns>A MSIL instruction.</returns>
    private static Instruction GetLdc_I4_X(ILProcessor processor, int i)
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
        default: return processor.Create(OpCodes.Ldc_I4_S, i);
      }
    }

    private TypeReference GetTypeReference<T>() where T : class
    {
      return MainModule.Import(typeof(T));
    }

    private TypeReference GetReturnType(MethodReference method)
    {
      return MainModule.Import(method.ReturnType);
    }

    #endregion
  }
}
