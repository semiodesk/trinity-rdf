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


using Semiodesk.Trinity.Configuration.Legacy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

// This namespace needs to be this for legacy applications to resolve the configuration correctly
namespace Semiodesk.Trinity.Configuration
{
    /// <summary>
    /// Constains the settings for the Semiodesk.Trinity framework.
    /// </summary>
    public class TrinitySettings : ConfigurationSection, IConfiguration
    {
        /// <summary>
        /// Namespace of the generated ontology file.
        /// </summary>
        [ConfigurationProperty("namespace", DefaultValue = "Semiodesk.Trinity.Model", IsRequired = true)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string Namespace
        {
            get
            {
                return (string)this["namespace"];
            }
            set
            {
                this["namespace"] = value;
            }
        }

        /// <summary>
        /// Collection of ontologies
        /// </summary>
        [ConfigurationProperty("OntologySettings", IsDefaultCollection = true)]
        public OntologyCollection Ontologies
        {
            get { return (OntologyCollection)base["OntologySettings"]; }
        }


        public object GetSettings(string name)
        {
            return this[name];
        }

        /// <summary>
        /// Virtuoso specific settings
        /// </summary>
        [ConfigurationProperty("VirtuosoStoreSettings")]
        public VirtuosoStoreSettings VirtuosoStoreSettings
        {
            get
            {
                return (VirtuosoStoreSettings)this["VirtuosoStoreSettings"];
            }
            set
            {
                this["VirtuosoStoreSettings"] = value;
            }
        }


        public IEnumerable<IOntologyConfiguration> ListOntologies()
        {
            return Ontologies;
        }

        public IEnumerable<IStoreConfiguration> ListStoreConfigurations()
        {
            yield return VirtuosoStoreSettings;
        }
    }
}
