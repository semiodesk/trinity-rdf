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
    /// <remarks>
    /// <b>Prefer this over <see cref="Uri"/> for resource identity.</b> .NET's <see cref="Uri.Equals"/>
    /// ignores the fragment, so <c>http://example.org/x#a</c> and <c>http://example.org/x#b</c> compare
    /// equal — in RDF those are two different resources, which makes raw <see cref="Uri"/> unsafe as a
    /// key or in comparisons. This type compares fragments as well.
    ///
    /// It also carries whether the identifier is a blank node (<see cref="IsBlankId"/>), which a plain
    /// <see cref="Uri"/> cannot express.
    ///
    /// <b>Implementing <see cref="IEquatable{T}"/> is load-bearing, not decoration.</b> .NET 10 added
    /// <c>IEquatable&lt;Uri&gt;</c> to <see cref="Uri"/> itself, and <see cref="System.Collections.Generic.EqualityComparer{T}"/>.Default
    /// prefers that strongly-typed path over the <see cref="object"/> overload. Without the interface
    /// here, every <c>HashSet&lt;Uri&gt;</c>, <c>Dictionary&lt;Uri,…&gt;</c>, <c>Contains</c> and
    /// <c>Distinct</c> silently reverts to fragment-blind comparison on .NET 10 — including for code
    /// compiled against netstandard2.0, which is how this reaches consumers without a recompile.
    ///
    /// <b>One hazard survives and cannot be fixed here:</b> the <c>==</c> operators below bind
    /// <i>statically</i>, so they only apply when both operands are declared <c>UriRef</c>. Assign a
    /// <c>UriRef</c> to a variable of type <see cref="Uri"/> and <c>==</c> binds to
    /// <c>Uri.operator ==</c>, which is fragment-blind on .NET 10. A static operator cannot be
    /// overridden. This is why ADR-0025 makes "use <c>UriRef</c>" a rule rather than a preference.
    /// </remarks>
    public class UriRef : Uri, IEquatable<Uri>
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
        /// <remarks>
        /// <see cref="IsBlankId"/> is carried across, so copying a blank identifier does not quietly
        /// turn it into an ordinary relative URI. A blank identifier is also the only relative form
        /// accepted here -- see <see cref="KindOf"/>.
        /// </remarks>
        /// <param name="uri"></param>
        public UriRef(Uri uri) : base(uri.OriginalString, KindOf(uri))
        {
            IsBlankId = uri is UriRef uriref && uriref.IsBlankId;
        }

        /// <summary>
        /// The <see cref="UriKind"/> to construct <paramref name="uri"/> under.
        /// </summary>
        /// <remarks>
        /// <see cref="UriKind.Absolute"/> for anything but a blank node identifier, which keeps the
        /// original behaviour of this constructor: a relative URI fails here, at the call site, with
        /// <see cref="UriFormatException"/>. Accepting one would defer the failure to
        /// <see cref="Uri.Fragment"/> inside <see cref="Equals(Uri)"/> or <see cref="GetHashCode"/>,
        /// far from the code that produced it. A blank identifier such as <c>_:b0</c> is the one
        /// legitimate relative form, and it never reaches Fragment.
        /// </remarks>
        private static UriKind KindOf(Uri uri)
        {
            return uri is UriRef uriref && uriref.IsBlankId ? UriKind.RelativeOrAbsolute : UriKind.Absolute;
        }

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
            return comparand is Uri uri && Equals(uri);
        }

        /// <summary>
        /// Tests the equality of this identifier against another URI, taking the fragment into account.
        /// </summary>
        /// <remarks>
        /// This is the single implementation of equality for this type; <see cref="Equals(object)"/>
        /// and the <c>==</c> operators both delegate here. Implementing
        /// <see cref="IEquatable{T}"/> of <see cref="Uri"/> is what keeps generic collections on
        /// .NET 10 from bypassing it — see the remarks on the class.
        /// </remarks>
        /// <param name="other">The URI to compare against.</param>
        /// <returns><c>true</c> if both denote the same resource, <c>false</c> otherwise.</returns>
        public bool Equals(Uri other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            // A blank node identifier is an opaque, store-local label rather than a URI: it must
            // never be normalized or compared by fragment. Testing this before anything else also
            // keeps us away from Fragment, which throws on the relative URI a blank identifier is.
            bool otherIsBlankId = other is UriRef uriref && uriref.IsBlankId;

            if (IsBlankId || otherIsBlankId)
            {
                return IsBlankId && otherIsBlankId && string.Equals(OriginalString, other.OriginalString);
            }

            return base.Equals(other) && Fragment.Equals(other.Fragment);
        }

        /// <summary>
        /// Tests two identifiers for equality, taking the fragment into account.
        /// </summary>
        /// <remarks>
        /// Overriding <see cref="Equals(object)"/> is not enough: <c>==</c> binds statically, so
        /// without this declaration a comparison of two <see cref="UriRef"/> values would resolve to
        /// <c>Uri.operator ==</c> and ignore the fragment.
        /// </remarks>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if both denote the same resource, <c>false</c> otherwise.</returns>
        public static bool operator ==(UriRef left, UriRef right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return left is object && left.Equals(right);
        }

        /// <summary>
        /// Tests two identifiers for inequality, taking the fragment into account.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if the operands denote different resources, <c>false</c> otherwise.</returns>
        public static bool operator !=(UriRef left, UriRef right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Override of GetHashCode which factors the fragment in. 
        /// </summary>
        /// <remarks>
        /// The two hashes are mixed rather than AND-ed. AND drives bits toward zero — for the common
        /// case of a URI with no fragment it masks every hash against one constant — which collapses
        /// distinct identifiers into the same bucket and turns a hash lookup into a linear scan.
        ///
        /// Reading <see cref="Uri.Fragment"/> throws for a relative URI. That is deliberate and
        /// asserted by <c>UriRefTest.GetHashCodeTest</c>: a relative URI has no resource identity to
        /// hash, so failing loudly beats silently hashing a fragment that was never parsed.
        /// </remarks>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if(IsBlankId)
            {
                return OriginalString.GetHashCode();
            }
            else
            {
                return (base.GetHashCode() * 397) ^ Fragment.GetHashCode();
            }
        }

        /// <summary>
        /// Generates a globally unique resource identifier in the Semiodesk namespace: &lt;urn:uuid:{GUID}/&gt;
        /// </summary>
        /// <returns>A Uniform Resource Identifier.</returns>
        public static UriRef GetGuid(string format = "urn:uuid:{0}")
        {
            return new UriRef(string.Format(format, Guid.NewGuid()));
        }

        #endregion
    }
}
