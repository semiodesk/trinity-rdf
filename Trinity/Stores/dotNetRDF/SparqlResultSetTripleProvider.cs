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
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Store
{
    class SparqlResultSetTripleProvider : ITripleProvider
    {
        SparqlResultSet _set;
        string _subjectVar;
        string _predicateVar;
        string _objectVar;

        int counter;
        public SparqlResultSetTripleProvider(SparqlResultSet set, string subjectVar, string predicateVar, string objectVar)
        {
            _set = set;
            counter = 0;

            _subjectVar = subjectVar;
            _predicateVar = predicateVar;
            _objectVar = objectVar;
        }

        public int Count
        {
            get { return _set.Count; }
        }

        public void Reset()
        {
            counter = 0;
        }


        public bool HasNext
        {
            get { return counter < _set.Count; }
        }

        public void SetNext()
        {
            counter += 1;
        }

        public INode S
        {
            get { return _set[counter][_subjectVar]; }
        }

        public Uri P
        {
            get { return (_set[counter][_predicateVar] as UriNode).Uri; }
        }

        public INode O
        {
            get { return _set[counter][_objectVar]; }
        }
    }


}
