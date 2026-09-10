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

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Objects of this class represent RDF properties.
    /// </summary>
    public class Property : Resource
    {
        #region Constructors
        /// <summary>
        /// Constructor taking a Uri parameter
        /// </summary>
        /// <param name="uri">Uri of the property</param>
        public Property(Uri uri) : base(uri) { }

        /// <summary>
        /// Constructor taking a UriRef parameter
        /// </summary>
        /// <param name="uri">Uri of the property</param>
        public Property(UriRef uri)  : base(uri)  { }

        #endregion

        #region Methods

        /// <summary>
        /// Determines wheter the URIs of the compared objects are equal.
        /// </summary>
        /// <param name="other">The object to be compared.</param>
        /// <returns><c>true</c> if the URIs of the compared objects are equal, <c>false</c> otherwise.</returns>
        public override bool Equals(object other)
        {
            if (other is IResource)
            {
                // The standard Uri.Equals method ignores the fragment identifier as
                // stated in RFC 3986. However, many properties in common RDF vocabularies
                // are implemented using a fragment identifier. Therefore we have to
                // compare the original URI string.
                return Uri.OriginalString.Equals((other as IResource).Uri.OriginalString);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the hash code of the objects URI.
        /// </summary>
        /// <returns>A hash code string.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
