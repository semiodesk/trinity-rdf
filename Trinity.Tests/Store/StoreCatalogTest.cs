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
// Copyright (c) Semiodesk GmbH 2023

using System;
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Store-level model-catalog behaviour: the members that answer "does this store hold this
    /// graph?". Store-independent, because every backend answers it differently — a HEAD request, a
    /// graph listing, an ASK — and nothing shared held them to the same contract.
    /// </summary>
    /// <remarks>
    /// This lived in a Fuseki-only fixture until ADR-0043. That is how the Fuseki
    /// <c>ContainsModel(Uri)</c> defect survived — it asked the server and discarded the answer,
    /// returning <c>false</c> unconditionally — and how Virtuoso kept an unguarded
    /// <c>ContainsModel(IModel)</c> that threw on null while every other store returned <c>false</c>.
    /// </remarks>
    [TestFixture]
#pragma warning disable CS0618 // the members under test are themselves obsolete
    public abstract class StoreCatalogTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        /// <summary>
        /// A graph holding a triple is present; an unrelated graph is not.
        /// </summary>
        [Test]
        public virtual void ContainsModelReportsPresenceAndAbsence()
        {
            var absent = BaseUri.GetUriRef("catalog-absent");

            Assert.IsFalse(Store.ContainsModel(absent),
                "a graph that was never written must be reported absent");

            var present = BaseUri.GetUriRef("catalog-present");
            var model = Store.GetModel(present);

            model.Clear();

            var resource = model.CreateResource(BaseUri.GetUriRef("catalog-r1"));
            resource.AddProperty(new Property(BaseUri.GetUriRef("catalog-p1")), "a value");
            resource.Commit();

            try
            {
                Assert.IsTrue(Store.ContainsModel(present),
                    "a graph that holds a triple must be reported present");
                Assert.IsFalse(Store.ContainsModel(absent),
                    "an unrelated graph must still be reported absent");
            }
            finally
            {
                model.Clear();
            }
        }

        /// <summary>
        /// The two overloads must agree, and neither may throw on null.
        /// </summary>
        [Test]
        public virtual void ContainsModelTreatsNullAsAbsentRatherThanThrowing()
        {
            Assert.IsFalse(Store.ContainsModel((Uri)null));
            Assert.IsFalse(Store.ContainsModel((IModel)null));
        }

        /// <summary>
        /// A graph is addressed by exactly the IRI it was named with, whichever path reaches it.
        /// </summary>
        /// <remarks>
        /// RDF compares IRIs as strings, so <c>http://Example.org/g</c> and <c>http://example.org/g</c>
        /// are two graphs. <see cref="Uri.AbsoluteUri"/> is not the IRI: it lower-cases the host and
        /// re-escapes the path. A Graph Store Protocol write that names the graph by
        /// <c>AbsoluteUri</c> while the SPARQL path uses <c>OriginalString</c> puts the data where
        /// neither SPARQL nor <see cref="IStore.ListModels"/> will find it under the caller's name.
        /// </remarks>
        [Test]
        public virtual void AGraphIsAddressedByTheExactIriItWasNamedWith()
        {
            var graph = new UriRef("http://Example.org/trinity/CatalogCase");
            var subject = new UriRef("http://example.org/trinity/catalog-case-subject");

            try
            {
                // A Read goes through the Graph Store path on the HTTP backends...
                Store.Read($"<{subject}> <http://example.org/p> \"v\" .", graph, RdfSerializationFormat.Turtle, false);

                // ...and ContainsResource through SPARQL.
                Assert.IsTrue(Store.GetModel(graph).ContainsResource(subject),
                    "SPARQL must find what the Graph Store write put under this exact name");
                Assert.IsTrue(Store.ListModels().Any(m => m.Uri.OriginalString == graph.OriginalString),
                    "ListModels must report the name as it was written");
            }
            finally
            {
                Store.GetModel(graph).Clear();

                // Where a broken write actually put it, so a failure here does not leak into other tests.
                Store.GetModel(new Uri(graph.AbsoluteUri)).Clear();
            }
        }
    }
#pragma warning restore CS0618
}
