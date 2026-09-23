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
// Copyright (c) Semiodesk GmbH 2026


using System;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// The one guard for "may this identifier be named as a subject in a query", shared by every
    /// <see cref="IModel"/>.
    /// </summary>
    /// <remarks>
    /// It is shared for the same reason <see cref="BulkResourceReader"/> is: the contract was written
    /// down as holding on every model, and it did not. <see cref="Model"/> and <c>LayeredModel</c>
    /// carried a copy each and <c>ModelGroup</c> carried none, so a blank node reached its
    /// <c>ContainsResource</c> — which interpolates the identifier into a triple pattern, where a bare
    /// <c>_:b0</c> is not a reference to anything but a fresh existential variable. It matched any
    /// subject with any property and answered <c>true</c> for any non-empty group: a silently wrong
    /// answer, which is worse than the half-working capability the contract exists to refuse.
    /// <para>
    /// Why every blank node and not just the unwritable ones is recorded on
    /// <see cref="UriExtensions.CanBeQuerySubject"/>.
    /// </para>
    /// </remarks>
    internal static class QuerySubject
    {
        /// <summary>
        /// Rejects an identifier that may not be named as a subject in a SPARQL query.
        /// </summary>
        /// <param name="uri">The identifier a caller supplied.</param>
        /// <param name="paramName">The name of the parameter it arrived in.</param>
        internal static void Require(Uri uri, string paramName = "uri")
        {
            // Null first: a caller who passed nothing should not be told their URI is a blank node.
            if (uri == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (!uri.CanBeQuerySubject())
            {
                throw new ArgumentException("Blank nodes are not supported as query subjects in SPARQL 1.1");
            }
        }
    }
}
