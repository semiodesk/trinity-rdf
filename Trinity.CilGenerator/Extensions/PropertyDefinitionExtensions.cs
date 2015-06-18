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

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
  public static class PropertyDefinitionExtensions
  {
    public static bool HasAttribute(this PropertyDefinition property, Type attributeType)
    {
      return property.CustomAttributes.Any(a => a.AttributeType.FullName == attributeType.FullName);
    }

    public static string TryGetAttributeParameter(this PropertyDefinition property, Type attributeType)
    {
      string attributeName = attributeType.FullName;

      foreach (CustomAttribute a in property.CustomAttributes.Where(a => a.AttributeType.FullName == attributeName))
      {
        return a.ConstructorArguments.First().Value.ToString();
      }

      return string.Empty;
    }

    public static bool HasBackingField(this PropertyDefinition property)
    {
      return property.SetMethod.Body.Instructions
        .Where(x => x.OpCode == OpCodes.Stfld)
        .Select(x => x.Operand as FieldReference).Any();
    }

    public static FieldReference TryGetBackingField(this PropertyDefinition property)
    {
      FieldReference result = property.SetMethod.Body.Instructions
        .Where(x => x.OpCode == OpCodes.Stfld)
        .Select(x => x.Operand as FieldReference).FirstOrDefault();

      return result;
    }

    public static IEnumerable<Instruction> GetCallSetValueInstructions(this PropertyDefinition property, ILProcessor processor, FieldReference backingField, MethodReference setValue, string uri)
    {
      if (string.IsNullOrEmpty(uri)) yield break;

      yield return processor.Create(OpCodes.Ldarg_0);
      yield return processor.Create(OpCodes.Ldstr, uri);
      yield return processor.Create(OpCodes.Ldarg_0);
      yield return processor.Create(OpCodes.Ldflda, backingField);
      yield return processor.Create(OpCodes.Ldarg_1);
      yield return processor.Create(OpCodes.Call, setValue);
      yield return processor.Create(OpCodes.Nop);
    }
  }
}
