using System.Reflection;
using System.IO;
using NUnit.Framework;
using Semiodesk.Trinity.Store.Fuseki;
using Semiodesk.Trinity.Tests;

namespace Semiodesk.Trinity.Tests.Fuseki
{

    public class SetupClass
    {
        #region Members
        public static string ConnectionString;

        #endregion

        #region Methods

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            StoreFactory.LoadProvider<FusekiStoreProvider>();
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
            OntologyDiscovery.AddAssembly(typeof(AbstractMappingClass).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(AbstractMappingClass).Assembly);

            FileInfo location = new FileInfo(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo folder = new DirectoryInfo(Path.Combine(location.DirectoryName, "nunit"));

            if (folder.Exists)
            {
                folder.Delete(true);
            }

            folder.Create();

            // ConnectionString is set by the FusekiContainer [SetUpFixture], which starts the
            // Dockerized server on a random host port before any fixture runs (ADR-0036).
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
        }

        #endregion
    }
}
