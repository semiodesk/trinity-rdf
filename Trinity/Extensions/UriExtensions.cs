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
    /// Extension of Uri class concering UriRef handling.
    /// </summary>
    public static class UriExtensions
    {
        /// <summary>
        /// Create a UriRef from this Uri.
        /// </summary>
        /// <param name="uri">A uniform resource identifier (URI)</param>
        /// <returns>A UriRef instance.</returns>
        public static UriRef ToUriRef(this Uri uri)
        {
            return uri is UriRef ? uri as UriRef : new UriRef(uri);
        }

        /// <summary>
        /// Create a new URI from appending a given local name to this URI.
        /// </summary>
        /// <param name="uri">A uniform resource identifier (URI)</param>
        /// <param name="localName">The local name to append to the URI.</param>
        /// <returns>A new UriRef instance.</returns>
        public static UriRef GetUriRef(this Uri uri, string localName)
        {
            return new UriRef(uri.OriginalString + localName);
        }
        
        /// <summary>
        /// Indicates if the given URI denotes a blank node identifier.
        /// </summary>
        /// <remarks>
        /// Deliberately broader than the <see cref="UriRef.IsBlankId"/> property, and they answer
        /// different questions. The property says whether the identifier was <i>constructed</i> as a
        /// blank one, which only <c>UriRef(string, bool)</c> does — <c>new UriRef("_:0",
        /// UriKind.RelativeOrAbsolute)</c> and a plain <see cref="Uri"/> both report <c>false</c>
        /// while carrying a blank node label. This method asks the question a query builder actually
        /// needs: <i>is this a blank node label by any route</i>. Testing only the property let a
        /// consumer-constructed label reach a query, where it is a syntax error that takes every
        /// other subject in the same query down with it (ADR-0046).
        /// <para>
        /// The prefix test is <c>_:</c>, not <c>_</c>: an absolute IRI cannot begin with an
        /// underscore because a scheme must start with a letter, but a relative one can, and
        /// <c>_foo</c> is not a blank node label.
        /// </para>
        /// </remarks>
        /// <param name="uri">A uniform resource identifier (URI)</param>
        /// <returns><c>true</c> if the Uri denotes a blank node identifier.</returns>
        public static bool IsBlankId(this Uri uri)
        {
            if (uri == null)
            {
                return false;
            }

            return (uri is UriRef uriRef && uriRef.IsBlankId) || uri.IsBlankNodeLabel();
        }

        /// <summary>
        /// Indicates if the given URI is spelled as a blank node <i>label</i> (<c>_:x</c>).
        /// </summary>
        /// <remarks>
        /// This is the lexical question, and it is the one that decides how an identifier may be
        /// written into a query — which is <b>not</b> the same as <see cref="IsBlankId(Uri)"/>.
        /// A label cannot be expressed in SPARQL at all: bare it means a fresh variable matching
        /// everything, and bracketed it is an unresolvable relative IRI reference. But Virtuoso hands
        /// back its blank nodes as <c>nodeID://b10000</c> — flagged as blank ids, yet absolute IRIs
        /// that can be bracketed and queried perfectly well. Deciding serialization on the flag
        /// instead of the spelling emits those bare and drops them from subject bindings, which
        /// breaks blank-node round-trips on Virtuoso while the in-memory store, whose blank ids
        /// really are <c>_:</c> labels, shows nothing.
        /// <para>
        /// Rule of thumb: ask <see cref="IsBlankId(Uri)"/> when the question is "is this a blank
        /// node", and this when the question is "can I write this into a query".
        /// </para>
        /// </remarks>
        /// <param name="uri">A uniform resource identifier (URI)</param>
        /// <returns><c>true</c> if the Uri is spelled as a blank node label.</returns>
        public static bool IsBlankNodeLabel(this Uri uri)
        {
            return uri != null && uri.OriginalString.StartsWith("_:", StringComparison.Ordinal);
        }
    }
}
