using System;
using System.Linq;

namespace netstandard20
{
    public class TestMain
    {
        public int Run()
        {
            var agent = new Agent(new Uri("ex:Test"));
            agent.Name = "August";
            agent.Commit();
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
