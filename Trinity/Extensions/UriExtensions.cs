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
        /// Indicates whether the given URI can be named as a subject in a SPARQL query.
        /// </summary>
        /// <remarks>
        /// Named for the decision rather than for a property of the node, so a guard that asks the
        /// wrong question reads wrong at the call site. <c>IsBlankId</c> and
        /// <see cref="IsBlankNodeLabel(Uri)"/> both name facts, which is why a guard phrased as
        /// "can this be a subject" could call either and look reasonable.
        /// <para>
        /// <b>Trinity refuses every blank node here, on every store, and that is a deliberate
        /// contract rather than a limitation of the spelling.</b> It is tempting to allow the ones
        /// that look writable: Virtuoso hands its blank nodes back as <c>nodeID://b10000</c>, an
        /// absolute IRI that brackets perfectly well, and <c>ContainsResource</c> does find it —
        /// because it puts the identifier straight into a triple pattern, where Virtuoso resolves it
        /// back to the blank node. But <c>GetResource</c> cannot: it binds the subject, and a bound
        /// IRI term never matches a blank-node subject however it is spelled. Allowing them would
        /// therefore buy a capability that half works on one backend — <c>ContainsResource</c> says
        /// yes, <c>GetResource</c> says not found — and does not exist on the others. A uniform
        /// refusal is the honest contract, and it is what <c>GetResourceWithBlankIdTest</c> and its
        /// two neighbours have always asserted.
        /// </para>
        /// <para>
        /// The spelling still matters, just not here: <see cref="IsBlankNodeLabel(Uri)"/> decides how
        /// an identifier is <i>serialized</i> — a label bare, everything else bracketed — and getting
        /// that wrong emits Virtuoso's <c>nodeID://</c> identifiers unbracketed, which breaks writing
        /// them at all.
        /// </para>
        /// <para>
        /// <b>Which answer you get is decided by the static type of the receiver.</b> A <see cref="Uri"/>
        /// binds to these extensions; a <see cref="UriRef"/> also has the <see cref="UriRef.IsBlankId"/>
        /// property, which is the narrow semantic one. Neither spelling is invocable at the other's
        /// type, so there is no silent two-character slip — but changing a local's declared type from
        /// <c>Uri</c> to <c>UriRef</c>, or adding an <c>as UriRef</c> for an unrelated reason, flips
        /// which question is asked with no diagnostic.
        /// </para>
        /// </remarks>
        /// <param name="uri">A uniform resource identifier (URI)</param>
        /// <returns><c>true</c> if the Uri can appear as a subject in a SPARQL query.</returns>
        public static bool CanBeQuerySubject(this Uri uri)
        {
            return uri != null && !uri.IsBlankId();
        }

        /// <summary>
        /// Indicates if the given URI is spelled as a blank node <i>label</i> (<c>_:x</c>).
        /// </summary>
        /// <remarks>
        /// The lexical primitive behind <see cref="CanBeQuerySubject(Uri)"/>. Prefer that at a call
        /// site: it names the decision being made, where this names a property of the spelling and so
        /// reads at a guard as though it were interchangeable with <see cref="IsBlankId(Uri)"/>.
        /// Serialization uses this directly, because there the question really is the spelling — a
        /// label is emitted bare, everything else is bracketed.
        /// </remarks>
        /// <param name="uri">A uniform resource identifier (URI)</param>
        /// <returns><c>true</c> if the Uri is spelled as a blank node label.</returns>
        internal static bool IsBlankNodeLabel(this Uri uri)
        {
            return uri != null && uri.OriginalString.StartsWith("_:", StringComparison.Ordinal);
        }
    }
}
