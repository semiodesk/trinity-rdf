using NUnit.Framework;
using Semiodesk.Trinity.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    class LinqTest
    {
        [Test]
        public void LinqTest1()
        {
            IStore s = StoreFactory.CreateStore("provider=dotnetrdf");
            IModel model = s.CreateModel(new Uri("http://test.com/test"));
            var x = from res in model.ListResources<MappingTestClass>() where res.uniqueIntTest < 5 select res;
            var ret = x.ToList();
        }

        [Test]
        public void LinqTest2()
        {
            IStore s = StoreFactory.CreateStore("provider=dotnetrdf");
            IModel model = s.CreateModel(new Uri("http://test.com/test"));
            var x = from res in model.ListResources<MappingTestClass>() where res.uniqueIntTest < 5 && res.uniqueResourceTest.uniqueStringTest == "abc" select res;
            var ret = x.ToList();
        }
    }
}
