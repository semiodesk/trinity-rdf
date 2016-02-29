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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Compares two models by their uris
    /// </summary>
    class IModelEqualityComparer : IEqualityComparer<IModel>
    {
        #region IEqualityComparer<IModel> Members

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(IModel x, IModel y)
        {
 	        return x.Uri.Equals(y.Uri);
        }

        /// <summary>
        /// HashCode
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(IModel obj)
        {
 	        return obj.Uri.AbsoluteUri.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Implementation of the IModelGroup interface
    /// </summary>
    class ModelGroup : IModelGroup
    {
        #region Member

        private IStore _store;

        private HashSet<IModel> _set = new HashSet<IModel>(new IModelEqualityComparer());

        private MethodInfo _getResourceMethod;

        private string _datasetClause = null;

        internal string DatasetClause
        {
            get
            {
                if (_datasetClause == null)
                    UpdateDatasetClause();
                return _datasetClause;
            }
        }

        #endregion

        #region Constructor
        public ModelGroup(IStore store, IEnumerable<IModel> models)
        {
            _store = store; 
            foreach (var x in models)
                _set.Add(x);
            UpdateDatasetClause();

            foreach (MethodInfo methodInfo in GetType().GetMethods())
            {
                if (methodInfo.Name == "GetResource" && methodInfo.IsGenericMethod)
                {
                    _getResourceMethod = methodInfo;
                    break;
                }
            }
        }
        #endregion

        #region Methods

        void UpdateDatasetClause()
        {
            _datasetClause = SparqlSerializer.GenerateDatasetClause(_set);
        }

        #region IModelGroup Members

        public IModel DefaultModel
        {
            get;
            set;
        }

        #endregion

        #region IModel Members

        /// <summary>
        /// Uri of the model group is null
        /// </summary>
        public UriRef Uri
        {
            get { return null; }
        }

        /// <summary>
        /// Tests if all contained models are empty
        /// </summary>
        public bool IsEmpty
        {
            get 
            {
                SparqlQuery query = new SparqlQuery(string.Format(@"ASK {0} {{ ?s ?p ?o . }}", DatasetClause));
                return !ExecuteQuery(query).GetAnwser();
            }
        }

        #region Not Supported
        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IResource AddResource(IResource resource, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public T AddResource<T>(T resource, ITransaction transaction = null) where T : Resource
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IResource CreateResource(string format = "http://semiodesk.com/id/{0}", ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IResource CreateResource(Uri uri, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="format"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public T CreateResource<T>(string format = "http://semiodesk.com/id/{0}", ITransaction transaction = null) where T : Resource
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public T CreateResource<T>(Uri uri, ITransaction transaction = null) where T : Resource
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="format"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public object CreateResource(Type t, string format = "http://semiodesk.com/id/{0}", ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="t"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public object CreateResource(Uri uri, Type t, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="transaction"></param>
        public void DeleteResource(Uri uri, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="transaction"></param>
        public void DeleteResource(IResource resource, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="transaction"></param>
        public void UpdateResource(Resource resource, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        public ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="transaction"></param>
        public void ExecuteUpdate(SparqlUpdate update, ITransaction transaction = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported. ModelGroups are read-only.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="format"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public bool Read(Uri url, RdfSerializationFormat format, bool update)
        {
            throw new NotSupportedException();
        }


        #endregion

        public bool ContainsResource(Uri uri, ITransaction transaction = null)
        {
            return ExecuteQuery(new SparqlQuery(string.Format(@"ASK {0} {{ {1} ?p ?o . }}",
                DatasetClause,
                SparqlSerializer.SerializeUri(uri))), transaction: transaction).GetAnwser();
        }

        public bool ContainsResource(IResource resource, ITransaction transaction = null)
        {
            return ContainsResource(resource.Uri, transaction);
        }

        public ISparqlQueryResult ExecuteQuery(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            query.Model = this;
            query.IsInferenceEnabled = inferenceEnabled;

            return _store.ExecuteQuery(query, transaction);
        }

        public IResourceQueryResult ExecuteQuery(ResourceQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            return new ResourceQueryResult(this, query, inferenceEnabled, transaction);
        }

        public IResource GetResource(Uri uri, ITransaction transaction = null)
        {
            SparqlQuery query = new SparqlQuery(String.Format("DESCRIBE {0} {1}", SparqlSerializer.SerializeUri(uri), DatasetClause));
            ISparqlQueryResult result = ExecuteQuery(query, transaction: transaction);

            IList resources = result.GetResources().ToList();

            if (resources.Count > 0)
            {
                Resource res = resources[0] as Resource;
                res.IsNew = false;
                res.IsReadOnly = true;
                res.IsSynchronized = true;
                res.SetModel(this);
                return (IResource)resources[0];
            }
            else
            {
                string msg = "Error: Could not find resource {0}.";
                throw new ArgumentException(string.Format(msg, uri));
            }
        }

        public T GetResource<T>(Uri uri, ITransaction transaction = null) where T : Resource
        {
            SparqlQuery query = new SparqlQuery(String.Format("DESCRIBE {0} {1}", SparqlSerializer.SerializeUri(uri), DatasetClause));
            ISparqlQueryResult result = ExecuteQuery(query, transaction: transaction);

            IList resources = result.GetResources<T>().ToList();

            if (resources.Count > 0)
            {
                T res = resources[0] as T;
                res.IsNew = false;
                res.IsSynchronized = true;
                res.IsReadOnly = true;
                res.SetModel(this);
                return res;
            }
            else
            {
                string msg = "Error: Could not find resource <{0}>.";
                throw new ArgumentException(string.Format(msg, uri));
            }
        }

        public object GetResource(Uri uri, Type type, ITransaction transaction = null)
        {
            if (_getResourceMethod != null)
            {
                if (typeof(IResource).IsAssignableFrom(type))
                {
                    MethodInfo getResource = _getResourceMethod.MakeGenericMethod(type);
                    Resource res = getResource.Invoke(this, new object[] { uri, transaction }) as Resource;
                    res.IsNew = false;
                    res.IsReadOnly = true;
                    res.IsSynchronized = true;
                    res.SetModel(this);
                    return res;
                }
                else
                {
                    string msg = string.Format("Error: The given type {0} does not implement the IResource interface.", type);
                    throw new ArgumentException(msg);
                }
            }
            else
            {
                string msg = string.Format("Error: No handle to the generic method T GetResource<T>(Uri)");
                throw new InvalidOperationException(msg);
            }
        }

        public IEnumerable<Resource> GetResources(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            IEnumerable<Resource> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<Resource>();

            if (result != null)
            {
                foreach (Resource r in result)
                {
                    r.SetModel(this);
                    r.IsNew = false;
                    r.IsReadOnly = true;
                    r.IsSynchronized = true;
                }
            }

            return result;
        }

        public IEnumerable<Resource> GetResources(ResourceQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            IEnumerable<Resource> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<Resource>();

            if (result != null)
            {
                // TODO: Should be done in the SparqlQueryResult for increased performance.
                foreach (Resource r in result)
                {
                    r.SetModel(this);
                    r.IsNew = false;
                    r.IsSynchronized = true;
                    r.IsReadOnly = true;
                }
            }

            return result;
        }

        public IEnumerable<T> GetResources<T>(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            IEnumerable<T> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<T>();

            // TODO: Could be done in the SparqlQueryResult for increased performance.
            if (result != null)
            {
                foreach (object r in result)
                {
                    T t = r as T;

                    // NOTE: This safeguard is required because of a bug in ExecuteQuery where 
                    // it returns null objects when a rdf:type triple is missing..
                    if (t == null) continue;

                    t.SetModel(this);
                    t.IsNew = false;
                    t.IsSynchronized = true;
                    t.IsReadOnly = true;

                    yield return t;
                }
            }
        }

        public IEnumerable<T> GetResources<T>(ResourceQuery query, bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            IEnumerable<T> result = ExecuteQuery(query, inferenceEnabled, transaction).GetResources<T>();

            // TODO: Could be done in the SparqlQueryResult for increased performance.
            if (result != null)
            {
                foreach (object r in result)
                {
                    T t = r as T;

                    // NOTE: This safeguard is required because of a bug in ExecuteQuery where 
                    // it returns null objects when a rdf:type triple is missing..
                    if (t == null) continue;

                    t.SetModel(this);
                    t.IsNew = false;
                    t.IsSynchronized = true;
                    t.IsReadOnly = true;

                    yield return t;
                }
            }
        }

        /// <summary>
        /// Returns a enumeration of all resources that match the given type.
        /// </summary>
        /// <returns>An enumeration of resources that match the given query.</returns>
        public IEnumerable<T> GetResources<T>(bool inferenceEnabled = false, ITransaction transaction = null) where T : Resource
        {
            T resource = Activator.CreateInstance<T>();

            //resource.Classes
            return null;
        }

        public IEnumerable<BindingSet> GetBindings(ISparqlQuery query, bool inferenceEnabled = false, ITransaction transaction = null)
        {
            return ExecuteQuery(query, inferenceEnabled, transaction).GetBindings();
        }

        public void Write(Stream fs, RdfSerializationFormat format)
        {
            throw new NotSupportedException();
        }

        public bool Read(Stream stream, RdfSerializationFormat format, bool update)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region ISet<IModel> Members

        public bool Add(IModel item)
        {
            var x = _set.Add(item);
            UpdateDatasetClause();
            return x;
        }

        public void ExceptWith(IEnumerable<IModel> other)
        {
            _set.ExceptWith(other);
            UpdateDatasetClause();
        }

        public void IntersectWith(IEnumerable<IModel> other)
        {
            _set.IntersectWith(other);
            UpdateDatasetClause();
        }

        public bool IsProperSubsetOf(IEnumerable<IModel> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<IModel> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<IModel> other)
        {
            return _set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<IModel> other)
        {
            return _set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<IModel> other)
        {
            return _set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<IModel> other)
        {
            return _set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<IModel> other)
        {
            _set.SymmetricExceptWith(other);
            UpdateDatasetClause();
        }

        public void UnionWith(IEnumerable<IModel> other)
        {
            _set.UnionWith(other);
            UpdateDatasetClause();
        }

        #endregion

        #region ICollection<IModel> Members

        void ICollection<IModel>.Add(IModel item)
        {
            _set.Add(item);
            UpdateDatasetClause();
        }

        public bool Contains(IModel item)
        {
            return _set.Contains(item);
        }

        public void CopyTo(IModel[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _set.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IModel item)
        {
            var res = _set.Remove(item);
            UpdateDatasetClause();
            return res;
        }

        #endregion

        #region IEnumerable<IModel> Members

        /// <summary>
        /// Enumerator of the models
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IModel> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Enumerator of the models
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        #endregion
        #endregion
    }
}
