using System;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.PostalAddress)]
    public partial class PostalAddress : Resource
    {
        #region Members

        [RdfProperty(NCO.country)]
        public partial string Country { get; set; }

        [RdfProperty(NCO.postalcode)]
        public partial string PostalCode { get; set; }

        [RdfProperty(NCO.locality)]
        public partial string City { get; set; }

        [RdfProperty(NCO.streetAddress)]
        public partial string StreetAddress { get; set; }

        #endregion

        #region Constructors

        public PostalAddress(Uri uri) : base(uri) { }

        #endregion
    }
}
