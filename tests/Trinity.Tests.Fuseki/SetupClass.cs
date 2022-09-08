using System.Reflection;
using System.IO;
using NUnit.Framework;
using Semiodesk.Trinity.Store.Fuseki;

namespace Semiodesk.Trinity.Test.Fuseki
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


            ConnectionString = "provider=fuseki;host=http://localhost:3030;dataset=ds";
            
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
          
        }

        #endregion
    }
}
