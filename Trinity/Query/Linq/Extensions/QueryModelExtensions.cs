using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using System.Linq;

namespace Semiodesk.Trinity.Query
{
    internal static class QueryModelExtensions
    {
        public static bool HasNumericResultOperator(this QueryModel queryModel)
        {
            return queryModel.ResultOperators.Any(op => IsNumericResultOperator(op));
        }

        private static bool IsNumericResultOperator(ResultOperatorBase op)
        {
            if (op is SumResultOperator
                || op is CountResultOperator
                || op is LongCountResultOperator
                || op is AverageResultOperator
                || op is MinResultOperator
                || op is MaxResultOperator)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
