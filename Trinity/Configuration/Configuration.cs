using System;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Xml;

namespace Semiodesk.Trinity.Configuration
{
    [XmlRoot(ElementName = "filesource")]
    public class FileSource
    {
        [XmlAttribute(AttributeName = "location")]
        public string Location { get; set; }
    }

    [XmlRoot(ElementName = "ontology")]
    public class Ontology
    {
        [XmlAttribute(AttributeName = "uri")]
        public string _uri { get; set; }

        public Uri Uri { get { return new Uri(_uri); } }
        
        [XmlAttribute(AttributeName = "prefix")]
        public string Prefix { get; set; }

        [XmlAttribute(AttributeName = "timestamp")]
        public DateTime Timestamp { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
        
        [XmlElement(ElementName = "websource")]
        public WebSource WebSource { get; set; }

        [XmlElement(ElementName = "filesource")]
        public FileSource FileSource { get; set; }

        public override string ToString()
        {
            return _uri;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [XmlRoot(ElementName = "websource")]
    public class WebSource
    {
        [XmlAttribute(AttributeName = "location")]
        public string Location { get; set; }
    }

    [XmlRoot(ElementName = "ontologies")]
    public class Ontologies
    {
        [XmlElement(ElementName = "ontology")]
        public List<Ontology> OntologyList { get; set; }
        [XmlAttribute(AttributeName = "namespace")]
        public string Namespace { get; set; }
    }


    [XmlRoot(ElementName = "store")]
    public class StoreConfiguration
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlElement(ElementName="data")]
        public XElement Data { get; set; }
    }

    [XmlRoot(ElementName = "stores")]
    public class Stores
    {
        [XmlElement(ElementName = "store")]
        public StoreConfiguration Store { get; set; }
    }

    [XmlRoot(ElementName = "configuration")]
    public class OntologyConfiguration
    {
        [XmlElement(ElementName = "ontologies")]
        public Ontologies Ontologies { get; set; }
        [XmlElement(ElementName = "stores")]
        public Stores Stores { get; set; }
    }


}
