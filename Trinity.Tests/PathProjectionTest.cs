#if !NET_3_5
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

using Semiodesk.Trinity;
using Semiodesk.Trinity.Ontologies;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class PathProjectionTest
    {
        private IModel _data = null;
        private IModel _projections = null;

        public PathProjectionTest()
        {
            ModelManager modelManager = ModelManager.Instance;
            modelManager.Connect();

            try
            {
                _data = modelManager.GetModel(new Uri("http://localhost:8899/Models/PathProjectionTest"));
                _data.Clear();
            }
            catch (Exception)
            {
                _data = modelManager.CreateModel(new Uri("http://localhost:8899/Models/PathProjectionTest"));
            }

            try
            {
                _projections = modelManager.GetModel(new Uri("http://localhost:8899/Models/PathProjection"));
                _projections.Clear();
            }
            catch (Exception)
            {
                _projections = modelManager.CreateModel(new Uri("http://localhost:8899/Models/PathProjection"));
            }
        }

        [SetUp]
        public void SetUp()
        {
            _projections.Read(new Uri("file:Models/test-ppo.rdf", UriKind.Relative), RdfSerializationFormat.RdfXml);

            IResource document = _data.CreateResource(new Uri("file:///xyz.doc"));
            document.AddProperty(rdf.type, nfo.Document);
            document.AddProperty(nie.title, "How To Recognise Different Types Of Trees From Quite A Long Way Away");
            document.AddProperty(dc.creator, "Python (Monty) Pictures");
            document.AddProperty(nie.created, new DateTime(1969, 9, 14));
            document.AddProperty(nie.language, "en");
            document.Commit();
        }

        [TearDown]
        public void TearDown()
        {
            _data.Clear();
        }

        [Test]
        public void TestGenerateName()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            IResource document = _data.GetResource(new Uri("file:///xyz.doc"));

            PropertyProjector pathProjector = PropertyProjector.Instance;
            string path = pathProjector.GeneratePath(document);
            string name = pathProjector.GenerateName(document);

            string result = path + name + ".doc";

            // stopwatch.Elapsed {00:00:00.1448302}
            // stopwatch.Elapsed {00:00:00.0463022}
            // stopwatch.Elapsed {00:00:00.0598417}
            // stopwatch.Elapsed {00:00:00.0236001}
            // stopwatch.Elapsed {00:00:00.0172570}
            stopwatch.Stop();
            stopwatch.Reset();
        }
    }
}
#endif