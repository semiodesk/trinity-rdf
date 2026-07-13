using System;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.EmailAddress)]
    public partial class EmailAddress : Resource
    {
        #region Members

        [RdfProperty(NCO.emailAddress)]
        public partial string Address { get; set; }

        #endregion

        #region Constructors

        public EmailAddress(Uri uri) : base(uri) { }

        #endregion
    }
}
