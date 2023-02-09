using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Test
{
    [RdfClass(NCO.PersonContact)]
    public class PersonContact : Contact
    {
        #region Members
        
        [RdfProperty(NCO.nameGiven, true)]
        public string NameGiven { get; set; }

        [RdfProperty(NCO.nameFamily, true)]
        public string NameFamily { get; set; }
        
        [RdfProperty(NCO.nameAdditional, true)]
        public List<string> NameAdditional { get; set; }

        #endregion
        
        #region Constructors
        
        public PersonContact(Uri uri) : base(uri) { }
        
        #endregion
    }
}