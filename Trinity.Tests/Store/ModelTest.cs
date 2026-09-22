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
using Semiodesk.Trinity.Tests.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class ModelTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members
        
        protected UriRef R1;

        protected UriRef R2;

        protected UriRef R3;

        protected Property P1;

        protected Property P2;
        
        #endregion
        
        #region Methods
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            R1 = BaseUri.GetUriRef("r1");
            R2 = BaseUri.GetUriRef("r2");
            R3 = BaseUri.GetUriRef("r3");
            
            P1 = new Property(BaseUri.GetUriRef("p1"));
            P2 = new Property(BaseUri.GetUriRef("p2"));
        }

        private void InitializeModels()
        {
            var m1_r1 = Model1.CreateResource(R1);
            m1_r1.AddProperty(P1, "in the jungle");
            m1_r1.AddProperty(P1, 123);
            m1_r1.AddProperty(P1, DateTime.Now);
            m1_r1.Commit();

            var m1_r2 = Model1.CreateResource(R2);
            m1_r2.AddProperty(P1, "in the jungle");
            m1_r2.AddProperty(P1, 123);
            m1_r2.AddProperty(P1, DateTime.Now);
            m1_r2.Commit();
            
            var m2_r1 = Model2.CreateResource(R1);
            m2_r1.AddProperty(P1, "in the jungle");
            m2_r1.AddProperty(P1, 123);
            m2_r1.AddProperty(P1, DateTime.Now);
            m2_r1.Commit();

            var m2_r2 = Model2.CreateResource(R2);
            m2_r2.AddProperty(P1, "in the jungle");
            m2_r2.AddProperty(P1, 123);
            m2_r2.AddProperty(P1, DateTime.Now);
            m2_r2.Commit();
        }

        [Test]
        public virtual void ModelNameTest()
        {
            InitializeModels();
            
            var model1 = Store.GetModel(BaseUri.GetUriRef("_model1"));
            model1.Clear();
            
            Assert.IsTrue(model1.IsEmpty);

            var model2 = Store.GetModel(BaseUri.GetUriRef("_model2"));
            model2.Clear();

            Assert.IsTrue(model2.IsEmpty);
            
            var m1_r1 = model1.CreateResource<PersonContact>(BaseUri.GetUriRef("r1"));
            m1_r1.NameFamily = "Doe";
            m1_r1.Commit();

            Assert.IsFalse(model1.IsEmpty);
            Assert.IsTrue(model2.IsEmpty);

            model1.Clear();

            Assert.IsTrue(model1.IsEmpty);
            Assert.IsTrue(model2.IsEmpty);
        }

        [Test]
        public virtual void ContainsResourceTest()
        {
            InitializeModels();
            
            Assert.IsTrue(Model1.ContainsResource(R1));
            Assert.IsTrue(Model1.ContainsResource(R2));
            
            Assert.IsTrue(Model2.ContainsResource(R1));
            Assert.IsTrue(Model2.ContainsResource(R2));
        }

        [Test]
        public virtual void CreateEmptyResourceTest()
        {
            var empty = Model1.CreateResource(BaseUri.GetUriRef("empty"));
            empty.Commit();
        }
        
        [Test]
        public virtual void ContainsResourceReferenceTest()
        {
            Assert.IsFalse(Model1.ContainsResource(R1));
            Assert.IsFalse(Model1.ContainsResource(R2));
            
            var r2 = Model1.CreateResource(R2);
            r2.AddProperty(P1, 123);
            r2.AddProperty(P2, new Resource(R1));
            r2.Commit();

            // r1 is only referenced and not described as a subject.
            Assert.IsFalse(Model1.ContainsResource(R1));
            Assert.IsTrue(Model1.ContainsResource(R2));
        }

        [Test]
        public virtual void DeleteResourceTest()
        {
            InitializeModels();
            
            Assert.IsTrue(Model1.ContainsResource(R1));

            Model1.DeleteResource(R1);

            Assert.IsFalse(Model1.ContainsResource(R1));
            
            var r0 = Model1.CreateResource(R1);
            r0.AddProperty(P1, "in the jungle");
            r0.AddProperty(P1, 123);
            r0.Commit();

            var r3 = Model1.CreateResource(R3);
            r3.AddProperty(P1, 123);
            r3.AddProperty(P2, r0);
            r3.Commit();

            Assert.IsTrue(Model1.ContainsResource(r0));
            Assert.IsTrue(Model1.ContainsResource(r3));

            Model1.DeleteResource(r0);

            Assert.IsFalse(Model1.ContainsResource(r0));
            Assert.IsTrue(Model1.ContainsResource(r3));

            // Update the resource from the model.
            r3 = Model1.GetResource(r3);

            Assert.IsTrue(r3.HasProperty(P1, 123));
            Assert.IsFalse(r3.HasProperty(P2, r0));
        }

        [Test]
        public virtual void DeleteResourcesTest()
        {
            InitializeModels();
            
            Assert.IsTrue(Model1.ContainsResource(R1));
            Assert.IsTrue(Model1.ContainsResource(R2));
            
            var r1 = Model1.GetResource(R1);
            var r2 = Model1.GetResource(R2);

            Model1.DeleteResources(null, r1, r2);

            Assert.IsFalse(Model1.ContainsResource(R1));
            Assert.IsFalse(Model1.ContainsResource(R2));
        }

        [Test]
        public virtual void DeleteResourcesFromUrisTest()
        {
            InitializeModels();
            
            Assert.IsTrue(Model1.ContainsResource(R1));
            Assert.IsTrue(Model1.ContainsResource(R2));

            Model1.DeleteResources(new Uri[] { R1, R2 });

            Assert.IsFalse(Model1.ContainsResource(R1));
            Assert.IsFalse(Model1.ContainsResource(R2));
        }

        [Test]
        public virtual void GetResourceWithBlankIdTest()
        {
            var rX = Model1.CreateResource(new BlankId());
            rX.AddProperty(P1, 123);
            rX.Commit();

            Assert.Throws<ArgumentException>(() => Model1.GetResource<Resource>(rX.Uri));
        }

        [Test]
        public virtual void GetResourceWithBlankIdPropertyTest()
        {
            var r0 = Model1.CreateResource(new UriRef("_:0", true));
            r0.AddProperty(P1, "0");
            r0.Commit();

            var r2 = Model1.CreateResource(new UriRef("_:1", true));
            r0.AddProperty(P1, "1");
            r2.AddProperty(P2, r0);
            r2.Commit();

            Assert.Throws<ArgumentException>(() => Model1.ContainsResource(r2.Uri));
            Assert.Throws<ArgumentException>(() => Model1.GetResource(r2.Uri));
            Assert.Throws<ArgumentException>(() => Model1.GetResource(r2));

            var results = Model1.GetResources<Resource>().ToArray();

            Assert.AreEqual(2, results.Length);

            foreach (var r in results)
            {
                Assert.IsTrue(r.Uri.IsBlankId);

                foreach(var x in r.ListValues(P2).OfType<Resource>())
                {
                    Assert.IsTrue(x.Uri.IsBlankId);
                }
            }
        }
        
        [Test]
        public virtual void GetResourceWithDuplicateBlankIdPropertyTest()
        {
            var r0 = Model1.CreateResource(new UriRef("_:0", true));
            r0.AddProperty(P1, "0");
            r0.Commit();

            var r1 = Model1.CreateResource(new UriRef("_:0", true));
            r0.AddProperty(P1, "1");
            r1.AddProperty(P2, r0);
            r1.Commit();

            Assert.Throws<ArgumentException>(() => Model1.ContainsResource(r1.Uri));
            Assert.Throws<ArgumentException>(() => Model1.GetResource(r1.Uri));
            Assert.Throws<ArgumentException>(() => Model1.GetResource(r1));

            var resources = Model1.GetResources<Resource>().ToArray();

            Assert.AreEqual(2, resources.Length);

            foreach (var r in resources)
            {
                Assert.IsTrue(r.Uri.IsBlankId);

                foreach (var x in r.ListValues(P2).OfType<Resource>())
                {
                    Assert.IsTrue(x.Uri.IsBlankId);
                }
            }
        }

        [Test]
        public virtual void GetResourceTest()
        {
            InitializeModels();
            
            var r1 = Model1.GetResource(R1);
            
            Assert.NotNull(r1);
            Assert.NotNull(r1.Model);

            r1 = Model1.GetResource<Resource>(R1);
            
            Assert.NotNull(r1);
            Assert.NotNull(r1.Model);

            r1 = Model1.GetResource(R1, typeof(Resource)) as Resource;
            
            Assert.NotNull(r1);
            Assert.NotNull(r1.Model);

            try
            {
                var x1 = BaseUri.GetUriRef("x1");
                
                Model1.GetResource<Resource>(x1);

                Assert.Fail();
            }
            catch(ArgumentException)
            {
            }
        }

        [Test]
        public virtual void GetResourcesTest()
        {
            InitializeModels();
            
            Assert.IsTrue(Model1.ContainsResource(R1));
            
            var query = new SparqlQuery("DESCRIBE @resource").Bind("@resource", R1);
            var results = Model1.GetResources(query).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.NotNull(results[0].Model);
        }

        [Test]
        public virtual void UpdateResourceTest()
        {
            InitializeModels();
            
            var r1 = Model1.GetResource(R1);
            r1.RemoveProperty(P1, 123);
            r1.Commit();

            var actual = Model1.GetResource(R1);

            Assert.AreEqual(r1, actual);

            actual = Model1.GetResource<Resource>(R1);

            Assert.AreEqual(r1, actual);

            // Try to update resource with different properties then persisted..
            var r1mod = new Resource(R1);
            r1mod.Model = Model1;
            r1mod.AddProperty(P1, "in the jengle");
            r1mod.Commit();
            
            actual = Model1.GetResource<Resource>(R1);
            
            Assert.AreEqual(r1mod, actual);
        }

        [Test]
        public virtual void DateTimeResourceTest()
        {
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out var t));
            
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, t);
            r1.Commit();

            var actual = Model1.GetResource(R1);
            var v = (DateTime)actual.GetValue(P1);

            Assert.AreEqual(t.ToUniversalTime(), v.ToUniversalTime());
        }

        [Test]
        public virtual void TimeSpanResourceTest()
        {
            var t = TimeSpan.FromMinutes(5);
            
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, t);
            r1.Commit();

            var actual = Model1.GetResource(R1);
            var v = (TimeSpan)actual.GetValue(P1);

            Assert.AreEqual(t.TotalMinutes, v.TotalMinutes);
        }

        [Test]
        public virtual void LiteralWithHyphenTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, "\"in the jungle\"");
            r1.Commit();

            var actual = Model1.GetResource(R1);
            var v = (string)actual.GetValue(P1);
            
            Assert.AreEqual("\"in the jungle\"", v);
        }

        [Test]
        public virtual void LiteralWithLangTagTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, "in the jungle", "en");
            r1.Commit();

            var actual = Model1.GetResource(R1);
            var v = (Tuple<string, string>)actual.GetValue(P1);

            Assert.AreEqual("in the jungle", v.Item1);
            Assert.AreEqual("en", v.Item2);
        }

        [Test]
        public virtual void LiteralWithNewLineTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, "in the\n jungle");
            r1.Commit();

            var actual = Model1.GetResource(R1);
            var v = (string)actual.GetValue(P1);
            
            Assert.AreEqual("in the\n jungle", v);
        }

        [Test]
        public virtual void AddResourceTest()
        {
            var r1 = new Resource (R1);
            r1.AddProperty(P1, "in the jungle");
            r1.AddProperty(P1, 123);
            r1.AddProperty(P1, DateTime.Now);

            Model1.AddResource(r1);

            var actual = Model1.GetResource(R1);

            Assert.AreEqual(R1, actual.Uri);
            Assert.AreEqual(r1.ListValues(P1).Count(), actual.ListValues(P1).Count());
            
            var r2 = new Contact(R2);
            r2.Fullname = "Peter";

            Model1.AddResource(r2);

            var actual2 = Model1.GetResource<Contact>(R2);

            Assert.AreEqual(R2, actual2.Uri);
            Assert.AreEqual(r2.Fullname, actual2.Fullname);
        }

        #region Percent-encoded IRIs (ADR-0046)

        /// <summary>
        /// Every query builder must serialize an IRI through <c>SparqlSerializer.SerializeUri</c>,
        /// which uses <c>OriginalString</c>. Interpolating a <see cref="Uri"/> instead calls
        /// <c>Uri.ToString()</c>, which returns the display form and unescapes percent-encoding —
        /// and where the unescaped character is one SPARQL forbids inside an <c>IRIREF</c>, such as
        /// the space that <c>%20</c> becomes, the whole query is a parse error.
        /// </summary>
        /// <remarks>
        /// <see cref="EncodedUriContact"/> exists for this: its <c>[RdfClass]</c> and
        /// <c>[RdfProperty]</c> IRIs both carry <c>%20</c>. This covers the write path
        /// (<c>SerializeTypedLiteral</c> and the resource serializer), the type-constrained read
        /// (<c>Model.GetResources&lt;T&gt;()</c>, which builds <c>?s a &lt;type&gt;</c>), and the
        /// mapped read back.
        /// </remarks>
        [Test]
        public virtual void PercentEncodedIrisSurviveEveryQueryBuilder()
        {
            var r1 = Model1.CreateResource<EncodedUriContact>(R1);
            r1.Fullname = "Peter";
            r1.Commit();

            // Type-constrained read: builds "?s a <type>" from the [RdfClass] IRI.
            var byType = Model1.GetResources<EncodedUriContact>().ToList();

            Assert.AreEqual(1, byType.Count, "the type-constrained query must find the resource");
            Assert.AreEqual("Peter", byType[0].Fullname, "the mapped property IRI must round-trip too");

            // Plain read back, which goes through the resource/describe path.
            var loaded = Model1.GetResource<EncodedUriContact>(R1);

            Assert.AreEqual("Peter", loaded.Fullname);

            // And the bulk lazy-load path, which is what ADR-0046 rebuilt.
            var byUri = Model1.GetResources(new Uri[] { R1 }, typeof(EncodedUriContact))
                .Cast<EncodedUriContact>()
                .ToList();

            Assert.AreEqual(1, byUri.Count);
            Assert.AreEqual("Peter", byUri[0].Fullname);
        }

        /// <summary>
        /// A resource whose own IRI carries percent-encoding, as opposed to its class and property
        /// IRIs. This is the case the lazy-load filter used to turn into
        /// <c>RdfParseException: Illegal white space in URI</c>.
        /// </summary>
        [Test]
        public virtual void PercentEncodedResourceIriRoundTrips()
        {
            var encoded = new UriRef(BaseUri.OriginalString + "a%20b");

            var r1 = Model1.CreateResource<Contact>(encoded);
            r1.Fullname = "Peter";
            r1.Commit();

            Assert.IsTrue(Model1.ContainsResource(encoded));

            var byUri = Model1.GetResources(new Uri[] { encoded }, typeof(Contact))
                .Cast<Contact>()
                .ToList();

            Assert.AreEqual(1, byUri.Count, "a percent-encoded subject must not break its own lookup");
            Assert.AreEqual("Peter", byUri[0].Fullname);
        }

        #endregion

        #region Bulk subject binding (ADR-0046)

        /// <summary>
        /// A mapped collection is lazy-loaded through
        /// <see cref="IModel.GetResources(System.Collections.Generic.IEnumerable{Uri}, Type, ITransaction)"/>,
        /// which used to constrain the subjects with an equality chain
        /// (<c>FILTER(?s = &lt;a&gt;||?s = &lt;b&gt;||...)</c>). Virtuoso parses that as nested binary
        /// pairs and its compiler caps the nesting depth, so past a threshold every read *and*
        /// every write of the collection failed with
        /// <c>SP031: The nesting depth of subexpressions exceed limits of SPARQL compiler</c> —
        /// writes too, because Add/Remove read the collection before mutating it.
        /// </summary>
        /// <remarks>
        /// dotNetRDF has no such limit, so the in-memory run of this test proves only that the
        /// round-trip is correct. The Virtuoso run is the one that guards the defect.
        /// </remarks>
        [Test]
        public virtual void GetResourcesByUriHandlesLargeCollections()
        {
            const int count = 300;

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

            var loaded = Model1.GetResource<Contact>(R1);

            Assert.AreEqual(count, loaded.EmailAddresses.Count, "every member of the collection must come back");
            Assert.AreEqual(count, loaded.EmailAddresses.Select(e => e.Uri).Distinct().Count(), "and each exactly once");
            Assert.IsTrue(loaded.EmailAddresses.All(e => !string.IsNullOrEmpty(e.Address)),
                "each member must be materialized with its properties, not just its identifier");

            // The write path reads the collection before mutating it, which is the other half of
            // what the equality chain broke.
            loaded.EmailAddresses.RemoveAt(0);
            loaded.Commit();

            Assert.AreEqual(count - 1, Model1.GetResource<Contact>(R1).EmailAddresses.Count,
                "a write that touches a large collection must succeed too");
        }

        /// <summary>
        /// No subjects means no resources. Omitting the constraint instead left
        /// <c>SELECT ?s ?p ?o WHERE { ?s ?p ?o. }</c>, which returns the whole model — so this
        /// asserts against a model that holds unrelated resources, or the regression passes.
        /// </summary>
        [Test]
        public virtual void GetResourcesByUriReturnsEmptyForNoSubjects()
        {
            InitializeModels();

            Assert.IsFalse(Model1.IsEmpty, "the guard is meaningless against an empty model");

            Assert.IsEmpty(Model1.GetResources(new Uri[0], typeof(Resource)).ToList(),
                "an empty subject set must not be read as 'every subject'");

            Assert.IsEmpty(Model1.GetResources(null, typeof(Resource)).ToList(),
                "a null subject set must not throw, and must not be read as 'every subject'");
        }

        /// <summary>
        /// A blank node label is not a legal <c>VALUES</c> operand and not legal in a <c>FILTER</c>
        /// expression either, so a blank-node-valued link cannot be resolved by label. It must
        /// still appear in the mapped collection — flagged unresolved — rather than take the whole
        /// query down, which is what emitting <c>&lt;_:b0&gt;</c> used to do.
        /// </summary>
        [Test]
        public virtual void GetResourcesByUriToleratesBlankNodeSubjects()
        {
            var blank = new UriRef("_:0", true);
            var named = BaseUri.GetUriRef("mail0");

            var address = Model1.CreateResource<EmailAddress>(named);
            address.Address = "user0@example.org";
            address.Commit();

            var resources = Model1.GetResources(new Uri[] { blank, named }, typeof(EmailAddress))
                .Cast<EmailAddress>()
                .ToList();

            Assert.AreEqual(1, resources.Count, "the addressable subject must still come back");
            Assert.AreEqual(named, resources[0].Uri);

            Assert.IsEmpty(Model1.GetResources(new Uri[] { blank }, typeof(EmailAddress)).ToList(),
                "a blank-node-only request must return empty rather than query for every subject");
        }

        #endregion

        [Test]
        public virtual void GetTypedResourcesTest()
        {
            var r1 = Model1.CreateResource<Contact>(R1);
            r1.Fullname = "Peter";
            r1.Commit();

            var r2 = Model1.CreateResource<Contact>(R2);
            r2.Fullname = "Hans";
            r2.Commit();

            var results = Model1.GetResources<Contact>().ToList();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(r1));
            Assert.IsTrue(results.Contains(r2));
        }
        
        [Test]
        public virtual void GetTypedResourcesWithInferencingTest()
        {
            var r1 = Model1.CreateResource<PersonContact>(R1);
            r1.Fullname = "Peter";
            r1.Commit();

            var results = Model1.GetResources<Contact>().ToList();
            
            Assert.AreEqual(0, results.Count);

            results = Model1.GetResources<Contact>(true).ToList();
            
            Assert.AreEqual(1, results.Count);

            var actual = Model1.GetResource(R1);

            Assert.AreEqual(typeof(PersonContact), actual.GetType());
        }

        [Test]
        public virtual void WriteTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, "in the\n jungle");
            r1.Commit();

            var stream = new MemoryStream();
            
            Model1.Write(stream, RdfSerializationFormat.RdfXml);
            
            var result = Encoding.UTF8.GetString(stream.ToArray());
            
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        [Test]
        public virtual void WriteWithBaseUriTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, "test");
            r1.Commit();

            using (var s = new MemoryStream())
            {
                Model1.Write(s, RdfSerializationFormat.Turtle, null, BaseUri, true);

                s.Seek(0, SeekOrigin.Begin);

                var result = Encoding.UTF8.GetString(s.ToArray());

                Assert.IsFalse(string.IsNullOrEmpty(result));
                Assert.IsTrue(result.StartsWith("@base <" + BaseUri.OriginalString + ">"));
            }
        }

        [Test]
        public virtual void ReadTest()
        {
            // Path.Combine, not a literal separator: a backslash is a valid filename character on
            // Linux, so "Models\\test-ntriples.nt" is one nonexistent file name there.
            var file = new FileInfo(Path.Combine("Models", "test-ntriples.nt"));
            var fileUri = file.ToUriRef();
            
            Assert.IsTrue(Model1.Read(fileUri, RdfSerializationFormat.NTriples, false));
            Assert.IsFalse(Model1.IsEmpty);

            Model1.Clear();

            file = new FileInfo(Path.Combine("Models", "test-tmo.trig"));
            fileUri = file.ToUriRef();
            
            Assert.Throws(typeof(ArgumentException), () => { Model1.Read(fileUri, RdfSerializationFormat.Trig, false); });

        }

        /// <summary>
        /// Reading a graph from an <c>http</c> URL rather than a file.
        /// </summary>
        /// <remarks>
        /// Split out of <see cref="ReadTest"/> and made tolerant of the fetch failing, because it
        /// reaches a third party — <c>w3.org</c> — over the public internet. That was survivable while
        /// the store suites were local and serial; running them as a CI matrix has three jobs request
        /// the same URL at the same moment, and w3.org answers 503. A remote outage or a rate limit is
        /// not a defect in this repository, and a test that reddens CI for one teaches people to
        /// ignore CI. The assertion still runs, and still fails, whenever the fetch actually succeeds.
        /// </remarks>
        [Test]
        public virtual void ReadFromUrlTest()
        {
            try
            {
                Assert.IsTrue(Model1.Read(rdf.Namespace, RdfSerializationFormat.RdfXml, false));
            }
            catch (Exception e) when (IsNetworkFailure(e))
            {
                Assert.Inconclusive(
                    $"Could not fetch <{rdf.Namespace}>: {e.GetType().Name}: {e.Message}. "
                    + "The remote vocabulary is unreachable, which is not a defect here.");
            }

            Assert.IsFalse(Model1.IsEmpty);
        }

        /// <summary>
        /// Distinguishes "the network or the remote host let us down" from a genuine read failure.
        /// </summary>
        private static bool IsNetworkFailure(Exception e)
        {
            for (var current = e; current != null; current = current.InnerException)
            {
                if (current is System.Net.WebException || current is System.Net.Http.HttpRequestException)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public virtual void ReadFromStringTest()
        {
            var data = @"
@base <http://example.org/> .
@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .
@prefix rel: <http://www.perceive.net/schemas/relationship/> .

<#green-goblin>
    rel:enemyOf <#spiderman> ;
    a foaf:Person ;    # in the context of the Marvel universe
    foaf:name ""Green Goblin"" .

<#spiderman>
    rel:enemyOf <#green-goblin> ;
    a foaf:Person ;
    foaf:name ""Spiderman"", ""Человек-паук""@ru .
";

            using (var s1 = GenerateStreamFromString(data))
            {
                Assert.IsTrue(Model1.Read(s1, RdfSerializationFormat.Turtle, false));
            }

            var r1 = Model1.GetResource(new Uri("http://example.org/#green-goblin"));
            var v1 = (string)r1.GetValue(foaf.name);
            
            Assert.AreEqual("Green Goblin", v1);

            var data2 = @"
@base <http://example.org/> .
@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .

<#green-goblin> foaf:age ""27""^^xsd:int .
";

            using (Stream s2 = GenerateStreamFromString(data2))
            {
                Assert.IsTrue(Model1.Read(s2, RdfSerializationFormat.Turtle, true));
            }

            var r2 = Model1.GetResource(new Uri("http://example.org/#green-goblin"));
            var v2 = (int)r2.GetValue(foaf.age);
            
            Assert.AreEqual(27, v2);

            var data3 = @"
@base <http://example.org/> .
@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .
@prefix rel: <http://www.perceive.net/schemas/relationship/> .

<#green-goblin>
    rel:enemyOf <#spiderman> ;
    a foaf:Person ;    # in the context of the Marvel universe
    foaf:name ""Green Gobo"" .

<#spiderman>
    rel:enemyOf <#green-goblin> ;
    a foaf:Person ;
    foaf:name ""Spiderman"", ""Человек-паук""@ru .
";

            using (Stream s3 = GenerateStreamFromString(data3))
            {
                Assert.IsTrue(Model1.Read(s3, RdfSerializationFormat.Turtle, false));
            }

            var r3 = Model1.GetResource(new Uri("http://example.org/#green-goblin"));
            var v3 = (string)r3.GetValue(foaf.name);
            
            Assert.AreEqual("Green Gobo", v3);
        }

        [Test]
        public virtual void WriteToStringTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(P1, "test");
            r1.Commit();

            using (var s = new MemoryStream())
            {
                Model1.Write(s, RdfSerializationFormat.Turtle, null, null, true);

                s.Seek(0, SeekOrigin.Begin);
                
                var result = Encoding.UTF8.GetString(s.ToArray());

                Assert.IsFalse(string.IsNullOrEmpty(result));   
            }
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

            var present = BaseUri.GetUriRef("mail0");

            var address = Model1.CreateResource<EmailAddress>(present);
            address.Address = "user0@example.org";
            address.Commit();

            var uris = new List<Uri> { present };

            for (int i = 1; i < count; i++)
            {
                uris.Add(BaseUri.GetUriRef("absent" + i));
            }

            var resources = Model1.GetResources(uris, typeof(EmailAddress))
                .Cast<EmailAddress>()
                .ToList();

            Assert.AreEqual(1, resources.Count, "only the subject that exists should come back");
            Assert.AreEqual(present, resources[0].Uri);
        }

        #endregion
    }
}
