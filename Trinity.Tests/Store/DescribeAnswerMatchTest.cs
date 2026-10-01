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
    /// Pins which resource <c>Model.GetResource&lt;T&gt;</c> takes from a <c>DESCRIBE</c> answer that holds
    /// more than the subject.
    /// </summary>
    /// <remarks>
    /// The shared <c>ResourceMappingTest</c> covers this against real stores, but a store decides the order of
    /// its answer, and the case here needs a particular one: a referrer that is <see cref="Uri"/>-equal to the
    /// subject, listed first. So the answer is a list handed to <c>Model</c> through
    /// <see cref="CapturingStore.Answer"/>.
    /// <para>
    /// It is a list, not a dotNetRDF graph, because a graph cannot carry the case. Measured: dotNetRDF compares
    /// nodes by <see cref="Uri"/> equality, so asserting <c>&lt;http://example.org/x&gt; a C</c> after
    /// <c>&lt;http://example.org:80/x&gt; a C</c> is dropped as a duplicate, and the result reader then builds a
    /// single resource from both subjects' triples. Its Turtle parser also rewrites the port away on parse. So
    /// for every store whose answer passes through a dotNetRDF graph (in-memory, Fuseki, GraphDB, Oxigraph),
    /// such IRIs are merged before <c>Model</c> sees them. The exact match decides for answers that keep them
    /// apart, such as Virtuoso's own result reader.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class DescribeAnswerMatchTest
    {
        private CapturingStore _store;

        private static readonly Uri ModelUri = new Uri("http://example.org/describe/model");

        /// <summary>
        /// A query result that answers with the given resources, in the given order.
        /// </summary>
        private class ListedAnswer : ISparqlQueryResult
        {
            private readonly List<Resource> _resources;

            public ListedAnswer(params Resource[] resources)
            {
                _resources = resources.ToList();
            }

            public IEnumerable<T> GetResources<T>() where T : Resource => _resources.OfType<T>();
            public IEnumerable<T> GetResources<T>(int offset = -1, int limit = -1) where T : Resource => GetResources<T>();
            public IEnumerable<Resource> GetResources() => _resources;
            public IEnumerable<Resource> GetResources(int offset = -1, int limit = -1) => _resources;
            public IEnumerable<Resource> GetResources(Type type) => _resources.Where(type.IsInstanceOfType);
            public int Count() => _resources.Count;
            public bool GetAnwser() => throw new NotSupportedException();
            public IEnumerable<BindingSet> GetBindings() => throw new NotSupportedException();
            public void Dispose() { }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
        }

        [SetUp]
        public void SetUp()
        {
            _store = new CapturingStore(StoreFactory.CreateStore("provider=dotnetrdf"));
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
        }

        private void AnswerDescribeWith(params Resource[] resources)
        {
            _store.Answer = q => q.QueryType == SparqlQueryType.Describe ? new ListedAnswer(resources) : null;
        }

        private static MappingTestClass Labelled(UriRef uri, string label)
        {
            return new MappingTestClass(uri) { uniqueStringTest = label };
        }

        /// <summary>
        /// The subject spelled exactly as asked wins over a referrer that is only <see cref="Uri"/>-equal to
        /// it, even when the answer lists the referrer first.
        /// </summary>
        /// <remarks>
        /// The match falls back to <see cref="UriRef"/> equality because the in-memory store and Fuseki hand a
        /// subject back with its host lower-cased. But Uri equality also ignores default ports, dot-segments
        /// and percent-encoding, which RDF identity does not, so the fallback must not decide while an exact
        /// match exists. GraphDB's answer includes the incoming triples, so a referrer such as
        /// <c>&lt;http://example.org:80/x&gt; owl:sameAs &lt;http://example.org/x&gt;</c> can be in it.
        /// </remarks>
        [Test]
        public void AnExactMatchWinsOverAUriEqualReferrerListedFirst()
        {
            var subject = new UriRef("http://example.org/describe/x");
            var referrer = new UriRef("http://example.org:80/describe/x");

            Assert.IsTrue(subject.Equals(referrer), "the fixture needs a referrer that is Uri-equal to the subject");

            AnswerDescribeWith(Labelled(referrer, "referrer"), Labelled(subject, "subject"));

            var actual = new Model(_store, ModelUri.ToUriRef()).GetResource<MappingTestClass>(subject);

            Assert.AreEqual(subject.OriginalString, actual.Uri.OriginalString);
            Assert.AreEqual("subject", actual.uniqueStringTest);
        }

        /// <summary>
        /// Without an exact match, the subject handed back with its host lower-cased is still found.
        /// </summary>
        [Test]
        public void AHostCaseVariantIsFoundWhenThereIsNoExactMatch()
        {
            var subject = new UriRef("http://Example.org/describe/y");
            var returned = new UriRef("http://example.org/describe/y");

            AnswerDescribeWith(Labelled(new UriRef("http://example.org/describe/referrer"), "referrer"), Labelled(returned, "subject"));

            var actual = new Model(_store, ModelUri.ToUriRef()).GetResource<MappingTestClass>(subject);

            Assert.AreEqual("subject", actual.uniqueStringTest);
        }
    }
}
