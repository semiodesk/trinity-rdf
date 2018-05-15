using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


        protected override bool HandleTripleInternal(Triple t)
        {
            return true;
        }
    }

    class StardogResultHandler : BaseResultsHandler
    {
        public StardogResultHandler()
        {
        }
        public bool BoolResult { get; set; }
        public SparqlResultSet SparqlResultSet { get { return new SparqlResultSet(_results); } }
        private List<SparqlResult> _results = new List<SparqlResult>();


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
