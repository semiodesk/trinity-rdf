using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Test
{
    [RdfClass(NCO.OrganizationContact)]
    public class OrganizationContact : Contact
    {
        #region Members
        
        #endregion
        
        #region Constructors
        
        public OrganizationContact(Uri uri) : base(uri) { }
        
        #endregion
    }
}