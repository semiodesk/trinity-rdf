using System;

namespace Semiodesk.Trinity.Test.Linq
{
    [RdfClass(MUSIC.SoloArtist)]
    internal class SoloArtist : Person, IArtist
    {
        #region Members

        public string Name { get; set; }

        #endregion

        #region Constructors

        public SoloArtist(Uri uri) : base(uri) { }

        #endregion
    }
}