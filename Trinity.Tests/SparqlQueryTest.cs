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
using System.Text.RegularExpressions;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    public class SparqlQueryTest
    {
        protected IStore Store;

        protected IModel Model;

        protected NamespaceManager NamespaceManager = new NamespaceManager();


        [SetUp]
        public void SetUp()
        {
            string connectionString = SetupClass.ConnectionString;

            Store = StoreFactory.CreateStore(string.Format("{0};rule=urn:semiodesk/test/ruleset", connectionString));
            Store.LoadOntologySettings();
            Model = Store.GetModel(new Uri("http://example.org/TestModel"));

            if (!Model.IsEmpty)
            {
                Model.Clear();
            }

            OntologyDiscovery.AddNamespace("ex", new Uri("http://example.org/"));
            OntologyDiscovery.AddNamespace("dc", new Uri("http://purl.org/dc/elements/1.1/"));
            OntologyDiscovery.AddNamespace("vcard", new Uri("http://www.w3.org/2001/vcard-rdf/3.0#"));
            OntologyDiscovery.AddNamespace("foaf", new Uri("http://xmlns.com/foaf/0.1/"));
            OntologyDiscovery.AddNamespace("dbpedia", new Uri("http://dbpedia.org/ontology/"));
            OntologyDiscovery.AddNamespace("dbpprop", new Uri("http://dbpedia.org/property/"));
            OntologyDiscovery.AddNamespace("schema", new Uri("http://schema.org/"));
            OntologyDiscovery.AddNamespace("nie", new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#"));
            OntologyDiscovery.AddNamespace("nco", new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#"));
            OntologyDiscovery.AddNamespace("sfo", sfo.GetNamespace());
            OntologyDiscovery.AddNamespace(nfo.GetPrefix(), nfo.GetNamespace());

            Model.Clear();

            IResource resource0 = Model.CreateResource(new Uri("http://example.org/Hans"));
            resource0.AddProperty(rdf.type, nco.PersonContact);
            resource0.AddProperty(nco.fullname, "Hans Wurscht");
            resource0.AddProperty(nco.birthDate, DateTime.Now);
            resource0.AddProperty(nco.blogUrl, "http://blog.com/Hans");
            resource0.Commit();

            IResource resource1 = Model.CreateResource(new Uri("http://example.org/Organization"));
            resource1.AddProperty(rdf.type, nco.OrganizationContact);
            resource1.AddProperty(nco.fullname, "ACME");
            resource1.AddProperty(nco.creator, resource0);
            resource1.Commit();

            IResource resource2 = Model.CreateResource(new Uri("http://example.org/PhoneNumber"));
            resource2.AddProperty(rdf.type, nco.PhoneNumber);
            resource2.AddProperty(dc.date, DateTime.Today);
            resource2.AddProperty(nco.creator, resource0);
            resource2.Commit();

            // NOTE: The different name influences the ordering of the resource in query results.
            IResource resource3 = Model.CreateResource(new Uri("http://example.org/Boc#2"));
            resource3.AddProperty(rdf.type, nco.PagerNumber);
            resource3.AddProperty(dc.date, DateTime.Today.AddHours(1));
            resource3.AddProperty(nco.creator, resource0);
            resource3.Commit();

            IResource resource4 = Model.CreateResource(new Uri("http://example.org/Doc#3"));
            resource4.AddProperty(rdf.type, nco.PhoneNumber);
            resource4.AddProperty(dc.date, DateTime.Today.AddHours(2));
            resource4.AddProperty(nco.creator, resource0);
            resource4.Commit();

            resource0.AddProperty(nco.hasPhoneNumber, resource2);
            resource0.Commit();
        }

        [TearDown]
        public void TearDown()
        {
            Model.Clear();

            if (Store != null)
            {
                Store.Dispose();
            }
        }

        [Test]
        public void TestAsk()
        {
            // Checking the model using ASK queries.
            SparqlQuery query = new SparqlQuery("ASK WHERE { ?s nco:fullname 'Hans Wurscht' . }");
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());

            query = new SparqlQuery("ASK WHERE { ?s nco:fullname 'Hans Meier' . }");
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(false, result.GetAnwser());
        }

        [Test]
        public void TestSelect()
        {
            // Retrieving bound variables using the SELECT query form.
            SparqlQuery query = new SparqlQuery("SELECT ?name ?birthday WHERE { ?x nco:fullname ?name. ?x nco:birthDate ?birthday. }");
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            Assert.AreEqual(1, result.GetBindings().Count());

            // Retrieving resoures using the SELECT or DESCRIBE query form.
            query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s nco:fullname 'Hans Wurscht'. }");
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
        public void TestSelectProvidesStatements()
        {
            SparqlQuery query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o . }");

            string[] vars = query.GetGlobalScopeVariableNames();

            Assert.IsTrue(query.ProvidesStatements());
            Assert.AreEqual("s", vars[0]);
            Assert.AreEqual("p", vars[1]);
            Assert.AreEqual("o", vars[2]);

            query = new SparqlQuery("SELECT * WHERE { ?s ?p ?o . }");

            vars = query.GetGlobalScopeVariableNames();

            Assert.IsTrue(query.ProvidesStatements());
            Assert.AreEqual("s", vars[0]);
            Assert.AreEqual("p", vars[1]);
            Assert.AreEqual("o", vars[2]);

            query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o . ?x ?y ?z . }");

            vars = query.GetGlobalScopeVariableNames();

            Assert.IsTrue(query.ProvidesStatements());
            Assert.AreEqual("s", vars[0]);
            Assert.AreEqual("p", vars[1]);
            Assert.AreEqual("o", vars[2]);

            query = new SparqlQuery("SELECT * WHERE { ?s ?p ?o . ?x ?y ?z . }");

            vars = query.GetGlobalScopeVariableNames();

            Assert.IsFalse(query.ProvidesStatements());
            Assert.AreEqual(6, vars.Length);

            query = new SparqlQuery(@"
                PREFIX nie: <http://www.semanticdesktop.org/ontologies/2007/01/19/nie#>
                PREFIX artpro: <http://semiodesk.com/artivitypro/1.0/>

                SELECT ?s ?p ?o WHERE
                {
                       ?s ?p ?o .
                       ?s a artpro:Project .
                       ?s nie:lastModified ?lastModified .

                       FILTER isIRI(?s)
                }
                ORDER BY DESC(?lastModified)");

            vars = query.GetGlobalScopeVariableNames();

            Assert.IsTrue(query.ProvidesStatements());
            Assert.AreEqual("s", vars[0]);
            Assert.AreEqual("p", vars[1]);
            Assert.AreEqual("o", vars[2]);
        }

        [Test]
        public void TestDescribe()
        {
            SparqlQuery query = new SparqlQuery("DESCRIBE <http://example.org/Hans>");
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            IList resources = result.GetResources().ToList();
            Assert.AreEqual(1, resources.Count);

            query = new SparqlQuery("DESCRIBE ?s WHERE { ?s nco:fullname 'Hans Wurscht'. }");
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
            SparqlQuery query = new SparqlQuery(@"
                CONSTRUCT
                {
                    ?x  vcard:N _:v .
                    _:v vcard:givenName ?name .
                }
                WHERE
                {
                    ?x nco:fullname ?name .
                }");

            ISparqlQueryResult result = Model.ExecuteQuery(query);

            IList resources = result.GetResources().ToList();
            Assert.AreEqual(2, resources.Count);
        }

        [Test]
        public void TestInferencing()
        {

            // Retrieving resources using the model API.
            Assert.AreEqual(true, Model.ContainsResource(new Uri("http://example.org/Hans")));
            Assert.AreEqual(true, Model.ContainsResource(new Uri("http://example.org/PhoneNumber")));

            SparqlQuery query;
            ISparqlQueryResult result;

            // This fact is not explicitly stated.
            query = new SparqlQuery("ASK WHERE { <http://example.org/Hans> a nco:Role . }");

            result = Model.ExecuteQuery(query);
            Assert.IsFalse(result.GetAnwser());

            result = Model.ExecuteQuery(query, true);
            Assert.IsTrue(result.GetAnwser());

            // This fact is not explicitly stated.
            query = new SparqlQuery("SELECT ?url WHERE { ?s nco:url ?url . }");

            result = Model.ExecuteQuery(query);
            Assert.AreEqual(0, result.GetBindings().Count());

            result = Model.ExecuteQuery(query, true);
            Assert.AreEqual(1, result.GetBindings().Count());

            query = new SparqlQuery("ASK WHERE { <http://example.org/Hans> nco:hasContactMedium <http://example.org/PhoneNumber> . }");

            result = Model.ExecuteQuery(query);
            Assert.IsFalse(result.GetAnwser());

            result = Model.ExecuteQuery(query, true);
            Assert.IsTrue(result.GetAnwser());

            query = new SparqlQuery("DESCRIBE ?element WHERE { ?element nco:hasContactMedium <http://example.org/PhoneNumber> . }");

            result = Model.ExecuteQuery(query);
            Assert.AreEqual(0, result.GetResources().Count());

            result = Model.ExecuteQuery(query, true);
            Assert.AreEqual(1, result.GetResources().Count());

            query = new SparqlQuery("DESCRIBE ?doc WHERE { ?doc rdf:type nco:ContactMedium . ?doc nco:creator <http://example.org/Hans> . ?doc dc:date ?date . } ORDER BY ASC(?date)");
            result = Model.ExecuteQuery(query, true);
            Assert.AreEqual(3, result.GetResources().Count());

            DateTime? c = null;
            DateTime? d = null;

            foreach (Resource r in result.GetResources())
            {
                d = (DateTime)r.GetValue(dc.date);

                if (c != null) { Assert.Greater(d, c); }

                c = d;
            }
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
        public void TestEscaping()
        {
            SparqlQuery query = new SparqlQuery(@"
                SELECT ?s ?p ?o WHERE
                {
                    ?s ?p ""Hello World"" .
                    ?s ?p ""'Hello World'"" .
                    ?s ?p '''Hello 
                             World''' .
                    ?s ?p 'C:\\Directory\\file.ext' .
                }");

            string queryString = query.ToString();

            Assert.IsTrue(queryString.Contains('\n'));
            Assert.IsTrue(queryString.Contains("\\\\"));
            Assert.IsTrue(queryString.Contains("\\'"));
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
        public void TestQueryParameters()
        {
            SparqlQuery query = new SparqlQuery(@"SELECT ?s WHERE { ?s ?p ?o . ?s ?p @someValue . }");

            query.Bind("@someValue", "Value");

            string queryString = query.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(queryString));

            query = new SparqlQuery(@"SELECT ?s WHERE { ?s ?p 'Hallo'@de . }");

            queryString = query.ToString();

            Assert.AreEqual(queryString, @"SELECT ?s WHERE { ?s ?p 'Hallo'@de . }");

            query = new SparqlQuery(@"SELECT ?s WHERE { ?s ?p 'Hallo'@de-de . }");

            queryString = query.ToString();

            Assert.AreEqual(queryString, @"SELECT ?s WHERE { ?s ?p 'Hallo'@de-de . }");
        }

        [Test]
        public void TestSelectCount()
        {
            SparqlQuery query = new SparqlQuery("SELECT COUNT(?s) AS ?count WHERE { ?s rdf:type nco:PhoneNumber. }");
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            var bindings = result.GetBindings();
            Assert.AreEqual(1, bindings.Count());
            Assert.AreEqual(2, bindings.First()["count"]);
        }

        [Test]
        public void TestCount()
        {
            SparqlQuery query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s rdf:type nco:PhoneNumber. }");
            ISparqlQueryResult result = Model.ExecuteQuery(query);

            Assert.AreEqual(2, result.Count());

            query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s rdf:type nco:PhoneNumber. }");
            result = Model.ExecuteQuery(query);

            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void TestSetModel()
        {
            Regex expression = new Regex(Regex.Escape("FROM"));

            SparqlQuery query = new SparqlQuery("SELECT COUNT(?s) AS ?count WHERE { ?s ?p ?o . }");

            Assert.IsNull(query.Model);
            Assert.AreEqual(0, expression.Matches(query.ToString()).Count);

            query.Model = Model;

            Assert.NotNull(query.Model);
            Assert.AreEqual(1, expression.Matches(query.ToString()).Count);

            SparqlQuery query2 = new SparqlQuery("ASK FROM <http://example.org/TestModel> WHERE { ?s ?p ?o . }");

            Assert.IsNull(query2.Model);
            Assert.AreEqual(1, expression.Matches(query2.ToString()).Count);

            query2.Model = Model;

            Assert.IsNotNull(query2.Model);
            Assert.AreEqual(1, expression.Matches(query2.ToString()).Count);

            SparqlQuery query3 = new SparqlQuery("ASK FROM @graph WHERE { ?s ?p ?o . }");
            query3.Bind("@graph", Model);

            Assert.IsNull(query3.Model);
            Assert.AreEqual(1, expression.Matches(query3.ToString()).Count);

            query3.Model = Model;

            Assert.IsNotNull(query3.Model);
            Assert.AreEqual(1, expression.Matches(query3.ToString()).Count);

            SparqlQuery query4 = new SparqlQuery("ASK FROM @graph WHERE { ?s ?p ?o . }");
            query4.Model = Model;

            Assert.IsNotNull(query4.Model);

            Assert.Throws<ArgumentException>(delegate { query4.Bind("@graph", Model); });
        }

        [Test]
        public void TestSetLimit()
        {
            SparqlQuery query = new SparqlQuery(@"
                SELECT ?s0 ?p0 ?o0 WHERE
                {
                    ?s0 ?p0 ?o0 .
                    {
                        SELECT DISTINCT ?s0 WHERE
                        {
                            ?s ?p ?o.
                            ?s @type @class .

                            {
                                ?s ?p1 ?o1 .
                                FILTER ISLITERAL(?o1) . FILTER REGEX(STR(?o1), '', 'i') .
                            }
                            UNION
                            {
                                ?s ?p1 ?s1 .
                                ?s1 ?p2 ?o2 .
                                FILTER ISLITERAL(?o2) . FILTER REGEX(STR(?o2), '', 'i') .
                            }
                       }
                       ORDER BY ?o
                    }
                }");

            query.Bind("@type", rdf.type);
            query.Bind("@class", tmo.Task);
            
            MethodInfo method = query.GetType().GetMethod("SetLimit", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(query, new object[] { 10 });

            ISparqlQueryResult result = Model.ExecuteQuery(query);

            List<Resource> resources = result.GetResources().ToList();
        }

        [Test]
        public void TestModelGroup()
        {
            Uri modelUri1 = new Uri("http://example.org/TestModel1");
            Uri modelUri2 = new Uri("http://example.org/TestModel2");
            IModelGroup g = Store.CreateModelGroup(modelUri1, modelUri2);
            var query = new SparqlQuery("PREFIX nco: <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#> SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s nco:fullname 'Hans Wurscht'. }");
            query.Model = g;
            var x = query.ToString();


        }
    }
}
