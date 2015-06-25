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
using System.Collections;
using System.Diagnostics;

namespace Semiodesk.Trinity
{
    class ResourceCache
    {
        #region Fields

        protected Dictionary<IPropertyMapping, List<Uri>> Cache = new Dictionary<IPropertyMapping, List<Uri>>();

        public IModel Model;

        #endregion

        #region Constructors

        public ResourceCache() {}

        #endregion

        #region Methods

        public void Clear()
        {
            Cache.Clear();
        }

        public void CacheValues(IPropertyMapping mapping, IEnumerable<Uri> values)
        {
            if (!Cache.ContainsKey(mapping))
            {
                Cache.Add(mapping, new List<Uri>());
                Cache[mapping].AddRange(values);
            }
            else
            {
                Cache[mapping].AddRange(values);
            }
        }

        public void CacheValue(IPropertyMapping mapping, Uri value)
        {
            if (!Cache.ContainsKey(mapping))
            {
                Cache[mapping] = new List<Uri>();
                Cache[mapping].Add(value);
            }
            else
            {
                Cache[mapping].Add(value);
            }
        }

        /// <summary>
        /// This method loads the cached Resources for the given MappingProperty from the Storage and returns them.
        /// They are instantiated as the defined type. The cache for this mapping property is emptied.
        /// </summary>
        /// <param name="mapping">Mapping property which should be loaded from cache.</param>
        /// <returns>List of formerly cached resources.</returns>
        public void LoadCachedValues(IPropertyMapping mapping)
        {
            if (!Cache.ContainsKey(mapping))
            {
                return;
            }

            Type baseType = (mapping.IsList) ? mapping.GenericType : mapping.DataType;

            List<Uri> cachedUris = Cache[mapping];

            if (!mapping.IsList && cachedUris.Count > 1)
            {
                throw new Exception(string.Format("An error occured while loading the cached resources for property {0}.", mapping.PropertyName));
            }

            foreach (Uri u in cachedUris)
            {
                object r = null;

                if (Model == null)
                {
                    Debugger.Break();
                }

                if (Model.ContainsResource(u))
                {
                    r = Model.GetResource(u, baseType);
                }
                else
                {
                    r = Activator.CreateInstance(baseType, u);
                }

                if (mapping.IsList)
                {
                    IList l = mapping.GetValueObject() as IList;

                    if (l != null)
                    {
                        l.Add(r);
                    }
                }
                else
                {
                    mapping.SetOrAddMappedValue(r);
                }
            }

            Cache.Remove(mapping);
        }

        /// <summary>
        /// Tests if the mapping has cached values.
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public bool HasCachedValues(IPropertyMapping mapping)
        {
            return Cache.ContainsKey(mapping);
        }

        /// <summary>
        /// Tests if the mapping has a certain cached values.
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public bool HasCachedValues(IPropertyMapping mapping, Uri uri)
        {
            if (Cache.ContainsKey(mapping))
            {
                return Cache[mapping].Contains(uri);
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool HasCachedValue(Uri uri)
        {
            foreach (List<Uri> l in Cache.Values)
            {
                if (l.Contains(uri))
                    return true;
            }
            return false;
        }

        public IPropertyMapping ValueMappedOn(Uri uri)
        {
            foreach (KeyValuePair<IPropertyMapping, List<Uri>> pair in Cache)
            {
                if (pair.Value.Contains(uri))
                    return pair.Key;
            }
            return null;
        }

        public IEnumerable<Uri> ListCachedValues(IPropertyMapping mapping)
        {
            return Cache[mapping];
        }


        #endregion

    }
}
