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

using System.Collections.Generic;

namespace Semiodesk.Trinity.Configuration
{
    /// <summary>
    /// Exposes settings for Trinity RDF projects.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Get the default namespace for generated C# classes.
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// Get the ontology settings for the current project.
        /// </summary>
        /// <returns>An enumeration of ontology settings.</returns>
        IEnumerable<IOntologyConfiguration> ListOntologies();

        /// <summary>
        /// Get the triple store settings for the current project.
        /// </summary>
        /// <returns>An enumeration of triple store settings.</returns>
        IEnumerable<IStoreConfiguration> ListStoreConfigurations();
    }
}
