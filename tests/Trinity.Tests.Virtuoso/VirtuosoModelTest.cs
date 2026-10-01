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

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Virtuoso
{
    [TestFixture]
    public class VirtuosoModelTest : ModelTest<VirtuosoTestSetup>
    {
        [Test]
        public override void TimeSpanResourceTest()
        {
            // Virtuoso 7 still has no support xsd:duration 10 years after this was reported as an issue:
            // https://sourceforge.net/p/virtuoso/mailman/virtuoso-users/thread/CAE94aYXGvk0bZr-sJhOM%2BtDpDaEmpUD-GxhTCrMg9ad0QODdLA%40mail.gmail.com/#msg31757337
             
            Assert.Inconclusive("Virtuoso does not support xsd:duration.");
        }

        /// <summary>
        /// Deleting a resource names it in the triple patterns rather than filtering every triple of the graph.
        /// </summary>
        /// <remarks>
        /// The answer is the same either way, which is why <see cref="ModelTest{T}.DeleteResourceTest"/> cannot
        /// tell them apart. The cost is not: the <c>FILTER (?s = &lt;r&gt; || ?o = &lt;r&gt;)</c> form scans
        /// the graph on every delete, 36 ms per delete at 64,000 resources against 0.8 ms bound.
        /// </remarks>
        [Test]
        public void DeleteResourceBindsTheResourceInsteadOfFiltering()
        {
            var resource = Model1.CreateResource(BaseUri.GetUriRef("deleted"));
            resource.AddProperty(new Property(BaseUri.GetUriRef("p")), "x");
            resource.Commit();

            var statements = new List<string>();

            Store.Log = statements.Add;

            try
            {
                Model1.DeleteResource(resource.Uri);
            }
            finally
            {
                Store.Log = null;
            }

            var delete = statements.Single(s => s.Contains("DELETE"));

            StringAssert.DoesNotContain("FILTER", delete);
            StringAssert.Contains(SparqlSerializer.SerializeUri(resource.Uri) + " ?p ?o", delete);
            StringAssert.Contains("?s ?q " + SparqlSerializer.SerializeUri(resource.Uri), delete);

            Assert.IsFalse(Model1.ContainsResource(resource.Uri));
        }
    }
}
