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
using System.Text;
using VDS.RDF;
using VDS.RDF.Writing;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This class encapsulates the functionality of an abstract triple store. Cannot be used directly. 
    /// Use StoreFactory to get a concrete implementation.
    /// </summary>
    public abstract class StoreBase : IStore
    {
        #region Methods

        /// <summary>
        /// Indicates if the store is connected and awaiting queries.
        /// </summary>
        public virtual bool IsReady { get; protected set; } = true;

        /// <summary>
        /// Set this property to log the SPARQL queries which are executed on this store.
        /// For example, to log to the console, set this property to System.Console.Write(System.String).
        /// </summary>
        public Action<string> Log { get; set; }

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="uri">Uri of the model which is to be removed.</param>
        public abstract void RemoveModel(Uri uri);

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="model">Handle of the model which is to be removed.</param>
        public virtual void RemoveModel(IModel model)
        {
            RemoveModel(model.Uri);
        }

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty
        /// </summary>
        /// <param name="uri">Uri of the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty")]
        public abstract bool ContainsModel(Uri uri);

        /// <summary>
        /// Query if the model exists in the store.
        /// OBSOLETE: This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty
        /// </summary>
        /// <param name="model">Handle to the model which is to be queried.</param>
        /// <returns></returns>
        [Obsolete("This method does not list empty models. At the moment you should just call GetModel() and test for IsEmpty")]
        public virtual bool ContainsModel(IModel model)
        {
            // A null model contains nothing; it is not an error. Every store's override of this
            // overload was a verbatim delegation to the Uri overload, so they are gone and this is
            // the single implementation -- which is what makes the guard reach all of them.
            return model != null && ContainsModel(model.Uri);
        }

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
        public abstract IEnumerable<IModel> ListModels();

        /// <summary>
        /// Executes a <c>SparqlQuery</c> on the store.
        /// </summary>
        /// <param name="query">SPARQL query to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        /// <returns></returns>
        public abstract ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null);

        /// <summary>
        /// Executes a string query directly on the store.
        /// </summary>
        /// <param name="queryString">SPARQL query to be executed.</param>
        /// <returns>A native return value is possible here.</returns>
        public virtual object ExecuteQuery(string queryString)
        {
            return null;
        }

        /// <summary>
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="update">SPARQL Update to be executed.</param>
        /// <param name="transaction">An optional transaction.</param>
        public abstract void ExecuteNonQuery(ISparqlUpdate update, ITransaction transaction = null);

        /// <summary>
        /// Starts a transaction. The resulting transaction handle can be used to chain operations together.
        /// </summary>
        /// <param name="isolationLevel">Isolation level of the operations executed in the transaction.</param>
        /// <returns></returns>
        public abstract ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel);

        /// <summary>
        /// Loads a serialized graph from the given location into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="modelUri">Uri of the graph in this store</param>
        /// <param name="url">Location</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public abstract Uri Read(Uri modelUri, Uri url, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Loads a serialized graph from the given stream into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after reading completes.</param>
        /// <returns></returns>
        public abstract Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update, bool leaveOpen=false);

        /// <summary>
        /// Loads a serialized graph from the given string into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="content">string containing a serialized graph</param>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="update">Pass false if you want to overwrite the existing data. True if you want to add the new data to the existing.</param>
        /// <returns></returns>
        public abstract Uri Read(string content, Uri graphUri, RdfSerializationFormat format, bool update);

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="fs">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store</param>
        /// <param name="format">Allowed formats</param>
        /// <param name="namespaces">Defines namespace to prefix mappings for the output.</param>
        /// <param name="baseUri">Base URI for shortening URIs in formats that support it.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after the writing finished.</param>
        /// <returns></returns>
        public abstract void Write(Stream fs, Uri graphUri, RdfSerializationFormat format, INamespaceMap namespaces = null, Uri baseUri = null, bool leaveOpen = false);

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="fs">Stream to which the content should be written.</param>
        /// <param name="graphUri">Uri fo the graph in this store</param>
        /// <param name="writer">A RDF writer.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after the writing finished.</param>
        /// <returns></returns>
        public abstract void Write(Stream fs, Uri graphUri, IRdfWriter writer, bool leaveOpen = false);

        /// <summary>
        /// Disposes this store and it's underlying connection. This object cannot be reused after disposing.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Adds a new model with the given uri to the storage. 
        /// </summary>
        /// <param name="uri">Uri of the model</param>
        /// <returns>Handle to the model</returns>
        [Obsolete("It is not necessary to create models explicitly. Use GetModel() instead, if the model does not exist, it will be created implicitly.")]
        public virtual IModel CreateModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        /// <summary>
        /// Gets a handle to a model in the store.
        /// </summary>
        /// <param name="uri">Model URI.</param>
        /// <returns></returns>
        public virtual IModel GetModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of uris of the models that should be grouped together.</param>
        /// <returns></returns>
        public virtual IModelGroup CreateModelGroup(params Uri[] models)
        {
            List<IModel> result = new List<IModel>();

            foreach (var model in models)
            {
                result.Add(GetModel(model));
            }

            return new ModelGroup(this, result);
        }

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models">The list of model handles that should be grouped together.</param>
        /// <returns></returns>
        public virtual IModelGroup CreateModelGroup(params IModel[] models)
        {
            List<IModel> result = new List<IModel>();

            foreach (var model in models)
            {
                result.Add(GetModel(model.Uri));
            }

            return new ModelGroup(this, result);
        }


        /// <summary>
        /// Updates the properties of a resource in the backing RDF store.
        /// </summary>
        /// <param name="resource">Resource that is to be updated in the backing store.</param>
        /// <param name="modelUri">Uri of the model where the resource will be updated</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <param name="ignoreUnmappedProperties">Set this to true to update only mapped properties.</param>
        public virtual void UpdateResource(Resource resource, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            string updateString;

            if (resource.IsNew)
            {
                if (resource.Uri.IsBlankId)
                {
                    string queryString = string.Format(@"SELECT BNODE() AS ?x FROM <{0}> WHERE {{}}", modelUri.OriginalString);

                    var result = ExecuteQuery(new SparqlQuery(queryString), transaction);
                    var id = result.GetBindings().First()["x"] as UriRef;

                    resource.Uri = id;
                }

                updateString = string.Format(@"
                    WITH <{0}>
                    INSERT {{ {1} }} 
                    WHERE {{}}",
                modelUri.OriginalString,
                SparqlSerializer.SerializeResource(resource, ignoreUnmappedProperties));
            }
            else if (TryBuildDeltaUpdate(resource, modelUri, ignoreUnmappedProperties, out updateString))
            {
                if (updateString == null)
                {
                    // Nothing changed since this copy was loaded — writing would only risk clobbering
                    // whatever another writer has done in the meantime.
                    resource.IsNew = false;
                    resource.IsSynchronized = true;

                    return;
                }
            }
            else
            {
                // The resource was never synchronized, so there is no baseline to diff against and the
                // whole resource has to be replaced.
                updateString = string.Format(@"
                    WITH <{0}>
                    DELETE {{ {1} ?p ?o. }}
                    INSERT {{ {2} }}
                    WHERE {{ OPTIONAL {{ {1} ?p ?o. }} }} ",
                modelUri.OriginalString,
                SparqlSerializer.SerializeUri(resource.Uri),
                SparqlSerializer.SerializeResource(resource, ignoreUnmappedProperties));
            }

            ExecuteNonQuery(new SparqlUpdate(updateString), transaction);

            resource.IsNew = false;
            resource.IsSynchronized = true;
        }

        /// <summary>
        /// Builds a SPARQL update that writes only the values which changed since the resource was last
        /// synchronized, instead of replacing the resource as a whole.
        /// </summary>
        /// <remarks>
        /// Naming only the changed triples is what stops two callers who loaded the same resource from
        /// erasing each other's edits — see <c>doc/trinity-write-semantics.md</c>. Every store backend
        /// shares this so the semantics cannot drift between them.
        /// </remarks>
        /// <param name="resource">The resource being committed.</param>
        /// <param name="modelUri">Uri of the model the resource lives in.</param>
        /// <param name="ignoreUnmappedProperties">Set this to true to write only mapped properties.</param>
        /// <param name="updateString">
        /// Receives the update, or <c>null</c> when nothing changed and no write is needed.
        /// </param>
        /// <returns>
        /// False if the resource has never been synchronized, so no delta can be computed and the caller
        /// must fall back to replacing the whole resource.
        /// </returns>
        protected static bool TryBuildDeltaUpdate(Resource resource, Uri modelUri, bool ignoreUnmappedProperties, out string updateString)
        {
            updateString = null;

            if (!SparqlSerializer.TrySerializeResourceDelta(resource, ignoreUnmappedProperties, out var deleted, out var inserted))
            {
                return false;
            }

            if (deleted.Count == 0 && inserted.Count == 0)
            {
                return true;
            }

            var subject = SparqlSerializer.SerializeUri(resource.Uri);
            var update = new StringBuilder();

            update.AppendFormat("WITH <{0}> ", modelUri.OriginalString);

            if (deleted.Count > 0)
            {
                update.AppendFormat("DELETE {{ {0} }} ", SerializeTripleBlock(subject, deleted));
            }

            if (inserted.Count > 0)
            {
                update.AppendFormat("INSERT {{ {0} }} ", SerializeTripleBlock(subject, inserted));
            }

            // An empty pattern yields exactly one solution, so the ground templates apply once.
            update.Append("WHERE {}");

            updateString = update.ToString();

            return true;
        }

        /// <summary>
        /// Joins <c>predicate object</c> fragments into a triple block for a single subject.
        /// </summary>
        protected static string SerializeTripleBlock(string subject, IEnumerable<string> predicateObjects)
        {
            var result = new StringBuilder();

            foreach (var predicateObject in predicateObjects)
            {
                result.AppendFormat("{0} {1}. ", subject, predicateObject);
            }

            return result.ToString();
        }

        /// <summary>
        /// Updates the properties of multiple resources in the backing RDF store.
        /// </summary>
        /// <param name="resources">Resources that are to be updated in the backing store.</param>
        /// <param name="modelUri">Uri of the model where the resource will be updated</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <param name="ignoreUnmappedProperties">Set this to true to update only mapped properties.</param>
        public virtual void UpdateResources(IEnumerable<Resource> resources, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false)
        {
            string WITH = $"{SparqlSerializer.SerializeUri(modelUri)} ";

            // Resources that have been synchronized are written as a delta, exactly as in
            // UpdateResource — a bulk write must not erase other writers' values either.
            StringBuilder deltaDelete = new StringBuilder();
            StringBuilder deltaInsert = new StringBuilder();

            // Resources without a baseline still have to be replaced wholesale.
            StringBuilder INSERT = new StringBuilder();
            StringBuilder DELETE = new StringBuilder();
            StringBuilder OPTIONAL = new StringBuilder();

            int count = 0;
            foreach (var res in resources)
            {
                if (SparqlSerializer.TrySerializeResourceDelta(res, ignoreUnmappedProperties, out var deleted, out var inserted))
                {
                    var subject = SparqlSerializer.SerializeUri(res.Uri);

                    if (deleted.Count > 0)
                    {
                        deltaDelete.Append(SerializeTripleBlock(subject, deleted));
                    }

                    if (inserted.Count > 0)
                    {
                        deltaInsert.Append(SerializeTripleBlock(subject, inserted));
                    }
                }
                else
                {
                    DELETE.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p{count} ?o{count}. ");
                    OPTIONAL.Append($" {SparqlSerializer.SerializeUri(res.Uri)} ?p{count} ?o{count}. ");
                    INSERT.Append($" {SparqlSerializer.SerializeResource(res, ignoreUnmappedProperties)} ");
                    count++;
                }
            }

            if (deltaDelete.Length > 0 || deltaInsert.Length > 0)
            {
                var delta = new StringBuilder();

                delta.AppendFormat("WITH {0} ", WITH);

                if (deltaDelete.Length > 0)
                {
                    delta.AppendFormat("DELETE {{ {0} }} ", deltaDelete);
                }

                if (deltaInsert.Length > 0)
                {
                    delta.AppendFormat("INSERT {{ {0} }} ", deltaInsert);
                }

                delta.Append("WHERE {}");

                ExecuteNonQuery(new SparqlUpdate(delta.ToString()), transaction);
            }

            if (count > 0)
            {
                string updateString = $"WITH {WITH} DELETE {{ {DELETE} }} INSERT {{ {INSERT} }} WHERE {{ OPTIONAL {{ {OPTIONAL} }} }}";

                ExecuteNonQuery(new SparqlUpdate(updateString), transaction);
            }

            foreach (var resource in resources)
            {
                resource.IsNew = false;
                resource.IsSynchronized = true;
            }
        }

        /// <summary>
        /// Writes a serialized graph to the given stream. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="stream">Stream to which the content should be written.</param>
        /// <param name="graph">The graph to be serialized.</param>
        /// <param name="format">The serialization format.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after writing completes.</param>
        public void Write(Stream stream, IGraph graph, RdfSerializationFormat format, bool leaveOpen)
        {
            StreamWriter writer = new StreamWriter(stream);
            
            switch (format)
            {
                case RdfSerializationFormat.GZippedJsonLd:
                    {
                        var w = new GZippedJsonLdWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedN3:
                    {
                        var w = new GZippedNotation3Writer();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedNQuads:
                    {
                        var w = new GZippedNQuadsWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedRdfXml:
                    {
                        var w = new GZippedRdfXmlWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedTrig:
                    {
                        var w = new GZippedTriGWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.GZippedTurtle:
                    {
                        var w = new GZippedTurtleWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.Json:
                    {
                        var w = new RdfJsonWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }

#if !NET35
                case RdfSerializationFormat.JsonLd:
                    {
                        var w = new JsonLdWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
#endif
                case RdfSerializationFormat.N3:
                    {
                        var w = new Notation3Writer();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }

#if !NET35
                case RdfSerializationFormat.NQuads:
                    {
                        var w = new NQuadsWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
#endif
                case RdfSerializationFormat.NTriples:
                    {
                        var w = new NTriplesWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }

                
                case RdfSerializationFormat.RdfXml:
                    {
                        var w = new RdfXmlWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
                case RdfSerializationFormat.Trig:
                    {
                        var w = new TriGWriter();
                        var sgWriter = new SingleGraphWriter(w);
                        sgWriter.Save(graph, writer, leaveOpen);
                        break;
                    }
                default:
                case RdfSerializationFormat.Turtle:
                    {
                        var w = new CompressingTurtleWriter();
                        w.Save(graph, writer, leaveOpen);
                        break;
                    }
            }

            if(leaveOpen)
            {
                writer.Flush();
            }
        }

        /// <summary>
        /// Writes a serialized graph to the given stream using a specific RDF writer.
        /// </summary>
        /// <param name="stream">Stream to which the content should be written.</param>
        /// <param name="graph">The graph to be serialized.</param>
        /// <param name="formatWriter">A RDF format writer.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after writing completes.</param>
        public void Write(Stream stream, IGraph graph, IRdfWriter formatWriter, bool leaveOpen)
        {
            StreamWriter streamWriter = new StreamWriter(stream);

            formatWriter.Save(graph, streamWriter, leaveOpen);

            if(leaveOpen)
            {
                streamWriter.Flush();
            }
        }

        /// <summary>
        /// Removes a resource from a model, including every statement that references it as an object.
        /// </summary>
        /// <param name="modelUri">Uri of the model the resource belongs to.</param>
        /// <param name="resourceUri">Uri of the resource to be removed.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void DeleteResource(Uri modelUri, Uri resourceUri, ITransaction transaction = null)
        {
            // NOTE: Regrettably, dotNetRDF does not support the full SPARQL 1.1 update syntax. To be precise,
            // it does not support FILTERs or OPTIONAL in Modify clauses.

            SparqlUpdate delete = new SparqlUpdate(@"
                DELETE WHERE { GRAPH @graph { @subject ?p ?o . } }; 
                DELETE WHERE { GRAPH @graph { ?s ?p @object . } }");
            delete.Bind("@graph", modelUri);
            delete.Bind("@subject", resourceUri);
            delete.Bind("@object", resourceUri);

            ExecuteNonQuery(delete, transaction);
        }

        /// <summary>
        /// Removes a resource from its model, including every statement that references it as an object.
        /// </summary>
        /// <param name="resource">The resource to be removed.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            DeleteResource(resource.Model.Uri, resource.Uri, transaction);
        }

        /// <summary>
        /// Removes several resources from a model, including statements that reference them as objects.
        /// </summary>
        /// <param name="modelUri">Uri of the model the resources belong to.</param>
        /// <param name="resources">Uris of the resources to be removed.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void DeleteResources(Uri modelUri, IEnumerable<Uri> resources, ITransaction transaction = null)
        {
            foreach (var resource in resources)
                DeleteResource(modelUri, resource, transaction);
        }

        /// <summary>
        /// Removes several resources from their models, including statements that reference them as objects.
        /// </summary>
        /// <param name="resources">The resources to be removed.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        public virtual void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null)
        {
            foreach (var resource in resources)
                DeleteResource(resource, transaction);
        }

        /// <summary>
        /// Groups the graphs parsed from a TriG file by the graph they should be written to, merging
        /// any that share a target.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Lives here rather than in each backend because it was written twice, identically, and two
        /// copies of a routing rule drift into triples landing in the wrong graph on one backend only
        /// -- the hardest version of this bug to notice. A new backend gets the behaviour rather than
        /// a third copy.
        /// </para>
        /// A graph carrying its own name is written under that name. Triples carrying none go to
        /// <paramref name="graphUri"/>, the graph the caller asked to read into: they have no home of
        /// their own, and silently discarding them would be data loss the caller cannot detect, since
        /// <c>Read</c> returns the same URI either way.
        /// </remarks>
        /// <param name="store">The parsed TriG content.</param>
        /// <param name="graphUri">Target for triples with no graph name of their own.</param>
        protected static IEnumerable<(Uri Uri, IGraph Graph)> GroupByTargetGraph(ITripleStore store, Uri graphUri)
        {
            var targets = new Dictionary<string, (Uri Uri, IGraph Graph)>();

            foreach (var parsed in store.Graphs)
            {
                var target = (parsed.Name as IUriNode)?.Uri ?? graphUri;

                if (!targets.TryGetValue(target.OriginalString, out var entry))
                {
                    // Named with the target so the graph is self-describing; BaseUri because that is
                    // what the connector actually reads when deciding where to write (ADR-0038).
                    IGraph merged = new Graph(new UriNode(target)) { BaseUri = target };

                    entry = (target, merged);
                    targets[target.OriginalString] = entry;
                }

                entry.Graph.Merge(parsed);
            }

            return targets.Values;
        }

        /// <summary>
        /// Gets a SPARQL query which is used to retrieve all triples about a subject that is
        /// either referenced using a URI or blank node.
        /// </summary>
        /// <param name="modelUri">The graph to be queried.</param>
        /// <param name="subjectUri">The subject to be described.</param>
        /// <returns>An instance of <c>ISparqlQuery</c></returns>
        public virtual ISparqlQuery GetDescribeQuery(Uri modelUri, Uri subjectUri)
        {
            ISparqlQuery query = new SparqlQuery("DESCRIBE @subject FROM @model");
            query.Bind("@model", modelUri);
            query.Bind("@subject", subjectUri);

            return query;
        }

        #endregion
    }
}
