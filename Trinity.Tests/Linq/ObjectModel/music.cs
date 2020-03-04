using System;

namespace Semiodesk.Trinity.Test.Linq
{
    public class music : Ontology
    {
        public static readonly Uri Namespace = new Uri("http://www.example.org/music/");
        public static Uri GetNamespace() { return Namespace; }

        public static readonly string Prefix = "music";
        public static string GetPrefix() { return Prefix; }

        ///<summary>
        ///A person or a group of people creating and performing music.
        ///<see cref="http://www.example.org/music/Artist"/>
        ///</summary>
        public static readonly Class Artist = new Class(new Uri("http://www.example.org/music/Artist"));

        ///<summary>
        ///A music group; that is, is a group of people creating and performing music together.
        ///<see cref="http://www.example.org/music/Band"/>
        ///</summary>
        public static readonly Class Band = new Class(new Uri("http://www.example.org/music/Band"));

        ///<summary>
        ///A single person who is a musical artist.
        ///<see cref="http://www.example.org/music/SoloArtist"/>
        ///</summary>
        public static readonly Class SoloArtist = new Class(new Uri("http://www.example.org/music/SoloArtist"));

        ///<summary>
        ///A collection of songs released by an artist on physical or digital medium.
        ///<see cref="http://www.example.org/music/Album"/>
        ///</summary>
        public static readonly Class Album = new Class(new Uri("http://www.example.org/music/Album"));

        ///<summary>
        ///A music recording that is a single work of music.
        ///<see cref="http://www.example.org/music/Song"/>
        ///</summary>
        public static readonly Class Song = new Class(new Uri("http://www.example.org/music/Song"));

        ///<summary>
        ///A person or a group of people who participated in the creation of song as a composer or a lyricist.
        ///<see cref="http://www.example.org/music/Songwriter"/>
        ///</summary>
        public static readonly Class Songwriter = new Class(new Uri("http://www.example.org/music/Songwriter"));

        ///<summary>
        ///The name of an entity.
        ///<see cref="http://www.example.org/music/name"/>
        ///</summary>
        public static readonly Property name = new Property(new Uri("http://www.example.org/music/name"));

        ///<summary>
        ///A member of a band. Does not distinguish between past vs current members.
        ///<see cref="http://www.example.org/music/member"/>
        ///</summary>
        public static readonly Property member = new Property(new Uri("http://www.example.org/music/member"));

        ///<summary>
        ///The release date of an album.
        ///<see cref="http://www.example.org/music/date"/>
        ///</summary>
        public static readonly Property date = new Property(new Uri("http://www.example.org/music/date"));

        ///<summary>
        ///The artist that performed this album.
        ///<see cref="http://www.example.org/music/artist"/>
        ///</summary>
        public static readonly Property artist = new Property(new Uri("http://www.example.org/music/artist"));

        ///<summary>
        ///A song included in an album.
        ///<see cref="http://www.example.org/music/track"/>
        ///</summary>
        public static readonly Property track = new Property(new Uri("http://www.example.org/music/track"));

        ///<summary>
        ///A person or a group of people who participated in the creation of song as a composer or a lyricist.
        ///<see cref="http://www.example.org/music/writer"/>
        ///</summary>
        public static readonly Property writer = new Property(new Uri("http://www.example.org/music/writer"));

        ///<summary>
        ///The length of a song in the album expressed in seconds.
        ///<see cref="http://www.example.org/music/length"/>
        ///</summary>
        public static readonly Property length = new Property(new Uri("http://www.example.org/music/length"));
    }

    ///<summary>
    ///
    ///
    ///</summary>
    public static class MUSIC
    {
        public static readonly Uri Namespace = new Uri("http://www.example.org/music/");
        public static Uri GetNamespace() { return Namespace; }

        public static readonly string Prefix = "MUSIC";
        public static string GetPrefix() { return Prefix; }

        ///<summary>
        ///A person or a group of people creating and performing music.
        ///<see cref="http://www.example.org/music/Artist"/>
        ///</summary>
        public const string Artist = "http://www.example.org/music/Artist";

        ///<summary>
        ///A music group; that is, is a group of people creating and performing music together.
        ///<see cref="http://www.example.org/music/Band"/>
        ///</summary>
        public const string Band = "http://www.example.org/music/Band";

        ///<summary>
        ///A single person who is a musical artist.
        ///<see cref="http://www.example.org/music/SoloArtist"/>
        ///</summary>
        public const string SoloArtist = "http://www.example.org/music/SoloArtist";

        ///<summary>
        ///A collection of songs released by an artist on physical or digital medium.
        ///<see cref="http://www.example.org/music/Album"/>
        ///</summary>
        public const string Album = "http://www.example.org/music/Album";

        ///<summary>
        ///A music recording that is a single work of music.
        ///<see cref="http://www.example.org/music/Song"/>
        ///</summary>
        public const string Song = "http://www.example.org/music/Song";

        ///<summary>
        ///A person or a group of people who participated in the creation of song as a composer or a lyricist.
        ///<see cref="http://www.example.org/music/Songwriter"/>
        ///</summary>
        public const string Songwriter = "http://www.example.org/music/Songwriter";

        ///<summary>
        ///The name of an entity.
        ///<see cref="http://www.example.org/music/name"/>
        ///</summary>
        public const string name = "http://www.example.org/music/name";

        ///<summary>
        ///A member of a band. Does not distinguish between past vs current members.
        ///<see cref="http://www.example.org/music/member"/>
        ///</summary>
        public const string member = "http://www.example.org/music/member";

        ///<summary>
        ///The release date of an album.
        ///<see cref="http://www.example.org/music/date"/>
        ///</summary>
        public const string date = "http://www.example.org/music/date";

        ///<summary>
        ///The artist that performed this album.
        ///<see cref="http://www.example.org/music/artist"/>
        ///</summary>
        public const string artist = "http://www.example.org/music/artist";

        ///<summary>
        ///A song included in an album.
        ///<see cref="http://www.example.org/music/track"/>
        ///</summary>
        public const string track = "http://www.example.org/music/track";

        ///<summary>
        ///A person or a group of people who participated in the creation of song as a composer or a lyricist.
        ///<see cref="http://www.example.org/music/writer"/>
        ///</summary>
        public const string writer = "http://www.example.org/music/writer";

        ///<summary>
        ///The length of a song in the album expressed in seconds.
        ///<see cref="http://www.example.org/music/length"/>
        ///</summary>
        public const string length = "http://www.example.org/music/length";
    }
}