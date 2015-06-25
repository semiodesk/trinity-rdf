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
    public class SortCriterion
    {
        #region Members

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                }
            }
        }

        private Property _property;

        public Property Property
        {
            get { return _property; }
            set
            {
                if(_property != value)
                {
                    _property = value;
                }
            }
        }

        private SortDirection _direction = SortDirection.Ascending;

        public SortDirection Direction
        {
            get { return _direction; }
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                }
            }

        }

        #endregion

        #region Constructors

        public SortCriterion()
        {
        }

        public SortCriterion(string name, Property property, SortDirection direction = SortDirection.Ascending)
        {
            Name = name;
            Property = property;
            Direction = direction;
        }

        #endregion
    }
}
