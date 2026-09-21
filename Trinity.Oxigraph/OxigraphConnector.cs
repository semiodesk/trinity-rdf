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
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Writing;

namespace Semiodesk.Trinity.Store.Oxigraph
{
    /// <summary>
    /// Connects to an Oxigraph server.
    /// </summary>
    /// <remarks>
    /// Oxigraph serves the SPARQL 1.1 Graph Store HTTP Protocol at <c>/store</c>, SPARQL Query at
    /// <c>/query</c> and SPARQL Update at <c>/update</c>. <see cref="SparqlHttpProtocolConnector"/>
    /// already speaks the Graph Store Protocol, which is what the store layer leans on — of the
    /// connector calls a Trinity store makes, the large majority are <c>HasGraph</c>,
    /// <c>SaveGraph</c>, <c>LoadGraph</c> and <c>DeleteGraph</c>. So this type is the same shape as
    /// dotNetRDF's own <c>FusekiConnector</c>: that base, plus query, update and graph listing.
    ///
    /// It exists rather than reusing <c>FusekiConnector</c> because that connector derives all three
    /// endpoints from one URI and <b>requires it to end in <c>/data</c></b>. Pointed at an Oxigraph
    /// server it would resolve <c>/query</c> and <c>/update</c> correctly by coincidence of layout,
    /// and resolve the Graph Store endpoint to <c>/data</c>, where Oxigraph serves nothing — so the
    /// majority of operations would 404. The endpoints are taken separately here for that reason.
    /// </remarks>
    public class OxigraphConnector : SparqlHttpProtocolConnector, IUpdateableStorage
    {
        #region Members

        private readonly Uri _queryUri;

        private readonly Uri _updateUri;

        /// <summary>
        /// Oxigraph has no list-graphs operation of its own, but it answers the SPARQL query used
        /// below, so listing is supported here even though the Graph Store Protocol has no such verb.
        /// </summary>
        public override bool ListGraphsSupported => true;

        /// <summary>
        /// Gets that triple level updates are supported.
        /// </summary>
        public override bool UpdateSupported => true;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new connection to an Oxigraph server.
        /// </summary>
        /// <param name="serverUri">Base URI of the server, e.g. <c>http://localhost:7878/</c>.</param>
        public OxigraphConnector(Uri serverUri)
            : base(GetEndpoint(serverUri, "store"))
        {
            _queryUri = GetEndpoint(serverUri, "query");
            _updateUri = GetEndpoint(serverUri, "update");
        }

        /// <summary>
        /// Creates a new connection to an Oxigraph server.
        /// </summary>
        /// <param name="serverUri">Base URI of the server, e.g. <c>http://localhost:7878/</c>.</param>
        public OxigraphConnector(string serverUri)
            : this(ToUri(serverUri))
        {
        }

        #endregion

        #region Methods

        private static Uri ToUri(string serverUri)
        {
            if (string.IsNullOrWhiteSpace(serverUri))
            {
                throw new ArgumentException("An Oxigraph server URI is required.", nameof(serverUri));
            }

            return new Uri(serverUri);
        }

        /// <summary>
        /// Appends a path segment to the server URI.
        /// </summary>
        /// <remarks>
        /// The trailing slash is forced because <see cref="Uri"/> resolution drops the last segment
        /// without it, so <c>http://host/oxigraph</c> + <c>query</c> would give
        /// <c>http://host/query</c> rather than <c>http://host/oxigraph/query</c> — silently talking
        /// to the wrong path when the server is behind a prefix.
        /// </remarks>
        private static Uri GetEndpoint(Uri serverUri, string path)
        {
            if (serverUri == null)
            {
                throw new ArgumentNullException(nameof(serverUri));
            }

            var baseUri = serverUri.AbsoluteUri.EndsWith("/")
                ? serverUri
                : new Uri(serverUri.AbsoluteUri + "/");

            return new Uri(baseUri, path);
        }

        /// <summary>
        /// Executes a SPARQL query against the Oxigraph server.
        /// </summary>
        /// <param name="sparqlQuery">SPARQL query.</param>
        /// <returns>A <see cref="SparqlResultSet"/> or an <see cref="IGraph"/>, by query form.</returns>
        public object Query(string sparqlQuery)
        {
            var graph = new Graph();
            var results = new SparqlResultSet();

            Query(new GraphHandler(graph), new ResultSetHandler(results), sparqlQuery);

            return results.ResultsType != SparqlResultsType.Unknown ? (object)results : graph;
        }

        /// <summary>
        /// Executes a SPARQL query, processing the results with the given handlers.
        /// </summary>
        /// <param name="rdfHandler">Handler for a graph result.</param>
        /// <param name="resultsHandler">Handler for a result set.</param>
        /// <param name="sparqlQuery">SPARQL query.</param>
        public virtual void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery)
        {
            try
            {
                // POST with application/sparql-query rather than a GET with the query in the URL:
                // there is no length ceiling to fall off, and the generated queries are routinely
                // long enough to matter.
                var request = new HttpRequestMessage(HttpMethod.Post, _queryUri)
                {
                    Content = new StringContent(sparqlQuery, Encoding.UTF8, MimeTypesHelper.SparqlQuery)
                };

                request.Headers.Add("Accept", AcceptFor(sparqlQuery));

                using (var response = HttpClient.SendAsync(request).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw StorageHelper.HandleHttpQueryError(response);
                    }

                    var contentType = response.Content.Headers.ContentType?.MediaType;

                    using (var stream = response.Content.ReadAsStreamAsync().Result)
                    using (var reader = new StreamReader(stream))
                    {
                        // ASK and SELECT come back as results; CONSTRUCT and DESCRIBE come back as a
                        // graph. Which one it is can only be known from the response content type.
                        try
                        {
                            ISparqlResultsReader resultsReader = MimeTypesHelper.GetSparqlParser(contentType);

                            resultsReader.Load(resultsHandler, reader);

                            return;
                        }
                        catch (RdfParserSelectionException)
                        {
                            // Not a results format, so it is RDF.
                        }

                        IRdfReader rdfReader = MimeTypesHelper.GetParser(contentType);

                        rdfReader.Load(rdfHandler, reader);
                    }
                }
            }
            catch (RdfStorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw StorageHelper.HandleError(ex, "querying");
            }
        }

        /// <summary>
        /// The Accept header to send for a given query.
        /// </summary>
        /// <remarks>
        /// Chosen by query form rather than sent as one catch-all list, because the catch-all
        /// includes the plain-text result formats and Oxigraph will negotiate to them: an ASK then
        /// comes back as the five bytes <c>false</c>, which is not a SPARQL result document and not
        /// RDF, and the response fails to parse rather than answering the question. GraphDB's
        /// connector carries the same caveat in its own words ("this query fails if
        /// allowPlainTextResults is true, which is the default in dotNetRdf").
        ///
        /// A query that will not parse gets the catch-all: the server, not the Accept header, is
        /// the right thing to be refused by.
        /// </remarks>
        /// <param name="sparqlQuery">The query about to be sent.</param>
        private static string AcceptFor(string sparqlQuery)
        {
            try
            {
                var parsed = new SparqlQueryParser().ParseFromString(sparqlQuery);

                return SparqlSpecsHelper.IsSelectQuery(parsed.QueryType)
                       || parsed.QueryType == VDS.RDF.Query.SparqlQueryType.Ask
                    ? MimeTypesHelper.HttpSparqlAcceptHeader
                    : MimeTypesHelper.HttpAcceptHeader;
            }
            catch
            {
                return MimeTypesHelper.HttpRdfOrSparqlAcceptHeader;
            }
        }

        /// <summary>
        /// Executes a SPARQL update against the Oxigraph server.
        /// </summary>
        /// <param name="sparqlUpdate">SPARQL update.</param>
        public void Update(string sparqlUpdate)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _updateUri)
                {
                    Content = new StringContent(sparqlUpdate, Encoding.UTF8, MimeTypesHelper.SparqlUpdate)
                };

                using (var response = HttpClient.SendAsync(request).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw StorageHelper.HandleHttpError(response, "updating");
                    }
                }
            }
            catch (RdfStorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw StorageHelper.HandleError(ex, "updating");
            }
        }

        /// <summary>
        /// Writes a graph to the server, replacing whatever it held under that name.
        /// </summary>
        /// <remarks>
        /// Overridden because the inherited implementation sends RDF/XML encoded as UTF-8 with a
        /// byte order mark, and Oxigraph rejects both. Neither is an Oxigraph quirk: in both cases
        /// what dotNetRDF emits is at fault, and the other backends accepting it is why that has
        /// gone unnoticed.
        ///
        /// <list type="bullet">
        /// <item>dotNetRDF's RDF/XML writer emits a DTD whose entity values are unquoted --
        /// <c>&lt;!ENTITY ns0 http://example.org/&gt;</c> -- which is not well-formed XML. Oxigraph
        /// answers HTTP 400 "&lt;!ENTITY values should be enclosed in double quotes".</item>
        /// <item>A byte order mark is not valid at the head of a Turtle document. Oxigraph reports
        /// it as "not a valid subject or graph name" at line 1, column 1.</item>
        /// </list>
        ///
        /// Writing Turtle from an explicit BOM-less encoder settles both. Setting the encoding on a
        /// <see cref="MimeTypeDefinition"/> does not: the inherited SaveGraph never consults it.
        /// </remarks>
        /// <param name="g">The graph to write. Its name selects the target graph; an unnamed graph
        /// goes to the default graph.</param>
        public override void SaveGraph(IGraph g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            try
            {
                string body;

                using (var writer = new System.IO.StringWriter())
                {
                    new CompressingTurtleWriter().Save(g, writer);

                    body = writer.ToString();
                }

                var request = new HttpRequestMessage(HttpMethod.Put, GraphEndpoint(g.Name))
                {
                    // UTF8Encoding(false) rather than Encoding.UTF8: the latter prefixes a BOM.
                    Content = new StringContent(body, new UTF8Encoding(false), MimeTypesHelper.Turtle[0])
                };

                using (var response = HttpClient.SendAsync(request).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw StorageHelper.HandleHttpError(response, "saving a Graph to");
                    }
                }
            }
            catch (RdfStorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw StorageHelper.HandleError(ex, "saving a Graph to");
            }
        }

        /// <summary>
        /// The Graph Store Protocol URL addressing a particular graph, or the default graph.
        /// </summary>
        /// <param name="name">Name of the graph, or <c>null</c> for the default graph.</param>
        private Uri GraphEndpoint(IRefNode name)
        {
            // The base's _serviceUri: the Graph Store endpoint this connector was constructed with,
            // held once there rather than copied here. It is a string, not a Uri.
            var store = _serviceUri;

            return name is IUriNode uri
                ? new Uri($"{store}?graph={Uri.EscapeDataString(uri.Uri.AbsoluteUri)}")
                : new Uri($"{store}?default");
        }

        /// <summary>
        /// Lists the names of the graphs held by the server.
        /// </summary>
        /// <remarks>
        /// Overridden because the base throws: the Graph Store HTTP Protocol has no list operation,
        /// so <see cref="SparqlHttpProtocolConnector"/> declares listing unsupported. Oxigraph does
        /// answer the equivalent SPARQL query, which is how the other HTTP backends list graphs too.
        /// </remarks>
        public override IEnumerable<string> ListGraphNames()
        {
            try
            {
                if (!(Query("SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } }") is SparqlResultSet results))
                {
                    throw new RdfStorageException(
                        "Tried to list the graphs of an Oxigraph server but did not get a SPARQL result set back.");
                }

                var names = new List<string>();

                foreach (var result in results)
                {
                    if (!result.HasValue("g"))
                    {
                        continue;
                    }

                    switch (result["g"])
                    {
                        case IUriNode uri:
                            names.Add(uri.Uri.AbsoluteUri);
                            break;

                        case IBlankNode blank:
                            names.Add(blank.InternalID);
                            break;
                    }
                }

                return names;
            }
            catch (RdfStorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw StorageHelper.HandleError(ex, "listing graphs from");
            }
        }

        /// <summary>
        /// Gets a string which gives details of the connection.
        /// </summary>
        public override string ToString()
        {
            return $"[Oxigraph] {_queryUri.AbsoluteUri}";
        }

        #endregion
    }
}
