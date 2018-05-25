using System;
using System.Linq;

namespace NetCore_Test
{
    class Program
    {
        static int Main(string[] args)
        {
            var agent = new Agent(new Uri("ex:Test"));
            agent.Name = "August";
            agent.Interests.Add("Fishing");
            agent.Commit();
     
            if (agent.ListValues(foaf.name).Count() <= 0)
            {
                Console.WriteLine("false");
                return -1;
            }
            if (agent.ListValues(foaf.interest).Count() <= 0)
            {
                Console.WriteLine("false");
                return -1;
            }
            if (agent.ListValues(foaf.birthday).Count() <= 0)
            {
                Console.WriteLine("false");
                return -1;
            }
            Console.WriteLine("true");
            return 0;
        }
    }
}
