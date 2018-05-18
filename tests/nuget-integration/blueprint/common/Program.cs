using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semiodesk.Trinity;

namespace NugetIntegrationTest
{
    class Program
    {
        static int Main(string[] args)
        {
            var Store = StoreFactory.CreateStore("provider=dotnetrdf");
            Uri testModel = new Uri("ex:Test");
            var Model = Store.CreateModel(testModel);

            var agentUri = new Uri("ex:Test");
            var agent = Model.CreateResource<Agent>(agentUri);
            agent.Name = "August";
            agent.Commit();

            var result = Model.GetResource<Agent>(agentUri);
            // This line serves two purposes, it tests the ontology generation as well as cilg
            if (result.ListValues(foaf.name).Count() > 0)
            {
                Console.WriteLine("true");
                return 0;
            }
            Console.WriteLine("false");
            return -1;
        }
    }
}
