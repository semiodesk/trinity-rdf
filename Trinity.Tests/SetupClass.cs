using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semiodesk.TinyVirtuoso;
using System.Reflection;

namespace Semiodesk.Trinity.Test
{
    [SetUpFixture]
    public class SetupClass
    {
        TinyVirtuoso.TinyVirtuoso virtuoso;
        Virtuoso instance;
        public static string ConnectionString;
        public static string HostAndPort;

        [SetUp]
        public void Setup()
        {
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());

            TinyVirtuoso.TinyVirtuoso v = new TinyVirtuoso.TinyVirtuoso(Environment.CurrentDirectory);
            instance = v.GetOrCreateInstance("NUnit");
            instance.Start(true);
            ConnectionString = instance.GetTrinityConnectionString();
            HostAndPort = instance.Configuration.Parameters.ServerPort;
        }

        [TearDown]
        public void TearDown()
        {
            instance.Stop();
        }

    }
}
