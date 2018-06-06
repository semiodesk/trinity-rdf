using ICSharpCode.Decompiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semiodesk.Trinity.CilGenerator.Resolver
{
    class CustomResolver : UniversalAssemblyResolver
    {
        protected CustomResolver(string mainAssemblyFileName, bool throwOnError) :base(mainAssemblyFileName, throwOnError)
        {
            
        }

        public static ModuleDefinition LoadMainModule(string mainAssemblyFileName)
        {
            var resolver = new CustomResolver(mainAssemblyFileName, true);

            var module = ModuleDefinition.ReadModule(mainAssemblyFileName, new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadWrite = true
            });

            resolver.TargetFramework = module.Assembly.DetectTargetFrameworkId();

            return module;
        }
    }
}
