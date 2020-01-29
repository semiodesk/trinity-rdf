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
// Copyright (c) Semiodesk GmbH 2015-2019

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Mdb;
using Semiodesk.Trinity.CilGenerator.Extensions;
using Semiodesk.Trinity.CilGenerator.Tasks;
using System.Diagnostics;
using System.IO;
using ICSharpCode.Decompiler;
using Semiodesk.Trinity.CilGenerator.Resolver;

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

        public ModuleDefinition ModuleDefinition { get; private set; }

        /// <summary>
        /// Logger for warnings, errors and info messages.
        /// </summary>
        public ILogger Log { get; private set; }

        /// <summary>
        /// Indicates if the generator should write debugging symbols for the manipulated assembly.
        /// </summary>
        public bool WriteSymbols { get; set; }

        public static TypeDefinition PropertyMapping;
        //public static TypeDefinition RdfClass;

        #endregion

        #region Constructors

        public ILGenerator(ILogger log, bool writeSymbols = true)
        {
            WriteSymbols = writeSymbols;
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
                using (var module = CustomResolver.LoadMainModule(sourceFile))
                {
                    if (WriteSymbols)
                    {
                        module.ReadSymbols();
                    }

                    Assembly = module.Assembly;

                    var resolver = module.AssemblyResolver;
                    var trinityRef = module.AssemblyReferences.Where(x => x.Name == "Semiodesk.Trinity").FirstOrDefault();
                    if (trinityRef == null)
                    {
                        Log.LogMessage("Reference to Semiodesk.Trinity not found. Stopping ImplementRdfMapping...");
                        return true;
                    }
                    var trinity = resolver.Resolve(trinityRef);

                    PropertyMapping = trinity.MainModule.Types.Where(b => b.FullName == "Semiodesk.Trinity.PropertyMapping`1").FirstOrDefault();
                    //RdfClass = trinity.MainModule.Types.Where(b => b.FullName == "Semiodesk.Trinity.Class").FirstOrDefault();

                    Log.LogMessage("------ Begin Task: ImplementRdfMapping [{0}]", Assembly.Name);

                    bool assemblyModified = false;

                    // Iterate over all types in the main assembly.
                    foreach (TypeDefinition type in Assembly.MainModule.Types)
                    {
                        // In the following we need to seperate between properties which have the following attribute combinations:
                        //  - PropertyAttribute with PropertyChangedAttribute
                        //  - PropertyAttribute without PropertyChangedAttribute
                        //  - PropertyChangedAttribute only
                        HashSet<PropertyDefinition> mapping = type.GetPropertiesWithAttribute("RdfPropertyAttribute").ToHashSet();
                        HashSet<PropertyDefinition> notifying = type.GetPropertiesWithAttribute("NotifyPropertyChangedAttribute").ToHashSet();

                        // Implement the GetTypes()-method for the given type.
                        if (mapping.Any() || type.TryGetCustomAttribute("RdfClassAttribute").Any())
                        {
                            ImplementRdfClassTask implementClass = new ImplementRdfClassTask(this, type);
                            if (implementClass.CanExecute())
                            {
                                // RDF types _must_ be implemented for classes with mapped properties.
                                assemblyModified = implementClass.Execute();
                            }
                        }

                        // Properties which do not raise the PropertyChanged-event can be implemented using minimal IL code.
                        if (mapping.Any())
                        {
                            var implementProperty = new ImplementRdfPropertyTask(this, type);

                            foreach (PropertyDefinition p in mapping.Except(notifying).Where(implementProperty.CanExecute))
                            {
                                assemblyModified = implementProperty.Execute(p);
                            }
                        }

                        // Properties which raise the PropertyChanged-event may also have the RdfProperty attribute.
                        if (notifying.Any())
                        {
                            var implementPropertyChanged = new ImplementNotifyPropertyChangedTask(this, type);

                            foreach (PropertyDefinition p in notifying.Where(implementPropertyChanged.CanExecute))
                            {
                                implementPropertyChanged.IsMappedProperty = mapping.Contains(p);

                                assemblyModified = implementPropertyChanged.Execute(p);
                            }
                        }
                    }

                    if (assemblyModified)
                    {
                        var param = new WriterParameters { WriteSymbols = WriteSymbols };

                        FileInfo sourceDll = new FileInfo(sourceFile);
                        FileInfo targetDll = new FileInfo(targetFile);

                        if (sourceDll.FullName != targetDll.FullName)
                        {
                            Assembly.Write(targetDll.FullName, param);
                        }
                        else
                        {
                            Assembly.Write(param);
                        }
                    }

                    result = true;
                }
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

        #endregion
    }
}
