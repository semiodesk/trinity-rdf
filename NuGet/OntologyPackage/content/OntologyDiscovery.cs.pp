using System;
using System.Reflection;
using Semiodesk.Trinity;

namespace $DefaultNameSpace$
{
    public static class SemiodeskDiscovery
    {
        public static void Discover()
        {
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterCallingAssembly();
        }
    }
}
