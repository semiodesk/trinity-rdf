using System;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Xml;

namespace Semiodesk.Trinity.Configuration
{
    /// <summary>
    /// The FileSource represents the path of the ontology on the disk.
    /// </summary>
    [XmlRoot(ElementName = "filesource")]
    public class FileSource
    {
        /// <summary>
        /// This is the string containing the path.
        /// </summary>
        [XmlAttribute(AttributeName = "location")]
        public string Location { get; set; }
    }

    /// <summary>
    /// The ontology configuration section.
    /// </summary>
    [XmlRoot(ElementName = "ontology")]
    public class Ontology : IOntologyConfiguration
    {
        /// <summary>
        /// The uri of the ontology as string.
        /// </summary>
        [XmlAttribute(AttributeName = "uri")]
        public string _uri { get; set; }

        /// <summary>
        /// Wrapper for the uri of the ontology as Uri.
        /// </summary>
        public Uri Uri { get { return new Uri(_uri); } }
        
        /// <summary>
        /// The prefix of the ontology.
        /// </summary>
        [XmlAttribute(AttributeName = "prefix")]
        public string Prefix { get; set; }

        /// <summary>
        /// The timestamp when the ontology was first introduced in the project.
        /// </summary>
        [XmlAttribute(AttributeName = "timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Version of the ontology.
        /// </summary>
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
        
        /// <summary>
        /// The location of the ontology file in the web.
        /// </summary>
        [XmlElement(ElementName = "websource")]
        public WebSource WebSource { get; set; }

        /// <summary>
        /// The location of the ontology file on the disk.
        /// </summary>
        [XmlElement(ElementName = "filesource")]
        public FileSource FileSource { get; set; }

        /// <summary>
        /// Serialization of th ontology.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _uri;
        }

        /// <summary>
        /// Hashcode of the ontology.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _uri.GetHashCode();
        }

        /// <summary>
        /// Wrapper for the local path of the ontology.
        /// </summary>
        public string Location
        {
            get
            { 
                if (FileSource != null) 
                    return FileSource.Location; 
                return null; 
            }
        }
    }

    /// <summary>
    /// Location of the ontology in the web.
    /// </summary>
    [XmlRoot(ElementName = "websource")]
    public class WebSource
    {
        /// <summary>
        /// The url of the ontology in the web.
        /// </summary>
        [XmlAttribute(AttributeName = "location")]
        public string Location { get; set; }
    }

    /// <summary>
    /// The section containing all ontologies.
    /// </summary>
    [XmlRoot(ElementName = "ontologies")]
    public class Ontologies
    {
        /// <summary>
        /// The list of all ontologies
        /// </summary>
        [XmlElement(ElementName = "ontology")]
        public List<Ontology> OntologyList { get; set; }

        /// <summary>
        /// The namespace the ontologies should be generated to.
        /// </summary>
        [XmlAttribute(AttributeName = "namespace")]
        public string Namespace { get; set; }
    }

    /// <summary>
    /// The general store configuration section.
    /// </summary>
    [XmlRoot(ElementName = "store")]
    public class StoreConfiguration : IStoreConfiguration
    {
        /// <summary>
        /// The store type this configuration belongs to.
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// The content of the store configuration. Will be handled by the store implementation.
        /// </summary>
        [XmlElement(ElementName="data")]
        public XElement Data { get; set; }
    }

    /// <summary>
    /// The store section of the configuration.
    /// </summary>
    [XmlRoot(ElementName = "stores")]
    public class Stores
    {
        /// <summary>
        /// The list of the store configurations.
        /// </summary>
        [XmlElement(ElementName = "store")]
        public List<StoreConfiguration> StoreList { get; set; }
    }

    /// <summary>
    /// The general configuration section.
    /// </summary>
    [XmlRoot(ElementName = "configuration")]
    public class Configuration : IConfiguration
    {
        /// <summary>
        /// The ontology section.
        /// </summary>
        [XmlElement(ElementName = "ontologies")]
        public Ontologies Ontologies { get; set; }

        /// <summary>
        /// The store section.
        /// </summary>
        [XmlElement(ElementName = "stores")]
        public Stores Stores { get; set; }

        /// <summary>
        /// Wrapper for the namespace for this project.
        /// </summary>
        public string Namespace
        {
            get 
            {
                if( Ontologies != null ) 
                    return Ontologies.Namespace;
                return null;
            }
        }
        
        /// <summary>
        /// Wrapper for an easier ontology access.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IOntologyConfiguration> ListOntologies()
        {
#if NET35
            foreach( var x in Ontologies.OntologyList )
                yield return x;
#else
            return Ontologies.OntologyList;
#endif
        }

        /// <summary>
        /// Wrapper for the store configurations.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IStoreConfiguration> ListStoreConfigurations()
        {
#if NET35
            foreach( var x in Stores.StoreList )
                yield return x;
#else
            return Stores.StoreList;
#endif
        }
    }


}
