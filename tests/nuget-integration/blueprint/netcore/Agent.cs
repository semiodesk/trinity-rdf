using System;
using System.Collections.Generic;
using Semiodesk.Trinity;

namespace NugetIntegrationTest
{

    [RdfClass("http://xmlns.com/foaf/0.1/Agent")]
    public class Agent : Resource
    {
        public Agent(Uri uri)
            : base(uri)
        {
        }

        [RdfProperty("http://xmlns.com/foaf/0.1/name")]
        public string Name { get; set; }

        [RdfProperty("http://xmlns.com/foaf/0.1/mbox")]
        public List<Resource> EMailAccounts { get; set; }
    }

}
