namespace Semiodesk.Trinity.Tests.Store
{
    public interface IStoreTestSetup
    {
        #region Members
        
        UriRef BaseUri { get; }
        
        string ConnectionString { get; }
        
        #endregion
        
        #region Methods

        void LoadProvider();

        #endregion
    }
}