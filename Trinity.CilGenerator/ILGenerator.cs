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
using Mono.Cecil.Pdb;
using Semiodesk.Trinity;
using Semiodesk.Trinity.CilGenerator.Extensions;
using Semiodesk.Trinity.CilGenerator.Tasks;

namespace Semiodesk.Trinity.CilGenerator
{
  public class ILGenerator
  {
    #region Members

    public AssemblyDefinition Assembly { get; private set; }

    public ILogger Log { get; private set; }

    #endregion

    #region Constructors

    public ILGenerator(ILogger log)
    {
      Log = log;
    }

    #endregion

    #region Methods

    public void ProcessFile(string targetFile)
    {
      try
      {
        Assembly = AssemblyDefinition.ReadAssembly(targetFile);

        Log.LogMessage("------ Begin Task: ImplementRdfMapping");

        // Read the debug symbols from the assembly.
        using (ISymbolReader symbolReader = new PdbReaderProvider().GetSymbolReader(Assembly.MainModule, targetFile))
        {
          Assembly.MainModule.ReadSymbols(symbolReader);

          bool assemblyModified = false;

          // Iterate over all types in the main assembly.
          foreach (TypeDefinition type in Assembly.MainModule.Types)
          {
            // Implement the GetTypes()-method for the given type.
            var implementClass = new ImplementRdfClassTask(this, type);

            if (implementClass.CanExecute())
            {
              assemblyModified = implementClass.Execute();
            }

            // In the following we need to seperate between properties which have the following attribute combinations:
            //  - PropertyAttribute with PropertyChangedAttribute
            //  - PropertyAttribute without PropertyChangedAttribute
            //  - PropertyChangedAttribute only
            ISet<PropertyDefinition> mapping = type.GetPropertiesWithAttribute<RdfPropertyAttribute>().ToHashSet();
            ISet<PropertyDefinition> notifying = type.GetPropertiesWithAttribute<NotifyPropertyChangedAttribute>().ToHashSet();

            // Properties which do not raise the PropertyChanged-event can be implemented using minimal IL code.
            var implementProperty = new ImplementRdfPropertyTask(this, type);

            foreach (PropertyDefinition p in mapping.Except(notifying).Where(implementProperty.CanExecute))
            {
              assemblyModified = implementProperty.Execute(p);
            }

            // Properties which raise the PropertyChanged-event may also have the RdfProperty attribute.
            var implementPropertyChanged = new ImplementNotifyPropertyChanged(this, type);

            foreach (PropertyDefinition p in notifying.Where(implementPropertyChanged.CanExecute))
            {
              implementPropertyChanged.CallSetValue = mapping.Contains(p);

              assemblyModified = implementPropertyChanged.Execute(p);
            }
          }

          if (assemblyModified)
          {
            // NOTE: "WriteSymbols = true" generates the .pdb symbols required for debugging.
            Assembly.Write(targetFile, new WriterParameters { WriteSymbols = true });
          }
        }
      }
      catch (Exception ex)
      {
        Log.LogMessage(ex.ToString());
      }

      Log.LogMessage("------ End Task: ImplementRdfMapping");
    }

    #endregion
  }
}
