using System;
using System.IO;
using VDS.RDF;

namespace Semiodesk.Trinity.Tests.dotnetrdf
{
    /// <summary>
    /// A very simple custom RDF format writer. Writes a line for each triple.
    /// </summary>
    class TestFormatWriter : IRdfWriter
    {
        public event RdfWriterWarning Warning;

        public void Save(IGraph g, string filename)
        {
            throw new NotSupportedException();
        }

        public void Save(IGraph g, TextWriter output)
        {
            Save(g, output, false);
        }

        public void Save(IGraph g, TextWriter output, bool leaveOpen)
        {
            foreach(var triple in g.Triples)
            {
                var s = triple.Subject.ToString();
                var p = triple.Predicate.ToString();
                var o = triple.Object.ToString();

                output.WriteLine($"{s} {p} {o}");
            }

            if (!leaveOpen)
            {
                output.Close();
            }
        }
    }
}
