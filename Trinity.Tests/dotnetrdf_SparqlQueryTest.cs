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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

using Semiodesk.Trinity;
using Semiodesk.Trinity.Ontologies;


using NUnit.Framework;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class DotNetRDF_SparqlQueryTest
    {
        protected IModel Model = null;

        protected NamespaceManager NamespaceManager = new NamespaceManager();

        protected IStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            Uri modelUri = new Uri("http://example.org/TestModel");

            if( _store.ContainsModel(modelUri) )
                Model = _store.GetModel(modelUri);
            else
                Model = _store.CreateModel(modelUri);
            

            NamespaceManager.AddNamespace("ex", "http://example.org/");
            NamespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            NamespaceManager.AddNamespace("vcard", "http://www.w3.org/2001/vcard-rdf/3.0#");
            NamespaceManager.AddNamespace("foaf", "http://xmlns.com/foaf/0.1/");
            NamespaceManager.AddNamespace("dbpedia", "http://dbpedia.org/ontology/");
            NamespaceManager.AddNamespace("dbpprop", "http://dbpedia.org/property/");
            NamespaceManager.AddNamespace("schema", "http://schema.org/");
            NamespaceManager.AddNamespace("nie", "http://www.semanticdesktop.org/ontologies/2007/01/19/nie#");
            NamespaceManager.AddNamespace("nco", "http://www.semanticdesktop.org/ontologies/2007/03/22/nco#");
            NamespaceManager.AddNamespace("test", "http://www.semiodesk.com/ontologies/test#");
            NamespaceManager.AddNamespace("sfo", sfo.GetNamespace());
            NamespaceManager.AddNamespace(nfo.GetPrefix(), nfo.GetNamespace());

            //User u = UserManager.Instance.AddUser(new Uri("http://semiodesk.com/id/TestUser"));
            //u.GetDesktopFolder();
            //u.GetDownloadsFolder();

            MappingDiscovery.RegisterAllCurrentAssemblies();

            Model.Clear();

            IResource resource0 = Model.CreateResource(new Uri("http://example.org/Hans"));
            resource0.AddProperty(rdf.type, nco.PersonContact);
            resource0.AddProperty(nco.fullname, "Hans Wurscht");
            resource0.AddProperty(nco.birthDate, DateTime.Now);
            resource0.AddProperty(nco.blogUrl, "http://blog.com/Hans");
            resource0.Commit();

            IResource resource1 = Model.CreateResource(new Uri("http://example.org/Task"));
            resource1.AddProperty(rdf.type, tmo.Task);
            resource1.AddProperty(tmo.taskName, "Eine Aufgabe.");
            resource1.AddProperty(nco.creator, resource0);
            resource1.Commit();

            IResource resource2 = Model.CreateResource(new Uri("http://example.org/Doc#1"));
            resource2.AddProperty(rdf.type, nfo.Document);
            resource2.AddProperty(dc.date, DateTime.Today);
            resource2.AddProperty(nco.creator, resource0);
            resource2.Commit();

            // NOTE: The different name influences the ordering of the resource in query results.
            IResource resource3 = Model.CreateResource(new Uri("http://example.org/Boc#2"));
            resource3.AddProperty(rdf.type, nfo.Document);
            resource3.AddProperty(dc.date, DateTime.Today.AddHours(1));
            resource3.AddProperty(nco.creator, resource0);
            resource3.Commit();

            IResource resource4 = Model.CreateResource(new Uri("http://example.org/Doc#3"));
            resource4.AddProperty(rdf.type, nfo.Document);
            resource4.AddProperty(dc.date, DateTime.Today.AddHours(2));
            resource4.AddProperty(nco.creator, resource0);
            resource4.Commit();
        }

        [TearDown]
        public void TearDown()
        {
            Model.Clear();
        }

        [Test]
        public void TestAsk()
        {
            // Checking the model using ASK queries.
            SparqlQuery query = new SparqlQuery("ASK { ?s nco:fullname 'Hans Wurscht' . }", NamespaceManager);
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());

            query = new SparqlQuery("ASK { ?s nco:fullname 'Hans Meier' . }", NamespaceManager);
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(false, result.GetAnwser());
        }

        [Test]
        public void TestSelect()
        {
            // Retrieving bound variables using the SELECT query form.
            SparqlQuery query = new SparqlQuery("SELECT ?name ?birthday WHERE { ?x nco:fullname ?name. ?x nco:birthDate ?birthday. }", NamespaceManager);
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            Assert.AreEqual(1, result.GetBindings().Count());

            // Retrieving resoures using the SELECT or DESCRIBE query form.
            query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s nco:fullname 'Hans Wurscht'. }", NamespaceManager);
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(1, result.GetResources().Count());

            // Test SELECT with custom defined PREFIXes
            query = new SparqlQuery("PREFIX nco: <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#> SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s nco:fullname 'Hans Wurscht'. }");
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(1, result.GetResources().Count());

            // Check if the select statement only works on the given model.
            query = new SparqlQuery("SELECT * WHERE { ?s ?p ?o. }");
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(5, result.GetResources().Count());

            // Check that resource creation is done correctly for Resources containing dashes.
            IResource r0 = Model.CreateResource(new Uri("http://example.org/Something#0"));
            r0.AddProperty(new Property(new Uri("http://example.org/fullName")), "Something");
            r0.Commit();

            IResource r1 = Model.CreateResource(new Uri("http://example.org/Something#1"));
            r1.AddProperty(new Property(new Uri("http://example.org/fullName")), "Anotherthing");
            r1.Commit();

            query = new SparqlQuery("SELECT * WHERE { ?s ?p ?o. }");
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(7, result.GetResources().Count());
        }

        [Test]
        public void TestDescribe()
        {
            SparqlQuery query = new SparqlQuery("DESCRIBE <http://example.org/Hans>");
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            IList resources = result.GetResources().ToList();
            Assert.AreEqual(1, resources.Count);

            query = new SparqlQuery("DESCRIBE ?s WHERE { ?s nco:fullname 'Hans Wurscht'. }", NamespaceManager);
            result = Model.ExecuteQuery(query);

            resources = result.GetResources<PersonContact>().ToList();
            Assert.AreEqual(1, resources.Count);

            foreach (Contact c in resources)
            {
                Assert.AreEqual(c.GetType(), typeof(PersonContact));
            }
        }

        [Test]
        public void TestConstruct()
        {
            Assert.Inconclusive("Blank nodes are currently problematic.");
            SparqlQuery query = new SparqlQuery(@"
                CONSTRUCT
                {
                    ?x  vcard:N _:v .
                    _:v vcard:givenName ?name .
                }
                WHERE
                {
                    ?x nco:fullname ?name .
                }", NamespaceManager);

            ISparqlQueryResult result = Model.ExecuteQuery(query);

            IList resources = result.GetResources().ToList();
            Assert.AreEqual(2, resources.Count);
        }

        [Test]
        public void TestInferencing()
        {
            Assert.Inconclusive("dotnetrdf does not support inferencing.");
            _store = StoreFactory.CreateStore("provider=dotnetrdf;schema=Models/test-vocab.rdf");


            var model = _store.CreateModel(new Uri("http://example.org/TestModel"));

            Class horse = new Class(new Uri("http://www.semiodesk.com/ontologies/test#Horse"));
            Class animal = new Class(new Uri("http://www.semiodesk.com/ontologies/test#Animal"));
            Property eats = new Property(new Uri("http://www.semiodesk.com/ontologies/test#eats"));
            Property consumes = new Property(new Uri("http://www.semiodesk.com/ontologies/test#consumes"));

            IResource res = model.CreateResource(new Uri("http://www.example.org/Hans"));

            res.AddProperty(rdf.type, horse);
            res.AddProperty(eats, "Straw");
            res.Commit();


            SparqlQuery query;
            ISparqlQueryResult result;

            // This fact is not explicitly stated.
            query = new SparqlQuery("ASK WHERE { <http://www.example.org/Hans> a test:Animal . }", NamespaceManager);

            result = model.ExecuteQuery(query);
            Assert.IsFalse(result.GetAnwser());

            result = model.ExecuteQuery(query, true);
            Assert.IsTrue(result.GetAnwser());

            result = model.ExecuteQuery(query);
            Assert.IsFalse(result.GetAnwser());

            // This fact is not explicitly stated.
            query = new SparqlQuery("SELECT ?food WHERE { ?s test:consumes ?food . }", NamespaceManager);

            result = model.ExecuteQuery(query);
            Assert.AreEqual(0, result.GetBindings().Count());

            result = model.ExecuteQuery(query, true);
            Assert.AreEqual(1, result.GetBindings().Count());

        }

        [Test]
        public void TestModelApi()
        {
            // Retrieving resources using the model API.
            Assert.AreEqual(true, Model.ContainsResource(new Uri("http://example.org/Hans")));
            Assert.AreEqual(false, Model.ContainsResource(new Uri("http://example.org/Peter")));

            IResource hans = Model.GetResource(new Uri("http://example.org/Hans"));
            Assert.Throws(typeof(ArgumentException), delegate { Model.GetResource(new Uri("http://example.org/Peter")); });

            hans = Model.GetResource(new Uri("http://example.org/Hans"), typeof(Resource)) as IResource;
            Assert.NotNull(hans);
        }

        [Test]
        public void TestUriEscaping()
        {
            Uri uri = new Uri("file:///F:/test/02%20-%20Take%20Me%20Somewhere%20Nice.mp3");
            var x = Model.CreateResource(uri);
            var nameProperty = new Property(new Uri("ex:name"));
            x.AddProperty(nameProperty, "Name");
            x.Commit();

            var result = Model.GetResource(uri);
            Assert.AreEqual(x.Uri, result.Uri);
            Assert.AreEqual(x, result);
        }

        [Test]
        public void TestSelectCount()
        {
            
            SparqlQuery query = new SparqlQuery("SELECT COUNT(?s) AS ?count WHERE { ?s rdf:type nfo:Document. }", NamespaceManager);
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            var bindings = result.GetBindings();
            Assert.AreEqual(1, bindings.Count());
            Assert.AreEqual(3, bindings.First()["count"]);
        }

        [Test]
        public void TestCount()
        {
            SparqlQuery query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s rdf:type nfo:Document. ?s ?p ?o. }", NamespaceManager);
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            Assert.AreEqual(3, result.Count());


            query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s rdf:type nfo:Document. ?s ?p ?o. }", NamespaceManager);
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(3, result.Count());
        }

        [Test]
        public void TestSetModel()
        {

            // Testing SetModel with a SparqlQuery that uses no query parser, this should fail, because the SparqlQuery class has no way of modfiying the query

            SparqlQuery query = new SparqlQuery(SparqlQueryType.Select, "SELECT COUNT(?s) AS ?count WHERE { ?s ?p ?o . }", NamespaceManager);

            Assert.IsNull(query.Model);
            Assert.IsFalse(query.ToString().Contains("FROM"));

            MethodInfo dynMethod = query.GetType().GetMethod("SetModel", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(query, new object[] { Model });

            Assert.NotNull(query.Model);
            Assert.IsFalse(query.ToString().Contains("FROM"));

            // Testing SetModel with a SparqlQuery that uses the query parser, this should succeed

            query = new SparqlQuery("SELECT COUNT(?s) AS ?count WHERE { ?s ?p ?o . }", NamespaceManager);

            Assert.IsNull(query.Model);
            Assert.IsFalse(query.ToString().Contains("FROM"));

            dynMethod = query.GetType().GetMethod("SetModel", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(query, new object[] { Model });

            Assert.NotNull(query.Model);
            Assert.IsTrue(query.ToString().Contains("FROM"));
        }

        [Test]
        public void TestComplexQuery()
        {
            string queryString = "SELECT ?s0 ?p0 ?o0 "+
                 "WHERE "+
                 "{{ "+
                    "?s0 ?p0 ?o0 . "+
                    "{{ "+
                     "  SELECT DISTINCT ?s0 "+
                       "WHERE "+
                       "{{ "+
                         " ?s ?p ?o."+
                          "?s <{0}> <{1}> ."+
                          "{{"+
                           "  ?s ?p1 ?o1 ."+
                             "FILTER ISLITERAL(?o1) . FILTER REGEX(STR(?o1), \"\", \"i\") ."+
                          "}}"+
                          "UNION"+
                          "{{"+
                             "?s ?p1 ?s1 ."+
                             "?s1 ?p2 ?o2 ."+
                             "FILTER ISLITERAL(?o2) . FILTER REGEX(STR(?o2), \"\", \"i\") ."+
                          "}}"+
                       "}}"+
                       "ORDER BY ?o"+
                    "}}"+
                 "}}";
            string q = string.Format(queryString, rdf.type.Uri.OriginalString, tmo.Task.Uri.OriginalString);
            SparqlQuery query = new SparqlQuery(q);
            
            MethodInfo method = query.GetType().GetMethod("SetLimit", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(query, new object[] { 10 });




            var x = Model.ExecuteQuery(query);
            var res = x.GetResources().ToList();

        }

        [Test]
        public void TestIsSorted()
        {
           

            string queryString = "SELECT ?s0 ?p0 ?o0 " +
                 "WHERE " +
                 "{{ " +
                    "?s0 ?p0 ?o0 . " +
                    "{{ " +
                     "  SELECT DISTINCT ?s0 " +
                       "WHERE " +
                       "{{ " +
                         " ?s ?p ?o." +
                          "?s <{0}> <{1}> ." +
                          "{{" +
                           "  ?s ?p1 ?o1 ." +
                             "FILTER ISLITERAL(?o1) . FILTER REGEX(STR(?o1), \"\", \"i\") ." +
                          "}}" +
                          "UNION" +
                          "{{" +
                             "?s ?p1 ?s1 ." +
                             "?s1 ?p2 ?o2 ." +
                             "FILTER ISLITERAL(?o2) . FILTER REGEX(STR(?o2), \"\", \"i\") ." +
                          "}}" +
                       "}}" +
                       "ORDER BY ?o" +
                    "}}" +
                 "}}";
            string q = string.Format(queryString, rdf.type.Uri.OriginalString, tmo.Task.Uri.OriginalString);
            SparqlQuery query = new SparqlQuery(q);
            MethodInfo method = query.GetType().GetMethod("IsSorted", BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.AreEqual(true, method.Invoke(query, null));


             queryString = "SELECT ?s0 ?p0 ?o0 " +
                 "WHERE " +
                 "{{ " +
                    "?s0 ?p0 ?o0 . " +
                    "{{ " +
                     "  SELECT DISTINCT ?s0 " +
                       "WHERE " +
                       "{{ " +
                         " ?s ?p ?o." +
                          "?s <{0}> <{1}> ." +
                          "{{" +
                           "  ?s ?p1 ?o1 ." +
                             "FILTER ISLITERAL(?o1) . FILTER REGEX(STR(?o1), \"\", \"i\") ." +
                          "}}" +
                          "UNION" +
                          "{{" +
                             "?s ?p1 ?s1 ." +
                             "?s1 ?p2 ?o2 ." +
                             "FILTER ISLITERAL(?o2) . FILTER REGEX(STR(?o2), \"\", \"i\") ." +
                          "}}" +
                       "}}" +
                    "}}" +
                 "}}";
             q = string.Format(queryString, rdf.type.Uri.OriginalString, tmo.Task.Uri.OriginalString);
             query = new SparqlQuery(q);
             Assert.AreEqual(false, method.Invoke(query, null));


             queryString = @"SELECT DISTINCT ?s0 FROM <http://semiodesk.com/id/8083cf10-5f90-40d4-b30a-c18fea31177b/>
                WHERE
                { { 

                  ?s0 a <http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Visual> . 

                  ?s0 <http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateTime> ?o1 . 

                  ?s0 ?p0 ?o0 . 

                }}
                ORDER BY ASC(?o1) LIMIT 50 ";
             //q = string.Format(queryString, rdf.type.Uri.OriginalString, tmo.Task.Uri.OriginalString);
             query = new SparqlQuery(queryString);
             Assert.AreEqual(true, method.Invoke(query, null));
            

        }


    }

}
