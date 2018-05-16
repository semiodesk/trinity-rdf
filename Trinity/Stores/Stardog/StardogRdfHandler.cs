using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;

namespace Semiodesk.Trinity.Store.Stardog
{
    class StardogRdfHandler : BaseRdfHandler
    {
        public override bool AcceptsAll
        {
            get { return true;  }
        }

        public bool HandleBaseUri(Uri baseUri)
        {
            return true;
        }

        public bool HandleNamespace(string prefix, Uri namespaceUri)
        {
            return true;
        }

        public bool HandleTriple(Triple t)
        {
            return true;
        }

        public void StartRdf()
        {
        }

        public void EndRdf(bool ok)
        {
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            return true;
        }
    }

    class StardogResultHandler : BaseResultsHandler
    {
        public bool BoolResult { get; set; }

        public SparqlResultSet SparqlResultSet { get { return new SparqlResultSet(_results); } }

        private List<SparqlResult> _results = new List<SparqlResult>();

        public StardogResultHandler()
        {
        }

        protected override void HandleBooleanResultInternal(bool result)
        {
            BoolResult = result;
        }

        protected override bool HandleResultInternal(VDS.RDF.Query.SparqlResult result)
        {
            _results.Add(result);

            return true;
        }

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }

        public bool GetAnwser()
        {
            return BoolResult;
        }
    }
}
