using System;

namespace Semiodesk.Trinity.Tests
{
    /// <summary>
    /// A mapped class whose RDF class and property IRIs carry percent-encoding that
    /// <see cref="Uri.ToString()"/> would unescape — <c>%20</c> becomes a space, which SPARQL
    /// forbids inside an <c>IRIREF</c>.
    /// </summary>
    /// <remarks>
    /// Exists so the query builders are exercised with an IRI whose display form differs from its
    /// <c>OriginalString</c>. Any builder that interpolates a <see cref="Uri"/> instead of routing
    /// through <c>SparqlSerializer.SerializeUri</c> turns into a parse error here rather than
    /// failing silently somewhere else. See ADR-0046.
    /// </remarks>
    [RdfClass("http://example.org/encoded/Contact%20Class")]
    public partial class EncodedUriContact : Resource
    {
        #region Members

        [RdfProperty("http://example.org/encoded/full%20name")]
        public partial string Fullname { get; set; }

        #endregion

        #region Constructors

        public EncodedUriContact(Uri uri) : base(uri) { }

        #endregion
    }
}
