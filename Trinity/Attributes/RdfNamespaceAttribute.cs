using System;

namespace Semiodesk.Trinity
{
  [AttributeUsage(AttributeTargets.All)]
  public class RdfNamespaceAttribute : Attribute
  {
    #region Members

    public readonly Uri MappedUri;

    #endregion

    #region Constructors

    public RdfNamespaceAttribute(Uri uri)
    {
      MappedUri = uri;
    }

    public RdfNamespaceAttribute(string uriString)
    {
      MappedUri = new Uri(uriString);
    }

    #endregion
  }
}
