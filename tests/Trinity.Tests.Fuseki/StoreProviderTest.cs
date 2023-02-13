using NUnit.Framework;
using Semiodesk.Trinity.Store.Fuseki;

namespace Semiodesk.Trinity.Tests.Fuseki
{
    [TestFixture]
    public class StoreProviderTest : SetupClass
    {
        [Test]
        public void FusekiConfigurationStringTest()
        {
            var connectionString = "provider=fuseki;host=http://localhost:3000;dataset=ds";
            var store = StoreFactory.CreateStore(connectionString);
            
            Assert.IsNotNull(store);
            Assert.IsInstanceOf<FusekiStore>(store);
            
            store.Dispose();
        }
    }
}
