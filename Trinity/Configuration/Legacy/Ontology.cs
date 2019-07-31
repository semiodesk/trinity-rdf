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
using System.Configuration;

namespace Semiodesk.Trinity.Configuration.Legacy
{
    /// <summary>
    /// A ontology element
    /// </summary>
    public class Ontology : ConfigurationElement, IOntologyConfiguration
    {
        /// <summary>
        /// Prefix of this ontology.
        /// </summary>
        [ConfigurationProperty("Prefix", IsRequired=true)]
        public string Prefix
        {
            get { return (string)base["Prefix"]; }
            set { base["Prefix"] = value; }
        }

        /// <summary>
        /// URI of this ontology.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return !string.IsNullOrEmpty(UriString) ? new UriRef(UriString) : null;
            }
            set
            {
                UriString = value.OriginalString;
            }
        }

        /// <summary>
        /// String representation of the URI.
        /// </summary>
        [ConfigurationProperty("Uri", IsKey = true, IsRequired = true)]
        public string UriString
        {
            get { return (string)base["Uri"]; }
            set { base["Uri"] = value; }
        }

        /// <summary>
        /// The key of the element.
        /// </summary>
        public object KeyElement
        {
            get { return UriString; }
        }

        /// <summary>
        /// The location of the ontology file.
        /// </summary>
        [ConfigurationProperty("FileSource", IsRequired = true)]
        public FileSource FileSource
        {
            get { return (FileSource)base["FileSource"]; }
            set { base["FileSource"] = value; }
        }

        /// <summary>
        /// The uri of the metadata graph, only needed for TriG serialisations.
        /// </summary>
        public UriRef MetadataUri
        {
            get
            {
                return !string.IsNullOrEmpty(MetadataUriString) ? new UriRef(MetadataUriString) : null;
            }
            set
            {
                MetadataUriString = value.OriginalString;
            }
        }

        /// <summary>
        /// The string representation of the metadata graph URI.
        /// </summary>
        [ConfigurationProperty("MetadataUri", IsRequired = false)]
        public string MetadataUriString
        {
            get { return (string)base["MetadataUri"]; }
            set { base["MetadataUri"] = value; }
        }

        /// <summary>
        /// String representation of this element.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Uri != null ? Uri.OriginalString : "Empty Ontology.";
        }

        /// <summary>
        /// Overwritten hashcode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Get the location of the ontology file source.
        /// </summary>
        public string Location
        {
            get 
            {
                return FileSource?.Location;                
            }
        }
    }

}
