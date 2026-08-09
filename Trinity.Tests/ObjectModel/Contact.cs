using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.Contact)]
    public partial class Contact : Resource
    {
        #region Members

        [RdfProperty(NCO.fullname)]
        public partial string Fullname { get; set; }

        [RdfProperty(NCO.birthday)]
        public partial DateTime BirthDate { get; set; }

        [RdfProperty(NCO.hasEmailAddress)]
        public partial List<EmailAddress> EmailAddresses { get; set; }

        [RdfProperty(NCO.hasPostalAddress)]
        public partial List<PostalAddress> PostalAddresses { get; set; }

        #endregion

        #region Constructors

        public Contact(Uri uri) : base(uri) { }
        
        #endregion
    }
}