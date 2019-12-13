using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Exposes a dictionary which maps prefixes to namespace URIs.
    /// </summary>
    public interface INamespaceMap : IDictionary<string, Uri> {}

    /// <summary>
    /// A dictionary which maps prefixes to namespace URIs.
    /// </summary>
    public class NamespaceMap : Dictionary<string, Uri>, INamespaceMap {}
}
