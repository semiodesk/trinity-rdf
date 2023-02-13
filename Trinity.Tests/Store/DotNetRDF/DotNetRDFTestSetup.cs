using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.DotNetRDF
{
    public class DotNetRDFTestSetup : IStoreTestSetup
    {
        #region Members

        public UriRef BaseUri => new UriRef("http://localhost/graph/trinity-rdf/");
        
        public string ConnectionString => "provider=dotnetrdf";

        #endregion
        
        #region Methods

        public void LoadProvider() { }

        #endregion
    }
}