using System;

namespace Semiodesk.Trinity
{
  [AttributeUsage(AttributeTargets.Class)]
  public class RdfClassAttribute : Attribute
  {
    #region Members

    public readonly Uri MappedUri;

    #endregion

    #region Constructors

    public RdfClassAttribute(Uri uri)
    {
      MappedUri = uri;
    }

    public RdfClassAttribute(string uriString)
    {
      MappedUri = new Uri(uriString);
    }

    public RdfClassAttribute(Uri baseUri, string relativeUri)
    {
      MappedUri = new Uri(baseUri, relativeUri);
    }

    public RdfClassAttribute(Uri baseUri, Uri relativeUri)
    {
      MappedUri = new Uri(baseUri, relativeUri);
    }

    public RdfClassAttribute(IResource resource)
    {
      MappedUri = resource.Uri;
    }

    #endregion
  }
}
