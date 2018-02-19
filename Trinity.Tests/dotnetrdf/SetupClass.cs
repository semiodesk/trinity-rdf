using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semiodesk.TinyVirtuoso;
using System.Reflection;
using System.IO;
using Semiodesk.Trinity.Store;

namespace dotNetRDFStore.Test
{
    [SetUpFixture]
    public class SetupClass
    {


        [OneTimeSetUp]
        public void Setup()
        {

            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

        }
    }
}
