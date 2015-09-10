using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{

    /// <summary>
    /// Enumerates all supported RDF serialization formats.
    /// </summary>
    public enum RdfSerializationFormat
    {
        /// <summary>
        /// RDF/XML <see href="http://www.w3.org/TR/REC-rdf-syntax/">http://www.w3.org/TR/REC-rdf-syntax/</see>
        /// </summary>
        RdfXml,
        /// <summary>
        /// N3 <see href="http://www.w3.org/TeamSubmission/n3/">http://www.w3.org/TeamSubmission/n3/</see>
        /// </summary>
        N3,
        /// <summary>
        /// NTriples <see href="http://www.w3.org/2001/sw/RDFCore/ntriples/">http://www.w3.org/2001/sw/RDFCore/ntriples/</see>
        /// </summary>
        NTriples,
        /// <summary>
        /// TriG <see href="http://www.w3.org/TR/trig/">http://www.w3.org/TR/trig/</see>
        /// </summary>
        Trig,
        /// <summary>
        /// Turtle <see href="http://www.w3.org/TR/turtle/">http://www.w3.org/TR/turtle/</see>
        /// </summary>
        Turtle
    };
}
