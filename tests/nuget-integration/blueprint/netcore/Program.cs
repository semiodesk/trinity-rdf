using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetIntegrationTest
{
    class Program
    {
        static int Main(string[] args)
        {
            var agent = new Agent(new Uri("ex:Test"));
            agent.Name = "August";
            agent.Commit();
            // This line serves two purposes, it tests the ontology generation as well as cilg
            if (agent.ListValues(foaf.name).Count() > 0)
            {
                Console.WriteLine("true");
                return 0;
            }
            Console.WriteLine("false");
            return -1;
        }
    }
}
