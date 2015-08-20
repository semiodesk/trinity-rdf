using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Semiodesk.Trinity.Query
{
    class ResourceQuerable<T> : QueryableBase<T> where T : Resource
    {
        public ResourceQuerable(IQueryParser queryParser, IQueryExecutor executor)
            : base(new DefaultQueryProvider(typeof(ResourceQuerable<>), queryParser, executor))
        {
        }

        public ResourceQuerable(IQueryProvider provider, Expression expression)
            : base(provider, expression)
        {
        }
    }
}
