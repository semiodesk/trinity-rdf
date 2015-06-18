using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semiodesk.Trinity.CilGenerator.Extensions
{
    public static class AssemblyDefinitionExtensions
    {
        public static MethodDefinition GetSystemObjectEqualsMethodReference(this AssemblyDefinition assembly)
        {
            var @object = assembly.MainModule.TypeSystem.Object.Resolve();

            return @object.Methods.Single(
                m => m.Name == "Equals"
                    && m.Parameters.Count == 2
                    && m.Parameters[0].ParameterType.MetadataType == MetadataType.Object
                    && m.Parameters[1].ParameterType.MetadataType == MetadataType.Object);
        }
    }
}
