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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenLink.Data.Virtuoso;
using System.IO;

namespace Semiodesk.Trinity.OntologyDeployment
{
    internal class OntologyUpdater
    {
        #region Fields
        private DirectoryInfo _sourceDirectory;
        IStore _store;
        #endregion

        #region Constructors

        public OntologyUpdater(string connectionString, DirectoryInfo sourceDir)
        {
            _sourceDirectory = sourceDir;

            _store = Stores.CreateStore(connectionString);
        }

        #endregion

        #region Methods

        protected void RemoveGraph(Uri graphUri)
        {
            if (_store.ContainsModel(graphUri))
            {
                _store.RemoveModel(graphUri);
            }
        }

        public void UpdateOntologies(List<Ontology> ontologies)
        {
            foreach (Ontology onto in ontologies)
            {
                Uri path = GetPathFromSource(onto.Source);

                RdfSerializationFormat format = GetSerializationFormatFromUri(path);

                _store.Read(onto.Uri, path, format);
            }
        }

        public void UpdateStorageSpecifics(IStorageSpecific storageSpecific)
        {
            storageSpecific.Update(_store);
        }

        protected Uri GetPathFromSource(Source source)
        {
            Uri result = null;
            if (source != null && source is FileSource)
            {
                string sourcePath = (source as FileSource).Path;

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
            else if (source != null && source is WebSource)
            {
                result = (source as WebSource).FileUrl;
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

            return RdfSerializationFormat.RdfXml;
        }

        protected Uri DeployOntology(Uri source, DirectoryInfo target)
        {
            if (source.Scheme == "file")
            {
                FileInfo sourceFile = new FileInfo(source.LocalPath);

                FileInfo targetFile = new FileInfo(Path.Combine(target.FullName, sourceFile.Name));
                if (Encoding.ASCII == GetEncoding(sourceFile))
                {
                    string destFileName = Path.Combine(target.ToString(), sourceFile.Name);

                    sourceFile.CopyTo(destFileName, true);
                    return new Uri(destFileName);
                }
                else
                {
                    Console.WriteLine(@"Omitting the file {0} is not ASCII encoded. Please re-encode manually.", sourceFile.Name);
                }
            }
            else if (source.Scheme == "http")
            {
                return source;
            }
            return null;
        }

        public static Encoding GetEncoding(FileInfo file)
        {
            Encoding enc = null;
            FileStream fileStream = new System.IO.FileStream(file.FullName,
                FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fileStream.CanSeek)
            {
                byte[] bom = new byte[4]; // Get the byte-order mark, if there is one 
                fileStream.Read(bom, 0, 4);
                if ((bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) || // utf-8 
                    (bom[0] == 0xff && bom[1] == 0xfe) || // ucs-2le, ucs-4le, and ucs-16le 
                    (bom[0] == 0xfe && bom[1] == 0xff) || // utf-16 and ucs-2 
                    (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)) // ucs-4 
                {
                    enc = System.Text.Encoding.Unicode;
                }
                else
                {
                    enc = System.Text.Encoding.ASCII;
                }

                // Now reposition the file cursor back to the start of the file 
                fileStream.Seek(0, System.IO.SeekOrigin.Begin);
            }
            else
            {
                // The file cannot be randomly accessed, so you need to decide what to set the default to 
                // based on the data provided. If you're expecting data from a lot of older applications, 
                // default your encoding to Encoding.ASCII. If you're expecting data from a lot of newer 
                // applications, default your encoding to Encoding.Unicode. Also, since binary files are 
                // single byte-based, so you will want to use Encoding.ASCII, even though you'll probably 
                // never need to use the encoding then since the Encoding classes are really meant to get 
                // strings from the byte array that is the file. 

                enc = System.Text.Encoding.ASCII;
            }

            return enc;

        }
        #endregion
    }
}
