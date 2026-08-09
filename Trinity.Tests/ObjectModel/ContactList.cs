using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.ContactList)]
    public partial class ContactList : Resource
    {
        #region Members

        [RdfProperty(NCO.containsContact)]
        public partial List<Contact> ContainsContact { get; set; }

        #endregion

        #region Constructors

        public ContactList(Uri uri) : base(uri) { }

        #endregion
    }
}
