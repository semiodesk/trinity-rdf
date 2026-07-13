using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.PersonContact)]
    public partial class PersonContact : Contact
    {
        #region Members
        
        [RdfProperty(NCO.nameGiven, true)]
        public partial string NameGiven { get; set; }

        [RdfProperty(NCO.nameFamily, true)]
        public partial string NameFamily { get; set; }
        
        [RdfProperty(NCO.nameAdditional, true)]
        public partial List<string> NameAdditional { get; set; }

        #endregion
        
        #region Constructors
        
        public PersonContact(Uri uri) : base(uri) { }
        
        #endregion
    }
}