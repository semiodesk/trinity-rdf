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
// Copyright (c) Semiodesk GmbH 2017


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;

namespace Semiodesk.Trinity.Store
{
    class GraphTripleProvider : ITripleProvider
    {
        IGraph _graph;
        int counter;
        public GraphTripleProvider(IGraph graph)
        {
            _graph = graph;
            counter = 0;
        }

        public int Count
        {
            get { return _graph.Triples.Count; }
        }

        public void Reset()
        {
            counter = 0;
        }


        public bool HasNext
        {
            get { return counter < _graph.Triples.Count; }
        }

        public void SetNext()
        {
            counter += 1;
        }

        public INode S
        {
            get { return _graph.Triples.ElementAt(counter).Subject; }
        }

        public Uri P
        {
            get { return (_graph.Triples.ElementAt(counter).Predicate as UriNode).Uri; }
        }

        public INode O
        {
            get { return _graph.Triples.ElementAt(counter).Object; }
        }
    }


}
