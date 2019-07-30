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
using System.IO;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This class extends the framework Uri class to also include fragments for
    /// equality testing.
    /// </summary>
    public class UriRef : Uri
    {
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
                return base.Equals(comparand) && Fragment.Equals((comparand as Uri).Fragment);
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
            return base.GetHashCode() & Fragment.GetHashCode();
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

    /// <summary>
    /// Collection of string extension related to Uris
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Create a UriRef from this string.
        /// </summary>
        /// <param name="uriString"></param>
        /// <returns></returns>
        public static UriRef ToUriRef(this string uriString)
        {
            return new UriRef(uriString);
        }

        /// <summary>
        /// Create a UriRef from this string with a given kind
        /// </summary>
        /// <returns></returns>
        public static UriRef ToUriRef(this string uriString, UriKind uriKind)
        {
            return new UriRef(uriString, uriKind);
        }
    }

    /// <summary>
    /// Extension of Uri class concering UriRef handling.
    /// </summary>
    public static class UriExtensions
    {
        /// <summary>
        /// Create a UriRef from this Uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static UriRef ToUriRef(this Uri uri)
        {
            return new UriRef(uri);
        }
    }

    /// <summary>
    /// Extension to FileSystemInfo concerting UriRef handling
    /// </summary>
    public static class FileSystemInfoExtensions
    {
        /// <summary>
        /// Create a UriRef from a FileSystemInfo
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static UriRef ToUriRef(this FileSystemInfo fileInfo)
        {
            return new UriRef(new Uri(fileInfo.FullName).AbsoluteUri);
        }
    }
}
