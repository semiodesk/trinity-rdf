using NUnit.Framework;
using Semiodesk.Trinity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace dotNetRDFStore.Test
{
    [TestFixture]
    class StoreHandleTest
    {

        [Test]
        public void TestOpenClose()
        {
            var p = new Property(new Uri("ex:myProperty"));

            var Store = Stores.CreateStore("provider=dotnetrdf");
            IModel m = Store.CreateModel(new UriRef("semio:test"));
            m.Clear();
            var x = m.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            Store.Dispose();

            Store = Stores.CreateStore("provider=dotnetrdf");
            m = Store.CreateModel(new UriRef("semio:test"));
            m.Clear();
            x = m.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            Store.Dispose();

            Store = Stores.CreateStore("provider=dotnetrdf");
            m = Store.CreateModel(new UriRef("semio:test"));
            m.Clear();
            x = m.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            Store.Dispose();

            Store = Stores.CreateStore("provider=dotnetrdf");
            m = Store.CreateModel(new UriRef("semio:test"));
            m.Clear();
            x = m.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            Store.Dispose();

            Store = Stores.CreateStore("provider=dotnetrdf");
            m = Store.CreateModel(new UriRef("semio:test"));
            m.Clear();
            x = m.CreateResource<Resource>();
            x.AddProperty(p, "uarg");
            x.Commit();
            Store.Dispose();
        }
    }
}
