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
    /// This class extends the framework Uri class to also include fragments for
    /// equality testing.
    /// </summary>
    public class UriRef : Uri
    {
        #region Members

        /// <summary>
        /// Indicates if the UriRef is a triple store specific blank node identifier.
        /// </summary>
        public bool IsBlankId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an UriRef from an Uri
        /// </summary>
        /// <param name="uri"></param>
        public UriRef(Uri uri) : base(uri.OriginalString) { }

        /// <summary>
        /// Create an UriRef from a string.
        /// </summary>
        /// <param name="uriString"></param>
        public UriRef(string uriString) : base(uriString) { }

        /// <summary>
        /// Creates an UriRef from a string with a given UriKind.
        /// </summary>
        /// <param name="uriString"></param>
        /// <param name="uriKind"></param>
        public UriRef(string uriString, UriKind uriKind) : base(uriString, uriKind) { }

        /// <summary>
        /// Creates a UriRef instance for an existing blank node identifier.
        /// </summary>
        /// <param name="uriString">URI string or blank node identifier.</param>
        /// <param name="isBlankId">Indicate if the URI is a blank node identifier.</param>
        public UriRef(string uriString, bool isBlankId) : base(uriString, UriKind.RelativeOrAbsolute)
        {
            IsBlankId = isBlankId;
        }

        /// <summary>
        /// Creates an UriRef from a base uri and a relative uri as string.
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="relativeUri"></param>
        public UriRef(Uri baseUri, string relativeUri) : base(baseUri, relativeUri) { }

        #endregion

        #region Methods

        /// <summary>
        /// Tests the equality of two UriRefs.
        /// </summary>
        /// <param name="comparand"></param>
        /// <returns></returns>
        public override bool Equals(object comparand)
        {
            if (comparand is Uri)
            {
                return GetHashCode() == comparand.GetHashCode();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Override of GetHashCode which factors the fragment in. 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if(IsBlankId)
            {
                return OriginalString.GetHashCode();
            }
            else
            {
                return base.GetHashCode() & Fragment.GetHashCode();
            }
        }

        /// <summary>
        /// Generates a globally unique resource identifier in the Semiodesk namespace: &lt;urn:uuid:{GUID}/&gt;
        /// </summary>
        /// <returns>A Uniform Resource Identifier.</returns>
        public static Uri GetGuid(string format = "urn:uuid:{0}")
        {
            return new Uri(string.Format(format, Guid.NewGuid()));
        }

        #endregion
    }
}
