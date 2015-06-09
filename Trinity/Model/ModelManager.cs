/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) Semiodesk GmbH 2015

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VDS.RDF.Storage;
using VDS.RDF;
using System.Reflection;
using System.Threading;
using System.Net;
using Semiodesk.Trinity.Store;
using System.IO;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Responsible entity for managing the connection to the OpenLink Virtuoso database and its RDF models.
    /// TODO: We should remove IEnumerable because of clutter.
    /// </summary>
    public class ModelManager : IEnumerable<IModel>
    {
        #region Members

        // The backing RDF store.
        //private IStore _store = null;

        // Reference to the singleton instance of this class.
        private static ModelManager _instance = null;
        private Dictionary<Thread, IStore> ThreadDictionary = new Dictionary<Thread, IStore>();
        private static volatile object _lock = new object();
        //private bool _connectUbiquity = true;

        private IStore _remoteSparqlEndpoint;

        /// <summary>
        /// Reference to the singleton instance of the ModelManager.
        /// </summary>
        public static ModelManager Instance
        {
            get 
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ModelManager();
                    }

                    return _instance;
                }
            }
        }



        /// <summary>
        /// Indicates if a connection to the database has been established.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (ThreadDictionary)
                {
                    if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                    {
                        return ThreadDictionary[Thread.CurrentThread].IsReady;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Private due to the singleton pattern. Use ModelManager.Instance instead.
        /// </summary>
        private ModelManager()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Connect to the database using the default credentials. Note that this class 
        /// is for testing purposes only and will be removed in release versions of this library.
        /// </summary>
        /// <returns>True if a connection has been established, False otherwise.</returns>
        public bool Connect()
        {
            #if DEBUG
            //return Connect("localhost", 1112, "dba", "dba");
            return Connect("localhost", 1111, "dba", "dba", "urn:semiodesk/ruleset");
            #else
            return Connect("localhost", 1112, "dba", "dba", "urn:semiodesk/ruleset");
            #endif

        }

        /// <summary>
        /// Connect to the database using the given credentials.
        /// </summary>
        /// <returns>True if a connection has been established, False otherwise.</returns>
        public bool Connect(string host, int port, string user, string password, string defaultInferenceRule)
        {
            lock (ThreadDictionary)
            {
                var store = new VirtuosoStore(host, port, user, password, defaultInferenceRule);
                if( !ThreadDictionary.ContainsKey(Thread.CurrentThread))
                    ThreadDictionary.Add(Thread.CurrentThread, store);
            
                return store.IsReady;
            }
        }

        public bool Connect(string connectionString)
        {
            lock (ThreadDictionary)
            {
                var store = Stores.CreateStore(connectionString);
                if (!ThreadDictionary.ContainsKey(Thread.CurrentThread))
                    ThreadDictionary.Add(Thread.CurrentThread, store);

                return store.IsReady;
            }
        }

        /// <summary>
        /// We use this method to test the Sparql Endpoint stuff. Later we have to see how we manage multiple storages.
        /// </summary>
        /// <param name="endpointUri"></param>
        /// <param name="proxy"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public bool ConnectRemote(Uri endpointUri,  IWebProxy proxy = null, ICredentials credentials = null)
        {
            _remoteSparqlEndpoint = new SparqlEndpointStorage(endpointUri, proxy, credentials);
            return true;
        }

        public IModel GetRemoteModel(Uri uri)
        {
            return _remoteSparqlEndpoint.GetModel(uri);
        }

        public List<IModel> ListRemoteModels()
        {
            return null;
        }

        /// <summary>
        /// Disconnect from the database.
        /// </summary>
        /// <returns>True if the connection has been properly closed, False otherwise.</returns>
        public bool Disconnect()
        {
            if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
            {
                IStore store;
                lock (ThreadDictionary)
                {
                   store = ThreadDictionary[Thread.CurrentThread];
                   ThreadDictionary.Remove(Thread.CurrentThread);
                }
                store.Dispose();
                store = null;
                
            }
            return true;
        }

        /// <summary>
        /// Create a new empty model.
        /// </summary>
        /// <param name="uri">Uniform Resource Identifier of the model.</param>
        /// <returns>A reference to the newly created model.</returns>
        /// <exception cref="NotSupportedException">Throws NotSupportedException if the database is read only.</exception>
        /// <exception cref="ArgumentException">Throws ArgumentException if a model with the given URI already exists.</exception>
        /// <exception cref="Exception">Throws Exception when there is no connection to the database.</exception>
        public IModel CreateModel(Uri uri)
        {
            lock (ThreadDictionary)
            {
                if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                {
                    var store = ThreadDictionary[Thread.CurrentThread];
                    if (store.ContainsModel(uri))
                    {
                        throw new ArgumentException(string.Format("Error: The model {0} already exists.", uri));
                    }
                    else
                    {
                        return store.CreateModel(uri);
                    }
                }
            }
            throw new InvalidOperationException("This thread has no open connection to the RDF store. You need to call the Connect method first.");
        }

        /// <summary>
        /// Deletes a model and its contents.
        /// </summary>
        /// <param name="uri">Uniform Resource Identifier of the model.</param>
        /// <returns>True if the model has been deleted, False otherwise.</returns>
        /// <exception cref="NotSupportedException">Throws NotSupportedException if the database is read only.</exception>
        /// <exception cref="Exception">Throws Exception when there is no connection to the database.</exception>
        public bool DeleteModel(Uri uri)
        {
            lock (ThreadDictionary)
            {
            
            if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
            {
                var store = ThreadDictionary[Thread.CurrentThread];
                if (!store.ContainsModel(uri))
                {
                    throw new ArgumentException(string.Format("Error: The model {0} does not exist.", uri));
                }
                else
                {
                    store.RemoveModel(uri);
                }
                return true;
            }
            }
            throw new InvalidOperationException("This thread has no open connection to the RDF store. You need to call the Connect method first.");
        }

        public bool ContainsModel(Uri uri)
        {
            lock (ThreadDictionary)
            {
                if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                {
                    return ThreadDictionary[Thread.CurrentThread].ContainsModel(uri);
                }
            }
            throw new InvalidOperationException("This thread has no open connection to the RDF store. You need to call the Connect method first.");
        }


        /// <summary>
        /// Deletes a model and its contents.
        /// </summary>
        /// <param name="model">Handle of the model.</param>
        /// <returns>True if the model has been deleted, False otherwise.</returns>
        /// <exception cref="NotSupportedException">Throws NotSupportedException if the database is read only.</exception>
        /// <exception cref="Exception">Throws Exception when there is no connection to the database.</exception>
        public bool DeleteModel(IModel model)
        {
            return DeleteModel(model.Uri);
        }

        /// <summary>
        /// Lists all the models which are accessible through the established database connection.
        /// </summary>
        /// <returns>An enumeration of Models.</returns>
        /// <exception cref="Exception">Throws Exception when there is no connection to the database.</exception>
        public IEnumerable<IModel> ListModels()
        {
            lock(ThreadDictionary)
            {
            if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
            {
                return ThreadDictionary[Thread.CurrentThread].ListModels();
            }
            }

            throw new InvalidOperationException("This thread has no open connection to the RDF store. You need to call the Connect method first.");
        }

        /// <summary>
        /// Returns an enumerator for easy traversal through the accessible models in the RDF database.
        /// </summary>
        /// <returns>An enumerator for traversing the list of models.</returns>
        public IEnumerator<IModel> GetEnumerator()
        {
            return ListModels().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator for easy traversal through the accessible models in the RDF database.
        /// </summary>
        /// <returns>An enumerator for traversing the list of models.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ListModels().GetEnumerator();
        }

        /// <summary>
        /// Retrieves a model from the database.
        /// </summary>
        /// <param name="uri">Uniform Resource Identifier of the model.</param>
        /// <returns>A reference to the requested model.</returns>
        /// <exception cref="Exception">Throws Exception when there is no connection to the database.</exception>
        public IModel GetModel(Uri uri)
        {
            lock (ThreadDictionary)
            {
                if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                {
                    return ThreadDictionary[Thread.CurrentThread].GetModel(uri);
                }
            }

            throw new InvalidOperationException("This thread has no open connection to the RDF store. You need to call the Connect method first.");
        }

        public IModel Import(Uri graphUri, Uri location, RdfSerializationFormat format)
        {
            lock (ThreadDictionary)
            {
                if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                {
                    Uri t = ThreadDictionary[Thread.CurrentThread].Read(graphUri, location, format);
                }
            }
            return null;
        }

        public IModel Export(FileInfo target, Uri graphUri, RdfSerializationFormat format)
        {
            if (target.Exists)
                target.Delete();

            FileStream s = target.OpenWrite();
     
            lock (ThreadDictionary)
            {
                if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                {
                     ThreadDictionary[Thread.CurrentThread].Write(s, graphUri, format);
                }
            }
            return null;
        }

        public IModelGroup CreateModelGroup(params Uri[] models)
        {
            lock (ThreadDictionary)
            {
                if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                {
                    List<IModel> modelList = new List<IModel>();
                    foreach (var x in models)
                    {
                        modelList.Add(GetModel(x));
                    }

                    return new ModelGroup(ThreadDictionary[Thread.CurrentThread], modelList);
                }
            }

            throw new InvalidOperationException("This thread has no open connection to the RDF store. You need to call the Connect method first.");
            
        }

        public IStore GetStore()
        {
            lock (ThreadDictionary)
            {
                if (ThreadDictionary.ContainsKey(Thread.CurrentThread))
                {
                    return ThreadDictionary[Thread.CurrentThread];
                }
            }

            throw new InvalidOperationException("This thread has no open connection to the RDF store. You need to call the Connect method first.");
            
        }
        #endregion


    }
}
