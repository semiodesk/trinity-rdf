// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015-2019

using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Configuration;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    class LegacyConfigurationTest
    {
        [Test]
        public void TestAppConfig()
        {
            IConfiguration config = ConfigurationLoader.LoadConfiguration(null);

            Assert.AreEqual("Semiodesk.Trinity.Test", config.Namespace);

            var ontologies = config.ListOntologies();

            Assert.AreEqual(7, ontologies.Count());

            var x = config.ListStoreConfigurations().ToList();

            Assert.IsNotNull(x.First().Data);
        }

        [Test]
        public void TestInitialize()
        {
            string connectionString = SetupClass.ConnectionString;

            var store = StoreFactory.CreateStore(string.Format("{0};rule=urn:semiodesk/test/ruleset", connectionString));
            store.InitializeFromConfiguration();
        }
    }
}
