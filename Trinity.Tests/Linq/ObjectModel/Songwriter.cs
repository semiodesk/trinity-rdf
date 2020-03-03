using System;

namespace Semiodesk.Trinity.Test.Linq
{
    [RdfClass(MUSIC.Songwriter)]
    internal class Songwriter : Person
    {
        #region Members

        #endregion

        #region Constructors

        public Songwriter(Uri uri) : base(uri) { }

        #endregion
    }
}