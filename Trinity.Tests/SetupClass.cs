using Semiodesk.TinyVirtuoso;
using Semiodesk.Trinity.Store.Virtuoso;
using System.Reflection;
using System.IO;
using NUnit.Framework;

namespace Semiodesk.Trinity.Test
{
    [SetUpFixture]
    public class SetupClass
    {
        #region Members

        Virtuoso _instance;

        public static string ConnectionString;

        public static string HostAndPort;

        #endregion

        #region Methods

        [OneTimeSetUp]
        public void Setup()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            StoreFactory.LoadProvider(Assembly.GetAssembly(typeof(VirtuosoStoreProvider)));
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());

            FileInfo location = new FileInfo(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo folder = new DirectoryInfo(Path.Combine(location.DirectoryName, "nunit"));

            if (folder.Exists)
            {
                folder.Delete(true);
            }

            folder.Create();

            _instance = new TinyVirtuoso.TinyVirtuoso(folder).GetOrCreateInstance("NUnit");
            _instance.Start(true);

            ConnectionString = _instance.GetTrinityConnectionString();
            HostAndPort = _instance.Configuration.Parameters.ServerPort;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
           _instance.Stop();
        }

        #endregion
    }
}
