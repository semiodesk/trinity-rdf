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

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Semiodesk.Trinity.CilGenerator.Extensions;
using Semiodesk.Trinity.CilGenerator.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Semiodesk.Trinity.CilGenerator
{
    /// <summary>
    /// Generator for manipulating the MSIL / CIL byte code of assemblies.
    /// </summary>
    public class ILGenerator
    {
        #region Members

        /// <summary>
        /// Assembly the generator is modifying.
        /// </summary>
        public AssemblyDefinition Assembly { get; private set; }

        /// <summary>
        /// Logger for warnings, errors and info messages.
        /// </summary>
        public ILogger Log { get; private set; }

        /// <summary>
        /// Indicates if the generator should write debugging symbols for the manipulated assembly.
        /// </summary>
        public bool WriteSymbols { get; set; }

        #endregion

        #region Constructors

        public ILGenerator(ILogger log)
        {
            WriteSymbols = true;
            Log = log;
        }

        #endregion

        #region Methods

        public bool ProcessFile(string sourceFile, string targetFile = "")
        {
            bool result = false;
            if (string.IsNullOrEmpty(targetFile))
            {
                targetFile = sourceFile;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(GetAssemblyDirectoryFromType(typeof(Resource)));
                var parameters = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                };

                Assembly = AssemblyDefinition.ReadAssembly(sourceFile, parameters);

                Log.LogMessage("------ Begin Task: ImplementRdfMapping [{0}]", Assembly.Name);

                // Read the debug symbols from the assembly.
                using (ISymbolReader symbolReader = new PdbReaderProvider().GetSymbolReader(Assembly.MainModule, sourceFile))
                {
                    Assembly.MainModule.ReadSymbols(symbolReader);

                    bool assemblyModified = false;

                    // Iterate over all types in the main assembly.
                    foreach (TypeDefinition type in Assembly.MainModule.Types)
                    {
                        // In the following we need to seperate between properties which have the following attribute combinations:
                        //  - PropertyAttribute with PropertyChangedAttribute
                        //  - PropertyAttribute without PropertyChangedAttribute
                        //  - PropertyChangedAttribute only
                        HashSet<PropertyDefinition> mapping = type.GetPropertiesWithAttribute<Semiodesk.Trinity.RdfPropertyAttribute>().ToHashSet();
                        HashSet<PropertyDefinition> notifying = type.GetPropertiesWithAttribute<Semiodesk.Trinity.NotifyPropertyChangedAttribute>().ToHashSet();

                        // Implement the GetTypes()-method for the given type.
                        if (mapping.Count > 0 || type.TryGetCustomAttribute<Semiodesk.Trinity.RdfClassAttribute>().Any())
                        {
                            ImplementRdfClassTask implementClass = new ImplementRdfClassTask(this, type);

                            // RDF types _must_ be implemented for classes with mapped properties.
                            assemblyModified = implementClass.Execute();
                        }

                        // Properties which do not raise the PropertyChanged-event can be implemented using minimal IL code.
                        var implementProperty = new ImplementRdfPropertyTask(this, type);

                        foreach (PropertyDefinition p in mapping.Except(notifying).Where(implementProperty.CanExecute))
                        {
                            assemblyModified = implementProperty.Execute(p);
                        }

                        // Properties which raise the PropertyChanged-event may also have the RdfProperty attribute.
                        var implementPropertyChanged = new ImplementNotifyPropertyChangedTask(this, type);

                        foreach (PropertyDefinition p in notifying.Where(implementPropertyChanged.CanExecute))
                        {
                            implementPropertyChanged.IsMappedProperty = mapping.Contains(p);

                            assemblyModified = implementPropertyChanged.Execute(p);
                        }
                    }

                    if (assemblyModified)
                    {
                        // NOTE: "WriteSymbols = true" generates the .pdb symbols required for debugging.
                        Assembly.Write(targetFile, new WriterParameters { WriteSymbols = WriteSymbols });
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
                result = false;
            }
            stopwatch.Stop();

            Log.LogMessage("------ End Task: ImplementRdfMapping [Total time: {0}s]", stopwatch.Elapsed.TotalSeconds);
            return result;
        }


        string GetAssemblyDirectoryFromType(Type type)
        {
            return new FileInfo(System.Reflection.Assembly.GetAssembly(type).Location).DirectoryName;
        }

        #endregion
    }
}
