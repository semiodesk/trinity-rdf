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

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Exposes the results of a SPARQL query.
    /// </summary>
    /// <remarks>
    /// The accessor has to match the query form — this type does not adapt one form to another, and
    /// calling the wrong one yields an empty result rather than an error:
    ///
    /// <list type="table">
    ///   <item><term>SELECT</term><description><see cref="GetBindings"/>, or <see cref="GetResources()"/>
    ///   when the projection is shaped so resources can be marshalled from it</description></item>
    ///   <item><term>DESCRIBE / CONSTRUCT</term><description><see cref="GetResources()"/> or its typed
    ///   overloads</description></item>
    ///   <item><term>ASK</term><description><see cref="GetAnwser"/></description></item>
    ///   <item><term>any</term><description><see cref="Count"/></description></item>
    /// </list>
    ///
    /// The <c>offset</c>/<c>limit</c> overloads page in the store rather than in memory.
    /// </remarks>
    public interface ISparqlQueryResult : IDisposable
    {
        #region Methods

        /// <summary>
        /// Number of items in the result set.
        /// </summary>
        /// <returns></returns>
        int Count();

        /// <summary>
        /// Enumerate the resource objects in the result.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        IEnumerable<Resource> GetResources(int offset = -1, int limit = -1);

        /// <summary>
        /// Enumerate the resource objects of a given type in the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        IEnumerable<T> GetResources<T>(int offset = -1, int limit = -1) where T : Resource;

        /// <summary>
        /// Returns the bool value from ASK query forms.
        /// </summary>
        /// <remarks>
        /// The misspelling is preserved from 1.x for source compatibility; it is "answer".
        /// Meaningful only for ASK — other query forms do not produce a boolean.
        /// </remarks>
        /// <returns>True on success, False otherwise.</returns>
        bool GetAnwser();

        /// <summary>
        /// Returns marshalled Resource objects returned from DESCRIBE, CONSTRUCT 
        /// or interpretable SELECT query forms.
        /// </summary>
        /// <returns>An enumeration of Resource objects.</returns>
        IEnumerable<Resource> GetResources();

        /// <summary>
        /// Returns marshalled Resource objects returned from DESCRIBE, CONSTRUCT 
        /// or interpretable SELECT query forms.
        /// </summary>
        /// <param name="type">The type that is required.</param>
        /// <returns>An enumeration of Resource objects.</returns>
        IEnumerable<Resource> GetResources(Type type);

        /// <summary>
        /// Returns marshalled instances of the given Resource type which were 
        /// returned from DESCRIBE, CONSTRUCT or interpretable SELECT query forms.
        /// </summary>
        /// <typeparam name="T">The Resource type object.</typeparam>
        /// <returns>An enumeration of instances of the given type.</returns>
        IEnumerable<T> GetResources<T>() where T : Resource;

        /// <summary>
        /// Returns a set of bound values (bindings) returned from SELECT query forms.
        /// </summary>
        /// <returns>An enumeration of bound solution variables (BindingSet).</returns>
        IEnumerable<BindingSet> GetBindings();

        #endregion
    }
}
