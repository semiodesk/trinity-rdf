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
using System.Reflection;

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
        /// Every <see cref="IModel"/> method that names a caller-supplied identifier as a query
        /// subject must refuse a blank node, on every implementation.
        /// </summary>
        /// <remarks>
        /// <b>The set of methods is discovered, not listed.</b> An earlier version of this test
        /// enumerated nine accessors by hand and was green while a tenth — <c>GetResource(Uri, Type)</c>
        /// — went unguarded on all three models: it reaches the guard only by reflectively invoking
        /// <c>GetResource&lt;T&gt;</c>, so what came back was a <see cref="TargetInvocationException"/>
        /// that a caller writing <c>catch (ArgumentException)</c> would not catch. A hand-maintained
        /// list cannot detect its own omission, which is the same failure mode as the three duplicated
        /// implementations this branch removed, one level up.
        /// <para>
        /// So the sweep reflects over <see cref="IModel"/> and asserts every method whose first
        /// parameter is a <see cref="Uri"/>, minus an explicit exclusion list with a reason for each.
        /// A method added to the interface is therefore asserted <i>by default</i> and fails this test
        /// until it is either guarded or excluded deliberately.
        /// </para>
        /// </remarks>
        [TestCaseSource(nameof(QuerySubjectAccessors))]
        public void EveryQuerySubjectAccessorRefusesABlankNode(string label, Func<IModel, Uri, object> call)
        {
            // A triple, so an accessor that puts a bare label in pattern position has something to
            // match against - that is how ModelGroup.ContainsResource used to answer true.
            var seed = Model(Model1Uri).CreateResource(new UriRef("http://example.org/shape/seed"));
            seed.AddProperty(new Property(new Uri("http://example.org/shape/p")), "v");
            seed.Commit();

            var blank = new UriRef("_:b0", true);

            foreach (var model in Models())
            {
                Assert.Throws<ArgumentException>(() => call(model.Item2, blank),
                    $"{model.Item1}.{label} must refuse a blank node, not answer a different question");
            }
        }

        /// <summary>
        /// A null identifier is a null identifier, not a blank node. Telling a caller their URI is a
        /// blank node when they passed nothing sends them looking in the wrong place.
        /// </summary>
        [TestCaseSource(nameof(QuerySubjectAccessors))]
        public void EveryQuerySubjectAccessorReportsANullUriAsSuch(string label, Func<IModel, Uri, object> call)
        {
            foreach (var model in Models())
            {
                Assert.Throws<ArgumentNullException>(() => call(model.Item2, null),
                    $"{model.Item1}.{label} must report a null URI as null, not as a blank node");
            }
        }

        private IEnumerable<Tuple<string, IModel>> Models()
        {
            yield return Tuple.Create("Model", Model(Model1Uri));
            yield return Tuple.Create("ModelGroup", (IModel)Group());
            yield return Tuple.Create("LayeredModel", (IModel)View());
        }

        /// <summary>
        /// The <see cref="IModel"/> methods that take a caller-supplied identifier and name it as a
        /// query subject — discovered, so a new one is covered without anyone remembering to add it.
        /// </summary>
        public static IEnumerable<TestCaseData> QuerySubjectAccessors()
        {
            // Excluded deliberately, each for a reason. Anything not named here and taking a Uri first
            // is asserted, so a new accessor fails until this decision is made for it.
            var excluded = new Dictionary<string, string>
            {
                // Creating a blank node is done *by* passing a blank identifier, so these must accept one.
                { "CreateResource", "blank identifiers are how a blank node is created" },
                // A write path. Refusing here would prevent deleting a blank node, which some stores can
                // do -- a separate decision from naming one as a query subject.
                { "DeleteResource", "write path; see ADR-0046" },
                // The Uri is a source URL to read *from*, not a subject.
                { "Read", "the Uri is a document location, not a resource identifier" },
            };

            var covered = new List<string>();

            foreach (MethodInfo method in typeof(IModel).GetMethods())
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(Uri))
                {
                    continue;
                }

                if (excluded.ContainsKey(method.Name))
                {
                    continue;
                }

                covered.Add(method.Name);

                MethodInfo target = method.IsGenericMethodDefinition
                    ? method.MakeGenericMethod(typeof(Resource))
                    : method;

                object[] tail = target.GetParameters()
                    .Skip(1)
                    .Select(p => p.ParameterType == typeof(Type) ? (object)typeof(Resource) : null)
                    .ToArray();

                string label = target.Name + "(" + string.Join(", ", target.GetParameters().Select(p => p.ParameterType.Name)) + ")";

                yield return new TestCaseData(label, new Func<IModel, Uri, object>((model, uri) =>
                {
                    try
                    {
                        return target.Invoke(model, new object[] { uri }.Concat(tail).ToArray());
                    }
                    catch (TargetInvocationException e)
                    {
                        // Unwrap only what reflection added. A guard that is reached transitively -- via
                        // an inner reflective call -- still fails, because the exception the *caller*
                        // sees is the wrapper, and that is what this asserts against.
                        throw e.InnerException;
                    }
                }));
            }

            Assert.IsNotEmpty(covered, "the sweep discovered no accessors, which means it is not sweeping");
        }

    }
}
