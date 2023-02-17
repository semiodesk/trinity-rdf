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
// Copyright (c) Semiodesk GmbH 2023

using NUnit.Framework;
using Semiodesk.Trinity.Ontologies;
using System.Linq;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class ModelGroupTest<T> : StoreTest<T> where T : IStoreTestSetup
    {
        #region Members
        
        protected UriRef R1;
        
        #endregion
        
        #region Methods
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            R1 = BaseUri.GetUriRef("r1");
        }
        
        [Test]
        public virtual void ContainsResourceTest()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);
            
            var u1 = BaseUri.GetUriRef("r1");

            Assert.IsFalse(group.ContainsResource(u1));

            var r1 = Model1.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();
            
            Assert.IsTrue(group.ContainsResource(u1));

            Model1.DeleteResource(r1);

            Assert.IsFalse(group.ContainsResource(u1));

            r1 = Model2.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            Assert.IsTrue(group.ContainsResource(u1));
        }
        
        [Test]
        public virtual void DeleteResourceTest()
        {
            var r1 = Model1.CreateResource(R1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            Assert.IsTrue(Model1.ContainsResource(R1));

            Model1.DeleteResource(r1);

            Assert.IsFalse(Model1.ContainsResource(R1));
        }
        
        [Test]
        public virtual void GetResourceTest()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);
            
            var u1 = BaseUri.GetUriRef("r1");
            
            Assert.Throws<ResourceNotFoundException>(() => group.GetResource(u1));
            
            var r1 = Model1.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            var gr1 = group.GetResource(u1);
            
            Assert.IsNotNull(gr1);
            Assert.IsTrue(gr1.IsReadOnly);
            Assert.AreEqual(u1, gr1.Uri);
            Assert.Contains(nco.Contact, gr1.ListValues(rdf.type).ToList());

            r1 = Model2.CreateResource(u1);
            r1.AddProperty(rdf.type, nco.Contact);
            r1.Commit();

            gr1 = group.GetResource(u1);
            
            Assert.IsNotNull(gr1);
            Assert.IsTrue(gr1.IsReadOnly);
            Assert.AreEqual(u1, gr1.Uri);
            Assert.AreEqual(1, gr1.ListValues(rdf.type).Count());
            Assert.Contains(nco.Contact, gr1.ListValues(rdf.type).ToList());
        }

        [Test]
        public virtual void LazyLoadResourceTest()
        {
            var group = Store.CreateModelGroup(Model1.Uri, Model2.Uri);
            
            var u1 = BaseUri.GetUriRef("r1");
            var u2 = BaseUri.GetUriRef("r2");

            var r2 = Model1.CreateResource<MappingTestClass2>(u2);
            
            var r1 = Model1.CreateResource<MappingTestClass>(u1);
            r1.uniqueResourceTest = r2; // TODO: Debug message, because t2 was not committed
            r1.Commit();

            var gr1 = group.GetResource<MappingTestClass>(u1);
            var gr2 = gr1.ListValues(to.uniqueResourceTest).OfType<Resource>().First();
            
            Assert.AreEqual(r2.Uri.OriginalString, gr2.Uri.OriginalString);

            Model1.DeleteResource(r1);
            Model1.DeleteResource(r2);

            r2 = Model1.CreateResource<MappingTestClass2>(u2);
            r2.Commit();
            
            r1 = Model1.CreateResource<MappingTestClass>(u1);
            r1.uniqueResourceTest = r2;
            r1.Commit();

            gr1 = group.GetResource<MappingTestClass>(u1);
            
            Assert.AreEqual(r2, gr1.uniqueResourceTest);
            
            var x1 = group.GetResource(u1);
            
            Assert.AreEqual(typeof(MappingTestClass), x1.GetType());
        }
        
        #endregion
    }
}
