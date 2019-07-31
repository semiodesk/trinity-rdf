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
using VDS.RDF;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Store
{
    internal class SparqlResultSetTripleProvider : ITripleProvider
    {
        private int _n;

        private SparqlResultSet _set;

        private string _subjectKey;

        public INode S
        {
            get { return _set[_n][_subjectKey]; }
        }

        private string _predicateKey;

        public Uri P
        {
            get { return (_set[_n][_predicateKey] as UriNode).Uri; }
        }

        private string _objectKey;

        public INode O
        {
            get { return _set[_n][_objectKey]; }
        }

        public int Count
        {
            get { return _set.Count; }
        }

        public bool HasNext
        {
            get { return _n < _set.Count; }
        }

        #region Constructors
  
        public SparqlResultSetTripleProvider(SparqlResultSet set, string subjectKey, string predicateKey, string objectKey)
        {
            _n = 0;
            _set = set;
            _subjectKey = subjectKey;
            _predicateKey = predicateKey;
            _objectKey = objectKey;
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
