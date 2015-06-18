using System;

namespace Semiodesk.Trinity
{
  [AttributeUsage(AttributeTargets.Property)]
  public class RdfPropertyAttribute : Attribute
  {
    #region Members

    public readonly Uri MappedUri;

    #endregion

    #region Constructors

    public RdfPropertyAttribute(Uri uri)
    {
      MappedUri = uri;
    }

    public RdfPropertyAttribute(string uriString)
    {
      MappedUri = new Uri(uriString);
    }

    public RdfPropertyAttribute(Uri baseUri, string relativeUri)
    {
      MappedUri = new Uri(baseUri, relativeUri);
    }

    public RdfPropertyAttribute(Uri baseUri, Uri relativeUri)
    {
      MappedUri = new Uri(baseUri, relativeUri);
    }

    public RdfPropertyAttribute(IResource resource)
    {
      MappedUri = resource.Uri;
    }

    #endregion
  }
}
