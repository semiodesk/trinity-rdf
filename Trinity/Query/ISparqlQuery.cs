using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    interface ISparqlQuery
    {
        #region Members

        IModel Model { get; }

        SparqlQueryType QueryType { get; }

        bool InferenceEnabled { get; set; }

        #endregion

        #region Methods

        string ToString();

        #endregion
    }
}
