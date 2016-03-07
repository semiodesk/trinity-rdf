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
// Copyright (c) Semiodesk GmbH 2015

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
            List<Property> properties = result.ListProperties();
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
            List<Property> properties = result.ListProperties();
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
            List<Property> properties = result.ListProperties();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, result.GetValue(property));
        }

        [Test]
        public void ResourceQueryTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            ResourceQuery q = new ResourceQuery();
            q.Where(property, literal);
            var queryResult = Model.ExecuteQuery(q);
            Assert.AreEqual(1, queryResult.Count());
            var result = queryResult.GetResources<Resource>();
            Assert.AreEqual(1, result.Count());
            var resultResource = result.First();
            Assert.AreEqual(resourceUri, resultResource.Uri);
            List<Property> properties = resultResource.ListProperties();
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(property, properties[0]);
            Assert.AreEqual(literal, resultResource.GetValue(property));
        }


        [Test]
        public void ResourceQueryCountTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var property = new Property(new Uri("ex:test:property"));
            var res = Model.CreateResource(resourceUri);
            res.AddProperty(property, literal);
            res.Commit();

            ResourceQuery q = new ResourceQuery();
            q.Where(property, literal);
            var queryResult = Model.ExecuteQuery(q);

            Assert.AreEqual(1, queryResult.Count());
        }

        [Test]
        public void Mapped_ResourceQueryTest()
        {
            var literal = "var";
            var resourceUri = new Uri("ex:test:resource");
            var res = Model.CreateResource<SingleMappingTestClass>(resourceUri);
            res.stringTest.Add(literal);
            res.Commit();

            ResourceQuery q = new ResourceQuery(TestOntology.SingleMappingTestClass);
            q.Where(TestOntology.stringTest, literal);
            var queryResult = Model.ExecuteQuery(q);

            var result = queryResult.GetResources<SingleMappingTestClass>();
            Assert.AreEqual(1, result.Count());
            var resultResource = result.First();
            Assert.AreEqual(resourceUri, resultResource.Uri);
            Assert.AreEqual(1, resultResource.stringTest.Count);
            
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
    }
}
