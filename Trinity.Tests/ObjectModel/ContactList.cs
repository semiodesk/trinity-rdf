using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.ContactList)]
    public class ContactList : Resource
    {
        #region Members

        [RdfProperty(NCO.containsContact)]
        public List<Contact> ContainsContact { get; set; }

        #endregion

        #region Constructors

        public ContactList(Uri uri) : base(uri) { }

        #endregion
    }
}
