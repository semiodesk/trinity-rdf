using System;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.EmailAddress)]
    public class EmailAddress : Resource
    {
        #region Members

        [RdfProperty(NCO.emailAddress)]
        public string Address { get; set; }

        #endregion

        #region Constructors

        public EmailAddress(Uri uri) : base(uri) { }

        #endregion
    }
}
