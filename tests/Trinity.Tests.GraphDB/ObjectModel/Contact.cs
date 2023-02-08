using System;

namespace Semiodesk.Trinity.Test.GraphDB
{
    [RdfClass(NCO.Contact)]
    public class Contact : Resource
    {
        #region Members

        [RdfProperty(NCO.fullname)]
        public string Fullname { get; set; }

        [RdfProperty(NCO.birthday)]
        public DateTime Birthday { get; set; }

        #endregion
        
        #region Constructors
        
        public Contact(Uri uri) : base(uri) { }
        
        #endregion
    }
}