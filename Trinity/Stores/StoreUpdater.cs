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
using System.ComponentModel;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// This class can be used to load or update ontologies in stores. It provides convinence methods to load directly from the ontologies.config file.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]   
    public class StoreUpdater
    {
        #region Members

        private DirectoryInfo _sourceDirectory;

        private IStore _store;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new instance of the <c>StoreUpdater</c> class.
        /// </summary>
        /// <param name="store">The store you want to update.</param>
        /// <param name="sourceDir">A directory used as base path.</param>
        public StoreUpdater(IStore store, DirectoryInfo sourceDir)
        {
            _sourceDirectory = sourceDir;
            _store = store;
        }

        #endregion

        #region Methods
        /// <summary>
        /// This method loads the given ontologies into the provided store. 
        /// A model will be created for each ontology. If it already exists, it wil be replaced.
        /// </summary>
        /// <param name="ontologies">A collection of ontologies to be loaded.</param>
        public void UpdateOntologies(IEnumerable<Semiodesk.Trinity.Configuration.IOntologyConfiguration> ontologies)
        {
            foreach (var onto in ontologies)
            {
                if (!string.IsNullOrEmpty(onto.Location))
                {
                    Uri path = GetPathFromLocation(onto.Location);

                    RdfSerializationFormat format = GetSerializationFormatFromUri(path);

                    _store.Read(onto.Uri, path, format, false);
                }
                else
                {
                    throw new ArgumentException(string.Format("The file for the ontology {0} ({1}) could not be found. Please check the configuration file.", onto.Prefix, onto.Uri));
                }
            }
        }

        /// <summary>
        /// Gets an absolute path from a location relative to the triple store instance.
        /// </summary>
        /// <param name="location">A relative path.</param>
        /// <returns></returns>
        protected Uri GetPathFromLocation(string location)
        {
            Uri result = null;

            if (Path.IsPathRooted(location))
            {
                result = new Uri(location);
            }
            else
            {
                string fullPath = Path.Combine(_sourceDirectory.FullName, location);
                result = new Uri(fullPath);
            }

            return result;
        }

        /// <summary>
        /// This method can be used to load storage specific configuration.
        /// </summary>
        /// <param name="storageSpecific"></param>
        public void UpdateStorageSpecifics(IStoreSpecific storageSpecific)
        {
            storageSpecific.Update(_store);
        }


        private RdfSerializationFormat GetSerializationFormatFromUri(Uri uri)
        {
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (ext == ".n3")
                return RdfSerializationFormat.N3;

            if (ext == ".trig")
                return RdfSerializationFormat.Trig;

            if (ext == ".ttl")
                return RdfSerializationFormat.Turtle;

            if (ext == ".nt")
                return RdfSerializationFormat.NTriples;

            return RdfSerializationFormat.RdfXml;
        }

        #endregion
    }
}
