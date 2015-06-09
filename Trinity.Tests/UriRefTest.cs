using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Semiodesk.Trinity;


namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class UriRefTest
    {
        [Test]
        public void TestEquals()
        {
            UriRef u0 = new UriRef("http://semiodesk.com/ontologies/ppo");
            UriRef u1 = new UriRef("http://semiodesk.com/ontologies/ppo#UriScehma");

            Assert.AreNotEqual(u0, u1);
            Assert.IsFalse(u0 == u1);
            Assert.AreEqual(u0, u0);


            UriRef u2 = new UriRef("file://D:/Documents/x.doc");
            UriRef u3 = new UriRef("file://D:/Documents/2012/../x.doc");

            Assert.AreEqual(u2, u3);
            Assert.IsTrue(u2 == u3);

            UriRef u4 = new UriRef("file://D:/Documents/x.doc#Metadata");
            UriRef u5 = new UriRef("file://D:/Documents/2012/../x.doc#Metadata");

            Assert.AreEqual(u4, u5);
        }

        [Test]
        public void TestToString()
        {
            UriRef u0 = new UriRef("http://semiodesk.com/ontologies/ppo#UriScehma");
            string u1 = "http://semiodesk.com/ontologies/ppo#UriScehma";

            Assert.AreEqual(u1, u0.ToString());
        }

        [Test]
        public void TestToUriRef()
        {
            string uriString = "http://semiodesk.com/ontologies/ppo#UriScehma";
            UriRef u0 = uriString.ToUriRef();
            Assert.AreEqual(uriString, u0.OriginalString);
        }
    }
}
