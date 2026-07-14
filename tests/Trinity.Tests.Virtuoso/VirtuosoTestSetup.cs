using Semiodesk.Trinity.Store.Virtuoso;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Virtuoso
{
    // The Virtuoso server is started automatically for this assembly by the VirtuosoContainer
    // [SetUpFixture] via Testcontainers (Docker). No manually-provisioned server is required —
    // just a running Docker daemon (ADR-0036).
    public class VirtuosoTestSetup : IStoreTestSetup
    {
        #region Members

        public UriRef BaseUri => new UriRef("http://localhost:1111/graph/trinity-rdf/");
        
        // Points at the Dockerized Virtuoso started by the VirtuosoContainer [SetUpFixture] on a
        // random host port (ADR-0036).
        public string ConnectionString => VirtuosoContainer.ConnectionString;

        #endregion
        
        #region Methods

        public void LoadProvider()
        {
            StoreFactory.LoadProvider<VirtuosoStoreProvider>();
        }

        #endregion
    }
}