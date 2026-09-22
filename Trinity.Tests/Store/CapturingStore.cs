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
using System.Collections.Generic;
using System.Data;
using System.IO;
using VDS.RDF;

namespace Semiodesk.Trinity.Tests.Store
{
    /// <summary>
    /// An <see cref="IStore"/> that delegates everything to a real in-memory store and records the
    /// SPARQL that actually reached it.
    /// </summary>
    /// <remarks>
    /// This exists to pin a <b>call site</b>, which is the one thing the other tests cannot reach.
    /// Asserting on <c>SparqlSerializer</c>'s output proves the helper emits <c>VALUES</c>; it does
    /// not prove that <see cref="Model"/> and <see cref="ModelGroup"/> still call it — and building
    /// the constraint inline instead of calling the shared helper is precisely how the equality-chain
    /// defect existed in the first place (ADR-0046).
    /// <para>
    /// It also catches what the 2000-subject store test cannot: with subjects batched at 1000, that
    /// test sends two queries of 1000, both under the 1024 terms an equality chain still compiles on
    /// Virtuoso. A revert of the shape alone would pass it.
    /// </para>
    /// </remarks>
    internal class CapturingStore : IStore
    {
        private readonly IStore _store;

        public readonly List<string> Queries = new List<string>();

        public CapturingStore(IStore store)
        {
            _store = store;
        }

        /// <summary>
        /// The SPARQL of the most recent query, or <c>null</c> if none was issued.
        /// </summary>
        public string LastQuery => Queries.Count > 0 ? Queries[Queries.Count - 1] : null;

        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null)
        {
            Queries.Add(query.ToString());

            return _store.ExecuteQuery(query, transaction);
        }

        #region Pass-through

        public bool IsReady => _store.IsReady;

        public Action<string> Log { get => _store.Log; set => _store.Log = value; }

        public IModel CreateModel(Uri uri) => _store.CreateModel(uri);
        public void RemoveModel(Uri uri) => _store.RemoveModel(uri);
        public void RemoveModel(IModel model) => _store.RemoveModel(model);
        public bool ContainsModel(Uri uri) => _store.ContainsModel(uri);
        public bool ContainsModel(IModel model) => _store.ContainsModel(model);
        public IModel GetModel(Uri uri) => _store.GetModel(uri);
        public IEnumerable<IModel> ListModels() => _store.ListModels();
        public void ExecuteNonQuery(ISparqlUpdate update, ITransaction transaction = null) => _store.ExecuteNonQuery(update, transaction);
        public ITransaction BeginTransaction(IsolationLevel isolationLevel) => _store.BeginTransaction(isolationLevel);
        public IModelGroup CreateModelGroup(params Uri[] models) => _store.CreateModelGroup(models);
        public IModelGroup CreateModelGroup(params IModel[] models) => _store.CreateModelGroup(models);
        public Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format, bool update) => _store.Read(graphUri, url, format, update);
        public Uri Read(Stream stream, Uri graphUri, RdfSerializationFormat format, bool update, bool leaveOpen = false) => _store.Read(stream, graphUri, format, update, leaveOpen);
        public Uri Read(string content, Uri graphUri, RdfSerializationFormat format, bool update) => _store.Read(content, graphUri, format, update);
        public void Write(Stream fs, Uri graphUri, RdfSerializationFormat format, INamespaceMap namespaces = null, Uri baseUri = null, bool leaveOpen = false) => _store.Write(fs, graphUri, format, namespaces, baseUri, leaveOpen);
        public void Write(Stream fs, Uri graphUri, IRdfWriter formatWriter, bool leaveOpen = false) => _store.Write(fs, graphUri, formatWriter, leaveOpen);
        public void UpdateResource(Resource resource, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false) => _store.UpdateResource(resource, modelUri, transaction, ignoreUnmappedProperties);
        public void UpdateResources(IEnumerable<Resource> resources, Uri modelUri, ITransaction transaction = null, bool ignoreUnmappedProperties = false) => _store.UpdateResources(resources, modelUri, transaction, ignoreUnmappedProperties);
        public void DeleteResource(Uri modelUri, Uri resourceUri, ITransaction transaction = null) => _store.DeleteResource(modelUri, resourceUri, transaction);
        public void DeleteResource(IResource resource, ITransaction transaction = null) => _store.DeleteResource(resource, transaction);
        public void DeleteResources(Uri modelUri, IEnumerable<Uri> resources, ITransaction transaction = null) => _store.DeleteResources(modelUri, resources, transaction);
        public void DeleteResources(IEnumerable<IResource> resources, ITransaction transaction = null) => _store.DeleteResources(resources, transaction);
        public ISparqlQuery GetDescribeQuery(Uri modelUri, Uri subjectUri) => _store.GetDescribeQuery(modelUri, subjectUri);
        public void Dispose() => _store.Dispose();

        #endregion
    }
}
