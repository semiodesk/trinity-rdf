using System;

namespace Semiodesk.Trinity.Tests.Linq
{
    [RdfClass(MUSIC.Artist)]
    internal class Artist : Resource, IArtist
    {
        #region Members

        /// <summary>
        /// The name of an entity.
        /// </summary>
        [RdfProperty(MUSIC.name)]
        public string Name { get; set; }

        #endregion

        #region Constructors

        public Artist(Uri uri) : base(uri) { }

        #endregion
    }
}