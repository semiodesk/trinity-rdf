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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF;

namespace Semiodesk.Trinity.Store.GraphDB
{
  /// <summary>
  /// Class for connecting to GraphDB triple stores.
  /// </summary>
  public class GraphDBConnector : SesameHttpProtocolVersion6Connector
  {
    #region Constructors
    
    /// <summary>
    /// Creates a new connection to a Sesame HTTP Protocol supporting Store.
    /// </summary>
    /// <param name="baseUri">URL of the database server.</param>
    /// <param name="repositoryName">Name of the GraphDB repository.</param>
    /// <param name="username">Username to use for requests that require authentication.</param>
    /// <param name="password">Password to use for requests that require authentication.</param>
    public GraphDBConnector(string baseUri, string repositoryName, string username, string password)
      : base(baseUri, repositoryName, username, password)
    {
    }
    
    #endregion

    #region Methods
    
    /// <summary>
    /// Makes a SPARQL Query against the underlying Store.
    /// </summary>
    /// <param name="sparqlQuery">SPARQL Query.</param>
    /// <param name="allowPlainTextResults">Indicate if the query may return results that have no RDF metadata.</param>
    /// <param name="inferenceEnabled">Indicate if the query should be executed with reasoning.</param>
    /// <returns></returns>
    public object Query(string sparqlQuery, bool allowPlainTextResults, bool inferenceEnabled)
    {
      var graph = new Graph();
      var results = new SparqlResultSet();
      
      Query(new GraphHandler(graph), new ResultSetHandler(results), sparqlQuery, allowPlainTextResults, inferenceEnabled);
      
      return results.ResultsType != SparqlResultsType.Unknown ? (object) results : (object) graph;
    }
    
    /// <summary>
    /// Makes a SPARQL Query against the underlying Store processing the results with an appropriate handler from those provided.
    /// </summary>
    /// <param name="rdfHandler">RDF Handler.</param>
    /// <param name="resultsHandler">Results Handler.</param>
    /// <param name="sparqlQuery">SPARQL Query.</param>
    /// <returns></returns>
    public override void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery)
    {
      Query(rdfHandler, resultsHandler, sparqlQuery, true, false);
    }
    
    /// <summary>
    /// Makes a SPARQL Query against the underlying Store processing the results with an appropriate handler from those provided.
    /// </summary>
    /// <param name="rdfHandler">RDF Handler.</param>
    /// <param name="resultsHandler">Results Handler.</param>
    /// <param name="sparqlQuery">SPARQL Query.</param>
    /// <param name="allowPlainTextResults">Indicate if the query may return results that have no RDF metadata.</param>
    /// <param name="inferenceEnabled">Indicate if the query should be executed with reasoning.</param>
    /// <returns></returns>
    public virtual void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery, bool allowPlainTextResults, bool inferenceEnabled)
    {
      try
      {
        VDS.RDF.Query.SparqlQuery sparqlQuery1 = null;

        string accept;
        
        if (allowPlainTextResults)
        {
          try
          {
            sparqlQuery1 = new SparqlQueryParser().ParseFromString(sparqlQuery);
            allowPlainTextResults = sparqlQuery1.QueryType == VDS.RDF.Query.SparqlQueryType.Ask;
          }
          catch
          {
            allowPlainTextResults = Regex.IsMatch(sparqlQuery, "ASK", RegexOptions.IgnoreCase);
          }

          accept = sparqlQuery1 == null
            ? MimeTypesHelper.HttpRdfOrSparqlAcceptHeader
            : (SparqlSpecsHelper.IsSelectQuery(sparqlQuery1.QueryType) || sparqlQuery1.QueryType == VDS.RDF.Query.SparqlQueryType.Ask
              ? MimeTypesHelper.HttpSparqlAcceptHeader
              : MimeTypesHelper.HttpAcceptHeader);
        }
        else
        {
          accept = MimeTypesHelper.HttpAcceptHeader;
        }

        var url = _repositoriesPrefix + _store + _queryPath;
        var parameters = new Dictionary<string, string>();

        if (inferenceEnabled)
        {
          parameters["infer"] = "true";
        }

        var request = CreateRequest(url, accept, "POST", parameters);
        request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
        
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("query=");
        queryBuilder.Append(HttpUtility.UrlEncode(this.EscapeQuery(sparqlQuery)));
        
        using (var writer = new StreamWriter(request.GetRequestStream(), new UTF8Encoding(Options.UseBomForUtf8)))
        {
          writer.Write(queryBuilder);
          writer.Close();
        }

        Tools.HttpDebugRequest(request);
        
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        {
          Tools.HttpDebugResponse(response);
          
          var input = new StreamReader(response.GetResponseStream());
          
          try
          {
            MimeTypesHelper.GetSparqlParser(response.ContentType, allowPlainTextResults).Load(resultsHandler, input);
            response.Close();
          }
          catch (RdfParserSelectionException)
          {
            if (response.ContentType.StartsWith("application/xml"))
            {
              try
              {
                MimeTypesHelper.GetSparqlParser("application/sparql-results+xml").Load(resultsHandler, input);
                response.Close();
              }
              catch (RdfParserSelectionException)
              {
              }
            }

            var parser = MimeTypesHelper.GetParser(response.ContentType);
            
            if (sparqlQuery1 != null && (SparqlSpecsHelper.IsSelectQuery(sparqlQuery1.QueryType) ||
                                         sparqlQuery1.QueryType == VDS.RDF.Query.SparqlQueryType.Ask))
              new SparqlRdfParser(parser).Load(resultsHandler, input);
            else
            {
              parser.Load(rdfHandler, input);
            }

            response.Close();
          }
        }
      }
      catch (WebException ex)
      {
        throw StorageHelper.HandleHttpQueryError(ex);
      }
    }
    
    public override IEnumerable<Uri> ListGraphs()
    {
      try
      {
        // Note: This query fails if allowPlainTextResults is true which is the default in dotNetRdf.
        var obj = Query("SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } }", false, false);

        if (!(obj is SparqlResultSet))
        {
          return Enumerable.Empty<Uri>();
        }

        var uriList = new List<Uri>();
        
        foreach (SparqlResult sparqlResult in (SparqlResultSet) obj)
        {
          if (sparqlResult.HasValue("g"))
          {
            INode node = sparqlResult["g"];

            if (node.NodeType == NodeType.Uri)
            {
              uriList.Add(((IUriNode)node).Uri);
            }
          }
        }
        
        return uriList;
      }
      catch (Exception ex)
      {
        throw StorageHelper.HandleError(ex, "listing Graphs from");
      }
    }
    
    #endregion
  }
}