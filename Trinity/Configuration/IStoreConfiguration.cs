using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Semiodesk.Trinity.Configuration
{
    public interface IStoreConfiguration
    {
        string Type { get; }

        XElement Data { get; }
    }
}
