
using ICSharpCode.Decompiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semiodesk.Trinity.CilGenerator.Resolver
{

    class CustomResolver : ICSharpCode.Decompiler.Metadata.UniversalAssemblyResolver
    {
        protected CustomResolver(string mainAssemblyFileName, bool throwOnError, string targetFramework) :base(mainAssemblyFileName, throwOnError, targetFramework, System.Reflection.PortableExecutable.PEStreamOptions.PrefetchEntireImage)
        {
            
        }

        public static ModuleDefinition LoadMainModule(string mainAssemblyFileName)
        {
            var moduleDefinition = new ICSharpCode.Decompiler.Metadata.PEFile(mainAssemblyFileName, System.Reflection.PortableExecutable.PEStreamOptions.PrefetchEntireImage);
            var targetFramework = ICSharpCode.Decompiler.Metadata.DotNetCorePathFinderExtensions.DetectTargetFrameworkId(moduleDefinition.Reader);
            var resolver = new CustomResolver(mainAssemblyFileName, true, targetFramework);

            var module = ModuleDefinition.ReadModule(mainAssemblyFileName, new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadWrite = true
            });

            
            return module;
        }

       

    }
}
