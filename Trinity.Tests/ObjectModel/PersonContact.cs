using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests
{
    [RdfClass(NCO.PersonContact)]
    public partial class PersonContact : Contact
    {
        #region Members
        
        // These carried languageInvariant:true. The flag is gone: a string property sees untagged
        // literals only, which is exactly what it meant (ADR-0047).
        [RdfProperty(NCO.nameGiven)]
        public partial string NameGiven { get; set; }

        [RdfProperty(NCO.nameFamily)]
        public partial string NameFamily { get; set; }
        
        [RdfProperty(NCO.nameAdditional)]
        public partial List<string> NameAdditional { get; set; }

        #endregion
        
        #region Constructors
        
        public PersonContact(Uri uri) : base(uri) { }
        
        #endregion
    }
}