using Semiodesk.TinyVirtuoso;
using Semiodesk.Trinity.Store.Virtuoso;
using System.Reflection;
using System.IO;
using NUnit.Framework;

namespace Semiodesk.Trinity.Test.Virtuoso
{

    public class SetupClass
    {
        #region Members
        public static string ConnectionString;

        public static string HostAndPort;

        #endregion

        #region Methods

        [OneTimeSetUp]
        public void Setup()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            StoreFactory.LoadProvider<VirtuosoStoreProvider>();
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


            ConnectionString = "provider=virtuoso;host=127.0.0.1;port=1111;uid=dba;pw=dba";
            HostAndPort = "localhost:1111";
        }

        [OneTimeTearDown]
        public void TearDown()
        {
          
        }

        #endregion
    }
}
