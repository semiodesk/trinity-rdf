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

using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class ModelGroupTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members
        
        protected UriRef R1;
        
        #endregion
        
        #region Methods
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            R1 = BaseUri.GetUriRef("r1");
        }
        
        [Test]
        public virtual void ContainsResourceTest()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);
            
            var u1 = BaseUri.GetUriRef("r1");

            Assert.IsFalse(group.ContainsResource(u1));

            var r1 = Model1.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();
            
            Assert.IsTrue(group.ContainsResource(u1));

            Model1.DeleteResource(r1);

            Assert.IsFalse(group.ContainsResource(u1));

            r1 = Model2.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            Assert.IsTrue(group.ContainsResource(u1));
        }
        
        [Test]
        public virtual void DeleteResourceTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            Assert.IsTrue(Model1.ContainsResource(R1));

            Model1.DeleteResource(r1);

            Assert.IsFalse(Model1.ContainsResource(R1));
        }
        
        [Test]
        public virtual void GetResourceTest()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);
            
            var u1 = BaseUri.GetUriRef("r1");
            
            Assert.Throws<ResourceNotFoundException>(() => group.GetResource(u1));
            
            var r1 = Model1.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            var gr1 = group.GetResource(u1);
            
            Assert.IsNotNull(gr1);
            Assert.IsTrue(gr1.IsReadOnly);
            Assert.AreEqual(u1, gr1.Uri);
            Assert.Contains(nco.Contact, gr1.ListValues(rdf.type).ToList());

            r1 = Model2.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            gr1 = group.GetResource(u1);
            
            Assert.IsNotNull(gr1);
            Assert.IsTrue(gr1.IsReadOnly);
            Assert.AreEqual(u1, gr1.Uri);
            Assert.AreEqual(1, gr1.ListValues(rdf.type).Count());
            Assert.Contains(nco.Contact, gr1.ListValues(rdf.type).ToList());
        }

        [Test]
        public virtual void LazyLoadResourceTest()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);
            
            var u1 = BaseUri.GetUriRef("r1");
            var u2 = BaseUri.GetUriRef("r2");

            var r2 = Model1.CreateResource<MappingTestClass2>(u2);
            
            var r1 = Model1.CreateResource<MappingTestClass>(u1);
            r1.uniqueResourceTest = r2; // TODO: Debug message, because t2 was not committed
            r1.Commit();

            var gr1 = group.GetResource<MappingTestClass>(u1);
            var gr2 = gr1.ListValues(to.uniqueResourceTest).OfType<Resource>().First();
            
            Assert.AreEqual(r2.Uri.OriginalString, gr2.Uri.OriginalString);

            Model1.DeleteResource(r1);
            Model1.DeleteResource(r2);

            r2 = Model1.CreateResource<MappingTestClass2>(u2);
            r2.Commit();
            
            r1 = Model1.CreateResource<MappingTestClass>(u1);
            r1.uniqueResourceTest = r2;
            r1.Commit();

            gr1 = group.GetResource<MappingTestClass>(u1);
            
            Assert.AreEqual(r2, gr1.uniqueResourceTest);
            
            var x1 = group.GetResource(u1);
            
            Assert.AreEqual(typeof(MappingTestClass), x1.GetType());
        }
        
        #endregion

        /// <summary>
        /// A model group refuses a blank node as a query subject exactly as a plain model does — it
        /// had no guard at all, and its <c>ContainsResource</c> puts the identifier into a triple
        /// pattern, where a bare label matched anything and answered <c>true</c> for any non-empty
        /// group.
        /// </summary>
        /// <remarks>
        /// Uses a store-minted identifier, which is the only case that distinguishes "is a blank node"
        /// from "is spelled as a label" — and it does so only on Virtuoso, whose blank ids are
        /// <c>nodeID://</c> IRIs.
        /// </remarks>
        [Test]
        public virtual void AStoreMintedBlankIdentifierIsStillRefusedAsAQuerySubject()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);

            var child = (Resource)Model1.CreateResource(new UriRef("_:0", true));
            child.AddProperty(new Property(BaseUri.GetUriRef("p1")), "blank");
            child.Commit();

            var minted = child.Uri;

            Assert.IsTrue(minted.IsBlankId(), "the minted identifier is still a blank node");
            Assert.IsFalse(minted.CanBeQuerySubject(), "and is therefore not a query subject");

            Assert.Throws<ArgumentException>(() => group.ContainsResource(minted));
            Assert.Throws<ArgumentException>(() => group.GetResource(minted));
        }

        #region Bulk subject binding (ADR-0046)

        /// <summary>
        /// The model-group copy of the bulk lazy-load query carried the same equality chain as
        /// <see cref="Model"/>, so a mapped collection past the store's nesting limit failed here
        /// too — on reads and, because Add/Remove read before mutating, on writes.
        /// </summary>
        /// <remarks>
        /// dotNetRDF has no nesting limit, so the in-memory run proves only that the round-trip is
        /// correct; the Virtuoso run is the one that guards the defect.
        /// </remarks>
        [Test]
        public virtual void GetResourcesByUriHandlesLargeCollections()
        {
            const int count = 300;

            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);

            var contact = Model1.CreateResource<Contact>(R1);
            contact.Fullname = "Bulk";

            for (int i = 0; i < count; i++)
            {
                var address = Model1.CreateResource<EmailAddress>(BaseUri.GetUriRef("mail" + i));
                address.Address = "user" + i + "@example.org";
                address.Commit();

                contact.EmailAddresses.Add(address);
            }

            contact.Commit();

            var loaded = group.GetResource<Contact>(R1);

            Assert.AreEqual(count, loaded.EmailAddresses.Count, "every member of the collection must come back");
            Assert.AreEqual(count, loaded.EmailAddresses.Select(e => e.Uri).Distinct().Count(), "and each exactly once");
            Assert.IsTrue(loaded.EmailAddresses.All(e => !string.IsNullOrEmpty(e.Address)),
                "each member must be materialized with its properties, not just its identifier");
        }

        /// <summary>
        /// This overload had no guard at all: an empty subject set emitted <c>FILTER ( )</c>, a
        /// syntax error, and a null one threw <see cref="System.NullReferenceException"/> out of
        /// <c>string.Join</c>. Both must now be an empty result — and not a whole-model read.
        /// </summary>
        [Test]
        public virtual void GetResourcesByUriReturnsEmptyForNoSubjects()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);

            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            Assert.IsFalse(group.IsEmpty, "the guard is meaningless against an empty model group");

            Assert.IsEmpty(group.GetResources(new Uri[0], typeof(Resource)).ToList(),
                "an empty subject set must not be read as 'every subject'");

            Assert.IsEmpty(group.GetResources(null, typeof(Resource)).ToList(),
                "a null subject set must not throw, and must not be read as 'every subject'");
        }

        /// <summary>
        /// A blank node label cannot be addressed by a SPARQL query, so it is skipped rather than
        /// serialized — emitting <c>&lt;_:b0&gt;</c> took the addressable subjects down with it.
        /// </summary>
        [Test]
        public virtual void GetResourcesByUriToleratesBlankNodeSubjects()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);

            var blank = new UriRef("_:0", true);
            var named = BaseUri.GetUriRef("mail0");

            var address = Model1.CreateResource<EmailAddress>(named);
            address.Address = "user0@example.org";
            address.Commit();

            var resources = group.GetResources(new Uri[] { blank, named }, typeof(EmailAddress))
                .Cast<EmailAddress>()
                .ToList();

            Assert.AreEqual(1, resources.Count, "the addressable subject must still come back");
            Assert.AreEqual(named, resources[0].Uri);

            Assert.IsEmpty(group.GetResources(new Uri[] { blank }, typeof(EmailAddress)).ToList(),
                "a blank-node-only request must return empty rather than query for every subject");
        }


        /// <summary>
        /// Proves a subject set larger than one batch round-trips end to end against a real store.
        /// </summary>
        /// <remarks>
        /// <b>This is not the guard against the equality chain, despite what it once claimed.</b> With
        /// subjects batched at <c>SubjectBindingBatchSize</c>, asking for 2000 sends two queries of
        /// 1000 — both under the 1024 terms an equality chain still compiles on Virtuoso 7.2.12/7.2.14
        /// (measured; the cap is a compile-time constant of the build, unmoved by
        /// <c>ThreadStackSize</c>). A revert of the shape alone would pass here. What pins the shape is
        /// <c>BulkResourceQueryShapeTest</c>, which captures the SPARQL each model actually emits.
        /// <para>
        /// What this test does prove is worth keeping: that batching works against a real server, and
        /// that the batches concatenate into one correct result. Deliberately cheap — the subjects need
        /// not exist, because the failure mode was in compiling the query, not in answering it.
        /// </para>
        /// </remarks>
        [Test]
        public virtual void GetResourcesByUriCompilesBeyondTheEqualityChainLimit()
        {
            const int count = 2000;

            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);

            var present = BaseUri.GetUriRef("mail0");

            var address = Model1.CreateResource<EmailAddress>(present);
            address.Address = "user0@example.org";
            address.Commit();

            var uris = new List<Uri> { present };

            for (int i = 1; i < count; i++)
            {
                uris.Add(BaseUri.GetUriRef("absent" + i));
            }

            var resources = group.GetResources(uris, typeof(EmailAddress))
                .Cast<EmailAddress>()
                .ToList();

            Assert.AreEqual(1, resources.Count, "only the subject that exists should come back");
            Assert.AreEqual(present, resources[0].Uri);
        }

        #endregion
    }
}
