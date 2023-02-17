using Semiodesk.Trinity.Store.GraphDB;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.GraphDB
{
    // These tests were created with GraphDB version 10.1.13.
            
    // How to run:
    // 1. Create a Docker container of 'ontotext/graphdb' exposing port 7200 on the host.
    // 2. Create a repository named 'trinity-rdf'.
    // 3. Assign the repository with full privileges to a user 'trinity' with password 'test'.
    public class GraphDBTestSetup : IStoreTestSetup
    {
        #region Members

        public UriRef BaseUri => new UriRef("http://localhost:7200/repository/trinity-rdf/");
        
        public string ConnectionString => "provider=graphdb;host=http://localhost:7200;uid=trinity;pw=test;repository=trinity-rdf";

        #endregion
        
        #region Methods

        public void LoadProvider()
        {
            StoreFactory.LoadProvider<GraphDBStoreProvider>();
        }

        #endregion
    }
}