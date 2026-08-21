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
    }
#pragma warning restore CS0618
}
