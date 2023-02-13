using System.Reflection;
using System.IO;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests
{
    [SetUpFixture]
    public class SetupClass
    {
        #region Methods

        [OneTimeSetUp]
        public virtual void Setup()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
        }

        [OneTimeTearDown]
        public virtual void TearDown()
        {
        }

        #endregion
    }
}
