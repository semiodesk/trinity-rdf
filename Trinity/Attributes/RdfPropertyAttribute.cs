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

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Decorate a property with this attribute to mark it as mapped RDF property with the given type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RdfPropertyAttribute : Attribute
    {
        #region Members
        /// <summary>
        /// Uri of the the RDF property
        /// </summary>
        public readonly UriRef MappedUri;

        /// <summary>
        /// No longer has any effect. The property's declared type decides how language tags are handled.
        /// </summary>
        [Obsolete(LanguageInvariantMessage)]
        public bool LanguageInvariant;

        internal const string LanguageInvariantMessage =
            "languageInvariant no longer has any effect. A string property is language-invariant by " +
            "construction; declare LocalizedString or LocalizedStringCollection for language-tagged " +
            "values, or LangString for the raw tagged literal. This parameter is removed in 2.1.";

        #endregion

        #region Constructors

        /// <summary>
        /// Maps a property to an RDF predicate.
        /// </summary>
        /// <param name="uriString">There uri of the rdf property for this mapping.</param>
        public RdfPropertyAttribute(string uriString)
        {
            MappedUri = new UriRef(uriString);
        }

        /// <summary>
        /// Maps a property to an RDF predicate, with the obsolete language-invariance flag.
        /// </summary>
        /// <remarks>
        /// Kept for one release so that 1.x call sites get an explanation rather than CS1501. The flag
        /// is redundant now: a <c>string</c> property is language-invariant by construction, and a
        /// language-tagged one declares a container that carries its own tags (ADR-0048). The old flag
        /// turns out to be an exact partition of the migration - <c>true</c> stays <c>string</c>,
        /// everything else string-typed becomes <c>LocalizedString</c>.
        /// </remarks>
        /// <param name="uriString">There uri of the rdf property for this mapping.</param>
        /// <param name="languageInvariant">Ignored.</param>
        [Obsolete(LanguageInvariantMessage)]
        public RdfPropertyAttribute(string uriString, bool languageInvariant)
        {
            MappedUri = new UriRef(uriString);
        }


        #endregion
    }
}
