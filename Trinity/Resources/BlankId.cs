using System;
using System.Collections.Generic;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A blank node identifier.
    /// </summary>
    public class BlankId : UriRef
    {
        #region Constructors

        /// <summary>
        /// Create a new and uninitialized BlankId instance.
        /// </summary>
        public BlankId() : base("_:null", true) { }

        /// <summary>
        /// Create a BlankId instance from an existing blank id.
        /// </summary>
        public BlankId(string blankId) : base(blankId, true) { }

        #endregion
    }
}
