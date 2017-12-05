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
using System.IO;
using Semiodesk.Trinity.Configuration;

namespace Semiodesk.Trinity.OntologyDeployment
{
    internal class OntologyUpdater
    {
        #region Fields
        private DirectoryInfo _sourceDirectory;
        IStore _store;
        public ILogger Logger { get; set; }
        #endregion

        #region Constructors

        public OntologyUpdater(IStore store, DirectoryInfo sourceDir)
        {
            _sourceDirectory = sourceDir;

            _store = store;
        }

        #endregion

        #region Methods

        public void UpdateOntologies(IEnumerable<Semiodesk.Trinity.Configuration.Ontology> ontologies)
        {
            foreach (Semiodesk.Trinity.Configuration.Ontology onto in ontologies)
            {
                Uri path = GetPathFromSource(onto.FileSource);

                RdfSerializationFormat format = GetSerializationFormatFromUri(path);

                _store.Read(onto.Uri, path, format, false);
            }
        }

        public void UpdateStorageSpecifics(IStorageSpecific storageSpecific)
        {
            storageSpecific.Update(_store);
        }

        protected Uri GetPathFromSource(FileSource source)
        {
            Uri result = null;
            if (source != null && source is FileSource)
            {
                string sourcePath = (source as FileSource).Location;

                if (Path.IsPathRooted(sourcePath))
                {
                    result = new Uri(sourcePath);
                }
                else
                {
                    string fullPath = Path.Combine(_sourceDirectory.FullName, sourcePath);
                    result = new Uri(fullPath);
                }
            }
            return result;
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
