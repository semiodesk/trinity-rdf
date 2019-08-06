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
    /// This exception will be thrown if the store rejects the query as invalid.
    /// </summary>
    public class InvalidQueryException : ArgumentException
    {
        #region Members
        /// <summary>
        /// Contains the offending query 
        /// </summary>
        public string Query { get; private set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Create a new exception without information.
        /// </summary>
        public InvalidQueryException()
        {
        }

        /// <summary>
        /// Create a new exception with an error string.
        /// </summary>
        /// <param name="message">Details about the issue.</param>
        public InvalidQueryException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Create a new exception with an error string and an inner exception.
        /// </summary>
        /// <param name="message">Details about the issue.</param>
        /// <param name="innerException">The exception that propmted the query failure.</param>
        /// <param name="query">The offending query</param>
        public InvalidQueryException(string message, Exception innerException, string query)
            : base(message, innerException)
        {
            Query = query;
        }

        #endregion
    }
}
