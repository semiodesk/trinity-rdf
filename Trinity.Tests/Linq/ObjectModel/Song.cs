using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Tests.Linq
{

    /// <summary>
    ///  A music recording that is a single work of music.
    /// </summary>
    [RdfClass(MUSIC.Song)]
    internal class Song : Resource
    {

        #region Members

        /// <summary>
        /// The name of an entity.
        /// </summary>
        [RdfProperty(MUSIC.name)]
        public string Name { get; set; }

        /// <summary>
        /// A person or a group of people who participated in the creation of song as a composer or a lyricist.
        /// </summary>
        [RdfProperty(MUSIC.writer)]
        public List<Songwriter> Writers { get; set; }

        /// <summary>
        /// The length of a song in the album expressed in seconds.
        /// </summary>
        [RdfProperty(MUSIC.length)]
        public int Length { get; set; }

        #endregion

        #region Constructors

        public Song(Uri uri) : base(uri) { }

        #endregion
    }
}