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
using Semiodesk.Trinity.Tests.Query;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class SparqlQueryTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members
        
        private Uri _acme;
        
        private Uri _hans;

        private Uri _hansPager;

        private Uri _hansPhone1;

        private Uri _hansPhone2;
        
        #endregion
        
        #region Methods
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            OntologyDiscovery.AddNamespace("dbpedia", new Uri("http://dbpedia.org/ontology/"));
            OntologyDiscovery.AddNamespace("dbpprop", new Uri("http://dbpedia.org/property/"));
            OntologyDiscovery.AddNamespace("dc", dc.Namespace);
            OntologyDiscovery.AddNamespace("ex", new Uri("http://example.org/"));
            OntologyDiscovery.AddNamespace("foaf", foaf.Namespace);
            OntologyDiscovery.AddNamespace("nco", nco.Namespace);
            OntologyDiscovery.AddNamespace("nfo", nfo.Namespace);
            OntologyDiscovery.AddNamespace("nie", nie.Namespace);
            OntologyDiscovery.AddNamespace("schema", new Uri("http://schema.org/"));
            OntologyDiscovery.AddNamespace("sfo", sfo.Namespace);
            OntologyDiscovery.AddNamespace("vcard", vcard.Namespace);

            _acme = BaseUri.GetUriRef("acme");
            _hans = BaseUri.GetUriRef("hans");
            _hansPhone1 = BaseUri.GetUriRef("hansPhone1");
            _hansPhone2 = BaseUri.GetUriRef("hansPhone2");
            _hansPager = BaseUri.GetUriRef("hansPager");
        }

        private void InitializeModels()
        {
            var hansPager = Model1.CreateResource(_hansPager);
            hansPager.AddProperty(rdf.type, nco.PagerNumber);
            hansPager.AddProperty(dc.date, DateTime.Today);
            hansPager.AddProperty(nco.creator, _hans);
            hansPager.Commit();

            var hansPhone1 = Model1.CreateResource(_hansPhone1);
            hansPhone1.AddProperty(rdf.type, nco.PhoneNumber);
            hansPhone1.AddProperty(dc.date, DateTime.Today.AddDays(1));
            hansPhone1.AddProperty(nco.creator, _hans);
            hansPhone1.Commit();

            var hansPhone2 = Model1.CreateResource(_hansPhone2);
            hansPhone2.AddProperty(rdf.type, nco.PhoneNumber);
            hansPhone2.AddProperty(dc.date, DateTime.Today.AddDays(2));
            hansPhone2.AddProperty(nco.creator, _hans);
            hansPhone2.Commit();

            var hans = Model1.CreateResource(_hans);
            hans.AddProperty(rdf.type, nco.PersonContact);
            hans.AddProperty(nco.fullname, "Hans Wurscht");
            hans.AddProperty(nco.birthDate, DateTime.Now);
            hans.AddProperty(nco.blogUrl, "http://blog.com/Hans");
            hans.AddProperty(nco.hasContactMedium, hansPager);
            hans.AddProperty(nco.hasPhoneNumber, hansPhone1);
            hans.AddProperty(nco.hasPhoneNumber, hansPhone2);
            hans.Commit();

            var acme = Model1.CreateResource(_acme);
            acme.AddProperty(rdf.type, nco.OrganizationContact);
            acme.AddProperty(nco.fullname, "ACME");
            acme.AddProperty(nco.creator, hans);
            acme.Commit();
        }

        [Test]
        public virtual void TestAsk()
        {
            InitializeModels();
            
            // Checking the model using ASK queries.
            var query = new SparqlQuery("ASK WHERE { ?s nco:fullname 'Hans Wurscht' . }");
            var result = Model1.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());

            query = new SparqlQuery("ASK WHERE { ?s nco:fullname 'Hans Meier' . }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(false, result.GetAnwser());
        }

        /// <summary>
        /// An ASK over a sub-select is an ASK, and the model's dataset goes on the ASK.
        /// </summary>
        /// <remarks>
        /// The preprocessor took the form from the last form keyword it read and put the model's <c>FROM</c>
        /// before the first <c>WHERE</c>. Both are the sub-select's here: the query was taken for a SELECT,
        /// so its answer could not be read, and the dataset clause landed where SPARQL forbids one. The LINQ
        /// provider's equality lookup is such a sub-select, which is how <c>Any(…)</c> found it (#64).
        /// </remarks>
        [Test]
        public virtual void TestAskOverASubSelect()
        {
            InitializeModels();

            var query = new SparqlQuery("ASK { { SELECT DISTINCT ?s WHERE { ?s nco:fullname 'Hans Wurscht' . } } }");

            Assert.AreEqual(SparqlQueryType.Ask, query.QueryType);
            Assert.IsTrue(Model1.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery("ASK { { SELECT DISTINCT ?s WHERE { ?s nco:fullname 'Hans Meier' . } } }");

            Assert.IsFalse(Model1.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public virtual void TestSelect()
        {
            InitializeModels();
            
            // Retrieving bound variables using the SELECT query form.
            var query = new SparqlQuery("SELECT ?name ?birthday WHERE { ?x nco:fullname ?name. ?x nco:birthDate ?birthday. }");
            var result = Model1.ExecuteQuery(query);

            Assert.AreEqual(1, result.GetBindings().Count());

            // Retrieving resoures using the SELECT or DESCRIBE query form.
            query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s nco:fullname 'Hans Wurscht'. }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(1, result.GetResources().Count());

            // Test SELECT with custom defined PREFIXes
            query = new SparqlQuery("PREFIX contact: <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#> SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s contact:fullname 'Hans Wurscht'. }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(1, result.GetResources().Count());

            // Check if the select statement only works on the given model.
            query = new SparqlQuery("SELECT * WHERE { ?s ?p ?o. }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(5, result.GetResources().Count());

            // Check that resource creation is done correctly for Resources containing dashes.
            var r0 = Model1.CreateResource(new Uri("http://example.org/Something#0"));
            r0.AddProperty(new Property(new Uri("http://example.org/fullName")), "Thing 1");
            r0.Commit();

            var r1 = Model1.CreateResource(new Uri("http://example.org/Something#1"));
            r1.AddProperty(new Property(new Uri("http://example.org/fullName")), "Thing 2");
            r1.Commit();

            query = new SparqlQuery("SELECT * WHERE { ?s ?p ?o. }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(7, result.GetResources().Count());
        }

        [Test]
        public virtual void TestSelectProvidesStatements()
        {
            var query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o . }");
            var vars = query.GetGlobalScopeVariableNames();

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
        public virtual void TestDescribe()
        {
            InitializeModels();
            
            // The DESCRIBE queries return all resources referenced by Hans. However, GraphDB does not return the full
            // sub-graph describing the pager, phone numbers and ACME. Because of the lacking rdf:types of the related
            // resources they are converted into a PersonContact. This is a bug in GraphDB in my opinion.
            var query = new SparqlQuery("DESCRIBE @hans").Bind("@hans", _hans);
            var result = Model1.ExecuteQuery(query);
            var resources = result.GetResources().ToList();
            
            Assert.LessOrEqual(1, resources.Count);

            var query2 = new SparqlQuery("DESCRIBE ?s WHERE { ?s nco:fullname 'Hans Wurscht'. }");
            var result2 = Model1.ExecuteQuery(query2);
            var contacts = result2.GetResources<PersonContact>().ToList();
            
            Assert.LessOrEqual(1, contacts.Count);

            foreach (var c in contacts)
            {
                Assert.AreEqual(c.GetType(), typeof(PersonContact));
            }
        }

        [Test]
        public virtual void TestConstruct()
        {
            InitializeModels();
            
            var query = new SparqlQuery(@"
                CONSTRUCT
                {
                    ?x nco:fullname ?name .
                    ?x vcard:N _:v .
                    _:v vcard:givenName ?name .
                }
                WHERE
                {
                    ?x nco:fullname ?name .
                }");

            var resources = Model1.GetResources(query).ToList();

            // We expect 4 resources: 2 VCARD blank nodes and the original 2 NCO contacts.
            Assert.AreEqual(4, resources.Count);
            Assert.AreEqual(2, resources.Count(r => r.HasProperty(nco.fullname)));
            Assert.AreEqual(2, resources.Count(r => r.HasProperty(vcard.N)));
            Assert.AreEqual(2, resources.Count(r => r.HasProperty(vcard.givenName)));
        }

        [Test]
        public virtual void TestInferencing()
        {
            InitializeModels();
            
            // Retrieving resources using the model API.
            Assert.IsTrue(Model1.ContainsResource(_hans));
            Assert.IsTrue(Model1.ContainsResource(_acme));

            // This fact is not explicitly stated.
            var query = new SparqlQuery("ASK WHERE { @hans a nco:Role . }").Bind("@hans", _hans);
            var result = Model1.ExecuteQuery(query);
            
            Assert.IsFalse(result.GetAnwser());

            result = Model1.ExecuteQuery(query, true);
            
            Assert.IsTrue(result.GetAnwser());

            // This fact is not explicitly stated.
            query = new SparqlQuery("SELECT ?url WHERE { ?x nco:url ?url . }");
            result = Model1.ExecuteQuery(query);
            
            Assert.AreEqual(0, result.GetBindings().Count());

            result = Model1.ExecuteQuery(query, true);
            
            Assert.AreEqual(1, result.GetBindings().Count());

            query = new SparqlQuery("ASK WHERE { @hans nco:hasContactMedium @hansPhone1 . }")
                .Bind("@hans", _hans)
                .Bind("@hansPhone1", _hansPhone1);
            
            result = Model1.ExecuteQuery(query);
            
            Assert.IsFalse(result.GetAnwser());

            result = Model1.ExecuteQuery(query, true);
            
            Assert.IsTrue(result.GetAnwser());

            query = new SparqlQuery("DESCRIBE ?element WHERE { ?element nco:hasContactMedium @hansPhone1 . }")
                .Bind("@hansPhone1", _hansPhone1);
            
            result = Model1.ExecuteQuery(query);
            
            Assert.AreEqual(0, result.GetResources().Count());

            result = Model1.ExecuteQuery(query, true);
            
            Assert.LessOrEqual(1, result.GetResources().Count());

            // DISTINCT because the assertion below counts contact media, not derivations. Under
            // inferencing `?m rdf:type nco:ContactMedium` is entailed rather than stated, and SPARQL
            // SELECT is a bag: a store that back-chains (Virtuoso) yields one solution per derivation
            // path and returned 8, where one that materializes (GraphDB) returned 3. Neither is wrong,
            // so the query has to say which it means. The three media have distinct dates, so DISTINCT
            // collapses only the duplicates.
            query = new SparqlQuery("SELECT DISTINCT ?date WHERE { ?m rdf:type nco:ContactMedium ; nco:creator @hans ; dc:date ?date . } ORDER BY ASC(?date)")
                .Bind("@hans", _hans);
            
            result = Model1.ExecuteQuery(query, true);

            var bindings = result.GetBindings().ToList();

            Assert.AreEqual(3, bindings.Count);

            DateTime? d0 = null;

            foreach (var d1 in bindings.Select(b => (DateTime)b["date"]))
            {
                if (d0 != null)
                {
                    Assert.Greater(d1, d0);
                }

                d0 = d1;
            }
        }

        [Test]
        public virtual void TestModelApi()
        {
            InitializeModels();

            var peter = BaseUri.GetUriRef("peter");
            
            // Retrieving resources using the model API.
            Assert.AreEqual(true, Model1.ContainsResource(_hans));
            Assert.AreEqual(false, Model1.ContainsResource(peter));

            var hans = Model1.GetResource(_hans);
            
            Assert.Throws<ResourceNotFoundException>(delegate { Model1.GetResource(peter); });

            hans = Model1.GetResource(_hans, typeof(Resource)) as IResource;
            
            Assert.NotNull(hans);
        }

        /// <summary>
        /// Every literal a caller writes, in any of SPARQL's four quoting forms, comes out of the
        /// preprocessor as one short double-quoted literal holding the same value.
        /// </summary>
        /// <remarks>
        /// This used to assert that the output still contained a raw newline and a <c>\'</c>, which
        /// checked that the long form survived rather than that the values did. It is now decided by
        /// parsing the output and reading the values back.
        /// </remarks>
        [Test]
        public virtual void TestEscaping()
        {
            var query = new SparqlQuery(@"
                SELECT ?s ?p ?o WHERE
                {
                    ?s ?p ""Hello World"" .
                    ?s ?p ""'Hello World'"" .
                    ?s ?p '''Hello
World''' .
                    ?s ?p 'C:\\Directory\\file.ext' .
                }");

            var queryString = query.ToString();

            Assert.IsFalse(queryString.Contains('\n'), "a newline in a value is escaped, not written raw");

            var parsed = new VDS.RDF.Parsing.SparqlQueryParser().ParseFromString(queryString);
            var values = parsed.RootGraphPattern.TriplePatterns
                .OfType<VDS.RDF.Query.Patterns.TriplePattern>()
                .Select(t => ((VDS.RDF.ILiteralNode)((VDS.RDF.Query.Patterns.NodeMatchPattern)t.Object).Node).Value)
                .ToList();

            CollectionAssert.AreEqual(new[]
            {
                "Hello World",
                "'Hello World'",
                "Hello\nWorld",
                "C:\\Directory\\file.ext",
            }, values.Select(v => v.Replace("\r\n", "\n")));
        }

        /// <summary>
        /// Any string value, bound into a query or compared in LINQ, matches exactly the resources that
        /// hold it in the queried model, and nothing in another graph (ADR-0052).
        /// </summary>
        /// <remarks>
        /// The decoys hold the same values in <c>Model2</c>, so a value that ended its literal early and
        /// widened the query - to another graph, or to every subject - would return one. The LINQ writer
        /// always escaped correctly; until 2.0 the preprocessor re-wrote its literals in the long form,
        /// which did not.
        /// </remarks>
        [Test]
        public virtual void AnyStringValueMatchesExactlyAndOnlyInTheModel()
        {
            var victim = Store.GetModel(BaseUri.GetUriRef("any-string-victim"));

            try
            {
                string[] values = HostileLiterals.Values(victim.Uri);
                var subjects = new UriRef[values.Length];

                for (int i = 0; i < values.Length; i++)
                {
                    subjects[i] = BaseUri.GetUriRef("any-string-" + i);

                    var contact = Model1.CreateResource<PersonContact>(subjects[i]);
                    contact.NameGiven = values[i];
                    contact.Commit();

                    var decoy = Model2.CreateResource<PersonContact>(BaseUri.GetUriRef("any-string-decoy-" + i));
                    decoy.NameGiven = values[i];
                    decoy.Commit();
                }

                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i];
                    string name = SparqlLiteralOracle.Display(value);

                    var bound = new SparqlQuery("SELECT ?s WHERE { ?s @predicate @value . }")
                        .Bind("@predicate", new Uri(NCO.nameGiven))
                        .Bind("@value", value);

                    CollectionAssert.AreEqual(new[] { subjects[i].OriginalString },
                        Model1.GetBindings(bound).Select(b => ((Uri)b["s"]).OriginalString), "bound " + name);

                    CollectionAssert.AreEqual(new[] { subjects[i].OriginalString },
                        Model1.AsQueryable<PersonContact>().Where(c => c.NameGiven == value).ToList().Select(c => c.Uri.OriginalString),
                        "LINQ == " + name);

                    if (value.Length > 0)
                    {
                        var containing = Model1.AsQueryable<PersonContact>().Where(c => c.NameGiven.Contains(value)).ToList()
                            .Select(c => c.Uri.OriginalString).ToList();

                        CollectionAssert.Contains(containing, subjects[i].OriginalString, "LINQ Contains " + name);
                        CollectionAssert.IsSubsetOf(containing, subjects.Select(s => s.OriginalString), "LINQ Contains " + name);
                    }
                }

                HostileLiterals.AssertNothingEscaped(Store, Model1, victim);
            }
            finally
            {
                victim.Clear();
            }
        }

        /// <summary>
        /// A carriage return in a value survives the bindings of a <c>SELECT</c>.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="AnyStringValueMatchesExactlyAndOnlyInTheModel"/>, which reads values
        /// through <c>GetResource</c>, because a backend can lose a CR on this path only. Oxigraph did: it
        /// was asked for SPARQL XML results first, and an XML parser normalizes a raw CR to LF, while the
        /// stored value stayed exact. Its connector now asks for JSON results first.
        /// </remarks>
        [Test]
        public virtual void BindingsPreserveACarriageReturn()
        {
            foreach (string value in new[] { "carriage\rreturn", "windows\r\nline" })
            {
                var uri = BaseUri.GetUriRef("carriage-return");

                var contact = Model1.CreateResource<PersonContact>(uri);
                contact.NameGiven = value;
                contact.Commit();

                var query = new SparqlQuery("SELECT ?o WHERE { @subject @predicate ?o }")
                    .Bind("@subject", uri)
                    .Bind("@predicate", new Uri(NCO.nameGiven));

                Assert.AreEqual(value, Model1.GetBindings(query).Single()["o"], SparqlLiteralOracle.Display(value));

                Model1.Clear();
            }
        }

        [Test]
        public virtual void TestUriEscaping()
        {
            var fileUri = new Uri("file:///F:/test/02%20-%20Take%20Me%20Somewhere%20Nice.mp3");
            
            var file = Model1.CreateResource(fileUri);
            file.AddProperty(foaf.name, "Name");
            file.Commit();

            var result = Model1.GetResource(fileUri);
            
            Assert.AreEqual(file.Uri, result.Uri);
            Assert.AreEqual(file, result);
        }

        [Test]
        public virtual void TestQueryParameters()
        {
            var query = new SparqlQuery(@"SELECT ?s WHERE { ?s ?p ?o . ?s ?p @someValue . }")
                .Bind("@someValue", "Value");

            var queryString = query.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(queryString));

            // A caller may write either quote; the preprocessor writes every literal back in the one
            // form SparqlSerializer.SerializeString produces.
            query = new SparqlQuery(@"SELECT ?s WHERE { ?s ?p 'Hallo'@de . }");
            queryString = query.ToString();

            Assert.AreEqual(@"SELECT ?s WHERE { ?s ?p ""Hallo""@de . }", queryString);

            query = new SparqlQuery(@"SELECT ?s WHERE { ?s ?p 'Hallo'@de-de . }");
            queryString = query.ToString();

            Assert.AreEqual(@"SELECT ?s WHERE { ?s ?p ""Hallo""@de-de . }", queryString);
        }

        [Test]
        public virtual void TestSelectCount()
        {
            InitializeModels();
            
            var query = new SparqlQuery("SELECT ( COUNT(?s) AS ?count ) WHERE { ?s rdf:type nco:PhoneNumber. }");
            var result = Model1.ExecuteQuery(query);
            var bindings = result.GetBindings();
            
            Assert.AreEqual(1, bindings.Count());
            Assert.AreEqual(2, bindings.First()["count"]);
        }

        [Test]
        public virtual void TestCount()
        {
            InitializeModels();
            
            var query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s rdf:type nco:PhoneNumber. }");
            var result = Model1.ExecuteQuery(query);

            Assert.AreEqual(2, result.Count());

            query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s rdf:type nco:PhoneNumber. }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public virtual void TestSetModel()
        {
            var fromClause = new Regex(Regex.Escape("FROM"));

            var query = new SparqlQuery("SELECT COUNT(?s) AS ?count WHERE { ?s ?p ?o . }");

            Assert.IsNull(query.Model);
            Assert.AreEqual(0, fromClause.Matches(query.ToString()).Count);

            query.Model = Model1;

            Assert.NotNull(query.Model);
            Assert.AreEqual(1, fromClause.Matches(query.ToString()).Count);

            var query2 = new SparqlQuery($"ASK FROM <{Model1.Uri}> WHERE {{ ?s ?p ?o . }}");

            Assert.IsNull(query2.Model);
            Assert.AreEqual(1, fromClause.Matches(query2.ToString()).Count);

            query2.Model = Model1;

            Assert.IsNotNull(query2.Model);
            Assert.AreEqual(1, fromClause.Matches(query2.ToString()).Count);

            var query3 = new SparqlQuery("ASK FROM @graph WHERE { ?s ?p ?o . }").Bind("@graph", Model1);

            Assert.IsNull(query3.Model);
            Assert.AreEqual(1, fromClause.Matches(query3.ToString()).Count);

            query3.Model = Model1;

            Assert.IsNotNull(query3.Model);
            Assert.AreEqual(1, fromClause.Matches(query3.ToString()).Count);

            var query4 = new SparqlQuery("ASK FROM @graph WHERE { ?s ?p ?o . }");
            query4.Model = Model1;

            Assert.IsNotNull(query4.Model);
            Assert.Throws<ArgumentException>(delegate { query4.Bind("@graph", Model1); });
        }

        [Test]
        public virtual void TestSetLimit()
        {
            InitializeModels();
            
            var query = new SparqlQuery(@"SELECT ?s ?p ?o WHERE { ?s ?p ?o . }");

            var method = query.GetType().GetMethod("SetLimit", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(query, new object[] { 3 });
            
            var result = Model1.ExecuteQuery(query);
            var bindings = result.GetBindings().ToList();
            
            Assert.AreEqual(3, bindings.Count);
        }

        [Test]
        public virtual void TestModelGroup()
        {
            InitializeModels();
            
            var group = Store.CreateModelGroup(Model1, Model2);
            
            var query = new SparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o. ?s nco:fullname 'Hans Wurscht'. }");
            var result = group.ExecuteQuery(query);
            var resources = result.GetResources().ToList();
            
            Assert.AreEqual(1, resources.Count);
        }
        
        [Test]
        public virtual void TestBindLimit()
        {
            InitializeModels();

            var query = new SparqlQuery("SELECT DISTINCT ?s WHERE { ?s ?p ?o. } LIMIT @limit");
            query.Bind("@limit", 1);
            
            var result = Model1.ExecuteQuery(query);
            var resources = result.GetBindings().ToList();
            
            Assert.AreEqual(1, resources.Count);
        }
        
        [Test]
        public virtual void TestBindOffset()
        {
            InitializeModels();

            var query = new SparqlQuery("SELECT DISTINCT ?s WHERE { ?s ?p ?o. } ORDER BY ?s");
            var result = Model1.ExecuteQuery(query);
            
            var expected = result.GetBindings().ToList();
            
            Assert.AreEqual(5, expected.Count);
            
            var query0 = new SparqlQuery("SELECT DISTINCT ?s WHERE { ?s ?p ?o. } ORDER BY ?s OFFSET @offset");
            query0.Bind("@offset", 0);
            
            var b0 = Model1.ExecuteQuery(query0).GetBindings().First();
            
            Assert.AreEqual(expected[0].ToString(), b0.ToString());
            
            var query1 = new SparqlQuery("SELECT DISTINCT ?s WHERE { ?s ?p ?o. } ORDER BY ?s OFFSET @offset");
            query1.Bind("@offset", 1);
            
            var b1 = Model1.ExecuteQuery(query1).GetBindings().First();
            
            Assert.AreEqual(expected[1].ToString(), b1.ToString());
        }
        
        #endregion
    }
}
