using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    class LinqTest
    {

        [SetUp]
        public void SetUp()
        {
            if (ResourceMappingTest.RegisteredOntology == false)
            {
                OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
                MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
                RegisterOntologies.Register();
                ResourceMappingTest.RegisteredOntology = true;
            }

        }

        [Test]
        public void LinqTest1()
        {
         
            IStore s = StoreFactory.CreateStore("provider=dotnetrdf");
            IModel model = s.CreateModel(new Uri("http://test.com/test"));
            var x = from res in model.ListResources<MappingTestClass>() where res.uniqueIntTest < 5select res;
            var ret = x.ToList();
        }

        [Test]
        public void LinqTest2()
        {
            string b = "blub";
            IStore s = StoreFactory.CreateStore("provider=dotnetrdf");
            IModel model = s.CreateModel(new Uri("http://test.com/test"));
            var x = from res in model.ListResources<MappingTestClass>() where res.uniqueIntTest < 5 && res.uniqueResourceTest.uniqueStringTest == "abc" && res.uniqueResourceTest.uniqueStringTest == "Test" || res.uniqueStringTest == b select res;
            var ret = x.ToList();
        }
    }
}
