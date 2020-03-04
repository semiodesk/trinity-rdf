using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Semiodesk.Trinity.Test.Linq
{
    [TestFixture]
    public class ResourceWithMultipleTypesTest
    {
        IStore Store;
        IModel Model;

        Songwriter sw;

        [SetUp]
        public void SetUp()
        {
            // DotNetRdf memory store.
            string connectionString = "provider=dotnetrdf";

            // Stardog store.
            //string connectionString = "provider=stardog;host=http://localhost:5820;uid=admin;pw=admin;sid=test";

            // OpenLink Virtoso store.
            //string connectionString = string.Format("{0};rule=urn:semiodesk/test/ruleset", SetupClass.ConnectionString);


            Store = StoreFactory.CreateStore(connectionString);
            Store.InitializeFromConfiguration();
            Store.Log = (l) => Debug.WriteLine(l);

            Model = Store.CreateModel(ex.Namespace);
            Model.Clear();

            var typeProperty = new Property(new Uri("rdf:type"));

            var p1 = Model.CreateResource<Person>(ex.JohnLennon);
            p1.AddProperty(typeProperty, new Uri("http://www.example.org/music/Songwriter"));
            p1.AddProperty(typeProperty, new Uri("http://www.example.org/music/SoloArtist"));
            p1.Commit();

            Songwriter sw1 = Model.CreateResource<Songwriter>(ex.PaulMcCartney);
            sw1.FirstName = "Paul";
            sw1.LastName = "McCartney";
            sw1.Age = 77;
            sw1.Birthday = new DateTime(1942, 06, 18);
            sw1.AccountBalance = 10000.1f;
            sw1.Commit();

            Band b1 = Model.CreateResource<Band>(ex.TheBeatles);
            b1.Name = "The Beatles";
            b1.Members.Add(new SoloArtist(p1.Uri));
            b1.Commit();

            Song s1 = Model.CreateResource<Song>(ex.BackInTheUSSR);
            sw = new Songwriter(p1.Uri);
            s1.Writers.Add(sw);
            s1.Writers.Add(sw1);
            s1.Length = 163;
            s1.Commit();

            Album al1 = Model.CreateResource<Album>(ex.TheBeatlesAlbum);
            al1.Name = "The Beatles (Album)";
            al1.ReleaseDate = new DateTime(1968, 11, 22);
            al1.Artist = b1;
            al1.Tracks.Add(s1);
            al1.Commit();
        }

        [TearDown]
        public void TearDown()
        {
            Store.Dispose();
            Store = null;
        }

        [Test]
        public void CanSelectResourcesWithMultipleTypes()
        {
            var actual = Model.AsQueryable<Band>().ToList();
            Assert.AreEqual(1, actual.Count, "bands found");

            Band b = actual[0];

            Assert.AreEqual(ex.TheBeatles, b.Uri, "The Beatles are the band");
            Assert.AreEqual(1, b.Members.Count, "band member count");
            Assert.AreEqual(ex.JohnLennon, b.Members[0], "john lennon is the band member");

            var albums = (from album in Model.AsQueryable<Album>() where album.Artist.Uri == ex.TheBeatles select album).ToList();
            Assert.AreEqual(1, albums.Count, "album count");

            Album a = albums[0];

            Assert.AreEqual(ex.TheBeatlesAlbum, a.Uri, "The Beatles Album is the album");
            Assert.AreEqual(1, a.Tracks.Count, "Album track count");

            Song s = a.Tracks[0];
            Assert.AreEqual(ex.BackInTheUSSR, s.Uri, "'Back in the USSR' is the song");

            Assert.AreEqual(2, s.Writers.Count, "Song writers count");
            Assert.IsTrue(s.Writers.Contains(sw), "Song writers' collection contains Lennon");
        }
    }
}
