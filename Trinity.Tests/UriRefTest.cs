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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class UriRefTest
    {
        [Test]
        public void EqualsTest()
        {
            UriRef u0 = new UriRef("http://semiodesk.com/ontologies/ppo");
            UriRef u1 = new UriRef("http://semiodesk.com/ontologies/ppo#UriScehma");

            Assert.IsFalse(ReferenceEquals(u0, u1));
            Assert.AreEqual(u0, u0);
            Assert.AreNotEqual(u0, u1);
            Assert.IsFalse(u0 == u1);

            UriRef u2 = new UriRef("file://D:/Documents/x.doc");
            UriRef u3 = new UriRef("file://D:/Documents/2012/../x.doc");

            Assert.IsFalse(ReferenceEquals(u2, u3));
            Assert.AreEqual(u2, u3);
            Assert.IsTrue(u2 == u3);

            UriRef u4 = new UriRef("file://D:/Documents/x.doc#Metadata");
            UriRef u5 = new UriRef("file://D:/Documents/2012/../x.doc#Metadata");

            Assert.IsFalse(ReferenceEquals(u4, u5));
            Assert.AreEqual(u4, u5);
            Assert.IsTrue(u4 == u5);

            UriRef u6 = new UriRef("_:b0", true);
            UriRef u7 = new UriRef("_:b0", true);
            UriRef u8 = new UriRef("_:b1", true);

            Assert.IsFalse(ReferenceEquals(u6, u7));
            Assert.AreEqual(u6, u7);
            Assert.IsTrue(u6.Equals(u7));
            Assert.IsTrue(u6 == u7);

            Assert.IsFalse(ReferenceEquals(u7, u8));
            Assert.AreNotEqual(u7, u8);
            Assert.IsFalse(u7.Equals(u8));
            Assert.IsFalse(u7 == u8);
        }

        /// <summary>
        /// The consumer-facing symptom, and the one nothing covered before: generic collections do not
        /// call Equals(object). They use EqualityComparer&lt;T&gt;.Default, which prefers IEquatable&lt;T&gt;
        /// -- and .NET 10 added IEquatable&lt;Uri&gt; to System.Uri itself. Without UriRef implementing it
        /// too, every Dictionary, HashSet, Contains and Distinct in Trinity and in consumer code silently
        /// started comparing fragment-blind on that runtime, with no recompile involved.
        /// </summary>
        [Test]
        public void FragmentDistinctUrisStayDistinctInGenericCollections()
        {
            // Declared Uri, not UriRef: that is the case the interface has to carry, because the
            // == operators below cannot help once the static type is Uri.
            Uri u0 = new UriRef("http://semiodesk.com/ontologies/ppo#a");
            Uri u1 = new UriRef("http://semiodesk.com/ontologies/ppo#b");
            Uri u2 = new UriRef("http://semiodesk.com/ontologies/ppo");

            Assert.IsFalse(EqualityComparer<Uri>.Default.Equals(u0, u1),
                "EqualityComparer<Uri>.Default must honour the fragment.");
            Assert.IsFalse(EqualityComparer<Uri>.Default.Equals(u0, u2));

            var set = new HashSet<Uri> { u0, u1, u2 };

            Assert.AreEqual(3, set.Count, "Three distinct resources must occupy three slots.");
            Assert.IsTrue(set.Contains(new UriRef("http://semiodesk.com/ontologies/ppo#a")));
            Assert.IsFalse(set.Contains(new UriRef("http://semiodesk.com/ontologies/ppo#c")));

            var map = new Dictionary<Uri, string> { { u0, "a" }, { u1, "b" }, { u2, "none" } };

            Assert.AreEqual(3, map.Count);
            Assert.AreEqual("a", map[new UriRef("http://semiodesk.com/ontologies/ppo#a")]);

            Assert.AreEqual(3, new[] { u0, u1, u2, u0, u1 }.Distinct().Count());
        }

        /// <summary>
        /// Pins the Equals/GetHashCode contract. The old implementation branched on the *argument's*
        /// IsBlankId in Equals but on 'this' in GetHashCode, so symmetry and hash agreement held only
        /// incidentally -- through short-circuit evaluation rather than by construction. Both methods
        /// now decide from the same operands, and this test is what keeps that true.
        /// </summary>
        [Test]
        public void EqualityIsSymmetricAndAgreesWithGetHashCode()
        {
            var blank = new UriRef("_:b0", true);
            var absolute = new UriRef("http://semiodesk.com/ontologies/ppo#a");
            var sameAbsolute = new UriRef("http://semiodesk.com/ontologies/ppo#a");

            Assert.AreEqual(blank.Equals(absolute), absolute.Equals(blank),
                "A blank identifier compared against an absolute URI must answer the same both ways.");
            Assert.IsFalse(blank.Equals(absolute));

            Assert.IsTrue(absolute.Equals(sameAbsolute));
            Assert.IsTrue(sameAbsolute.Equals(absolute));
            Assert.AreEqual(absolute.GetHashCode(), sameAbsolute.GetHashCode(),
                "Equal values must hash equally.");

            // A blank identifier is relative, so reaching Fragment would throw. Equality must not.
            Assert.DoesNotThrow(() => blank.Equals(absolute));
            Assert.DoesNotThrow(() => absolute.Equals(blank));

            var set = new HashSet<UriRef> { blank, absolute };

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(new UriRef("_:b0", true)));
        }

        /// <summary>
        /// Copying a blank identifier through the Uri constructor used to throw (a blank label is not a
        /// valid absolute URI) and, before that, would have dropped the flag that makes it blank.
        /// </summary>
        [Test]
        public void CopyingPreservesBlankIdentity()
        {
            var blank = new UriRef("_:b0", true);
            var copy = new UriRef(blank);

            Assert.IsTrue(copy.IsBlankId);
            Assert.AreEqual(blank, copy);
            Assert.AreEqual(blank.GetHashCode(), copy.GetHashCode());
        }

        /// <summary>
        /// The runtime half of the TRIN007 enforcement. The generator only sees the partial properties
        /// it emits, but mappings can also be declared by hand (ADR-0018) and those reach the runtime
        /// with no diagnostic at all -- so PropertyMapping&lt;T&gt; refuses System.Uri itself, in Release
        /// builds too.
        /// </summary>
        [Test]
        public void PropertyMappingRefusesRawUriAndAcceptsUriRef()
        {
            var property = new Property(new UriRef("http://example.org/test#homepage"));

            var error = Assert.Throws<ArgumentException>(
                () => new PropertyMapping<Uri>("Homepage", property));

            Assert.That(error.Message, Does.Contain("Homepage").And.Contain("UriRef"),
                "The message has to name the property: it surfaces at assembly registration, not at the property.");

            Assert.DoesNotThrow(() => new PropertyMapping<UriRef>("Homepage", property));
            Assert.DoesNotThrow(
                () => new PropertyMapping<List<UriRef>>("Homepages", property, new List<UriRef>()));

            // The element type is what gets stored, so a collection of raw URIs has the same defect.
            Assert.Throws<ArgumentException>(
                () => new PropertyMapping<List<Uri>>("Homepages", property, new List<Uri>()));
        }

        /// <summary>
        /// Migrating a mapping to UriRef -- which TRIN007 and the constructor check now require -- must
        /// not break callers who hand it a plain Uri. The unmapped surface takes raw Uri values
        /// (IResource.AddProperty(Property, Uri) is a public overload), so without a widening the value
        /// would be refused by the mapping, land in the unmapped bag, and the mapped getter would
        /// quietly return null.
        /// </summary>
        [Test]
        public void APlainUriWidensIntoAUriRefMapping()
        {
            var property = new Property(new UriRef("http://example.org/test#homepage"));
            var scalar = (IPropertyMapping)new PropertyMapping<UriRef>("Homepage", property);

            Assert.IsTrue(scalar.IsValueCompatible(new Uri("http://example.org/x#a")),
                "The gate has to accept what the setter can convert.");

            scalar.SetOrAddMappedValue(new Uri("http://example.org/x#a"));

            var stored = ((PropertyMapping<UriRef>)scalar).GetValue();

            Assert.IsNotNull(stored, "The value must reach the mapping, not the unmapped bag.");
            Assert.AreEqual("http://example.org/x#a", stored.OriginalString);
            Assert.IsInstanceOf<UriRef>(stored, "It must be stored fragment-aware, not as a plain Uri.");

            var list = (IPropertyMapping)new PropertyMapping<List<UriRef>>(
                "Homepages", property, new List<UriRef>());

            Assert.IsTrue(list.IsValueCompatible(new Uri("http://example.org/y#b")));

            list.SetOrAddMappedValue(new Uri("http://example.org/y#b"));

            var storedList = ((PropertyMapping<List<UriRef>>)list).GetValue();

            Assert.AreEqual(1, storedList.Count);
            Assert.AreEqual("http://example.org/y#b", storedList[0].OriginalString);
        }

        /// <summary>
        /// The counterpart to <see cref="APlainUriWidensIntoAUriRefMapping"/>. A value that can be
        /// added must be removable with the same argument, or a property can be set and never cleared.
        /// The scalar path threw "Provided argument value was not of type UriRef"; the collection path
        /// was worse -- its type guard was inverted, so a plain Uri passed it and reached
        /// IList.Remove, which type-checks internally and drops the call. Silent, and a following
        /// Commit() re-persists the value the caller asked to delete.
        /// </summary>
        [Test]
        public void APlainUriCanAlsoBeRemovedFromAUriRefMapping()
        {
            var property = new Property(new UriRef("http://example.org/test#homepage"));

            var scalar = (IPropertyMapping)new PropertyMapping<UriRef>("Homepage", property);
            scalar.SetOrAddMappedValue(new Uri("http://example.org/x#a"));

            Assert.DoesNotThrow(() => scalar.RemoveOrResetValue(new Uri("http://example.org/x#a")));
            Assert.IsNull(((PropertyMapping<UriRef>)scalar).GetValue());

            var list = (IPropertyMapping)new PropertyMapping<List<UriRef>>(
                "Homepages", property, new List<UriRef>());
            list.SetOrAddMappedValue(new Uri("http://example.org/y#b"));

            var values = ((PropertyMapping<List<UriRef>>)list).GetValue();

            Assert.AreEqual(1, values.Count, "Precondition: the value was added.");

            list.RemoveOrResetValue(new Uri("http://example.org/y#b"));

            Assert.AreEqual(0, values.Count, "Removal must actually remove, not silently do nothing.");
        }

        /// <summary>
        /// The narrow tightening half of the inverted-guard correction. The old test,
        /// <c>value.GetType().IsAssignableFrom(_genericType)</c>, passed whenever the value's type was a
        /// *supertype* of the element type -- an instance that can never be in the collection --
        /// and IList.Remove then dropped the call silently. It is now refused, consistent with the
        /// scalar branch and this method's own fallthrough.
        ///
        /// The same inversion's larger half went the other way: it rejected *subclasses*, the ordinary
        /// polymorphic case. That is covered by
        /// <c>ResourceWriteSemanticsTest.RemovesACollectionValueOfASubclassThroughTheMappedInterface</c>,
        /// which runs against every store.
        /// </summary>
        [Test]
        public void RemovingAValueThatCannotBeInTheCollectionIsReported()
        {
            var property = new Property(new UriRef("http://example.org/test#related"));
            var list = (IPropertyMapping)new PropertyMapping<List<Property>>(
                "Related", property, new List<Property>());

            // A Resource is not a Property, so it cannot be in this collection. The old guard let it
            // through because Resource.IsAssignableFrom(Property) is true.
            Assert.Throws<Exception>(
                () => list.RemoveOrResetValue(new Resource(new UriRef("http://example.org/test#r"))));
        }

        [Test]
        public void ToStringTest()
        {
            UriRef u0 = new UriRef("http://semiodesk.com/ontologies/ppo#UriScehma");
            string u1 = "http://semiodesk.com/ontologies/ppo#UriScehma";

            Assert.AreEqual(u1, u0.ToString());
        }

        [Test]
        public void ToUriRefTest()
        {
            string uriString = "http://semiodesk.com/ontologies/ppo#UriScehma";
            UriRef u0 = uriString.ToUriRef();
            Assert.AreEqual(uriString, u0.OriginalString);
        }

        [Test]
        public void GetHashCodeTest()
        {
            var absoluteUri = new UriRef("https://trinity-rdf.net");
            var absoluteUriHash = absoluteUri.GetHashCode();

            var absoluteUriRef = new UriRef("https://trinity-rdf.net#download");
            var absoluteUriRefHash = absoluteUriRef.GetHashCode();

            Assert.AreNotEqual(absoluteUriHash, absoluteUriRefHash);

            var relativeUri = new UriRef("#download", UriKind.Relative);

            Assert.Throws<InvalidOperationException>(() => relativeUri.GetHashCode());

            var blankId = new UriRef("_:0", true);
            var blankIdHash = blankId.GetHashCode();
            
            Assert.AreNotEqual(absoluteUriHash, blankIdHash);
        }
    }
}
