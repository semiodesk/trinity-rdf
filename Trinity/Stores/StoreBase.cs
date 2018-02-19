using Semiodesk.Trinity.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Semiodesk.Trinity
{
    public abstract class StoreBase : IStore
    {
        #region Methods

        public virtual bool IsReady { get { return true; } }

        public abstract void RemoveModel(Uri uri);

        public virtual void RemoveModel(IModel model)
        {
            RemoveModel(model.Uri);
        }

        public abstract bool ContainsModel(Uri uri);

        public virtual bool ContainsModel(IModel model)
        {
            return ContainsModel(model.Uri);
        }

        public abstract IEnumerable<IModel> ListModels();

        public abstract ISparqlQueryResult ExecuteQuery(ISparqlQuery query, ITransaction transaction = null);

        public abstract void ExecuteNonQuery(SparqlUpdate update, ITransaction transaction = null);

        public abstract ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel);

        public abstract Uri Read(Uri modelUri, Uri url, RdfSerializationFormat format, bool update);

        public abstract Uri Read(System.IO.Stream stream, Uri graphUri, RdfSerializationFormat format, bool update);

        public abstract void Write(System.IO.Stream fs, Uri graphUri, RdfSerializationFormat format);

        public virtual void LoadOntologies(string configPath = null, string sourceDir = null)
        {
            var config = LoadConfiguration(configPath);
            LoadOntologies(config, sourceDir);
        }

        protected OntologyConfiguration LoadConfiguration(string configPath = null)
        {
            FileInfo configFile;
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                // Take the provided config file.
                configFile = new FileInfo(configPath);
            }
            else
            {
                // Load the default configuration file.
                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ontologies.config");
                configFile = new FileInfo(path);
            }

            var settings = Configuration.ConfigurationLoader.LoadConfiguration(configFile);

            
            return settings;
        }

        protected void LoadOntologies(OntologyConfiguration configuration, string sourceDir = null) 
        {
            DirectoryInfo srcDir;
            if (string.IsNullOrEmpty(sourceDir))
            {
                srcDir = new DirectoryInfo(Environment.CurrentDirectory);
            }
            else
            {
                srcDir = new DirectoryInfo(sourceDir);
            }

            StoreUpdater updater = new StoreUpdater(this, srcDir);

            updater.UpdateOntologies(configuration.Ontologies.OntologyList);

        }

        public abstract void Dispose();

        public virtual IModel CreateModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        public virtual IModel GetModel(Uri uri)
        {
            return new Model(this, new UriRef(uri));
        }

        public virtual IModelGroup CreateModelGroup(params Uri[] models)
        {
            List<IModel> result = new List<IModel>();

            foreach (var model in models)
            {
                result.Add(GetModel(model));
            }

            return ModelGroupFactory.CreateModelGroup(this, result);
        }

        #endregion

    
    }
}
