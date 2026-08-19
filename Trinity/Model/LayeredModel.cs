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
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VDS.RDF;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Implementation of <see cref="ILayeredModel"/>.
    /// </summary>
    /// <remarks>
    /// Every read this class formulates is composed from <see cref="LayeredModelSparql"/>, so the
    /// overlay reaches the graph pattern of each one. Reads it cannot rewrite — caller-supplied
    /// SPARQL, and anything with inferencing on — throw instead of returning unsubtracted data.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LayeredModel : ILayeredModel
    {
        #region Members

        private readonly IStore _store;

        private readonly MethodInfo _getResourceMethod;

        /// <summary>
        /// Cached <c>FROM NAMED</c> clause. The three graphs are fixed for the lifetime of the
        /// view, so unlike a model group's dataset clause this never needs invalidating.
        /// </summary>
        private readonly string _datasetClause;

        /// <inheritdoc />
        public IModel Baseline { get; }

        /// <inheritdoc />
        public IModel Additions { get; }

        /// <inheritdoc />
        public IModel Removals { get; }

        /// <summary>
        /// All unmapped properties will be ignored for update and thus deleted.
        /// </summary>
        /// <remarks>
        /// Settable for interface compatibility only — this view never writes.
        /// </remarks>
        public bool IgnoreUnmappedProperties { get; set; } = false;

        /// <summary>
        /// A layered view spans three graphs, so it has no URI of its own — as with a model group.
        /// </summary>
        public UriRef Uri => null;

        /// <summary>
        /// Tests whether the view is empty, i.e. whether the effective graph holds no triple.
        /// </summary>
        /// <remarks>
        /// A baseline whose every triple is staged for removal reads as empty here while the
        /// baseline graph itself is not.
        /// </remarks>
        public bool IsEmpty
        {
            get
            {
                ISparqlQuery query = CreateOverlayQuery(
                    "ASK " + _datasetClause + "{ " + Overlay("?s", "?p", "?o") + " }");

                return !ExecuteOverlayQuery(query, null).GetAnwser();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a layered view over three models of the same store.
        /// </summary>
        /// <param name="store">The store holding all three graphs.</param>
        /// <param name="baseline">The unchanged graph read through the view.</param>
        /// <param name="additions">Triples staged for addition.</param>
        /// <param name="removals">Triples staged for removal.</param>
        /// <remarks>
        /// Internal by design: use <c>IStore.CreateLayeredModel(...)</c>. All three graphs must live
        /// in <paramref name="store"/>, because the overlay is one SPARQL query over one dataset -
        /// see the remarks on <c>StoreExtensions.CreateLayeredModel</c>. Routing construction
        /// through the factory, which resolves all three URIs against a single store, makes a
        /// cross-store view unrepresentable rather than merely discouraged.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown if any two of the three models are the same graph. Overlapping layers would make
        /// the view's result depend on evaluation order rather than on the staged change.
        /// </exception>
        internal LayeredModel(IStore store, IModel baseline, IModel additions, IModel removals)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
            Additions = additions ?? throw new ArgumentNullException(nameof(additions));
            Removals = removals ?? throw new ArgumentNullException(nameof(removals));

            RequireDistinct(baseline, additions, nameof(baseline), nameof(additions));
            RequireDistinct(baseline, removals, nameof(baseline), nameof(removals));
            RequireDistinct(additions, removals, nameof(additions), nameof(removals));

            _datasetClause = LayeredModelSparql.NamedDatasetClause(this);

            foreach (MethodInfo methodInfo in GetType().GetMethods())
            {
                if (methodInfo.Name == "GetResource" && methodInfo.IsGenericMethod)
                {
                    _getResourceMethod = methodInfo;
                    break;
                }
            }
        }

        private static void RequireDistinct(IModel a, IModel b, string nameA, string nameB)
        {
            if (a.Uri == null || b.Uri == null)
            {
                throw new ArgumentException("A layered model requires three models that each name a graph.");
            }

            if (a.Uri.Equals(b.Uri))
            {
                throw new ArgumentException(
                    $"The {nameA} and {nameB} models of a layered model must be different graphs, both are <{a.Uri}>.");
            }
        }

        #endregion

        #region Overlay query construction

        /// <summary>
        /// Wraps one triple pattern in the overlay.
        /// </summary>
        private string Overlay(string subject, string predicate, string @object)
        {
            return LayeredModelSparql.Overlay(this, subject, predicate, @object);
        }

        /// <summary>
        /// Builds a query and marks it as having the overlay already applied, which is what
        /// distinguishes Trinity's own overlay-aware reads from a caller's SPARQL.
        /// </summary>
        private static ISparqlQuery CreateOverlayQuery(string queryString)
        {
            return new SparqlQuery(queryString, declarePrefixes: false) { IsOverlayApplied = true };
        }

        /// <summary>
        /// Runs a query this class built. Bypasses the guard in
        /// <see cref="ExecuteQuery(ISparqlQuery, bool, ITransaction)"/> because the overlay is
        /// already in the query's graph patterns.
        /// </summary>
        private ISparqlQueryResult ExecuteOverlayQuery(ISparqlQuery query, ITransaction transaction)
        {
            query.Model = this;
            query.IsInferenceEnabled = false;

            return _store.ExecuteQuery(query, transaction);
        }

        /// <summary>
        /// The query every whole-resource read uses: all triples of the given subjects, resolved
        /// against the effective graph.
        /// </summary>
        /// <remarks>
        /// The subjects are bound with <c>VALUES</c> ahead of the overlay so the read is an
        /// indexed probe on every backend, and the projection stays <c>?s ?p ?o</c> in that order
        /// so <c>ISparqlQuery.ProvidesStatements()</c> — a token-level heuristic that demands
        /// exactly three same-ordered variables — keeps returning true. Without that, resource
        /// materialization refuses the query outright.
        /// </remarks>
        private ISparqlQuery CreateResourceQuery(IEnumerable<Uri> uris)
        {
            var queryString = new StringBuilder();

            queryString.Append("SELECT DISTINCT ?s ?p ?o ");
            queryString.Append(_datasetClause);
            queryString.Append("WHERE { ");
            queryString.Append(LayeredModelSparql.BindSubjects("?s", uris));
            queryString.Append(Overlay("?s", "?p", "?o"));
            queryString.Append(" }");

            return CreateOverlayQuery(queryString.ToString());
        }

        /// <summary>
        /// Rejects a subject that cannot appear in a SPARQL query.
        /// </summary>
        private static void RequireQueryableSubject(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (uri is UriRef uriRef && uriRef.IsBlankId)
            {
                throw new ArgumentException("Blank nodes are not supported as query subjects in SPARQL 1.1");
            }
        }

        #endregion

        #region Read methods

        /// <summary>
        /// Indicates whether a resource is visible through the view.
        /// </summary>
        /// <remarks>
        /// False for a resource whose every triple is staged for removal, even though the baseline
        /// still holds them.
        /// </remarks>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns><c>true</c> if the resource is part of the effective graph.</returns>
        public bool ContainsResource(Uri uri, ITransaction transaction = null)
        {
            RequireQueryableSubject(uri);

            ISparqlQuery query = CreateOverlayQuery("ASK " + _datasetClause + "{ " +
                Overlay(SparqlSerializer.SerializeUri(uri), "?p", "?o") + " }");

            return ExecuteOverlayQuery(query, transaction).GetAnwser();
        }

        /// <inheritdoc cref="ContainsResource(Uri, ITransaction)" />
        public bool ContainsResource(IResource resource, ITransaction transaction = null)
        {
            return ContainsResource(resource.Uri, transaction);
        }

        /// <summary>
        /// Retrieves a resource as it appears through the view.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all effective properties.</returns>
        public IResource GetResource(Uri uri, ITransaction transaction = null)
        {
            RequireQueryableSubject(uri);

            ISparqlQueryResult result = ExecuteOverlayQuery(CreateResourceQuery(new[] { uri }), transaction);

            Resource resource = result.GetResources().FirstOrDefault();

            if (resource == null)
            {
                throw new ResourceNotFoundException(uri);
            }

            Attach(resource);

            return resource;
        }

        /// <inheritdoc cref="GetResource(Uri, ITransaction)" />
        public IResource GetResource(IResource resource, ITransaction transaction = null)
        {
            return GetResource(resource.Uri, transaction);
        }

        /// <summary>
        /// Retrieves a resource of the given type as it appears through the view.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all effective properties.</returns>
        public T GetResource<T>(Uri uri, ITransaction transaction = null) where T : Resource
        {
            RequireQueryableSubject(uri);

            ISparqlQueryResult result = ExecuteOverlayQuery(CreateResourceQuery(new[] { uri }), transaction);

            T resource = result.GetResources<T>().FirstOrDefault();

            if (resource == null)
            {
                throw new ResourceNotFoundException(uri);
            }

            Attach(resource);

            return resource;
        }

        /// <inheritdoc cref="GetResource{T}(Uri, ITransaction)" />
        public T GetResource<T>(IResource resource, ITransaction transaction = null) where T : Resource
        {
            return GetResource<T>(resource.Uri, transaction);
        }

        /// <summary>
        /// Retrieves a resource of the given runtime type as it appears through the view.
        /// </summary>
        /// <param name="uri">A Uniform Resource Identifier.</param>
        /// <param name="type">The type the resource should have.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>A resource with all effective properties.</returns>
        public object GetResource(Uri uri, Type type, ITransaction transaction = null)
        {
            if (_getResourceMethod == null)
            {
                throw new InvalidOperationException("No handle to the generic method T GetResource<T>(Uri)");
            }

            if (!typeof(IResource).IsAssignableFrom(type))
            {
                throw new ArgumentException($"The given type {type} does not implement the IResource interface.");
            }

            MethodInfo getResource = _getResourceMethod.MakeGenericMethod(type);

            return getResource.Invoke(this, new object[] { uri, transaction });
        }

        /// <summary>
        /// Retrieves several resources of the given type as they appear through the view.
        /// </summary>
        /// <param name="uris">The Uniform Resource Identifiers to retrieve.</param>
        /// <param name="type">The type the resources should have.</param>
        /// <param name="transaction">Transaction associated with this action.</param>
        /// <returns>An enumeration of resources with all effective properties.</returns>
        public IEnumerable<object> GetResources(IEnumerable<Uri> uris, Type type, ITransaction transaction = null)
        {
            if (!typeof(IResource).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Error: The given type {type} does not implement the IResource interface.");
            }

            List<Uri> subjects = (uris ?? Enumerable.Empty<Uri>()).ToList();

            if (subjects.Count == 0)
            {
                return Enumerable.Empty<object>();
            }

            foreach (Uri uri in subjects)
            {
                RequireQueryableSubject(uri);
            }

            ISparqlQueryResult result = ExecuteOverlayQuery(CreateResourceQuery(subjects), transaction);

            return Materialize(result.GetResources(type));
        }

        /// <summary>
        /// Returns every resource of the given type that is visible through the view.
        /// </summary>
        /// <remarks>
        /// The type constraints are emitted <b>before</b> the wildcard triple pattern. Order is
        /// not cosmetic here: an engine that cannot reorder joins across the overlay's
        /// <c>UNION</c> evaluates the patterns as written, and putting the unselective wildcard
        /// first makes it materialize the whole effective graph before applying any constraint.
        /// </remarks>
        /// <typeparam name="T">The type of the resources.</typeparam>
        /// <param name="inferenceEnabled">Must be <c>false</c>; inferencing is not supported.</param>
        /// <param name="transaction">Transaction associated with the action.</param>
        /// <returns>An enumeration of resources visible through the view.</returns>
        public IEnumerable<T> GetResources<T>(bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            RequireNoInferencing(inferenceEnabled);

            var instance = (T)Activator.CreateInstance(typeof(T), new UriRef("urn:"));

            var queryString = new StringBuilder();

            queryString.Append("SELECT DISTINCT ?s ?p ?o ");
            queryString.Append(_datasetClause);
            queryString.Append("WHERE { ");

            foreach (Class type in instance.GetTypes())
            {
                queryString.Append(Overlay("?s", "a", SparqlSerializer.SerializeUri(type.Uri)));
                queryString.Append(' ');
            }

            queryString.Append(Overlay("?s", "?p", "?o"));
            queryString.Append(" }");

            ISparqlQueryResult result = ExecuteOverlayQuery(
                CreateOverlayQuery(queryString.ToString()), transaction);

            return Materialize(result.GetResources<T>());
        }

        /// <summary>
        /// Returns a LINQ query source over the view.
        /// </summary>
        /// <remarks>
        /// Every triple pattern the provider emits is wrapped in the overlay, so a <c>Where</c>
        /// clause matches against the effective graph rather than the raw baseline — a resource
        /// whose only matching value is staged for removal does not match.
        /// </remarks>
        /// <typeparam name="T">The type of the resources.</typeparam>
        /// <param name="inferenceEnabled">Must be <c>false</c>; inferencing is not supported.</param>
        public IQueryable<T> AsQueryable<T>(bool inferenceEnabled = false) where T : Resource
        {
            RequireNoInferencing(inferenceEnabled);

            return new Query.Sparql.TrinityQueryable<T>(new Query.Sparql.SparqlQueryProvider(this, false));
        }

        /// <summary>
        /// Attaches a freshly read resource to this view and marks it read-only.
        /// </summary>
        private void Attach(Resource resource)
        {
            resource.IsNew = false;
            resource.IsSynchronized = true;
            resource.IsReadOnly = true;
            resource.SetModel(this);
        }

        private IEnumerable<T> Materialize<T>(IEnumerable<T> resources) where T : Resource
        {
            if (resources == null)
            {
                yield break;
            }

            foreach (T resource in resources)
            {
                // A missing rdf:type yields a null entry; see the same guard in ModelGroup.
                if (resource == null)
                {
                    continue;
                }

                Attach(resource);

                yield return resource;
            }
        }

        private IEnumerable<object> Materialize(IEnumerable<Resource> resources)
        {
            return Materialize<Resource>(resources).Cast<object>();
        }

        #endregion

        #region Query execution: caller queries are rewritten, or refused

        /// <summary>
        /// Runs a query against the view, rewriting it so every triple pattern resolves against the
        /// effective graph.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Queries Trinity built itself already carry the overlay and run as they are. A
        /// caller-supplied query is rewritten by <see cref="OverlayQueryRewriter"/>, which works on
        /// the parse tree and accepts only the forms it can prove it handles. A form it cannot —
        /// property paths, explicit <c>GRAPH</c> blocks, <c>SERVICE</c>, <c>CONSTRUCT</c>,
        /// <c>DESCRIBE</c>, <c>FILTER EXISTS</c> — throws with the reason.
        /// </para>
        /// <para>
        /// Refusing matters because running such a query unchanged is wrong in two different ways
        /// depending on its shape, and neither announces itself: a pattern against the default graph
        /// returns nothing at all (the view names its graphs with <c>FROM NAMED</c>, leaving the
        /// default graph empty), while a pattern naming a graph returns triples staged for removal.
        /// </para>
        /// </remarks>
        /// <exception cref="NotSupportedException">
        /// Thrown if the query cannot be rewritten faithfully, or if inferencing is requested.
        /// </exception>
        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            RequireNoInferencing(inferenceEnabled);

            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query is SparqlQuery sparqlQuery && sparqlQuery.IsOverlayApplied)
            {
                return ExecuteOverlayQuery(query, transaction);
            }

            // ToString() substitutes the @parameters, which the strict SPARQL parser the rewriter
            // uses would otherwise reject. The caller's query object is left untouched.
            ISparqlQuery rewritten = CreateOverlayQuery(OverlayQueryRewriter.Rewrite(this, query.ToString()));

            return ExecuteOverlayQuery(rewritten, transaction);
        }

        /// <summary>
        /// Materializes the resources selected by a query, rewritten to honour the overlay. See
        /// <see cref="ExecuteQuery(ISparqlQuery, bool, ITransaction)" /> for which forms are accepted.
        /// </summary>
        public IEnumerable<Resource> GetResources(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            return Materialize<Resource>(ExecuteQuery(query, inferenceEnabled, transaction).GetResources<Resource>());
        }

        /// <summary>
        /// Materializes the resources selected by a query, rewritten to honour the overlay. See
        /// <see cref="ExecuteQuery(ISparqlQuery, bool, ITransaction)" /> for which forms are accepted.
        /// </summary>
        /// <remarks>
        /// This is also the entry point the LINQ provider materializes through; the queries it builds
        /// already carry the overlay and pass straight through.
        /// </remarks>
        public IEnumerable<T> GetResources<T>(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            return Materialize(ExecuteQuery(query, inferenceEnabled, transaction).GetResources<T>());
        }

        /// <summary>
        /// Returns the bindings of a query, rewritten to honour the overlay. See
        /// <see cref="ExecuteQuery(ISparqlQuery, bool, ITransaction)" /> for which forms are accepted.
        /// </summary>
        public IEnumerable<BindingSet> GetBindings(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            return ExecuteQuery(query, inferenceEnabled, transaction).GetBindings();
        }

        /// <summary>
        /// Refuses an inference-enabled read.
        /// </summary>
        /// <remarks>
        /// Every store's inference path defeats the overlay. GraphDB rebuilds the query's dataset
        /// as a plain model group, which is union-only; Virtuoso answers inference-enabled
        /// resource reads with a bare <c>DESCRIBE</c>, which cannot carry a guard. Beyond the
        /// mechanics, entailment over a subtracted graph is not something any of the store
        /// reasoners defines. So this is refused rather than approximated.
        /// </remarks>
        internal static void RequireNoInferencing(bool inferenceEnabled)
        {
            if (inferenceEnabled)
            {
                throw new NotSupportedException(
                    "Inferencing is not supported on a layered model. The stores' inference paths cannot " +
                    "carry the baseline/additions/removals overlay — GraphDB rebuilds the dataset as a " +
                    "union and Virtuoso falls back to a bare DESCRIBE — so the result would silently " +
                    "include triples staged for removal.");
            }
        }

        #endregion

        #region Not supported: the view is read-only

        private static NotSupportedException ReadOnly()
        {
            return new NotSupportedException(
                "Layered models are read-only. Stage a change by writing to the Additions and Removals " +
                "models, which are ordinary models, and the view will read the result.");
        }

        /// <summary>Not supported. Layered models are read-only.</summary>
        public IResource AddResource(IResource resource, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public T AddResource<T>(T resource, ITransaction transaction = null) where T : Resource => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public IResource CreateResource(string format = "urn:uuid:{0}", ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public IResource CreateResource(Uri uri, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public T CreateResource<T>(string format = "urn:uuid:{0}", ITransaction transaction = null) where T : Resource => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public T CreateResource<T>(Uri uri, ITransaction transaction = null) where T : Resource => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public object CreateResource(Type type, string format = "urn:uuid:{0}", ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public object CreateResource(Uri uri, Type t, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void DeleteResource(Uri uri, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void DeleteResource(IResource resource, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void DeleteResources(IEnumerable<Uri> uris, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void DeleteResources(ITransaction transaction = null, params IResource[] resources) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void UpdateResource(Resource resource, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void UpdateResources(IEnumerable<Resource> resources, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void UpdateResources(ITransaction transaction = null, params Resource[] resources) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void ExecuteUpdate(ISparqlUpdate update, ITransaction transaction = null) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public void Clear() => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public bool Read(Uri url, RdfSerializationFormat format, bool update) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public bool Read(Stream stream, RdfSerializationFormat format, bool update) => throw ReadOnly();

        /// <summary>Not supported. Layered models are read-only.</summary>
        public bool Read(string content, RdfSerializationFormat format, bool update) => throw ReadOnly();

        /// <summary>
        /// Not supported. Serializing the view would mean materializing the effective graph, which
        /// is a different operation from writing a model out; write the layer models instead.
        /// </summary>
        public void Write(Stream stream, RdfSerializationFormat format, INamespaceMap namespaces = null, Uri baseUri = null, bool leaveOpen = false) => throw ReadOnly();

        /// <inheritdoc cref="Write(Stream, RdfSerializationFormat, INamespaceMap, Uri, bool)" />
        public void Write(Stream stream, IRdfWriter formatWriter, bool leaveOpen = false) => throw ReadOnly();

        /// <summary>
        /// Not supported. A layered view spans three graphs and never writes, so there is nothing
        /// for a transaction to scope.
        /// </summary>
        public ITransaction BeginTransaction(IsolationLevel isolationLevel) => throw ReadOnly();

        #endregion
    }
}
