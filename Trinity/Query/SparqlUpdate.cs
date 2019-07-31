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
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A simple SPARQL Update class. This class aims to ease formulating SPARQL
    /// query strings by automatically setting PREFIX declarations from a given
    /// namespace manager.
    /// </summary>
    public class SparqlUpdate
    {
        #region Properties

        /// <summary>
        /// Get or set the model used for this query.
        /// </summary>
        public IModel Model { get; set; }

        /// <summary>
        /// Get or set the resource being updated.
        /// </summary>
        public IResource Resource { get; set; }

        /// <summary>
        /// The SPARQL processor used to determine the prefixes and statement variables in the query.
        /// </summary>
        internal SparqlPreprocessor Preprocessor;

        /// <summary>
        /// The plain SPARQL update string.
        /// </summary>
        private string _updateString;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new SPARQL Update with an optional namespace manager instance which
        /// can be used to declare PREFIX declarations for the namespace abbreviations
        /// used in the update string.
        /// </summary>
        /// <param name="updateString">The u update string.</param>
        public SparqlUpdate(string updateString)
        {
            _updateString = updateString;

            using (TextReader reader = new StringReader(updateString))
            {
                // Parse the query for namespace prefixes and optionally remove any formatting characters.
                Preprocessor = new SparqlPreprocessor(reader, SparqlQuerySyntax.Extended);
                Preprocessor.Process(true);
            }
        }

        /// <summary>
        /// Set the value for a query parameter which is preceeded by '@'.
        /// </summary>
        /// <param name="parameter">The parameter name including the '@'.</param>
        /// <param name="value">The paramter value.</param>
        public void Bind(string parameter, object value)
        {
            if (Preprocessor == null)
            {
                throw new NotSupportedException("SPARQL query parameters can only be used with a query processor. Try using the default constructor.");
            }

            Preprocessor.Bind(parameter, value);
        }

        /// <summary>
        /// Returns the query string with generated prefixes and subsituted parameters.
        /// </summary>
        /// <returns>A valid SPARQL string.</returns>
        public override string ToString()
        {
            return Preprocessor != null ? Preprocessor.ToString() : _updateString;
        }

        #endregion
    }
}
