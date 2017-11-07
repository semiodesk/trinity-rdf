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

using Remotion.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Semiodesk.Trinity.Query
{
    internal class ResourceQueryExecutor : IQueryExecutor
    {
        #region Members
        
        protected IModel Model { get; private set; }

        // A handle to the generic version of the GetResources method which is being used
        // for implementing the ExecuteCollection(QueryModel) method that supports runtime type specification.
        private MethodInfo _getResourceMethod;

        #endregion

        #region Constructors

        public ResourceQueryExecutor(IModel model)
        {
            Model = model;

            // Searches for the generic method IEnumerable<T> GetResources<T>(ResourceQuery) and saves a handle
            // for later use within ExecuteCollection(QueryModel);
            _getResourceMethod = model.GetType().GetMethods().FirstOrDefault(m => m.IsGenericMethod && m.Name == "GetResources" && m.GetParameters().Any(p => p.ParameterType == typeof(ISparqlQuery)));
        }
        
        #endregion

        #region Methods

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            MethodInfo getResources = _getResourceMethod.MakeGenericMethod(typeof(T));

            if (getResources != null)
            {
                QueryModelVisitor visitor = new QueryModelVisitor();
                visitor.VisitQueryModel(queryModel);

                object[] args = new object[] { visitor.GetQuery(), false, null };

                IEnumerable<T> result = getResources.Invoke(Model, args) as IEnumerable<T>;

                return result;
            }
            else
            {
                string msg = string.Format("The LINQ result type {0} is currently not supported.", typeof(T));
                throw new NotSupportedException(msg);
            }
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var sequence = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            // We'll get to this one later...
            throw new NotImplementedException();
        }

        #endregion
    }
}
