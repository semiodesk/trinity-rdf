using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;



namespace Semiodesk.Trinity.Tests
{
    class TestRunner
    {
       [STAThread]
       static void Main(string[] args)
       { 
            string[] nunitArg = new string[args.Count() + 1];

            int i = 0;
            foreach (string arg in args)
            {
                nunitArg[i] = arg;
                i++;
            }

            nunitArg[i] = Assembly.GetExecutingAssembly().Location;

            if (i == 0)
            {
               // NUnit.AppEntry.Main(nunitArg);
            }
            else
            {
              //  NUnit.ConsoleRunner.Runner.Main(nunitArg);
            }
        }
    }
}
