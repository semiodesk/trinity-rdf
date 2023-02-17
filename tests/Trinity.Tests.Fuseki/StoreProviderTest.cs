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
            const string connectionString = "provider=fuseki;host=http://localhost:3030;uid=admin;pw=test;dataset=ds";
            
            var store = StoreFactory.CreateStore(connectionString);
            
            Assert.IsNotNull(store);
            Assert.IsInstanceOf<FusekiStore>(store);
            Assert.IsTrue(store.IsReady);
            
            store.Dispose();
        }
    }
}
