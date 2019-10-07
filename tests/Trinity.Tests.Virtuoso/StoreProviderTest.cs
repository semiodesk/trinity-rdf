using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semiodesk.Trinity.Test.Virtuoso
{
    [TestFixture]
    public class StoreProviderTest : SetupClass
    {

        [Test]
        public void VirtuosoConfigurationStringTest()
        {
            string[] components = SetupClass.HostAndPort.Split(':');
            string host = components[0];
            string port = components[1];
            string connectionString = string.Format("provider=virtuoso;host={0};port={1};uid=dba;pw=dba;rule=urn:semiodesk/test/ruleset", host, port);
            IStore anObject = StoreFactory.CreateStore(connectionString);
            Assert.IsNotNull(anObject);
            anObject.Dispose();
        }
    }
}
