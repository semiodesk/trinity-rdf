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
using System;
using Semiodesk.Trinity.Ontologies;
using Semiodesk.Trinity.Test.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class SparqlUpdateTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            OntologyDiscovery.AddNamespace("vcard", vcard.Namespace);
            OntologyDiscovery.AddNamespace("foaf", foaf.Namespace);
            OntologyDiscovery.AddNamespace("dc", dc.Namespace);
            OntologyDiscovery.AddNamespace("ex", new Uri("http://example.org/"));
        }
        
        [Test]
        public void TestInsert()
        {
            var update = new SparqlUpdate(@"INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");
            var result = Model1.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());

            Model1.Clear();
            
            update = new SparqlUpdate(@"INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title'@en . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title'@en . }");
            result = Model1.ExecuteQuery(query);

            Assert.AreEqual(true, result.GetAnwser());
        }

        [Test]
        public void TestModify()
        {
            var update = new SparqlUpdate(@"
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } };
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title too' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, Model1.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title too' . }");

            Assert.AreEqual(true, Model1.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestMultipleModify()
        {
            var update = new SparqlUpdate(@"
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } };
                INSERT DATA { GRAPH @graph { ex:book2 dc:title 'This is an example title2' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(true, Model1.ExecuteQuery(query).GetAnwser());

            query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title2' . }");

            Assert.AreEqual(true, Model1.ExecuteQuery(query).GetAnwser());
        }


        [Test]
        public void TestDelete()
        {
            var update = new SparqlUpdate(@"
                INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            update = new SparqlUpdate(@"
                DELETE DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            SparqlQuery query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, Model1.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestClear()
        {
            var update = new SparqlUpdate(@"INSERT DATA { GRAPH @graph { ex:book dc:title 'This is an example title' . } }")
                .Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            update = new SparqlUpdate(@"CLEAR GRAPH @graph").Bind("@graph", Model1);

            Model1.ExecuteUpdate(update);

            var query = new SparqlQuery(@"ASK WHERE { ?s dc:title 'This is an example title' . }");

            Assert.AreEqual(false, Model1.ExecuteQuery(query).GetAnwser());
        }

        [Test]
        public void TestUpdateParameters()
        {
            var update = new SparqlUpdate(@"
                DELETE { ?s ?p @oldValue . }
                INSERT { ?s ?p @newValue . }
                WHERE { ?s ?p ?o . }");

            update.Bind("@oldValue", "Fail");
            update.Bind("@newValue", "Success");

            var updateString = update.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(updateString));

            Model1.ExecuteUpdate(update);
            
            // If no exception is thrown then the SPARQL query was valid.
        }
    }
}
