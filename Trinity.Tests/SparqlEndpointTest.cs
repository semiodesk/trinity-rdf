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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests
{
    [TestFixture]
    public class SparqlEndpointTest
    {
        #region Members

        private const string SelectResult = "{\"head\":{\"vars\":[\"s\"]},\"results\":{\"bindings\":[{\"s\":{\"type\":\"uri\",\"value\":\"http://example.org/a\"}}]}}";

        private const string AskResult = "{\"head\":{},\"boolean\":true}";

        private const string ConstructResult = "<http://example.org/a> <http://example.org/p> \"v\" .";

        private static readonly Uri ModelUri = new Uri("http://example.org/model");

        private HttpListener _listener;

        private Thread _server;

        private Uri _baseUri;

        /// <summary>
        /// The <c>Authorization</c> header of every request the stub received, in order; null where absent.
        /// </summary>
        private readonly List<string> _authorizations = new List<string>();

        #endregion

        #region Setup

        /// <summary>
        /// Starts a local stub endpoint. The store's only other tests point at a public endpoint that no
        /// longer exists, so without this nothing exercises the store's HTTP path at all.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // NUnit reuses one fixture instance for every test in it.
            _authorizations.Clear();

            // HttpListener cannot bind port 0, so borrow a free port from the OS first.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            _baseUri = new Uri($"http://127.0.0.1:{port}/");
            _listener = new HttpListener();
            _listener.Prefixes.Add(_baseUri.AbsoluteUri);
            _listener.Start();

            _server = new Thread(Serve) { IsBackground = true };
            _server.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _listener.Close();
        }

        /// <summary>
        /// Answers any query under <c>/sparql</c>; under <c>/secured</c> it first challenges for Basic
        /// credentials, as a protected endpoint does.
        /// </summary>
        private void Serve()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = _listener.GetContext();
                }
                catch (Exception)
                {
                    return;
                }

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string authorization = request.Headers["Authorization"];

                lock (_authorizations)
                {
                    _authorizations.Add(authorization);
                }

                if (request.Url.AbsolutePath == "/secured" && authorization == null)
                {
                    response.StatusCode = 401;
                    response.AddHeader("WWW-Authenticate", "Basic realm=\"test\"");
                    response.Close();

                    continue;
                }

                string query = request.QueryString["query"];

                if (query == null)
                {
                    using (var reader = new StreamReader(request.InputStream))
                    {
                        query = reader.ReadToEnd();
                    }
                }

                string body;

                if (query.IndexOf("CONSTRUCT", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    response.ContentType = "text/turtle";
                    body = ConstructResult;
                }
                else
                {
                    response.ContentType = "application/sparql-results+json";
                    body = query.IndexOf("ASK", StringComparison.OrdinalIgnoreCase) >= 0 ? AskResult : SelectResult;
                }

                byte[] bytes = Encoding.UTF8.GetBytes(body);
                response.OutputStream.Write(bytes, 0, bytes.Length);
                response.Close();
            }
        }

        private IModel GetModel(string path = "sparql", NetworkCredential credentials = null)
        {
            IStore store = StoreFactory.CreateSparqlEndpointStore(new Uri(_baseUri, path), null, credentials);

            return store.GetModel(ModelUri);
        }

        #endregion

        #region Methods

        [Test]
        public void SelectQueryReturnsBindings()
        {
            var bindings = GetModel().ExecuteQuery(new SparqlQuery("SELECT ?s WHERE { ?s ?p ?o . }")).GetBindings().ToList();

            Assert.AreEqual(1, bindings.Count);
            Assert.AreEqual("http://example.org/a", bindings[0]["s"].ToString());
        }

        [Test]
        public void AskQueryReturnsAnswer()
        {
            Assert.IsTrue(GetModel().ExecuteQuery(new SparqlQuery("ASK WHERE { ?s ?p ?o . }")).GetAnwser());
        }

        [Test]
        public void ConstructQueryReturnsResources()
        {
            var resources = GetModel().ExecuteQuery(new SparqlQuery("CONSTRUCT { ?s ?p ?o . } WHERE { ?s ?p ?o . }")).GetResources().ToList();

            Assert.AreEqual(1, resources.Count);
            Assert.AreEqual(new Uri("http://example.org/a"), resources[0].Uri);
        }

        /// <summary>
        /// Credentials moved from the obsolete endpoint type onto an <c>HttpClientHandler</c> in the
        /// dotNetRDF migration. Both answer a challenge and neither sends credentials pre-emptively, so
        /// that is the behaviour pinned here.
        /// </summary>
        [Test]
        public void CredentialsAreSentWhenChallenged()
        {
            var model = GetModel("secured", new NetworkCredential("user", "secret"));

            Assert.AreEqual(1, model.ExecuteQuery(new SparqlQuery("SELECT ?s WHERE { ?s ?p ?o . }")).GetBindings().Count());

            string expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret"));

            Assert.AreEqual(new string[] { null, expected }, _authorizations.ToArray());
        }

        /// <summary>
        /// <c>SparqlQueryClient</c> is async-only while <see cref="IStore"/> is synchronous. Blocking on
        /// it directly from a thread with a single-threaded synchronization context — a WPF or WinForms
        /// UI thread — deadlocks, because its continuations are posted back to the context the caller
        /// is blocking. The obsolete <c>SparqlRemoteEndpoint</c> did not, so this is the regression
        /// that guards the migration.
        /// </summary>
        [TestCase("SELECT ?s WHERE { ?s ?p ?o . }")]
        [TestCase("CONSTRUCT { ?s ?p ?o . } WHERE { ?s ?p ?o . }")]
        public void QueryDoesNotDeadlockOnSingleThreadedContext(string queryString)
        {
            var model = GetModel();

            Exception error = null;

            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());

                try
                {
                    // The request is made eagerly, inside ExecuteQuery; reading the result is not needed.
                    model.ExecuteQuery(new SparqlQuery(queryString));
                }
                catch (Exception e)
                {
                    error = e;
                }
            }) { IsBackground = true };

            thread.Start();

            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "The query deadlocked on a single-threaded synchronization context.");
            Assert.IsNull(error, error?.ToString());
        }

        [Test]
        public void TestDBPediaQuery()
        {
            Assert.Inconclusive("Endpoint doesn't seem to exist anymore.");
            IStore store = StoreFactory.CreateSparqlEndpointStore(new Uri("http://live.dbpedia.org/sparql"));
            IModel model = store.GetModel(new Uri("http://dbpedia.org"));

            SparqlQuery query = new SparqlQuery(@"SELECT ?s ?p ?o WHERE { ?s ?p ?o . ?s <http://dbpedia.org/ontology/wikiPageID> @id . }");
            query.Bind("@id", 445980);

            Assert.AreEqual(1, model.ExecuteQuery(query).GetResources().Count());
        }

        [Test]
        public void TestDBPediaGetResource()
        {
            Assert.Inconclusive("Endpoint doesn't seem to exist anymore.");
            IStore store = StoreFactory.CreateSparqlEndpointStore(new Uri("http://live.dbpedia.org/sparql"));
            IModel model = store.GetModel(new Uri("http://dbpedia.org"));

            IResource r = model.GetResource(new Uri("http://dbpedia.org/resource/Munich"));

            Assert.Greater(r.ListProperties().Count(), 0);
        }

        #endregion

        /// <summary>
        /// A UI thread's context in miniature: continuations are posted to a queue that only the
        /// owning thread would drain, and that thread is blocked waiting for the query.
        /// </summary>
        private class NonPumpingSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object state)
            {
            }
        }
    }
}
