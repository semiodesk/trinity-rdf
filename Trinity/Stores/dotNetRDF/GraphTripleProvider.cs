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
using System.Linq;
using VDS.RDF;

namespace Semiodesk.Trinity.Store
{
    internal class GraphTripleProvider : ITripleProvider
    {
        #region Members

        private int _n;

        private readonly Triple[] _triples;

        public INode S
        {
            get { return _triples[_n].Subject; }
        }

        public Uri P
        {
            get { return (_triples[_n].Predicate as UriNode).Uri; }
        }

        public INode O
        {
            get { return _triples[_n].Object; }
        }

        public int Count
        {
            get { return _triples.Length; }
        }

        public bool HasNext
        {
            get { return _n < _triples.Length; }
        }

        #endregion

        #region Constructors

        public GraphTripleProvider(IGraph graph)
        {
            _n = 0;

            // IGraph.Triples has no indexer: ElementAt(k) re-enumerates from the start, which made reading
            // a graph of n triples O(n^2) -- every GetResource<T>, since each is a DESCRIBE (#63). One
            // snapshot, then index into it. It enumerates in the order ElementAt did, and GenerateResources
            // depends on that order, because it keeps a handle on the current subject.
            _triples = graph.Triples.ToArray();
        }

        #endregion

        #region Methods

        public void Reset()
        {
            _n = 0;
        }

        public void SetNext()
        {
            _n += 1;
        }

        #endregion
    }
}
