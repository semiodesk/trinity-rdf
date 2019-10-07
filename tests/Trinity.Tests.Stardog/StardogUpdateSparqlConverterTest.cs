using System;
using System.Linq;
using NUnit.Framework;
using Semiodesk.Trinity.Store.Stardog;
using VDS.RDF;

namespace Semiodesk.Trinity.Test.Stardog
{
    [TestFixture]
    public class StardogUpdateSparqlConverterTest
    {
        #region region Members

        private const string Namespace = "http://www.foo.com/";

        IStore Store;
        IModel Model;

        #endregion

        #region Methods

        [SetUp]
        public void SetUp()
        {
            Store = StoreFactory.CreateStore("provider=stardog;host=http://localhost:5820;uid=admin;pw=admin;sid=test");

            var testModel = new Uri($"{Namespace}");
            Model = Store.CreateModel(testModel);
            Model.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Store.Dispose();
            Store = null;
        }

        [Test, Category("UnitTest")]
        public void CanParseUpdate()
        {
            const string query = @"WITH <http://www.foo.com/> DELETE { <http://www.foo.com/bd1e4760-2b8b-48de-9eef-939b7242e8b4> ?p ?o . } INSERT { <http://www.foo.com/bd1e4760-2b8b-48de-9eef-939b7242e8b4> <http://www.foo.com/hasName> '2A4DC1FF0C40405996B5F882B5A127D9' ; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.foo.com/Country> . } WHERE { OPTIONAL { <http://www.foo.com/bd1e4760-2b8b-48de-9eef-939b7242e8b4> ?p ?o . } }";

            var parser = new StardogUpdateSparqlConverter();

            parser.ParseQuery(query);
            Assert.AreEqual("http://www.foo.com/", parser.GraphUri);
            Assert.AreEqual("http://www.foo.com/bd1e4760-2b8b-48de-9eef-939b7242e8b4", parser.PrimaryUri);

            Assert.AreEqual(2, parser.UpdateTriples.Count);
            Assert.AreEqual(2, parser.Additions.Count);
            var sNode = parser.Additions.First().Subject;
            Assert.IsNotNull(sNode);

            foreach (var parserAddition in parser.Additions)
            {
                Assert.AreEqual(sNode, parserAddition.Subject);
                Assert.IsFalse(parserAddition.Subject is LiteralNode);
                Assert.IsFalse(parserAddition.Predicate is LiteralNode);
            }
        }

        [Test, Category("UnitTest")]
        public void CanParsePureInsert()
        {
            const string query = @"WITH <http://www.foo.com/> INSERT { <http://www.foo.com/0ec76664-547f-4cf3-b65d-0f152e4bb0a6> <http://www.foo.com/hasReferenceId> '20623344' ; <http://www.foo.com/hasSourcedFrom> <http://www.foo.com/vendorNumberOne> ; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.foo.com/ExternalReference> . } WHERE { }";

            var parser = new StardogUpdateSparqlConverter();

            parser.ParseQuery(query);
            Assert.AreEqual("http://www.foo.com/", parser.GraphUri);
            Assert.AreEqual("http://www.foo.com/0ec76664-547f-4cf3-b65d-0f152e4bb0a6", parser.PrimaryUri);

            Assert.AreEqual(3, parser.UpdateTriples.Count);
            Assert.AreEqual(3, parser.Additions.Count);
            var sNode = parser.Additions.First().Subject;
            Assert.IsNotNull(sNode);

            foreach (var parserAddition in parser.Additions)
            {
                Assert.AreEqual(sNode, parserAddition.Subject);
                Assert.IsFalse(parserAddition.Subject is LiteralNode);
                Assert.IsFalse(parserAddition.Predicate is LiteralNode);
            }

            Assert.IsNull(parser.Removals);
        }

        [Test, Category("UnitTest")]
        public void CanParseLiteralWithEscapeCharacters()
        {
            const string query = @"WITH <http://www.foo.com/> INSERT { <http://www.foo.com/63e71ee4-0094-4196-88c6-3e827436f2ee> <http://www.foo.com/hasLocatedOnFloor> <http://www.foo.com/833ef375-82cf-4266-a0d3-980787e59bc7> ; <http://www.foo.com/hasLocatedInBuilding> <http://www.foo.com/d112ceeb-f879-4607-8e3d-07cb2a62a55d> ; <http://www.foo.com/hasLocatedInSite> <http://www.foo.com/5c4f2724-4d13-4e72-806d-91d3c29b0320> ; <http://www.foo.com/hasArea> '27.91580998197316' ^^<http://www.w3.org/2001/XMLSchema#decimal> ; <http://www.foo.com/hasInternalAddress> '' ; <http://www.foo.com/hasReferencedInSystem> <http://www.foo.com/55b7eed3-3245-40a5-8ef2-c336542d805b> ; <http://www.foo.com/hasName> 'Women\'s Bathroom' ; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.foo.com/Bathroom> . } WHERE { }";

            var parser = new StardogUpdateSparqlConverter();

            parser.ParseQuery(query);
            Assert.AreEqual("http://www.foo.com/", parser.GraphUri);
            Assert.AreEqual("http://www.foo.com/63e71ee4-0094-4196-88c6-3e827436f2ee", parser.PrimaryUri);

            Assert.AreEqual(8, parser.UpdateTriples.Count);
            Assert.AreEqual(8, parser.Additions.Count);
            var sNode = parser.Additions.First().Subject;
            Assert.IsNotNull(sNode);

            foreach (var parserAddition in parser.Additions)
            {
                Assert.AreEqual(sNode, parserAddition.Subject);
                Assert.IsFalse(parserAddition.Subject is LiteralNode);
                Assert.IsFalse(parserAddition.Predicate is LiteralNode);
            }

            Assert.IsNull(parser.Removals);
        }

        [Test, Category("UnitTest")]
        public void CanParseQueryIntoTripleSets()
        {
            const string query = @"WITH <http://www.foo.com/> DELETE { <http://www.foo.com/1f9847db-0b7d-448b-be6c-66f369a4b1dd> ?p ?o . } INSERT { <http://www.foo.com/1f9847db-0b7d-448b-be6c-66f369a4b1dd> <http://www.foo.com/hasLocatedInCity> <http://www.foo.com/918da383-e72a-47ec-99fc-63a4ddb683cf> ; <http://www.foo.com/hasPostalCode> 'Something' ; <http://www.foo.com/hasReferencedInSystem> <http://www.foo.com/deee39e2-731d-4166-b38a-0b4a37df868a> ; <http://www.foo.com/hasName> 'New Name' ; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.foo.com/Site> . } WHERE { OPTIONAL { <http://www.foo.com/1f9847db-0b7d-448b-be6c-66f369a4b1dd> ?p ?o . } }";

            var parser = new StardogUpdateSparqlConverter();

            parser.ParseQuery(query);
            Assert.AreEqual("http://www.foo.com/", parser.GraphUri);
            Assert.AreEqual("http://www.foo.com/1f9847db-0b7d-448b-be6c-66f369a4b1dd", parser.PrimaryUri);

            Assert.AreEqual(5, parser.UpdateTriples.Count);
            Assert.AreEqual(5, parser.Additions.Count);
            var sNode = parser.Additions.First().Subject;
            Assert.IsNotNull(sNode);

            foreach (var parserAddition in parser.Additions)
            {
                Assert.AreEqual(sNode, parserAddition.Subject);
                Assert.IsFalse(parserAddition.Subject is LiteralNode);
                Assert.IsFalse(parserAddition.Predicate is LiteralNode);
            }

            Assert.IsNull(parser.Removals);
        }

        [Test, Category("UnitTest")]
        public void CanParseDecimalPrimitive()
        {
            const string query = @"WITH <http://www.foo.com/> DELETE { <http://www.foo.com/8843ce65-ac95-471b-9d12-ea270d04445b> ?p ?o . } INSERT { <http://www.foo.com/8843ce65-ac95-471b-9d12-ea270d04445b> <http://www.foo.com/hasMainFloor> 'true' ^^<http://www.w3.org/2001/XMLSchema#boolean> ; <http://www.foo.com/hasShortName> '1' ; <http://www.foo.com/hasTypeOfFloor> 'indoor' ; <http://www.foo.com/hasLocatedInBuilding> <http://www.foo.com/e1be9b7e-5c4b-4631-b7ce-459005a025b5> ; <http://www.foo.com/hasLocatedInSite> <http://www.foo.com/086ab86c-7d45-4b81-a160-7be39b5955c2> ; <http://www.foo.com/hasArea> '7240.969097518792' ^^<http://www.w3.org/2001/XMLSchema#decimal> ; <http://www.foo.com/hasReferencedInSystem> <http://www.foo.com/c1fdc91e-fd6d-48f4-b36b-23c9b4a24e3d> ; <http://www.foo.com/hasName> 'Floor 1' ; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.foo.com/Floor> . } WHERE { OPTIONAL { <http://www.foo.com/8843ce65-ac95-471b-9d12-ea270d04445b> ?p ?o . } }";

            var parser = new StardogUpdateSparqlConverter();

            parser.ParseQuery(query);
            Assert.AreEqual("http://www.foo.com/", parser.GraphUri);
            Assert.AreEqual("http://www.foo.com/8843ce65-ac95-471b-9d12-ea270d04445b", parser.PrimaryUri);

            Assert.AreEqual(9, parser.UpdateTriples.Count);
            Assert.AreEqual(9, parser.Additions.Count);
            var sNode = parser.Additions.First().Subject;
            Assert.IsNotNull(sNode);

            foreach (var parserAddition in parser.Additions)
            {
                Assert.AreEqual(sNode, parserAddition.Subject);
                Assert.IsFalse(parserAddition.Subject is LiteralNode);
                Assert.IsFalse(parserAddition.Predicate is LiteralNode);
            }

            Assert.IsNull(parser.Removals);
        }

        [Test, Category("UnitTest")]
        public void CanParsePropertyWithListPropertyInsert()
        {
            var query = @"WITH <http://www.foo.com/> INSERT { <http://www.foo.com/1e0499d7-c07c-46db-812c-6da89a262f83> <http://www.foo.com/hasDefaultSubType> 'Medium' ; <http://www.foo.com/hasName> 'Room' ; <http://www.foo.com/hasOntologyTypeNameList> '''Namespace.Sub.Model.Auditorium
Namespace.Sub.Model.ChatRoom
Namespace.Sub.Model.CollaborativeArea
Namespace.Sub.Model.ConferenceRoom
Namespace.Sub.Model.HuddleRoom
Namespace.Sub.Model.MeetingRoom
Namespace.Sub.Model.TrainingRoom
Namespace.Sub.Model.VideoConferenceRoom''' ; <http://www.foo.com/hasRegisteredApplication> <http://www.foo.com/applicationNumberOne> ; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.foo.com/CustomTypeMaster> . } WHERE { }
";

            var parser = new StardogUpdateSparqlConverter();

            parser.ParseQuery(query);
            Assert.AreEqual("http://www.foo.com/", parser.GraphUri);
            Assert.AreEqual("http://www.foo.com/1e0499d7-c07c-46db-812c-6da89a262f83", parser.PrimaryUri);

            Assert.AreEqual(5, parser.UpdateTriples.Count);
            Assert.AreEqual(5, parser.Additions.Count);
            var sNode = parser.Additions.First().Subject;
            Assert.IsNotNull(sNode);

            foreach (var parserAddition in parser.Additions)
            {
                Assert.AreEqual(sNode, parserAddition.Subject);
                Assert.IsFalse(parserAddition.Subject is LiteralNode);
                Assert.IsFalse(parserAddition.Predicate is LiteralNode);
            }

            Assert.IsNull(parser.Removals);
        }

        [Test, Category("IntegrationTest")]
        public void CanParseDelete()
        {
            // Commit any entity to the RDF
            var resourceUri = new Uri($"{Namespace}myResource");
            var r = Model.CreateResource(resourceUri);
            r.AddProperty(new Property(new Uri($"{Namespace}test:property")), "var");
            r.Commit();

            // Simulate the delete SPARQL
            var query = $"DELETE WHERE {{ GRAPH <{Namespace}> {{ ?s ?p <{resourceUri}> . }} }}";

            // Get a connected parser instance
            var parser = new StardogUpdateSparqlConverter(Store as StardogStore);

            // Invoke the parser
            parser.ParseQuery(query);

            // Confirm the appropriate Removal and Updates
            Assert.AreEqual(Namespace, parser.GraphUri);
            Assert.AreEqual(resourceUri.ToString(), parser.PrimaryUri);
            Assert.AreEqual(0, parser.UpdateTriples.Count);
            Assert.AreEqual(0, parser.Additions.Count);
            Assert.AreEqual(1, parser.Removals.Count);
        }

        #endregion
    }
}
