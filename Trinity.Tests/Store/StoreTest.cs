using System;
using System.IO;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Store
{
    [TestFixture]
    public abstract class StoreTest<T> where T : IStoreTestSetup
    {
        #region Members

        protected string ConnectionString;

        protected Uri BaseUri;
        
        protected IStore Store;
        
        protected IModel Model1;

        protected IModel Model2;

        #endregion
        
        #region Methods

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var environment = (IStoreTestSetup)Activator.CreateInstance(typeof(T));

            BaseUri = environment.BaseUri;
            ConnectionString = environment.ConnectionString;
            
            environment.LoadProvider();

            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());
            OntologyDiscovery.AddAssembly(typeof(AbstractMappingClass).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(AbstractMappingClass).Assembly);

            var location = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var folder = new DirectoryInfo(Path.Combine(location.DirectoryName, "nunit"));

            if (folder.Exists)
            {
                folder.Delete(true);
            }

            folder.Create();
            
            Store = StoreFactory.CreateStore(ConnectionString);
            Store.InitializeFromConfiguration();
            
            // Wait until the inference engine has loaded the ontologies..
            Thread.Sleep(1000);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Store.Dispose();
        }
        
        [SetUp]
        public virtual void SetUp()
        {
            Model1 = Store.GetModel(BaseUri.GetUriRef("model1"));
            
            if (!Model1.IsEmpty) Model1.Clear();
            
            Model2 = Store.GetModel(BaseUri.GetUriRef("model2"));
            
            if (!Model2.IsEmpty) Model2.Clear();
        }
        
        [TearDown]
        public void TearDown()
        {
            Model1.Clear();
            Model2.Clear();
        }

        protected Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            
            stream.Position = 0;
            
            return stream;
        }
        
        #endregion
    }
}