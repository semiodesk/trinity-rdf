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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.Diagnostics;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    public class SparqlEndpointTest
    {

        [SetUp]
        public void SetUp()
        {

        }

        [TearDown]
        public void TearDown()
        {

        }


        [Test]
        public void QueryDBPediaTest()
        {

            var store = StoreFactory.CreateStore("provider=sparqlendpoint;endpoint=http://live.dbpedia.org/sparql");
            IModel m = store.GetModel(new Uri("http://dbpedia.org"));
            ResourceQuery q = new ResourceQuery();
            Property wikiPageID = new Property(new Uri("http://dbpedia.org/ontology/wikiPageID"));
            q.Where(wikiPageID, 445980);
            var result = m.ExecuteQuery(q); 
            var b = result.GetResources();
            foreach (var res in b)
            {
                Console.WriteLine(res);
            }
        }

        [Test]
        public void GetResourceDBPediaTest()
        {

            var store = StoreFactory.CreateStore("provider=sparqlendpoint;endpoint=http://live.dbpedia.org/sparql");
            IModel m = store.GetModel(new Uri("http://dbpedia.org"));
            IResource res = m.GetResource(new Uri("http://dbpedia.org/resource/Munich"));
            
        }

        //[Test]
        public void QueryWordnetTest()
        {
            var store = StoreFactory.CreateStore("provider=sparqlendpoint;endpoint=http://wordnet.rkbexplorer.com/sparql/");
            IModel m = store.GetModel(new Uri("http://wordnet.rkbexplorer.com/sparql/"));
            ResourceQuery q = new ResourceQuery();
            q.Where(rdfs.label, "eat");
            var result = m.ExecuteQuery(q);
            var b = result.GetResources();
            foreach (var res in b)
            {
                Console.WriteLine(res);
            }
        }

  
    }
}
