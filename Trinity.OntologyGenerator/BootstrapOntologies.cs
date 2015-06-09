/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) Semiodesk GmbH 2015

Authors:
Moritz Eberl <moritz@semiodesk.com>
Sebastian Faubel <sebastian@semiodesk.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semiodesk.Trinity;

namespace Semiodesk.Trinity.OntologyGenerator
{
    static class nao
    {
        public static Property hasdefaultnamespaceabbreviation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasDefaultNamespaceAbbreviation"));
        public static Property hasdefaultnamespace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasDefaultNamespace"));
    }

    static class dces
    {
        public static Property Title = new Property(new Uri("http://purl.org/dc/elements/1.1/title"));
        public static Property Description = new Property(new Uri("http://purl.org/dc/elements/1.1/description"));
    }

    ///<summary>
    ///The RDF Vocabulary (RDF)
    ///This is the RDF Schema for the RDF vocabulary defined in the RDF namespace.
    ///</summary>
    public class rdf : Semiodesk.Trinity.Ontology
    {
        public static readonly Uri Namespace = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        public static Uri GetNamespace() { return Namespace; }

        public static readonly string Prefix = "rdf";
        public static string GetPrefix() { return Prefix; }

        ///<summary>
        ///The subject is an instance of a class.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#type"/>
        ///</summary>
        public static readonly Property type = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

        ///<summary>
        ///The class of RDF properties.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#Property"/>
        ///</summary>
        public static readonly Class Property = new Class(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#Property"));

        ///<summary>
        ///The first item in the subject RDF list.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#first"/>
        ///</summary>
        public static readonly Property first = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#first"));

        ///<summary>
        ///The rest of the subject RDF list after the first item.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#rest"/>
        ///</summary>
        public static readonly Property rest = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#rest"));

        ///<summary>
        ///The empty list, with no items in it. If the rest of a list is nil then the list has no more items in it.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#nil"/>
        ///</summary>
        public static readonly Resource nil = new Resource(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#nil"));

        ///<summary>
        ///The class of RDF Lists.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#List"/>
        ///</summary>
        public static readonly Class List = new Class(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#List"));

        ///<summary>
        ///The class of RDF statements.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#Statement"/>
        ///</summary>
        public static readonly Class Statement = new Class(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#Statement"));

        ///<summary>
        ///The subject of the subject RDF statement.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#subject"/>
        ///</summary>
        public static readonly Property subject = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#subject"));

        ///<summary>
        ///The object of the subject RDF statement.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#object"/>
        ///</summary>
        public static readonly Property _object = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#object"));

        ///<summary>
        ///The class of unordered containers.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#Bag"/>
        ///</summary>
        public static readonly Class Bag = new Class(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#Bag"));

        ///<summary>
        ///The class of ordered containers.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#Seq"/>
        ///</summary>
        public static readonly Class Seq = new Class(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#Seq"));

        ///<summary>
        ///The class of XML literal values.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral"/>
        ///</summary>
        public static readonly Resource XMLLiteral = new Resource(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral"));

        ///<summary>
        ///The predicate of the subject RDF statement.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#predicate"/>
        ///</summary>
        public static readonly Property predicate = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#predicate"));

        ///<summary>
        ///The class of containers of alternatives.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#Alt"/>
        ///</summary>
        public static readonly Class Alt = new Class(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#Alt"));

        ///<summary>
        ///Idiomatic property used for structured values.
        ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#value"/>
        ///</summary>
        public static readonly Property value = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#value"));
    }

    ///<summary>
    ///The RDF Schema vocabulary (RDFS)
    ///
    ///</summary>
    public class rdfs : Semiodesk.Trinity.Ontology
    {
        public static readonly Uri Namespace = new Uri("http://www.w3.org/2000/01/rdf-schema#");
        public static Uri GetNamespace() { return Namespace; }

        public static readonly string Prefix = "rdfs";
        public static string GetPrefix() { return Prefix; }

        ///<summary>
        ///A human-readable name for the subject.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#label"/>
        ///</summary>
        public static readonly Property label = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#label"));

        ///<summary>
        ///A domain of the subject property.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#domain"/>
        ///</summary>
        public static readonly Property domain = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#domain"));

        ///<summary>
        ///A range of the subject property.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#range"/>
        ///</summary>
        public static readonly Property range = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#range"));

        ///<summary>
        ///The defininition of the subject resource.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#isDefinedBy"/>
        ///</summary>
        public static readonly Property isDefinedBy = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#isDefinedBy"));

        ///<summary>
        ///A description of the subject resource.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#comment"/>
        ///</summary>
        public static readonly Property comment = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#comment"));

        ///<summary>
        ///The class of classes.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#Class"/>
        ///</summary>
        public static readonly Class Class = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#Class"));

        ///<summary>
        ///The subject is a subclass of a class.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#subClassOf"/>
        ///</summary>
        public static readonly Property subClassOf = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#subClassOf"));

        ///<summary>
        ///The subject is a subproperty of a property.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#subPropertyOf"/>
        ///</summary>
        public static readonly Property subPropertyOf = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#subPropertyOf"));

        ///<summary>
        ///Further information about the subject resource.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#seeAlso"/>
        ///</summary>
        public static readonly Property seeAlso = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#seeAlso"));

        ///<summary>
        ///The class resource, everything.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#Resource"/>
        ///</summary>
        public static readonly Class Resource = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#Resource"));

        ///<summary>
        ///The class of RDF datatypes.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#Datatype"/>
        ///</summary>
        public static readonly Class Datatype = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#Datatype"));

        ///<summary>
        ///The class of container membership properties, rdf:_1, rdf:_2, ..., all of which are sub-properties of 'member'.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#ContainerMembershipProperty"/>
        ///</summary>
        public static readonly Class ContainerMembershipProperty = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#ContainerMembershipProperty"));

        ///<summary>
        ///A member of the subject resource.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#member"/>
        ///</summary>
        public static readonly Property member = new Property(new Uri("http://www.w3.org/2000/01/rdf-schema#member"));

        ///<summary>
        ///The class of RDF containers.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#Container"/>
        ///</summary>
        public static readonly Class Container = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#Container"));

        ///<summary>
        ///The class of literal values, eg. textual strings and integers.
        ///<see cref="http://www.w3.org/2000/01/rdf-schema#Literal"/>
        ///</summary>
        public static readonly Class Literal = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#Literal"));
    }
    ///<summary>
    ///
    ///
    ///</summary>
    public class owl : Semiodesk.Trinity.Ontology
    {
        public static readonly Uri Namespace = new Uri("http://www.w3.org/2002/07/owl#");
        public static Uri GetNamespace() { return Namespace; }

        public static readonly string Prefix = "owl";
        public static string GetPrefix() { return Prefix; }

        ///<summary>
        ///The class of OWL individuals.
        ///<see cref="http://www.w3.org/2002/07/owl#Thing"/>
        ///</summary>
        public static readonly Class Thing = new Class(new Uri("http://www.w3.org/2002/07/owl#Thing"));

        ///<summary>
        ///The class of OWL classes.
        ///<see cref="http://www.w3.org/2002/07/owl#Class"/>
        ///</summary>
        public static readonly Class Class = new Class(new Uri("http://www.w3.org/2002/07/owl#Class"));

        ///<summary>
        ///This is the empty class.
        ///<see cref="http://www.w3.org/2002/07/owl#Nothing"/>
        ///</summary>
        public static readonly Class Nothing = new Class(new Uri("http://www.w3.org/2002/07/owl#Nothing"));

        ///<summary>
        ///The property that determines that a given class is the complement of another class.
        ///<see cref="http://www.w3.org/2002/07/owl#complementOf"/>
        ///</summary>
        public static readonly Property complementOf = new Property(new Uri("http://www.w3.org/2002/07/owl#complementOf"));

        ///<summary>
        ///The property that determines the collection of classes or data ranges that build a union.
        ///<see cref="http://www.w3.org/2002/07/owl#unionOf"/>
        ///</summary>
        public static readonly Property unionOf = new Property(new Uri("http://www.w3.org/2002/07/owl#unionOf"));

        ///<summary>
        ///
        ///  This ontology partially describes the built-in classes and
        ///  properties that together form the basis of the RDF/XML syntax of OWL 2.
        ///  The content of this ontology is based on Tables 6.1 and 6.2
        ///  in Section 6.4 of the OWL 2 RDF-Based Semantics specification,
        ///  available at http://www.w3.org/TR/owl2-rdf-based-semantics/.
        ///  Please note that those tables do not include the different annotations
        ///  (labels, comments and rdfs:isDefinedBy links) used in this file.
        ///  Also note that the descriptions provided in this ontology do not
        ///  provide a complete and correct formal description of either the syntax
        ///  or the semantics of the introduced terms (please see the OWL 2
        ///  recommendations for the complete and normative specifications).
        ///  Furthermore, the information provided by this ontology may be
        ///  misleading if not used with care. This ontology SHOULD NOT be imported
        ///  into OWL ontologies. Importing this file into an OWL 2 DL ontology
        ///  will cause it to become an OWL 2 Full ontology and may have other,
        ///  unexpected, consequences.
        ///   
        ///<see cref="http://www.w3.org/2002/07/owl"/>
        ///</summary>
        public static readonly Resource owl_0 = new Resource(new Uri("http://www.w3.org/2002/07/owl"));

        ///<summary>
        ///The class of ontologies.
        ///<see cref="http://www.w3.org/2002/07/owl#Ontology"/>
        ///</summary>
        public static readonly Class Ontology = new Class(new Uri("http://www.w3.org/2002/07/owl#Ontology"));

        ///<summary>
        ///The property that is used for importing other ontologies into a given ontology.
        ///<see cref="http://www.w3.org/2002/07/owl#imports"/>
        ///</summary>
        public static readonly Resource imports = new Resource(new Uri("http://www.w3.org/2002/07/owl#imports"));

        ///<summary>
        ///The annotation property that provides version information for an ontology or another OWL construct.
        ///<see cref="http://www.w3.org/2002/07/owl#versionInfo"/>
        ///</summary>
        public static readonly Resource versionInfo = new Resource(new Uri("http://www.w3.org/2002/07/owl#versionInfo"));

        ///<summary>
        ///The annotation property that indicates the predecessor ontology of a given ontology.
        ///<see cref="http://www.w3.org/2002/07/owl#priorVersion"/>
        ///</summary>
        public static readonly Resource priorVersion = new Resource(new Uri("http://www.w3.org/2002/07/owl#priorVersion"));

        ///<summary>
        ///The class of ontology properties.
        ///<see cref="http://www.w3.org/2002/07/owl#OntologyProperty"/>
        ///</summary>
        public static readonly Class OntologyProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#OntologyProperty"));

        ///<summary>
        ///The class of annotation properties.
        ///<see cref="http://www.w3.org/2002/07/owl#AnnotationProperty"/>
        ///</summary>
        public static readonly Class AnnotationProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#AnnotationProperty"));

        ///<summary>
        ///The property that determines that two given classes are equivalent, and that is used to specify datatype definitions.
        ///<see cref="http://www.w3.org/2002/07/owl#equivalentClass"/>
        ///</summary>
        public static readonly Property equivalentClass = new Property(new Uri("http://www.w3.org/2002/07/owl#equivalentClass"));

        ///<summary>
        ///The property that determines that two given classes are disjoint.
        ///<see cref="http://www.w3.org/2002/07/owl#disjointWith"/>
        ///</summary>
        public static readonly Property disjointWith = new Property(new Uri("http://www.w3.org/2002/07/owl#disjointWith"));

        ///<summary>
        ///The property that determines that two given properties are equivalent.
        ///<see cref="http://www.w3.org/2002/07/owl#equivalentProperty"/>
        ///</summary>
        public static readonly Property equivalentProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#equivalentProperty"));

        ///<summary>
        ///The property that determines that two given individuals are different.
        ///<see cref="http://www.w3.org/2002/07/owl#differentFrom"/>
        ///</summary>
        public static readonly Property differentFrom = new Property(new Uri("http://www.w3.org/2002/07/owl#differentFrom"));

        ///<summary>
        ///The property that determines the collection of pairwise different individuals in a owl:AllDifferent axiom.
        ///<see cref="http://www.w3.org/2002/07/owl#distinctMembers"/>
        ///</summary>
        public static readonly Property distinctMembers = new Property(new Uri("http://www.w3.org/2002/07/owl#distinctMembers"));

        ///<summary>
        ///The class of collections of pairwise different individuals.
        ///<see cref="http://www.w3.org/2002/07/owl#AllDifferent"/>
        ///</summary>
        public static readonly Class AllDifferent = new Class(new Uri("http://www.w3.org/2002/07/owl#AllDifferent"));

        ///<summary>
        ///The property that determines the collection of classes or data ranges that build an intersection.
        ///<see cref="http://www.w3.org/2002/07/owl#intersectionOf"/>
        ///</summary>
        public static readonly Property intersectionOf = new Property(new Uri("http://www.w3.org/2002/07/owl#intersectionOf"));

        ///<summary>
        ///The property that determines the collection of individuals or data values that build an enumeration.
        ///<see cref="http://www.w3.org/2002/07/owl#oneOf"/>
        ///</summary>
        public static readonly Property oneOf = new Property(new Uri("http://www.w3.org/2002/07/owl#oneOf"));

        ///<summary>
        ///The class of property restrictions.
        ///<see cref="http://www.w3.org/2002/07/owl#Restriction"/>
        ///</summary>
        public static readonly Class Restriction = new Class(new Uri("http://www.w3.org/2002/07/owl#Restriction"));

        ///<summary>
        ///The property that determines the property that a property restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#onProperty"/>
        ///</summary>
        public static readonly Property onProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#onProperty"));

        ///<summary>
        ///The property that determines the class that a universal property restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#allValuesFrom"/>
        ///</summary>
        public static readonly Property allValuesFrom = new Property(new Uri("http://www.w3.org/2002/07/owl#allValuesFrom"));

        ///<summary>
        ///The property that determines the individual that a has-value restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#hasValue"/>
        ///</summary>
        public static readonly Property hasValue = new Property(new Uri("http://www.w3.org/2002/07/owl#hasValue"));

        ///<summary>
        ///The property that determines the class that an existential property restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#someValuesFrom"/>
        ///</summary>
        public static readonly Property someValuesFrom = new Property(new Uri("http://www.w3.org/2002/07/owl#someValuesFrom"));

        ///<summary>
        ///The property that determines the cardinality of a minimum cardinality restriction.
        ///<see cref="http://www.w3.org/2002/07/owl#minCardinality"/>
        ///</summary>
        public static readonly Property minCardinality = new Property(new Uri("http://www.w3.org/2002/07/owl#minCardinality"));

        ///<summary>
        ///The property that determines the cardinality of a maximum cardinality restriction.
        ///<see cref="http://www.w3.org/2002/07/owl#maxCardinality"/>
        ///</summary>
        public static readonly Property maxCardinality = new Property(new Uri("http://www.w3.org/2002/07/owl#maxCardinality"));

        ///<summary>
        ///The property that determines the cardinality of an exact cardinality restriction.
        ///<see cref="http://www.w3.org/2002/07/owl#cardinality"/>
        ///</summary>
        public static readonly Property cardinality = new Property(new Uri("http://www.w3.org/2002/07/owl#cardinality"));

        ///<summary>
        ///The class of object properties.
        ///<see cref="http://www.w3.org/2002/07/owl#ObjectProperty"/>
        ///</summary>
        public static readonly Class ObjectProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#ObjectProperty"));

        ///<summary>
        ///The class of data properties.
        ///<see cref="http://www.w3.org/2002/07/owl#DatatypeProperty"/>
        ///</summary>
        public static readonly Class DatatypeProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#DatatypeProperty"));

        ///<summary>
        ///The property that determines that two given properties are inverse.
        ///<see cref="http://www.w3.org/2002/07/owl#inverseOf"/>
        ///</summary>
        public static readonly Property inverseOf = new Property(new Uri("http://www.w3.org/2002/07/owl#inverseOf"));

        ///<summary>
        ///The class of transitive properties.
        ///<see cref="http://www.w3.org/2002/07/owl#TransitiveProperty"/>
        ///</summary>
        public static readonly Class TransitiveProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#TransitiveProperty"));

        ///<summary>
        ///The class of symmetric properties.
        ///<see cref="http://www.w3.org/2002/07/owl#SymmetricProperty"/>
        ///</summary>
        public static readonly Class SymmetricProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#SymmetricProperty"));

        ///<summary>
        ///The class of functional properties.
        ///<see cref="http://www.w3.org/2002/07/owl#FunctionalProperty"/>
        ///</summary>
        public static readonly Class FunctionalProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#FunctionalProperty"));

        ///<summary>
        ///The class of inverse-functional properties.
        ///<see cref="http://www.w3.org/2002/07/owl#InverseFunctionalProperty"/>
        ///</summary>
        public static readonly Class InverseFunctionalProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#InverseFunctionalProperty"));

        ///<summary>
        ///The annotation property that indicates that a given ontology is backward compatible with another ontology.
        ///<see cref="http://www.w3.org/2002/07/owl#backwardCompatibleWith"/>
        ///</summary>
        public static readonly Resource backwardCompatibleWith = new Resource(new Uri("http://www.w3.org/2002/07/owl#backwardCompatibleWith"));

        ///<summary>
        ///The annotation property that indicates that a given ontology is incompatible with another ontology.
        ///<see cref="http://www.w3.org/2002/07/owl#incompatibleWith"/>
        ///</summary>
        public static readonly Resource incompatibleWith = new Resource(new Uri("http://www.w3.org/2002/07/owl#incompatibleWith"));

        ///<summary>
        ///The class of deprecated classes.
        ///<see cref="http://www.w3.org/2002/07/owl#DeprecatedClass"/>
        ///</summary>
        public static readonly Class DeprecatedClass = new Class(new Uri("http://www.w3.org/2002/07/owl#DeprecatedClass"));

        ///<summary>
        ///The class of deprecated properties.
        ///<see cref="http://www.w3.org/2002/07/owl#DeprecatedProperty"/>
        ///</summary>
        public static readonly Class DeprecatedProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#DeprecatedProperty"));

        ///<summary>
        ///The class of OWL data ranges, which are special kinds of datatypes. Note: The use of the IRI owl:DataRange has been deprecated as of OWL 2. The IRI rdfs:Datatype SHOULD be used instead.
        ///<see cref="http://www.w3.org/2002/07/owl#DataRange"/>
        ///</summary>
        public static readonly Class DataRange = new Class(new Uri("http://www.w3.org/2002/07/owl#DataRange"));

        ///<summary>
        ///The property that determines that two given individuals are equal.
        ///<see cref="http://www.w3.org/2002/07/owl#sameAs"/>
        ///</summary>
        public static readonly Property sameAs = new Property(new Uri("http://www.w3.org/2002/07/owl#sameAs"));

        ///<summary>
        ///The property that identifies the version IRI of an ontology.
        ///<see cref="http://www.w3.org/2002/07/owl#versionIRI"/>
        ///</summary>
        public static readonly Resource versionIRI = new Resource(new Uri("http://www.w3.org/2002/07/owl#versionIRI"));

        ///<summary>
        ///The class of collections of pairwise disjoint classes.
        ///<see cref="http://www.w3.org/2002/07/owl#AllDisjointClasses"/>
        ///</summary>
        public static readonly Class AllDisjointClasses = new Class(new Uri("http://www.w3.org/2002/07/owl#AllDisjointClasses"));

        ///<summary>
        ///The class of collections of pairwise disjoint properties.
        ///<see cref="http://www.w3.org/2002/07/owl#AllDisjointProperties"/>
        ///</summary>
        public static readonly Class AllDisjointProperties = new Class(new Uri("http://www.w3.org/2002/07/owl#AllDisjointProperties"));

        ///<summary>
        ///The class of annotated annotations for which the RDF serialization consists of an annotated subject, predicate and object.
        ///<see cref="http://www.w3.org/2002/07/owl#Annotation"/>
        ///</summary>
        public static readonly Class Annotation = new Class(new Uri("http://www.w3.org/2002/07/owl#Annotation"));

        ///<summary>
        ///The class of asymmetric properties.
        ///<see cref="http://www.w3.org/2002/07/owl#AsymmetricProperty"/>
        ///</summary>
        public static readonly Class AsymmetricProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#AsymmetricProperty"));

        ///<summary>
        ///The class of annotated axioms for which the RDF serialization consists of an annotated subject, predicate and object.
        ///<see cref="http://www.w3.org/2002/07/owl#Axiom"/>
        ///</summary>
        public static readonly Class Axiom = new Class(new Uri("http://www.w3.org/2002/07/owl#Axiom"));

        ///<summary>
        ///The class of irreflexive properties.
        ///<see cref="http://www.w3.org/2002/07/owl#IrreflexiveProperty"/>
        ///</summary>
        public static readonly Class IrreflexiveProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#IrreflexiveProperty"));

        ///<summary>
        ///The class of named individuals.
        ///<see cref="http://www.w3.org/2002/07/owl#NamedIndividual"/>
        ///</summary>
        public static readonly Class NamedIndividual = new Class(new Uri("http://www.w3.org/2002/07/owl#NamedIndividual"));

        ///<summary>
        ///The class of negative property assertions.
        ///<see cref="http://www.w3.org/2002/07/owl#NegativePropertyAssertion"/>
        ///</summary>
        public static readonly Class NegativePropertyAssertion = new Class(new Uri("http://www.w3.org/2002/07/owl#NegativePropertyAssertion"));

        ///<summary>
        ///The class of reflexive properties.
        ///<see cref="http://www.w3.org/2002/07/owl#ReflexiveProperty"/>
        ///</summary>
        public static readonly Class ReflexiveProperty = new Class(new Uri("http://www.w3.org/2002/07/owl#ReflexiveProperty"));

        ///<summary>
        ///The property that determines the predicate of an annotated axiom or annotated annotation.
        ///<see cref="http://www.w3.org/2002/07/owl#annotatedProperty"/>
        ///</summary>
        public static readonly Property annotatedProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#annotatedProperty"));

        ///<summary>
        ///The property that determines the subject of an annotated axiom or annotated annotation.
        ///<see cref="http://www.w3.org/2002/07/owl#annotatedSource"/>
        ///</summary>
        public static readonly Property annotatedSource = new Property(new Uri("http://www.w3.org/2002/07/owl#annotatedSource"));

        ///<summary>
        ///The property that determines the object of an annotated axiom or annotated annotation.
        ///<see cref="http://www.w3.org/2002/07/owl#annotatedTarget"/>
        ///</summary>
        public static readonly Property annotatedTarget = new Property(new Uri("http://www.w3.org/2002/07/owl#annotatedTarget"));

        ///<summary>
        ///The property that determines the predicate of a negative property assertion.
        ///<see cref="http://www.w3.org/2002/07/owl#assertionProperty"/>
        ///</summary>
        public static readonly Property assertionProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#assertionProperty"));

        ///<summary>
        ///The data property that does not relate any individual to any data value.
        ///<see cref="http://www.w3.org/2002/07/owl#bottomDataProperty"/>
        ///</summary>
        public static readonly Property bottomDataProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#bottomDataProperty"));

        ///<summary>
        ///The object property that does not relate any two individuals.
        ///<see cref="http://www.w3.org/2002/07/owl#bottomObjectProperty"/>
        ///</summary>
        public static readonly Property bottomObjectProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#bottomObjectProperty"));

        ///<summary>
        ///The property that determines that a given data range is the complement of another data range with respect to the data domain.
        ///<see cref="http://www.w3.org/2002/07/owl#datatypeComplementOf"/>
        ///</summary>
        public static readonly Property datatypeComplementOf = new Property(new Uri("http://www.w3.org/2002/07/owl#datatypeComplementOf"));

        ///<summary>
        ///The annotation property that indicates that a given entity has been deprecated.
        ///<see cref="http://www.w3.org/2002/07/owl#deprecated"/>
        ///</summary>
        public static readonly Resource deprecated = new Resource(new Uri("http://www.w3.org/2002/07/owl#deprecated"));

        ///<summary>
        ///The property that determines that a given class is equivalent to the disjoint union of a collection of other classes.
        ///<see cref="http://www.w3.org/2002/07/owl#disjointUnionOf"/>
        ///</summary>
        public static readonly Property disjointUnionOf = new Property(new Uri("http://www.w3.org/2002/07/owl#disjointUnionOf"));

        ///<summary>
        ///The property that determines the collection of properties that jointly build a key.
        ///<see cref="http://www.w3.org/2002/07/owl#hasKey"/>
        ///</summary>
        public static readonly Property hasKey = new Property(new Uri("http://www.w3.org/2002/07/owl#hasKey"));

        ///<summary>
        ///The property that determines the property that a self restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#hasSelf"/>
        ///</summary>
        public static readonly Property hasSelf = new Property(new Uri("http://www.w3.org/2002/07/owl#hasSelf"));

        ///<summary>
        ///The property that determines the cardinality of a maximum qualified cardinality restriction.
        ///<see cref="http://www.w3.org/2002/07/owl#maxQualifiedCardinality"/>
        ///</summary>
        public static readonly Property maxQualifiedCardinality = new Property(new Uri("http://www.w3.org/2002/07/owl#maxQualifiedCardinality"));

        ///<summary>
        ///The property that determines the collection of members in either a owl:AllDifferent, owl:AllDisjointClasses or owl:AllDisjointProperties axiom.
        ///<see cref="http://www.w3.org/2002/07/owl#members"/>
        ///</summary>
        public static readonly Property members = new Property(new Uri("http://www.w3.org/2002/07/owl#members"));

        ///<summary>
        ///The property that determines the cardinality of a minimum qualified cardinality restriction.
        ///<see cref="http://www.w3.org/2002/07/owl#minQualifiedCardinality"/>
        ///</summary>
        public static readonly Property minQualifiedCardinality = new Property(new Uri("http://www.w3.org/2002/07/owl#minQualifiedCardinality"));

        ///<summary>
        ///The property that determines the class that a qualified object cardinality restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#onClass"/>
        ///</summary>
        public static readonly Property onClass = new Property(new Uri("http://www.w3.org/2002/07/owl#onClass"));

        ///<summary>
        ///The property that determines the data range that a qualified data cardinality restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#onDataRange"/>
        ///</summary>
        public static readonly Property onDataRange = new Property(new Uri("http://www.w3.org/2002/07/owl#onDataRange"));

        ///<summary>
        ///The property that determines the datatype that a datatype restriction refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#onDatatype"/>
        ///</summary>
        public static readonly Property onDatatype = new Property(new Uri("http://www.w3.org/2002/07/owl#onDatatype"));

        ///<summary>
        ///The property that determines the n-tuple of properties that a property restriction on an n-ary data range refers to.
        ///<see cref="http://www.w3.org/2002/07/owl#onProperties"/>
        ///</summary>
        public static readonly Property onProperties = new Property(new Uri("http://www.w3.org/2002/07/owl#onProperties"));

        ///<summary>
        ///The property that determines the n-tuple of properties that build a sub property chain of a given property.
        ///<see cref="http://www.w3.org/2002/07/owl#propertyChainAxiom"/>
        ///</summary>
        public static readonly Property propertyChainAxiom = new Property(new Uri("http://www.w3.org/2002/07/owl#propertyChainAxiom"));

        ///<summary>
        ///The property that determines that two given properties are disjoint.
        ///<see cref="http://www.w3.org/2002/07/owl#propertyDisjointWith"/>
        ///</summary>
        public static readonly Property propertyDisjointWith = new Property(new Uri("http://www.w3.org/2002/07/owl#propertyDisjointWith"));

        ///<summary>
        ///The property that determines the cardinality of an exact qualified cardinality restriction.
        ///<see cref="http://www.w3.org/2002/07/owl#qualifiedCardinality"/>
        ///</summary>
        public static readonly Property qualifiedCardinality = new Property(new Uri("http://www.w3.org/2002/07/owl#qualifiedCardinality"));

        ///<summary>
        ///The property that determines the subject of a negative property assertion.
        ///<see cref="http://www.w3.org/2002/07/owl#sourceIndividual"/>
        ///</summary>
        public static readonly Property sourceIndividual = new Property(new Uri("http://www.w3.org/2002/07/owl#sourceIndividual"));

        ///<summary>
        ///The property that determines the object of a negative object property assertion.
        ///<see cref="http://www.w3.org/2002/07/owl#targetIndividual"/>
        ///</summary>
        public static readonly Property targetIndividual = new Property(new Uri("http://www.w3.org/2002/07/owl#targetIndividual"));

        ///<summary>
        ///The property that determines the value of a negative data property assertion.
        ///<see cref="http://www.w3.org/2002/07/owl#targetValue"/>
        ///</summary>
        public static readonly Property targetValue = new Property(new Uri("http://www.w3.org/2002/07/owl#targetValue"));

        ///<summary>
        ///The data property that relates every individual to every data value.
        ///<see cref="http://www.w3.org/2002/07/owl#topDataProperty"/>
        ///</summary>
        public static readonly Property topDataProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#topDataProperty"));

        ///<summary>
        ///The object property that relates every two individuals.
        ///<see cref="http://www.w3.org/2002/07/owl#topObjectProperty"/>
        ///</summary>
        public static readonly Property topObjectProperty = new Property(new Uri("http://www.w3.org/2002/07/owl#topObjectProperty"));

        ///<summary>
        ///The property that determines the collection of facet-value pairs that define a datatype restriction.
        ///<see cref="http://www.w3.org/2002/07/owl#withRestrictions"/>
        ///</summary>
        public static readonly Property withRestrictions = new Property(new Uri("http://www.w3.org/2002/07/owl#withRestrictions"));
    }

}
