using NUnit.Framework;
using Semiodesk.Trinity.Store.Fuseki;

namespace Semiodesk.Trinity.Test.Fuseki
{
    [TestFixture]
    public class StoreProviderTest : SetupClass
    {

        [Test]
        public void FusekiConfigurationStringTest()
        {

            string connectionString = string.Format("provider=fuseki;host=http://localhost:3000;dataset=ds");
            IStore anObject = StoreFactory.CreateStore(connectionString);
            Assert.IsNotNull(anObject);
            Assert.IsInstanceOf<FusekiStore>(anObject);
            anObject.Dispose();
        }
    }
}
