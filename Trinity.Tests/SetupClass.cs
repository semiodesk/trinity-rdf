using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semiodesk.TinyVirtuoso;
using System.Reflection;
using System.IO;

namespace Semiodesk.Trinity.Test
{
    [SetUpFixture]
    public class SetupClass
    {
        Virtuoso instance;

        public static string ConnectionString;

        public static string HostAndPort;

        [OneTimeSetUp]
        public void Setup()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());

            FileInfo i = new FileInfo(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(i.DirectoryName, "nunit"));

            TinyVirtuoso.TinyVirtuoso v = new TinyVirtuoso.TinyVirtuoso(dir);
            instance = v.GetOrCreateInstance("NUnit");
            instance.Start(true);
            ConnectionString = instance.GetTrinityConnectionString();
            HostAndPort = instance.Configuration.Parameters.ServerPort;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            instance.Stop();
        }

    }
}
