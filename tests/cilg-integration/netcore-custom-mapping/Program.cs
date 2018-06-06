﻿using System;
using System.Linq;

namespace NetCore_Test
{
    class Program
    {
        static int Main(string[] args)
        {
            var agent = new Agent(new Uri("ex:Test"));
            agent.Name = "August";
            agent.Commit();
            agent.GetTypes();
            if (agent.ListValues(foaf.name).Count() > 0 && agent.Changed == true)
            {
                Console.WriteLine("true");
                return 0;
            }
            Console.WriteLine("false");
            return -1;
        }
    }
}
