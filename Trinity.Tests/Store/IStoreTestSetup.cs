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

        /// <summary>
        /// Called once after <c>TestOntologies</c> has seeded the schema graphs, before any fixture
        /// runs. Defaults to doing nothing; a store overrides it when some provisioning step can
        /// only happen once the schema is actually in the store.
        /// </summary>
        /// <remarks>
        /// Virtuoso is the reason this exists: its inference rule set is built from a snapshot of the
        /// schema graphs taken when <c>rdfs_rule_set</c> is called, so registering it at container
        /// start — before anything is seeded — produces a rule set with no rules, and every
        /// <c>inferenceEnabled</c> query silently returns nothing.
        /// </remarks>
        /// <param name="store">The seeded store.</param>
        void AfterSeed(IStore store)
        {
        }

        #endregion
    }
}