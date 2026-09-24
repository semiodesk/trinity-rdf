using System;

namespace Semiodesk.Trinity.Tests
{
    /// <summary>
    /// A generator-authored mapped class using the localized-text containers, in the shape ADR-0047
    /// recommends: <c>partial</c>, attribute-mapped, and <b>get-only</b>.
    /// </summary>
    /// <remarks>
    /// The hand-written counterpart is <c>LocalizedMappingTestClass</c>. Both routes have to stay
    /// first-class (ADR-0018), and the generator route is the one that was briefly impossible: emitting
    /// <c>get</c>+<c>set</c> unconditionally made a get-only mapped property CS9253, so a container
    /// could only be declared in the shape that TRIN009 now warns about. This class compiling at all is
    /// the regression test for that.
    /// </remarks>
    [RdfClass("semio:test:LocalizedDocument")]
    public partial class LocalizedDocument : Resource
    {
        public LocalizedDocument(Uri uri) : base(uri) { }

        /// <summary>One value per language.</summary>
        [RdfProperty("semio:test:documentTitle")]
        public partial LocalizedString Title { get; }

        /// <summary>Any number of values per language.</summary>
        [RdfProperty("semio:test:documentKeyword")]
        public partial LocalizedStringCollection Keywords { get; }

        /// <summary>Untagged by construction, beside the containers on the same class.</summary>
        [RdfProperty("semio:test:documentCode")]
        public partial string Code { get; set; }
    }
}
