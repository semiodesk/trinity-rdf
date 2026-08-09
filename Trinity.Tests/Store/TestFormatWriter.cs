using System;
using System.IO;
using System.Text;
using VDS.RDF;

namespace Semiodesk.Trinity.Tests.dotnetrdf
{
    /// <summary>
    /// A very simple custom RDF format writer. Writes a line for each triple.
    /// </summary>
    class TestFormatWriter : IRdfWriter
    {
        // Required by IRdfWriter; this writer never emits warnings, hence never raises it.
#pragma warning disable CS0067
        public event RdfWriterWarning Warning;
#pragma warning restore CS0067

        public void Save(IGraph g, string filename)
        {
            throw new NotSupportedException();
        }

        // Added by IRdfWriter in dotNetRDF 3.x.
        public void Save(IGraph g, string filename, Encoding fileEncoding)
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
