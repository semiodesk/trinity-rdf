using VDS.RDF.Query;
using VDS.RDF.Query.Aggregates;

namespace Semiodesk.Trinity.Query
{
    internal static class SparqlVariableExtensions
    {
        public static string GetProjectedName(this SparqlVariable variable)
        {
            if (variable.IsAggregate)
            {
                return variable.Aggregate.GetProjectedName(variable.Name);
            }
            else
            {
                return variable.Name;
            }
        }

        public static string GetProjectedName(this ISparqlAggregate aggregate, string variableName)
        {
            string name = variableName;
            string functor = aggregate.Functor.ToLowerInvariant();

            return string.Format("{0}_{1}", name, functor);
        }
    }
}
