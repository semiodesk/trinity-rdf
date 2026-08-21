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
        /// <summary>
        /// Selects the graph(s) an authored read resolves patterns against: the three layers in
        /// rewriting mode, the single effective graph when materialized.
        /// </summary>
        /// <remarks>
        /// Named for what it selects, not for where it goes, because it is only correct for a query
        /// whose patterns are wrapped in <see cref="Overlay(string, string, string)"/>. A query that
        /// reasons over the <i>layers</i> - <see cref="HasDiverged"/> is the one - must use
        /// <see cref="LayeredModelSparql.NamedDatasetClause"/> instead: in materialized mode this
        /// clause is a bare <c>FROM</c>, which leaves the named-graph set empty, so a
        /// <c>GRAPH &lt;removals&gt;</c> block against it can never match.
        /// </remarks>
        private readonly string _effectiveDatasetClause;

        /// <inheritdoc />
        public IModel Baseline { get; }

        /// <inheritdoc />
        public IModel Additions { get; }

        /// <inheritdoc />
        public IModel Removals { get; }

        /// <inheritdoc />
        public IModel Materialized { get; }

        /// <inheritdoc />
        public bool IsMaterialized => Materialized != null;

        /// <summary>
        /// Set when <see cref="VerifyMaterialized"/> found the materialized graph short. Latched,
        /// because the store already committed the truncated write and there is nothing to roll back:
        /// without it the view throws once from <see cref="Refresh"/> and every later read quietly
        /// answers from a graph known to be incomplete.
        /// </summary>
        private bool _materializationFailed;

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
                    "ASK " + _effectiveDatasetClause + "{ " + Overlay("?s", "?p", "?o") + " }");

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
        /// <param name="materialized">
        /// Optional fourth graph holding the effective triples. When given, reads run natively against
        /// it instead of through the overlay.
        /// </param>
        internal LayeredModel(IStore store, IModel baseline, IModel additions, IModel removals, IModel materialized = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
            Additions = additions ?? throw new ArgumentNullException(nameof(additions));
            Removals = removals ?? throw new ArgumentNullException(nameof(removals));
            Materialized = materialized;

            RequireDistinct(baseline, additions, nameof(baseline), nameof(additions));
            RequireDistinct(baseline, removals, nameof(baseline), nameof(removals));
            RequireDistinct(additions, removals, nameof(additions), nameof(removals));

            if (materialized != null)
            {
                RequireDistinct(baseline, materialized, nameof(baseline), nameof(materialized));
                RequireDistinct(additions, materialized, nameof(additions), nameof(materialized));
                RequireDistinct(removals, materialized, nameof(removals), nameof(materialized));
            }

            // The two seams that make the mode switch cheap: every authored read composes its query
            // from this clause and from Overlay() below, so pointing both at one ordinary graph is all
            // it takes for the same templates to run natively against the effective triples.
            _effectiveDatasetClause = materialized == null
                ? LayeredModelSparql.NamedDatasetClause(this)
                : "FROM " + SparqlSerializer.SerializeUri(materialized.Uri) + " ";

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
        /// One triple pattern, resolved against the effective graph.
        /// </summary>
        /// <remarks>
        /// In rewriting mode that means the overlay. In materialized mode the effective graph already
        /// exists as an ordinary graph, so the pattern is just a pattern — which is the whole point of
        /// the mode: no overlay to apply means nothing to refuse.
        /// </remarks>
        private string Overlay(string subject, string predicate, string @object)
        {
            return IsMaterialized
                ? string.Concat(subject, " ", predicate, " ", @object, " .")
                : LayeredModelSparql.Overlay(this, subject, predicate, @object);
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
        private ISparqlQueryResult ExecuteOverlayQuery(ISparqlQuery query, ITransaction transaction, bool inferenceEnabled = false)
        {
            RequireUsableMaterialization();

            query.Model = this;

            // Only meaningful when materialized: the store is then reasoning over one ordinary graph.
            query.IsInferenceEnabled = inferenceEnabled && IsMaterialized;

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
            queryString.Append(_effectiveDatasetClause);
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

            ISparqlQuery query = CreateOverlayQuery("ASK " + _effectiveDatasetClause + "{ " +
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
            queryString.Append(_effectiveDatasetClause);
            queryString.Append("WHERE { ");

            foreach (Class type in instance.GetTypes())
            {
                queryString.Append(Overlay("?s", "a", SparqlSerializer.SerializeUri(type.Uri)));
                queryString.Append(' ');
            }

            queryString.Append(Overlay("?s", "?p", "?o"));
            queryString.Append(" }");

            ISparqlQueryResult result = ExecuteOverlayQuery(
                CreateOverlayQuery(queryString.ToString()), transaction, inferenceEnabled);

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

            return new Query.Sparql.TrinityQueryable<T>(
                new Query.Sparql.SparqlQueryProvider(this, inferenceEnabled && IsMaterialized));
        }

        /// <summary>
        /// Attaches a freshly read resource to this view and marks it read-only.
        /// </summary>
        private void Attach(Resource resource)
        {
            resource.IsNew = false;

            // Capturing the snapshot: IsSynchronized's setter calls CapturePersistedValues, so the
            // effective state as just read becomes the baseline the staged delta is computed against.
            resource.IsSynchronized = true;

            // Deliberately NOT read-only. Resource.Commit() is guarded by IsReadOnly, so setting it
            // would make a caller's Commit() a silent no-op; instead it routes into UpdateResource
            // below and stages.
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

            // Materialized: the effective graph is an ordinary graph, so a caller's query runs against
            // it unchanged. Every refusal that exists for want of a faithful rewrite is lifted -
            // property paths, CONSTRUCT, DESCRIBE and inferencing all simply work.
            //
            // Graph selection is the exception, and stays refused in both modes. It is not a rewriting
            // limitation: the view names the effective graph in its own dataset clause, and a caller
            // FROM is appended beside it rather than replacing it, so the query would read the union of
            // the two - including triples staged for removal. A GRAPH block reaches for the layers the
            // view exists to combine, for the same reason.
            if (IsMaterialized)
            {
                // The view's own graph is passed in, not merely "refuse any dataset clause": assigning
                // ISparqlQuery.Model injects FROM <effective>, so ToString() already carries it.
                OverlayQueryRewriter.RequireNoGraphSelection(query.ToString(), Materialized.Uri);

                return ExecuteOverlayQuery(query, transaction, inferenceEnabled);
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
        internal void RequireNoInferencing(bool inferenceEnabled)
        {
            RequireNoInferencing(this, inferenceEnabled);
        }

        /// <inheritdoc cref="RequireNoInferencing(bool)" />
        /// <remarks>
        /// Static and interface-typed so any <see cref="ILayeredModel"/> implementation is held to it,
        /// not just this class. <see cref="ILayeredModel.IsMaterialized"/> is all the check needs.
        /// </remarks>
        internal static void RequireNoInferencing(ILayeredModel model, bool inferenceEnabled)
        {
            // A materialized view can be reasoned over: it is one ordinary graph, and the store's
            // inference path has nothing to defeat.
            if (inferenceEnabled && !model.IsMaterialized)
            {
                throw new NotSupportedException(
                    "Inferencing is not supported on a layered model. The stores' inference paths cannot " +
                    "carry the baseline/additions/removals overlay — GraphDB rebuilds the dataset as a " +
                    "union and Virtuoso falls back to a bare DESCRIBE — so the result would silently " +
                    "include triples staged for removal.");
            }
        }

        #endregion

        #region Staged writes

        /// <summary>
        /// Stages a resource's changes into the additions and removals graphs.
        /// </summary>
        /// <remarks>
        /// This is what <see cref="Resource.Commit"/> routes into for a resource read through a view,
        /// so staging needs no separate API: committing a resource whose model is a view <i>means</i>
        /// staging it.
        /// </remarks>
        public void UpdateResource(Resource resource, ITransaction transaction = null)
        {
            UpdateResources(new[] { resource }, transaction);
        }

        /// <inheritdoc cref="UpdateResource(Resource, ITransaction)" />
        public void UpdateResources(IEnumerable<Resource> resources, ITransaction transaction = null)
        {
            var staged = new List<Resource>();
            var affected = new List<string>();
            string update = BuildStagingUpdate(resources, staged, affected);

            if (update != null)
            {
                _store.ExecuteNonQuery(new SparqlUpdate(update), transaction);

                // The view owns this change, so it can keep the materialized graph in step for exactly
                // the triples it touched. That is the whole reason staging belongs to the view.
                SynchronizeMaterialized(affected, transaction);
            }

            foreach (Resource resource in staged)
            {
                resource.IsNew = false;

                // Re-snapshot, so a second Commit stages only what changed since this one.
                resource.IsSynchronized = true;
            }
        }

        /// <inheritdoc cref="UpdateResource(Resource, ITransaction)" />
        public void UpdateResources(ITransaction transaction = null, params Resource[] resources)
        {
            UpdateResources(resources, transaction);
        }

        /// <summary>
        /// Builds the update that stages every given resource's delta, or <c>null</c> when nothing
        /// changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Routing is conditional, and this is the crux of staging rather than a detail of it. Sending
        /// every deleted value straight to the removals graph is wrong: stage <c>name = "new"</c>, then
        /// change it to <c>"newer"</c>, and additions would hold both while removals held <c>"new"</c> -
        /// and additions win, so <b>both</b> values would stay visible.
        /// </para>
        /// <para>
        /// So for each value the delta reports as <b>inserted</b>: drop it from removals (which restores
        /// it from the baseline), then add it to additions only if the baseline does not already have
        /// it. And for each value reported as <b>deleted</b>: drop it from additions, then add it to
        /// removals only if the baseline has it.
        /// </para>
        /// <para>
        /// Those guards are also what maintain <c>additions ∩ baseline = ∅</c> and
        /// <c>removals ⊆ baseline</c> — the invariants that keep the pre-change baseline reconstructible
        /// as <c>(effective ∖ additions) ∪ removals</c>. See ADR-0042.
        /// </para>
        /// </remarks>
        private string BuildStagingUpdate(IEnumerable<Resource> resources, List<Resource> staged, List<string> affected)
        {
            if (resources == null)
            {
                return null;
            }

            var inserted = new List<string>();
            var deleted = new List<string>();

            foreach (Resource resource in resources)
            {
                if (resource == null)
                {
                    continue;
                }

                RequireQueryableSubject(resource.Uri);
                staged.Add(resource);

                string subject = SparqlSerializer.SerializeUri(resource.Uri);

                if (SparqlSerializer.TrySerializeResourceDelta(
                        resource, IgnoreUnmappedProperties, out var removedValues, out var addedValues))
                {
                    foreach (string value in addedValues) inserted.Add(subject + " " + value);
                    foreach (string value in removedValues) deleted.Add(subject + " " + value);
                }
                else
                {
                    // No snapshot to diff against: the resource has never been synchronized, so
                    // everything it holds is an addition.
                    foreach (string value in SparqlSerializer.SerializeValueSet(resource, IgnoreUnmappedProperties))
                    {
                        inserted.Add(subject + " " + value);
                    }
                }
            }

            if (inserted.Count == 0 && deleted.Count == 0)
            {
                return null;
            }

            affected.AddRange(inserted);
            affected.AddRange(deleted);

            var operations = new List<string>();

            foreach (string triple in inserted)
            {
                // Un-stage a removal first: if that restored the value from the baseline, the third
                // operation then takes it back out of additions again.
                operations.Add(Delete(Removals, triple));

                // Add, then retract if the baseline already has it - rather than the more obvious
                // "insert unless the baseline has it". Virtuoso *ignores a WHERE clause consisting
                // only of a FILTER*, so `INSERT ... WHERE { FILTER NOT EXISTS { ... } }` inserts
                // unconditionally there, leaving a value in additions that the baseline already holds
                // and breaking the additions-disjoint-from-baseline invariant. Every operation here
                // carries a real pattern instead, which all three stores evaluate correctly.
                operations.Add(string.Format("INSERT DATA {{ GRAPH {0} {{ {1} }} }}", Graph(Additions), triple));
                operations.Add(string.Format("DELETE {{ GRAPH {0} {{ {1} }} }} WHERE {{ GRAPH {2} {{ {1} }} }}",
                    Graph(Additions), triple, Graph(Baseline)));
            }

            foreach (string triple in deleted)
            {
                operations.Add(Delete(Additions, triple));
                operations.Add(string.Format("INSERT {{ GRAPH {0} {{ {1} }} }} WHERE {{ GRAPH {2} {{ {1} }} }}",
                    Graph(Removals), triple, Graph(Baseline)));
            }

            return string.Join("; ", operations);
        }

        /// <summary>
        /// A guarded delete, used in preference to <c>DELETE DATA</c> so a graph that does not yet
        /// exist is a no-op rather than an error on stores that are strict about it.
        /// </summary>
        private static string Delete(IModel model, string triple)
        {
            return string.Format("DELETE WHERE {{ GRAPH {0} {{ {1} }} }}", Graph(model), triple);
        }

        private static string Graph(IModel model)
        {
            return SparqlSerializer.SerializeUri(model.Uri);
        }

        #endregion

        #region Materialization

        /// <summary>
        /// Rebuilds the materialized graph from the three layers.
        /// </summary>
        /// <remarks>
        /// O(baseline), so this is the expensive operation the mode exists to avoid paying repeatedly.
        /// It is needed when the view is first materialized, and whenever something changed a layer or
        /// the baseline behind the view's back — see the staleness note on
        /// <see cref="ILayeredModel.Materialized"/>.
        /// </remarks>
        public void Refresh(ITransaction transaction = null)
        {
            RequireMaterialized();

            string update = string.Join("; ",
                string.Format("DELETE WHERE {{ GRAPH {0} {{ ?s ?p ?o }} }}", Graph(Materialized)),
                string.Format("INSERT {{ GRAPH {0} {{ ?s ?p ?o }} }} WHERE {{ {1} }}",
                    Graph(Materialized), LayeredModelSparql.Overlay(this, "?s", "?p", "?o")));

            // Optimistic: the rebuild is the recovery path, so a view latched as failed has to be
            // able to run the verification queries that would clear it.
            _materializationFailed = false;

            RunAtomically(update, transaction);

            VerifyMaterialized();
        }

        /// <summary>
        /// Confirms the rebuild actually wrote what the overlay says the effective graph contains.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not defensive padding. <b>Virtuoso silently inserts nothing</b> when a single
        /// <c>INSERT … WHERE</c> exceeds its transaction log limit: measured, the same statement
        /// succeeds at 500,000 rows and writes zero at 1,000,000, reporting success either way. An
        /// empty materialized graph is indistinguishable from an empty baseline, so a caller would read
        /// a view that says the data does not exist.
        /// </para>
        /// <para>
        /// Counting the overlay's solutions is O(baseline), which is proportionate for an operation
        /// that is already O(baseline) and happens once. The two counts must match exactly, because the
        /// overlay's branches are disjoint by construction and so each effective triple yields exactly
        /// one solution.
        /// </para>
        /// </remarks>
        private void VerifyMaterialized()
        {
            long expected = CountOf(string.Format("SELECT (COUNT(*) AS ?n) {0}WHERE {{ {1} }}",
                LayeredModelSparql.NamedDatasetClause(this),
                LayeredModelSparql.Overlay(this, "?s", "?p", "?o")));

            long actual = CountOf(string.Format("SELECT (COUNT(*) AS ?n) FROM {0} WHERE {{ ?s ?p ?o }}",
                Graph(Materialized)));

            if (expected == actual)
            {
                return;
            }

            // Latch before throwing. RunAtomically already committed the short graph, so every read
            // from here on would be answered from it; only another Refresh can clear this.
            _materializationFailed = true;

            throw new InvalidOperationException(
                $"Materializing this view wrote {actual:N0} triples where the overlay says there are " +
                $"{expected:N0}. The store accepted the update and reported success, so this is a limit " +
                "it did not report rather than a malformed request - Virtuoso, for one, silently writes " +
                "nothing when a single INSERT ... WHERE exceeds its transaction log limit, which it does " +
                "somewhere between 500,000 and 1,000,000 rows. An empty or short materialized graph " +
                "cannot be told apart from a small baseline, so it is reported rather than served. " +
                "Either use the rewriting mode for a baseline this size, or raise the store's limit - on " +
                "Virtuoso, log_enable(3,1) disables transaction logging for bulk updates.");
        }

        private long CountOf(string sparql)
        {
            var bindings = ExecuteOverlayQuery(CreateOverlayQuery(sparql), null).GetBindings().FirstOrDefault();

            return bindings == null || !bindings.Any() ? 0 : Convert.ToInt64(bindings.First().Value);
        }

        /// <summary>
        /// Brings the materialized graph back in step for a known set of triples.
        /// </summary>
        /// <remarks>
        /// <para>
        /// O(changes) rather than O(baseline), which is what makes the mode practical: the full rebuild
        /// is paid once, and every subsequent stage costs only the triples it touched.
        /// </para>
        /// <para>
        /// Each triple is deleted and then re-inserted <i>if the overlay says it belongs</i>, rather
        /// than by working out from the delta what should have happened to it. Asking the overlay is
        /// correct by construction and cannot drift from the read path; a hand-derived rule for each of
        /// the four staging cases could.
        /// </para>
        /// </remarks>
        private void SynchronizeMaterialized(IEnumerable<string> triples, ITransaction transaction)
        {
            if (!IsMaterialized)
            {
                return;
            }

            var operations = new List<string>();

            foreach (string triple in triples)
            {
                operations.Add(string.Format("DELETE WHERE {{ GRAPH {0} {{ {1} }} }}", Graph(Materialized), triple));
                operations.Add(string.Format("INSERT {{ GRAPH {0} {{ {1} }} }} WHERE {{ {2} }}",
                    Graph(Materialized), triple, LayeredModelSparql.Overlay(this, triple)));
            }

            if (operations.Count > 0)
            {
                RunAtomically(string.Join("; ", operations), transaction);
            }
        }

        /// <summary>
        /// Brings the materialized graph back in step for every triple mentioning a resource, on either
        /// the subject or the object side.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Asks the overlay what should remain, exactly as the ground-triple sibling does, rather than
        /// deriving the outcome from the operation. For a delete the overlay happens to hold nothing for
        /// the resource afterwards, but hard-coding that would be a rule that can drift from the read
        /// path, and this cannot.
        /// </para>
        /// <para>
        /// <b>Two bound patterns rather than one filtered scan</b>, which is the difference between
        /// O(changes) and O(baseline). <c>?s ?p ?o</c> with
        /// <c>FILTER (?s = &lt;r&gt; || ?o = &lt;r&gt;)</c> cannot be answered from an index at all - the
        /// engine enumerates every triple and tests each one - and where the filter sits makes almost no
        /// difference. Measured on a 1,000,000-triple in-memory baseline: 3,891 ms with the filter
        /// outside the union, 3,695 ms interpolated into both branches, and <b>0-3 ms</b> as two bound
        /// patterns. See ADR-0042.
        /// </para>
        /// </remarks>
        private void SynchronizeMaterializedFor(string subject, ITransaction transaction)
        {
            if (!IsMaterialized)
            {
                return;
            }

            var operations = new List<string>();

            // Subject side, then object side (ADR-0030: a delete is broad by design). A self-referring
            // triple matches both, which is harmless - a graph is a set, so the insert and the delete
            // are both idempotent.
            foreach ((string s, string o) in new[] { (subject, "?o"), ("?s", subject) })
            {
                string triple = string.Concat(s, " ?p ", o);

                operations.Add(string.Format("DELETE WHERE {{ GRAPH {0} {{ {1} }} }}", Graph(Materialized), triple));
                operations.Add(string.Format("INSERT {{ GRAPH {0} {{ {1} }} }} WHERE {{ {2} }}",
                    Graph(Materialized), triple, LayeredModelSparql.Overlay(this, s, "?p", o)));
            }

            RunAtomically(string.Join("; ", operations), transaction);
        }

        /// <summary>
        /// Refuses to answer from a materialized graph that is known to be incomplete.
        /// </summary>
        private void RequireUsableMaterialization()
        {
            if (!_materializationFailed)
            {
                return;
            }

            throw new InvalidOperationException(
                "This view's materialized graph is known to be incomplete: a previous rebuild wrote " +
                "fewer triples than the overlay contains and the store reported success anyway. Reading " +
                "from it would silently omit data, so it is refused until Refresh() succeeds. See the " +
                "exception from that rebuild for the store limit involved.");
        }

        private void RequireMaterialized()
        {
            if (!IsMaterialized)
            {
                throw new NotSupportedException(
                    "This view is not materialized. Create one with the materialized graph parameter of " +
                    "IStore.CreateLayeredModel to get a fourth graph holding the effective triples, which " +
                    "lets queries run natively against it.");
            }
        }

        #endregion

        #region Accept and discard

        /// <summary>
        /// Applies the staged change to the baseline and empties both layers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Deliberately not called <c>Commit</c>: a view owns staging, so
        /// <see cref="Resource.Commit"/> already means "stage", and reusing the word one level up for
        /// "push everything to the baseline" would be a trap.
        /// </para>
        /// <para>
        /// Removals are applied before additions, so a triple in both survives — the same precedence
        /// the read path gives it.
        /// </para>
        /// <para>
        /// Throws if the baseline has moved on ground the change depends on, unless
        /// <paramref name="force"/> is set. That check matters because applying a stale changeset never
        /// fails by itself: a changeset is a set of triples and INSERT/DELETE are idempotent, so a
        /// competing change to a single-valued property would silently leave two values behind.
        /// </para>
        /// </remarks>
        /// <param name="force">Apply even if the baseline diverged.</param>
        /// <param name="transaction">
        /// Transaction to run in. When omitted one is started: real on Virtuoso, a no-op elsewhere,
        /// where a multi-operation request is atomic in its own right (measured — see ADR-0042).
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown when the baseline diverged.</exception>
        public void Accept(bool force = false, ITransaction transaction = null)
        {
            if (!force && HasDiverged())
            {
                throw new InvalidOperationException(
                    "The baseline has changed since this change was staged: at least one triple staged for " +
                    "removal is no longer present in it, so the change was computed against a state that no " +
                    "longer holds. Applying it anyway would not fail - a changeset is a set of triples and " +
                    "INSERT/DELETE are idempotent - it would silently merge, which for a single-valued " +
                    "property leaves two values behind. Re-read and re-stage, or pass force to apply anyway.");
            }

            string update = string.Join("; ",
                string.Format("DELETE {{ GRAPH {0} {{ ?s ?p ?o }} }} WHERE {{ GRAPH {1} {{ ?s ?p ?o }} }}",
                    Graph(Baseline), Graph(Removals)),
                string.Format("INSERT {{ GRAPH {0} {{ ?s ?p ?o }} }} WHERE {{ GRAPH {1} {{ ?s ?p ?o }} }}",
                    Graph(Baseline), Graph(Additions)),
                string.Format("DELETE WHERE {{ GRAPH {0} {{ ?s ?p ?o }} }}", Graph(Additions)),
                string.Format("DELETE WHERE {{ GRAPH {0} {{ ?s ?p ?o }} }}", Graph(Removals)));

            RunAtomically(update, transaction);

            // After a clean accept the baseline holds exactly what the materialized graph already held,
            // so it needs no rebuild. After a forced one it does not: the change was applied over a
            // baseline that had moved, so the effective graph is no longer what was materialized.
            if (force && IsMaterialized)
            {
                Refresh(transaction);
            }
        }

        /// <summary>
        /// Abandons the staged change, leaving the baseline untouched.
        /// </summary>
        public void Discard(ITransaction transaction = null)
        {
            string update = string.Join("; ",
                string.Format("DELETE WHERE {{ GRAPH {0} {{ ?s ?p ?o }} }}", Graph(Additions)),
                string.Format("DELETE WHERE {{ GRAPH {0} {{ ?s ?p ?o }} }}", Graph(Removals)));

            RunAtomically(update, transaction);

            // Discard drops the layers, so the effective graph reverts to the baseline - which is not
            // what was materialized. Unlike accept, this always needs the rebuild.
            if (IsMaterialized)
            {
                Refresh(transaction);
            }
        }

        /// <summary>
        /// Indicates whether the baseline has moved on ground the staged change depends on: a triple
        /// staged for removal that the baseline no longer holds.
        /// </summary>
        /// <remarks>
        /// O(removals) rather than O(baseline), and deliberately conservative — it also reports the
        /// benign case where a third party already made the same removal. A version control system
        /// complains in the analogous situation, where the context a patch assumes no longer matches.
        /// </remarks>
        public bool HasDiverged()
        {
            // The layers, never the effective graph: this asks whether the baseline still holds what
            // the changeset assumes, which is a question about the layers themselves. In materialized
            // mode _effectiveDatasetClause is a bare FROM with no named graphs, so both GRAPH blocks
            // below would match nothing and the guard would silently answer "no divergence".
            ISparqlQuery query = CreateOverlayQuery(string.Format(
                "ASK {0}{{ GRAPH {1} {{ ?s ?p ?o }} FILTER NOT EXISTS {{ GRAPH {2} {{ ?s ?p ?o }} }} }}",
                LayeredModelSparql.NamedDatasetClause(this), Graph(Removals), Graph(Baseline)));

            return ExecuteOverlayQuery(query, null).GetAnwser();
        }

        /// <summary>
        /// Runs a multi-operation update, in a real transaction where the store has one.
        /// </summary>
        private void RunAtomically(string update, ITransaction transaction)
        {
            if (transaction != null)
            {
                _store.ExecuteNonQuery(new SparqlUpdate(update), transaction);

                return;
            }

            ITransaction own = _store.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                _store.ExecuteNonQuery(new SparqlUpdate(update), own);
                own.Commit();
            }
            catch
            {
                own.Rollback();

                throw;
            }
        }

        #endregion

        #region Creating and deleting resources

        /// <inheritdoc cref="UpdateResource(Resource, ITransaction)" />
        public IResource AddResource(IResource resource, ITransaction transaction = null)
        {
            return AddResource<Resource>((Resource)resource, transaction);
        }

        /// <inheritdoc cref="UpdateResource(Resource, ITransaction)" />
        public T AddResource<T>(T resource, ITransaction transaction = null) where T : Resource
        {
            T staged = CreateResource<T>(resource.Uri, transaction);

            foreach (var value in resource.ListValues())
            {
                staged.AddPropertyToMapping(value.Item1, value.Item2, true);
            }

            staged.Commit();

            return staged;
        }

        /// <summary>Creates a resource whose triples will be staged as additions when committed.</summary>
        public IResource CreateResource(string format = "urn:uuid:{0}", ITransaction transaction = null)
        {
            return CreateResource<Resource>(format, transaction);
        }

        /// <inheritdoc cref="CreateResource(string, ITransaction)" />
        public IResource CreateResource(Uri uri, ITransaction transaction = null)
        {
            return CreateResource<Resource>(uri, transaction);
        }

        /// <inheritdoc cref="CreateResource(string, ITransaction)" />
        public T CreateResource<T>(string format = "urn:uuid:{0}", ITransaction transaction = null) where T : Resource
        {
            return CreateResource<T>(UriRef.GetGuid(format), transaction);
        }

        /// <inheritdoc cref="CreateResource(string, ITransaction)" />
        public T CreateResource<T>(Uri uri, ITransaction transaction = null) where T : Resource
        {
            return (T)CreateResource(uri, typeof(T), transaction);
        }

        /// <inheritdoc cref="CreateResource(string, ITransaction)" />
        public object CreateResource(Type type, string format = "urn:uuid:{0}", ITransaction transaction = null)
        {
            return CreateResource(UriRef.GetGuid(format), type, transaction);
        }

        /// <inheritdoc cref="CreateResource(string, ITransaction)" />
        public object CreateResource(Uri uri, Type type, ITransaction transaction = null)
        {
            RequireQueryableSubject(uri);

            if (!typeof(Resource).IsAssignableFrom(type))
            {
                throw new ArgumentException($"The given type {type} does not derive from Resource.");
            }

            var resource = (Resource)Activator.CreateInstance(type, uri);

            resource.IsNew = true;
            resource.SetModel(this);

            return resource;
        }

        /// <summary>
        /// Stages the removal of every triple the view shows for this resource.
        /// </summary>
        /// <remarks>
        /// Broad by design, as elsewhere in Trinity (ADR-0030): triples where the resource is the
        /// object are staged for removal too, so a deleted resource leaves no dangling references
        /// through the view.
        /// </remarks>
        public void DeleteResource(Uri uri, ITransaction transaction = null)
        {
            RequireQueryableSubject(uri);

            string subject = SparqlSerializer.SerializeUri(uri);
            var operations = new List<string>();

            // Bound patterns, not one scan filtered to ?s = <r> || ?o = <r>. A filter over ?s ?p ?o
            // cannot use an index, so that shape is O(baseline) per delete - measured at ~4 s against a
            // 1,000,000-triple in-memory baseline where the bound form is under 3 ms.
            foreach ((string s, string o) in new[] { (subject, "?o"), ("?s", subject) })
            {
                string triple = string.Concat(s, " ?p ", o);

                // Anything the baseline holds for it becomes a staged removal...
                operations.Add(string.Format("INSERT {{ GRAPH {0} {{ {1} }} }} WHERE {{ GRAPH {2} {{ {1} }} }}",
                    Graph(Removals), triple, Graph(Baseline)));
                // ...and anything only staged as an addition is simply un-staged.
                operations.Add(string.Format("DELETE {{ GRAPH {0} {{ {1} }} }} WHERE {{ GRAPH {0} {{ {1} }} }}",
                    Graph(Additions), triple));
            }

            RunAtomically(string.Join("; ", operations), transaction);

            // A delete is a change made through the view like any other, so the view owes the
            // materialized graph the same maintenance it gives a staged update. Without this a
            // deleted resource stays readable through the very view that deleted it, and nothing
            // tells the caller - Refresh() is documented as being for out-of-band writes.
            SynchronizeMaterializedFor(subject, transaction);
        }

        /// <inheritdoc cref="DeleteResource(Uri, ITransaction)" />
        public void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            DeleteResource(resource.Uri, transaction);
        }

        /// <inheritdoc cref="DeleteResource(Uri, ITransaction)" />
        public void DeleteResources(IEnumerable<Uri> uris, ITransaction transaction = null)
        {
            foreach (Uri uri in uris)
            {
                DeleteResource(uri, transaction);
            }
        }

        /// <inheritdoc cref="DeleteResource(Uri, ITransaction)" />
        public void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null)
        {
            DeleteResources(resources.Select(r => (Uri)r.Uri), transaction);
        }

        /// <inheritdoc cref="DeleteResource(Uri, ITransaction)" />
        public void DeleteResources(ITransaction transaction = null, params IResource[] resources)
        {
            DeleteResources(resources, transaction);
        }

        /// <summary>
        /// Begins a transaction on the underlying store.
        /// </summary>
        /// <remarks>
        /// Real only where the store provides one — Virtuoso does; the others hand back a
        /// <c>NoOpTransaction</c> whose rollback provably undoes nothing (ADR-0028), and rely instead
        /// on a multi-operation request being atomic in its own right.
        /// </remarks>
        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return _store.BeginTransaction(isolationLevel);
        }

        #endregion

        #region Not supported

        private static NotSupportedException Unsupported(string what)
        {
            return new NotSupportedException(
                "A layered model cannot " + what + ". Stage changes by modifying resources read through " +
                "the view and committing them, then Accept() or Discard() the result; or address the " +
                "Baseline, Additions and Removals models directly, which are ordinary models.");
        }

        /// <summary>
        /// Not supported: a caller's update cannot be routed into the layers the way a resource delta
        /// can, because there is no way to tell which of its effects should become an addition and
        /// which a removal.
        /// </summary>
        public void ExecuteUpdate(ISparqlUpdate update, ITransaction transaction = null) =>
            throw Unsupported("run a caller-supplied SPARQL update");

        /// <summary>
        /// Not supported: ambiguous on a view. Use <see cref="Discard"/> to drop the staged change, or
        /// clear the layer models individually.
        /// </summary>
        public void Clear() => throw Unsupported("be cleared");

        /// <summary>Not supported. Read into the Additions or Baseline model instead.</summary>
        public bool Read(Uri url, RdfSerializationFormat format, bool update) => throw Unsupported("be read into");

        /// <inheritdoc cref="Read(Uri, RdfSerializationFormat, bool)" />
        public bool Read(Stream stream, RdfSerializationFormat format, bool update) => throw Unsupported("be read into");

        /// <inheritdoc cref="Read(Uri, RdfSerializationFormat, bool)" />
        public bool Read(string content, RdfSerializationFormat format, bool update) => throw Unsupported("be read into");

        /// <summary>
        /// Not supported: serializing the view would mean materializing the effective graph, which is a
        /// different operation from writing a model out.
        /// </summary>
        public void Write(Stream stream, RdfSerializationFormat format, INamespaceMap namespaces = null, Uri baseUri = null, bool leaveOpen = false) =>
            throw Unsupported("be written out");

        /// <inheritdoc cref="Write(Stream, RdfSerializationFormat, INamespaceMap, Uri, bool)" />
        public void Write(Stream stream, IRdfWriter formatWriter, bool leaveOpen = false) => throw Unsupported("be written out");

        #endregion
    }
}
