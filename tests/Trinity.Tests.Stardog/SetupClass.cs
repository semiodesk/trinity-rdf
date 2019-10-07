
using System.Reflection;
using System.IO;
using NUnit.Framework;


namespace Semiodesk.Trinity.Test.Stardog
{
    [SetUpFixture]
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

            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            OntologyDiscovery.AddAssembly(typeof(AbstractMappingClass).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(AbstractMappingClass).Assembly);

    
            //ConnectionString = _instance.GetTrinityConnectionString();
            //HostAndPort = _instance.Configuration.Parameters.ServerPort;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
           
        }

        #endregion
    }
}
