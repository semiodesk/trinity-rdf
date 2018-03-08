using System;
using System.Collections.Generic;
using Semiodesk.Trinity;

namespace NetCore_Test
{
    [RdfClass(FOAF.Agent)]
    public class Agent : Resource
    {
        public Agent(Uri uri)
            : base(uri)
        {
        }

        [RdfProperty(FOAF.name)]
        public string Name { get; set; }

        [RdfProperty(FOAF.mbox)]
        public List<Resource> EMailAccounts { get; set; }

    }

}
