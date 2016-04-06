using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    class SparqlSerializerTest
    {
        [TestCase]
        public void TestStringSerializeResource()
        {
            Resource r = new Resource("http://example.com/ex");
            r.AddProperty(Ontologies.dc.title, "MyResource");

            string res = SparqlSerializer.SerializeResource(r);
            string expected = "<http://example.com/ex> <http://purl.org/dc/elements/1.1/title> 'MyResource'. ";
            Assert.AreEqual(expected, res);


            PersonContact contact = new PersonContact(new Uri("http://example.com/ex"));
            contact.NameGiven = "Peter";
            res = SparqlSerializer.SerializeResource(contact);
            expected = "<http://example.com/ex> <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameGiven> 'Peter'; <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PersonContact>. ";
            Assert.AreEqual(expected, res);

            contact.Language = "DE";
            res = SparqlSerializer.SerializeResource(contact);
            Assert.AreEqual(expected, res);
        }

    }
}
