using VDS.RDF.Query;

namespace Semiodesk.Trinity.Query
{
    internal class VariableBuilder
    {
        #region Members

        private int _predicateCount;

        private int _objectCount;

        #endregion

        #region Methods

        public SparqlVariable GeneratePredicateVariable(string prefix = "p")
        {
            string v = prefix + _predicateCount;

            _predicateCount += 1;

            return new SparqlVariable(v);
        }

        public SparqlVariable GenerateObjectVariable(string prefix = "o")
        {
            string v = prefix + _objectCount;

            _objectCount += 1;

            return new SparqlVariable(v);
        }

        #endregion
    }
}
