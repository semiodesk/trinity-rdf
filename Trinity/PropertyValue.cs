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
using Semiodesk.Trinity;

namespace Semiodesk.Trinity
{
    public class PropertyValue : IEquatable<PropertyValue>
    {
        #region Members

        public Property Property;

        public object Value;

        internal FilterOperation FilterOperation;

        internal object FilterValue;

        internal SortDirection SortDirection;

        internal int SortOrder = -1;

        #endregion

        #region Constructors

        public PropertyValue(Property property, object value)
        {
            Property = property;
            Value = value;
        }

        internal PropertyValue(Property property, object value, SortDirection sortDirection, int sortOrder)
        {
            Property = property;
            Value = value;
            SortDirection = sortDirection;
            SortOrder = sortOrder;
        }

        internal PropertyValue(Property property, FilterOperation filter, object filterValue, SortDirection sortDirection, int sortOrder)
        {
            Property = property;
            FilterValue = filterValue;
            FilterOperation = filter;
            SortDirection = sortDirection;
            SortOrder = sortOrder;
        }

        #endregion

        #region Methods

        public bool Equals(PropertyValue other)
        {
            if (other != null)
            {
                return Property.Equals(other.Property) && Value.Equals(other.Value);
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}
