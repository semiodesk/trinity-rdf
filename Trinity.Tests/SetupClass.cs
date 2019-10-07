using Semiodesk.TinyVirtuoso;

using System.Reflection;
using System.IO;
using NUnit.Framework;

namespace Semiodesk.Trinity.Test
{
    [SetUpFixture]
    public class SetupClass
    {
        #region Members

        #endregion

        #region Methods

        [OneTimeSetUp]
        public void Setup()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());

        }

        [OneTimeTearDown]
        public void TearDown()
        {
       
        }

        #endregion
    }
}
