using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Semiodesk.Trinity
{
        [XmlRoot(ElementName = "ontology")]
        public class OntologyConfiguration
        {
            [XmlElement(ElementName = "filesource")]
            public FileSource FileSource { get; set; }
            [XmlAttribute(AttributeName = "uri")]
            public string Uri { get; set; }
            [XmlAttribute(AttributeName = "prefix")]
            public string Prefix { get; set; }
            [XmlAttribute(AttributeName = "meta")]
            public string Meta { get; set; }
        }

        [XmlRoot(ElementName = "filesource")]
        public class FileSource
        {
            [XmlAttribute(AttributeName = "location")]
            public string Location { get; set; }
        }

        [XmlRoot(ElementName = "ontologies")]
        public class Ontologies
        {
            [XmlElement(ElementName = "ontology")]
            public List<OntologyConfiguration> Ontology { get; set; }
            [XmlAttribute(AttributeName = "namespace")]
            public string Namespace { get; set; }
        }

        [XmlRoot(ElementName = "graph")]
        public class GraphConfiguration
        {
            [XmlAttribute(AttributeName = "Uri")]
            public string Uri { get; set; }
        }

        [XmlRoot(ElementName = "graphs")]
        public class Graphs
        {
            [XmlElement(ElementName = "graph")]
            public List<GraphConfiguration> Graph { get; set; }
        }

        [XmlRoot(ElementName = "ruleset")]
        public class Ruleset
        {
            [XmlElement(ElementName = "graphs")]
            public Graphs Graphs { get; set; }
            [XmlAttribute(AttributeName = "uri")]
            public string Uri { get; set; }
        }

        [XmlRoot(ElementName = "rulesets")]
        public class Rulesets
        {
            [XmlElement(ElementName = "ruleset")]
            public Ruleset Ruleset { get; set; }
        }

        [XmlRoot(ElementName = "store")]
        public class StoreConfiguration
        {
            [XmlElement(ElementName = "rulesets")]
            public Rulesets Rulesets { get; set; }
            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }
        }

        [XmlRoot(ElementName = "stores")]
        public class Stores
        {
            [XmlElement(ElementName = "store")]
            public List<StoreConfiguration> Store { get; set; }
        }

        [XmlRoot(ElementName = "configuration")]
        public class Config
        {
            [XmlElement(ElementName = "ontologies")]
            public Ontologies Ontologies { get; set; }
            [XmlElement(ElementName = "stores")]
            public Stores Stores { get; set; }
        }

}
