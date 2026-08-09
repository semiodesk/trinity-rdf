using System;

namespace Semiodesk.Trinity.Tests.Linq
{
    [RdfClass(MUSIC.Artist)]
    internal partial class Artist : Resource, IArtist
    {
        #region Members

        /// <summary>
        /// The name of an entity.
        /// </summary>
        [RdfProperty(MUSIC.name)]
        public partial string Name { get; set; }

        #endregion

        #region Constructors

        public Artist(Uri uri) : base(uri) { }

        #endregion
    }
}