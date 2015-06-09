using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.Reflection;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class OntologyTest
    {
        [Test]
        public void TestDiscovery()
        {
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            var l = OntologyDiscovery.Classes;
        }
    }
}
