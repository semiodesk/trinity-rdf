using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity.Store;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    class StoreProviderTest
    {
        [Test]
        public void ParseConfigurationTest()
        {
            Dictionary<string, string> result = Stores.ParseConfiguration("provider=virtuoso");
            Assert.AreEqual("virtuoso", result["provider"]);

            result = Stores.ParseConfiguration("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");
            Assert.AreEqual("localhost", result["host"]);
            Assert.AreEqual("1111", result["port"]);
            Assert.AreEqual("dba", result["uid"]);
            Assert.AreEqual("dba", result["pw"]);

            result = Stores.ParseConfiguration("provider=virtuoso;path=c:/path/to/my/files.ext");
            Assert.AreEqual("c:/path/to/my/files.ext", result["path"]);

            result = Stores.ParseConfiguration("provider=dotnetrdf;schema=c:/path/to/my/schema1.rdf,c:/path/to/my/schema2.rdf");

        }

        [Test]
        public void DotNetRDFConfigTest()
        {
          dotNetRDFStoreProvider p = new dotNetRDFStoreProvider();
          p.GetStore(Stores.ParseConfiguration("provider=dotnetrdf;schema=Models/rdf-schema.rdf,Models/rdf-syntax.rdf"));
        }

        [Test]
        public void VirtuosoConfigurationStringTest()
        {
            IStore anObject = Stores.CreateStore("provider=virtuoso");
            Assert.IsNotNull(anObject);
            anObject.Dispose();

            anObject = Stores.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");
            Assert.IsNotNull(anObject);
            anObject.Dispose();
        }


    }
}
