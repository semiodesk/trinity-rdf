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
        #region Members

        public IModel Model;

        protected Dictionary<IPropertyMapping, HashSet<Uri>> Cache = new Dictionary<IPropertyMapping, HashSet<Uri>>();

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
                Cache[mapping] = new HashSet<Uri>(values);
            }
            else
            {
                HashSet<Uri> cache = Cache[mapping];

                foreach (Uri value in values)
                {
                    cache.Add(value);
                }
            }
        }

        public void CacheValue(IPropertyMapping mapping, Uri value)
        {
            if (!Cache.ContainsKey(mapping))
            {
                Cache[mapping] = new HashSet<Uri>() { value };
            }
            else
            {
                HashSet<Uri> cache = Cache[mapping];

                cache.Add(value);
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

            HashSet<Uri> cachedUris = Cache[mapping];

            if (!mapping.IsList && cachedUris.Count > 1)
            {
                throw new Exception(string.Format("An error occured while loading the cached resources for property {0}. Found {1} elements but it is mapped to a non-list property. Try to map to a list of objects.", mapping.PropertyName, cachedUris.Count));
            }

            foreach (Uri uri in cachedUris)
            {
                object resource = null;

                #if DEBUG
                if (Model == null)
                {
                    Debugger.Break();
                }
                #endif

                if (Model.ContainsResource(uri))
                {
                    resource = Model.GetResource(uri, baseType);
                }
                else
                {
                    resource = Activator.CreateInstance(baseType, uri);
                }

                if (mapping.IsList)
                {
                    // Getting the reference to the mapped list object
                    IList list = mapping.GetValueObject() as IList;

                    if (list != null)
                    {
                        // Make sure the resource exits only one time
                        if (list.Contains(resource))
                            list.Remove(resource);

                        // Add ther resource to the mapped list
                        list.Add(resource);
                    }
                }
                else
                {
                    mapping.SetOrAddMappedValue(resource);
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
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool HasCachedValues(IPropertyMapping mapping, Uri uri)
        {
            return Cache.ContainsKey(mapping) ? Cache[mapping].Contains(uri) : false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool HasCachedValue(Uri uri)
        {
            return Cache.Values.Any(set => set.Contains(uri));
        }

        public IEnumerable<Uri> ListCachedValues(IPropertyMapping mapping)
        {
            return Cache[mapping];
        }

        #endregion
    }
}
