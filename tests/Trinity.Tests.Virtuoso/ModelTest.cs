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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using System.IO;
using System.Globalization;

namespace Semiodesk.Trinity.Test.Virtuoso
{
    [TestFixture]
    public class ModelTest : SetupClass
    {
        private IStore Store;

        private IModel Model;
        private IModel Model2;

        public ModelTest()
        {
        }

        [SetUp]
        public void SetUp()
        {
            string connectionString = SetupClass.ConnectionString;

            Store = StoreFactory.CreateStore(string.Format("{0};rule=urn:semiodesk/test/ruleset", connectionString));
            Store.InitializeFromConfiguration();

            Model = Store.GetModel(new Uri("http://example.org/TestModel"));

            if (!Model.IsEmpty)
            {
                Model.Clear();
            }

            Model2 = Store.GetModel(new Uri("semiodesk:Trinity:Test"));

            if (!Model2.IsEmpty)
            {
                Model2.Clear();
            }

            IResource model_resource = Model.CreateResource(new Uri("http://example.org/MyResource"));

            Property property = new Property(new Uri("http://example.org/MyProperty"));
            model_resource.AddProperty(property, "in the jungle");
            model_resource.AddProperty(property, 123);
            model_resource.AddProperty(property, DateTime.Now);
            model_resource.Commit();

            IResource model_resource2 = Model.CreateResource(new Uri("ex:Resource"));
            model_resource2.AddProperty(property, "in the jungle");
            model_resource2.AddProperty(property, 123);
            model_resource2.AddProperty(property, DateTime.Now);
            model_resource2.Commit();


            IResource model2_resource = Model2.CreateResource(new Uri("http://example.org/MyResource"));
            model2_resource.AddProperty(property, "in the jungle");
            model2_resource.AddProperty(property, 123);
            model2_resource.AddProperty(property, DateTime.Now);
            model2_resource.Commit();

            IResource model2_resource2 = Model2.CreateResource(new Uri("ex:Resource"));
            model2_resource2.AddProperty(property, "in the jungle");
            model2_resource2.AddProperty(property, 123);
            model2_resource2.AddProperty(property, DateTime.Now);
            model2_resource2.Commit();
        }

        [TearDown]
        public void TearDown()
        {
            Model.Clear();
            Model2.Clear();
            Store.Dispose();
        }

        public class Contact : Resource
        {
            // Type Mapping
            public override IEnumerable<Class> GetTypes()
            {
                return new List<Class> { nco.Contact };
            }

            protected PropertyMapping<string> FullnameProperty =
                   new PropertyMapping<string>("Fullname", nco.fullname);
            public string Fullname
            {
                get { return GetValue(FullnameProperty); }
                set { SetValue(FullnameProperty, value); }
            }

            protected PropertyMapping<DateTime> BirthdayProperty =
                   new PropertyMapping<DateTime>("Birthday", nco.birthDate);
            public DateTime Birthday
            {
                get { return GetValue(BirthdayProperty); }
                set { SetValue(BirthdayProperty, value); }
            }


            public Contact(Uri uri) : base(uri) { }

        }

        [Test]
        public void ModelNameTest()
        {
            Uri modelUri = new Uri("http://www.example.com");
            Uri modelUri2 = new Uri("http://www.example.com/");
            IModel m1 = Store.GetModel(modelUri);
            m1.Clear();
            IModel m2 = Store.GetModel(modelUri2);

            Assert.IsTrue(m1.IsEmpty);
            Assert.IsTrue(m2.IsEmpty);
            
            PersonContact c = m1.CreateResource<PersonContact>(new Uri("http://www.example.com/testResource"));
            c.NameFamily = "Doe";
            c.Commit();

            Assert.IsFalse(m1.IsEmpty);
            Assert.IsFalse(m2.IsEmpty);

            m1.Clear();

            Assert.IsTrue(m1.IsEmpty);
            Assert.IsTrue(m2.IsEmpty);

        }

        [Test]
        public void ContainsResourceTest()
        {
            Assert.IsTrue(Model.ContainsResource(new Uri("http://example.org/MyResource")));
            Assert.IsTrue(Model.ContainsResource(new Uri("ex:Resource")));
            Assert.IsTrue(Model2.ContainsResource(new Uri("http://example.org/MyResource")));
            Assert.IsTrue(Model2.ContainsResource(new Uri("ex:Resource")));
        }
        
        [Test]
        public void CreateResourceTest()
        {
            Assert.IsTrue(Model.ContainsResource(new Uri("http://example.org/MyResource")));
        }

        [Test]
        public void CreateEmptyResourceTest()
        {
            var res = Model.CreateResource(new Uri("http://semiodesk.com/emptyResource"));
            res.Commit();
        }

        [Test]
        public void DeleteResourceTest()
        {
            Uri uri0 = new Uri("http://example.org/MyResource");
            Uri uri1 = new Uri("http://example.org/MyResource1");

            Assert.IsTrue(Model.ContainsResource(uri0));

            Model.DeleteResource(uri0);

            Assert.IsFalse(Model.ContainsResource(uri0));

            Property p0 = new Property(new Uri("http://example.org/MyProperty"));
            Property p1 = new Property(new Uri("http://example.org/MyProperty1"));

            IResource r0 = Model.CreateResource(uri0);
            r0.AddProperty(p0, "in the jungle");
            r0.AddProperty(p0, 123);
            r0.Commit();

            IResource r1 = Model.CreateResource(uri1);
            r1.AddProperty(p0, 123);
            r1.AddProperty(p1, r0);
            r1.Commit();

            Assert.IsTrue(Model.ContainsResource(r0));
            Assert.IsTrue(Model.ContainsResource(r1));

            Model.DeleteResource(r0);

            Assert.IsFalse(Model.ContainsResource(r0));
            Assert.IsTrue(Model.ContainsResource(r1));

            // Update the resource from the model.
            r1 = Model.GetResource(uri1);

            Assert.IsTrue(r1.HasProperty(p0, 123));
            Assert.IsFalse(r1.HasProperty(p1, r0));
        }

        [Test]
        public void GetResourceTest()
        {
            IResource hans = Model.GetResource(new Uri("http://example.org/MyResource"));
            Assert.NotNull(hans);
            Assert.NotNull(hans.Model);

            hans = Model.GetResource<Resource>(new Uri("http://example.org/MyResource"));
            Assert.NotNull(hans);
            Assert.NotNull(hans.Model);

            hans = Model.GetResource(new Uri("http://example.org/MyResource"), typeof(Resource)) as Resource;
            Assert.NotNull(hans);
            Assert.NotNull(hans.Model);

            try
            {
                Model.GetResource<Resource>(new Uri("http://example.org/None"));

                Assert.Fail();
            }
            catch(ArgumentException)
            {
            }
        }

        [Test]
        public void GetResourcesTest()
        {
            SparqlQuery query = new SparqlQuery("DESCRIBE <http://example.org/MyResource>");

            List<Resource> resources = new List<Resource>();
            resources.AddRange(Model.GetResources(query));

            Assert.Greater(resources.Count, 0);

            foreach (IResource res in resources)
            {
                Assert.NotNull(res.Model);
            }
        }

        [Test]
        public void UpdateResourceTest()
        {
            Property property = new Property(new Uri("http://example.org/MyProperty"));

            Uri resourceUri = new Uri("http://example.org/MyResource");

            IResource resource = Model.GetResource(resourceUri);
            resource.RemoveProperty(property, 123);
            resource.Commit();

            IResource actual = Model.GetResource(resourceUri);

            Assert.AreEqual(resource, actual);

            actual = Model.GetResource<Resource>(resourceUri);

            Assert.AreEqual(resource, actual);

            // Try to update resource with different properties then persisted
            Resource r2 = new Resource(resourceUri);
            r2.AddProperty(property, "in the jengle");

            r2.Model = Model;
            r2.Commit();
            actual = Model.GetResource<Resource>(resourceUri);
            Assert.AreEqual(r2, actual);
        }

        [Test]
        public void DateTimeResourceTest()
        {
            Uri resUri = new Uri("http://example.org/DateTimeTest");
            IResource res = Model.CreateResource(resUri);

            Property property = new Property(new Uri("http://example.org/MyProperty"));

            DateTime t = new DateTime();
            Assert.IsTrue(DateTime.TryParse("2013-01-21T16:27:23.000Z", out t));

            res.AddProperty(property, t);
            res.Commit();

            IResource actual = Model.GetResource(resUri);
            object o = actual.GetValue(property);
            Assert.AreEqual(typeof(DateTime), o.GetType());
            DateTime actualDateTime = (DateTime)actual.GetValue(property);

            Assert.AreEqual(t.ToUniversalTime(), actualDateTime.ToUniversalTime());
        }

        [Test]
        public void LiteralWithHyphenTest()
        {
            Model.Clear();

            Property property = new Property(new Uri("http://example.org/MyProperty"));

            IResource model2_resource2 = Model.CreateResource(new Uri("ex:Resource"));
            model2_resource2.AddProperty(property, "\"in the jungle\"");
            model2_resource2.Commit();

            IResource r = Model.GetResource(new Uri("ex:Resource"));
            object o = r.GetValue(property);
            Assert.AreEqual(typeof(string), o.GetType());
            Assert.AreEqual("\"in the jungle\"", o);
        }

        [Test]
        public void LiteralWithLangTagTest()
        {
            Model.Clear();

            Property property = new Property(new Uri("http://example.org/MyProperty"));

            IResource model2_resource2 = Model.CreateResource(new Uri("ex:Resource"));
            model2_resource2.AddProperty(property, "in the jungle", "EN");
            model2_resource2.Commit();

            IResource r = Model.GetResource(new Uri("ex:Resource"));
            object o = r.GetValue(property);
            Assert.AreEqual(typeof(Tuple<string, CultureInfo>), o.GetType());
            var val = o as Tuple<string, CultureInfo>;
            Assert.AreEqual("in the jungle", val.Item1);

            var x = r.ListValues(property);

        }

        [Test]
        public void LiteralWithNewLineTest()
        {
            Model.Clear();

            Property p0 = new Property(new Uri("http://example.org/MyProperty"));

            IResource r0 = Model.CreateResource(new Uri("ex:Resource"));
            r0.AddProperty(p0, "in the\n jungle");
            r0.Commit();

            r0 = Model.GetResource(new Uri("ex:Resource"));

            object o = r0.GetValue(p0);

            Assert.AreEqual(typeof(string), o.GetType());
            Assert.AreEqual("in the\n jungle", o);
        }

        [Test]
        public void AddResourceTest()
        {
            Uri uriResource = new Uri("http://example.org/AddResourceTest");
            IResource resource = new Resource (uriResource);

            Property property = new Property(new Uri("http://example.org/MyProperty"));
            resource.AddProperty(property, "in the jungle");
            resource.AddProperty(property, 123);
            resource.AddProperty(property, DateTime.Now);

            Model.AddResource(resource);

            IResource actual = Model.GetResource(uriResource);

            Assert.AreEqual(uriResource, uriResource);
            Assert.AreEqual(resource.ListValues(property).Count(), actual.ListValues(property).Count());


            uriResource = new Uri("http://example.org/AddResourceTest2");
            Contact contact = new Contact(uriResource);
            contact.Fullname = "Peter";

            Model.AddResource<Contact>(contact);

            Contact actualContact = Model.GetResource<Contact>(uriResource);

            Assert.AreEqual(uriResource, uriResource);
            Assert.AreEqual(contact.Fullname, actualContact.Fullname);
        }

        [Test]
        public void GetTypedResourcesTest()
        {
            Uri uriResource = new Uri("http://example.org/Peter");
            Contact contact = Model.CreateResource<Contact>(uriResource);
            contact.Fullname = "Peter";
            contact.Commit();

            uriResource = new Uri("http://example.org/Hans");
            Contact contact2 = Model.CreateResource<Contact>(uriResource);
            contact2.Fullname = "Hans";
            contact2.Commit();

            var r = Model.GetResources<Contact>();

            Assert.AreEqual(2, r.Count());
            Assert.IsTrue(r.Contains(contact));
            Assert.IsTrue(r.Contains(contact2));

            Model.Clear();

            PersonContact personContact = Model.CreateResource<PersonContact>(uriResource);
            personContact.Fullname = "Peter";
            personContact.Commit();

            r = Model.GetResources<Contact>();
            Assert.AreEqual(0, r.Count());

            r = Model.GetResources<Contact>(true);
            Assert.AreEqual(1, r.Count());

            var x = Model.GetResource(uriResource);

            Assert.AreEqual(typeof(PersonContact), x.GetType());
        }

        [Test]
        public void WriteTest()
        {
            Model.Clear();

            Property property = new Property(new Uri("http://example.org/MyProperty"));

            IResource model2_resource2 = Model.CreateResource(new Uri("ex:Resource"));
            model2_resource2.AddProperty(property, "in the\n jungle");
            model2_resource2.Commit();

            MemoryStream wr = new MemoryStream();
            Model.Write(wr, RdfSerializationFormat.RdfXml);
            var myString = Encoding.UTF8.GetString(wr.ToArray());
        }

        [Test]
        public void ReadTest()
        {
            Model.Clear();

            FileInfo fi = new FileInfo("Models\\test-ntriples.nt");
            UriRef fileUri = fi.ToUriRef();

            Assert.IsTrue(Model.IsEmpty);
            Assert.IsTrue(Model.Read(fileUri, RdfSerializationFormat.NTriples, false));
            Assert.IsFalse(Model.IsEmpty);

            Model.Clear();

            Assert.IsTrue(Model.IsEmpty);
            Assert.IsTrue(Model.Read(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"), RdfSerializationFormat.RdfXml, false));
            Assert.IsFalse(Model.IsEmpty);

            Model.Clear();

            fi = new FileInfo("Models\\test-tmo.trig");
            fileUri = fi.ToUriRef();

            Assert.IsTrue(Model.IsEmpty);
            Assert.Throws(typeof(ArgumentException), () => { Model.Read(fileUri, RdfSerializationFormat.Trig, false); });
            
        }

        [Test]
        public void ReadFromStringTest()
        {
            Model.Clear();

            string turtle = @"@base <http://example.org/> .
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
    foaf:name ""Spiderman"", ""Человек-паук""@ru .";

            using (Stream s = GenerateStreamFromString(turtle))
            {
                Assert.IsTrue(Model.Read(s, RdfSerializationFormat.Turtle, false));
            }

            IResource r = Model.GetResource(new Uri("http://example.org/#green-goblin"));
            string name = r.GetValue(new Property(new Uri("http://xmlns.com/foaf/0.1/name"))) as string;
            Assert.AreEqual("Green Goblin", name);

            string turtle2 = @"@base <http://example.org/> .
@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .


<#green-goblin> foaf:age ""27""^^xsd:int .";

            using (Stream s = GenerateStreamFromString(turtle2))
            {
                Assert.IsTrue(Model.Read(s, RdfSerializationFormat.Turtle, true));
            }

            r = Model.GetResource(new Uri("http://example.org/#green-goblin"));
            int age = (int)r.GetValue(new Property(new Uri("http://xmlns.com/foaf/0.1/age")));
            name = r.GetValue(new Property(new Uri("http://xmlns.com/foaf/0.1/name"))) as string;
            Assert.AreEqual(27, age);

            turtle = @"@base <http://example.org/> .
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
    foaf:name ""Spiderman"", ""Человек-паук""@ru .";

            using (Stream s = GenerateStreamFromString(turtle))
            {
                Assert.IsTrue(Model.Read(s, RdfSerializationFormat.Turtle, false));
            }

            r = Model.GetResource(new Uri("http://example.org/#green-goblin"));
            name = r.GetValue(new Property(new Uri("http://xmlns.com/foaf/0.1/name"))) as string;
            Assert.AreEqual("Green Gobo", name);
        }

        [Test]
        public void TestAddMultipleResources()
        {
            Assert.Inconclusive("This test should work, it just takes too long.");
            Model.Clear();
            for (int j = 1; j < 7; j++)
            {
                for (int i = 1; i < 1000; i++)
                {
                    using (PersonContact pers = Model.CreateResource<PersonContact>())
                    {
                        pers.Fullname = string.Format("Name {0}", i * j);
                        pers.Commit();
                    }
                }
                

            }
        }

        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }


    }

    
}
