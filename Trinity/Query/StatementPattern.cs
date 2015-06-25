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

namespace Semiodesk.Trinity
{
    public class StatementPattern
    {
        #region Properties

        public Uri Subject { get; protected set; }

        private string _subjectName;

        internal string SubjectName
        {
            get { return _subjectName; }
            set { _subjectName = value; }
        }

        public Uri Predicate { get; protected set; }

        private string _predicateName;

        internal string PredicateName
        {
            get { return _predicateName; }
            set { _predicateName = value; }
        }

        public object Object { get; protected set; }

        private string _objectName;

        internal string ObjectName
        {
            get { return _objectName; }
            set { _objectName = value; }
        }

        public FilterOperation FilterOperation { get; protected set; }

        public SortDirection SortDirection { get; protected set; }

        public int SortPriority { get; protected set; }

        #endregion

        #region Constructors

        public StatementPattern(Uri s, Uri p, object o)
        {
            Subject = s;
            Predicate = p;
            Object = o;
        }

        #endregion

        #region Methods

        public StatementPattern Equal(object value)
        {
            FilterOperation = FilterOperation.Equal;
            Object = value;

            return this;
        }

        public StatementPattern NotEqual(object value)
        {
            FilterOperation = FilterOperation.NotEqual;
            Object = value;

            return this;
        }

        public StatementPattern GreaterThan(object value)
        {
            FilterOperation = FilterOperation.GreaterThan;
            Object = value;

            return this;
        }

        public StatementPattern GreaterOrEqual(object value)
        {
            FilterOperation = FilterOperation.GreaterOrEqual;
            Object = value;

            return this;
        }

        public StatementPattern LessThan(object value)
        {
            FilterOperation = FilterOperation.LessThan;
            Object = value;

            return this;
        }

        public StatementPattern LessOrEqual(object value)
        {
            FilterOperation = FilterOperation.LessOrEqual;
            Object = value;

            return this;
        }

        public StatementPattern Contains(string value, bool caseSensitive = false)
        {
            FilterOperation = FilterOperation.Contains;
            Object = value;

            return this;
        }

        public StatementPattern SortAscending(int priority = -1)
        {
            SortDirection = SortDirection.Ascending;
            SortPriority = priority;

            return this;
        }

        public StatementPattern SortDescending(int priority = -1)
        {
            SortDirection = SortDirection.Descending;
            SortPriority = priority;

            return this;
        }

        #endregion
    }
}
