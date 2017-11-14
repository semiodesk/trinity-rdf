using System.Linq;
using VDS.RDF.Query;
using VDS.RDF.Query.Aggregates;

namespace Semiodesk.Trinity.Query
{
    internal static class AggregateExtensions
    {
        public static SparqlVariable AsSparqlVariable(this ISparqlAggregate aggregate)
        {
            string variableName = aggregate.Expression.Variables.First();

            return new SparqlVariable(aggregate.GetProjectedName(variableName), aggregate);
        }
    }
}
