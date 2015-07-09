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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// The IStorage interface describes the methods an RDF Storage has to implement.
    /// </summary>
    public interface IStore : IDisposable
    {
        #region Properties

        bool IsReady { get; }

        #endregion

        #region Methods
        /// <summary>
        /// Adds a new model with the given uri to the storage. 
        /// </summary>
        /// <param name="uri">Uri of the model</param>
        /// <returns>Handle to the model</returns>
        IModel CreateModel(Uri uri);

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="uri">Uri of the model which is to be removed.</param>
        void RemoveModel(Uri uri);

        /// <summary>
        /// Removes model from the store.
        /// </summary>
        /// <param name="model">Handle to the model which is to be removed.</param>
        void RemoveModel(IModel model);

        /// <summary>
        /// Query if the model exists in the store.
        /// </summary>
        /// <param name="uri">Uri of the model which is to be queried.</param>
        /// <returns></returns>
        bool ContainsModel(Uri uri);

        /// <summary>
        /// Query if the model exists in the store.
        /// </summary>
        /// <param name="uri">Handle to the model which is to be queried.</param>
        /// <returns></returns>
        bool ContainsModel(IModel model);

        /// <summary>
        /// Gets a handle to a model in the store.
        /// </summary>
        /// <param name="uri">Uri of the model.</param>
        /// <returns></returns>
        IModel GetModel(Uri uri);

        /// <summary>
        /// Lists all models in the store.
        /// </summary>
        /// <returns>All handles to existing models.</returns>
        IEnumerable<IModel> ListModels();

        /// <summary>
        /// Executes a SparqlQuery on the store.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        ISparqlQueryResult ExecuteQuery(SparqlQuery query, ITransaction transaction = null);

        /// <summary>
        /// Executes a query on the store which does not expect a result.
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="transaction"></param>
        void ExecuteNonQuery(SparqlUpdate update, ITransaction transaction = null);

        /// <summary>
        /// Starts a transaction. The resulting transaction handle can be used to chain operations together.
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        ITransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Creates a model group which allows for queries to be made on multiple models at once.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        IModelGroup CreateModelGroup(params Uri[] models);
        
        /// <summary>
        /// Loads a serialized graph from the given location into the current store. See allowed <see cref="RdfSerializationFormat">formats</see>.
        /// </summary>
        /// <param name="graphUri">Uri of the graph in this store</param>
        /// <param name="url">Location</param>
        /// <param name="format">Allowed formats</param>
        /// <returns></returns>
        Uri Read(Uri graphUri, Uri url, RdfSerializationFormat format);

        void Write(Stream fs, Uri graphUri, RdfSerializationFormat format);



        #endregion
    }
}
