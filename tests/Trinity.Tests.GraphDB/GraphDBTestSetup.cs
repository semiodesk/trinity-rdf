using Semiodesk.Trinity.Store.GraphDB;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.GraphDB
{
    // These tests were created with GraphDB version 10.1.13.
            
    // The GraphDB server is started automatically for this assembly by the GraphDBContainer
    // [SetUpFixture] via Testcontainers (Docker), which also creates the 'trinity-rdf' repository.
    // No manually-provisioned server is required — just a running Docker daemon (ADR-0036).
    public class GraphDBTestSetup : IStoreTestSetup
    {
        #region Members

        public UriRef BaseUri => new UriRef("http://localhost:7200/repository/trinity-rdf/");
        
        // Points at the Dockerized GraphDB started by the GraphDBContainer [SetUpFixture] on a
        // random host port, with the trinity-rdf repository already created (ADR-0036).
        public string ConnectionString => GraphDBContainer.ConnectionString;

        #endregion
        
        #region Methods

        public void LoadProvider()
        {
            StoreFactory.LoadProvider<GraphDBStoreProvider>();
        }

        #endregion
    }
}