using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
{
    public interface IOntologyConfiguration
    {
        Uri Uri { get; }
        string Prefix { get; }
        string Location { get; }
    }
}
