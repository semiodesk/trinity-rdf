using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Semiodesk.Trinity;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class DotNetRDF_ResourceQueryTest
    {
        #region Members

        IModel _model;


        protected IStore _store;

        IResource _resource;

        #endregion

        #region Constructors

        #endregion

        #region Methods

        [SetUp]
        public void SetUp()
        {
          UriRef uri = new UriRef("http://localhost:8899/models/ResourceQueryTest");

          _store = Stores.CreateStore("provider=dotnetrdf");
            Uri modelUri = new Uri("http://example.org/TestModel");
            if (_store.ContainsModel(modelUri))
            _model = _store.GetModel(modelUri);
          else
            _model = _store.CreateModel(modelUri);
          


            if (_model == null)
            {
                throw new Exception(string.Format("Error: Unable to create model <{0}>.", uri));
            }

            if (_model.IsEmpty)
            {
                IResource q = null;

                for (int i = 1; i < 51; i++)
                {
                    IResource r = _model.CreateResource<Resource>();
                    r.AddProperty(nco.fullname, (char)(i % 26));
                    r.AddProperty(nco.gender, (i % 2 == 1) ? nco.female : nco.male);

                    if (i % 5 != 0)
                    {
                        r.AddProperty(rdf.type, nco.PersonContact);
                    }
                    else
                    {
                        r.AddProperty(rdf.type, nco.OrganizationContact);
                    }

                    if (i <= 30)
                    {
                        r.AddProperty(nco.birthDate, new DateTime(1989, 12, i));
                    }
                    else
                    {
                        r.AddProperty(nco.birthDate, new DateTime(1990, 1, i - 30));
                    }

                    if (q != null)
                    {
                        r.AddProperty(nie.relatedTo, q);
                    }
                    else
                    {
                        _resource = r;
                    }

                    r.Commit();
                    q = r;
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void TestConstructors()
        {
            ResourceQuery query;
            IEnumerable<Resource> result;

            query = new ResourceQuery();
            result = _model.GetResources(query);

            query = new ResourceQuery(nco.PersonContact);
            result = _model.GetResources(query);

            query = new ResourceQuery(_resource);
            result = _model.GetResources(query);
        }

        [Test]
        public void TestWhere()
        {
            ResourceQuery b = new ResourceQuery(nco.PersonContact);
            var filter = new DateTime(1990, 1, 1);
            b.Where(nco.birthDate).LessThan(filter);

            ResourceQuery a = new ResourceQuery(nco.PersonContact);
            a.Where(nco.gender);
            a.Where(nie.relatedTo, b);

            IResourceQueryResult result = _model.ExecuteQuery(b);

            List<Resource> resources = result.GetResources().ToList();
            Assert.AreEqual(18, resources.Count);

            foreach (Resource r in resources)
            {
                DateTime t = (DateTime)r.GetValue(nco.birthDate);
                Assert.IsTrue(t < filter);
            }

            resources = result.GetResources(0, 10).ToList();

            Assert.AreEqual(10, resources.Count);

            a = new ResourceQuery(nco.PersonContact);
            a.Where(nie.relatedTo, _resource);

            result = _model.ExecuteQuery(a);

            resources = result.GetResources().ToList();

            Assert.AreEqual(1, resources.Count);
        }

        [Test]
        public void TestSort()
        {
            ResourceQuery b = new ResourceQuery(nco.PersonContact);
            b.Where(nco.birthDate).LessThan(new DateTime(1990, 1, 1)).SortAscending();

            ResourceQuery a = new ResourceQuery(nco.PersonContact);
            a.Where(nco.gender);
            a.Where(nie.relatedTo, b);

            IResourceQueryResult result = _model.ExecuteQuery(b);

            List<Resource> resources = result.GetResources().ToList();
            Assert.AreEqual(18, resources.Count);

            DateTime? l = null;
            foreach (Resource r in resources)
            {
                DateTime t = (DateTime)r.GetValue(nco.birthDate);
                if (l.HasValue)
                    Assert.IsTrue(t > l);
                l = t;
            }


            a = new ResourceQuery(nco.PersonContact);
            a.Where(nco.gender);
            a.Where(nie.relatedTo, b);

            result = _model.ExecuteQuery(b, true);

            resources = result.GetResources().ToList();
            Assert.AreEqual(18, resources.Count);

            l = null;
            foreach (Resource r in resources)
            {
                DateTime t = (DateTime)r.GetValue(nco.birthDate);
                if (l.HasValue)
                    Assert.IsTrue(t > l);
                l = t;
            }
        }

        [Test]
        public void TestCount()
        {
            ResourceQuery query = new ResourceQuery(nco.PersonContact);
            IResourceQueryResult result = _model.ExecuteQuery(query);

            Assert.AreEqual(40, result.Count());

            query = new ResourceQuery(_resource);
            result = _model.ExecuteQuery(query);

            Assert.AreEqual(1, result.Count());
        }

        [Test]
        public void TestContains()
        {
            ResourceQuery a = new ResourceQuery(nco.PersonContact);
            a.Where(nco.fullname).Contains("0");

            IResourceQueryResult result = _model.ExecuteQuery(a);

            Assert.Greater(result.Count(), 0);
        }

        [Test]
        public void TestClone()
        {
            ResourceQuery b = new ResourceQuery(nco.PersonContact);
            b.Where(nco.birthDate).LessThan(new DateTime(1990, 1, 1)).SortAscending();

            ResourceQuery a = new ResourceQuery(nco.PersonContact);
            a.Where(nco.gender);
            a.Where(nie.relatedTo, b);

            ResourceQuery c = b.Clone();

            string q = SparqlSerializer.Serialize(_model, c);

            IResourceQueryResult result = _model.ExecuteQuery(c);

            int i = 0;

            foreach (Resource r in result.GetResources())
            {
                i++;
            }

            Assert.AreEqual(18, i);
        }

        #endregion
    }
}
