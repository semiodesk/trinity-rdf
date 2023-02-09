using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Test
{
    [RdfClass(NCO.Contact)]
    public class Contact : Resource
    {
        #region Members

        [RdfProperty(NCO.fullname)]
        public string Fullname { get; set; }

        [RdfProperty(NCO.birthday)]
        public DateTime BirthDate { get; set; }

        [RdfProperty(NCO.hasEmailAddress)]
        public List<EmailAddress> EmailAddresses { get; set; }

        [RdfProperty(NCO.hasPostalAddress)]
        public List<PostalAddress> PostalAddresses { get; set; }

        #endregion

        #region Constructors

        public Contact(Uri uri) : base(uri) { }
        
        #endregion
    }
}