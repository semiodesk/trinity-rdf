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
using System.Linq.Expressions;

namespace Semiodesk.Trinity.Query
{
    class QueryModelVisitor : QueryModelVisitorBase
    {
        #region Members

        Dictionary<Type, ResourceQuery> _queryType = new Dictionary<Type, ResourceQuery>();

        Dictionary<Type, Resource> _instances = new Dictionary<Type, Resource>();

        public List<IPropertyMapping> PropertyMappings;

        #endregion

        #region Methods

        public override void VisitMainFromClause(Remotion.Linq.Clauses.MainFromClause fromClause, QueryModel queryModel)
        {
            IList<Class> classes = MappingDiscovery.GetRdfClasses(fromClause.ItemType);

            ResourceQuery query = new ResourceQuery(classes);
            Resource type = (Resource)Activator.CreateInstance(fromClause.ItemType, new UriRef("semio:empty"));

            _queryType.Add(fromClause.ItemType, query);
            _instances.Add(fromClause.ItemType, type);

            //PropertyMappings = MappingDiscovery.ListMappings()
        }

        protected override void VisitBodyClauses(Remotion.Linq.Collections.ObservableCollection<Remotion.Linq.Clauses.IBodyClause> bodyClauses, QueryModel queryModel)
        {
            base.VisitBodyClauses(bodyClauses, queryModel);
        }

        public override void VisitWhereClause(Remotion.Linq.Clauses.WhereClause whereClause, QueryModel queryModel, int index)
        {
            ExpressionVisitor visitor = new ExpressionVisitor(this);
            visitor.VisitExpression(whereClause.Predicate);

            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitSelectClause(Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel)
        {
            base.VisitSelectClause(selectClause, queryModel);
        }

        public ResourceQuery GetResourceQuery(Expression exp)
        {
            ResourceQuery query = null;

            _queryType.TryGetValue(exp.Type, out query);

            return query;
        }

        public IPropertyMapping GetMapping(Type type, string propertyName)
        {
            Resource resource = null;

            _instances.TryGetValue(type, out resource);

            return resource.GetPropertyMapping(propertyName);
        }

        #endregion
    }
}
