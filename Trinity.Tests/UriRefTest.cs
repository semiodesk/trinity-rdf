// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015-2019

using NUnit.Framework;
using System;

namespace Semiodesk.Trinity.Test
{
    [TestFixture]
    public class UriRefTest
    {
        [Test]
        public void EqualsTest()
        {
            UriRef u0 = new UriRef("http://semiodesk.com/ontologies/ppo");
            UriRef u1 = new UriRef("http://semiodesk.com/ontologies/ppo#UriScehma");

            Assert.IsFalse(ReferenceEquals(u0, u1));
            Assert.AreEqual(u0, u0);
            Assert.AreNotEqual(u0, u1);
            Assert.IsFalse(u0 == u1);

            UriRef u2 = new UriRef("file://D:/Documents/x.doc");
            UriRef u3 = new UriRef("file://D:/Documents/2012/../x.doc");

            Assert.IsFalse(ReferenceEquals(u2, u3));
            Assert.AreEqual(u2, u3);
            Assert.IsTrue(u2 == u3);

            UriRef u4 = new UriRef("file://D:/Documents/x.doc#Metadata");
            UriRef u5 = new UriRef("file://D:/Documents/2012/../x.doc#Metadata");

            Assert.IsFalse(ReferenceEquals(u4, u5));
            Assert.AreEqual(u4, u5);
            Assert.IsTrue(u4 == u5);

            UriRef u6 = new UriRef("_:b0", true);
            UriRef u7 = new UriRef("_:b0", true);
            UriRef u8 = new UriRef("_:b1", true);

            Assert.IsFalse(ReferenceEquals(u6, u7));
            Assert.AreEqual(u6, u7);
            Assert.IsTrue(u6.Equals(u7));
            Assert.IsTrue(u6 == u7);

            Assert.IsFalse(ReferenceEquals(u7, u8));
            Assert.AreNotEqual(u7, u8);
            Assert.IsFalse(u7.Equals(u8));
            Assert.IsFalse(u7 == u8);
        }

        [Test]
        public void ToStringTest()
        {
            UriRef u0 = new UriRef("http://semiodesk.com/ontologies/ppo#UriScehma");
            string u1 = "http://semiodesk.com/ontologies/ppo#UriScehma";

            Assert.AreEqual(u1, u0.ToString());
        }

        [Test]
        public void ToUriRefTest()
        {
            string uriString = "http://semiodesk.com/ontologies/ppo#UriScehma";
            UriRef u0 = uriString.ToUriRef();
            Assert.AreEqual(uriString, u0.OriginalString);
        }

        [Test]
        public void GetHashCodeTest()
        {
            var absoluteUri = new UriRef("https://trinity-rdf.net");
            var absoluteUriHash = absoluteUri.GetHashCode();

            var absoluteUriRef = new UriRef("https://trinity-rdf.net#download");
            var absoluteUriRefHash = absoluteUriRef.GetHashCode();

            Assert.AreNotEqual(absoluteUriHash, absoluteUriRefHash);

            var relativeUri = new UriRef("#download", UriKind.Relative);

            Assert.Throws<InvalidOperationException>(() => relativeUri.GetHashCode());

            var blankId = new UriRef("_:0", true);
            var blankIdHash = blankId.GetHashCode();
            
            Assert.AreNotEqual(absoluteUriHash, blankIdHash);
        }
    }
}
