using Semiodesk.Example;
using Semiodesk.Trinity;
using Semiodesk.Trinity.Store.Virtuoso;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Asp.Net_Core_Example
{
    public class DataProvider
    {

        public IStore Store { get { return StoreFactory.CreateStore(_connectionString); } }

        private string _connectionString = "provider=virtuoso;host=127.0.0.1;port=1111;uid=dba;pw=dba;rule=urn:example/ruleset";
        public  IModel DefaultModel { get { return Store.GetModel(_defaultModelUri); } }

        Uri _defaultModelUri = new Uri("http://my-default-model/");
        public DataProvider()
        {
        }

        public void Initialize()
        {
            StoreFactory.LoadProvider<VirtuosoStoreProvider>();
            Store.InitializeFromConfiguration(Path.Combine(Environment.CurrentDirectory, "ontologies.config"));
            OntologyDiscovery.AddAssembly(Assembly.GetExecutingAssembly());
            MappingDiscovery.RegisterCallingAssembly();
        }

        public IEnumerable<Patient> ListPatients()
        {
            return DefaultModel.GetResources<Patient>(true);
        }
    }
}
