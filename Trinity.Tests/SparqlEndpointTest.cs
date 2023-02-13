﻿// LICENSE:
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
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class SparqlEndpointTest
    {
        [Test]
        public void TestDBPediaQuery()
        {
            Assert.Inconclusive("Endpoint doesn't seem to exist anymore.");
            IStore store = StoreFactory.CreateSparqlEndpointStore(new Uri("http://live.dbpedia.org/sparql"));
            IModel model = store.GetModel(new Uri("http://dbpedia.org"));

            SparqlQuery query = new SparqlQuery(@"SELECT ?s ?p ?o WHERE { ?s ?p ?o . ?s <http://dbpedia.org/ontology/wikiPageID> @id . }");
            query.Bind("@id", 445980);

            Assert.AreEqual(1, model.ExecuteQuery(query).GetResources().Count());
        }

        [Test]
        public void TestDBPediaGetResource()
        {
            Assert.Inconclusive("Endpoint doesn't seem to exist anymore.");
            IStore store = StoreFactory.CreateSparqlEndpointStore(new Uri("http://live.dbpedia.org/sparql"));
            IModel model = store.GetModel(new Uri("http://dbpedia.org"));

            IResource r = model.GetResource(new Uri("http://dbpedia.org/resource/Munich"));

            Assert.Greater(r.ListProperties().Count(), 0);
        }
    }
}
