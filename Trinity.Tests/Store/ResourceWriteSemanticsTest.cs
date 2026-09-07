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
using Semiodesk.Trinity.Tests.Linq;
using System.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Covers what a <c>Commit()</c> is allowed to touch in the backing store.
    ///
    /// A commit must write the caller's own changes and nothing else. Historically it replaced the
    /// whole resource (<c>DELETE { &lt;uri&gt; ?p ?o }</c> followed by a full re-serialization), so
    /// anything the in-memory copy did not know about was silently erased — see
    /// <c>doc/trinity-write-semantics.md</c>. These tests pin the property-level behaviour.
    ///
    /// They are deliberately sequential: the defect is a stale in-memory copy overwriting newer
    /// state, and interleaving two loaded copies reproduces it deterministically. Doing it with
    /// threads reproduces the same thing, only flakily.
    /// </summary>
    [TestFixture]
    public abstract class ResourceWriteSemanticsTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Methods

        /// <summary>
        /// Two callers each load the parent, each add a different child, and each commit. Both links
        /// must survive: the second writer's serialization does not contain the first writer's child,
        /// so a whole-resource overwrite erases it.
        /// </summary>
        [Test]
        public void CommitDoesNotEraseCollectionValuesAddedByAnotherWriter()
        {
            var parentUri = BaseUri.GetUriRef("parent");

            var parent = Model1.CreateResource<Person>(parentUri);
            parent.FirstName = "Parent";
            parent.Commit();

            var child1 = Model1.CreateResource<Person>(BaseUri.GetUriRef("child1"));
            child1.FirstName = "Child 1";
            child1.Commit();

            var child2 = Model1.CreateResource<Person>(BaseUri.GetUriRef("child2"));
            child2.FirstName = "Child 2";
            child2.Commit();

            // Both callers read the parent before either writes it — the read-modify-write cycle
            // that a caller performs to add a single link.
            var first = Model1.GetResource<Person>(parentUri);
            var second = Model1.GetResource<Person>(parentUri);

            first.KnownPeople.Add(child1);
            first.Commit();

            second.KnownPeople.Add(child2);
            second.Commit();

            var reloaded = Model1.GetResource<Person>(parentUri);

            Assert.AreEqual(2, reloaded.KnownPeople.Count,
                "Both concurrently added links must survive; a whole-resource overwrite keeps only the last.");
        }

        /// <summary>
        /// A commit that changes one property must leave every other predicate on the resource alone,
        /// including predicates written by someone else after this copy was loaded.
        /// </summary>
        [Test]
        public void CommitDoesNotEraseUnrelatedPredicatesWrittenAfterLoad()
        {
            var resourceUri = BaseUri.GetUriRef("subject");

            var person = Model1.CreateResource<Person>(resourceUri);
            person.FirstName = "Original";
            person.Commit();

            // Loaded before the concurrent write below, so this copy has never seen the new predicate.
            var stale = Model1.GetResource<Person>(resourceUri);

            var other = new Property(BaseUri.GetUriRef("addedByAnotherWriter"));

            Model1.ExecuteUpdate(new SparqlUpdate(
                $"INSERT DATA {{ GRAPH <{Model1.Uri.OriginalString}> {{ <{resourceUri.OriginalString}> <{other.Uri.OriginalString}> 'kept' }} }}"));

            stale.FirstName = "Changed";
            stale.Commit();

            var reloaded = Model1.GetResource<Person>(resourceUri);

            Assert.AreEqual("Changed", reloaded.FirstName, "The committed change must be persisted.");
            Assert.IsTrue(reloaded.HasProperty(other, "kept"),
                "A predicate this copy never loaded must not be deleted by committing an unrelated property.");
        }

        /// <summary>
        /// Stores without transaction support used to return <c>null</c>, which forced every caller to
        /// null-check and made transaction handling untestable in the in-memory fixture. They now return
        /// a no-op handle instead.
        /// </summary>
        [Test]
        public void BeginTransactionReturnsAHandleRatherThanNull()
        {
            var transaction = Store.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            Assert.IsNotNull(transaction,
                "BeginTransaction must never return null; stores without transaction support return a no-op handle.");

            // Must be callable without throwing, whether it is real or a no-op.
            transaction.Commit();
        }

        /// <summary>
        /// The dirty check the snapshot makes possible: a freshly loaded resource has nothing to write,
        /// a modified one does, and committing settles it again.
        /// </summary>
        [Test]
        public void HasUnsavedChangesTracksModification()
        {
            var resourceUri = BaseUri.GetUriRef("subject");

            var person = Model1.CreateResource<Person>(resourceUri);
            person.FirstName = "Original";

            Assert.IsTrue(person.HasUnsavedChanges(), "A resource that was never committed has unsaved changes.");

            person.Commit();

            Assert.IsFalse(person.HasUnsavedChanges(), "Committing settles the resource.");

            var loaded = Model1.GetResource<Person>(resourceUri);

            Assert.IsFalse(loaded.HasUnsavedChanges(), "A freshly loaded resource has nothing to write.");

            loaded.FirstName = "Changed";

            Assert.IsTrue(loaded.HasUnsavedChanges(), "A modified property must be reported.");

            loaded.Commit();

            Assert.IsFalse(loaded.HasUnsavedChanges(), "Committing settles it again.");
        }

        /// <summary>
        /// Committing a resource that links to an uncommitted one persists the link but no triples for
        /// the target, and reading it back materializes an empty instance. That must be distinguishable
        /// from a resource which genuinely exists and has no properties.
        /// </summary>
        [Test]
        public void UnresolvedLinksAreDistinguishableFromEmptyResources()
        {
            var parentUri = BaseUri.GetUriRef("parent");

            // Deliberately never committed, so the store holds no triples for it.
            var ghost = new Person(BaseUri.GetUriRef("ghost"));

            var parent = Model1.CreateResource<Person>(parentUri);
            parent.FirstName = "Parent";
            parent.KnownPeople.Add(ghost);
            parent.Commit();

            var reloaded = Model1.GetResource<Person>(parentUri);
            var link = reloaded.KnownPeople.Single();

            Assert.IsTrue(link.IsUnresolved,
                "A link to a resource that was never written must report itself as unresolved.");
        }

        /// <summary>
        /// The counterpart to the test above: a resource that really is in the store must never be
        /// reported as unresolved, however few properties it has.
        /// </summary>
        [Test]
        public void ResolvedLinksAreNotReportedAsUnresolved()
        {
            var parentUri = BaseUri.GetUriRef("parent");

            var child = Model1.CreateResource<Person>(BaseUri.GetUriRef("child1"));
            child.FirstName = "Child 1";
            child.Commit();

            var parent = Model1.CreateResource<Person>(parentUri);
            parent.FirstName = "Parent";
            parent.KnownPeople.Add(child);
            parent.Commit();

            var reloaded = Model1.GetResource<Person>(parentUri);
            var link = reloaded.KnownPeople.Single();

            Assert.IsFalse(link.IsUnresolved, "A link to a committed resource must resolve.");
        }

        /// <summary>
        /// The bulk API has the same obligation as a single commit: write the caller's changes without
        /// disturbing anything else on the resources involved.
        /// </summary>
        [Test]
        public void BulkUpdateDoesNotEraseUnrelatedPredicatesWrittenAfterLoad()
        {
            var firstUri = BaseUri.GetUriRef("bulk1");
            var secondUri = BaseUri.GetUriRef("bulk2");

            var first = Model1.CreateResource<Person>(firstUri);
            first.FirstName = "First";
            first.Commit();

            var second = Model1.CreateResource<Person>(secondUri);
            second.FirstName = "Second";
            second.Commit();

            var staleFirst = Model1.GetResource<Person>(firstUri);
            var staleSecond = Model1.GetResource<Person>(secondUri);

            var other = new Property(BaseUri.GetUriRef("addedByAnotherWriter"));

            Model1.ExecuteUpdate(new SparqlUpdate(
                $"INSERT DATA {{ GRAPH <{Model1.Uri.OriginalString}> {{ <{firstUri.OriginalString}> <{other.Uri.OriginalString}> 'kept' }} }}"));

            staleFirst.FirstName = "First changed";
            staleSecond.FirstName = "Second changed";

            Model1.UpdateResources(new Resource[] { staleFirst, staleSecond });

            var reloadedFirst = Model1.GetResource<Person>(firstUri);
            var reloadedSecond = Model1.GetResource<Person>(secondUri);

            Assert.AreEqual("First changed", reloadedFirst.FirstName);
            Assert.AreEqual("Second changed", reloadedSecond.FirstName);
            Assert.IsTrue(reloadedFirst.HasProperty(other, "kept"),
                "A bulk update must not delete predicates the loaded copies never saw.");
        }

        /// <summary>
        /// A resource loaded without its mapped types still holds every triple in the unmapped bag, so
        /// committing it must not drop the properties the typed view would have owned. Under the old
        /// whole-resource rewrite, anything the in-memory copy failed to account for was deleted.
        /// </summary>
        [Test]
        public void CommitOfUntypedCopyDoesNotDeleteMappedProperties()
        {
            var resourceUri = BaseUri.GetUriRef("subject");

            var person = Model1.CreateResource<Person>(resourceUri);
            person.FirstName = "Original";
            person.LastName = "Surname";
            person.Commit();

            // Loaded as a plain resource: no property mappings are involved at all.
            var untyped = Model1.GetResource(resourceUri);
            untyped.AddProperty(new Property(BaseUri.GetUriRef("note")), "added");
            untyped.Commit();

            var reloaded = Model1.GetResource<Person>(resourceUri);

            Assert.AreEqual("Original", reloaded.FirstName, "Committing an untyped copy must not drop mapped values.");
            Assert.AreEqual("Surname", reloaded.LastName, "Committing an untyped copy must not drop mapped values.");
        }

        /// <summary>
        /// Blank nodes are not legal in a SPARQL DELETE template, so a delta that removes a
        /// blank-node-valued triple cannot name it directly — a hazard the old whole-resource rewrite
        /// never hit because it deleted through variables.
        /// </summary>
        /// <remarks>
        /// Quarantined: this never reaches the delta. Reading a mapped collection whose value is a blank
        /// node already fails on the query side with
        /// <c>"Cannot resolve a Relative URI Reference since there is no in-scope Base URI"</c>, thrown
        /// from dotNetRDF's expression parser while resolving the lazy-load filter. That is a pre-existing
        /// limitation of blank-node handling in the read path, independent of write semantics, and it
        /// extends item 6 of <c>doc/trinity-write-semantics.md</c>. The test is kept because the delta
        /// hazard is real and will need covering once blank-node reads work.
        /// </remarks>
        [Test]
        [Ignore("Pre-existing: blank-node values in mapped collections fail on the read path. See doc/known-test-failures.md.")]
        public void CanRemoveBlankNodeValuedLink()
        {
            var parentUri = BaseUri.GetUriRef("parent");

            // The typed CreateResource<T> overload rejects blank ids, so the untyped one is used here.
            var child = (Resource)Model1.CreateResource(new UriRef("_:0", true));
            child.AddProperty(new Property(BaseUri.GetUriRef("label")), "Blank child");
            child.Commit();

            var parent = Model1.CreateResource<Person>(parentUri);
            parent.FirstName = "Parent";
            parent.Interests.Add(child);
            parent.Commit();

            var loaded = Model1.GetResource<Person>(parentUri);

            Assert.AreEqual(1, loaded.Interests.Count, "The blank-node link must round-trip.");

            loaded.Interests.Remove(loaded.Interests.First());
            loaded.Commit();

            var reloaded = Model1.GetResource<Person>(parentUri);

            Assert.AreEqual(0, reloaded.Interests.Count, "Removing a blank-node-valued link must delete the triple.");
        }

        /// <summary>
        /// Removing a mapped collection value through the unmapped interface, with a value of a
        /// *subclass* of the element type -- Person.Interests is List&lt;Resource&gt;, so a Person is
        /// the ordinary polymorphic case.
        ///
        /// This is the direction the inverted type guard in RemoveOrResetValue actually broke. The test
        /// it had (removing through List&lt;T&gt;.Remove on the collection itself) never enters
        /// RemoveOrResetValue at all, so the mapped-interface path was uncovered: the guard asked
        /// value.GetType().IsAssignableFrom(_genericType), and Person.IsAssignableFrom(Resource) is
        /// false, so every subclass instance was refused outright.
        /// </summary>
        [Test]
        public void RemovesACollectionValueOfASubclassThroughTheMappedInterface()
        {
            var personUri = BaseUri.GetUriRef("subtype-remove-person");
            var interestUri = BaseUri.GetUriRef("subtype-remove-interest");

            var interest = Model1.CreateResource<Person>(interestUri);
            interest.FirstName = "Interest";
            interest.Commit();

            var person = Model1.CreateResource<Person>(personUri);
            person.FirstName = "Person";
            person.AddProperty(foaf.interest, interest);
            person.Commit();

            var loaded = Model1.GetResource<Person>(personUri);

            Assert.AreEqual(1, loaded.Interests.Count, "Precondition: the link was written.");

            // A Person into a List<Resource> mapping: the everyday polymorphic case, which used to
            // throw "Provided argument value was not of type Semiodesk.Trinity.Resource".
            Assert.DoesNotThrow(() => person.RemoveProperty(foaf.interest, interest));

            person.Commit();

            var reloaded = Model1.GetResource<Person>(personUri);

            Assert.AreEqual(0, reloaded.Interests.Count, "The removal must reach the store.");
        }

        /// <summary>
        /// The baseline the delta must not break: removing a value has to actually delete the triple,
        /// not merely stop writing it.
        /// </summary>
        [Test]
        public void CommitPersistsRemovedCollectionValues()
        {
            var parentUri = BaseUri.GetUriRef("parent");

            var child = Model1.CreateResource<Person>(BaseUri.GetUriRef("child1"));
            child.FirstName = "Child 1";
            child.Commit();

            var parent = Model1.CreateResource<Person>(parentUri);
            parent.FirstName = "Parent";
            parent.KnownPeople.Add(child);
            parent.Commit();

            var loaded = Model1.GetResource<Person>(parentUri);
            loaded.KnownPeople.Remove(loaded.KnownPeople.First());
            loaded.Commit();

            var reloaded = Model1.GetResource<Person>(parentUri);

            Assert.AreEqual(0, reloaded.KnownPeople.Count, "A removed link must be deleted from the store.");
        }

        #endregion
    }
}
