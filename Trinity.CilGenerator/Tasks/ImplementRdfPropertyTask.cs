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
  public class ImplementRdfPropertyTask : GeneratorTaskBase
  {
    #region Constructors

    public ImplementRdfPropertyTask(ILGenerator generator, TypeDefinition type) : base(generator, type) { }

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

      if (property == null || !property.HasBackingField()) return false;

      ILProcessor processor = property.SetMethod.Body.GetILProcessor();

      FieldReference backingField = property.TryGetBackingField();

      if (backingField == null)
      {
        Log.LogError("{0}.{1}: Unabled to find property backing field. May be the file has already been instrumented?", property.DeclaringType.FullName, property.Name);

        return false;
      }

      MethodReference setValue = MainModule.Import(Type.TryGetSetValueMethod(backingField.FieldType));

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

      IList<Instruction> spv = property.GetCallSetValueInstructions(processor, backingField, setValue, uri).ToList();

      // Only continue if we have a valid instruction set.
      if (spv.Count == 0)
      {
        Log.LogError("{0}: Failed to implement SetValue for property {0}.", Type.FullName, property.FullName);

        return false;
      }

      processor.Body.Instructions.Clear();
      processor.Append(processor.Create(OpCodes.Nop));

      foreach (Instruction i in spv)
      {
        processor.Append(i);
      }

      processor.Append(processor.Create(OpCodes.Ret));

      Log.LogMessage("{0}.{1}: Implemented property with setter {2}", property.DeclaringType.FullName, property.Name, setValue);

      return true;
    }

    #endregion
  }
}
