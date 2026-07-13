using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests.Linq
{

    /// <summary>
    /// A collection of songs released by an artist on physical or digital medium.
    /// </summary>
    [RdfClass(MUSIC.Album)]
    internal partial class Album : Resource
    {

        #region Members

        /// <summary>
        /// The name of an entity.
        /// </summary>
        [RdfProperty(MUSIC.name)]
        public partial string Name { get; set; }

        /// <summary>
        /// The release date of an album.
        /// </summary>
        [RdfProperty(MUSIC.date)]
        public partial DateTime ReleaseDate { get; set; }

        /// <summary>
        /// The artist that performed this album.
        /// </summary>
        [RdfProperty(MUSIC.artist)]
        public partial Artist Artist { get; set; }

        /// <summary>
        /// A song included in an album.
        /// </summary>
        [RdfProperty(MUSIC.track)]
        public partial List<Song> Tracks { get; set; }

        #endregion

        #region Constructors

        public Album(Uri uri) : base(uri) { }

        #endregion
    }
}