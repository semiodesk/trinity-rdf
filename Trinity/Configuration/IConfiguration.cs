using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
{
    public interface IConfiguration
    {
        string Namespace { get; }
        IEnumerable<IOntologyConfiguration> ListOntologies();
    }
}
