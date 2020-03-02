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
using Semiodesk.Trinity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Semiodesk.Trinity.Test;

namespace dotNetRDFStore.Test
{


    [TestFixture]
    class ModelTest
    {
        IStore Store;
        IModel Model;

        [SetUp]
        public void SetUp()
        {
            Store = StoreFactory.CreateStore("provider=dotnetrdf");
            Uri testModel = new Uri("ex:Test");
            Model = Store.CreateModel(testModel);
        }

        [TearDown]
        public void TearDown()
        {
            Store.Dispose();
            Store = null;
        }

        [Test]
        public void CreateResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            Assert.AreEqual(resourceUri, result.Uri);
            List<Property> properties = result.ListProperties().ToList();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, result.GetValue(property));
        }

        [Test]
        public void ModifyResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            
            result.RemoveProperty(property, literal);
            literal = "var2";
            result.AddProperty(property, literal);
            result.Commit();

            result = Model.GetResource(resourceUri);

            Assert.AreEqual(resourceUri, result.Uri);
            List<Property> properties = result.ListProperties().ToList();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, result.GetValue(property));

        }

        [Test]
        public void RemoveResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);

            Model.DeleteResource(result);

            Assert.IsFalse(Model.ContainsResource(result));

        }

        [Test]
        public void GetResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            Assert.AreEqual(resourceUri, result.Uri);
            List<Property> properties = result.ListProperties().ToList();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, result.GetValue(property));
        }

        [Test]
        public void GetResourcesTest()
        {
            var resourceUri1 = new Uri("ex:test:resource1");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri1);
            res.AddProperty(property, "lit1");
            res.Commit();

            var resourceUri2 = new Uri("ex:test:resource2");
            res = Model.CreateResource(resourceUri2);
            res.AddProperty(property, "lit2");
            res.Commit();

            var result = Model.GetResources(new[] { resourceUri1, resourceUri2 }, typeof(Resource)).ToList();
            Assert.AreEqual(2, result.Count);

            IResource res1 = result[0] as IResource;
            Assert.AreEqual(resourceUri1, res1.Uri);
            List<Property> properties = res1.ListProperties().ToList();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual("lit1", res1.GetValue(property));

            IResource res2 = result[1] as IResource;
            Assert.AreEqual(resourceUri2, res2.Uri);
            properties = res2.ListProperties().ToList();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual("lit2", res2.GetValue(property));

        }

        [Test]
        public void UpdateResourceTest()
        {
            Property property = new Property(new Uri("http://example.org/MyProperty"));
            Uri resourceUri = new Uri("http://example.org/MyResource");
            IResource resource = Model.CreateResource(resourceUri);
            resource.AddProperty(property, 123);
            resource.AddProperty(property, "in the jungle");
            resource.Commit();

            // Try to update resource with different properties then persisted
            Resource r2 = new Resource(resourceUri);
            r2.AddProperty(property, "in the jengle");

            r2.Model = Model;
            r2.Commit();
            var actual = Model.GetResource<Resource>(resourceUri);
            Assert.AreEqual(r2, actual);


            // Try to update resource without properties
            Resource r3 = new Resource(resourceUri);

            r3.Model = Model;
            r3.Commit();
            actual = Model.GetResource<Resource>(resourceUri);
            Assert.AreEqual(r3, actual);
        }

        [Test]
        public void UpdateResourcesTest()
        {
            Property intProperty = new Property(new Uri("http://example.org/int"));
            Property stringProperty = new Property(new Uri("http://example.org/string"));
            Uri r1Uri = new Uri("http://example.org/r1");
            Resource r1 = Model.CreateResource<Resource>(r1Uri);
            r1.AddProperty(intProperty, 123);
            r1.AddProperty(stringProperty, "in the jungle");
            Uri r2Uri = new Uri("http://example.org/r2");
            Resource r2 = Model.CreateResource<Resource>(r2Uri);
            r2.AddProperty(intProperty, 111);
            r2.AddProperty(stringProperty, "in the jingle");

            Uri r3Uri = new Uri("http://example.org/r3");
            Resource r3 = Model.CreateResource<Resource>(r3Uri);
            r3.AddProperty(intProperty, 333);
            r3.AddProperty(stringProperty, "in the jongle");

            Model.UpdateResources(null, r1, r2, r3);

            var actual = Model.GetResources<Resource>().ToList();
            Assert.Contains(r1, actual);
            Assert.Contains(r2, actual);
            Assert.Contains(r3, actual);

            r1.RemoveProperty(intProperty, 123);
            r1.AddProperty(intProperty, 154);

            r2.RemoveProperty(stringProperty, "in the jingle");
            r2.AddProperty(stringProperty, "boo");

            r3.AddProperty(stringProperty, "hooo");
            Model.UpdateResources(null, r1, r2, r3);

            actual = Model.GetResources<Resource>().ToList();
            Assert.Contains(r1, actual);
            Assert.Contains(r2, actual);
            Assert.Contains(r3, actual);

        }

        [Test]
        public void ContainsResourceTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            IResource result = Model.GetResource(resourceUri);
            Assert.AreEqual(true, Model.ContainsResource(new UriRef("ex:test:resource")));
        }

        [Test]
        public void AskQueryTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            SparqlQuery q = new SparqlQuery("ASK { <ex:test:resource> ?p ?o . }");
            var b = Model.ExecuteQuery(q);
        }

        [Test]
        public void SparqlQueryTest()
        {

        }

        [Test]
        public void ReadFromStringTest()
        {
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

            using( Stream s = GenerateStreamFromString(turtle))
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
            int age = (int) r.GetValue(new Property(new Uri("http://xmlns.com/foaf/0.1/age")));
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
        public void ReadLocalizedFromStringTest()
        {
            string turtle = @"@base <http://example.org/> .
@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .


<#spiderman> a foaf:Person ;
    foaf:name ""Spiderman"", ""Человек-паук""@ru .";

            using (Stream s = GenerateStreamFromString(turtle))
            {
                Assert.IsTrue(Model.Read(s, RdfSerializationFormat.Turtle, false));
            }

            IResource r = Model.GetResource(new Uri("http://example.org/#spiderman"));
            string name = r.GetValue(new Property(new Uri("http://xmlns.com/foaf/0.1/name"))) as string;

          
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

        [Test]
        public void WriteTest()
        {
            Model.Clear();

            INamespaceMap namespaces = new NamespaceMap()
            {
                { "ex2", new Uri("http://example.org") }
            };

            Property property = new Property(new Uri("http://example.org/MyProperty"));

            IResource model2_resource2 = Model.CreateResource(new Uri("ex:Resource"));
            model2_resource2.AddProperty(property, "in the\n jungle");
            model2_resource2.Commit();

            using (MemoryStream wr = new MemoryStream())
            {
                Model.Write(wr, RdfSerializationFormat.RdfXml, namespaces);

                var result = Encoding.UTF8.GetString(wr.ToArray());
            }
        }
    }
}
