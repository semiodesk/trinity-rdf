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
//
// Copyright (c) Semiodesk GmbH 2026


using NUnit.Framework;
using Semiodesk.Trinity.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// Pins what <see cref="GraphTripleProvider"/> yields: every triple of the graph, in the graph's own
    /// order, and the same again after <see cref="GraphTripleProvider.Reset"/>.
    /// </summary>
    /// <remarks>
    /// The provider used to read the graph by position, with <c>ElementAt</c>, which re-enumerates from the
    /// start on every call and made each <c>GetResource&lt;T&gt;</c> quadratic in the resource's size (#63).
    /// It now snapshots the triples once. Both properties asserted here are what <c>dotNetRDFQueryResult</c>
    /// relies on: it reads the provider twice (types first, then values), and the second pass keeps a handle
    /// on the current subject, so an order that differed between the passes would split a resource.
    /// </remarks>
    [TestFixture]
    public class GraphTripleProviderTest
    {
        private static IGraph CreateGraph()
        {
            var graph = new Graph();

            var subjects = new List<INode>
            {
                graph.CreateUriNode(new Uri("http://example.org/provider/s0")),
                graph.CreateUriNode(new Uri("http://example.org/provider/s1")),
                graph.CreateUriNode(new Uri("http://example.org/provider/s2")),
                graph.CreateBlankNode(),
            };

            foreach (var s in subjects)
            {
                for (int i = 0; i < 75; i++)
                {
                    var p = graph.CreateUriNode(new Uri("http://example.org/provider/p" + i));

                    INode o = i % 2 == 0
                        ? graph.CreateLiteralNode("v" + i)
                        : (INode)graph.CreateUriNode(new Uri("http://example.org/provider/o" + i));

                    graph.Assert(new Triple(s, p, o));
                }
            }

            return graph;
        }

        private static List<Tuple<INode, Uri, INode>> ReadAll(GraphTripleProvider provider)
        {
            var result = new List<Tuple<INode, Uri, INode>>();

            while (provider.HasNext)
            {
                result.Add(Tuple.Create(provider.S, provider.P, provider.O));

                provider.SetNext();
            }

            return result;
        }

        private static List<Tuple<INode, Uri, INode>> Expected(IGraph graph)
        {
            return graph.Triples
                .Select(t => Tuple.Create(t.Subject, ((IUriNode)t.Predicate).Uri, t.Object))
                .ToList();
        }

        [Test]
        public void YieldsEveryTripleInTheGraphsOrder()
        {
            var graph = CreateGraph();
            var provider = new GraphTripleProvider(graph);

            Assert.AreEqual(300, graph.Triples.Count, "the fixture itself");
            Assert.AreEqual(graph.Triples.Count, provider.Count);

            CollectionAssert.AreEqual(Expected(graph), ReadAll(provider));
        }

        [Test]
        public void YieldsTheSameSequenceAgainAfterReset()
        {
            var graph = CreateGraph();
            var provider = new GraphTripleProvider(graph);

            var first = ReadAll(provider);

            Assert.IsFalse(provider.HasNext);

            provider.Reset();

            CollectionAssert.AreEqual(first, ReadAll(provider));
        }
    }
}
