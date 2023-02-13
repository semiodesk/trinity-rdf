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
using System.Linq;

namespace Semiodesk.Trinity.Tests.Linq
{
    [RdfClass(FOAF.Agent)]
    public class Agent : Resource
    {
        #region Members

        [RdfProperty(FOAF.firstName)]
        public string FirstName { get; set; }

        [RdfProperty(FOAF.lastName)]
        public string LastName { get; set; }

        #endregion

        #region Constructors

        public Agent(Uri uri) : base(uri) { }

        #endregion
    }

    public static class AgentExtensions
    {
        /// <summary>
        /// A generic extension method used for testing purposes.
        /// </summary>
        /// <remarks>
        /// Used for a test where Remotion LINQ delivers the interface type on a MemberInfo rather 
        /// than the instance type. Since interfaces have no RdfPropertyAttributes, the SPARQL query
        /// generator failed in these cases.
        /// </remarks>
        /// <typeparam name="T">A resource which implements an interface.</typeparam>
        /// <param name="agent">An agent</param>
        /// <param name="model">The model to be queried.</param>
        /// <returns>A queryable object.</returns>
        public static IQueryable<T> GetImages<T>(this Agent agent, IModel model) where T : Resource, IImage
        {
            return model.AsQueryable<T>().Where(i => i.DepictedAgent == agent);
        }
    }
}
