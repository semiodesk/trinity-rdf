using System;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.PostalAddress)]
    public class PostalAddress : Resource
    {
        #region Members

        [RdfProperty(NCO.country)]
        public string Country { get; set; }

        [RdfProperty(NCO.postalcode)]
        public string PostalCode { get; set; }

        [RdfProperty(NCO.locality)]
        public string City { get; set; }

        [RdfProperty(NCO.streetAddress)]
        public string StreetAddress { get; set; }

        #endregion

        #region Constructors

        public PostalAddress(Uri uri) : base(uri) { }

        #endregion
    }
}
