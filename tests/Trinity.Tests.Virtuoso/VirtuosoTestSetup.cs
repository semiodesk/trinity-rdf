using Semiodesk.Trinity.Store.Virtuoso;
using Semiodesk.Trinity.Tests.Store;

namespace Semiodesk.Trinity.Tests.Virtuoso
{
    // How to run:
    // 1. Create a 'openlink/virtuoso-opensource' Docker container exposing port 1111 on the host and variable DBA_PASSWORD set to 'dba'.
    public class VirtuosoTestSetup : IStoreTestSetup
    {
        #region Members

        public UriRef BaseUri => new UriRef("http://localhost:1111/graph/trinity-rdf/");
        
        public string ConnectionString => "provider=virtuoso;host=127.0.0.1;port=1111;uid=dba;pw=dba;rule=urn:semiodesk/test/ruleset";

        #endregion
        
        #region Methods

        public void LoadProvider()
        {
            StoreFactory.LoadProvider<VirtuosoStoreProvider>();
        }

        #endregion
    }
}