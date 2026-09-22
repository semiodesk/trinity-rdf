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
// Copyright (c) Semiodesk GmbH 2026


using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Pins the SPARQL that <see cref="Model"/>, <see cref="ModelGroup"/> and a layered view
    /// actually emit for a bulk resource read.
    /// </summary>
    /// <remarks>
    /// The other tests for this pin <c>SparqlSerializer</c>'s output, which proves the helper emits
    /// <c>VALUES</c> — not that the models still call it. Building the subject constraint inline
    /// instead is exactly how the equality chain existed before ADR-0046, and the store-level test
    /// cannot see it: with batching at 1000, a 2000-subject read is two queries of 1000, and a
    /// 1000-term equality chain still compiles on Virtuoso. So this captures the query text.
    /// </remarks>
    [TestFixture]
    public class BulkResourceQueryShapeTest
    {
        private CapturingStore _store;

        private static readonly Uri Model1Uri = new Uri("http://example.org/shape/model1");
        private static readonly Uri Model2Uri = new Uri("http://example.org/shape/model2");

        [SetUp]
        public void SetUp()
        {
            _store = new CapturingStore(StoreFactory.CreateStore("provider=dotnetrdf"));

            _store.GetModel(Model1Uri).Clear();
            _store.GetModel(Model2Uri).Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
        }

        /// <summary>
        /// Constructed against the capturing store directly. <c>store.GetModel(uri)</c> would not do:
        /// the inner store builds the model with <i>itself</i>, so the decorator never sees the query.
        /// </summary>
        private IModel Model(Uri uri)
        {
            return new Model(_store, uri.ToUriRef());
        }

        private IModelGroup Group()
        {
            return new ModelGroup(_store, Model(Model1Uri), Model(Model2Uri));
        }

        private ILayeredModel View()
        {
            return _store.CreateLayeredModel(
                new Uri("http://example.org/shape/base"),
                new Uri("http://example.org/shape/add"),
                new Uri("http://example.org/shape/rem"));
        }

        private static List<Uri> Subjects(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => (Uri)new UriRef("http://example.org/shape/r" + i))
                .ToList();
        }

        private void AssertBoundWithValues(int expectedQueries)
        {
            Assert.AreEqual(expectedQueries, _store.Queries.Count,
                "one query per batch of " + SparqlSerializer.SubjectBindingBatchSize);

            foreach (string sparql in _store.Queries)
            {
                StringAssert.Contains("VALUES ?s", sparql,
                    "subjects must be bound with VALUES, not constrained afterwards:\n" + sparql);

                Assert.IsFalse(sparql.Contains("||"),
                    "an equality chain is what Virtuoso refuses past a nesting depth (SP031):\n" + sparql);

                Assert.IsTrue(new SparqlQuery(sparql).ProvidesStatements(),
                    "or resource materialization refuses the query outright:\n" + sparql);
            }
        }

        [Test]
        public void ModelBindsSubjectsWithValues()
        {
            Model(Model1Uri).GetResources(Subjects(10), typeof(Resource)).ToList();

            AssertBoundWithValues(1);
        }

        [Test]
        public void ModelGroupBindsSubjectsWithValues()
        {
            Group().GetResources(Subjects(10), typeof(Resource)).ToList();

            AssertBoundWithValues(1);
        }

        /// <summary>
        /// A layered view reads through the same path — <c>Attach</c> makes the view the resource's
        /// model, so a mapped-collection dereference lands here — and it was the implementation that
        /// kept issuing a single unbounded block after ADR-0046 fixed the other two.
        /// </summary>
        [Test]
        public void LayeredModelBindsSubjectsWithValues()
        {
            var view = View();

            _store.Queries.Clear();

            view.GetResources(Subjects(10), typeof(Resource)).ToList();

            Assert.IsNotEmpty(_store.Queries);

            foreach (string sparql in _store.Queries)
            {
                StringAssert.Contains("VALUES ?s", sparql, sparql);
                Assert.IsFalse(sparql.Contains("||"), sparql);
            }
        }

        /// <summary>
        /// Batching is per model, not per caller: every implementation must split a subject set
        /// larger than one block, because VALUES is bounded too (Virtuoso SP030 at ~4095 operands).
        /// </summary>
        [Test]
        public void EveryModelBatchesBeyondOneBlock()
        {
            int count = SparqlSerializer.SubjectBindingBatchSize + 1;

            Model(Model1Uri).GetResources(Subjects(count), typeof(Resource)).ToList();
            AssertBoundWithValues(2);

            _store.Queries.Clear();
            Group().GetResources(Subjects(count), typeof(Resource)).ToList();
            AssertBoundWithValues(2);

            var view = View();

            _store.Queries.Clear();
            view.GetResources(Subjects(count), typeof(Resource)).ToList();

            Assert.AreEqual(2, _store.Queries.Count,
                "a layered view must batch like the other two, or a large collection hits SP030");
        }

        /// <summary>
        /// A blank node among many is skipped, on every implementation. Refusing the whole call — as
        /// a layered view used to — loses every addressable subject with it, which on the lazy-load
        /// path makes one blank member of a mapped collection hide the entire collection.
        /// </summary>
        [Test]
        public void EveryModelSkipsABlankSubjectRatherThanRefusingTheRead()
        {
            var subjects = new List<Uri>
            {
                new UriRef("http://example.org/shape/r0"),
                new UriRef("_:b0", true),
                new UriRef("http://example.org/shape/r1")
            };

            var view = View();

            Assert.DoesNotThrow(() => Model(Model1Uri).GetResources(subjects, typeof(Resource)).ToList());
            Assert.DoesNotThrow(() => Group().GetResources(subjects, typeof(Resource)).ToList());
            Assert.DoesNotThrow(() => view.GetResources(subjects, typeof(Resource)).ToList());

            foreach (string sparql in _store.Queries)
            {
                Assert.IsFalse(sparql.Contains("_:"), "no blank node label may reach the query text:\n" + sparql);
            }
        }

        /// <summary>
        /// Asking for <i>one</i> blank node by identity stays an error: the caller named exactly that
        /// resource, so silently returning nothing would answer a question they did not ask.
        /// </summary>
        /// <remarks>
        /// Every single-resource accessor, on <b>every</b> implementation. `ModelGroup` had no guard at
        /// all, and its `ContainsResource` interpolates the identifier into a triple pattern — where a
        /// bare `_:b0` is not a reference but a fresh existential variable, so it matched any subject
        /// with any property and answered <c>true</c> for any non-empty group. A silently wrong answer,
        /// which is worse than the half-working capability the contract exists to refuse.
        /// </remarks>
        [Test]
        public void EverySingleResourceAccessorRefusesABlankNode()
        {
            var blank = new UriRef("_:b0", true);

            // A triple, so ContainsResource has something a bare label could match against.
            var seed = Model(Model1Uri).CreateResource(new UriRef("http://example.org/shape/seed"));
            seed.AddProperty(new Property(new Uri("http://example.org/shape/p")), "v");
            seed.Commit();

            foreach (var target in new (string, Func<Uri, object>)[]
            {
                ("Model.ContainsResource",        u => Model(Model1Uri).ContainsResource(u)),
                ("Model.GetResource",             u => Model(Model1Uri).GetResource(u)),
                ("Model.GetResource<T>",          u => Model(Model1Uri).GetResource<Resource>(u)),
                ("ModelGroup.ContainsResource",   u => Group().ContainsResource(u)),
                ("ModelGroup.GetResource",        u => Group().GetResource(u)),
                ("ModelGroup.GetResource<T>",     u => Group().GetResource<Resource>(u)),
                ("LayeredModel.ContainsResource", u => View().ContainsResource(u)),
                ("LayeredModel.GetResource",      u => View().GetResource(u)),
                ("LayeredModel.GetResource<T>",   u => View().GetResource<Resource>(u)),
            })
            {
                Assert.Throws<ArgumentException>(() => target.Item2(blank), target.Item1
                    + " must refuse a blank node, not answer a different question");
            }
        }

        /// <summary>
        /// A null identifier is a null identifier, not a blank node. Telling a caller their URI is a
        /// blank node when they passed nothing sends them looking in the wrong place.
        /// </summary>
        [Test]
        public void EverySingleResourceAccessorReportsANullUriAsSuch()
        {
            foreach (var target in new (string, Func<Uri, object>)[]
            {
                ("Model.ContainsResource",        u => Model(Model1Uri).ContainsResource(u)),
                ("Model.GetResource",             u => Model(Model1Uri).GetResource(u)),
                ("Model.GetResource<T>",          u => Model(Model1Uri).GetResource<Resource>(u)),
                ("ModelGroup.ContainsResource",   u => Group().ContainsResource(u)),
                ("ModelGroup.GetResource",        u => Group().GetResource(u)),
                ("ModelGroup.GetResource<T>",     u => Group().GetResource<Resource>(u)),
                ("LayeredModel.ContainsResource", u => View().ContainsResource(u)),
                ("LayeredModel.GetResource",      u => View().GetResource(u)),
                ("LayeredModel.GetResource<T>",   u => View().GetResource<Resource>(u)),
            })
            {
                Assert.Throws<ArgumentNullException>(() => target.Item2(null), target.Item1
                    + " must report a null URI as null, not as a blank node");
            }
        }
    }
}
