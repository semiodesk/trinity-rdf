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
// Copyright (c) Semiodesk GmbH 2015-2020

using System;
using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests.Query.Sparql
{
    /// <summary>
    /// A type carrying two rdf:type constraints that differ only by fragment.
    /// </summary>
    /// <remarks>
    /// Fragment IRIs are the normal shape for RDF vocabulary terms, and no other test model uses one
    /// for a class -- which is how the defect below survived.
    /// </remarks>
    [RdfClass("http://example.org/frag#Alpha")]
    [RdfClass("http://example.org/frag#Beta")]
    public partial class AlphaBeta : Resource
    {
        public AlphaBeta(Uri uri) : base(uri) { }
    }

    /// <summary>
    /// The LINQ provider deduplicates the rdf:type constraints it collects for a mapped type. That
    /// dedupe used to run over a <c>List&lt;Uri&gt;</c>, so it compared with
    /// <c>EqualityComparer&lt;Uri&gt;.Default</c> -- fragment-blind, and unconditionally so on .NET 10.
    /// Two classes in the same namespace therefore collapsed into one and a constraint vanished from
    /// the generated query, which loosens it: resources carrying only one of the two types start
    /// matching.
    /// </summary>
    [TestFixture]
    public class FragmentTypeConstraintTest
    {
        private IStore _store;

        private IModel _model;

        [SetUp]
        public void SetUp()
        {
            MappingDiscovery.RegisterAssembly(typeof(AlphaBeta).Assembly);
            OntologyDiscovery.AddAssembly(typeof(AlphaBeta).Assembly);

            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            _model = _store.CreateModel(new Uri("http://example.org/frag"));
            _model.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
        }

        [Test]
        public void KeepsBothTypeConstraintsWhenTheyDifferOnlyByFragment()
        {
            // Carries both types, so it must match.
            IResource both = _model.CreateResource(new UriRef("http://example.org/frag#both"));
            both.AddProperty(rdf.type, new UriRef("http://example.org/frag#Alpha"));
            both.AddProperty(rdf.type, new UriRef("http://example.org/frag#Beta"));
            both.Commit();

            // Carries only the first, so it must not: the query asks for both types. If the two
            // constraints are deduplicated into one, this resource matches and the count is 2.
            IResource alphaOnly = _model.CreateResource(new UriRef("http://example.org/frag#alphaOnly"));
            alphaOnly.AddProperty(rdf.type, new UriRef("http://example.org/frag#Alpha"));
            alphaOnly.Commit();

            var matches = _model.AsQueryable<AlphaBeta>().ToList();

            Assert.AreEqual(1, matches.Count,
                "Both rdf:type constraints must survive; a fragment-blind dedupe drops one and over-matches.");
            Assert.AreEqual("http://example.org/frag#both", matches[0].Uri.OriginalString);
        }

        /// <summary>
        /// A regression guard, not a reproduction. <c>Resource.AddPropertyToMapping</c> deduplicates an
        /// incoming rdf:type against the class's declared types by comparing a UriRef with a Uri-typed
        /// local, which binds to Uri.operator == and ignores the fragment; that was corrected by
        /// inspection. This test does not fail without the correction -- rdf:type values reach the
        /// resource by another route as well -- so it pins the behaviour that must hold rather than
        /// demonstrating the defect.
        /// </summary>
        [Test]
        public void KeepsBothDeclaredTypesWhenReadingBack()
        {
            AlphaBeta resource = _model.CreateResource<AlphaBeta>(new UriRef("http://example.org/frag#r"));
            resource.Commit();

            AlphaBeta actual = _model.GetResource<AlphaBeta>(new UriRef("http://example.org/frag#r"));

            var types = actual.ListValues(rdf.type)
                .Select(v => v is IResource r ? r.Uri.OriginalString : v.ToString())
                .OrderBy(v => v)
                .ToList();

            Assert.AreEqual(2, types.Count, "Both rdf:type values must survive the read: " + string.Join(", ", types));
            CollectionAssert.Contains(types, "http://example.org/frag#Alpha");
            CollectionAssert.Contains(types, "http://example.org/frag#Beta");
        }

        /// <summary>
        /// Projecting the subject identifier used to throw "Unsupported projection: x.Uri". The
        /// projection was gated on <c>node.Type == typeof(Uri)</c>, but <c>Resource.Uri</c> is declared
        /// UriRef, so the exact type test never matched. The same class of defect as the three that made
        /// a UriRef-typed mapping unusable: a subclass fails an exact type identity check.
        /// </summary>
        [Test]
        public void ProjectsTheSubjectIdentifier()
        {
            AlphaBeta resource = _model.CreateResource<AlphaBeta>(new UriRef("http://example.org/frag#p"));
            resource.Commit();

            var uris = _model.AsQueryable<AlphaBeta>().Select(x => x.Uri).ToList();

            Assert.AreEqual(1, uris.Count);
            Assert.AreEqual("http://example.org/frag#p", uris[0].OriginalString);
            Assert.IsInstanceOf<UriRef>(uris[0], "The projected identifier must be fragment-aware.");
        }
    }
}
