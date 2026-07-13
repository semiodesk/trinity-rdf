using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests.Linq
{

    [RdfClass(MUSIC.Band)]
    internal partial class Band : Artist
    {
        #region Members

        /// <summary>
        /// A member of a band. Does not distinguish between past vs current members
        /// </summary>
        [RdfProperty(MUSIC.member)]
        public partial List<SoloArtist> Members { get; set; }

        #endregion

        #region Constructors

        public Band(Uri uri) : base(uri) { }

        #endregion

    }
}
