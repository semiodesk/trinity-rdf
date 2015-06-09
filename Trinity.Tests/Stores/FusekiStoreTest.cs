using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.Diagnostics;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class FusekiEndpointTest
    {

        public FusekiEndpointTest()
        {

        }

        [SetUp]
        public void SetUp()
        {

        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void ConnectTest()
        {
            ModelManager modelManager = ModelManager.Instance;
            modelManager.ConnectFuseki(new Uri("http://localhost:3030/ds/data"));
            IEnumerable<IModel> m = modelManager.ListFusekiModels();
        }

    }
}
