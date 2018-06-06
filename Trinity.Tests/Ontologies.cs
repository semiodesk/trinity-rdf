// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Semiodesk.Trinity.Ontologies
{

	///<summary>
///The RDF Vocabulary (RDF)
///This is the RDF Schema for the RDF vocabulary defined in the RDF namespace.
///</summary>
public class rdf : Ontology
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
    ///The predicate of the subject RDF statement.
    ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#predicate"/>
    ///</summary>
    public static readonly Property predicate = new Property(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#predicate"));    

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
    ///The class of XML literal values.
    ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral"/>
    ///</summary>
    public static readonly Resource XMLLiteral = new Resource(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral"));    

    ///<summary>
    ///The class of ordered containers.
    ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#Seq"/>
    ///</summary>
    public static readonly Class Seq = new Class(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#Seq"));    

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
public class rdfs : Ontology
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
    ///The class of literal values, eg. textual strings and integers.
    ///<see cref="http://www.w3.org/2000/01/rdf-schema#Literal"/>
    ///</summary>
    public static readonly Class Literal = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#Literal"));    

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
    ///The class of container membership properties, rdf:_1, rdf:_2, ..., all of which are sub-properties of 'member'.
    ///<see cref="http://www.w3.org/2000/01/rdf-schema#ContainerMembershipProperty"/>
    ///</summary>
    public static readonly Class ContainerMembershipProperty = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#ContainerMembershipProperty"));
}

///<summary>
///The Dublin Core Terms Namespace providing access to its content by means of an RDF Schema.
///The Dublin Core Terms namespace provides URIs for the Dublin Core Element Set Qualifier Vocabulary. Vocabulary terms are declared using RDF Schema language to support RDF applications. The Dublin Core qualifiers form a richer vocabulary, which is intended to facilitate discovery of resources. It will be updated according to dc-usage decisions.
///</summary>
public class dcterms : Ontology
{
    public static readonly Uri Namespace = new Uri("http://purl.org/dc/terms/");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "dcterms";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///Date of creation of the resource.
    ///<see cref="http://purl.org/dc/terms/created"/>
    ///</summary>
    public static readonly Property created = new Property(new Uri("http://purl.org/dc/terms/created"));    

    ///<summary>
    ///Date on which the resource was changed.
    ///<see cref="http://purl.org/dc/terms/modified"/>
    ///</summary>
    public static readonly Property modified = new Property(new Uri("http://purl.org/dc/terms/modified"));    

    ///<summary>
    ///The size or duration of the resource.
    ///<see cref="http://purl.org/dc/terms/extent"/>
    ///</summary>
    public static readonly Property extent = new Property(new Uri("http://purl.org/dc/terms/extent"));    

    ///<summary>
    ///The described resource is referenced, cited, or otherwise pointed to by the referenced resource.
    ///<see cref="http://purl.org/dc/terms/isReferencedBy"/>
    ///</summary>
    public static readonly Property isReferencedBy = new Property(new Uri("http://purl.org/dc/terms/isReferencedBy"));    

    ///<summary>
    ///A class of entity for whom the resource is intended or useful.
    ///<see cref="http://purl.org/dc/terms/audience"/>
    ///</summary>
    public static readonly Property audience = new Property(new Uri("http://purl.org/dc/terms/audience"));    

    ///<summary>
    ///A summary of the content of the resource.
    ///<see cref="http://purl.org/dc/terms/abstract"/>
    ///</summary>
    public static readonly Property _abstract = new Property(new Uri("http://purl.org/dc/terms/abstract"));    

    ///<summary>
    ///The described resource is a version, edition, or adaptation of the referenced resource. Changes in version imply substantive changes in content rather than differences in format.
    ///<see cref="http://purl.org/dc/terms/isVersionOf"/>
    ///</summary>
    public static readonly Property isVersionOf = new Property(new Uri("http://purl.org/dc/terms/isVersionOf"));    

    ///<summary>
    ///The described resource supplants, displaces, or supersedes the referenced resource.
    ///<see cref="http://purl.org/dc/terms/replaces"/>
    ///</summary>
    public static readonly Property replaces = new Property(new Uri("http://purl.org/dc/terms/replaces"));    

    ///<summary>
    ///The described resource pre-existed the referenced resource, which is essentially the same intellectual content presented in another format.
    ///<see cref="http://purl.org/dc/terms/hasFormat"/>
    ///</summary>
    public static readonly Property hasFormat = new Property(new Uri("http://purl.org/dc/terms/hasFormat"));    

    ///<summary>
    ///Temporal characteristics of the intellectual content of the resource.
    ///<see cref="http://purl.org/dc/terms/temporal"/>
    ///</summary>
    public static readonly Property temporal = new Property(new Uri("http://purl.org/dc/terms/temporal"));    

    ///<summary>
    ///A bibliographic reference for the resource.
    ///<see cref="http://purl.org/dc/terms/bibliographicCitation"/>
    ///</summary>
    public static readonly Property bibliographicCitation = new Property(new Uri("http://purl.org/dc/terms/bibliographicCitation"));    

    ///<summary>
    ///A legal document giving official permission to do something with the resource.
    ///<see cref="http://purl.org/dc/terms/license"/>
    ///</summary>
    public static readonly Property license = new Property(new Uri("http://purl.org/dc/terms/license"));    

    ///<summary>
    ///A person or organization owning or managing rights over the resource.
    ///<see cref="http://purl.org/dc/terms/rightsHolder"/>
    ///</summary>
    public static readonly Property rightsHolder = new Property(new Uri("http://purl.org/dc/terms/rightsHolder"));    

    ///<summary>
    ///A set of resource type encoding schemes and/or formats
    ///<see cref="http://purl.org/dc/terms/TypeScheme"/>
    ///</summary>
    public static readonly Class TypeScheme = new Class(new Uri("http://purl.org/dc/terms/TypeScheme"));    

    ///<summary>
    ///A set of resource relation encoding schemes and/or formats
    ///<see cref="http://purl.org/dc/terms/RelationScheme"/>
    ///</summary>
    public static readonly Class RelationScheme = new Class(new Uri("http://purl.org/dc/terms/RelationScheme"));    

    ///<summary>
    ///A list of types used to categorize the nature or genre of the content of the resource.
    ///<see cref="http://purl.org/dc/terms/DCMIType"/>
    ///</summary>
    public static readonly Class DCMIType = new Class(new Uri("http://purl.org/dc/terms/DCMIType"));    

    ///<summary>
    ///ISO 639-2: Codes for the representation of names of languages.
    ///<see cref="http://purl.org/dc/terms/ISO639-2"/>
    ///</summary>
    public static readonly Class ISO639_2 = new Class(new Uri("http://purl.org/dc/terms/ISO639-2"));    

    ///<summary>
    ///A URI Uniform Resource Identifier
    ///<see cref="http://purl.org/dc/terms/URI"/>
    ///</summary>
    public static readonly Class URI = new Class(new Uri("http://purl.org/dc/terms/URI"));    

    ///<summary>
    ///The DCMI Point identifies a point in space using its geographic coordinates.
    ///<see cref="http://purl.org/dc/terms/Point"/>
    ///</summary>
    public static readonly Class Point = new Class(new Uri("http://purl.org/dc/terms/Point"));    

    ///<summary>
    ///A specification of the limits of a time interval.
    ///<see cref="http://purl.org/dc/terms/Period"/>
    ///</summary>
    public static readonly Class Period = new Class(new Uri("http://purl.org/dc/terms/Period"));    

    ///<summary>
    ///National Library of Medicine Classification
    ///<see cref="http://purl.org/dc/terms/NLM"/>
    ///</summary>
    public static readonly Class NLM = new Class(new Uri("http://purl.org/dc/terms/NLM"));    

    ///<summary>
    ///The described resource is required by the referenced resource, either physically or logically.
    ///<see cref="http://purl.org/dc/terms/isRequiredBy"/>
    ///</summary>
    public static readonly Property isRequiredBy = new Property(new Uri("http://purl.org/dc/terms/isRequiredBy"));    

    ///<summary>
    ///Any form of the title used as a substitute or alternative to the formal title of the resource.
    ///<see cref="http://purl.org/dc/terms/alternative"/>
    ///</summary>
    public static readonly Property alternative = new Property(new Uri("http://purl.org/dc/terms/alternative"));    

    ///<summary>
    ///A list of subunits of the content of the resource.
    ///<see cref="http://purl.org/dc/terms/tableOfContents"/>
    ///</summary>
    public static readonly Property tableOfContents = new Property(new Uri("http://purl.org/dc/terms/tableOfContents"));    

    ///<summary>
    ///Date (often a range) that the resource will become or did become available.
    ///<see cref="http://purl.org/dc/terms/available"/>
    ///</summary>
    public static readonly Property available = new Property(new Uri("http://purl.org/dc/terms/available"));    

    ///<summary>
    ///The material or physical carrier of the resource.
    ///<see cref="http://purl.org/dc/terms/medium"/>
    ///</summary>
    public static readonly Property medium = new Property(new Uri("http://purl.org/dc/terms/medium"));    

    ///<summary>
    ///The described resource is supplanted, displaced, or superseded by the referenced resource.
    ///<see cref="http://purl.org/dc/terms/isReplacedBy"/>
    ///</summary>
    public static readonly Property isReplacedBy = new Property(new Uri("http://purl.org/dc/terms/isReplacedBy"));    

    ///<summary>
    ///The described resource requires the referenced resource to support its function, delivery, or coherence of content.
    ///<see cref="http://purl.org/dc/terms/requires"/>
    ///</summary>
    public static readonly Property requires = new Property(new Uri("http://purl.org/dc/terms/requires"));    

    ///<summary>
    ///The described resource is a physical or logical part of the referenced resource.
    ///<see cref="http://purl.org/dc/terms/isPartOf"/>
    ///</summary>
    public static readonly Property isPartOf = new Property(new Uri("http://purl.org/dc/terms/isPartOf"));    

    ///<summary>
    ///The described resource is the same intellectual content of the referenced resource, but presented in another format.
    ///<see cref="http://purl.org/dc/terms/isFormatOf"/>
    ///</summary>
    public static readonly Property isFormatOf = new Property(new Uri("http://purl.org/dc/terms/isFormatOf"));    

    ///<summary>
    ///Date of acceptance of the resource (e.g. of thesis by university department, of article by journal, etc.).
    ///<see cref="http://purl.org/dc/terms/dateAccepted"/>
    ///</summary>
    public static readonly Property dateAccepted = new Property(new Uri("http://purl.org/dc/terms/dateAccepted"));    

    ///<summary>
    ///A statement of any changes in ownership and custody of the resource since its creation that are significant for its authenticity, integrity and interpretation.
    ///<see cref="http://purl.org/dc/terms/provenance"/>
    ///</summary>
    public static readonly Property provenance = new Property(new Uri("http://purl.org/dc/terms/provenance"));    

    ///<summary>
    ///A process, used to engender knowledge, attitudes and skills, that the resource is designed to support.
    ///<see cref="http://purl.org/dc/terms/instructionalMethod"/>
    ///</summary>
    public static readonly Property instructionalMethod = new Property(new Uri("http://purl.org/dc/terms/instructionalMethod"));    

    ///<summary>
    ///The frequency with which items are added to a collection.
    ///<see cref="http://purl.org/dc/terms/accrualPeriodicity"/>
    ///</summary>
    public static readonly Property accrualPeriodicity = new Property(new Uri("http://purl.org/dc/terms/accrualPeriodicity"));    

    ///<summary>
    ///The policy governing the addition of items to a collection.
    ///<see cref="http://purl.org/dc/terms/accrualPolicy"/>
    ///</summary>
    public static readonly Property accrualPolicy = new Property(new Uri("http://purl.org/dc/terms/accrualPolicy"));    

    ///<summary>
    ///A set of subject encoding schemes and/or formats
    ///<see cref="http://purl.org/dc/terms/SubjectScheme"/>
    ///</summary>
    public static readonly Class SubjectScheme = new Class(new Uri("http://purl.org/dc/terms/SubjectScheme"));    

    ///<summary>
    ///A set of date encoding schemes and/or formats 
    ///<see cref="http://purl.org/dc/terms/DateScheme"/>
    ///</summary>
    public static readonly Class DateScheme = new Class(new Uri("http://purl.org/dc/terms/DateScheme"));    

    ///<summary>
    ///A set of format encoding schemes.
    ///<see cref="http://purl.org/dc/terms/FormatScheme"/>
    ///</summary>
    public static readonly Class FormatScheme = new Class(new Uri("http://purl.org/dc/terms/FormatScheme"));    

    ///<summary>
    ///A set of encoding schemes for the coverage qualifier "temporal"
    ///<see cref="http://purl.org/dc/terms/TemporalScheme"/>
    ///</summary>
    public static readonly Class TemporalScheme = new Class(new Uri("http://purl.org/dc/terms/TemporalScheme"));    

    ///<summary>
    ///A set of resource identifier encoding schemes and/or formats
    ///<see cref="http://purl.org/dc/terms/IdentifierScheme"/>
    ///</summary>
    public static readonly Class IdentifierScheme = new Class(new Uri("http://purl.org/dc/terms/IdentifierScheme"));    

    ///<summary>
    ///Library of Congress Subject Headings
    ///<see cref="http://purl.org/dc/terms/LCSH"/>
    ///</summary>
    public static readonly Class LCSH = new Class(new Uri("http://purl.org/dc/terms/LCSH"));    

    ///<summary>
    ///Medical Subject Headings
    ///<see cref="http://purl.org/dc/terms/MESH"/>
    ///</summary>
    public static readonly Class MESH = new Class(new Uri("http://purl.org/dc/terms/MESH"));    

    ///<summary>
    ///Library of Congress Classification
    ///<see cref="http://purl.org/dc/terms/LCC"/>
    ///</summary>
    public static readonly Class LCC = new Class(new Uri("http://purl.org/dc/terms/LCC"));    

    ///<summary>
    ///Internet RFC 1766 'Tags for the identification of Language' specifies a two letter code taken from ISO 639, followed optionally by a two letter country code taken from ISO 3166.
    ///<see cref="http://purl.org/dc/terms/RFC1766"/>
    ///</summary>
    public static readonly Class RFC1766 = new Class(new Uri("http://purl.org/dc/terms/RFC1766"));    

    ///<summary>
    ///The DCMI Box identifies a region of space using its geographic limits.
    ///<see cref="http://purl.org/dc/terms/Box"/>
    ///</summary>
    public static readonly Class Box = new Class(new Uri("http://purl.org/dc/terms/Box"));    

    ///<summary>
    ///Internet RFC 3066 'Tags for the Identification of Languages' specifies a primary subtag which is a two-letter code taken from ISO 639 part 1 or a three-letter code taken from ISO 639 part 2, followed optionally by a two-letter country code taken from ISO 3166. When a language in ISO 639 has both a two-letter and three-letter code, use the two-letter code; when it has only a three-letter code, use the three-letter code.  This RFC replaces RFC 1766.
    ///<see cref="http://purl.org/dc/terms/RFC3066"/>
    ///</summary>
    public static readonly Class RFC3066 = new Class(new Uri("http://purl.org/dc/terms/RFC3066"));    

    ///<summary>
    ///Date of formal issuance (e.g., publication) of the resource.
    ///<see cref="http://purl.org/dc/terms/issued"/>
    ///</summary>
    public static readonly Property issued = new Property(new Uri("http://purl.org/dc/terms/issued"));    

    ///<summary>
    ///The described resource has a version, edition, or adaptation, namely, the referenced resource.
    ///<see cref="http://purl.org/dc/terms/hasVersion"/>
    ///</summary>
    public static readonly Property hasVersion = new Property(new Uri("http://purl.org/dc/terms/hasVersion"));    

    ///<summary>
    ///Date (often a range) of validity of a resource.
    ///<see cref="http://purl.org/dc/terms/valid"/>
    ///</summary>
    public static readonly Property valid = new Property(new Uri("http://purl.org/dc/terms/valid"));    

    ///<summary>
    ///The described resource includes the referenced resource either physically or logically.
    ///<see cref="http://purl.org/dc/terms/hasPart"/>
    ///</summary>
    public static readonly Property hasPart = new Property(new Uri("http://purl.org/dc/terms/hasPart"));    

    ///<summary>
    ///The described resource references, cites, or otherwise points to the referenced resource.
    ///<see cref="http://purl.org/dc/terms/references"/>
    ///</summary>
    public static readonly Property references = new Property(new Uri("http://purl.org/dc/terms/references"));    

    ///<summary>
    ///A reference to an established standard to which the resource conforms.
    ///<see cref="http://purl.org/dc/terms/conformsTo"/>
    ///</summary>
    public static readonly Property conformsTo = new Property(new Uri("http://purl.org/dc/terms/conformsTo"));    

    ///<summary>
    ///Spatial characteristics of the intellectual content of the resource.
    ///<see cref="http://purl.org/dc/terms/spatial"/>
    ///</summary>
    public static readonly Property spatial = new Property(new Uri("http://purl.org/dc/terms/spatial"));    

    ///<summary>
    ///A class of entity that mediates access to the resource and for whom the resource is intended or useful.
    ///<see cref="http://purl.org/dc/terms/mediator"/>
    ///</summary>
    public static readonly Property mediator = new Property(new Uri("http://purl.org/dc/terms/mediator"));    

    ///<summary>
    ///Date of a statement of copyright.
    ///<see cref="http://purl.org/dc/terms/dateCopyrighted"/>
    ///</summary>
    public static readonly Property dateCopyrighted = new Property(new Uri("http://purl.org/dc/terms/dateCopyrighted"));    

    ///<summary>
    ///Date of submission of the resource (e.g. thesis, articles, etc.).
    ///<see cref="http://purl.org/dc/terms/dateSubmitted"/>
    ///</summary>
    public static readonly Property dateSubmitted = new Property(new Uri("http://purl.org/dc/terms/dateSubmitted"));    

    ///<summary>
    ///A general statement describing the education or training context. Alternatively, a more specific  statement of the location of the audience in terms of its progression through an education or training context.
    ///<see cref="http://purl.org/dc/terms/educationLevel"/>
    ///</summary>
    public static readonly Property educationLevel = new Property(new Uri("http://purl.org/dc/terms/educationLevel"));    

    ///<summary>
    ///Information about who can access the resource or an indication of its security status.
    ///<see cref="http://purl.org/dc/terms/accessRights"/>
    ///</summary>
    public static readonly Property accessRights = new Property(new Uri("http://purl.org/dc/terms/accessRights"));    

    ///<summary>
    ///The method by which items are added to a collection.
    ///<see cref="http://purl.org/dc/terms/accrualMethod"/>
    ///</summary>
    public static readonly Property accrualMethod = new Property(new Uri("http://purl.org/dc/terms/accrualMethod"));    

    ///<summary>
    ///A set of language encoding schemes and/or formats.
    ///<see cref="http://purl.org/dc/terms/LanguageScheme"/>
    ///</summary>
    public static readonly Class LanguageScheme = new Class(new Uri("http://purl.org/dc/terms/LanguageScheme"));    

    ///<summary>
    ///A set of geographic place encoding schemes and/or formats
    ///<see cref="http://purl.org/dc/terms/SpatialScheme"/>
    ///</summary>
    public static readonly Class SpatialScheme = new Class(new Uri("http://purl.org/dc/terms/SpatialScheme"));    

    ///<summary>
    ///A set of source encoding schemes and/or formats
    ///<see cref="http://purl.org/dc/terms/SourceScheme"/>
    ///</summary>
    public static readonly Class SourceScheme = new Class(new Uri("http://purl.org/dc/terms/SourceScheme"));    

    ///<summary>
    ///Dewey Decimal Classification
    ///<see cref="http://purl.org/dc/terms/DDC"/>
    ///</summary>
    public static readonly Class DDC = new Class(new Uri("http://purl.org/dc/terms/DDC"));    

    ///<summary>
    ///Universal Decimal Classification
    ///<see cref="http://purl.org/dc/terms/UDC"/>
    ///</summary>
    public static readonly Class UDC = new Class(new Uri("http://purl.org/dc/terms/UDC"));    

    ///<summary>
    ///The Internet media type of the resource.
    ///<see cref="http://purl.org/dc/terms/IMT"/>
    ///</summary>
    public static readonly Class IMT = new Class(new Uri("http://purl.org/dc/terms/IMT"));    

    ///<summary>
    ///ISO 3166 Codes for the representation of names of countries
    ///<see cref="http://purl.org/dc/terms/ISO3166"/>
    ///</summary>
    public static readonly Class ISO3166 = new Class(new Uri("http://purl.org/dc/terms/ISO3166"));    

    ///<summary>
    ///The Getty Thesaurus of Geographic Names
    ///<see cref="http://purl.org/dc/terms/TGN"/>
    ///</summary>
    public static readonly Class TGN = new Class(new Uri("http://purl.org/dc/terms/TGN"));    

    ///<summary>
    ///W3C Encoding rules for dates and times - a profile based on ISO 8601
    ///<see cref="http://purl.org/dc/terms/W3CDTF"/>
    ///</summary>
    public static readonly Class W3CDTF = new Class(new Uri("http://purl.org/dc/terms/W3CDTF"));
}

///<summary>
///The DCMI Types namespace providing access to its content by means of an RDF Schema
///The Dublin Core Types namespace provides URIs for the entries of the DCMI Type Vocabulary. Entries are declared using RDF Schema language to support RDF applications. The Schema will be updated according to dc-usage decisions.
///</summary>
public class dctype : Ontology
{
    public static readonly Uri Namespace = new Uri("http://purl.org/dc/dcmitype/");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "dctype";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///
    ///<see cref="http://purl.org/dc/terms/DCMIType"/>
    ///</summary>
    public static readonly Class DCMIType = new Class(new Uri("http://purl.org/dc/terms/DCMIType"));    

    ///<summary>
    ///Data encoded in a defined structure.
    ///<see cref="http://purl.org/dc/dcmitype/Dataset"/>
    ///</summary>
    public static readonly Class Dataset = new Class(new Uri("http://purl.org/dc/dcmitype/Dataset"));    

    ///<summary>
    ///A visual representation other than text.
    ///<see cref="http://purl.org/dc/dcmitype/Image"/>
    ///</summary>
    public static readonly Class Image = new Class(new Uri("http://purl.org/dc/dcmitype/Image"));    

    ///<summary>
    ///A resource requiring interaction from the user to be understood, executed, or experienced.
    ///<see cref="http://purl.org/dc/dcmitype/InteractiveResource"/>
    ///</summary>
    public static readonly Class InteractiveResource = new Class(new Uri("http://purl.org/dc/dcmitype/InteractiveResource"));    

    ///<summary>
    ///An inanimate, three-dimensional object or substance.
    ///<see cref="http://purl.org/dc/dcmitype/PhysicalObject"/>
    ///</summary>
    public static readonly Class PhysicalObject = new Class(new Uri("http://purl.org/dc/dcmitype/PhysicalObject"));    

    ///<summary>
    ///A non-persistent, time-based occurrence.
    ///<see cref="http://purl.org/dc/dcmitype/Event"/>
    ///</summary>
    public static readonly Class Event = new Class(new Uri("http://purl.org/dc/dcmitype/Event"));    

    ///<summary>
    ///A resource primarily intended to be heard.
    ///<see cref="http://purl.org/dc/dcmitype/Sound"/>
    ///</summary>
    public static readonly Class Sound = new Class(new Uri("http://purl.org/dc/dcmitype/Sound"));    

    ///<summary>
    ///A static visual representation.
    ///<see cref="http://purl.org/dc/dcmitype/StillImage"/>
    ///</summary>
    public static readonly Class StillImage = new Class(new Uri("http://purl.org/dc/dcmitype/StillImage"));    

    ///<summary>
    ///An aggregation of resources.
    ///<see cref="http://purl.org/dc/dcmitype/Collection"/>
    ///</summary>
    public static readonly Class Collection = new Class(new Uri("http://purl.org/dc/dcmitype/Collection"));    

    ///<summary>
    ///A system that provides one or more functions.
    ///<see cref="http://purl.org/dc/dcmitype/Service"/>
    ///</summary>
    public static readonly Class Service = new Class(new Uri("http://purl.org/dc/dcmitype/Service"));    

    ///<summary>
    ///A computer program in source or compiled form.
    ///<see cref="http://purl.org/dc/dcmitype/Software"/>
    ///</summary>
    public static readonly Class Software = new Class(new Uri("http://purl.org/dc/dcmitype/Software"));    

    ///<summary>
    ///A resource consisting primarily of words for reading.
    ///<see cref="http://purl.org/dc/dcmitype/Text"/>
    ///</summary>
    public static readonly Class Text = new Class(new Uri("http://purl.org/dc/dcmitype/Text"));    

    ///<summary>
    ///A series of visual representations imparting an impression of motion when shown in succession.
    ///<see cref="http://purl.org/dc/dcmitype/MovingImage"/>
    ///</summary>
    public static readonly Class MovingImage = new Class(new Uri("http://purl.org/dc/dcmitype/MovingImage"));
}

///<summary>
///The Dublin Core Element Set v1.1 namespace providing access to its content by means of an RDF Schema
///The Dublin Core Element Set v1.1 namespace provides URIs for the Dublin Core Elements v1.1. Entries are declared using RDF Schema language to support RDF applications.
///</summary>
public class dc : Ontology
{
    public static readonly Uri Namespace = new Uri("http://purl.org/dc/elements/1.1/");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "dc";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///An account of the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/description"/>
    ///</summary>
    public static readonly Property description = new Property(new Uri("http://purl.org/dc/elements/1.1/description"));    

    ///<summary>
    ///An entity responsible for making contributions to the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/contributor"/>
    ///</summary>
    public static readonly Property contributor = new Property(new Uri("http://purl.org/dc/elements/1.1/contributor"));    

    ///<summary>
    ///An unambiguous reference to the resource within a given context.
    ///<see cref="http://purl.org/dc/elements/1.1/identifier"/>
    ///</summary>
    public static readonly Property identifier = new Property(new Uri("http://purl.org/dc/elements/1.1/identifier"));    

    ///<summary>
    ///A language of the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/language"/>
    ///</summary>
    public static readonly Property language = new Property(new Uri("http://purl.org/dc/elements/1.1/language"));    

    ///<summary>
    ///A related resource.
    ///<see cref="http://purl.org/dc/elements/1.1/relation"/>
    ///</summary>
    public static readonly Property relation = new Property(new Uri("http://purl.org/dc/elements/1.1/relation"));    

    ///<summary>
    ///The spatial or temporal topic of the resource, the spatial applicability of the resource, or the jurisdiction under which the resource is relevant.
    ///<see cref="http://purl.org/dc/elements/1.1/coverage"/>
    ///</summary>
    public static readonly Property coverage = new Property(new Uri("http://purl.org/dc/elements/1.1/coverage"));    

    ///<summary>
    ///An entity responsible for making the resource  available.
    ///<see cref="http://purl.org/dc/elements/1.1/publisher"/>
    ///</summary>
    public static readonly Property publisher = new Property(new Uri("http://purl.org/dc/elements/1.1/publisher"));    

    ///<summary>
    ///The nature or genre of the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/type"/>
    ///</summary>
    public static readonly Property type = new Property(new Uri("http://purl.org/dc/elements/1.1/type"));    

    ///<summary>
    ///The topic of the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/subject"/>
    ///</summary>
    public static readonly Property subject = new Property(new Uri("http://purl.org/dc/elements/1.1/subject"));    

    ///<summary>
    ///The resource from which the described  resource is derived.
    ///<see cref="http://purl.org/dc/elements/1.1/source"/>
    ///</summary>
    public static readonly Property source = new Property(new Uri("http://purl.org/dc/elements/1.1/source"));    

    ///<summary>
    ///Information about rights held in and  over the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/rights"/>
    ///</summary>
    public static readonly Property rights = new Property(new Uri("http://purl.org/dc/elements/1.1/rights"));    

    ///<summary>
    ///The file format, physical medium, or dimensions of the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/format"/>
    ///</summary>
    public static readonly Property format = new Property(new Uri("http://purl.org/dc/elements/1.1/format"));    

    ///<summary>
    ///A name given to the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/title"/>
    ///</summary>
    public static readonly Property title = new Property(new Uri("http://purl.org/dc/elements/1.1/title"));    

    ///<summary>
    ///An entity primarily responsible for making  the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/creator"/>
    ///</summary>
    public static readonly Property creator = new Property(new Uri("http://purl.org/dc/elements/1.1/creator"));    

    ///<summary>
    ///A point or period of time associated with an  event in the lifecycle of the resource.
    ///<see cref="http://purl.org/dc/elements/1.1/date"/>
    ///</summary>
    public static readonly Property date = new Property(new Uri("http://purl.org/dc/elements/1.1/date"));
}

///<summary>
///
///
///</summary>
public class ncal : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "ncal";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///A calendar list.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarList"/>
    ///</summary>
    public static readonly Class CalendarList = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarList"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#sunday"/>
    ///</summary>
    public static readonly Resource sunday = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#sunday"));    

    ///<summary>
    ///Day of the week. This class has been created to provide the limited vocabulary for ncal:byday property. See the documentation for ncal:byday for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Weekday"/>
    ///</summary>
    public static readonly Class Weekday = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Weekday"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfTimezoneObservanceEventJournalTimezoneTodo"/>
    ///</summary>
    public static readonly Class UnionOfTimezoneObservanceEventJournalTimezoneTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfTimezoneObservanceEventJournalTimezoneTodo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfTimezoneObservanceEventFreebusyJournalTimezoneTodo"/>
    ///</summary>
    public static readonly Class UnionOfTimezoneObservanceEventFreebusyJournalTimezoneTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfTimezoneObservanceEventFreebusyJournalTimezoneTodo"));    

    ///<summary>
    ///An alarm trigger. This class has been created to serve as the range of ncal:trigger property. See the documentation for ncal:trigger for more details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Trigger"/>
    ///</summary>
    public static readonly Class Trigger = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Trigger"));    

    ///<summary>
    ///Provide a grouping of component properties that describe an event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Event"/>
    ///</summary>
    public static readonly Class Event = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Event"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventJournalTodo"/>
    ///</summary>
    public static readonly Class UnionOfAlarmEventJournalTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventJournalTodo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventJournalTodo"/>
    ///</summary>
    public static readonly Class UnionOfEventJournalTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventJournalTodo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventTodo"/>
    ///</summary>
    public static readonly Class UnionOfEventTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventTodo"));    

    ///<summary>
    ///Alternate representation of the comment. Introduced to cover  the ALTREP parameter of the SUMMARY property. See documentation of ncal:summary for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#summaryAltRep"/>
    ///</summary>
    public static readonly Property summaryAltRep = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#summaryAltRep"));    

    ///<summary>
    ///Links the Vcalendar instance with the calendar components. This property has no direct equivalent in the RFC specification. It has been introduced to express the containmnent relations.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#component"/>
    ///</summary>
    public static readonly Property component = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#component"));    

    ///<summary>
    ///Recurrence Identifier Range. This class has been created to provide means to express the limited set of values for the ncal:range property. See documentation for ncal:range for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceIdentifierRange"/>
    ///</summary>
    public static readonly Class RecurrenceIdentifierRange = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceIdentifierRange"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#needsActionStatus"/>
    ///</summary>
    public static readonly Resource needsActionStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#needsActionStatus"));    

    ///<summary>
    ///A status of a calendar entity. This class has been introduced to express
    ///the limited set of values for the ncal:status property. The user may
    ///use the instances provided with this ontology or create his/her own.
    ///See the documentation for ncal:todoStatus for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TodoStatus"/>
    ///</summary>
    public static readonly Class TodoStatus = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TodoStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionParentClass"/>
    ///</summary>
    public static readonly Class UnionParentClass = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionParentClass"));    

    ///<summary>
    ///This property defines the persistent, globally unique identifier for the calendar component. Inspired by the RFC 2445 sec 4.8.4.7
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#uid"/>
    ///</summary>
    public static readonly Property uid = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#uid"));    

    ///<summary>
    ///Links the timezone with the standard timezone observance. This property has no direct equivalent in the RFC 2445. It has been inspired by the structure of the Vtimezone component defined in sec.4.6.5
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#standard"/>
    ///</summary>
    public static readonly Property standard = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#standard"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledTodoStatus"/>
    ///</summary>
    public static readonly Resource cancelledTodoStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledTodoStatus"));    

    ///<summary>
    ///A time entity. Conceived as a common superclass for NcalDateTime and NcalPeriod. According to RFC 2445 both DateTime and Period can be interpreted in different timezones. The first case is explored in many properties. The second case is theoretically possible in ncal:rdate property. Therefore the timezone properties have been defined at this level.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#NcalTimeEntity"/>
    ///</summary>
    public static readonly Class NcalTimeEntity = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#NcalTimeEntity"));    

    ///<summary>
    ///The uri of the attachment. Created to express the actual value of the ATTACH property defined in RFC 2445 sec. 4.8.1.1. This property expresses the default URI datatype of that property. see ncal:attachmentContents for the BINARY datatype.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attachmentUri"/>
    ///</summary>
    public static readonly Property attachmentUri = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attachmentUri"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#completedStatus"/>
    ///</summary>
    public static readonly Resource completedStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#completedStatus"));    

    ///<summary>
    ///Duration of a period of time. Inspired by the second part of a structured value of the PERIOD datatype specified in RFC 2445 sec. 4.3.9. Note that a single NcalPeriod instance shouldn't have the periodEnd and periodDuration properties specified simultaneously.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#periodDuration"/>
    ///</summary>
    public static readonly Property periodDuration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#periodDuration"));    

    ///<summary>
    ///A period of time. Inspired by the PERIOD datatype specified in RFC 2445 sec. 4.3.9
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#NcalPeriod"/>
    ///</summary>
    public static readonly Class NcalPeriod = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#NcalPeriod"));    

    ///<summary>
    ///The property indicates the date/time that the instance of the iCalendar object was created. Inspired by RFC 2445 sec. 4.8.7.1. Note that the RFC allows ONLY UTC values for this property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dtstamp"/>
    ///</summary>
    public static readonly Property dtstamp = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dtstamp"));    

    ///<summary>
    ///Expresses the compound value of a byday part of a recurrence rule. It stores the weekday and the integer modifier. Inspired by RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#BydayRulePart"/>
    ///</summary>
    public static readonly Class BydayRulePart = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#BydayRulePart"));    

    ///<summary>
    ///Defines the access classification for a calendar component. Inspired by RFC 2445 sec. 4.8.1.3 with the following reservations:  this property has limited vocabulary. Possible values are:  PUBLIC, PRIVATE and CONFIDENTIAL. The default is PUBLIC. Those values are expressed as instances of the AccessClassification class. The user may create his/her own if necessary.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#class"/>
    ///</summary>
    public static readonly Property _class = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#class"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#_8bitEncoding"/>
    ///</summary>
    public static readonly Resource _8bitEncoding = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#_8bitEncoding"));    

    ///<summary>
    ///Date an instance of NcalDateTime refers to. It was conceived to express values in DATE datatype specified in RFC 2445 4.3.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#date"/>
    ///</summary>
    public static readonly Property date = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#date"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#emailAction"/>
    ///</summary>
    public static readonly Resource emailAction = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#emailAction"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#privateClassification"/>
    ///</summary>
    public static readonly Resource privateClassification = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#privateClassification"));    

    ///<summary>
    ///This property specifies the identifier for the product that created the iCalendar object. Defined in RFC 2445 sec. 4.7.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#prodid"/>
    ///</summary>
    public static readonly Property prodid = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#prodid"));    

    ///<summary>
    ///Day of the month when the event should recur. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bymonthday"/>
    ///</summary>
    public static readonly Property bymonthday = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bymonthday"));    

    ///<summary>
    ///Defines a short summary or subject for the calendar component. Inspired by RFC 2445 sec 4.8.1.12 with the following reservations: the LANGUAGE parameter has been discarded. Please use xml:lang literals to express language. For the ALTREP parameter use the summaryAltRep property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#summary"/>
    ///</summary>
    public static readonly Property summary = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#summary"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#busyUnavailableFreebusyType"/>
    ///</summary>
    public static readonly Resource busyUnavailableFreebusyType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#busyUnavailableFreebusyType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#nonParticipantRole"/>
    ///</summary>
    public static readonly Resource nonParticipantRole = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#nonParticipantRole"));    

    ///<summary>
    ///This property defines the revision sequence number of the calendar component within a sequence of revisions. Inspired by RFC 2445 sec. 4.8.7.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#sequence"/>
    ///</summary>
    public static readonly Property sequence = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#sequence"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#yearly"/>
    ///</summary>
    public static readonly Resource yearly = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#yearly"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#opaqueTransparency"/>
    ///</summary>
    public static readonly Resource opaqueTransparency = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#opaqueTransparency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#monday"/>
    ///</summary>
    public static readonly Resource monday = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#monday"));    

    ///<summary>
    ///This property specifies when the calendar component begins. Inspired by RFC 2445 sec. 4.8.2.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dtstart"/>
    ///</summary>
    public static readonly Property dtstart = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dtstart"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#daily"/>
    ///</summary>
    public static readonly Resource daily = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#daily"));    

    ///<summary>
    ///The property is used to represent a relationship or reference between one calendar component and another. Inspired by RFC 2445 sec. 4.8.4.5. Originally this property had a RELTYPE parameter. It has been decided that it is more natural to introduce three different properties to express the values of that parameter. This property expresses the RELATED-TO property with RELTYPE=SIBLING parameter.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#relatedToSibling"/>
    ///</summary>
    public static readonly Property relatedToSibling = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#relatedToSibling"));    

    ///<summary>
    ///How many times should an event be repeated. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#count"/>
    ///</summary>
    public static readonly Property count = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#count"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#inProcessParticipationStatus"/>
    ///</summary>
    public static readonly Resource inProcessParticipationStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#inProcessParticipationStatus"));    

    ///<summary>
    ///This property is used by an assignee or delegatee of a to-do to convey the percent completion of a to-do to the Organizer. Inspired by RFC 2445 sec. 4.8.1.8
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#percentComplete"/>
    ///</summary>
    public static readonly Property percentComplete = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#percentComplete"));    

    ///<summary>
    ///This property defines the action to be invoked when an alarm is triggered. Inspired by RFC 2445 sec 4.8.6.1. Originally this property had a limited set of values. They are expressed as instances of the AlarmAction class.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#action"/>
    ///</summary>
    public static readonly Property action = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#action"));    

    ///<summary>
    ///The property specifies the date and time that the information associated with the calendar component was last revised in the calendar store. Note: This is analogous to the modification date and time for a file in the file system. Inspired by RFC 2445 sec. 4.8.7.3. Note that the RFC allows ONLY UTC time values for this property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#lastModified"/>
    ///</summary>
    public static readonly Property lastModified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#lastModified"));    

    ///<summary>
    ///The property is used to represent a relationship or reference between one calendar component and another. Inspired by RFC 2445 sec. 4.8.4.5. Originally this property had a RELTYPE parameter. It has been decided to introduce three different properties to express the values of that parameter. This property expresses the RELATED-TO property with RELTYPE=CHILD parameter.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#relatedToChild"/>
    ///</summary>
    public static readonly Property relatedToChild = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#relatedToChild"));    

    ///<summary>
    ///The property is used to represent a relationship or reference between one calendar component and another. Inspired by RFC 2445 sec. 4.8.4.5. Originally this property had a RELTYPE parameter. It has been decided that it is more natural to introduce three different properties to express the values of that parameter. This property expresses the RELATED-TO property with no RELTYPE parameter (the default value is PARENT), or with explicit RELTYPE=PARENT parameter.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#relatedToParent"/>
    ///</summary>
    public static readonly Property relatedToParent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#relatedToParent"));    

    ///<summary>
    ///Defines the equipment or resources anticipated for an activity specified by a calendar entity. Inspired by RFC 2445 sec. 4.8.1.10 with the following reservations:  the LANGUAGE parameter has been discarded. Please use xml:lang literals to express language. For the ALTREP parameter use the resourcesAltRep property. This property specifies multiple resources. The order is not important. it is recommended to introduce a separate triple for each resource.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#resources"/>
    ///</summary>
    public static readonly Property resources = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#resources"));    

    ///<summary>
    ///Provide a grouping of component properties that describe a journal entry.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Journal"/>
    ///</summary>
    public static readonly Class Journal = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Journal"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#saturday"/>
    ///</summary>
    public static readonly Resource saturday = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#saturday"));    

    ///<summary>
    ///Request Status. A class that was introduced to provide a structure for the value of ncal:requestStatus property. See documentation for ncal:requestStatus for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RequestStatus"/>
    ///</summary>
    public static readonly Class RequestStatus = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RequestStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tentativeStatus"/>
    ///</summary>
    public static readonly Resource tentativeStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tentativeStatus"));    

    ///<summary>
    ///Defines the overall status or confirmation for an Event. Based on the STATUS property defined in RFC 2445 sec. 4.8.1.11.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#eventStatus"/>
    ///</summary>
    public static readonly Property eventStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#eventStatus"));    

    ///<summary>
    ///This property defines the iCalendar object method associated with the calendar object. Defined in RFC 2445 sec. 4.7.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#method"/>
    ///</summary>
    public static readonly Property method = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#method"));    

    ///<summary>
    ///Weekdays the recurrence should occur. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byday"/>
    ///</summary>
    public static readonly Property byday = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byday"));    

    ///<summary>
    ///Beginng of a period. Inspired by the first part of a structured value of the PERIOD datatype specified in RFC 2445 sec. 4.3.9
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#periodBegin"/>
    ///</summary>
    public static readonly Property periodBegin = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#periodBegin"));    

    ///<summary>
    ///This property specifies the offset which is in use prior to this time zone observance. Inspired by RFC 2445 sec. 4.8.3.3. The original domain was underspecified. It said that this property must appear within a Timezone component. In this ontology a TimezoneObservance class has been introduced to clarify this specification. The original range was UTC-OFFSET. There is no equivalent among the XSD datatypes so plain string was chosen.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzoffsetfrom"/>
    ///</summary>
    public static readonly Property tzoffsetfrom = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzoffsetfrom"));    

    ///<summary>
    ///This property defines the calendar scale used for the calendar information specified in the iCalendar object. Defined in RFC 2445 sec. 4.7.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#calscale"/>
    ///</summary>
    public static readonly Property calscale = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#calscale"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#draftStatus"/>
    ///</summary>
    public static readonly Resource draftStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#draftStatus"));    

    ///<summary>
    ///Connects a BydayRulePath with a weekday.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bydayWeekday"/>
    ///</summary>
    public static readonly Property bydayWeekday = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bydayWeekday"));    

    ///<summary>
    ///This property specifies the offset which is in use in this time zone observance. nspired by RFC 2445 sec. 4.8.3.4. The original domain was underspecified. It said that this property must appear within a Timezone component. In this ontology a TimezoneObservance class has been introduced to clarify this specification. The original range was UTC-OFFSET. There is no equivalent among the XSD datatypes so plain string was chosen.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzoffsetto"/>
    ///</summary>
    public static readonly Property tzoffsetto = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzoffsetto"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#secondly"/>
    ///</summary>
    public static readonly Resource secondly = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#secondly"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#declinedParticipationStatus"/>
    ///</summary>
    public static readonly Resource declinedParticipationStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#declinedParticipationStatus"));    

    ///<summary>
    ///The property defines one or more free or busy time intervals. Inspired by RFC 2445 sec. 4.8.2.6. Note that the periods specified by this property can only be expressed with UTC times. Originally this property could have many comma-separated values. Please use a separate triple for each value.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#freebusy"/>
    ///</summary>
    public static readonly Property freebusy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#freebusy"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#roomUserType"/>
    ///</summary>
    public static readonly Resource roomUserType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#roomUserType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#publicClassification"/>
    ///</summary>
    public static readonly Resource publicClassification = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#publicClassification"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#audioAction"/>
    ///</summary>
    public static readonly Resource audioAction = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#audioAction"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#reqParticipantRole"/>
    ///</summary>
    public static readonly Resource reqParticipantRole = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#reqParticipantRole"));    

    ///<summary>
    ///Links a timezone with it's daylight observance. This property has no direct equivalent in the RFC 2445. It has been inspired by the structure of the Vtimezone component defined in sec.4.6.5
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#daylight"/>
    ///</summary>
    public static readonly Property daylight = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#daylight"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#confidentialClassification"/>
    ///</summary>
    public static readonly Resource confidentialClassification = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#confidentialClassification"));    

    ///<summary>
    ///Alternate representation of the resources needed for an event or todo. Introduced to cover the ALTREP parameter of the resources property. See documentation for ncal:resources for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#resourcesAltRep"/>
    ///</summary>
    public static readonly Property resourcesAltRep = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#resourcesAltRep"));    

    ///<summary>
    ///To specify an alternate inline encoding for the property value. Inspired by RFC 2445 sec. 4.2.7. Originally this property had a limited vocabulary. ('8BIT' and 'BASE64'). The terms of this vocabulary have been expressed as instances of the AttachmentEncoding class
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#encoding"/>
    ///</summary>
    public static readonly Property encoding = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#encoding"));    

    ///<summary>
    ///Frequency of a recurrence rule. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#freq"/>
    ///</summary>
    public static readonly Property freq = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#freq"));    

    ///<summary>
    ///Provide a grouping of component properties that defines a time zone.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Timezone"/>
    ///</summary>
    public static readonly Class Timezone = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Timezone"));    

    ///<summary>
    ///An attendee of an event. This class has been introduced to serve as the range for ncal:attendee property. See documentation of ncal:attendee for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Attendee"/>
    ///</summary>
    public static readonly Class Attendee = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Attendee"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TimezoneObservance"/>
    ///</summary>
    public static readonly Class TimezoneObservance = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TimezoneObservance"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfTimezoneObservanceEventFreebusyTimezoneTodo"/>
    ///</summary>
    public static readonly Class UnionOfTimezoneObservanceEventFreebusyTimezoneTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfTimezoneObservanceEventFreebusyTimezoneTodo"));    

    ///<summary>
    ///Attachment encoding. This class has been introduced to express the limited vocabulary of values for the ncal:encoding property. See the documentation of ncal:encoding for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AttachmentEncoding"/>
    ///</summary>
    public static readonly Class AttachmentEncoding = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AttachmentEncoding"));    

    ///<summary>
    ///To specify the relationship of the alarm trigger with respect to the start or end of the calendar component. Inspired by RFC 2445 4.2.14. The RFC has specified two possible values for this property ('START' and 'END') they have been expressed as instances of the TriggerRelation class.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#related"/>
    ///</summary>
    public static readonly Property related = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#related"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventFreebusy"/>
    ///</summary>
    public static readonly Class UnionOfEventFreebusy = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventFreebusy"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventJournalTimezoneTodo"/>
    ///</summary>
    public static readonly Class UnionOfEventJournalTimezoneTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventJournalTimezoneTodo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventTodo"/>
    ///</summary>
    public static readonly Class UnionOfAlarmEventTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventTodo"));    

    ///<summary>
    ///Non-processing information intended to provide a comment to the calendar user. Inspired by RFC 2445 sec. 4.8.1.4 with the following reservations:  the LANGUAGE parameter has been discarded. Please use xml:lang literals to express language. For the ALTREP parameter use the commentAltRep property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#comment"/>
    ///</summary>
    public static readonly Property comment = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#comment"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#completedParticipationStatus"/>
    ///</summary>
    public static readonly Resource completedParticipationStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#completedParticipationStatus"));    

    ///<summary>
    ///Participation Status. This class has been introduced to express the limited vocabulary of values for the ncal:partstat property. See the documentation of ncal:partstat for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#ParticipationStatus"/>
    ///</summary>
    public static readonly Class ParticipationStatus = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#ParticipationStatus"));    

    ///<summary>
    ///A DataObject found in a calendar. It is usually interpreted as one of the calendar entity types (e.g. Event, Journal, Todo etc.)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarDataObject"/>
    ///</summary>
    public static readonly Class CalendarDataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarDataObject"));    

    ///<summary>
    ///A calendar user type. This class has been introduced to express the limited vocabulary for the ncal:cutype property. See documentation of ncal:cutype for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarUserType"/>
    ///</summary>
    public static readonly Class CalendarUserType = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarUserType"));    

    ///<summary>
    ///The day that's counted as the start of the week. It is used to disambiguate the byweekno rule. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#wkst"/>
    ///</summary>
    public static readonly Property wkst = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#wkst"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#acceptedParticipationStatus"/>
    ///</summary>
    public static readonly Resource acceptedParticipationStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#acceptedParticipationStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#wednesday"/>
    ///</summary>
    public static readonly Resource wednesday = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#wednesday"));    

    ///<summary>
    ///An object attached to a calendar entity. This class has been introduced to serve as a structured value of the ncal:attach property. See the documentation of ncal:attach for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Attachment"/>
    ///</summary>
    public static readonly Class Attachment = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Attachment"));    

    ///<summary>
    ///A calendar scale. This class has been introduced to provide the limited vocabulary for the ncal:calscale property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarScale"/>
    ///</summary>
    public static readonly Class CalendarScale = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CalendarScale"));    

    ///<summary>
    ///To specify the group or list membership of the calendar user specified by the property. Inspired by RFC 2445 sec. 4.2.11. Originally this parameter had a value type of CAL-ADDRESS. This has been expressed as nco:Contact to promote integration between NCAL and NCO
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#member"/>
    ///</summary>
    public static readonly Property member = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#member"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#procedureAction"/>
    ///</summary>
    public static readonly Resource procedureAction = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#procedureAction"));    

    ///<summary>
    ///Access classification of a calendar component. Introduced to express 
    ///the set of values for the ncal:class property. The user may use instances
    ///provided with this ontology or create his/her own with desired semantics.
    ///See the documentation of ncal:class for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AccessClassification"/>
    ///</summary>
    public static readonly Class AccessClassification = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AccessClassification"));    

    ///<summary>
    ///Defines whether an event is transparent or not  to busy time searches. Inspired by RFC 2445 sec.4.8.2.7. Values for this property can be chosen from a limited vocabulary. To express this a TimeTransparency class has been introduced.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#transp"/>
    ///</summary>
    public static readonly Property transp = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#transp"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#needsActionParticipationStatus"/>
    ///</summary>
    public static readonly Resource needsActionParticipationStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#needsActionParticipationStatus"));    

    ///<summary>
    ///An organizer of an event. This class has been introduced to serve as a range of ncal:organizer property. See documentation of ncal:organizer for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Organizer"/>
    ///</summary>
    public static readonly Class Organizer = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Organizer"));    

    ///<summary>
    ///A common superclass for ncal:Attendee and ncal:Organizer.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AttendeeOrOrganizer"/>
    ///</summary>
    public static readonly Class AttendeeOrOrganizer = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AttendeeOrOrganizer"));    

    ///<summary>
    ///An aggregate of a period and a freebusy type. This class has been introduced to serve as a range of the ncal:freebusy property. See documentation for ncal:freebusy for details. Note that the specification of freebusy property states that the period is to be expressed using UTC time, so the timezone properties should NOT be used for instances of this class.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#FreebusyPeriod"/>
    ///</summary>
    public static readonly Class FreebusyPeriod = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#FreebusyPeriod"));    

    ///<summary>
    ///Specifies the customary designation for a timezone description. Inspired by RFC 2445 sec. 4.8.3.2 The LANGUAGE parameter has been discarded. Please xml:lang literals to express languages. Original specification for the domain of this property stated that it must appear within the timezone component. In this ontology the TimezoneObservance class has been itroduced to clarify this specification.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzname"/>
    ///</summary>
    public static readonly Property tzname = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzname"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#chairRole"/>
    ///</summary>
    public static readonly Resource chairRole = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#chairRole"));    

    ///<summary>
    ///A role the attendee is going to play during an event. This class has been introduced to express the limited vocabulary for the values of ncal:role property. Please refer to the documentation of ncal:role for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AttendeeRole"/>
    ///</summary>
    public static readonly Class AttendeeRole = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AttendeeRole"));    

    ///<summary>
    ///Alternate representation of the comment. Introduced to cover 
    ///the ALTREP parameter of the COMMENT property. See 
    ///documentation of ncal:comment for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#commentAltRep"/>
    ///</summary>
    public static readonly Property commentAltRep = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#commentAltRep"));    

    ///<summary>
    ///This property defines the number of time the alarm should be repeated, after the initial trigger. Inspired by RFC 2445 sec. 4.8.6.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#repeat"/>
    ///</summary>
    public static readonly Property repeat = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#repeat"));    

    ///<summary>
    ///A contact of the Attendee or the organizer involved in an event or other calendar entity. This property has been introduced to express the actual value of the ATTENDEE and ORGANIZER properties. The contact will also represent the CN parameter of those properties. See documentation of ncal:attendee or ncal:organizer for more details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#involvedContact"/>
    ///</summary>
    public static readonly Property involvedContact = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#involvedContact"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#inProcessStatus"/>
    ///</summary>
    public static readonly Resource inProcessStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#inProcessStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#unknownUserType"/>
    ///</summary>
    public static readonly Resource unknownUserType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#unknownUserType"));    

    ///<summary>
    ///Day of the year the event should occur. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byyearday"/>
    ///</summary>
    public static readonly Property byyearday = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byyearday"));    

    ///<summary>
    ///The property specifies a positive duration of time. Inspired by RFC 2445 sec. 4.8.2.5
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#duration"/>
    ///</summary>
    public static readonly Property duration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#duration"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#weekly"/>
    ///</summary>
    public static readonly Resource weekly = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#weekly"));    

    ///<summary>
    ///The uri of the attachment. Created to express the actual value of the ATTACH property defined in RFC 2445 sec. 4.8.1.1. This property expresses the BINARY datatype of that property. see ncal:attachmentUri for the URI datatype.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attachmentContent"/>
    ///</summary>
    public static readonly Property attachmentContent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attachmentContent"));    

    ///<summary>
    ///Provide a grouping of calendar properties that describe a to-do.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Todo"/>
    ///</summary>
    public static readonly Class Todo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Todo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#gregorianCalendarScale"/>
    ///</summary>
    public static readonly Resource gregorianCalendarScale = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#gregorianCalendarScale"));    

    ///<summary>
    ///To specify the calendar users to whom the calendar user specified by the property has delegated participation. Inspired by RFC 2445 sec. 4.2.5. Originally the value type for this parameter was CAL-ADDRESS. This has been expressed as nco:Contact to promote integration between NCAL and NCO.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#delegatedTo"/>
    ///</summary>
    public static readonly Property delegatedTo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#delegatedTo"));    

    ///<summary>
    ///This property specifies when an alarm will trigger. Inspired by RFC 2445 sec. 4.8.6.3 Originally the value of this property could accept two types : duration and date-time. To express this fact a Trigger class has been introduced. It also has a related property to account for the RELATED parameter.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#trigger"/>
    ///</summary>
    public static readonly Property trigger = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#trigger"));    

    ///<summary>
    ///The UNTIL rule part defines a date-time value which bounds the recurrence rule in an inclusive manner. If the value specified by UNTIL is synchronized with the specified recurrence, this date or date-time becomes the last instance of the recurrence. If specified as a date-time value, then it MUST be specified in an UTC time format. If not present, and the COUNT rule part is also not present, the RRULE is considered to repeat forever.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#until"/>
    ///</summary>
    public static readonly Property until = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#until"));    

    ///<summary>
    ///This property defines the date and time that a to-do was actually completed. Inspired by RFC 2445 sec. 4.8.2.1. Note that the RFC allows ONLY UTC time values for this property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#completed"/>
    ///</summary>
    public static readonly Property completed = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#completed"));    

    ///<summary>
    ///The property defines the relative priority for a calendar component. Inspired by RFC 2445 sec. 4.8.1.9
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#priority"/>
    ///</summary>
    public static readonly Property priority = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#priority"));    

    ///<summary>
    ///Second of a recurrence. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bysecond"/>
    ///</summary>
    public static readonly Property bysecond = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bysecond"));    

    ///<summary>
    ///Longer return status description. Inspired by the second part of the structured value of the REQUEST-STATUS property defined in RFC 2445 sec. 4.8.8.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#statusDescription"/>
    ///</summary>
    public static readonly Property statusDescription = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#statusDescription"));    

    ///<summary>
    ///An integer modifier for the BYDAY rule part.    Each BYDAY value can also be preceded by a positive (+n) or negative  (-n) integer. If present, this indicates the nth occurrence of the specific day within the MONTHLY or YEARLY RRULE. For example, within a MONTHLY rule, +1MO (or simply 1MO) represents the first Monday within the month, whereas -1MO represents the last Monday of the month. If an integer modifier is not present, it means all days of this type within the specified frequency. For example, within a MONTHLY rule, MO represents all Mondays within the month. Inspired by RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bydayModifier"/>
    ///</summary>
    public static readonly Property bydayModifier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bydayModifier"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tuesday"/>
    ///</summary>
    public static readonly Resource tuesday = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tuesday"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#endTriggerRelation"/>
    ///</summary>
    public static readonly Resource endTriggerRelation = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#endTriggerRelation"));    

    ///<summary>
    ///The INTERVAL rule part contains a positive integer representing how often the recurrence rule repeats. The default value is "1", meaning every second for a SECONDLY rule, or every minute for a MINUTELY rule, every hour for an HOURLY rule, every day for a DAILY rule, every week for a WEEKLY rule, every month for a MONTHLY rule andevery year for a YEARLY rule. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#interval"/>
    ///</summary>
    public static readonly Property interval = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#interval"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#friday"/>
    ///</summary>
    public static readonly Resource friday = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#friday"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#thisAndPriorRange"/>
    ///</summary>
    public static readonly Resource thisAndPriorRange = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#thisAndPriorRange"));    

    ///<summary>
    ///The duration of a trigger. This property has been created to express the VALUE=DURATION parameter of the TRIGGER property. See documentation for ncal:trigger for more details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#triggerDuration"/>
    ///</summary>
    public static readonly Property triggerDuration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#triggerDuration"));    

    ///<summary>
    ///The property defines the organizer for a calendar component. Inspired by RFC 2445 sec. 4.8.4.3. Originally this property accepted many parameters. The Organizer class has been introduced to express them all. Note that NCAL is aligned with NCO. The actual value (of the CAL-ADDRESS type) is expressed as an instance of nco:Contact. Remember that the CN parameter has been removed from NCAL. Instead that value should be expressed using nco:fullname property of the above mentioned nco:Contact instance.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#organizer"/>
    ///</summary>
    public static readonly Property organizer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#organizer"));    

    ///<summary>
    ///This property defines a rule or repeating pattern for recurring events, to-dos, or time zone definitions. sec. 4.8.5.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#rrule"/>
    ///</summary>
    public static readonly Property rrule = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#rrule"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tentativeParticipationStatus"/>
    ///</summary>
    public static readonly Resource tentativeParticipationStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tentativeParticipationStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#busyFreebusyType"/>
    ///</summary>
    public static readonly Resource busyFreebusyType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#busyFreebusyType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#monthly"/>
    ///</summary>
    public static readonly Resource monthly = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#monthly"));    

    ///<summary>
    ///The BYSETPOS rule part specify values which correspond to the nth occurrence within the set of events specified by the rule. Valid values are 1 to 366 or -366 to -1. It MUST only be used in conjunction with another BYxxx rule part. For example "the last work day of the month" could be represented as: RRULE: FREQ=MONTHLY; BYDAY=MO, TU, WE, TH, FR; BYSETPOS=-1. Each BYSETPOS value can include a positive (+n) or negative (-n)  integer. If present, this indicates the nth occurrence of the  specific occurrence within the set of events specified by the rule. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bysetpos"/>
    ///</summary>
    public static readonly Property bysetpos = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bysetpos"));    

    ///<summary>
    ///Alternate representation of the contact property. Introduced to cover 
    ///the ALTREP parameter of the CONTACT property. See 
    ///documentation of ncal:contact for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#contactAltRep"/>
    ///</summary>
    public static readonly Property contactAltRep = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#contactAltRep"));    

    ///<summary>
    ///The date and time of a recurrence identifier. Provided to express the actual value of the ncal:recurrenceId property. See documentation for ncal:recurrenceId for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#recurrenceIdDateTime"/>
    ///</summary>
    public static readonly Property recurrenceIdDateTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#recurrenceIdDateTime"));    

    ///<summary>
    ///Short return status. Inspired by the first element of the structured value of the REQUEST-STATUS property described in RFC 2445 sec. 4.8.8.2.
    ///
    ///The short return status is a PERIOD character (US-ASCII decimal 46) separated 3-tuple of integers. For example, "3.1.1". The successive  levels of integers provide for a successive level of status code granularity.
    ///
    ///The following are initial classes for the return status code. Individual iCalendar object methods will define specific return status codes for these classes. In addition, other classes for the return status code may be defined using the registration process defined later in this memo.
    ///
    /// 1.xx - Preliminary success. This class of status of status code indicates that the request has request has been initially processed but that completion is pending.
    ///
    ///2.xx -Successful. This class of status code indicates that the request was completed successfuly. However, the exact status code can indicate that a fallback has been taken.
    ///
    ///3.xx - Client Error. This class of status code indicates that the request was not successful. The error is the result of either a syntax or a semantic error in the client formatted request. Request should not be retried until the condition in the request is corrected.
    ///
    ///4.xx - Scheduling Error. This class of status code indicates that the request was not successful. Some sort of error occurred within the  calendaring and scheduling service, not directly related to the request itself.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#returnStatus"/>
    ///</summary>
    public static readonly Property returnStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#returnStatus"));    

    ///<summary>
    ///This property defines a rule or repeating pattern for an exception to a recurrence set. Inspired by RFC 2445 sec. 4.8.5.2.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#exrule"/>
    ///</summary>
    public static readonly Property exrule = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#exrule"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#confirmedStatus"/>
    ///</summary>
    public static readonly Resource confirmedStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#confirmedStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#groupUserType"/>
    ///</summary>
    public static readonly Resource groupUserType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#groupUserType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#finalStatus"/>
    ///</summary>
    public static readonly Resource finalStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#finalStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledJournalStatus"/>
    ///</summary>
    public static readonly Resource cancelledJournalStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledJournalStatus"));    

    ///<summary>
    ///References the event which is the first in a sequence of recurring events.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#firstEvent"/>
    ///</summary>
    public static readonly Property firstEvent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#firstEvent"));    

    ///<summary>
    ///References the event which is the next in a sequence of recurring events.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#nextEvent"/>
    ///</summary>
    public static readonly Property nextEvent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#nextEvent"));    

    ///<summary>
    ///The timezone instance that should be used to interpret an NcalDateTime. The purpose of this property is similar to the TZID parameter specified in RFC 2445 sec. 4.2.19
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#ncalTimezone"/>
    ///</summary>
    public static readonly Property ncalTimezone = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#ncalTimezone"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#NcalDateTime"/>
    ///</summary>
    public static readonly Class NcalDateTime = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#NcalDateTime"));    

    ///<summary>
    ///To specify the calendar users that have delegated their participation to the calendar user specified by the property. Inspired by RFC 2445 sec. 4.2.4. Originally the value type for this property was CAL-ADDRESS. This has been expressed as nco:Contact to promote integration between NCAL and NCO.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#delegatedFrom"/>
    ///</summary>
    public static readonly Property delegatedFrom = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#delegatedFrom"));    

    ///<summary>
    ///The relation between the trigger and its parent calendar component. This class has been introduced to express the limited vocabulary for the ncal:related property. See the documentation for ncal:related for more details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TriggerRelation"/>
    ///</summary>
    public static readonly Class TriggerRelation = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TriggerRelation"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventFreebusyJournalTodo"/>
    ///</summary>
    public static readonly Class UnionOfAlarmEventFreebusyJournalTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventFreebusyJournalTodo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventFreebusyJournalTodo"/>
    ///</summary>
    public static readonly Class UnionOfEventFreebusyJournalTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfEventFreebusyJournalTodo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventFreebusyTodo"/>
    ///</summary>
    public static readonly Class UnionOfAlarmEventFreebusyTodo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#UnionOfAlarmEventFreebusyTodo"));    

    ///<summary>
    ///This property specifies the date and time that the calendar information was created by the calendar user agent in the calendar store. Note: This is analogous to the creation date and time for a file in the file system. Inspired by RFC 2445 sec. 4.8.7.1. Note that this property is a subproperty of nie:created. The domain of nie:created is nie:DataObject. It is not a superclass of UnionOf_Vevent_Vjournal_Vtodo, but since that union is conceived as an 'abstract' class, and in real-life all resources referenced by this property will also be DataObjects, than this shouldn't cause too much of a problem. Note that RFC allows ONLY UTC time values for this property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#created"/>
    ///</summary>
    public static readonly Property created = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#created"));    

    ///<summary>
    ///A calendar. Inspirations for this class can be traced to the VCALENDAR component defined in RFC 2445 sec. 4.4, but it may just as well be used to represent any kind of Calendar.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Calendar"/>
    ///</summary>
    public static readonly Class Calendar = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Calendar"));    

    ///<summary>
    ///To specify the type of calendar user specified by the property. Inspired by RFC 2445 sec. 4.2.3. This parameter has a limited vocabulary. The terms that may serve as values for this property have been expressed as instances of CalendarUserType class. The user may use instances provided with this ontology or create his own.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cutype"/>
    ///</summary>
    public static readonly Property cutype = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cutype"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceRule"/>
    ///</summary>
    public static readonly Class RecurrenceRule = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceRule"));    

    ///<summary>
    ///The TZURL provides a means for a VTIMEZONE component to point to a network location that can be used to retrieve an up-to- date version of itself. Inspired by RFC 2445 sec. 4.8.3.5. Originally the range of this property had been specified as URI.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzurl"/>
    ///</summary>
    public static readonly Property tzurl = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzurl"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#displayAction"/>
    ///</summary>
    public static readonly Resource displayAction = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#displayAction"));    

    ///<summary>
    ///Action to be performed on alarm. This class has been introduced to express the limited set of values of the ncal:action property. Please refer to the documentation of ncal:action for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AlarmAction"/>
    ///</summary>
    public static readonly Class AlarmAction = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#AlarmAction"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#resourceUserType"/>
    ///</summary>
    public static readonly Resource resourceUserType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#resourceUserType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledEventStatus"/>
    ///</summary>
    public static readonly Resource cancelledEventStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#cancelledEventStatus"));    

    ///<summary>
    ///A status of an event. This class has been introduced to express
    ///the limited set of values for the ncal:status property. The user may
    ///use the instances provided with this ontology or create his/her own.
    ///See the documentation for ncal:eventStatus for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#EventStatus"/>
    ///</summary>
    public static readonly Class EventStatus = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#EventStatus"));    

    ///<summary>
    ///Time transparency. Introduced to provide a way to express the limited vocabulary for the values of ncal:transp property. See documentation of ncal:transp for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TimeTransparency"/>
    ///</summary>
    public static readonly Class TimeTransparency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TimeTransparency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#individualUserType"/>
    ///</summary>
    public static readonly Resource individualUserType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#individualUserType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#delegatedParticipationStatus"/>
    ///</summary>
    public static readonly Resource delegatedParticipationStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#delegatedParticipationStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#busyTentativeFreebusyType"/>
    ///</summary>
    public static readonly Resource busyTentativeFreebusyType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#busyTentativeFreebusyType"));    

    ///<summary>
    ///Type of a Freebusy indication. This class has been introduced to serve as a limited set of values for the ncal:fbtype property. See the documentation of ncal:fbtype for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#FreebusyType"/>
    ///</summary>
    public static readonly Class FreebusyType = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#FreebusyType"));    

    ///<summary>
    ///This property specifies the date and time that a calendar component ends. Inspired by RFC 2445 sec. 4.8.2.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dtend"/>
    ///</summary>
    public static readonly Property dtend = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dtend"));    

    ///<summary>
    ///Representation of a date an instance of NcalDateTime actually refers to. It's purpose is to express values in DATE-TIME datatype, as defined in RFC 2445 sec. 4.3.5
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dateTime"/>
    ///</summary>
    public static readonly Property dateTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dateTime"));    

    ///<summary>
    ///A common superproperty for all types of ncal relations. It is not to be used directly.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#ncalRelation"/>
    ///</summary>
    public static readonly Property ncalRelation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#ncalRelation"));    

    ///<summary>
    ///Frequency of a recurrence rule. This class has been introduced to express a limited set of allowed values for the ncal:freq property. See the documentation of ncal:freq for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceFrequency"/>
    ///</summary>
    public static readonly Class RecurrenceFrequency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceFrequency"));    

    ///<summary>
    ///To specify the free or busy time type. Inspired by RFC 2445 sec. 4.2.9. The RFC specified a limited vocabulary for the values of this property. The terms of this vocabulary have been expressed as instances of the FreebusyType class. The user can use instances provided with this ontology or create his own.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#fbtype"/>
    ///</summary>
    public static readonly Property fbtype = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#fbtype"));    

    ///<summary>
    ///Alternate representation of the calendar entity description. Introduced to cover 
    ///the ALTREP parameter of the DESCRIPTION property. See 
    ///documentation of ncal:description for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#descriptionAltRep"/>
    ///</summary>
    public static readonly Property descriptionAltRep = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#descriptionAltRep"));    

    ///<summary>
    ///The property is used to represent contact information or alternately a reference to contact information associated with the calendar component. Inspired by RFC 2445 sec. 4.8.4.2 with the following reservations: the LANGUAGE parameter has been discarded. Please use xml:lang literals to express language. For the ALTREP parameter use the contactAltRep property.RFC doesn't define any format for the string.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#contact"/>
    ///</summary>
    public static readonly Property contact = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#contact"));    

    ///<summary>
    ///Provide a grouping of component properties that define an alarm.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Alarm"/>
    ///</summary>
    public static readonly Class Alarm = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Alarm"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#freeFreebusyType"/>
    ///</summary>
    public static readonly Resource freeFreebusyType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#freeFreebusyType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#startTriggerRelation"/>
    ///</summary>
    public static readonly Resource startTriggerRelation = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#startTriggerRelation"));    

    ///<summary>
    ///To specify whether there is an expectation of a favor of a reply from the calendar user specified by the property value. Inspired by RFC 2445 sec. 4.2.17
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#rsvp"/>
    ///</summary>
    public static readonly Property rsvp = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#rsvp"));    

    ///<summary>
    ///This property specifies information related to the global position for the activity specified by a calendar component. Inspired by RFC 2445 sec. 4.8.1.6
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#geo"/>
    ///</summary>
    public static readonly Property geo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#geo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#hourly"/>
    ///</summary>
    public static readonly Resource hourly = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#hourly"));    

    ///<summary>
    ///To specify the content type of a referenced object. Inspired by RFC 2445 sec. 4.2.8. The value of this property should be an IANA-registered content type (e.g. application/binary)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#fmttype"/>
    ///</summary>
    public static readonly Property fmttype = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#fmttype"));    

    ///<summary>
    ///A more complete description of the calendar component, than  that provided by the ncal:summary property.Inspired by RFC 2445 sec. 4.8.1.5 with following reservations:  the LANGUAGE parameter has been discarded. Please use xml:lang literals to express language. For the ALTREP parameter use the descriptionAltRep property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#description"/>
    ///</summary>
    public static readonly Property description = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#description"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#thursday"/>
    ///</summary>
    public static readonly Resource thursday = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#thursday"));    

    ///<summary>
    ///To specify the participation status for the calendar user specified by the property. Inspired by RFC 2445 sec. 4.2.12. Originally this parameter had three sets of allowed values. Which set applied to a particular case - depended on the type of calendar entity this parameter occured in. (event, todo, journal entry). This would be awkward to model in RDF so a single ParticipationStatus class has been introduced. Terms of the values vocabulary are expressed as instances of this class. Users are advised to pay attention which instances they use.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#partstat"/>
    ///</summary>
    public static readonly Property partstat = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#partstat"));    

    ///<summary>
    ///Number of the month of the recurrence. Valid values are integers from 1 (January) to 12 (December). Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bymonth"/>
    ///</summary>
    public static readonly Property bymonth = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#bymonth"));    

    ///<summary>
    ///To specify the calendar user that is acting on behalf of the calendar user specified by the property. Inspired by RFC 2445 sec. 4.2.18. The original data type of this property was a mailto: URI. This has been changed to nco:Contact to promote integration between NCO and NCAL.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#sentBy"/>
    ///</summary>
    public static readonly Property sentBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#sentBy"));    

    ///<summary>
    ///The property provides the capability to associate a document object with a calendar component. Defined in the RFC 2445 sec. 4.8.1.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attach"/>
    ///</summary>
    public static readonly Property attach = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attach"));    

    ///<summary>
    ///Recurrence Identifier. Introduced to provide a structure for the value of ncal:recurrenceId property. See the documentation of ncal:recurrenceId for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceIdentifier"/>
    ///</summary>
    public static readonly Class RecurrenceIdentifier = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurrenceIdentifier"));    

    ///<summary>
    ///This property defines a Uniform Resource Locator (URL) associated with the iCalendar object. Inspired by the RFC 2445 sec. 4.8.4.6. Original range had been specified as URI.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#url"/>
    ///</summary>
    public static readonly Property url = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#url"));    

    ///<summary>
    ///Provide a grouping of component properties that describe either a request for free/busy time, describe a response to a request for free/busy time or describe a published set of busy time.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Freebusy"/>
    ///</summary>
    public static readonly Class Freebusy = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#Freebusy"));    

    ///<summary>
    ///A status of a journal entry. This class has been introduced to express
    ///the limited set of values for the ncal:status property. The user may
    ///use the instances provided with this ontology or create his/her own.
    ///See the documentation for ncal:journalStatus for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#JournalStatus"/>
    ///</summary>
    public static readonly Class JournalStatus = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#JournalStatus"));    

    ///<summary>
    ///Defines the intended venue for the activity defined by a calendar component. Inspired by RFC 2445 sec 4.8.1.7 with the following reservations:  the LANGUAGE parameter has been discarded. Please use xml:lang literals to express language.  For the ALTREP parameter use the locationAltRep property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#location"/>
    ///</summary>
    public static readonly Property location = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#location"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#minutely"/>
    ///</summary>
    public static readonly Resource minutely = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#minutely"));    

    ///<summary>
    ///The exact date and time of the trigger. This property has been created to express the VALUE=DATE, and VALUE=DATE-TIME parameters of the TRIGGER property. See the documentation for ncal:trigger for more details
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#triggerDateTime"/>
    ///</summary>
    public static readonly Property triggerDateTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#triggerDateTime"));    

    ///<summary>
    ///Specifies a reference to a directory entry associated with the calendar user specified by the property. Inspired by RFC 2445 sec. 4.2.6. Originally the data type of the value of this parameter was URI (Usually an LDAP URI). This has been expressed as rdfs:resource.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dir"/>
    ///</summary>
    public static readonly Property dir = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#dir"));    

    ///<summary>
    ///This property defines the status code returned for a scheduling request. Inspired by RFC 2445 sec. 4.8.8.2. Original value of this property was a four-element structure. The RequestStatus class has been introduced to express it. In RFC 2445 this property could have the LANGUAGE parameter. This has been discarded in this ontology. Use xml:lang literals to express it if necessary.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#requestStatus"/>
    ///</summary>
    public static readonly Property requestStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#requestStatus"));    

    ///<summary>
    ///This property is used in conjunction with the "UID" and "SEQUENCE" property to identify a specific instance of a recurring "VEVENT", "VTODO" or "VJOURNAL" calendar component. The property value is the effective value of the "DTSTART" property of the recurrence instance. Inspired by the RFC 2445 sec. 4.8.4.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#recurrenceId"/>
    ///</summary>
    public static readonly Property recurrenceId = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#recurrenceId"));    

    ///<summary>
    ///The number of the week an event should recur. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byweekno"/>
    ///</summary>
    public static readonly Property byweekno = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byweekno"));    

    ///<summary>
    ///End of a period of time. Inspired by the second part of a structured value of a PERIOD datatype specified in RFC 2445 sec. 4.3.9. Note that a single NcalPeriod instance shouldn't have the periodEnd and periodDuration properties specified simultaneously.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#periodEnd"/>
    ///</summary>
    public static readonly Property periodEnd = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#periodEnd"));    

    ///<summary>
    ///The property defines an "Attendee" within a calendar component. Inspired by RFC 2445 sec. 4.8.4.1. Originally this property accepted many parameters. The Attendee class has been introduced to express them all. Note that NCAL is aligned with NCO. The actual value (of the CAL-ADDRESS type) is expressed as an instance of nco:Contact. Remember that the CN parameter has been removed from NCAL. Instead that value should be expressed using nco:fullname property of the above mentioned nco:Contact instance. The RFC stated that whenever this property is attached to a Valarm instance, the Attendee cannot have any parameters apart from involvedContact.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attendee"/>
    ///</summary>
    public static readonly Property attendee = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#attendee"));    

    ///<summary>
    ///This property defines the list of date/time exceptions for a recurring calendar component. Inspired by RFC 2445 sec. 4.8.5.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#exdate"/>
    ///</summary>
    public static readonly Property exdate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#exdate"));    

    ///<summary>
    ///To specify the participation role for the calendar user specified by the property. Inspired by the RFC 2445 sec. 4.2.16. Originally this property had a limited vocabulary for values. The terms of that vocabulary have been expressed as instances of the AttendeeRole class.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#role"/>
    ///</summary>
    public static readonly Property role = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#role"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#optParticipantRole"/>
    ///</summary>
    public static readonly Resource optParticipantRole = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#optParticipantRole"));    

    ///<summary>
    ///This property specifies the identifier corresponding to the highest version number or the minimum and maximum range of the iCalendar specification that is required in order to interpret the iCalendar object. Defined in RFC 2445 sec. 4.7.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#version"/>
    ///</summary>
    public static readonly Property version = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#version"));    

    ///<summary>
    ///To specify the effective range of recurrence instances from the instance specified by the recurrence identifier specified by the property. It is intended to express the RANGE parameter specified in RFC 2445 sec. 4.2.13. The set of possible values for this property is limited. See also the documentation for ncal:recurrenceId for more details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#range"/>
    ///</summary>
    public static readonly Property range = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#range"));    

    ///<summary>
    ///Defines the overall status or confirmation for a todo. Based on the STATUS property defined in RFC 2445 sec. 4.8.1.11.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#todoStatus"/>
    ///</summary>
    public static readonly Property todoStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#todoStatus"));    

    ///<summary>
    ///Defines the overall status or confirmation for a journal entry. Based on the STATUS property defined in RFC 2445 sec. 4.8.1.11.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#journalStatus"/>
    ///</summary>
    public static readonly Property journalStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#journalStatus"));    

    ///<summary>
    ///This property defines the date and time that a to-do is expected to be completed. Inspired by RFC 2445 sec. 4.8.2.3
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#due"/>
    ///</summary>
    public static readonly Property due = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#due"));    

    ///<summary>
    ///This property defines the list of date/times for a recurrence set. Inspired by RFC 2445 sec. 4.8.5.3. Note that RFC allows both DATE, DATE-TIME and PERIOD values for this property. That's why the range has been set to NcalTimeEntity.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#rdate"/>
    ///</summary>
    public static readonly Property rdate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#rdate"));    

    ///<summary>
    ///Alternate representation of the event or todo location. 
    ///Introduced to cover the ALTREP parameter of the LOCATION 
    ///property. See documentation of ncal:location for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#locationAltRep"/>
    ///</summary>
    public static readonly Property locationAltRep = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#locationAltRep"));    

    ///<summary>
    ///Links an event or a todo with a DataObject that can be interpreted as an alarm. This property has no direct equivalent in the RFC 2445. It has been provided to express this relation.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#hasAlarm"/>
    ///</summary>
    public static readonly Property hasAlarm = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#hasAlarm"));    

    ///<summary>
    ///Hour of recurrence. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byhour"/>
    ///</summary>
    public static readonly Property byhour = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byhour"));    

    ///<summary>
    ///Minute of recurrence. Defined in RFC 2445 sec. 4.3.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byminute"/>
    ///</summary>
    public static readonly Property byminute = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#byminute"));    

    ///<summary>
    ///Additional data associated with a request status. Inspired by the third part of the structured value for the REQUEST-STATUS property defined in RFC 2445 sec. 4.8.8.2 ("Textual exception data. For example, the offending property name and value or complete property line")
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#requestStatusData"/>
    ///</summary>
    public static readonly Property requestStatusData = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#requestStatusData"));    

    ///<summary>
    ///This property specifies the text value that uniquely identifies the "VTIMEZONE" calendar component. Inspired by RFC 2445 sec 4.8.3.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzid"/>
    ///</summary>
    public static readonly Property tzid = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#tzid"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#thisAndFutureRange"/>
    ///</summary>
    public static readonly Resource thisAndFutureRange = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#thisAndFutureRange"));    

    ///<summary>
    ///Categories for a calendar component. Inspired by RFC 2445 sec 4.8.1.2 with the following reservations: The LANGUAGE parameter has been discarded. Please use xml:lang literals to express multiple languages. This property can specify multiple comma-separated categories. The order of categories doesn't matter. Please use a separate triple for each category.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#categories"/>
    ///</summary>
    public static readonly Property categories = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#categories"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#transparentTransparency"/>
    ///</summary>
    public static readonly Resource transparentTransparency = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#transparentTransparency"));    

    ///<summary>
    ///Indicates if an event is occuring the whole day.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#isFullDayEvent"/>
    ///</summary>
    public static readonly Property isFullDayEvent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#isFullDayEvent"));    

    ///<summary>
    ///References the event which is the previous in a sequence of recurring events.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#previousEvent"/>
    ///</summary>
    public static readonly Property previousEvent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#previousEvent"));    

    ///<summary>
    ///Reincarnation of an event for which a recurrence rule was defined.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurringEvent"/>
    ///</summary>
    public static readonly Class RecurringEvent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#RecurringEvent"));    

    ///<summary>
    ///Denotes that an event belongs to the Calendar.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#containsEvent"/>
    ///</summary>
    public static readonly Property containsEvent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#containsEvent"));    

    ///<summary>
    ///Denotes that an event belongs to a Calendar.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#belongsToCalendar"/>
    ///</summary>
    public static readonly Property belongsToCalendar = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#belongsToCalendar"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TimedRecurrenceRule"/>
    ///</summary>
    public static readonly Class TimedRecurrenceRule = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#TimedRecurrenceRule"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CountedRecurrenceRule"/>
    ///</summary>
    public static readonly Class CountedRecurrenceRule = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/04/02/ncal#CountedRecurrenceRule"));
}

///<summary>
///
///
///</summary>
public class ndo : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "ndo";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///A file available via a peer-to-peer network
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#P2PFile"/>
    ///</summary>
    public static readonly Class P2PFile = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#P2PFile"));    

    ///<summary>
    ///A file available via a BitTorrent peer-to-peer network
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#TorrentedFile"/>
    ///</summary>
    public static readonly Class TorrentedFile = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#TorrentedFile"));    

    ///<summary>
    ///Links a DataObject with its copy. This relation means that originally the Data Objects were copies but might have changed subsequentially.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#copiedFrom"/>
    ///</summary>
    public static readonly Property copiedFrom = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#copiedFrom"));    

    ///<summary>
    ///Points to the Information Element that contained the link to the download source which was used in the download event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#referrer"/>
    ///</summary>
    public static readonly Property referrer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#referrer"));    

    ///<summary>
    ///A .torrent file which contains references(ndo:TorrentedFile) to files available via BitTorrent. The references are pointed to via nie:hasLogicalPart
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#Torrent"/>
    ///</summary>
    public static readonly Class Torrent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#Torrent"));    

    ///<summary>
    ///A single event (from the point of view of the user) of downloading of a file or a set of files. Use nuao:involves to indicate the files involved. The event is assigned to downloaded copies of files. Can be assigned to multiple files.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#DownloadEvent"/>
    ///</summary>
    public static readonly Class DownloadEvent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/04/30/ndo#DownloadEvent"));
}

///<summary>
///
///
///</summary>
public class nexif : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nexif";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///A photo, an image captured using a camera, an EXIF Image File Directory. Implementation notes: use nie:copyright to store copyright notices.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#Photo"/>
    ///</summary>
    public static readonly Class Photo = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#Photo"));    

    ///<summary>
    ///tagNumber: 34856
    ///Indicates the Opto-Electric Conversion Function (OECF) specified in ISO 14524. OECF is the relationship between the camera optical input and the image values.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#oecf"/>
    ///</summary>
    public static readonly Property oecf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#oecf"));    

    ///<summary>
    ///An attribute relating to image data structure
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageDataStruct"/>
    ///</summary>
    public static readonly Property imageDataStruct = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageDataStruct"));    

    ///<summary>
    ///tagNumber: 12
    ///Saturation info for print image matching
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimSaturation"/>
    ///</summary>
    public static readonly Property pimSaturation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimSaturation"));    

    ///<summary>
    ///The location of the main subject in the scene. The value of this tag represents the pixel at the center of the main subject relative to the left edge, prior to rotation processing as per the Rotation tag. The first value indicates the X column number and second indicates the Y row number.
    ///tagNumber: 41492
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectLocation"/>
    ///</summary>
    public static readonly Property subjectLocation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectLocation"));    

    ///<summary>
    ///tagNumber: 41487
    ///The number of pixels in the image height (Y) direction per FocalPlaneResolutionUnit on the camera focal plane.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalPlaneYResolution"/>
    ///</summary>
    public static readonly Property focalPlaneYResolution = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalPlaneYResolution"));    

    ///<summary>
    ///tagNumber: 40961
    ///The color space information tag (ColorSpace) is always recorded as the color space specifier. Normally sRGB (=1) is used to define the color space based on the PC monitor conditions and environment.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#colorSpace"/>
    ///</summary>
    public static readonly Property colorSpace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#colorSpace"));    

    ///<summary>
    ///tagNumber: 41992
    ///The direction of contrast processing applied by the camera when the image was shot.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#contrast"/>
    ///</summary>
    public static readonly Property contrast = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#contrast"));    

    ///<summary>
    ///tagNumber: 40960
    ///The Flashpix format version supported by a FPXR file. If the FPXR function supports Flashpix format Ver. 1.0, this is indicated similarly to ExifVersion by recording "0100" as 4-byte ASCII.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#flashpixVersion"/>
    ///</summary>
    public static readonly Property flashpixVersion = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#flashpixVersion"));    

    ///<summary>
    ///An attribute relating to Interoperability. Tags stored in
    ///Interoperability IFD may be defined dependently to each Interoperability rule.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interopInfo"/>
    ///</summary>
    public static readonly Property interopInfo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interopInfo"));    

    ///<summary>
    ///tagNumber: 14
    ///The reference for giving the direction of GPS receiver movement. 'T' denotes true direction and 'M' is magnetic direction.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsTrackRef"/>
    ///</summary>
    public static readonly Property gpsTrackRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsTrackRef"));    

    ///<summary>
    ///The direction of GPS receiver movement. The range of values is from 0.00 to 359.99.
    ///tagNumber: 15
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsTrack"/>
    ///</summary>
    public static readonly Property gpsTrack = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsTrack"));    

    ///<summary>
    ///tagNumber: 1
    ///Indicates whether the latitude is north or south latitude. The ASCII value 'N' indicates north latitude, and 'S' is south latitude.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsLatitudeRef"/>
    ///</summary>
    public static readonly Property gpsLatitudeRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsLatitudeRef"));    

    ///<summary>
    ///tagNumber: 283
    ///The number of pixels per ResolutionUnit in the ImageLength direction. The same value as XResolution is designated.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yResolution"/>
    ///</summary>
    public static readonly Property yResolution = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yResolution"));    

    ///<summary>
    ///The date and time of image creation. In this standard it is the date and time the file was changed.
    ///tagNumber: 306
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateTime"/>
    ///</summary>
    public static readonly Property dateTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateTime"));    

    ///<summary>
    ///The geodetic survey data used by the GPS receiver. If the survey data is restricted to Japan, the value of this tag is 'TOKYO' or 'WGS-84'. If a GPS Info tag is recorded, it is strongly recommended that this tag be recorded.
    ///tagNumber: 18
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsMapDatum"/>
    ///</summary>
    public static readonly Property gpsMapDatum = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsMapDatum"));    

    ///<summary>
    ///The Exif field data type, such as ascii, byte, short etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#datatype"/>
    ///</summary>
    public static readonly Property datatype = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#datatype"));    

    ///<summary>
    ///The distance to the destination point.
    ///tagNumber: 26
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestDistance"/>
    ///</summary>
    public static readonly Property gpsDestDistance = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestDistance"));    

    ///<summary>
    ///tagNumber: 41729
    ///The type of scene. If a DSC recorded the image, this tag value shall always be set to 1, indicating that the image was directly photographed.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sceneType"/>
    ///</summary>
    public static readonly Property sceneType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sceneType"));    

    ///<summary>
    ///The unit for measuring FocalPlaneXResolution and FocalPlaneYResolution. This value is the same as the ResolutionUnit.
    ///tagNumber: 41488
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalPlaneResolutionUnit"/>
    ///</summary>
    public static readonly Property focalPlaneResolutionUnit = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalPlaneResolutionUnit"));    

    ///<summary>
    ///tagNumber: 37382
    ///The distance to the subject, given in meters. Note that if the numerator of the recorded value is FFFFFFFF.H, Infinity shall be indicated; and if the numerator is 0, Distance unknown shall be indicated.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectDistance"/>
    ///</summary>
    public static readonly Property subjectDistance = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectDistance"));    

    ///<summary>
    ///tagNumber: 33434
    ///Exposure time, given in seconds (sec).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureTime"/>
    ///</summary>
    public static readonly Property exposureTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureTime"));    

    ///<summary>
    ///An attribute relating to Date and/or Time
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateAndOrTime"/>
    ///</summary>
    public static readonly Property dateAndOrTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateAndOrTime"));    

    ///<summary>
    ///Sharpness info for print image matching
    ///tagNumber: 13
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimSharpness"/>
    ///</summary>
    public static readonly Property pimSharpness = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimSharpness"));    

    ///<summary>
    ///Metering mode, such as CenterWeightedAverage, Spot, MultiSpot,Pattern, Partial etc.
    ///tagNumber: 37383
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#meteringMode"/>
    ///</summary>
    public static readonly Property meteringMode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#meteringMode"));    

    ///<summary>
    ///tagNumber: 37396
    ///The location and area of the main subject in the overall scene.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectArea"/>
    ///</summary>
    public static readonly Property subjectArea = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectArea"));    

    ///<summary>
    ///The equivalent focal length assuming a 35mm film camera, in mm. A value of 0 means the focal length is unknown. Note that this tag differs from the FocalLength tag.
    ///tagNumber: 41989
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalLengthIn35mmFilm"/>
    ///</summary>
    public static readonly Property focalLengthIn35mmFilm = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalLengthIn35mmFilm"));    

    ///<summary>
    ///The bearing to the destination point. The range of values is from 0.00 to 359.99.
    ///tagNumber: 24
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestBearing"/>
    ///</summary>
    public static readonly Property gpsDestBearing = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestBearing"));    

    ///<summary>
    ///The number of pixels per ResolutionUnit in the ImageWidth direction. When the image resolution is unknown, 72 [dpi] is designated.
    ///tagNumber: 282
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#xResolution"/>
    ///</summary>
    public static readonly Property xResolution = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#xResolution"));    

    ///<summary>
    ///Information specific to compressed data. When a compressed file is recorded, the valid width of the meaningful image shall be recorded in this tag, whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file.
    ///tagNumber: 40962
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pixelXDimension"/>
    ///</summary>
    public static readonly Property pixelXDimension = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pixelXDimension"));    

    ///<summary>
    ///The speed of GPS receiver movement.
    ///tagNumber: 13
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsSpeed"/>
    ///</summary>
    public static readonly Property gpsSpeed = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsSpeed"));    

    ///<summary>
    ///tagNumber: 41987
    ///The white balance mode set when the image was shot.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#whiteBalance"/>
    ///</summary>
    public static readonly Property whiteBalance = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#whiteBalance"));    

    ///<summary>
    ///Indicates the ISO Speed and ISO Latitude of the camera or input device as specified in ISO 12232.
    ///tagNumber: 34855
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#isoSpeedRatings"/>
    ///</summary>
    public static readonly Property isoSpeedRatings = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#isoSpeedRatings"));    

    ///<summary>
    ///An attribute relating to User Information
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#userInfo"/>
    ///</summary>
    public static readonly Property userInfo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#userInfo"));    

    ///<summary>
    ///Indicates the identification of the Interoperability rule. 'R98' = conforming to R98 file specification of Recommended Exif Interoperability Rules (ExifR98) or to DCF basic file stipulated by Design Rule for Camera File System. 'THM' = conforming to DCF thumbnail file stipulated by Design rule for Camera File System.
    ///tagNumber: 1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interoperabilityIndex"/>
    ///</summary>
    public static readonly Property interoperabilityIndex = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interoperabilityIndex"));    

    ///<summary>
    ///tagNumber: 37520
    ///DateTime subseconds
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subSecTime"/>
    ///</summary>
    public static readonly Property subSecTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subSecTime"));    

    ///<summary>
    ///Reference for longitude of destination
    ///tagNumber: 21
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestLongitudeRef"/>
    ///</summary>
    public static readonly Property gpsDestLongitudeRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestLongitudeRef"));    

    ///<summary>
    ///tagNumber: 305
    ///The name and version of the software or firmware of the camera or image input device used to generate the image.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#software"/>
    ///</summary>
    public static readonly Property software = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#software"));    

    ///<summary>
    ///Indicates the spectral sensitivity of each channel of the camera used. The tag value is an ASCII string compatible with the standard developed by the ASTM Technical committee.
    ///tagNumber: 34852
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#spectralSensitivity"/>
    ///</summary>
    public static readonly Property spectralSensitivity = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#spectralSensitivity"));    

    ///<summary>
    ///The number of bytes of JPEG compressed thumbnail data. This is not used for primary image JPEG data.
    ///tagNumber: 514
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#jpegInterchangeFormatLength"/>
    ///</summary>
    public static readonly Property jpegInterchangeFormatLength = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#jpegInterchangeFormatLength"));    

    ///<summary>
    ///tagNumber: 270
    ///A character string giving the title of the image. It may be a comment such as "1988 company picnic" or the like. Two-byte character codes cannot be used. When a 2-byte code is necessary, the Exif Private tag UserComment is to be used.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageDescription"/>
    ///</summary>
    public static readonly Property imageDescription = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageDescription"));    

    ///<summary>
    ///An Exif tag whose meaning is not known
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#unknown"/>
    ///</summary>
    public static readonly Property unknown = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#unknown"));    

    ///<summary>
    ///Indicates the unit used to express the distance to the destination point. 'K', 'M' and 'N' represent kilometers, miles and knots.
    ///tagNumber: 25
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestDistanceRef"/>
    ///</summary>
    public static readonly Property gpsDestDistanceRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestDistanceRef"));    

    ///<summary>
    ///Geometric data such as latitude, longitude and altitude. Usually saved as rational number.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#geo"/>
    ///</summary>
    public static readonly Property geo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#geo"));    

    ///<summary>
    ///tagNumber: 41991
    ///The degree of overall image gain adjustment.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gainControl"/>
    ///</summary>
    public static readonly Property gainControl = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gainControl"));    

    ///<summary>
    ///tagNumber: 19
    ///Reference for latitude of destination
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestLatitudeRef"/>
    ///</summary>
    public static readonly Property gpsDestLatitudeRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestLatitudeRef"));    

    ///<summary>
    ///tagNumber: 272
    ///Model of image input equipment
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#model"/>
    ///</summary>
    public static readonly Property model = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#model"));    

    ///<summary>
    ///The compression scheme used for the image data. When a primary image is JPEG compressed, this designation is not necessary and is omitted. When thumbnails use JPEG compression, this tag value is set to 6.
    ///tagNumber: 259
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#compression"/>
    ///</summary>
    public static readonly Property compression = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#compression"));    

    ///<summary>
    ///The number of components per pixel. Since this standard applies to RGB and YCbCr images, the value set for this tag is 3. In JPEG compressed data a JPEG marker is used instead of this tag.
    ///tagNumber: 277
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#samplesPerPixel"/>
    ///</summary>
    public static readonly Property samplesPerPixel = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#samplesPerPixel"));    

    ///<summary>
    ///The position of chrominance components in relation to the luminance component. This field is designated only for JPEG compressed data or uncompressed YCbCr data.
    ///tagNumber: 531
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yCbCrPositioning"/>
    ///</summary>
    public static readonly Property yCbCrPositioning = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yCbCrPositioning"));    

    ///<summary>
    ///tagNumber: 41483
    ///The strobe energy at the time the image is captured, as measured in Beam Candle Power Seconds (BCPS).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#flashEnergy"/>
    ///</summary>
    public static readonly Property flashEnergy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#flashEnergy"));    

    ///<summary>
    ///tagNumber: 37122
    ///Information specific to compressed data. The compression mode used for a compressed image is indicated in unit bits per pixel.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#compressedBitsPerPixel"/>
    ///</summary>
    public static readonly Property compressedBitsPerPixel = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#compressedBitsPerPixel"));    

    ///<summary>
    ///Tag Relating to Related File Information
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedFile"/>
    ///</summary>
    public static readonly Property relatedFile = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedFile"));    

    ///<summary>
    ///tagNumber: 37522
    ///DateTimeDigitized subseconds
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subSecTimeDigitized"/>
    ///</summary>
    public static readonly Property subSecTimeDigitized = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subSecTimeDigitized"));    

    ///<summary>
    ///tagNumber: 279
    ///The total number of bytes in each strip. With JPEG compressed data this designation is not needed and is omitted.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#stripByteCounts"/>
    ///</summary>
    public static readonly Property stripByteCounts = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#stripByteCounts"));    

    ///<summary>
    ///Manufacturer notes
    ///tagNumber: 37500
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#makerNote"/>
    ///</summary>
    public static readonly Property makerNote = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#makerNote"));    

    ///<summary>
    ///The chromaticity of the three primary colors of the image. Normally this tag is not necessary, since color space is specified in the color space information tag (ColorSpace).
    ///tagNumber: 319
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#primaryChromaticities"/>
    ///</summary>
    public static readonly Property primaryChromaticities = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#primaryChromaticities"));    

    ///<summary>
    ///tagNumber: 34850
    ///The class of the program used by the camera to set exposure when the picture is taken.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureProgram"/>
    ///</summary>
    public static readonly Property exposureProgram = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureProgram"));    

    ///<summary>
    ///A pointer to the Interoperability IFD, which is composed of tags storing the information to ensure the Interoperability
    ///tagNumber: 40965
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interoperabilityIFDPointer"/>
    ///</summary>
    public static readonly Property interoperabilityIFDPointer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interoperabilityIFDPointer"));    

    ///<summary>
    ///This tag records the camera or input device spatial frequency table and SFR values in the direction of image width, image height, and diagonal direction, as specified in ISO 12233.
    ///tagNumber: 41484
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#spatialFrequencyResponse"/>
    ///</summary>
    public static readonly Property spatialFrequencyResponse = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#spatialFrequencyResponse"));    

    ///<summary>
    ///Related image length
    ///tagNumber: 4098
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedImageLength"/>
    ///</summary>
    public static readonly Property relatedImageLength = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedImageLength"));    

    ///<summary>
    ///tagNumber: 9
    ///Contrast info for print image matching
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimContrast"/>
    ///</summary>
    public static readonly Property pimContrast = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimContrast"));    

    ///<summary>
    ///The distance to the subject, such as Macro, Close View or Distant View.
    ///tagNumber: 41996
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectDistanceRange"/>
    ///</summary>
    public static readonly Property subjectDistanceRange = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subjectDistanceRange"));    

    ///<summary>
    ///For each strip, the byte offset of that strip. With JPEG compressed data this designation is not needed and is omitted.
    ///tagNumber: 273
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#stripOffsets"/>
    ///</summary>
    public static readonly Property stripOffsets = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#stripOffsets"));    

    ///<summary>
    ///tagNumber: 41995
    ///Information on the picture-taking conditions of a particular camera model. The tag is used only to indicate the picture-taking conditions in the reader.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#deviceSettingDescription"/>
    ///</summary>
    public static readonly Property deviceSettingDescription = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#deviceSettingDescription"));    

    ///<summary>
    ///An identifier assigned uniquely to each image. It is recorded as an ASCII string equivalent to hexadecimal notation and 128-bit fixed length.
    ///tagNumber: 42016
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageUniqueID"/>
    ///</summary>
    public static readonly Property imageUniqueID = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageUniqueID"));    

    ///<summary>
    ///The image source. If a DSC recorded the image, this tag value of this tag always be set to 3, indicating that the image was recorded on a DSC.
    ///tagNumber: 41728
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#fileSource"/>
    ///</summary>
    public static readonly Property fileSource = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#fileSource"));    

    ///<summary>
    ///Width of an object
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#width"/>
    ///</summary>
    public static readonly Property width = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#width"));    

    ///<summary>
    ///tagNumber: 50341
    ///A pointer to the print image matching IFD
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#printImageMatchingIFDPointer"/>
    ///</summary>
    public static readonly Property printImageMatchingIFDPointer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#printImageMatchingIFDPointer"));    

    ///<summary>
    ///An attribute relating to recording offset
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#recOffset"/>
    ///</summary>
    public static readonly Property recOffset = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#recOffset"));    

    ///<summary>
    ///A property that connects an IFD (or other resource) to one of its entries (Exif attribute). Super property which integrates all Exif tags. Domain definition dropped so that this vocabulary can be used to describe not only Exif IFD, but also general image.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifAttribute"/>
    ///</summary>
    public static readonly Property exifAttribute = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifAttribute"));    

    ///<summary>
    ///a rational number representing a resolution. Could be a subProperty of other general schema.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#resolution"/>
    ///</summary>
    public static readonly Property resolution = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#resolution"));    

    ///<summary>
    ///Pixel composition. In JPEG compressed data a JPEG marker is used instead of this tag.
    ///tagNumber: 262
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#photometricInterpretation"/>
    ///</summary>
    public static readonly Property photometricInterpretation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#photometricInterpretation"));    

    ///<summary>
    ///tagNumber: 41495
    ///The image sensor type on the camera or input device, such as One-chip color area sensor etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sensingMethod"/>
    ///</summary>
    public static readonly Property sensingMethod = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sensingMethod"));    

    ///<summary>
    ///An attribute relating to image data characteristics
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageDataCharacter"/>
    ///</summary>
    public static readonly Property imageDataCharacter = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageDataCharacter"));    

    ///<summary>
    ///tagNumber: 37385
    ///The status of flash when the image was shot.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#flash"/>
    ///</summary>
    public static readonly Property flash = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#flash"));    

    ///<summary>
    ///a date information. Usually saved as YYYY:MM:DD (HH:MM:SS) format in Exif data, but represented here as W3C-DTF format
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#date"/>
    ///</summary>
    public static readonly Property date = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#date"));    

    ///<summary>
    ///The exposure index selected on the camera or input device at the time the image is captured.
    ///tagNumber: 41493
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureIndex"/>
    ///</summary>
    public static readonly Property exposureIndex = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureIndex"));    

    ///<summary>
    ///Information specific to compressed data. The channels of each component are arranged in order from the 1st component to the 4th. For uncompressed data the data arrangement is given in the PhotometricInterpretation tag. However, since PhotometricInterpretation can only express the order of Y,Cb and Cr, this tag is provided for cases when compressed data uses components other than Y, Cb, and Cr and to enable support of other sequences.
    ///tagNumber: 37121
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#componentsConfiguration"/>
    ///</summary>
    public static readonly Property componentsConfiguration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#componentsConfiguration"));    

    ///<summary>
    ///An attribute relating to Image Configuration
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageConfig"/>
    ///</summary>
    public static readonly Property imageConfig = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageConfig"));    

    ///<summary>
    ///a mesurement of time length with unit of second
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#seconds"/>
    ///</summary>
    public static readonly Property seconds = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#seconds"));    

    ///<summary>
    ///Manufacturer of image input equipment
    ///tagNumber: 271
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#make"/>
    ///</summary>
    public static readonly Property make = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#make"));    

    ///<summary>
    ///tagNumber: 278
    ///The number of rows per strip. This is the number of rows in the image of one strip when an image is divided into strips. With JPEG compressed data this designation is not needed and is omitted.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#rowsPerStrip"/>
    ///</summary>
    public static readonly Property rowsPerStrip = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#rowsPerStrip"));    

    ///<summary>
    ///tagNumber: 36867
    ///The date and time when the original image data was generated. For a DSC the date and time the picture was taken are recorded.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateTimeOriginal"/>
    ///</summary>
    public static readonly Property dateTimeOriginal = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateTimeOriginal"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subsecond"/>
    ///</summary>
    public static readonly Property subsecond = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subsecond"));    

    ///<summary>
    ///tagNumber: 37378
    ///The lens aperture. The unit is the APEX value.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#apertureValue"/>
    ///</summary>
    public static readonly Property apertureValue = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#apertureValue"));    

    ///<summary>
    ///The Exif tag number (for this schema definition)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#tagNumber"/>
    ///</summary>
    public static readonly Property tagNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#tagNumber"));    

    ///<summary>
    ///tagNumber: 301
    ///A transfer function for the image, described in tabular style. Normally this tag is not necessary, since color space is specified in the color space information tag (ColorSpace).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#transferFunction"/>
    ///</summary>
    public static readonly Property transferFunction = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#transferFunction"));    

    ///<summary>
    ///Information specific to compressed data. When a compressed file is recorded, the valid height of the meaningful image shall be recorded in this tag, whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file. Since data padding is unnecessary in the vertical direction, the number of lines recorded in this valid image height tag will in fact be the same as that recorded in the SOF.
    ///tagNumber: 40963
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pixelYDimension"/>
    ///</summary>
    public static readonly Property pixelYDimension = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pixelYDimension"));    

    ///<summary>
    ///Indicates whether pixel components are recorded in chunky or planar format. In JPEG compressed files a JPEG marker is used instead of this tag. If this field does not exist, the TIFF default of 1 (chunky) is assumed.
    ///tagNumber: 284
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#planarConfiguration"/>
    ///</summary>
    public static readonly Property planarConfiguration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#planarConfiguration"));    

    ///<summary>
    ///The number of pixels in the image width (X) direction per FocalPlaneResolutionUnit on the camera focal plane.
    ///tagNumber: 41486
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalPlaneXResolution"/>
    ///</summary>
    public static readonly Property focalPlaneXResolution = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalPlaneXResolution"));    

    ///<summary>
    ///tagNumber: 513
    ///The offset to the start byte (SOI) of JPEG compressed thumbnail data. This is not used for primary image JPEG data.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#jpegInterchangeFormat"/>
    ///</summary>
    public static readonly Property jpegInterchangeFormat = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#jpegInterchangeFormat"));    

    ///<summary>
    ///Brightness info for print image matching
    ///tagNumber: 10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimBrightness"/>
    ///</summary>
    public static readonly Property pimBrightness = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimBrightness"));    

    ///<summary>
    ///The sampling ratio of chrominance components in relation to the luminance component. In JPEG compressed data a JPEG marker is used instead of this tag.
    ///tagNumber: 530
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yCbCrSubSampling"/>
    ///</summary>
    public static readonly Property yCbCrSubSampling = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yCbCrSubSampling"));    

    ///<summary>
    ///A length with unit of mm
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#mm"/>
    ///</summary>
    public static readonly Property mm = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#mm"));    

    ///<summary>
    ///tagNumber: 41994
    ///The direction of sharpness processing applied by the camera when the image was shot.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sharpness"/>
    ///</summary>
    public static readonly Property sharpness = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sharpness"));    

    ///<summary>
    ///A pointer to the GPS IFD, which is a set of tags for recording GPS information.
    ///tagNumber: 34853
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsInfoIFDPointer"/>
    ///</summary>
    public static readonly Property gpsInfoIFDPointer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsInfoIFDPointer"));    

    ///<summary>
    ///tagNumber: 3
    ///Indicates whether the longitude is east or west longitude. ASCII 'E' indicates east longitude, and 'W' is west longitude.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsLongitudeRef"/>
    ///</summary>
    public static readonly Property gpsLongitudeRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsLongitudeRef"));    

    ///<summary>
    ///tagNumber: 37521
    ///DateTimeOriginal subseconds
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subSecTimeOriginal"/>
    ///</summary>
    public static readonly Property subSecTimeOriginal = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subSecTimeOriginal"));    

    ///<summary>
    ///tagNumber: 296
    ///The unit for measuring XResolution and YResolution. The same unit is used for both XResolution and YResolution. If the image resolution in unknown, 2 (inches) is designated.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#resolutionUnit"/>
    ///</summary>
    public static readonly Property resolutionUnit = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#resolutionUnit"));    

    ///<summary>
    ///The actual focal length of the lens, in mm. Conversion is not made to the focal length of a 35 mm film camera.
    ///tagNumber: 37386
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalLength"/>
    ///</summary>
    public static readonly Property focalLength = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#focalLength"));    

    ///<summary>
    ///Indicates the reference used for giving the bearing to the destination point. 'T' denotes true direction and 'M' is magnetic direction.
    ///tagNumber: 23
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestBearingRef"/>
    ///</summary>
    public static readonly Property gpsDestBearingRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDestBearingRef"));    

    ///<summary>
    ///tagNumber: 33432
    ///Copyright information. In this standard the tag is used to indicate both the photographer and editor copyrights. It is the copyright notice of the person or organization claiming rights to the image. Deprecated in favor of the more generic nie:copyright.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#copyright"/>
    ///</summary>
    public static readonly Property copyright = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#copyright"));    

    ///<summary>
    ///The use of special processing on image data, such as rendering geared to output. When special processing is performed, the reader is expected to disable or minimize any further processing.
    ///tagNumber: 41985
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#customRendered"/>
    ///</summary>
    public static readonly Property customRendered = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#customRendered"));    

    ///<summary>
    ///tagNumber: 315
    ///Person who created the image
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#artist"/>
    ///</summary>
    public static readonly Property artist = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#artist"));    

    ///<summary>
    ///The location where the picture has been made. This property aggregates values of two properties from the original EXIF specification: gpsLatitute (tag number 2) and gpsLongitude (tag number 4), and gpsAltitude (tag number 6).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gps"/>
    ///</summary>
    public static readonly Property gps = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gps"));    

    ///<summary>
    ///tagNumber: 37380
    ///The exposure bias. The unit is the APEX value. Ordinarily it is given in the range of -99.99 to 99.99.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureBiasValue"/>
    ///</summary>
    public static readonly Property exposureBiasValue = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureBiasValue"));    

    ///<summary>
    ///tagNumber: 257
    ///Image height. The number of rows of image data. In JPEG compressed data a JPEG marker is used.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageLength"/>
    ///</summary>
    public static readonly Property imageLength = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageLength"));    

    ///<summary>
    ///tagNumber: 532
    ///The reference black point value and reference white point value. The color space is declared in a color space information tag, with the default being the value that gives the optimal image characteristics Interoperability these conditions.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#referenceBlackWhite"/>
    ///</summary>
    public static readonly Property referenceBlackWhite = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#referenceBlackWhite"));    

    ///<summary>
    ///tagNumber: 41986
    ///the exposure mode set when the image was shot. In auto-bracketing mode, the camera shoots a series of frames of the same scene at different exposure settings.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureMode"/>
    ///</summary>
    public static readonly Property exposureMode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exposureMode"));    

    ///<summary>
    ///tagNumber: 12
    ///The unit used to express the GPS receiver speed of movement. 'K' 'M' and 'N' represents kilometers per hour, miles per hour, and knots.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsSpeedRef"/>
    ///</summary>
    public static readonly Property gpsSpeedRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsSpeedRef"));    

    ///<summary>
    ///tagNumber: 7
    ///The time as UTC (Coordinated Universal Time). TimeStamp is expressed as three RATIONAL values giving the hour, minute, and second.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsTimeStamp"/>
    ///</summary>
    public static readonly Property gpsTimeStamp = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsTimeStamp"));    

    ///<summary>
    ///A character string recording the name of the GPS area. The first byte indicates the character code used, and this is followed by the name of the GPS area.
    ///tagNumber: 28
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsAreaInformation"/>
    ///</summary>
    public static readonly Property gpsAreaInformation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsAreaInformation"));    

    ///<summary>
    ///tagNumber: 29
    ///date and time information relative to UTC (Coordinated Universal Time). The record format is "YYYY:MM:DD" while converted to W3C-DTF to use in RDF
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDateStamp"/>
    ///</summary>
    public static readonly Property gpsDateStamp = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDateStamp"));    

    ///<summary>
    ///An attribute relating to Picture-Taking Conditions
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pictTaking"/>
    ///</summary>
    public static readonly Property pictTaking = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pictTaking"));    

    ///<summary>
    ///tagNumber: 37381
    ///The smallest F number of the lens. The unit is the APEX value. Ordinarily it is given in the range of 00.00 to 99.99, but it is not limited to this range.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#maxApertureValue"/>
    ///</summary>
    public static readonly Property maxApertureValue = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#maxApertureValue"));    

    ///<summary>
    ///tagNumber: 256
    ///Image width. The number of columns of image data, equal to the number of pixels per row. In JPEG compressed data a JPEG marker is used instead of this tag.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageWidth"/>
    ///</summary>
    public static readonly Property imageWidth = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#imageWidth"));    

    ///<summary>
    ///An attribute relating to print image matching
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimInfo"/>
    ///</summary>
    public static readonly Property pimInfo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimInfo"));    

    ///<summary>
    ///A tag that refers a child IFD
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#ifdPointer"/>
    ///</summary>
    public static readonly Property ifdPointer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#ifdPointer"));    

    ///<summary>
    ///Indicates the altitude used as the reference altitude. If the reference is sea level and the altitude is above sea level, 0 is given. If the altitude is below sea level, a value of 1 is given and the altitude is indicated as an absolute value in the GPSAltitude tag. The reference unit is meters.
    ///tagNumber: 5
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsAltitudeRef"/>
    ///</summary>
    public static readonly Property gpsAltitudeRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsAltitudeRef"));    

    ///<summary>
    ///An attribute relating to GPS information
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsInfo"/>
    ///</summary>
    public static readonly Property gpsInfo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsInfo"));    

    ///<summary>
    ///An attribute relating to Version
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#versionInfo"/>
    ///</summary>
    public static readonly Property versionInfo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#versionInfo"));    

    ///<summary>
    ///tagNumber: 37379
    ///The value of brightness. The unit is the APEX value. Ordinarily it is given in the range of -99.99 to 99.99. Note that if the numerator of the recorded value is FFFFFFFF.H, Unknown shall be indicated.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#brightnessValue"/>
    ///</summary>
    public static readonly Property brightnessValue = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#brightnessValue"));    

    ///<summary>
    ///tagNumber: 41988
    ///The digital zoom ratio when the image was shot. If the numerator of the recorded value is 0, this indicates that digital zoom was not used.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#digitalZoomRatio"/>
    ///</summary>
    public static readonly Property digitalZoomRatio = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#digitalZoomRatio"));    

    ///<summary>
    ///A length with unit of meter
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#meter"/>
    ///</summary>
    public static readonly Property meter = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#meter"));    

    ///<summary>
    ///tagNumber: 37384
    ///Light source such as Daylight, Tungsten, Flash etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#lightSource"/>
    ///</summary>
    public static readonly Property lightSource = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#lightSource"));    

    ///<summary>
    ///Length of an object. Could be a subProperty of other general schema.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#length"/>
    ///</summary>
    public static readonly Property length = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#length"));    

    ///<summary>
    ///The GPS measurement mode. '2' means two-dimensional measurement and '3' means three-dimensional measurement is in progress.
    ///tagNumber: 10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsMeasureMode"/>
    ///</summary>
    public static readonly Property gpsMeasureMode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsMeasureMode"));    

    ///<summary>
    ///tagNumber: 33437
    ///F number
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#fNumber"/>
    ///</summary>
    public static readonly Property fNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#fNumber"));    

    ///<summary>
    ///tagNumber: 36864
    ///Exif Version
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifVersion"/>
    ///</summary>
    public static readonly Property exifVersion = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifVersion"));    

    ///<summary>
    ///Height of an object
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#height"/>
    ///</summary>
    public static readonly Property height = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#height"));    

    ///<summary>
    ///tagNumber: 529
    ///The matrix coefficients for transformation from RGB to YCbCr image data.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yCbCrCoefficients"/>
    ///</summary>
    public static readonly Property yCbCrCoefficients = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#yCbCrCoefficients"));    

    ///<summary>
    ///tagNumber: 2
    ///Interoperability Version
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interoperabilityVersion"/>
    ///</summary>
    public static readonly Property interoperabilityVersion = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#interoperabilityVersion"));    

    ///<summary>
    ///The Exif tag number with context prefix, such as IFD type or maker name (for this schema definition)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#tagid"/>
    ///</summary>
    public static readonly Property tagid = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#tagid"));    

    ///<summary>
    ///tagNumber: 37510
    ///A tag for Exif users to write keywords or comments on the image besides those in ImageDescription, and without the character code limitations of the ImageDescription tag. The character code used in the UserComment tag is identified based on an ID code in a fixed 8-byte area at the start of the tag data area.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#userComment"/>
    ///</summary>
    public static readonly Property userComment = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#userComment"));    

    ///<summary>
    ///Related image file format
    ///tagNumber: 4096
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedImageFileFormat"/>
    ///</summary>
    public static readonly Property relatedImageFileFormat = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedImageFileFormat"));    

    ///<summary>
    ///tagNumber: 37377
    ///Shutter speed. The unit is the APEX (Additive System of Photographic Exposure) setting
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#shutterSpeedValue"/>
    ///</summary>
    public static readonly Property shutterSpeedValue = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#shutterSpeedValue"));    

    ///<summary>
    ///The date and time when the image was stored as digital data. If, for example, an image was captured by DSC and at the same time the file was recorded, then the DateTimeOriginal and DateTimeDigitized will have the same contents.
    ///tagNumber: 36868
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateTimeDigitized"/>
    ///</summary>
    public static readonly Property dateTimeDigitized = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#dateTimeDigitized"));    

    ///<summary>
    ///tagNumber: 27
    ///A character string recording the name of the method used for location finding. The first byte indicates the character code used, and this is followed by the name of the method.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsProcessingMethod"/>
    ///</summary>
    public static readonly Property gpsProcessingMethod = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsProcessingMethod"));    

    ///<summary>
    ///tagNumber: 4097
    ///Related image width
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedImageWidth"/>
    ///</summary>
    public static readonly Property relatedImageWidth = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedImageWidth"));    

    ///<summary>
    ///tagNumber: 41730
    ///The color filter array (CFA) geometric pattern of the image sensor when a one-chip color area sensor is used. It does not apply to all sensing methods.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#cfaPattern"/>
    ///</summary>
    public static readonly Property cfaPattern = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#cfaPattern"));    

    ///<summary>
    ///The direction of saturation processing applied by the camera when the image was shot.
    ///tagNumber: 41993
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#saturation"/>
    ///</summary>
    public static readonly Property saturation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#saturation"));    

    ///<summary>
    ///tagNumber: 8
    ///The GPS satellites used for measurements. This tag can be used to describe the number of satellites, their ID number, angle of elevation, azimuth, SNR and other information in ASCII notation. The format is not specified. If the GPS receiver is incapable of taking measurements, value of the tag shall be set to NULL.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsSatellites"/>
    ///</summary>
    public static readonly Property gpsSatellites = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsSatellites"));    

    ///<summary>
    ///The image orientation viewed in terms of rows and columns. As defined in the EXIF specification this is a number between 1 and 8.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#orientation"/>
    ///</summary>
    public static readonly Property orientation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#orientation"));    

    ///<summary>
    ///The version of GPSInfoIFD. The version is given as 2.2.0.0. This tag is mandatory when GPSInfo tag is present.
    ///tagNumber: 0
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsVersionID"/>
    ///</summary>
    public static readonly Property gpsVersionID = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsVersionID"));    

    ///<summary>
    ///A tag used to record fractions of seconds for a date property
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subseconds"/>
    ///</summary>
    public static readonly Property subseconds = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#subseconds"));    

    ///<summary>
    ///The GPS DOP (data degree of precision). An HDOP value is written during two-dimensional measurement, and PDOP during three-dimensional measurement.
    ///tagNumber: 11
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDOP"/>
    ///</summary>
    public static readonly Property gpsDOP = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDOP"));    

    ///<summary>
    ///An Exif IFD data entry
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifdata"/>
    ///</summary>
    public static readonly Property exifdata = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifdata"));    

    ///<summary>
    ///tagNumber: 41990
    ///The type of scene that was shot. It can also be used to record the mode in which the image was shot, such as Landscape, Portrait etc. Note that this differs from the scene type (SceneType) tag.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sceneCaptureType"/>
    ///</summary>
    public static readonly Property sceneCaptureType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#sceneCaptureType"));    

    ///<summary>
    ///tagNumber: 17
    ///The direction of the image when it was captured. The range of values is from 0.00 to 359.99.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsImgDirection"/>
    ///</summary>
    public static readonly Property gpsImgDirection = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsImgDirection"));    

    ///<summary>
    ///tagNumber: 34665
    ///A pointer to the Exif IFD, which is a set of tags for recording Exif-specific attribute information.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifIFDPointer"/>
    ///</summary>
    public static readonly Property exifIFDPointer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#exifIFDPointer"));    

    ///<summary>
    ///The chromaticity of the white point of the image. Normally this tag is not necessary, since color space is specified in the color space information tag (ColorSpace).
    ///tagNumber: 318
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#whitePoint"/>
    ///</summary>
    public static readonly Property whitePoint = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#whitePoint"));    

    ///<summary>
    ///Location of the destination. This property aggregates values of two other properties from the original exif specification. gpsDestLatitude (tag number 20) and gpsDestLongitude (tag number 22)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDest"/>
    ///</summary>
    public static readonly Property gpsDest = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDest"));    

    ///<summary>
    ///Related audio file
    ///tagNumber: 40964
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedSoundFile"/>
    ///</summary>
    public static readonly Property relatedSoundFile = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#relatedSoundFile"));    

    ///<summary>
    ///tagNumber: 258
    ///The number of bits per image component. In this standard each component of the image is 8 bits, so the value for this tag is 8. See also SamplesPerPixel. In JPEG compressed data a JPEG marker is used instead of this tag.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#bitsPerSample"/>
    ///</summary>
    public static readonly Property bitsPerSample = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#bitsPerSample"));    

    ///<summary>
    ///tagNumber: 16
    ///The reference for giving the direction of the image when it is captured. 'T' denotes true direction and 'M' is magnetic direction.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsImgDirectionRef"/>
    ///</summary>
    public static readonly Property gpsImgDirectionRef = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsImgDirectionRef"));    

    ///<summary>
    ///tagNumber: 9
    ///The status of the GPS receiver when the image is recorded. 'A' means measurement is in progress, and 'V' means the measurement is Interoperability.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsStatus"/>
    ///</summary>
    public static readonly Property gpsStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsStatus"));    

    ///<summary>
    ///tagNumber: 30
    ///Indicates whether differential correction is applied to the GPS receiver.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDifferential"/>
    ///</summary>
    public static readonly Property gpsDifferential = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#gpsDifferential"));    

    ///<summary>
    ///tagNumber: 11
    ///ColorBalance info for print image matching
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimColorBalance"/>
    ///</summary>
    public static readonly Property pimColorBalance = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nexif#pimColorBalance"));
}

///<summary>
///
///
///</summary>
public class nfo : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nfo";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///A file attached to another data object. Many data formats allow for attachments: emails, vcards, ical events, id3 and exif...
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Attachment"/>
    ///</summary>
    public static readonly Class Attachment = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Attachment"));    

    ///<summary>
    ///A file data object stored at a remote location. Don't confuse this class with a RemotePortAddress. This one applies to a particular resource, RemotePortAddress applies to an address, that can have various interpretations.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#RemoteDataObject"/>
    ///</summary>
    public static readonly Class RemoteDataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#RemoteDataObject"));    

    ///<summary>
    ///Visual content height in pixels.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#height"/>
    ///</summary>
    public static readonly Property height = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#height"));    

    ///<summary>
    ///Horizontal resolution of an image (if printed). Expressed in DPI.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#horizontalResolution"/>
    ///</summary>
    public static readonly Property horizontalResolution = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#horizontalResolution"));    

    ///<summary>
    ///A common superproperty for all properties specifying the media rate. Examples of subproperties may include frameRate for video and sampleRate for audio. This property is expressed in units per second.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#rate"/>
    ///</summary>
    public static readonly Property rate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#rate"));    

    ///<summary>
    ///A Presentation made by some presentation software (Corel Presentations, OpenOffice Impress, MS Powerpoint etc.)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Presentation"/>
    ///</summary>
    public static readonly Class Presentation = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Presentation"));    

    ///<summary>
    ///A generic document. A common superclass for all documents on the desktop.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Document"/>
    ///</summary>
    public static readonly Class Document = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Document"));    

    ///<summary>
    ///Code in a compilable or interpreted programming language.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#SourceCode"/>
    ///</summary>
    public static readonly Class SourceCode = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#SourceCode"));    

    ///<summary>
    ///The foundry, the organization that created the font.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#foundry"/>
    ///</summary>
    public static readonly Property foundry = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#foundry"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#losslessCompressionType"/>
    ///</summary>
    public static readonly Resource losslessCompressionType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#losslessCompressionType"));    

    ///<summary>
    ///Type of compression. Instances of this class represent the limited set of values allowed for the nfo:compressionType property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#CompressionType"/>
    ///</summary>
    public static readonly Class CompressionType = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#CompressionType"));    

    ///<summary>
    ///Number of channels. This property is to be used directly if no detailed information is necessary. Otherwise use more detailed subproperties.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#channels"/>
    ///</summary>
    public static readonly Property channels = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#channels"));    

    ///<summary>
    ///File containing visual content.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Visual"/>
    ///</summary>
    public static readonly Class Visual = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Visual"));    

    ///<summary>
    ///Visual content width in pixels.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#width"/>
    ///</summary>
    public static readonly Property width = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#width"));    

    ///<summary>
    ///A single node in the list of media files contained within an MediaList instance. This class is intended to provide a type all those links have. In valid NRL untyped resources cannot be linked. There are no properties defined for this class but the application may expect rdf:first and rdf:last links. The former points to the DataObject instance, interpreted as Media the latter points at another MediaFileListEntr. At the end of the list there is a link to rdf:nil.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MediaFileListEntry"/>
    ///</summary>
    public static readonly Class MediaFileListEntry = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MediaFileListEntry"));    

    ///<summary>
    ///Occupied storage space of the filesystem.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#occupiedSpace"/>
    ///</summary>
    public static readonly Property occupiedSpace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#occupiedSpace"));    

    ///<summary>
    ///A name of a function/method defined in the given source code file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#definesFunction"/>
    ///</summary>
    public static readonly Property definesFunction = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#definesFunction"));    

    ///<summary>
    ///The amount of lines in a text document
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#lineCount"/>
    ///</summary>
    public static readonly Property lineCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#lineCount"));    

    ///<summary>
    ///A bookmark of a webbrowser. Use nie:title for the name/label, nie:contentCreated to represent the date when the user added the bookmark, and nie:contentLastModified for modifications. nfo:bookmarks to store the link.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Bookmark"/>
    ///</summary>
    public static readonly Class Bookmark = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Bookmark"));    

    ///<summary>
    ///An address specifying a remote host and port. Such an address can be interpreted in many ways (examples of such interpretations include mailboxes, websites, remote calendars or filesystems), depending on an interpretation, various kinds of data may be extracted from such an address.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#RemotePortAddress"/>
    ///</summary>
    public static readonly Class RemotePortAddress = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#RemotePortAddress"));    

    ///<summary>
    ///The amount of characters in the document.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#characterCount"/>
    ///</summary>
    public static readonly Property characterCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#characterCount"));    

    ///<summary>
    ///A file containing a text document, that is unambiguously divided into pages. Examples might include PDF, DOC, PS, DVI etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#PaginatedTextDocument"/>
    ///</summary>
    public static readonly Class PaginatedTextDocument = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#PaginatedTextDocument"));    

    ///<summary>
    ///The amount of samples in an audio clip.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#sampleCount"/>
    ///</summary>
    public static readonly Property sampleCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#sampleCount"));    

    ///<summary>
    ///Number of front channels.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#frontChannels"/>
    ///</summary>
    public static readonly Property frontChannels = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#frontChannels"));    

    ///<summary>
    ///An image of a filesystem. Instances of this class may include CD images, DVD images or hard disk partition images created by various pieces of software (e.g. Norton Ghost). Deprecated in favor of nfo:Filesystem.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#FilesystemImage"/>
    ///</summary>
    public static readonly Class FilesystemImage = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#FilesystemImage"));    

    ///<summary>
    ///Number of rear channels.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#rearChannels"/>
    ///</summary>
    public static readonly Property rearChannels = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#rearChannels"));    

    ///<summary>
    ///A HTML document, may contain links to other files.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#HtmlDocument"/>
    ///</summary>
    public static readonly Class HtmlDocument = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#HtmlDocument"));    

    ///<summary>
    ///Duration of a media piece.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#duration"/>
    ///</summary>
    public static readonly Property duration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#duration"));    

    ///<summary>
    ///Connects a media container with a single media stream contained within.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hasMediaStream"/>
    ///</summary>
    public static readonly Property hasMediaStream = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hasMediaStream"));    

    ///<summary>
    ///The actual value of the hash.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hashValue"/>
    ///</summary>
    public static readonly Property hashValue = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hashValue"));    

    ///<summary>
    ///A file entity that has been deleted from the original source. Usually such entities are stored within various kinds of 'Trash' or 'Recycle Bin' folders.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#DeletedResource"/>
    ///</summary>
    public static readonly Class DeletedResource = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#DeletedResource"));    

    ///<summary>
    ///A MindMap, created by a mind-mapping utility. Examples might include FreeMind or mind mapper.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MindMap"/>
    ///</summary>
    public static readonly Class MindMap = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MindMap"));    

    ///<summary>
    ///A service published by a piece of software, either by an operating system or an application. Examples of such services may include calendar, addressbook and mailbox managed by a PIM application. This category is introduced to distinguish between data available directly from the applications (Via some Interprocess Communication Mechanisms) and data available from files on a disk. In either case both DataObjects would receive a similar interpretation (e.g. a Mailbox) and wouldn't differ on the content level.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#SoftwareService"/>
    ///</summary>
    public static readonly Class SoftwareService = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#SoftwareService"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#decryptedStatus"/>
    ///</summary>
    public static readonly Resource decryptedStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#decryptedStatus"));    

    ///<summary>
    ///This property is intended to point to an RDF list of MediaFiles.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hasMediaFileListEntry"/>
    ///</summary>
    public static readonly Property hasMediaFileListEntry = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hasMediaFileListEntry"));    

    ///<summary>
    ///Amount of bits used to express the color of each pixel.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#colorDepth"/>
    ///</summary>
    public static readonly Property colorDepth = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#colorDepth"));    

    ///<summary>
    ///The owner of the file as defined by the file system access rights feature.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileOwner"/>
    ///</summary>
    public static readonly Property fileOwner = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileOwner"));    

    ///<summary>
    ///Models the containment relations between Files and Folders (or CompressedFiles).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#belongsToContainer"/>
    ///</summary>
    public static readonly Property belongsToContainer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#belongsToContainer"));    

    ///<summary>
    ///Vertical resolution of an Image (if printed). Expressed in DPI
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#verticalResolution"/>
    ///</summary>
    public static readonly Property verticalResolution = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#verticalResolution"));    

    ///<summary>
    ///Amount of video frames per second.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#frameRate"/>
    ///</summary>
    public static readonly Property frameRate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#frameRate"));    

    ///<summary>
    ///The type of the bitrate. Examples may include CBR and VBR.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bitrateType"/>
    ///</summary>
    public static readonly Property bitrateType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bitrateType"));    

    ///<summary>
    ///A folder/directory. Examples of folders include folders on a filesystem and message folders in a mailbox.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Folder"/>
    ///</summary>
    public static readonly Class Folder = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Folder"));    

    ///<summary>
    ///The name of the codec necessary to decode a piece of media.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#codec"/>
    ///</summary>
    public static readonly Property codec = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#codec"));    

    ///<summary>
    ///The type of the compression. Values include, 'lossy' and 'lossless'.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#compressionType"/>
    ///</summary>
    public static readonly Property compressionType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#compressionType"));    

    ///<summary>
    ///The folder contains a bookmark.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#containsBookmark"/>
    ///</summary>
    public static readonly Property containsBookmark = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#containsBookmark"));    

    ///<summary>
    ///An executable file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Executable"/>
    ///</summary>
    public static readonly Class Executable = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Executable"));    

    ///<summary>
    ///An OperatingSystem
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#OperatingSystem"/>
    ///</summary>
    public static readonly Class OperatingSystem = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#OperatingSystem"));    

    ///<summary>
    ///A raster image.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#RasterImage"/>
    ///</summary>
    public static readonly Class RasterImage = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#RasterImage"));    

    ///<summary>
    ///The amount of audio samples per second.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#sampleRate"/>
    ///</summary>
    public static readonly Property sampleRate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#sampleRate"));    

    ///<summary>
    ///A file containing audio content
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Audio"/>
    ///</summary>
    public static readonly Class Audio = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Audio"));    

    ///<summary>
    ///Name of the file, together with the extension
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileName"/>
    ///</summary>
    public static readonly Property fileName = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileName"));    

    ///<summary>
    ///A stream of multimedia content, usually contained within a media container such as a movie (containing both audio and video) or a DVD (possibly containing many streams of audio and video). Most common interpretations for such a DataObject include Audio and Video.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MediaStream"/>
    ///</summary>
    public static readonly Class MediaStream = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MediaStream"));    

    ///<summary>
    ///A piece of media content. This class may be used to express complex media containers with many streams of various media content (both aural and visual).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Media"/>
    ///</summary>
    public static readonly Class Media = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Media"));    

    ///<summary>
    ///A fingerprint of the file, generated by some hashing function.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#FileHash"/>
    ///</summary>
    public static readonly Class FileHash = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#FileHash"));    

    ///<summary>
    ///The amount of character in comments i.e. characters ignored by the compiler/interpreter.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#commentCharacterCount"/>
    ///</summary>
    public static readonly Property commentCharacterCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#commentCharacterCount"));    

    ///<summary>
    ///A file containing plain text (ASCII, Unicode or other encodings). Examples may include TXT, HTML, XML, program source code etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#PlainTextDocument"/>
    ///</summary>
    public static readonly Class PlainTextDocument = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#PlainTextDocument"));    

    ///<summary>
    ///A text document
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#TextDocument"/>
    ///</summary>
    public static readonly Class TextDocument = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#TextDocument"));    

    ///<summary>
    ///A font.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Font"/>
    ///</summary>
    public static readonly Class Font = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Font"));    

    ///<summary>
    ///Number of side channels
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#sideChannels"/>
    ///</summary>
    public static readonly Property sideChannels = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#sideChannels"));    

    ///<summary>
    ///The amount of frames in a video sequence.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#frameCount"/>
    ///</summary>
    public static readonly Property frameCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#frameCount"));    

    ///<summary>
    ///A common superproperty for all properties signifying the amount of atomic media data units. Examples of subproperties may include sampleCount and frameCount.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#count"/>
    ///</summary>
    public static readonly Property count = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#count"));    

    ///<summary>
    ///Type of filesystem such as ext3 and ntfs.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#filesystemType"/>
    ///</summary>
    public static readonly Property filesystemType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#filesystemType"));    

    ///<summary>
    ///Total storage space of the filesystem, which can be different from nie:contentSize because the latter includes filesystem format overhead.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#totalSpace"/>
    ///</summary>
    public static readonly Property totalSpace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#totalSpace"));    

    ///<summary>
    ///Unoccupied storage space of the filesystem.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#freeSpace"/>
    ///</summary>
    public static readonly Property freeSpace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#freeSpace"));    

    ///<summary>
    ///A compressed file. May contain other files or folder inside. 
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Archive"/>
    ///</summary>
    public static readonly Class Archive = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Archive"));    

    ///<summary>
    ///A DataObject representing a piece of software. Examples of interpretations of a SoftwareItem include an Application and an OperatingSystem.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#SoftwareItem"/>
    ///</summary>
    public static readonly Class SoftwareItem = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#SoftwareItem"));    

    ///<summary>
    ///The amount of words in a text document.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#wordCount"/>
    ///</summary>
    public static readonly Property wordCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#wordCount"));    

    ///<summary>
    ///Page linked by the bookmark.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#pageNumber"/>
    ///</summary>
    public static readonly Property pageNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#pageNumber"));    

    ///<summary>
    ///A file embedded in another data object. There are many ways in which a file may be embedded in another one. Use this class directly only in cases if none of the subclasses gives a better description of your case.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#EmbeddedFileDataObject"/>
    ///</summary>
    public static readonly Class EmbeddedFileDataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#EmbeddedFileDataObject"));    

    ///<summary>
    ///Time when the file was last accessed.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileLastAccessed"/>
    ///</summary>
    public static readonly Property fileLastAccessed = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileLastAccessed"));    

    ///<summary>
    ///States that a piece of software supercedes another piece of software.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#supercedes"/>
    ///</summary>
    public static readonly Property supercedes = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#supercedes"));    

    ///<summary>
    ///A piece of software. Examples may include applications and the operating system. This interpretation most commonly applies to SoftwareItems.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Software"/>
    ///</summary>
    public static readonly Class Software = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Software"));    

    ///<summary>
    ///A file entity inside an archive.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#ArchiveItem"/>
    ///</summary>
    public static readonly Class ArchiveItem = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#ArchiveItem"));    

    ///<summary>
    ///A common superproperty for all properties signifying the amount of bits for an atomic unit of data. Examples of subproperties may include bitsPerSample and bitsPerPixel
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bitDepth"/>
    ///</summary>
    public static readonly Property bitDepth = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bitDepth"));    

    ///<summary>
    ///Number of Low Frequency Expansion (subwoofer) channels.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#lfeChannels"/>
    ///</summary>
    public static readonly Property lfeChannels = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#lfeChannels"));    

    ///<summary>
    ///The date and time of the deletion.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#deletionDate"/>
    ///</summary>
    public static readonly Property deletionDate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#deletionDate"));    

    ///<summary>
    ///A website, usually a container for remote resources, that may be interpreted as HTMLDocuments, images or other types of content.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Website"/>
    ///</summary>
    public static readonly Class Website = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Website"));    

    ///<summary>
    ///A file containing a list of media files.e.g. a playlist
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MediaList"/>
    ///</summary>
    public static readonly Class MediaList = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#MediaList"));    

    ///<summary>
    ///A folder with bookmarks of a webbrowser. Use nfo:containsBookmark to relate Bookmarks. Folders can contain subfolders, use containsBookmarkFolder to relate them.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#BookmarkFolder"/>
    ///</summary>
    public static readonly Class BookmarkFolder = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#BookmarkFolder"));    

    ///<summary>
    ///The average overall bitrate of a media container. (i.e. the size of the piece of media in bits, divided by it's duration expressed in seconds).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#averageBitrate"/>
    ///</summary>
    public static readonly Property averageBitrate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#averageBitrate"));    

    ///<summary>
    ///Visual content aspect ratio. (Width divided by Height)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#aspectRatio"/>
    ///</summary>
    public static readonly Property aspectRatio = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#aspectRatio"));    

    ///<summary>
    ///URL of the file. It points at the location of the file. In cases where creating a simple file:// or http:// URL for a file is difficult (e.g. for files inside compressed archives) the applications are encouraged to use conventions defined by Apache Commons VFS Project at http://jakarta.apache.org/  commons/ vfs/ filesystems.html.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileUrl"/>
    ///</summary>
    public static readonly Property fileUrl = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileUrl"));    

    ///<summary>
    ///The name of the font family.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fontFamily"/>
    ///</summary>
    public static readonly Property fontFamily = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fontFamily"));    

    ///<summary>
    ///Name of a global variable defined within the source code file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#definesGlobalVariable"/>
    ///</summary>
    public static readonly Property definesGlobalVariable = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#definesGlobalVariable"));    

    ///<summary>
    ///The status of the encryption of the InformationElement.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#encryptionStatus"/>
    ///</summary>
    public static readonly Property encryptionStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#encryptionStatus"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#lossyCompressionType"/>
    ///</summary>
    public static readonly Resource lossyCompressionType = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#lossyCompressionType"));    

    ///<summary>
    ///A file containing an image.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Image"/>
    ///</summary>
    public static readonly Class Image = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Image"));    

    ///<summary>
    ///A partition on a hard disk
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#HardDiskPartition"/>
    ///</summary>
    public static readonly Class HardDiskPartition = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#HardDiskPartition"));    

    ///<summary>
    ///A resource containing a finite sequence of bytes with arbitrary information, that is available to a computer program and is usually based on some kind of durable storage. A file is durable in the sense that it remains available for programs to use after the current program has finished.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#FileDataObject"/>
    ///</summary>
    public static readonly Class FileDataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#FileDataObject"));    

    ///<summary>
    ///Name of the algorithm used to compute the hash value. Examples might include CRC32, MD5, SHA, TTH etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hashAlgorithm"/>
    ///</summary>
    public static readonly Property hashAlgorithm = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hashAlgorithm"));    

    ///<summary>
    ///True if the image is interlaced, false if not.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#interlaceMode"/>
    ///</summary>
    public static readonly Property interlaceMode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#interlaceMode"));    

    ///<summary>
    ///A video file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Video"/>
    ///</summary>
    public static readonly Class Video = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Video"));    

    ///<summary>
    ///A filesystem. Examples of filesystems include hard disk partitions, removable media, but also images thereof stored in files such as ISO.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Filesystem"/>
    ///</summary>
    public static readonly Class Filesystem = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Filesystem"));    

    ///<summary>
    ///A superclass for all entities, whose primary purpose is to serve as containers for other data object. They usually don't have any "meaning" by themselves. Examples include folders, archives and optical disc images.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#DataContainer"/>
    ///</summary>
    public static readonly Class DataContainer = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#DataContainer"));    

    ///<summary>
    ///Universally unique identifier of the filesystem. In the future, this property may have its parent changed to a more generic class.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#uuid"/>
    ///</summary>
    public static readonly Property uuid = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#uuid"));    

    ///<summary>
    ///A string containing the permissions of a file. A feature common in many UNIX-like operating systems.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#permissions"/>
    ///</summary>
    public static readonly Property permissions = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#permissions"));    

    ///<summary>
    ///The address of the linked object. Usually a web URI.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bookmarks"/>
    ///</summary>
    public static readonly Property bookmarks = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bookmarks"));    

    ///<summary>
    ///Character position of the bookmark.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#characterPosition"/>
    ///</summary>
    public static readonly Property characterPosition = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#characterPosition"));    

    ///<summary>
    ///Stream position of the bookmark, suitable for e.g. audio books. Expressed in milliseconds
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#streamPosition"/>
    ///</summary>
    public static readonly Property streamPosition = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#streamPosition"));    

    ///<summary>
    ///Indicates the name of the programming language this source code file is written in. Examples might include 'C', 'C++', 'Java' etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#programmingLanguage"/>
    ///</summary>
    public static readonly Property programmingLanguage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#programmingLanguage"));    

    ///<summary>
    ///An application
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Application"/>
    ///</summary>
    public static readonly Class Application = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Application"));    

    ///<summary>
    ///Amount of bits in each audio sample.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bitsPerSample"/>
    ///</summary>
    public static readonly Property bitsPerSample = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#bitsPerSample"));    

    ///<summary>
    ///A spreadsheet, created by a spreadsheet application. Examples might include Gnumeric, OpenOffice Calc or MS Excel.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Spreadsheet"/>
    ///</summary>
    public static readonly Class Spreadsheet = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Spreadsheet"));    

    ///<summary>
    ///States if a given resource is password-protected.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#isPasswordProtected"/>
    ///</summary>
    public static readonly Property isPasswordProtected = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#isPasswordProtected"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#encryptedStatus"/>
    ///</summary>
    public static readonly Resource encryptedStatus = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#encryptedStatus"));    

    ///<summary>
    ///The status of the encryption of an InformationElement. nfo:encryptedStatus means that the InformationElement has been encrypted and couldn't be decrypted by the extraction software, thus no content is available. nfo:decryptedStatus means that decryption was successfull and the content is available.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#EncryptionStatus"/>
    ///</summary>
    public static readonly Class EncryptionStatus = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#EncryptionStatus"));    

    ///<summary>
    ///Uncompressed size of the content of a compressed file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#uncompressedSize"/>
    ///</summary>
    public static readonly Property uncompressedSize = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#uncompressedSize"));    

    ///<summary>
    ///The original location of the deleted resource.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#originalLocation"/>
    ///</summary>
    public static readonly Property originalLocation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#originalLocation"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#VectorImage"/>
    ///</summary>
    public static readonly Class VectorImage = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#VectorImage"));    

    ///<summary>
    ///A Cursor.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Cursor"/>
    ///</summary>
    public static readonly Class Cursor = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Cursor"));    

    ///<summary>
    ///An Icon (regardless of whether it's a raster or a vector icon. A resource representing an icon could have two types (Icon and Raster, or Icon and Vector) if required.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Icon"/>
    ///</summary>
    public static readonly Class Icon = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Icon"));    

    ///<summary>
    ///The folder contains a bookmark folder.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#containsBookmarkFolder"/>
    ///</summary>
    public static readonly Property containsBookmarkFolder = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#containsBookmarkFolder"));    

    ///<summary>
    ///File creation date
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileCreated"/>
    ///</summary>
    public static readonly Property fileCreated = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileCreated"));    

    ///<summary>
    ///The encoding used for the Embedded File. Examples might include BASE64 or UUEncode
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#encoding"/>
    ///</summary>
    public static readonly Property encoding = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#encoding"));    

    ///<summary>
    ///Links the file with it's hash value.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hasHash"/>
    ///</summary>
    public static readonly Property hasHash = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#hasHash"));    

    ///<summary>
    ///last modification date
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileLastModified"/>
    ///</summary>
    public static readonly Property fileLastModified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileLastModified"));    

    ///<summary>
    ///Number of pages.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#pageCount"/>
    ///</summary>
    public static readonly Property pageCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#pageCount"));    

    ///<summary>
    ///Represents a container for deleted files, a feature common in modern operating systems.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Trash"/>
    ///</summary>
    public static readonly Class Trash = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#Trash"));    

    ///<summary>
    ///States that a piece of software is in conflict with another piece of software.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#conflicts"/>
    ///</summary>
    public static readonly Property conflicts = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#conflicts"));    

    ///<summary>
    ///Name of a class defined in the source code file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#definesClass"/>
    ///</summary>
    public static readonly Property definesClass = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#definesClass"));    

    ///<summary>
    ///The size of the file in bytes. For compressed files it means the size of the packed file, not of the contents. For folders it means the aggregated size of all contained files and folders 
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileSize"/>
    ///</summary>
    public static readonly Property fileSize = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#fileSize"));    

    ///<summary>
    ///An information resources of which representations (files, streams) can be retrieved through a web server. They may be generated at retrieval time. Typical examples are pages served by PHP or AJAX or mp3 streams.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#WebDataObject"/>
    ///</summary>
    public static readonly Class WebDataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#WebDataObject"));    

    ///<summary>
    ///A local file data object which is stored on a local file system. Its nie:url always uses the file:/ protocol. The main use of this class is to distinguish local and non-local files.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#LocalFileDataObject"/>
    ///</summary>
    public static readonly Class LocalFileDataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#LocalFileDataObject"));    

    ///<summary>
    ///Relates an image to the information elements it depicts.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#depicts"/>
    ///</summary>
    public static readonly Property depicts = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#depicts"));    

    ///<summary>
    ///The number of colors used/available in a raster image.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#colorCount"/>
    ///</summary>
    public static readonly Property colorCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#colorCount"));    

    ///<summary>
    ///The number of colors defined in palette of the raster image.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#paletteSize"/>
    ///</summary>
    public static readonly Property paletteSize = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#paletteSize"));    

    ///<summary>
    ///Relates an information element to an image which depicts said element.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#depiction"/>
    ///</summary>
    public static readonly Property depiction = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nfo#depiction"));
}

///<summary>
///
///
///</summary>
public class nid3 : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nid3";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialArtistWebpage"/>
    ///</summary>
    public static readonly Property officialArtistWebpage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialArtistWebpage"));    

    ///<summary>
    ///A File annotated with ID3 tags. Implementation notes: use nie:title for the actual name of the piece (TIT2, the 'Title/Songname/Content description' frame); use nie:language for the languages of the text or lyrics spoken or sung in the audio (TLAN, the 'Language(s)' frame); use nie:comment for any kind of full text information that does not fit in any other frame (COMM frame).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#ID3Audio"/>
    ///</summary>
    public static readonly Class ID3Audio = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#ID3Audio"));    

    ///<summary>
    ///TALB
    ///The 'Album/Movie/Show title' frame is intended for the title of the recording(/source of sound) which the audio in the file is taken from.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#albumTitle"/>
    ///</summary>
    public static readonly Property albumTitle = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#albumTitle"));    

    ///<summary>
    ///Links the ID3Audio with an instance of SynchronizedText
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#hasSynchronizedText"/>
    ///</summary>
    public static readonly Property hasSynchronizedText = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#hasSynchronizedText"));    

    ///<summary>
    ///TRSN
    ///The 'Internet radio station name' frame contains the name of the internet radio station from which the audio is streamed.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#internetRadioStationName"/>
    ///</summary>
    public static readonly Property internetRadioStationName = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#internetRadioStationName"));    

    ///<summary>
    ///TENC
    ///The 'Encoded by' frame contains the name of the person or organisation that encoded the audio file. This field may contain a copyright message, if the audio file also is copyrighted by the encoder.
    ///Note that the RDF representation doesn't allow the copyright message in this field. Please move it to the copyrightMessage field.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#encodedBy"/>
    ///</summary>
    public static readonly Property encodedBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#encodedBy"));    

    ///<summary>
    ///WPUB
    ///The 'Publishers official webpage' frame is a URL pointing at the official wepage for the publisher.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#publishersWebpage"/>
    ///</summary>
    public static readonly Property publishersWebpage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#publishersWebpage"));    

    ///<summary>
    ///Since there might be a lot of people contributing to an audio file in various ways, such as musicians and technicians, the 'Text information frames' are often insufficient to list everyone involved in a project. The 'Involved people list' is a frame containing the names of those involved, and how they were involved. The body simply contains a terminated string with the involvement directly followed by a terminated string with the involvee followed by a new involvement and so on. There may only be one "IPLS" frame in each tag.
    ///Note that in this RDF representation each InvolvedPerson is represented with a separate instance of the InvolvedPerson class and with a separate involvedPerson triple.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#InvolvedPerson"/>
    ///</summary>
    public static readonly Class InvolvedPerson = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#InvolvedPerson"));    

    ///<summary>
    ///The 'time stamp' is set to zero or the whole sync is omitted if located directly at the beginning of the sound. All time stamps should be sorted in chronological order. The sync can be considered as a validator of the subsequent string.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textElementTimestamp"/>
    ///</summary>
    public static readonly Property textElementTimestamp = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textElementTimestamp"));    

    ///<summary>
    ///TOAL
    ///The 'Original album/movie/show title' frame is intended for the title of the original recording (or source of sound), if for example the music in the file should be a cover of a previously released song.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalAlbumTitle"/>
    ///</summary>
    public static readonly Property originalAlbumTitle = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalAlbumTitle"));    

    ///<summary>
    ///TRSO
    ///The 'Internet radio station owner' frame contains the name of the owner of the internet radio station from which the audio is streamed.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#internetRadioStationOwner"/>
    ///</summary>
    public static readonly Property internetRadioStationOwner = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#internetRadioStationOwner"));    

    ///<summary>
    ///TSRC
    ///The 'ISRC' frame should contain the International Standard Recording Code (ISRC) (12 characters).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#internationalStandardRecordingCode"/>
    ///</summary>
    public static readonly Property internationalStandardRecordingCode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#internationalStandardRecordingCode"));    

    ///<summary>
    ///TPOS
    ///The 'Part of a set' frame is a numeric string that describes which part of a set the audio came from. This frame is used if the source described in the "TALB" frame is divided into several mediums, e.g. a double CD. The value may be extended with a "/" character and a numeric string containing the total number of parts in the set. E.g. "1/2".
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#partOfSet"/>
    ///</summary>
    public static readonly Property partOfSet = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#partOfSet"));    

    ///<summary>
    ///This frame is intended for music that comes from a CD, so that the CD can be identified in databases such as the CDDB. The frame consists of a binary dump of the Table Of Contents, TOC, from the CD, which is a header of 4 bytes and then 8 bytes/track on the CD plus 8 bytes for the 'lead out' making a maximum of 804 bytes. The offset to the beginning of every track on the CD should be described with a four bytes absolute CD-frame address per track, and not with absolute time. This frame requires a present and valid "TRCK" frame, even if the CD's only got one track. There may only be one "MCDI" frame in each tag.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#musicCDIdentifier"/>
    ///</summary>
    public static readonly Property musicCDIdentifier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#musicCDIdentifier"));    

    ///<summary>
    ///TPE2
    ///The 'Band/Orchestra/Accompaniment' frame is used for additional information about the performers in the recording.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#backgroundArtist"/>
    ///</summary>
    public static readonly Property backgroundArtist = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#backgroundArtist"));    

    ///<summary>
    ///How was this particular person involved in this particular track.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#involvment"/>
    ///</summary>
    public static readonly Property involvment = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#involvment"));    

    ///<summary>
    ///TEXT
    ///The 'Lyricist(s)/Text writer(s)' frame is intended for the writer(s) of the text or lyrics in the recording. They are seperated with the "/" character.
    ///Note that in the RDF representation each text writer is represented with a separate triple.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textWriter"/>
    ///</summary>
    public static readonly Property textWriter = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textWriter"));    

    ///<summary>
    ///Links the synchronized text object with the text elements.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#hasSynchronizedTextElement"/>
    ///</summary>
    public static readonly Property hasSynchronizedTextElement = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#hasSynchronizedTextElement"));    

    ///<summary>
    ///TCON
    ///
    ///The 'Content type', which previously was stored as a one byte numeric value only, is now a numeric string. You may use one or several of the types as ID3v1.1 did or, since the category list would be impossible to maintain with accurate and up to date categories, define your own. 
    ///
    ///References to the ID3v1 genres can be made by, as first byte, enter "(" followed by a number from the genres list (appendix A) and ended with a ")" character. This is optionally followed by a refinement, e.g. "(21)" or "(4)Eurodisco". Several references can be made in the same frame, e.g. "(51)(39)". If the refinement should begin with a "(" character it should be replaced with "((", e.g. "((I can figure out any genre)" or "(55)((I think...)". The following new content types is defined in ID3v2 and is implemented in the same way as the numerig content types, e.g. "(RX)". 
    ///RX    Remix 
    ///CR    Cover
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#contentType"/>
    ///</summary>
    public static readonly Property contentType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#contentType"));    

    ///<summary>
    ///TRCK
    ///The 'Track number/Position in set' frame is a numeric string containing the order number of the audio-file on its original recording. This may be extended with a "/" character and a numeric string containing the total numer of tracks/elements on the original recording. E.g. "4/9".
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#trackNumber"/>
    ///</summary>
    public static readonly Property trackNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#trackNumber"));    

    ///<summary>
    ///Description of a user-defined frame.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#userDefinedFrameDescription"/>
    ///</summary>
    public static readonly Property userDefinedFrameDescription = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#userDefinedFrameDescription"));    

    ///<summary>
    ///TIT3
    ///The 'Subtitle/Description refinement' frame is used for information directly related to the contents title (e.g. "Op. 16" or "Performed live at Wembley").
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#subtitle"/>
    ///</summary>
    public static readonly Property subtitle = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#subtitle"));    

    ///<summary>
    ///TIT2
    ///The 'Title/Songname/Content description' frame is the actual name of the piece (e.g. "Adagio", "Hurricane Donna"). Deprecated in favor of the more generic nie:title.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#title"/>
    ///</summary>
    public static readonly Property title = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#title"));    

    ///<summary>
    ///TOWN
    ///The 'File owner/licensee' frame contains the name of the owner or licensee of the file and it's contents.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#fileOwner"/>
    ///</summary>
    public static readonly Property fileOwner = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#fileOwner"));    

    ///<summary>
    ///WCOP
    ///The 'Copyright/Legal information' frame is a URL pointing at a webpage where the terms of use and ownership of the file is described.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#copyrightInformationURL"/>
    ///</summary>
    public static readonly Property copyrightInformationURL = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#copyrightInformationURL"));    

    ///<summary>
    ///TYER
    ///The 'Year' frame is a numeric string with a year of the recording. This frames is always four characters long (until the year 10000).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#recordingYear"/>
    ///</summary>
    public static readonly Property recordingYear = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#recordingYear"));    

    ///<summary>
    ///SYLT This is another way of incorporating the words, said or sung lyrics, in the audio file as text, this time, however, in sync with the audio. It might also be used to describing events e.g. occurring on a stage or on the screen in sync with the audio. The header includes a content descriptor, represented with as terminated textstring. If no descriptor is entered, 'Content descriptor' is $00 (00) only.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#SynchronizedText"/>
    ///</summary>
    public static readonly Class SynchronizedText = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#SynchronizedText"));    

    ///<summary>
    ///COMM - This frame is indended for any kind of full text information that does not fit in any other frame. It consists of a frame header followed by encoding, language and content descriptors and is ended with the actual comment as a text string. Newline characters are allowed in the comment text string. There may be more than one comment frame in each tag, but only one with the same language and content descriptor. Deprecated in favor of the more generic nie:comment.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#comments"/>
    ///</summary>
    public static readonly Property comments = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#comments"));    

    ///<summary>
    ///TOLY
    ///The 'Original lyricist(s)/text writer(s)' frame is intended for the text writer(s) of the original recording, if for example the music in the file should be a cover of a previously released song. The text writers are seperated with the "/" character.
    ///Note that in the RDF representation each original lyricist is represented with a separate triple.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalTextWriter"/>
    ///</summary>
    public static readonly Property originalTextWriter = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalTextWriter"));    

    ///<summary>
    ///WOAS
    ///The 'Official audio source webpage' frame is a URL pointing at the official webpage for the source of the audio file, e.g. a movie.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialAudioSourceWebpage"/>
    ///</summary>
    public static readonly Property officialAudioSourceWebpage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialAudioSourceWebpage"));    

    ///<summary>
    ///TSSE
    ///The 'Software/Hardware and settings used for encoding' frame includes the used audio encoder and its settings when the file was encoded. Hardware refers to hardware encoders, not the computer on which a program was run.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#encodingSettings"/>
    ///</summary>
    public static readonly Property encodingSettings = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#encodingSettings"));    

    ///<summary>
    ///TKEY
    ///The 'Initial key' frame contains the musical key in which the sound starts. It is represented as a string with a maximum length of three characters. The ground keys are represented with "A","B","C","D","E", "F" and "G" and halfkeys represented with "b" and "#". Minor is represented as "m". Example "Cbm". Off key is represented with an "o" only.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#initialKey"/>
    ///</summary>
    public static readonly Property initialKey = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#initialKey"));    

    ///<summary>
    ///WPAY
    ///The 'Payment' frame is a URL pointing at a webpage that will handle the process of paying for this file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#paymentURL"/>
    ///</summary>
    public static readonly Property paymentURL = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#paymentURL"));    

    ///<summary>
    ///This frame is intended for one-string text information concerning the audiofile in a similar way to the other "T"-frames. The frame body consists of a description of the string, represented as a terminated string, followed by the actual string. There may be more than one "TXXX" frame in each tag, but only one with the same description.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#UserDefinedFrame"/>
    ///</summary>
    public static readonly Class UserDefinedFrame = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#UserDefinedFrame"));    

    ///<summary>
    ///WCOM
    ///The 'Commercial information' frame is a URL pointing at a webpage with information such as where the album can be bought. There may be more than one "WCOM" frame in a tag, but not with the same content.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#commercialInformationURL"/>
    ///</summary>
    public static readonly Property commercialInformationURL = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#commercialInformationURL"));    

    ///<summary>
    ///An arbitrary file embedded in an audio file. Inspired by http://www.id3.org/id3v2.3.0 sec. 
    ///4.16)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#generalEncapsulatedObject"/>
    ///</summary>
    public static readonly Property generalEncapsulatedObject = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#generalEncapsulatedObject"));    

    ///<summary>
    ///TPE1
    ///The 'Lead artist(s)/Lead performer(s)/Soloist(s)/Performing group' is used for the main artist(s). They are seperated with the "/" character.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#leadArtist"/>
    ///</summary>
    public static readonly Property leadArtist = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#leadArtist"));    

    ///<summary>
    ///TDAT
    ///The 'Date' frame is a numeric string in the DDMM format containing the date for the recording. This field is always four characters long.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#date"/>
    ///</summary>
    public static readonly Property date = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#date"));    

    ///<summary>
    ///Unsynchronized text content. Inspired by the content part of the USLT frame defined in the ID3 2.3.0 Spec sec. 4.9
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#unsynchronizedTextContent"/>
    ///</summary>
    public static readonly Property unsynchronizedTextContent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#unsynchronizedTextContent"));    

    ///<summary>
    ///TMED
    ///The 'Media type' frame describes from which media the sound originated. This may be a text string or a reference to the predefined media types found in the list below. References are made within "(" and ")" and are optionally followed by a text refinement, e.g. "(MC) with four channels". If a text refinement should begin with a "(" character it should be replaced with "((" in the same way as in the "TCO" frame. Predefined refinements is appended after the media type, e.g. "(CD/A)" or "(VID/PAL/VHS)".
    ///See http://www.id3.org/id3v2.3.0 for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#mediaType"/>
    ///</summary>
    public static readonly Property mediaType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#mediaType"));    

    ///<summary>
    ///Time stamp format is: 
    ///$01 Absolute time, 32 bit sized, using MPEG frames as unit
    ///$02 Absolute time, 32 bit sized, using milliseconds as unit
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#timestampFormat"/>
    ///</summary>
    public static readonly Property timestampFormat = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#timestampFormat"));    

    ///<summary>
    ///TFLT
    ///The 'File type' frame indicates which type of audio this tag defines. The following type and refinements are defined: 
    ///MPG MPEG Audio; 
    ////1 MPEG 1/2 layer I;
    ////2 MPEG 1/2 layer II;
    ////3 MPEG 1/2 layer III;
    ////2.5 MPEG 2.5;
    ////AAC Advanced audio compression;
    ///VQF Transform-domain Weighted Interleave Vector Quantization;
    ///PCM Pulse Code Modulated audio;
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#fileType"/>
    ///</summary>
    public static readonly Property fileType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#fileType"));    

    ///<summary>
    ///This frame's purpose is to be able to identify the audio file in a database that may contain more information relevant to the content. Since standardisation of such a database is beyond this document, all frames begin with a null-terminated string with a URL containing an email address, or a link to a location where an email address can be found, that belongs to the organisation responsible for this specific database implementation. Questions regarding the database should be sent to the indicated email address. The URL should not be used for the actual database queries. The string "http://www.id3.org/dummy/ufid.html" should be used for tests. Software that isn't told otherwise may safely remove such frames. The 'Owner identifier' must be non-empty (more than just a termination). The 'Owner identifier' is then followed by the actual identifier, which may be up to 64 bytes. There may be more than one "UFID" frame in a tag, but only one with the same 'Owner identifier'.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#uniqueFileIdentifier"/>
    ///</summary>
    public static readonly Property uniqueFileIdentifier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#uniqueFileIdentifier"));    

    ///<summary>
    ///TOPE
    ///The 'Original artist(s)/performer(s)' frame is intended for the performer(s) of the original recording, if for example the music in the file should be a cover of a previously released song. The performers are seperated with the "/" character.
    ///Note that in the RDF repressentation each orignal artist is represented with a separate triple.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalArtist"/>
    ///</summary>
    public static readonly Property originalArtist = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalArtist"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textElementContent"/>
    ///</summary>
    public static readonly Property textElementContent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textElementContent"));    

    ///<summary>
    ///TOFN
    ///The 'Original filename' frame contains the preferred filename for the file, since some media doesn't allow the desired length of the filename. The filename is case sensitive and includes its suffix.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalFilename"/>
    ///</summary>
    public static readonly Property originalFilename = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalFilename"));    

    ///<summary>
    ///TDLY
    ///The 'Playlist delay' defines the numbers of milliseconds of silence between every song in a playlist. The player should use the "ETC" frame, if present, to skip initial silence and silence at the end of the audio to match the 'Playlist delay' time. The time is represented as a numeric string.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#playlistDelay"/>
    ///</summary>
    public static readonly Property playlistDelay = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#playlistDelay"));    

    ///<summary>
    ///TORY
    ///The 'Original release year' frame is intended for the year when the original recording, if for example the music in the file should be a cover of a previously released song, was released. The field is formatted as in the "TYER" frame.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalReleaseYear"/>
    ///</summary>
    public static readonly Property originalReleaseYear = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#originalReleaseYear"));    

    ///<summary>
    ///TLEN
    ///The 'Length' frame contains the length of the audiofile in milliseconds, represented as a numeric string.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#length"/>
    ///</summary>
    public static readonly Property length = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#length"));    

    ///<summary>
    ///TSIZ
    ///The 'Size' frame contains the size of the audiofile in bytes, excluding the ID3v2 tag, represented as a numeric string.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#audiofileSize"/>
    ///</summary>
    public static readonly Property audiofileSize = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#audiofileSize"));    

    ///<summary>
    ///Synchronized text content descriptor. Inspired by the content descriptor part of the SYLT frame defined in ID3 2.3.0 spec sec. 4.10
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#synchronizedTextContentDescriptor"/>
    ///</summary>
    public static readonly Property synchronizedTextContentDescriptor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#synchronizedTextContentDescriptor"));    

    ///<summary>
    ///Content type: 
    ///$00     is other 
    ///$01     is lyrics
    ///$02     is text transcription
    ///$03     is movement/part name (e.g. "Adagio")
    ///$04     is events (e.g. "Don Quijote enters the stage")
    ///$05     is chord (e.g. "Bb F Fsus")
    ///$06     is trivia/'pop up' information
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textContentType"/>
    ///</summary>
    public static readonly Property textContentType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#textContentType"));    

    ///<summary>
    ///WORS
    ///The 'Official internet radio station homepage' contains a URL pointing at the homepage of the internet radio station.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialInternetRadioStationHomepage"/>
    ///</summary>
    public static readonly Property officialInternetRadioStationHomepage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialInternetRadioStationHomepage"));    

    ///<summary>
    ///Links an ID3 file to an InvolvedPerson, an equivalent of the involvedPeopleList tag. Since there might be a lot of people contributing to an audio file in various ways, such as musicians and technicians, the 'Text information frames' are often insufficient to list everyone involved in a project. The 'Involved people list' is a frame containing the names of those involved, and how they were involved. The body simply contains a terminated string with the involvement directly followed by a terminated string with the involvee followed by a new involvement and so on. There may only be one "IPLS" frame in each tag.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#involvedPerson"/>
    ///</summary>
    public static readonly Property involvedPerson = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#involvedPerson"));    

    ///<summary>
    ///The content descriptor of the unsynchronized text. Inspired by the Content Descriptor field of the USLT frame, defined in ID3 2.3.0 Spec sec. 4.9
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#unsynchronizedTextContentDescriptor"/>
    ///</summary>
    public static readonly Property unsynchronizedTextContentDescriptor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#unsynchronizedTextContentDescriptor"));    

    ///<summary>
    ///An element of the synchronized text. It aggregates the actual text content, with the timestamp.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#SynchronizedTextElement"/>
    ///</summary>
    public static readonly Class SynchronizedTextElement = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#SynchronizedTextElement"));    

    ///<summary>
    ///Links the ID3 file to a user-defined frame.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#userDefinedFrame"/>
    ///</summary>
    public static readonly Property userDefinedFrame = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#userDefinedFrame"));    

    ///<summary>
    ///Value of a user-defined frame.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#userDefinedFrameValue"/>
    ///</summary>
    public static readonly Property userDefinedFrameValue = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#userDefinedFrameValue"));    

    ///<summary>
    ///A picture attached to an audio file. The DataObject refered to by this property is usually interpreted as an nfo:Image Inspired by the attached picture tag defined in http://www.id3.org/id3v2.3.0 sec. 4.15)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#attachedPicture"/>
    ///</summary>
    public static readonly Property attachedPicture = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#attachedPicture"));    

    ///<summary>
    ///TIME
    ///The 'Time' frame is a numeric string in the HHMM format containing the time for the recording. This field is always four characters long.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#time"/>
    ///</summary>
    public static readonly Property time = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#time"));    

    ///<summary>
    ///TPUB
    ///The 'Publisher' frame simply contains the name of the label or publisher.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#publisher"/>
    ///</summary>
    public static readonly Property publisher = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#publisher"));    

    ///<summary>
    ///TBPM
    ///The 'BPM' frame contains the number of beats per minute in the mainpart of the audio. The BPM is an integer and represented as a numerical string.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#beatsPerMinute"/>
    ///</summary>
    public static readonly Property beatsPerMinute = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#beatsPerMinute"));    

    ///<summary>
    ///TCOM
    ///The 'Composer(s)' frame is intended for the name of the composer(s). They are seperated with the "/" character.
    ///Note that in the RDF representation each composer is represented with a separate triple.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#composer"/>
    ///</summary>
    public static readonly Property composer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#composer"));    

    ///<summary>
    ///This frame is intended for URL links concerning the audiofile in a similar way to the other "W"-frames. The frame body consists of a description of the string, represented as a terminated string, followed by the actual URL. The URL is always encoded with ISO-8859-1. There may be more than one "WXXX" frame in each tag, but only one with the same description.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#UserDefinedURLFrame"/>
    ///</summary>
    public static readonly Class UserDefinedURLFrame = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#UserDefinedURLFrame"));    

    ///<summary>
    ///TRDA
    ///The 'Recording dates' frame is a intended to be used as complement to the "TYER", "TDAT" and "TIME" frames. E.g. "4th-7th June, 12th June" in combination with the "TYER" frame.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#recordingDate"/>
    ///</summary>
    public static readonly Property recordingDate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#recordingDate"));    

    ///<summary>
    ///TLAN
    ///The 'Language(s)' frame should contain the languages of the text or lyrics spoken or sung in the audio. The language is represented with three characters according to ISO-639-2. If more than one language is used in the text their language codes should follow according to their usage. Deprecated in favor of the more generic nie:language.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#language"/>
    ///</summary>
    public static readonly Property language = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#language"));    

    ///<summary>
    ///TCOP
    ///The 'Copyright message' frame, which must begin with a year and a space character (making five characters), is intended for the copyright holder of the original sound, not the audio file itself. The absence of this frame means only that the copyright information is unavailable or has been removed, and must not be interpreted to mean that the sound is public domain. Every time this field is displayed the field must be preceded with "Copyright".
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#copyrightMessage"/>
    ///</summary>
    public static readonly Property copyrightMessage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#copyrightMessage"));    

    ///<summary>
    ///TIT1
    ///The 'Content group description' frame is used if the sound belongs to a larger category of sounds/music. For example, classical music is often sorted in different musical sections (e.g. "Piano Concerto", "Weather - Hurricane").
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#contentGroupDescription"/>
    ///</summary>
    public static readonly Property contentGroupDescription = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#contentGroupDescription"));    

    ///<summary>
    ///TPE3
    ///The 'Conductor' frame is used for the name of the conductor.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#conductor"/>
    ///</summary>
    public static readonly Property conductor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#conductor"));    

    ///<summary>
    ///TOWN
    ///The 'File owner/licensee' frame contains the name of the owner or licensee of the file and it's contents.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#licensee"/>
    ///</summary>
    public static readonly Property licensee = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#licensee"));    

    ///<summary>
    ///TPE4
    ///The 'Interpreted, remixed, or otherwise modified by' frame contains more information about the people behind a remix and similar interpretations of another existing piece.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#interpretedBy"/>
    ///</summary>
    public static readonly Property interpretedBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#interpretedBy"));    

    ///<summary>
    ///An actual contact to the involved person.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#involvedPersonContact"/>
    ///</summary>
    public static readonly Property involvedPersonContact = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#involvedPersonContact"));    

    ///<summary>
    ///WOAF
    ///The 'Official audio file webpage' frame is a URL pointing at a file specific webpage.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialFileWebpage"/>
    ///</summary>
    public static readonly Property officialFileWebpage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/05/10/nid3#officialFileWebpage"));
}

///<summary>
///
///
///</summary>
public class nie : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nie";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///A unit of content the user works with. This is a superclass for all interpretations of a DataObject.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#InformationElement"/>
    ///</summary>
    public static readonly Class InformationElement = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#InformationElement"));    

    ///<summary>
    ///A textual description of the resource. This property may be used for any metadata fields that provide some meta-information or comment about a resource in the form of a passage of text. This property is not to be confused with nie:plainTextContent. Use more specific subproperties wherever possible.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#description"/>
    ///</summary>
    public static readonly Property description = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#description"));    

    ///<summary>
    ///Date of creation of the DataObject. Note that this date refers to the creation of the DataObject itself (i.e. the physical representation). Compare with nie:contentCreated.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#created"/>
    ///</summary>
    public static readonly Property created = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#created"));    

    ///<summary>
    ///A linking relation. A piece of content links/mentions a piece of data
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#links"/>
    ///</summary>
    public static readonly Property links = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#links"));    

    ///<summary>
    ///URL of a DataObject. It points to the location of the object. A typial usage is FileDataObject. In cases where creating a simple file:// or http:// URL for a file is difficult (e.g. for files inside compressed archives) the applications are encouraged to use conventions defined by Apache Commons VFS Project at http://jakarta.apache.org/  commons/ vfs/ filesystems.html.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#url"/>
    ///</summary>
    public static readonly Property url = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#url"));    

    ///<summary>
    ///The overall size of the data object in bytes. That means the space taken by the DataObject in its container, and not the size of the content that is of interest to the user. For cases where the content size is different (e.g. in compressed files the content is larger, in messages the content excludes headings and is smaller) use more specific properties, not necessarily subproperties of this one.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#byteSize"/>
    ///</summary>
    public static readonly Property byteSize = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#byteSize"));    

    ///<summary>
    ///Plain-text representation of the content of a InformationElement with all markup removed. The main purpose of this property is full-text indexing and search. Its exact content is considered application-specific. The user can make no assumptions about what is and what is not contained within. Applications should use more specific properties wherever possible.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#plainTextContent"/>
    ///</summary>
    public static readonly Property plainTextContent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#plainTextContent"));    

    ///<summary>
    ///DataObjects extracted from a single data source are organized into a containment tree. This property links the root of that tree with the datasource it has been extracted from
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#rootElementOf"/>
    ///</summary>
    public static readonly Property rootElementOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#rootElementOf"));    

    ///<summary>
    ///A point or period of time associated with an event in the lifecycle of an Information Element. A common superproperty for all date-related properties of InformationElements in the NIE Framework.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#informationElementDate"/>
    ///</summary>
    public static readonly Property informationElementDate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#informationElementDate"));    

    ///<summary>
    ///Links the information element with the DataObject it is stored in.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#isStoredAs"/>
    ///</summary>
    public static readonly Property isStoredAs = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#isStoredAs"));    

    ///<summary>
    ///The mime type of the resource, if available. Example: "text/plain". See http://www.iana.org/assignments/media-types/. This property applies to data objects that can be described with one mime type. In cases where the object as a whole has one mime type, while it's parts have other mime types, or there is no mime type that can be applied to the object as a whole, but some parts of the content have mime types - use more specific properties.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#mimeType"/>
    ///</summary>
    public static readonly Property mimeType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#mimeType"));    

    ///<summary>
    ///A disclaimer
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#disclaimer"/>
    ///</summary>
    public static readonly Property disclaimer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#disclaimer"));    

    ///<summary>
    ///Date when information about this data object was retrieved (for the first time) or last refreshed from the data source. This property is important for metadata extraction applications that don't receive any notifications of changes in the data source and have to poll it regularly. This may lead to information becoming out of date. In these cases this property may be used to determine the age of data, which is an important element of it's dependability. 
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#lastRefreshed"/>
    ///</summary>
    public static readonly Property lastRefreshed = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#lastRefreshed"));    

    ///<summary>
    ///The date of the last modification of the original content (not its corresponding DataObject or local copy). Compare with nie:lastModified.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentLastModified"/>
    ///</summary>
    public static readonly Property contentLastModified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentLastModified"));    

    ///<summary>
    ///The size of the content. This property can be used whenever the size of the content of an InformationElement differs from the size of the DataObject. (e.g. because of compression, encoding, encryption or any other representation issues). The contentSize in expressed in bytes.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentSize"/>
    ///</summary>
    public static readonly Property contentSize = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentSize"));    

    ///<summary>
    ///An overall topic of the content of a InformationElement
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#subject"/>
    ///</summary>
    public static readonly Property subject = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#subject"));    

    ///<summary>
    ///Connects the data object with the graph that contains information about it. Deprecated in favor of a more generic nao:isDataGraphFor.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#coreGraph"/>
    ///</summary>
    public static readonly Property coreGraph = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#coreGraph"));    

    ///<summary>
    ///The type of the license. Possible values for this field may include "GPL", "BSD", "Creative Commons" etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#licenseType"/>
    ///</summary>
    public static readonly Property licenseType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#licenseType"));    

    ///<summary>
    ///A unit of data that is created, annotated and processed on the user desktop. It represents a native structure the user works with. The usage of the term 'native' is important. It means that a DataObject can be directly mapped to a data structure maintained by a native application. This may be a file, a set of files or a part of a file. The granularity depends on the user. This class is not intended to be instantiated by itself. Use more specific subclasses.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#DataObject"/>
    ///</summary>
    public static readonly Class DataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#DataObject"));    

    ///<summary>
    ///An unambiguous reference to the InformationElement within a given context. Recommended best practice is to identify the resource by means of a string conforming to a formal identification system.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#identifier"/>
    ///</summary>
    public static readonly Property identifier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#identifier"));    

    ///<summary>
    ///Software used to "generate" the contents. E.g. a word processor name.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#generator"/>
    ///</summary>
    public static readonly Property generator = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#generator"));    

    ///<summary>
    ///A common superproperty for all settings used by the generating software. This may include compression settings, algorithms, autosave, interlaced/non-interlaced etc. Note that this property has no range specified and therefore should not be used directly. Always use more specific properties.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#generatorOption"/>
    ///</summary>
    public static readonly Property generatorOption = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#generatorOption"));    

    ///<summary>
    ///Generic property used to express 'logical' containment relationships between InformationElements. NIE extensions are encouraged to provide more specific subproperties of this one. It is advisable for actual instances of InformationElement to use those specific subproperties. Note the difference between 'physical' containment (hasPart) and logical containment (hasLogicalPart)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#hasLogicalPart"/>
    ///</summary>
    public static readonly Property hasLogicalPart = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#hasLogicalPart"));    

    ///<summary>
    ///A user comment about an InformationElement.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#comment"/>
    ///</summary>
    public static readonly Property comment = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#comment"));    

    ///<summary>
    ///A common superproperty for all properties that point at legal information about an Information Element
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#legal"/>
    ///</summary>
    public static readonly Property legal = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#legal"));    

    ///<summary>
    ///Links the DataObject with the InformationElement it is interpreted as.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#interpretedAs"/>
    ///</summary>
    public static readonly Property interpretedAs = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#interpretedAs"));    

    ///<summary>
    ///A common superproperty for all relations between a piece of content and other pieces of data (which may be interpreted as other pieces of content).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#relatedTo"/>
    ///</summary>
    public static readonly Property relatedTo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#relatedTo"));    

    ///<summary>
    ///Adapted DublinCore: The topic of the content of the resource, as keyword. No sentences here. Recommended best practice is to select a value from a controlled vocabulary or formal classification scheme. 
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#keyword"/>
    ///</summary>
    public static readonly Property keyword = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#keyword"));    

    ///<summary>
    ///Generic property used to express 'physical' containment relationships between DataObjects. NIE extensions are encouraged to provide more specific subproperties of this one. It is advisable for actual instances of DataObjects to use those specific subproperties. Note to the developers: Please be aware of the distinction between containment relation and provenance. The hasPart relation models physical containment, an InformationElement (a nmo:Message) can have a 'physical' part (an nfo:Attachment).  Also, please note the difference between physical containment (hasPart) and logical containment (hasLogicalPart) the former has more strict meaning. They may occur independently of each other.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#hasPart"/>
    ///</summary>
    public static readonly Property hasPart = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#hasPart"));    

    ///<summary>
    ///Name given to an InformationElement
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#title"/>
    ///</summary>
    public static readonly Property title = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#title"));    

    ///<summary>
    ///Last modification date of the DataObject. Note that this date refers to the modification of the DataObject itself (i.e. the physical representation). Compare with nie:contentLastModified.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#lastModified"/>
    ///</summary>
    public static readonly Property lastModified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#lastModified"));    

    ///<summary>
    ///Content copyright
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#copyright"/>
    ///</summary>
    public static readonly Property copyright = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#copyright"));    

    ///<summary>
    ///Generic property used to express containment relationships between DataObjects. NIE extensions are encouraged to provide more specific subproperties of this one. It is advisable for actual instances of DataObjects to use those specific subproperties. Note to the developers: Please be aware of the distinction between containment relation and provenance. The isPartOf relation models physical containment, a nie:DataObject (e.g. an nfo:Attachment) is a 'physical' part of an nie:InformationElement (a nmo:Message). Also, please note the difference between physical containment (isPartOf) and logical containment (isLogicalPartOf) the former has more strict meaning. They may occur independently of each other.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#isPartOf"/>
    ///</summary>
    public static readonly Property isPartOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#isPartOf"));    

    ///<summary>
    ///The date of the content creation. This may not necessarily be equal to the date when the DataObject (i.e. the physical representation) itself was created. Compare with nie:created property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentCreated"/>
    ///</summary>
    public static readonly Property contentCreated = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentCreated"));    

    ///<summary>
    ///Language the InformationElement is expressed in. This property applies to the data object in its entirety. If the data object is divisible into parts expressed in multiple languages - more specific properties should be used. Users are encouraged to use the two-letter code specified in the RFC 3066
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#language"/>
    ///</summary>
    public static readonly Property language = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#language"));    

    ///<summary>
    ///Characterset in which the content of the InformationElement was created. Example: ISO-8859-1, UTF-8. One of the registered character sets at http://www.iana.org/assignments/character-sets. This characterSet is used to interpret any textual parts of the content. If more than one characterSet is used within one data object, use more specific properties.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#characterSet"/>
    ///</summary>
    public static readonly Property characterSet = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#characterSet"));    

    ///<summary>
    ///A superclass for all entities from which DataObjects can be extracted. Each entity represents a native application or some other system that manages information that may be of interest to the user of the Semantic Desktop. Subclasses may include FileSystems, Mailboxes, Calendars, websites etc. The exact choice of subclasses and their properties is considered application-specific. Each data extraction application is supposed to provide it's own DataSource ontology. Such an ontology should contain supported data source types coupled with properties necessary for the application to gain access to the data sources.  (paths, urls, passwords  etc...)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#DataSource"/>
    ///</summary>
    public static readonly Class DataSource = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#DataSource"));    

    ///<summary>
    ///The current version of the given data object. Exact semantics is unspecified at this level. Use more specific subproperties if needed.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#version"/>
    ///</summary>
    public static readonly Property version = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#version"));    

    ///<summary>
    ///Marks the provenance of a DataObject, what source does a data object come from.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#dataSource"/>
    ///</summary>
    public static readonly Property dataSource = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#dataSource"));    

    ///<summary>
    ///Dependency relation. A piece of content depends on another piece of data in order to be properly understood/used/interpreted.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#depends"/>
    ///</summary>
    public static readonly Property depends = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#depends"));    

    ///<summary>
    ///Generic property used to express 'logical' containment relationships between DataObjects. NIE extensions are encouraged to provide more specific subproperties of this one. It is advisable for actual instances of InformationElement to use those specific subproperties. Note the difference between 'physical' containment (isPartOf) and logical containment (isLogicalPartOf)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#isLogicalPartOf"/>
    ///</summary>
    public static readonly Property isLogicalPartOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#isLogicalPartOf"));    

    ///<summary>
    ///Terms and intellectual property rights licensing conditions.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#license"/>
    ///</summary>
    public static readonly Property license = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#license"));    

    ///<summary>
    ///The HTML content of an information element. This property can be used to store text including formatting in a generic fashion.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#htmlContent"/>
    ///</summary>
    public static readonly Property htmlContent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#htmlContent"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#hasIcon"/>
    ///</summary>
    public static readonly Property hasIcon = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#hasIcon"));    

    ///<summary>
    ///Date the DataObject was changed in any way.  Note that this date refers to the modification of the DataObject itself (i.e. the physical representation). Compare with nie:contentModified.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#modified"/>
    ///</summary>
    public static readonly Property modified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#modified"));    

    ///<summary>
    ///The date of a modification of the original content (not its corresponding DataObject or local copy). Compare with nie:modified.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentModified"/>
    ///</summary>
    public static readonly Property contentModified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/01/19/nie#contentModified"));
}

///<summary>
///
///
///</summary>
public class nmm : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nmm";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///Relates a TV Series to its seasons
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#hasSeason"/>
    ///</summary>
    public static readonly Property hasSeason = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#hasSeason"));    

    ///<summary>
    ///Relates a TV Season to its series
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#seasonOf"/>
    ///</summary>
    public static readonly Property seasonOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#seasonOf"));    

    ///<summary>
    ///Relates a TV Show season to its episodes
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#hasSeasonEpisode"/>
    ///</summary>
    public static readonly Property hasSeasonEpisode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#hasSeasonEpisode"));    

    ///<summary>
    ///Used to assign music-specific properties such a BPM to video and audio
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#MusicPiece"/>
    ///</summary>
    public static readonly Class MusicPiece = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#MusicPiece"));    

    ///<summary>
    ///Album the music belongs to
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicAlbum"/>
    ///</summary>
    public static readonly Property musicAlbum = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicAlbum"));    

    ///<summary>
    ///beats per minute
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#beatsPerMinute"/>
    ///</summary>
    public static readonly Property beatsPerMinute = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#beatsPerMinute"));    

    ///<summary>
    ///Composer
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#composer"/>
    ///</summary>
    public static readonly Property composer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#composer"));    

    ///<summary>
    ///Lyricist
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#lyricist"/>
    ///</summary>
    public static readonly Property lyricist = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#lyricist"));    

    ///<summary>
    ///ISRC ID. Format: 'CC-XXX-YY-NNNNN'
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#internationalStandardRecordingCode"/>
    ///</summary>
    public static readonly Property internationalStandardRecordingCode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#internationalStandardRecordingCode"));    

    ///<summary>
    ///MusicBrainz album ID
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicBrainzAlbumID"/>
    ///</summary>
    public static readonly Property musicBrainzAlbumID = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicBrainzAlbumID"));    

    ///<summary>
    ///ReplayGain album(audiophile) gain
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#albumGain"/>
    ///</summary>
    public static readonly Property albumGain = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#albumGain"));    

    ///<summary>
    ///Genre
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#genre"/>
    ///</summary>
    public static readonly Property genre = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#genre"));    

    ///<summary>
    ///A TV Series has multiple seasons and episodes
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#TVSeries"/>
    ///</summary>
    public static readonly Class TVSeries = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#TVSeries"));    

    ///<summary>
    ///Writer
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#writer"/>
    ///</summary>
    public static readonly Property writer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#writer"));    

    ///<summary>
    ///Director
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#director"/>
    ///</summary>
    public static readonly Property director = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#director"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#cinematographer"/>
    ///</summary>
    public static readonly Property cinematographer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#cinematographer"));    

    ///<summary>
    ///The part of a set the audio came from.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#setNumber"/>
    ///</summary>
    public static readonly Property setNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#setNumber"));    

    ///<summary>
    ///The music album as provided by the publisher. Not to be confused with media lists or collections.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#MusicAlbum"/>
    ///</summary>
    public static readonly Class MusicAlbum = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#MusicAlbum"));    

    ///<summary>
    ///Track number of the music in its album
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#trackNumber"/>
    ///</summary>
    public static readonly Property trackNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#trackNumber"));    

    ///<summary>
    ///MusicBrainz track ID
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicBrainzTrackID"/>
    ///</summary>
    public static readonly Property musicBrainzTrackID = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicBrainzTrackID"));    

    ///<summary>
    ///ReplayGain track gain
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#trackGain"/>
    ///</summary>
    public static readonly Property trackGain = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#trackGain"));    

    ///<summary>
    ///ReplayGain album(audiophile) peak gain
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#albumPeakGain"/>
    ///</summary>
    public static readonly Property albumPeakGain = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#albumPeakGain"));    

    ///<summary>
    ///Associated Artwork
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#artwork"/>
    ///</summary>
    public static readonly Property artwork = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#artwork"));    

    ///<summary>
    ///A Movie
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#Movie"/>
    ///</summary>
    public static readonly Class Movie = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#Movie"));    

    ///<summary>
    ///series
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#series"/>
    ///</summary>
    public static readonly Property series = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#series"));    

    ///<summary>
    ///A TVSeries has many episodes
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#hasEpisode"/>
    ///</summary>
    public static readonly Property hasEpisode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#hasEpisode"));    

    ///<summary>
    ///The season number. This property is deprecated. Use nmm:Season and nmm:hasSeason instead.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#season"/>
    ///</summary>
    public static readonly Property season = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#season"));    

    ///<summary>
    ///Long form description of video content (plot, premise, etc.)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#synopsis"/>
    ///</summary>
    public static readonly Property synopsis = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#synopsis"));    

    ///<summary>
    ///Producer
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#producer"/>
    ///</summary>
    public static readonly Property producer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#producer"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#assistantDirector"/>
    ///</summary>
    public static readonly Property assistantDirector = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#assistantDirector"));    

    ///<summary>
    ///Performer
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#performer"/>
    ///</summary>
    public static readonly Property performer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#performer"));    

    ///<summary>
    ///ReplayGain track peak gain
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#trackPeakGain"/>
    ///</summary>
    public static readonly Property trackPeakGain = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#trackPeakGain"));    

    ///<summary>
    ///Music CD identifier to for databases like FreeDB.org. This property is intended for music that comes from a CD, so that the CD can be identified in external databases.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicCDIdentifier"/>
    ///</summary>
    public static readonly Property musicCDIdentifier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#musicCDIdentifier"));    

    ///<summary>
    ///A TV Show
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#TVShow"/>
    ///</summary>
    public static readonly Class TVShow = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#TVShow"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#episodeNumber"/>
    ///</summary>
    public static readonly Property episodeNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#episodeNumber"));    

    ///<summary>
    ///Rating used to identify appropriate audience for video (MPAA rating, BBFC, FSK, TV content rating, etc.)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#audienceRating"/>
    ///</summary>
    public static readonly Property audienceRating = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#audienceRating"));    

    ///<summary>
    ///Actor
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#actor"/>
    ///</summary>
    public static readonly Property actor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#actor"));    

    ///<summary>
    ///The date the media was released.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#releaseDate"/>
    ///</summary>
    public static readonly Property releaseDate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#releaseDate"));    

    ///<summary>
    ///The number of a season
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#seasonNumber"/>
    ///</summary>
    public static readonly Property seasonNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#seasonNumber"));    

    ///<summary>
    ///Relates a TV Show to its season
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#isPartOfSeason"/>
    ///</summary>
    public static readonly Property isPartOfSeason = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#isPartOfSeason"));    

    ///<summary>
    ///The number of tracks in a music album.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#albumTrackCount"/>
    ///</summary>
    public static readonly Property albumTrackCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#albumTrackCount"));    

    ///<summary>
    ///The number of parts in the set.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#setSize"/>
    ///</summary>
    public static readonly Property setSize = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#setSize"));    

    ///<summary>
    ///A season of a TV Show
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#TVSeason"/>
    ///</summary>
    public static readonly Class TVSeason = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2009/02/19/nmm#TVSeason"));
}

///<summary>
///
///
///</summary>
public class nso : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2009/11/08/nso#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nso";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///The subject resource is shared with the object contact. 
    ///The resource and its subresources are transferred to the receiver.
    ///An existing sharedWithContact relation implies that updates on the resource should be transferred to the contact. 
    ///The contact may ask for updates actively, then the sharing party's software should send a new copy of the shared resource to the contact.
    ///Domain should be either a nie:InformationElement or a pimo:Thing but no DataObject. This includes ncal:Event instances and other resources we find on a desktop.
    ///DataObjects are the specific binary stream where an Information Element is stored, and can't be shared because the recipient will form a new binary stream to store the data object. 
    ///As there is no superclass of both nie:InformationElement and pimo:Thing, the domain is rdfs:Resource. 
    ///One resource can be shared to multiple contacts, the cardinality is 0..n. 
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/11/08/nso#sharedWithContact"/>
    ///</summary>
    public static readonly Property sharedWithContact = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/11/08/nso#sharedWithContact"));    

    ///<summary>
    ///The subject resource is shared with all contacts belonging to the object contact group. 
    ///The resource and its subresources are transferred to the receivers.
    ///An existing sharedWithGroup relation implies that updates on the resource should be transferred to the members belonging to the group. 
    ///The contact may ask for updates actively, then the sharing party's software should send a new copy of the shared resource to the contact.
    ///Domain should be either a nie:InformationElement or a pimo:Thing but no DataObject. This includes ncal:Event instances and other resources we find on a desktop.
    ///DataObjects are the specific binary stream where an Information Element is stored, and can't be shared because the recipient will form a new binary stream to store the data object. 
    ///As there is no superclass of both nie:InformationElement and pimo:Thing, the domain is rdfs:Resource. 
    ///One resource can be shared to multiple contact groups, the cardinality is 0..n. 
    ///<see cref="http://www.semanticdesktop.org/ontologies/2009/11/08/nso#sharedWithGroup"/>
    ///</summary>
    public static readonly Property sharedWithGroup = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2009/11/08/nso#sharedWithGroup"));
}

///<summary>
///
///
///</summary>
public class nuao : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nuao";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///An event: activity that have a specific start datetime and possibly a duration
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#Event"/>
    ///</summary>
    public static readonly Class Event = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#Event"));    

    ///<summary>
    ///The time the event finished
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#end"/>
    ///</summary>
    public static readonly Property end = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#end"));    

    ///<summary>
    ///The total number of events
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#eventCount"/>
    ///</summary>
    public static readonly Property eventCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#eventCount"));    

    ///<summary>
    ///The start time of the first usage
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#firstUsage"/>
    ///</summary>
    public static readonly Property firstUsage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#firstUsage"));    

    ///<summary>
    ///The start time of the first modification
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#firstModification"/>
    ///</summary>
    public static readonly Property firstModification = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#firstModification"));    

    ///<summary>
    ///An event which refers to the timespan in which a resource was in the focus of the user.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#FocusEvent"/>
    ///</summary>
    public static readonly Class FocusEvent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#FocusEvent"));    

    ///<summary>
    ///A desktop event: activity performed by an user. A "hook" class that should be extended by other ontologies such as desktop service ontology to specify which application or service was involved in the desktop event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#DesktopEvent"/>
    ///</summary>
    public static readonly Class DesktopEvent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#DesktopEvent"));    

    ///<summary>
    ///Duration of the event. Deprecated in favor of nuao:end.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#duration"/>
    ///</summary>
    public static readonly Property duration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#duration"));    

    ///<summary>
    ///The start time of the first event
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#firstEvent"/>
    ///</summary>
    public static readonly Property firstEvent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#firstEvent"));    

    ///<summary>
    ///The total duration of all usages
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalUsageDuration"/>
    ///</summary>
    public static readonly Property totalUsageDuration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalUsageDuration"));    

    ///<summary>
    ///An event that lead to changes of the resource that are meaningful to the user
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#ModificationEvent"/>
    ///</summary>
    public static readonly Class ModificationEvent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#ModificationEvent"));    

    ///<summary>
    ///The start time of the last modification
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#lastModification"/>
    ///</summary>
    public static readonly Property lastModification = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#lastModification"));    

    ///<summary>
    ///The time of the start of the event
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#start"/>
    ///</summary>
    public static readonly Property start = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#start"));    

    ///<summary>
    ///Relates an event to the resources that are involved in the event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#involves"/>
    ///</summary>
    public static readonly Property involves = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#involves"));    

    ///<summary>
    ///The start time of the last event
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#lastEvent"/>
    ///</summary>
    public static readonly Property lastEvent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#lastEvent"));    

    ///<summary>
    ///The total duration of all events
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalEventDuration"/>
    ///</summary>
    public static readonly Property totalEventDuration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalEventDuration"));    

    ///<summary>
    ///An event of primary or intended use of the resource by the user. Primary or intended use is defined as the use by the consumer of the resource, such as watching a movie or listening to a song. Not to be confused with a low-level read on a file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#UsageEvent"/>
    ///</summary>
    public static readonly Class UsageEvent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#UsageEvent"));    

    ///<summary>
    ///The start time of the last usage
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#lastUsage"/>
    ///</summary>
    public static readonly Property lastUsage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#lastUsage"));    

    ///<summary>
    ///The total number of usages
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#usageCount"/>
    ///</summary>
    public static readonly Property usageCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#usageCount"));    

    ///<summary>
    ///The total number of modifications
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#modificationCount"/>
    ///</summary>
    public static readonly Property modificationCount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#modificationCount"));    

    ///<summary>
    ///The total duration of all modifications
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalModificationDuration"/>
    ///</summary>
    public static readonly Property totalModificationDuration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalModificationDuration"));    

    ///<summary>
    ///The total amount of time a resource was in focus during a UsageEvent. This property should be used to 'compress' several FocusEvent instances into the essential information.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalFocusDuration"/>
    ///</summary>
    public static readonly Property totalFocusDuration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#totalFocusDuration"));    

    ///<summary>
    ///Relates an event to the agent initiating the event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#initiatingAgent"/>
    ///</summary>
    public static readonly Property initiatingAgent = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#initiatingAgent"));    

    ///<summary>
    ///Relates an event to the resource that was targetted in the event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#targettedResource"/>
    ///</summary>
    public static readonly Property targettedResource = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2010/01/25/nuao#targettedResource"));
}

///<summary>
///
///
///</summary>
public class pimo : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "pimo";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///Administrative subdivisions of a Nation that are broader than any other political subdivisions that may exist. This Class includes the states of the United States, as well as the provinces of Canada and European countries. (Definition from SUMO).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#State"/>
    ///</summary>
    public static readonly Class State = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#State"));    

    ///<summary>
    ///The object of statements is a literal, resource, or datatype value describing the subject thing. Users should be able to edit statements defined with this property. Abstract super-property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#datatypeProperty"/>
    ///</summary>
    public static readonly Property datatypeProperty = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#datatypeProperty"));    

    ///<summary>
    ///The territory occupied by a nation; "he returned to the land of his birth"; "he visited several European countries". (Definition from SUMO)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Country"/>
    ///</summary>
    public static readonly Class Country = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Country"));    

    ///<summary>
    ///Entities that are in the direct attention of the user when doing knowledge work.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Thing"/>
    ///</summary>
    public static readonly Class Thing = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Thing"));    

    ///<summary>
    ///The unique label of the tag. The label must be unique within the scope of one PersonalInformationModel. It is required and a subproperty of nao:prefLabel. It clarifies the use of nao:personalIdentifier by restricting the scope to tags. Semantically equivalent to skos:prefLabel, where uniqueness of labels is also recommended.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#tagLabel"/>
    ///</summary>
    public static readonly Property tagLabel = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#tagLabel"));    

    ///<summary>
    ///A collection of Things, independent of their class. The items in the collection share a common property. Which property may be modelled explicitly or mentioned in the description of the Collection. The requirement of explicit modelling the semantic meaning of the collection is not mandatory, as collections can be created ad-hoc. Implizit modelling can be applied by the system by learning the properties. For example, a Collection of "Coworkers" could be defined as that all elements must be of class "Person" and have an attribute "work for the same Organization as the user". Further standards can be used to model these attributes.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Collection"/>
    ///</summary>
    public static readonly Class Collection = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Collection"));    

    ///<summary>
    ///Short: hasOtherRepresentation points from a Class in your PIMO to a class in a domain ontology that represents the same class. Longer: hasOtherConceptualization means that a class of real world objects O represented by a concept C1 in the ontology has additional conceptualizations (as classes C2-Cn in different domain ontologies).
    ///This means: IF (O_i is conceptialized by C_j in Ontology_k) AND (O_l is conceptialized by C_m in Ontology_n) THEN (O_i and O_l is the same set of objects).
    ///hasOtherConceptualization is an transitive relation, but not equivalent (not symmetric nor reflexive).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOtherConceptualization"/>
    ///</summary>
    public static readonly Property hasOtherConceptualization = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOtherConceptualization"));    

    ///<summary>
    ///The global namespace of this user using the semdesk uri scheme, based on the Global Identifier of the user. Example semdesk://bob@example.com/things/. See http://dev.nepomuk.semanticdesktop.org/repos/trunk/doc/2008_09_semdeskurischeme/index.html
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasGlobalNamespace"/>
    ///</summary>
    public static readonly Property hasGlobalNamespace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasGlobalNamespace"));    

    ///<summary>
    ///The object topic is more general in meaning than the subject topic. Transitive. Similar to skos:broader.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#superTopic"/>
    ///</summary>
    public static readonly Property superTopic = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#superTopic"));    

    ///<summary>
    ///The object topic is more specific in meaning than the subject topic. Transitive. Similar in meaning to skos:narrower
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#subTopic"/>
    ///</summary>
    public static readonly Property subTopic = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#subTopic"));    

    ///<summary>
    ///A wiki-like free-text description of a Thing or a Class. The text can be formatted using a limited set of HTML elements and can contain links to other Things. The format is described in detail in the WIF specification (http://semanticweb.org/wiki/Wiki_Interchange_Format).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#wikiText"/>
    ///</summary>
    public static readonly Property wikiText = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#wikiText"));    

    ///<summary>
    ///A note. The textual contents of the note should be expressed in the nao:description value of the note.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Note"/>
    ///</summary>
    public static readonly Class Note = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Note"));    

    ///<summary>
    ///A generic document. This is a placeholder class for document-management domain ontologies to subclass. Create more and specified subclasses of pimo:Document for the document types in your domain. Documents are typically instances of both NFO:Document (modeling the information element used to store the document) and a LogicalMediaType subclass. Two examples are given for what to model here: a contract for a business domain, a BlogPost for an informal domain.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Document"/>
    ///</summary>
    public static readonly Class Document = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Document"));    

    ///<summary>
    ///An association between two or more pimo-things. This is used to model n-ary relations and metadata about relations. For example, the asociation of a person being organizational member is only effectual within a period of time (after the person joined the organization and before the person left the organization). There can be multiple periods of time when associations are valid.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Association"/>
    ///</summary>
    public static readonly Class Association = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Association"));    

    ///<summary>
    ///the person taking the role
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#roleHolder"/>
    ///</summary>
    public static readonly Property roleHolder = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#roleHolder"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#RuleViewSpecificationGroundingClosure"/>
    ///</summary>
    public static readonly Resource RuleViewSpecificationGroundingClosure = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#RuleViewSpecificationGroundingClosure"));    

    ///<summary>
    ///The local namespace of this user using the semdesk uri scheme, based on the Local Identifier of the user. Example semdesk://bob@/things/. See http://dev.nepomuk.semanticdesktop.org/repos/trunk/doc/2008_09_semdeskurischeme/index.html
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasLocalNamespace"/>
    ///</summary>
    public static readonly Property hasLocalNamespace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasLocalNamespace"));    

    ///<summary>
    ///Concepts that relate to a series of actions or operations conducing to an end. Abstract class. Defines optional start and endtime properties, names taken from NCAL.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ProcessConcept"/>
    ///</summary>
    public static readonly Class ProcessConcept = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ProcessConcept"));    

    ///<summary>
    ///The subject organization has the object person or organization (Agent) as a member.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOrganizationMember"/>
    ///</summary>
    public static readonly Property hasOrganizationMember = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOrganizationMember"));    

    ///<summary>
    ///A social event is attended by a person.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#attendee"/>
    ///</summary>
    public static readonly Property attendee = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#attendee"));    

    ///<summary>
    ///A person attends a social event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#attends"/>
    ///</summary>
    public static readonly Property attends = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#attends"));    

    ///<summary>
    ///The subject location is the current location of the object.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isLocationOf"/>
    ///</summary>
    public static readonly Property isLocationOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isLocationOf"));    

    ///<summary>
    ///The subject thing is currently located at the object location.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasLocation"/>
    ///</summary>
    public static readonly Property hasLocation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasLocation"));    

    ///<summary>
    ///This NIE Information Element was used as a grounding occurrence for the object Thing. The Thing was then deleted by the user manually, indicating that this Information Element should not cause an automatic creation of another Thing in the future. The object resource has no range to indicate that it was completely removed from the user's PIMO, including the rdf:type statement. Relevant for data alignment and enrichment algorithms.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#groundingForDeletedThing"/>
    ///</summary>
    public static readonly Property groundingForDeletedThing = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#groundingForDeletedThing"));    

    ///<summary>
    ///relation to the organization in an OrganizationMember association.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#organization"/>
    ///</summary>
    public static readonly Property organization = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#organization"));    

    ///<summary>
    ///The context where the role-holder impersonates this role. For example, the company where a person is employed.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#roleContext"/>
    ///</summary>
    public static readonly Property roleContext = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#roleContext"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ConcreteClass"/>
    ///</summary>
    public static readonly Resource ConcreteClass = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ConcreteClass"));    

    ///<summary>
    ///A binding agreement between two or more persons that is enforceable by law. (Definition from SUMO). This is an example class for a document type, there are more detailled ontologies to model Contracts.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Contract"/>
    ///</summary>
    public static readonly Class Contract = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Contract"));    

    ///<summary>
    ///Superclass of resources that can be generated by the user.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ClassOrThingOrPropertyOrAssociation"/>
    ///</summary>
    public static readonly Class ClassOrThingOrPropertyOrAssociation = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ClassOrThingOrPropertyOrAssociation"));    

    ///<summary>
    ///The creator of the Personal Information Model. The human being whose mental models are represented in the PIMO.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#createdPimo"/>
    ///</summary>
    public static readonly Property createdPimo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#createdPimo"));    

    ///<summary>
    ///Each element in a PIMO must be connected to the PIMO, to be able to track multiple PIMOs in a distributed scenario. Also, this is the way to find the user that this Thing belongs to.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isDefinedBy"/>
    ///</summary>
    public static readonly Property isDefinedBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isDefinedBy"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#GroundingClosure"/>
    ///</summary>
    public static readonly Resource GroundingClosure = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#GroundingClosure"));    

    ///<summary>
    ///Any piece of work that is undertaken or attempted (Wordnet). An enterprise carefully planned to achieve a particular aim (Oxford Dictionary).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Project"/>
    ///</summary>
    public static readonly Class Project = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Project"));    

    ///<summary>
    ///A physical location. Subclasses are modeled for the most common locations humans work in: Building, City, Country, Room, State. This selection is intended to be applicable cross-cultural and cross-domain. City is a prototype that can be further refined for villages, etc. Subclass of a WGS84:SpatialThing, can have geo-coordinates.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Location"/>
    ///</summary>
    public static readonly Class Location = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Location"));    

    ///<summary>
    ///A properPart of a Building which is separated from the exterior of the Building and/or other Rooms of the Building by walls. Some Rooms may have a specific purpose, e.g. sleeping, bathing, cooking, entertainment, etc. (Definition from SUMO).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Room"/>
    ///</summary>
    public static readonly Class Room = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Room"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#lat"/>
    ///</summary>
    public static readonly Property lat = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#lat"));    

    ///<summary>
    ///Tags in the context of PIMO. A marker class for Things that are used to categorize documents (or other things). Tags must be a kind of Thing and must have a unique label. Documents should not be Tags by default.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Tag"/>
    ///</summary>
    public static readonly Class Tag = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Tag"));    

    ///<summary>
    ///This thing is described further in the object thing. Similar  semantics as skos:isSubjectOf.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isTagFor"/>
    ///</summary>
    public static readonly Property isTagFor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isTagFor"));    

    ///<summary>
    ///hasOtherRepresentation points from a Thing in your PIMO to a thing in an ontology that represents the same real world thing. 
    ///This means that the real world object O represented by an instance I1 has additional representations (as instances I2-In of different conceptualizations).
    ///This means: IF (I_i represents O_j in Ontology_k) AND (I_m represents O_n in Ontology_o) THEN (O_n and O_j are the same object).
    ///hasOtherRepresentation is a transitive relation, but not equivalent (not symmetric nor reflexive).
    ///
    ///For example, the URI of a  foaf:Person representation published on the web is a hasOtherRepresentation for the person. This property is inverse functional, two Things from two information models having the same hasOtherRepresentation are considered to be representations of the same entity from the real world.
    ///
    ///TODO: rename this to subjectIndicatorRef to resemble topic maps ideas?
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOtherRepresentation"/>
    ///</summary>
    public static readonly Property hasOtherRepresentation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOtherRepresentation"));    

    ///<summary>
    ///A Personal Information Model (PIMO) of a user. Represents the sum of all information from the personal knowledge workspace (in literature also referred to as Personal Space of Information (PSI)) which a user needs for Personal Information Management (PIM).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#PersonalInformationModel"/>
    ///</summary>
    public static readonly Class PersonalInformationModel = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#PersonalInformationModel"));    

    ///<summary>
    ///The role of someone attending a social event.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Attendee"/>
    ///</summary>
    public static readonly Class Attendee = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Attendee"));    

    ///<summary>
    ///A person takes a certain role in a given context. The role can be that of "a mentor or another person" or "giving a talk at a meeting", etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#PersonRole"/>
    ///</summary>
    public static readonly Class PersonRole = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#PersonRole"));    

    ///<summary>
    ///This is part of the object. Like a page is part of a book or an engine is part of a car. You can make sub-properties of this to reflect more detailed relations.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#partOf"/>
    ///</summary>
    public static readonly Property partOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#partOf"));    

    ///<summary>
    ///An super-property of all roles that an entity can have in an association. Member is the generic role of a thing in an association. Association subclasses should define sub-properties of this property. Associations can have Things as
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#associationMember"/>
    ///</summary>
    public static readonly Property associationMember = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#associationMember"));    

    ///<summary>
    ///An agent (eg. person, group, software or physical artifact). The Agent class is the class of agents; things that do stuff. A well known sub-class is Person, representing people. Other kinds of agents include Organization and Group.
    ///(inspired by FOAF).
    ///Agent is not a subclass of NAO:Party.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Agent"/>
    ///</summary>
    public static readonly Class Agent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Agent"));    

    ///<summary>
    ///Things that can be at a location. Abstract class, use it as a superclass of things that can be placed in physical space.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Locatable"/>
    ///</summary>
    public static readonly Class Locatable = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Locatable"));    

    ///<summary>
    ///The duration of the process (meeting, event, etc). Difference between start and end time.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#duration"/>
    ///</summary>
    public static readonly Property duration = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#duration"));    

    ///<summary>
    ///This property specifies when the process begins. Inspired by NCAL:dtstart.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#dtstart"/>
    ///</summary>
    public static readonly Property dtstart = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#dtstart"));    

    ///<summary>
    ///An administrative and functional structure (as a business or a political party). (Definition from Merriam-Webster)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Organization"/>
    ///</summary>
    public static readonly Class Organization = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Organization"));    

    ///<summary>
    ///The object is part of the subject. Like a page is part of a book or an engine is part of a car. You can make sub-properties of this to reflect more detailed relations. The semantics of this relations is the same as skos:narrowerPartitive
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasPart"/>
    ///</summary>
    public static readonly Property hasPart = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasPart"));    

    ///<summary>
    ///The thing is related to the other thing. Similar in meaning to skos:related. Symmetric but not transitive.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isRelated"/>
    ///</summary>
    public static readonly Property isRelated = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isRelated"));    

    ///<summary>
    ///Jabber-ID of the user. Used to communicate amongst peers in the social scenario of the semantic desktop. Use the xmpp node identifier as specified by RFC3920, see http://www.xmpp.org/specs/rfc3920.html#addressing-node. The format is the same as e-mail addresses: username@hostname.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#jabberId"/>
    ///</summary>
    public static readonly Property jabberId = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#jabberId"));    

    ///<summary>
    ///The subject Thing was represented previously using the object resource. This indicates that the object resource was a duplicate representation of the subject and merged with the subject. Implementations can use this property to resolve dangling links in distributed system. When encountering resources that are deprecated representations of a Thing, they should be replaced with the Thing. The range is not declared as we assume all knowledge about the object is gone, including its rdf:type.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasDeprecatedRepresentation"/>
    ///</summary>
    public static readonly Property hasDeprecatedRepresentation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasDeprecatedRepresentation"));    

    ///<summary>
    ///Annotating abstract and concrete classes. Implementations may offer the feature to hide abstract classes. By default, classes are concrete. Classes can be declared abstract by setting their classRole to abstract. Instances should not have an abstract class as type (if not inferred).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#classRole"/>
    ///</summary>
    public static readonly Property classRole = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#classRole"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#AbstractClass"/>
    ///</summary>
    public static readonly Resource AbstractClass = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#AbstractClass"));    

    ///<summary>
    ///the attended meeting
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#attendingMeeting"/>
    ///</summary>
    public static readonly Property attendingMeeting = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#attendingMeeting"));    

    ///<summary>
    ///The subject Thing represents the entity that is described in the object InformationElement. The subject Thing is the canonical, unique representation in the personal information model for the entity described in the object. Multiple InformationElements can be the grounding occurrence of the same Thing,  one InformationElement can be the groundingOccurrence of only one Thing.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#groundingOccurrence"/>
    ///</summary>
    public static readonly Property groundingOccurrence = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#groundingOccurrence"));    

    ///<summary>
    ///A large and densely populated urban area; may include several independent administrative districts; "Ancient Troy was a great city". (Definition from SUMO)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#City"/>
    ///</summary>
    public static readonly Class City = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#City"));    

    ///<summary>
    ///The subject location is contained within the object location. For example, a room is located within a building or a city is located within a country.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#locatedWithin"/>
    ///</summary>
    public static readonly Property locatedWithin = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#locatedWithin"));    

    ///<summary>
    ///The subject location contains the object location. For example, a building contains a room or a country contains a city.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#containsLocation"/>
    ///</summary>
    public static readonly Property containsLocation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#containsLocation"));    

    ///<summary>
    ///The social act of assembling for some common purpose; "his meeting with the salesman was the high point of his day". (Definition from SUMO)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Meeting"/>
    ///</summary>
    public static readonly Class Meeting = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Meeting"));    

    ///<summary>
    ///A group of Persons. They are connected to each other by sharing a common attribute, for example they all belong to the same organization or have a common interest. Refer to pimo:Collection for more information about defining collections.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#PersonGroup"/>
    ///</summary>
    public static readonly Class PersonGroup = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#PersonGroup"));    

    ///<summary>
    ///Something that happens
    ///An Event is conceived as compact in time. (Definition from Merriam-Webster)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Event"/>
    ///</summary>
    public static readonly Class Event = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Event"));    

    ///<summary>
    ///Defines if this information model can be modified by the user of the system. This is usually false for imported ontologies and true for the user's own PersonalInformationModel.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isWriteable"/>
    ///</summary>
    public static readonly Property isWriteable = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isWriteable"));    

    ///<summary>
    ///During which time is this association effective? If omitted, the association is always effective. Start time and end-time may be left open, an open start time indicates that the fact is unknown, an open end-time indicates that the end-date is either unknown or the association has not ended.
    ///There can be multiple effectual periods.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#associationEffectual"/>
    ///</summary>
    public static readonly Property associationEffectual = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#associationEffectual"));    

    ///<summary>
    ///This property specifies the date and time when a process ends. Inspired by NCAL:dtend.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#dtend"/>
    ///</summary>
    public static readonly Property dtend = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#dtend"));    

    ///<summary>
    ///The creator of the Personal Information Model. A subproperty of NAO:creator. The human being whose mental models are represented in the PIMO. Range is an Agent.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#creator"/>
    ///</summary>
    public static readonly Property creator = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#creator"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#alt"/>
    ///</summary>
    public static readonly Property alt = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#alt"));    

    ///<summary>
    ///The root topics of this PersonalInformationModel's topic hierarchy. Every topic that has no pimo:superTopic is a root topic. Semantically equivalent to skos:hasTopConcept.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasRootTopic"/>
    ///</summary>
    public static readonly Property hasRootTopic = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasRootTopic"));    

    ///<summary>
    ///A (usually assigned) piece of work (often to be finished within a certain time). (Definition from Merriam-Webster)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Task"/>
    ///</summary>
    public static readonly Class Task = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Task"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#RuleViewSpecificationOccurrenceClosure"/>
    ///</summary>
    public static readonly Resource RuleViewSpecificationOccurrenceClosure = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#RuleViewSpecificationOccurrenceClosure"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#FullPimoView"/>
    ///</summary>
    public static readonly Resource FullPimoView = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#FullPimoView"));    

    ///<summary>
    ///A structure that has a roof and walls and stands more or less permanently in one place; "there was a three-story building on the corner"; "it was an imposing edifice". (Definition from SUMO).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Building"/>
    ///</summary>
    public static readonly Class Building = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Building"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#RuleViewSpecificationInferOccurrences"/>
    ///</summary>
    public static readonly Resource RuleViewSpecificationInferOccurrences = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#RuleViewSpecificationInferOccurrences"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#InferOccurrences"/>
    ///</summary>
    public static readonly Resource InferOccurrences = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#InferOccurrences"));    

    ///<summary>
    ///The subject's contents describes the object. Or the subject can be seen as belonging to the thing described by the object.  Similar semantics as skos:subject.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasTag"/>
    ///</summary>
    public static readonly Property hasTag = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasTag"));    

    ///<summary>
    ///The object of statements is another Thing. Users should be able to edit statements defined with this property. Abstract super-property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#objectProperty"/>
    ///</summary>
    public static readonly Property objectProperty = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#objectProperty"));    

    ///<summary>
    ///The subject Thing is represented also in the object resource. All facts added to the object resource are valid for the subject thing. The subject is the canonical represtation of the object. In particual, this implies when (?object ?p ?v) -> (?subject ?p ?v) and (?s ?p ?object) -> (?s ?p ?subject). The class of the object is not defined, but should be compatible with the class of the subject. Occurrence relations can be inferred through same identifiers or referencingOccurrence relations.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#occurrence"/>
    ///</summary>
    public static readonly Property occurrence = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#occurrence"));    

    ///<summary>
    ///A topic is the subject of a discussion or document. Topics are distinguished from Things in their taxonomic nature, examples are scientific areas such as "Information Science", "Biology", or categories used in content syndication such as "Sports", "Politics". They are specific to the user's domain.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Topic"/>
    ///</summary>
    public static readonly Class Topic = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Topic"));    

    ///<summary>
    ///Superclass of class and thing. To add properties to both class and thing.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ClassOrThing"/>
    ///</summary>
    public static readonly Class ClassOrThing = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ClassOrThing"));    

    ///<summary>
    ///Represents a person. Either living, dead, real or imaginary. (Definition from foaf:Person)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Person"/>
    ///</summary>
    public static readonly Class Person = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#Person"));    

    ///<summary>
    ///A social occasion or activity. (Definition from Merriam-Webster)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#SocialEvent"/>
    ///</summary>
    public static readonly Class SocialEvent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#SocialEvent"));    

    ///<summary>
    ///The subject person or organozation (Agent) is member of the object organization.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isOrganizationMemberOf"/>
    ///</summary>
    public static readonly Property isOrganizationMemberOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#isOrganizationMemberOf"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#OccurrenceClosure"/>
    ///</summary>
    public static readonly Resource OccurrenceClosure = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#OccurrenceClosure"));    

    ///<summary>
    ///Logical media types represent the content aspect of information elements e.g. a flyer, a contract, a promotional video, a todo list.  The user can create new logical media types dependend on their domain: a salesman will need MarketingFlyer, Offer, Invoice while a student might create Report, Thesis and Homework. This is independent from the information element and data object (NIE/NFO) in which the media type will be stored. The same contract can be stored in a PDF file, a text file, or an HTML website.
    ///The groundingOccurrence of a LogicalMediaType is the Document that stores the content.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#LogicalMediaType"/>
    ///</summary>
    public static readonly Class LogicalMediaType = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#LogicalMediaType"));    

    ///<summary>
    ///hasOtherSlot points from a clot  in your PIMO to a slot in a domain ontology that represents the same connection idea.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOtherSlot"/>
    ///</summary>
    public static readonly Property hasOtherSlot = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasOtherSlot"));    

    ///<summary>
    ///Roles of classes in PIMO: concrete instances are Abstract and Concrete.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ClassRole"/>
    ///</summary>
    public static readonly Class ClassRole = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#ClassRole"));    

    ///<summary>
    ///The role of one or multiple persons being a member in one or multiple organizations. Use pimo:organization and pimo:roleHolder to link to the organizations and persons.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#OrganizationMember"/>
    ///</summary>
    public static readonly Class OrganizationMember = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#OrganizationMember"));    

    ///<summary>
    ///Folders can be used to store information elements related to a Thing or Class. This property can be used to connect a Class or Thing to existing Folders. Implementations can suggest annotations for documents stored inside these folders or  suggest the folder for new documents related to the Thing or Class.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasFolder"/>
    ///</summary>
    public static readonly Property hasFolder = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#hasFolder"));    

    ///<summary>
    ///A blog note. You just want to write something down right now and need a place to do that. Add a blog-note! This is an example class for a document type, there are more detailled ontologies to model Blog-Posts (like SIOC).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#BlogPost"/>
    ///</summary>
    public static readonly Class BlogPost = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#BlogPost"));    

    ///<summary>
    ///The subject thing is described in the object document. Ideally, the document is public and its primary topic is the thing. Although this property is not inverse-functional (because the Occurrences are not canonical elements of a formal ontology) this property allows to use public documents, such as wikipedia pages, as indicators identity.  The more formal hasOtherRepresentation property can be used when an ontology about the subject exists.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#referencingOccurrence"/>
    ///</summary>
    public static readonly Property referencingOccurrence = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#referencingOccurrence"));    

    ///<summary>
    ///when is this task due? Represented in ISO 8601, example: 2003-11-22T17:00:00
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#taskDueTime"/>
    ///</summary>
    public static readonly Property taskDueTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/11/01/pimo#taskDueTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#long"/>
    ///</summary>
    public static readonly Property _long = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#long"));
}

///<summary>
///
///
///</summary>
public class tmo : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "tmo";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Creator"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Creator = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Creator"));    

    ///<summary>
    ///They further specify the type a person was related to an task.
    ///Examples instances  of AttachmentRoles are e.g.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PersonInvolvementRole"/>
    ///</summary>
    public static readonly Class PersonInvolvementRole = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PersonInvolvementRole"));    

    ///<summary>
    ///PersonInvolvement  realizes n-ary associations to Persons which are realtedd to an task. The involvement is further characterized by an PersonTaskRole.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PersonInvolvement"/>
    ///</summary>
    public static readonly Class PersonInvolvement = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PersonInvolvement"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#SimilarityDependence"/>
    ///</summary>
    public static readonly Class SimilarityDependence = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#SimilarityDependence"));    

    ///<summary>
    ///In a  PredecessorDependency the dependencyMemberA is the task which is to be executed before dependencyMemberB.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PredecessorDependency"/>
    ///</summary>
    public static readonly Class PredecessorDependency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PredecessorDependency"));    

    ///<summary>
    ///The PredecessorSuccessorDependency enables a directed relation between task. By means of the concrete sublcasses one can further distinguish from which point of view this relation is created.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PredecessorSuccessorDependency"/>
    ///</summary>
    public static readonly Class PredecessorSuccessorDependency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#PredecessorSuccessorDependency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dueDate"/>
    ///</summary>
    public static readonly Property dueDate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dueDate"));    

    ///<summary>
    ///dateTime subsumes various properties with Range XMLSchema:dateTime. If possible they are further grouped by "abstract" properties.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dateTime"/>
    ///</summary>
    public static readonly Property dateTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dateTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Priority"/>
    ///</summary>
    public static readonly Class Priority = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Priority"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Co-worker"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Co_worker = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Co-worker"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#timemanagement"/>
    ///</summary>
    public static readonly Property timemanagement = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#timemanagement"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependency"/>
    ///</summary>
    public static readonly Property dependency = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_10"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_10 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_10"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Importance"/>
    ///</summary>
    public static readonly Class Importance = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Importance"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetTime"/>
    ///</summary>
    public static readonly Property targetTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetTime"));    

    ///<summary>
    ///Endusers can clarify why they created a depedency.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyDescription"/>
    ///</summary>
    public static readonly Property dependencyDescription = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyDescription"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionState"/>
    ///</summary>
    public static readonly Property transmissionState = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionState"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_High"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Delegability_High = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_High"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Delegability"/>
    ///</summary>
    public static readonly Class Delegability = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Delegability"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionType_Transfer"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionType_Transfer = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionType_Transfer"));    

    ///<summary>
    ///By means of the TransmissionType one can distinguish several different types which might imply a different business logic. e.g. delegation can mean that the results of the task fulfillment care to be reported back to the sender of the task.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TransmissionType"/>
    ///</summary>
    public static readonly Class TransmissionType = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TransmissionType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPerson"/>
    ///</summary>
    public static readonly Property involvedPerson = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPerson"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Archived"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_Archived = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Archived"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyType"/>
    ///</summary>
    public static readonly Property dependencyType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetEndTime"/>
    ///</summary>
    public static readonly Property targetEndTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetEndTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_08"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_08 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_08"));    

    ///<summary>
    ///By means of attachments, references to other resources can be established. Resources are information objects. Every Thing, which can be referenced, on the SSD is an information object. In contrast to the usual SSD references/associations, here additionally information can be specified. Further metadata about the role an attachment plays can be stated by means of instances of AttachmentRole. It can be expressed what the Role of attachment is e.g., regarding "desired/requested" or "required" or "potentially useful / somehow related" or "used/produced/achieved". The reference property models the actual link to the attached piece of information.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Attachment"/>
    ///</summary>
    public static readonly Class Attachment = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Attachment"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Never"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Delegability_Never = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Never"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#subTask"/>
    ///</summary>
    public static readonly Property subTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#subTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrierInvolvement"/>
    ///</summary>
    public static readonly Property abilityCarrierInvolvement = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrierInvolvement"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AbilityCarrierRole_Required"/>
    ///</summary>
    public static readonly Resource TMO_Instance_AbilityCarrierRole_Required = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AbilityCarrierRole_Required"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualTime"/>
    ///</summary>
    public static readonly Property actualTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionType"/>
    ///</summary>
    public static readonly Property transmissionType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Medium"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Delegability_Medium = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Medium"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AssociationDependency"/>
    ///</summary>
    public static readonly Class AssociationDependency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AssociationDependency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#nextReviewIntervall"/>
    ///</summary>
    public static readonly Property nextReviewIntervall = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#nextReviewIntervall"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#delegability"/>
    ///</summary>
    public static readonly Property delegability = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#delegability"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Required"/>
    ///</summary>
    public static readonly Resource TMO_Instance_AttachmentRole_Required = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Required"));    

    ///<summary>
    ///AttachmentRoles further specify the type of how an attachment relates to a task. Example instances  of AttachmentRoles are e.g. "desired_request", "required" and "used".
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AttachmentRole"/>
    ///</summary>
    public static readonly Class AttachmentRole = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AttachmentRole"));    

    ///<summary>
    ///The Task Name helps the user to identify a task in a list. It should be expressive enough to give a meaningful recognition. Details should be written in the description attribute instead. A name attribute is not allowed to contain line breaks.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskName"/>
    ///</summary>
    public static readonly Property taskName = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskName"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Controller"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Controller = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Controller"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_06"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_06 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_06"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Accepted_NotTransmitted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionState_Accepted_NotTransmitted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Accepted_NotTransmitted"));    

    ///<summary>
    ///The semantic of this relation is defined in the sublclass of undirected Dependency on which this property is stated. (The subject of the statment where this property is expressed)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyMemberA"/>
    ///</summary>
    public static readonly Property dependencyMemberA = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyMemberA"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_08"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_08 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_08"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskStateChangesFrom"/>
    ///</summary>
    public static readonly Property taskStateChangesFrom = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskStateChangesFrom"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_05"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_05 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_05"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Collaborator"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Collaborator = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Collaborator"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Suspended"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_Suspended = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Suspended"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Priority_High"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Priority_High = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Priority_High"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Priority_Medium"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Priority_Medium = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Priority_Medium"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Low"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Delegability_Low = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Low"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_07"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_07 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_07"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Reviewer"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Reviewer = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Reviewer"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualEndTime"/>
    ///</summary>
    public static readonly Property actualEndTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualEndTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionType_Delegation"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionType_Delegation = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionType_Delegation"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_trashtasks"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskContainer_trashtasks = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_trashtasks"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Used"/>
    ///</summary>
    public static readonly Resource TMO_Instance_AttachmentRole_Used = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Used"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Rejected_NotTransmitted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionState_Rejected_NotTransmitted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Rejected_NotTransmitted"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_03"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_03 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_03"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Involved"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Involved = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Involved"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#importance"/>
    ///</summary>
    public static readonly Property importance = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#importance"));    

    ///<summary>
    ///Ordering of the subtasks listed in the tmo:subTasks property of this Task. This is only for ordering/sorting in GUIs, the semantic relation is defined in subTasks, and if this and subTasks differ, subTasks is the correct list.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#subTaskOrdering"/>
    ///</summary>
    public static readonly Property subTaskOrdering = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#subTaskOrdering"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_01"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_01 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_01"));    

    ///<summary>
    ///connects a Task with an Attachment object. Attachments are associations of Things.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachment"/>
    ///</summary>
    public static readonly Property attachment = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachment"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Receiver"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Receiver = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Receiver"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_09"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_09 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_09"));    

    ///<summary>
    ///examples are e.g. technologies like Java, XML,  ...
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Skill"/>
    ///</summary>
    public static readonly Class Skill = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Skill"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrierRole"/>
    ///</summary>
    public static readonly Property abilityCarrierRole = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrierRole"));    

    ///<summary>
    ///Examples instances  of AbilityCarrirRoles are e.g. "requested", "required" and "used" which further specify the type a person was involved in.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AbilityCarrierRole"/>
    ///</summary>
    public static readonly Class AbilityCarrierRole = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AbilityCarrierRole"));    

    ///<summary>
    ///A symmetric relations between task.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#UndirectedDependency"/>
    ///</summary>
    public static readonly Class UndirectedDependency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#UndirectedDependency"));    

    ///<summary>
    ///StateTypeRole is an abstract class which subsumes various other classes which represent "states" or roles e.g. in role based modelling conpetualisations.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#StateTypeRole"/>
    ///</summary>
    public static readonly Class StateTypeRole = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#StateTypeRole"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualCompletion"/>
    ///</summary>
    public static readonly Property actualCompletion = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualCompletion"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#urgency"/>
    ///</summary>
    public static readonly Property urgency = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#urgency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetStartTime"/>
    ///</summary>
    public static readonly Property targetStartTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetStartTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#startTime"/>
    ///</summary>
    public static readonly Property startTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#startTime"));    

    ///<summary>
    ///States a task can go through during transmission of an task.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TransmissionState"/>
    ///</summary>
    public static readonly Class TransmissionState = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TransmissionState"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Finalized"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_Finalized = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Finalized"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Rejected_Transmitted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionState_Rejected_Transmitted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Rejected_Transmitted"));    

    ///<summary>
    ///The Task Identifier allows a unique identification of a task object within the range of all Nepomuk objects.
    ///The Task Identifier is automatically generated during the creation of a task. The generation of identifiers (IDs) is a Nepomuk architecture issue (Wp2000/WP6000).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskId"/>
    ///</summary>
    public static readonly Property taskId = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskId"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_01"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_01 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_01"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_03"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_03 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_03"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPersons"/>
    ///</summary>
    public static readonly Property involvedPersons = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPersons"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPersonTask"/>
    ///</summary>
    public static readonly Property involvedPersonTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPersonTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#containsTask"/>
    ///</summary>
    public static readonly Property containsTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#containsTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Suggested"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Suggested = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Suggested"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#indexPosition"/>
    ///</summary>
    public static readonly Property indexPosition = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#indexPosition"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#superTask"/>
    ///</summary>
    public static readonly Property superTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#superTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskReference"/>
    ///</summary>
    public static readonly Property taskReference = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskReference"));    

    ///<summary>
    ///The task description helps users to understand the goal and the proceeding of a task. It can also describe the context of a task. The task description is composed at minimum of a summary of what is done to reach the goal. The task description is the main source for identifying related information, e.g., suitable patterns.
    ///A Task Description can be either an informal, described textual content (?TextualDescription) or it can be a more formally structured representation (àFormalDescription).
    ///Technology considerations: Informal descriptions allow for text similarity processing, a formal description allows for applying case based similarity measures.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskDescription"/>
    ///</summary>
    public static readonly Property taskDescription = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskDescription"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#contextTask"/>
    ///</summary>
    public static readonly Property contextTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#contextTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#contextThread"/>
    ///</summary>
    public static readonly Property contextThread = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#contextThread"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_06"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_06 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_06"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrierTask"/>
    ///</summary>
    public static readonly Property abilityCarrierTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrierTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualStartTime"/>
    ///</summary>
    public static readonly Property actualStartTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#actualStartTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Executor"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Executor = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Executor"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskPrivacy_Private"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskPrivacy_Private = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskPrivacy_Private"));    

    ///<summary>
    ///Privacy Status serves for the separation between a professional and a private purpose of a task. This attribute provides with the values "professional/private" a high-level separation of privacy in terms of setting distribution and access
    ///rights to other users for the task.
    ///This separation may arise as a general Nepomuk issue and may therefore be handled in conjunction with a privacy preserving SSD architecture.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskPrivacyState"/>
    ///</summary>
    public static readonly Class TaskPrivacyState = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskPrivacyState"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Related"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Related = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Related"));    

    ///<summary>
    ///For the separation between professional and private purpose of a task, this attribute provides with the values "professional/private" a high level separation of privacy in terms of setting distribution rights to other users for the task.
    ///This separation may arise as a general Nepomuk issue and may therefore be handled in conjunction with a privacy preserving SSD architecture.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskPrivacyState"/>
    ///</summary>
    public static readonly Property taskPrivacyState = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskPrivacyState"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AbilityCarrierRole_Requested"/>
    ///</summary>
    public static readonly Resource TMO_Instance_AbilityCarrierRole_Requested = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AbilityCarrierRole_Requested"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionStateChangesTo"/>
    ///</summary>
    public static readonly Property transmissionStateChangesTo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionStateChangesTo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_05"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_05 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_05"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachmentRole"/>
    ///</summary>
    public static readonly Property attachmentRole = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachmentRole"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskPrivacy_Professional"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskPrivacy_Professional = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskPrivacy_Professional"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Terminated"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_Terminated = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Terminated"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#logEntry"/>
    ///</summary>
    public static readonly Property logEntry = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#logEntry"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionTo"/>
    ///</summary>
    public static readonly Property transmissionTo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionTo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionType_Join"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionType_Join = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionType_Join"));    

    ///<summary>
    ///examples: Architect, Developer, ...
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Role"/>
    ///</summary>
    public static readonly Class Role = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Role"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#receiveDateTime"/>
    ///</summary>
    public static readonly Property receiveDateTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#receiveDateTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AbilityCarrierRole_Used"/>
    ///</summary>
    public static readonly Resource TMO_Instance_AbilityCarrierRole_Used = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AbilityCarrierRole_Used"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#sendDateTime"/>
    ///</summary>
    public static readonly Property sendDateTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#sendDateTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Observer"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Observer = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Observer"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_04"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_04 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_04"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_02"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_02 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_02"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Accepted_Transmitted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionState_Accepted_Transmitted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Accepted_Transmitted"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_New"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_New = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_New"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Transmitted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionState_Transmitted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_Transmitted"));    

    ///<summary>
    ///Inverse of attachment, connects an Attachment Association to the associated Task. Is required for every instance of Attachment.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachmentTask"/>
    ///</summary>
    public static readonly Property attachmentTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachmentTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskStateChangesTo"/>
    ///</summary>
    public static readonly Property taskStateChangesTo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskStateChangesTo"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_InternalObserver"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_InternalObserver = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_InternalObserver"));    

    ///<summary>
    ///The class AbilityCarrier_Involvement ties together an AbilityCarrier with an AbilityCarrier_Role. This is a role based modelling approach. An n-ary relation is realized.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AbilityCarrierInvolvement"/>
    ///</summary>
    public static readonly Class AbilityCarrierInvolvement = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AbilityCarrierInvolvement"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#stateTypeRole"/>
    ///</summary>
    public static readonly Property stateTypeRole = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#stateTypeRole"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Running"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_Running = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Running"));    

    ///<summary>
    ///The task state property allows tracking a task during its lifecycle. Initially the state is just "created". 
    ///The TaskState class was modeled so that for each state can be set which the typical prior and posterior states are. This has the advantage that e.g. a UI can retrieve the allowed states at runtime from the ontology; rather can having this potentially changing knowledge hard coded. But the prior and posterior states are only defaults; the human user is always free to change the state.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskState"/>
    ///</summary>
    public static readonly Class TaskState = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskState"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_outbox"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskContainer_outbox = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_outbox"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskContainer"/>
    ///</summary>
    public static readonly Class TaskContainer = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskContainer"));    

    ///<summary>
    ///The tmo:task is the central entitiey of the tmo. Task can range from vague things to be possibly done in e distant future to concrete things to be done in a precise forseeable manner. It is not unrealisitc to assume that knowledge worker have hundred or more tasks a day.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Task"/>
    ///</summary>
    public static readonly Class Task = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Task"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_04"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_04 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_04"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Urgency"/>
    ///</summary>
    public static readonly Class Urgency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Urgency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#progress"/>
    ///</summary>
    public static readonly Property progress = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#progress"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_archive"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskContainer_archive = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_archive"));    

    ///<summary>
    ///Between the tasks, further dependencies may exist. These dependencies allow for a graph network structure. For ease of use, dependencies should not be too frequent, otherwise the primarily character of a hierarchy would be diminished and a consequent graph representation would become considerable. However, such a graph representation has other drawbacks, the user is likely to loose oversight, tree structures are more helpful in structuring the work.
    ///
    ///A dependency relation is characterized by the type of the relation and by an additional description. There are different possibilities for dependency relations between tasks.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskDependency"/>
    ///</summary>
    public static readonly Class TaskDependency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskDependency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_09"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_09 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_09"));    

    ///<summary>
    ///On the SSD, tasks are not restricted to one person and may cross from
    ///the PTM of one person to the PTM of another. With transmission, we
    ///refer to the process of sending a task – from one person (sender) to one
    ///or more other persons (receiver(s)) (see Section 5.2.1.3 Task
    ///Transmission). Task delegation and task transfer are two special kinds of
    ///task transmission which are described at the end of this section. In
    ///addition, the collaborative task is realized by task transmission.
    ///For the process of sending a task, some information is required. This
    ///information is also modelled in the task ontology. This information is still
    ///useful after the process of sending a task was completed. Task Delegation is a process where the sender of the task restricts the
    ///access rights of the receiver. This includes the right to distribute further
    ///this task and additionally the obligation to give feedback to the sender.
    ///The person that receives a task by delegation usually has not the full
    ///control about the task. The attributes described in the following section
    ///have the purpose to enable such "access rights". The receiver will also
    ///probably have obligations regarding what to report to whom at which
    ///time.
    ///In contrast, the simplest case is that all rights are granted to the receiver
    ///and there is no feedback desired by the sender. What to do with the task
    ///may be apparent by the organization context, or it may be left to the
    ///receiver. This is like sending an email – but with the advantage that the
    ///information is transferred in the "task space" of the participating persons.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskTransmission"/>
    ///</summary>
    public static readonly Class TaskTransmission = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TaskTransmission"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrier"/>
    ///</summary>
    public static readonly Property abilityCarrier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#abilityCarrier"));    

    ///<summary>
    ///AbilityCarrier is an abstract class which circumferences all entities which can take action or which are somehow involved in tasks.
    ///This is in other task conceptualizations rather named "actor". But here it is named AbilityCarrier because it is not neccessarily "active".
    ///
    ///The execution of a task relies on certain abilities. The abstract concept of
    ///Abilitiy_Carriers circumference all those more concrete concepts
    ///of which one can think of while working on tasks. Using this abstract
    ///class enables to substitute such Ability Carrier's in the process of
    ///generating patterns from task instances and vice versa in the process of
    ///instantiating task instances from patterns without violating the schema.
    ///With this attribute, a series of ability carrying entities (Person, Role,
    ///Skill, OrganizationalUnit, InformalDescribedAbility)
    ///and the role of involvement (required, request, used) is enabled. The role
    ///hereby allows specifying how the ability carrying entity is or was
    ///involved.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AbilityCarrier"/>
    ///</summary>
    public static readonly Class AbilityCarrier = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AbilityCarrier"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_10"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Urgency_10 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Urgency_10"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#priority"/>
    ///</summary>
    public static readonly Property priority = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#priority"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#endTime"/>
    ///</summary>
    public static readonly Property endTime = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#endTime"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachmentReference"/>
    ///</summary>
    public static readonly Property attachmentReference = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#attachmentReference"));    

    ///<summary>
    ///By means of the SuperSubTaskDependency one can further describe the subtask-supertask relation .e.g by an descriptin. This enables an n-ary relation between subtask and supertask.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#SuperSubTaskDependency"/>
    ///</summary>
    public static readonly Class SuperSubTaskDependency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#SuperSubTaskDependency"));    

    ///<summary>
    ///In a SuccessorrDependency the dependencyMemberA is the task which is to be executed after dependencyMemberB.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#SuccessorDependency"/>
    ///</summary>
    public static readonly Class SuccessorDependency = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#SuccessorDependency"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_NotTransmitted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TransmissionState_NotTransmitted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TransmissionState_NotTransmitted"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskTransmission"/>
    ///</summary>
    public static readonly Property taskTransmission = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskTransmission"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionTask"/>
    ///</summary>
    public static readonly Property transmissionTask = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionTask"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetCompletion"/>
    ///</summary>
    public static readonly Property targetCompletion = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#targetCompletion"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AgentAbilityCarrier"/>
    ///</summary>
    public static readonly Class AgentAbilityCarrier = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#AgentAbilityCarrier"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_07"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_07 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_07"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionFrom"/>
    ///</summary>
    public static readonly Property transmissionFrom = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionFrom"));    

    ///<summary>
    ///The task state describes the current state of the task as described in Section 5.2.7.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskState"/>
    ///</summary>
    public static readonly Property taskState = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskState"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_inbox"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskContainer_inbox = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_inbox"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Delegate"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Delegate = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Delegate"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionStateChangesFrom"/>
    ///</summary>
    public static readonly Property transmissionStateChangesFrom = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#transmissionStateChangesFrom"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_ExternalObserver"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_ExternalObserver = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_ExternalObserver"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Completed"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_Completed = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Completed"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Owner"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Owner = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Owner"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Desired_Requested"/>
    ///</summary>
    public static readonly Resource TMO_Instance_AttachmentRole_Desired_Requested = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Desired_Requested"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Analyst"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Analyst = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Analyst"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#lastReviewDate"/>
    ///</summary>
    public static readonly Property lastReviewDate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#lastReviewDate"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskGoal"/>
    ///</summary>
    public static readonly Property taskGoal = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskGoal"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#createdBy"/>
    ///</summary>
    public static readonly Property createdBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#createdBy"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_activetasks"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskContainer_activetasks = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskContainer_activetasks"));    

    ///<summary>
    ///The semantic of this relation is defined in the sublclass of undirected Dependency on which this property is stated. (The subject of the statment where this property is expressed)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyMemberB"/>
    ///</summary>
    public static readonly Property dependencyMemberB = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyMemberB"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Unrestricted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Delegability_Unrestricted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Delegability_Unrestricted"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Interdependence"/>
    ///</summary>
    public static readonly Class Interdependence = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#Interdependence"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPersonRole"/>
    ///</summary>
    public static readonly Property involvedPersonRole = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#involvedPersonRole"));    

    ///<summary>
    ///here can be stated from which sources a task was derived. e.g from another task or from an task pattern
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskSource"/>
    ///</summary>
    public static readonly Property taskSource = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#taskSource"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Priority_Low"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Priority_Low = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Priority_Low"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Related"/>
    ///</summary>
    public static readonly Resource TMO_Instance_AttachmentRole_Related = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_AttachmentRole_Related"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyOrderNumber"/>
    ///</summary>
    public static readonly Property dependencyOrderNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#dependencyOrderNumber"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Initiator"/>
    ///</summary>
    public static readonly Resource TMO_Instance_PersonInvolvementRole_Initiator = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_PersonInvolvementRole_Initiator"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Deleted"/>
    ///</summary>
    public static readonly Resource TMO_Instance_TaskState_Deleted = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_TaskState_Deleted"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_02"/>
    ///</summary>
    public static readonly Resource TMO_Instance_Importance_02 = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2008/05/20/tmo#TMO_Instance_Importance_02"));
}

///<summary>
///
///
///</summary>
public class nao : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nao";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/1999/02/22-rdf-syntax-ns#Property"/>
    ///</summary>
    public static readonly Resource Property = new Resource(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#Property"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2000/01/rdf-schema#Class"/>
    ///</summary>
    public static readonly Resource Class = new Resource(new Uri("http://www.w3.org/2000/01/rdf-schema#Class"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2000/01/rdf-schema#Resource"/>
    ///</summary>
    public static readonly Resource Resource = new Resource(new Uri("http://www.w3.org/2000/01/rdf-schema#Resource"));    

    ///<summary>
    ///States the serialization language for a named graph that is represented within a document
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#serializationLanguage"/>
    ///</summary>
    public static readonly Property serializationLanguage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#serializationLanguage"));    

    ///<summary>
    ///Defines a generic identifier for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#identifier"/>
    ///</summary>
    public static readonly Property identifier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#identifier"));    

    ///<summary>
    ///If this property is assigned, the subject class, property, or resource, is deprecated and should not be used in production systems any longer. It may be removed without further notice.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#deprecated"/>
    ///</summary>
    public static readonly Property deprecated = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#deprecated"));    

    ///<summary>
    ///Represents a desktop icon as defined in the FreeDesktop Icon Naming Standard (http://standards.freedesktop.org/icon-naming-spec/icon-naming-spec-latest.html).
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#FreeDesktopIcon"/>
    ///</summary>
    public static readonly Class FreeDesktopIcon = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#FreeDesktopIcon"));    

    ///<summary>
    ///Defines a relationship between two resources, where the subject is a topic of the object
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isTopicOf"/>
    ///</summary>
    public static readonly Property isTopicOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isTopicOf"));    

    ///<summary>
    ///Defines a relationship between two resources, where the object is a topic of the subject
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasTopic"/>
    ///</summary>
    public static readonly Property hasTopic = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasTopic"));    

    ///<summary>
    ///Represents a generic tag
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Tag"/>
    ///</summary>
    public static readonly Class Tag = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Tag"));    

    ///<summary>
    ///An alternative label alongside the preferred label for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#altLabel"/>
    ///</summary>
    public static readonly Property altLabel = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#altLabel"));    

    ///<summary>
    ///Annotation for a resource in the form of a visual representation. Typically the symbol is a double-typed image file or a nao:FreeDesktopIcon.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasSymbol"/>
    ///</summary>
    public static readonly Property hasSymbol = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasSymbol"));    

    ///<summary>
    ///A non-technical textual annotation for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#description"/>
    ///</summary>
    public static readonly Property description = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#description"));    

    ///<summary>
    ///The plural form of the preferred label for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#pluralPrefLabel"/>
    ///</summary>
    public static readonly Property pluralPrefLabel = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#pluralPrefLabel"));    

    ///<summary>
    ///Mark a property, class, or even resource as user visible or not. Non-user-visible entities should never be presented to the user. By default everything is user-visible.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#userVisible"/>
    ///</summary>
    public static readonly Property userVisible = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#userVisible"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Graph"/>
    ///</summary>
    public static readonly Resource Graph = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Graph"));    

    ///<summary>
    ///Defines the default static namespace abbreviation for a graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasDefaultNamespaceAbbreviation"/>
    ///</summary>
    public static readonly Property hasDefaultNamespaceAbbreviation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasDefaultNamespaceAbbreviation"));    

    ///<summary>
    ///Specifies the version of a graph, in numeric format
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#version"/>
    ///</summary>
    public static readonly Property version = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#version"));    

    ///<summary>
    ///Represents a symbol, a visual representation of a resource. Typically a local or remote file would be double-typed to be used as a symbol. An alternative is nao:FreeDesktopIcon.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Symbol"/>
    ///</summary>
    public static readonly Class Symbol = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Symbol"));    

    ///<summary>
    ///Defines a name for a FreeDesktop Icon as defined in the FreeDesktop Icon Naming Standard
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#iconName"/>
    ///</summary>
    public static readonly Property iconName = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#iconName"));    

    ///<summary>
    ///An authoritative score for an item valued between 0 and 1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#score"/>
    ///</summary>
    public static readonly Property score = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#score"));    

    ///<summary>
    ///A marker property to mark selected properties which are input to a mathematical algorithm to generate scores for resources. Properties are marked by being defined as subproperties of this property
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#scoreParameter"/>
    ///</summary>
    public static readonly Property scoreParameter = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#scoreParameter"));    

    ///<summary>
    ///Defines an annotation for a resource in the form of a relationship between the subject resource and another resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isRelated"/>
    ///</summary>
    public static readonly Property isRelated = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isRelated"));    

    ///<summary>
    ///Links a named graph to the resource for which it contains metadata. Its typical usage would be to link the graph containing extracted file metadata to the file resource. This allows for easy maintenance later on.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isDataGraphFor"/>
    ///</summary>
    public static readonly Property isDataGraphFor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isDataGraphFor"));    

    ///<summary>
    ///Annotation for a resource in the form of an unrestricted rating
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#rating"/>
    ///</summary>
    public static readonly Property rating = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#rating"));    

    ///<summary>
    /// Annotation for a resource in the form of a numeric rating (float value), allowed values are between 1 and 10 whereas 0 is interpreted as not set
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#numericRating"/>
    ///</summary>
    public static readonly Property numericRating = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#numericRating"));    

    ///<summary>
    ///States the modification time for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#modified"/>
    ///</summary>
    public static readonly Property modified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#modified"));    

    ///<summary>
    ///States the creation, or first modification time for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#created"/>
    ///</summary>
    public static readonly Property created = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#created"));    

    ///<summary>
    ///Defines the default static namespace for a graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasDefaultNamespace"/>
    ///</summary>
    public static readonly Property hasDefaultNamespace = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasDefaultNamespace"));    

    ///<summary>
    ///Specifies the status of a graph, stable, unstable or testing
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#status"/>
    ///</summary>
    public static readonly Property status = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#status"));    

    ///<summary>
    ///Refers to the single or group of individuals that created the resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#creator"/>
    ///</summary>
    public static readonly Property creator = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#creator"));    

    ///<summary>
    ///Represents a single or a group of individuals
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Party"/>
    ///</summary>
    public static readonly Class Party = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Party"));    

    ///<summary>
    ///Refers to a single or a group of individuals that contributed to a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#contributor"/>
    ///</summary>
    public static readonly Property contributor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#contributor"));    

    ///<summary>
    ///States the last modification time for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#lastModified"/>
    ///</summary>
    public static readonly Property lastModified = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#lastModified"));    

    ///<summary>
    ///Generic annotation for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#annotation"/>
    ///</summary>
    public static readonly Property annotation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#annotation"));    

    ///<summary>
    ///Defines a relationship between a resource and one or more sub resources. Descriptions of sub-resources are only interpretable when the super-resource exists. Deleting a super-resource should then also delete all sub-resources, and transferring a super-resource (for example, sending it to another user) must also include the sub-resource.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasSubResource"/>
    ///</summary>
    public static readonly Property hasSubResource = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasSubResource"));    

    ///<summary>
    ///Defines a relationship between a resource and one or more super resources
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasSuperResource"/>
    ///</summary>
    public static readonly Property hasSuperResource = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasSuperResource"));    

    ///<summary>
    ///States which resources a tag is associated with
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isTagFor"/>
    ///</summary>
    public static readonly Property isTagFor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#isTagFor"));    

    ///<summary>
    ///Defines an existing tag for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasTag"/>
    ///</summary>
    public static readonly Property hasTag = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#hasTag"));    

    ///<summary>
    ///A unique preferred symbol representation for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#prefSymbol"/>
    ///</summary>
    public static readonly Property prefSymbol = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#prefSymbol"));    

    ///<summary>
    ///An alternative symbol representation for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#altSymbol"/>
    ///</summary>
    public static readonly Property altSymbol = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#altSymbol"));    

    ///<summary>
    ///A preferred label for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#prefLabel"/>
    ///</summary>
    public static readonly Property prefLabel = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#prefLabel"));    

    ///<summary>
    ///Specifies the engineering tool used to generate the graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#engineeringTool"/>
    ///</summary>
    public static readonly Property engineeringTool = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#engineeringTool"));    

    ///<summary>
    ///Defines a personal string identifier for a resource
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#personalIdentifier"/>
    ///</summary>
    public static readonly Property personalIdentifier = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#personalIdentifier"));    

    ///<summary>
    ///An agent is the artificial counterpart to nao:Party. It can be a software component or some service.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Agent"/>
    ///</summary>
    public static readonly Class Agent = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#Agent"));    

    ///<summary>
    ///The agent that maintains this resource, ie. created it and knows what to do with it.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nao#maintainedBy"/>
    ///</summary>
    public static readonly Property maintainedBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nao#maintainedBy"));
}

///<summary>
///
///
///</summary>
public class nrl : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nrl";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///Links a metadata graph to the graph for which it specifies the core graph properties including the semantics and the graph namespace. A graph can have only one unique core metadata graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#coreGraphMetadataFor"/>
    ///</summary>
    public static readonly Property coreGraphMetadataFor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#coreGraphMetadataFor"));    

    ///<summary>
    ///Specifies a maximum value cardinality for a specific property
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#maxCardinality"/>
    ///</summary>
    public static readonly Property maxCardinality = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#maxCardinality"));    

    ///<summary>
    ///An abstract class representing all named graph roles
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Data"/>
    ///</summary>
    public static readonly Class Data = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Data"));    

    ///<summary>
    ///Links two properties and specifies their inverse behaviour
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#inverseProperty"/>
    ///</summary>
    public static readonly Property inverseProperty = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#inverseProperty"));    

    ///<summary>
    ///A marker class to identify inverse functional properties
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#InverseFunctionalProperty"/>
    ///</summary>
    public static readonly Class InverseFunctionalProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#InverseFunctionalProperty"));    

    ///<summary>
    ///Represents a named graph containing both schematic and instance data
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#KnowledgeBase"/>
    ///</summary>
    public static readonly Class KnowledgeBase = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#KnowledgeBase"));    

    ///<summary>
    ///A marker class to identify functional properties
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#FunctionalProperty"/>
    ///</summary>
    public static readonly Class FunctionalProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#FunctionalProperty"));    

    ///<summary>
    ///Represents a named graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Graph"/>
    ///</summary>
    public static readonly Class Graph = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Graph"));    

    ///<summary>
    ///Represents a specification of the means to achieve a transformation of an input graph into the required graph view
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ViewSpecification"/>
    ///</summary>
    public static readonly Class ViewSpecification = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ViewSpecification"));    

    ///<summary>
    ///A marker class to identify transitive properties
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#TransitiveProperty"/>
    ///</summary>
    public static readonly Class TransitiveProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#TransitiveProperty"));    

    ///<summary>
    ///Points to the location of the realizer for the external view specification
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#externalRealizer"/>
    ///</summary>
    public static readonly Property externalRealizer = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#externalRealizer"));    

    ///<summary>
    ///Represents a named graph containing configuration data
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Configuration"/>
    ///</summary>
    public static readonly Class Configuration = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Configuration"));    

    ///<summary>
    ///Represents some declarative semantics
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Semantics"/>
    ///</summary>
    public static readonly Class Semantics = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Semantics"));    

    ///<summary>
    ///Links two equivalent named graphs. A symmetric property
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#equivalentGraph"/>
    ///</summary>
    public static readonly Property equivalentGraph = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#equivalentGraph"));    

    ///<summary>
    ///Points to a graph view over the subject named graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#viewOn"/>
    ///</summary>
    public static readonly Property viewOn = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#viewOn"));    

    ///<summary>
    ///A named graph containing instance data that can be recreated by analyzing the original resources. Intended to be used by metadata extractors.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DiscardableInstanceBase"/>
    ///</summary>
    public static readonly Class DiscardableInstanceBase = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DiscardableInstanceBase"));    

    ///<summary>
    ///A non-defining property's value is not part of what defines a resource, it rather
    ///                          is part of the resource's state or expresses an opinion about the resource. Whenever
    ///                          comparing resources or sharing them the value of this property should not be taken into
    ///                          account. By default all properties with a resource range are to be treated as
    ///                          non-defining properties unless they are marked as nrl:DefiningProperty.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#NonDefiningProperty"/>
    ///</summary>
    public static readonly Class NonDefiningProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#NonDefiningProperty"));    

    ///<summary>
    ///Represents a named graph having the role of an Ontology
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Ontology"/>
    ///</summary>
    public static readonly Class Ontology = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Ontology"));    

    ///<summary>
    ///A marker class to identify named graphs that exist within a physical document
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DocumentGraph"/>
    ///</summary>
    public static readonly Class DocumentGraph = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DocumentGraph"));    

    ///<summary>
    ///A core graph metadata property, this defines whether a graph can be freely updated '1' or otherwise '0'
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#updatable"/>
    ///</summary>
    public static readonly Property updatable = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#updatable"));    

    ///<summary>
    ///A marker class to identify symmetric properties
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#SymmetricProperty"/>
    ///</summary>
    public static readonly Class SymmetricProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#SymmetricProperty"));    

    ///<summary>
    ///Models a subsumption relationship between two graphs, stating that the object graph is imported and included in the subject graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#imports"/>
    ///</summary>
    public static readonly Property imports = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#imports"));    

    ///<summary>
    ///Specifies the rule language for a view specification that is driven by rules
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ruleLanguage"/>
    ///</summary>
    public static readonly Property ruleLanguage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ruleLanguage"));    

    ///<summary>
    ///Specifies the precise value cardinality for a specific property
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#cardinality"/>
    ///</summary>
    public static readonly Property cardinality = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#cardinality"));    

    ///<summary>
    ///Identifies a graph which is itself a view of another named graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#GraphView"/>
    ///</summary>
    public static readonly Class GraphView = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#GraphView"));    

    ///<summary>
    ///Links a metadata graph to the graph that is being described. A unique value is compulsory
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#graphMetadataFor"/>
    ///</summary>
    public static readonly Property graphMetadataFor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#graphMetadataFor"));    

    ///<summary>
    ///Points to a representation of the declarative semantics for a graph role
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#hasSemantics"/>
    ///</summary>
    public static readonly Property hasSemantics = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#hasSemantics"));    

    ///<summary>
    ///A marker class to identify asymmetric properties
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#AsymmetricProperty"/>
    ///</summary>
    public static readonly Class AsymmetricProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#AsymmetricProperty"));    

    ///<summary>
    ///Represents a named graph containing schematic data
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Schema"/>
    ///</summary>
    public static readonly Class Schema = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#Schema"));    

    ///<summary>
    ///Points to the human readable specifications for a representation of some declarative semantics
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#semanticsDefinedBy"/>
    ///</summary>
    public static readonly Property semanticsDefinedBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#semanticsDefinedBy"));    

    ///<summary>
    ///Represents a special named graph that contains metadata for another graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#GraphMetadata"/>
    ///</summary>
    public static readonly Class GraphMetadata = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#GraphMetadata"));    

    ///<summary>
    ///Specifies a minimum value cardinality for a specific property
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#minCardinality"/>
    ///</summary>
    public static readonly Property minCardinality = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#minCardinality"));    

    ///<summary>
    ///Represents a named graph containing instance data
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#InstanceBase"/>
    ///</summary>
    public static readonly Class InstanceBase = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#InstanceBase"));    

    ///<summary>
    ///Specifies a subsumption relationship between two graphs, meaning that the object graph is included in the subject graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#superGraphOf"/>
    ///</summary>
    public static readonly Property superGraphOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#superGraphOf"));    

    ///<summary>
    ///Represents a view specification that is composed of a set of rules which generate the required view from the input graph upon firing
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#RuleViewSpecification"/>
    ///</summary>
    public static readonly Class RuleViewSpecification = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#RuleViewSpecification"));    

    ///<summary>
    ///Points to the representation of the view specification required to generate the graph view in question
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#hasSpecification"/>
    ///</summary>
    public static readonly Property hasSpecification = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#hasSpecification"));    

    ///<summary>
    ///Represents an external view specification, this usually being a program which automatically generates the required view for an input graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ExternalViewSpecification"/>
    ///</summary>
    public static readonly Class ExternalViewSpecification = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ExternalViewSpecification"));    

    ///<summary>
    ///Specifies a containment relationship between two graphs, meaning that the subject graph is included in the object graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#subGraphOf"/>
    ///</summary>
    public static readonly Property subGraphOf = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#subGraphOf"));    

    ///<summary>
    ///Represents the default graph, the graph which contains any triple that does not belong to any other named graph
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DefaultGraph"/>
    ///</summary>
    public static readonly Resource DefaultGraph = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DefaultGraph"));    

    ///<summary>
    ///A marker class to identify reflexive properties
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ReflexiveProperty"/>
    ///</summary>
    public static readonly Class ReflexiveProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#ReflexiveProperty"));    

    ///<summary>
    ///Points to a representation of the declarative semantics that the view specification realizes
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#realizes"/>
    ///</summary>
    public static readonly Property realizes = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#realizes"));    

    ///<summary>
    ///Specifies rules for a view specification that is driven by rules
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#rule"/>
    ///</summary>
    public static readonly Property rule = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#rule"));    

    ///<summary>
    ///A defining property's value is part of what defines a resource, changing it means
    ///                          means chaning the identity of the resource. The set of values of all defining
    ///                          properties of a resource make up its identify.
    ///                          Whenever comparing resources or sharing them the value of this property should
    ///                          be taken into account. By default all properties with a literal range are to be
    ///                          treated as defining properties unless they are marked as nrl:NonDefiningProperty.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DefiningProperty"/>
    ///</summary>
    public static readonly Class DefiningProperty = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/08/15/nrl#DefiningProperty"));
}

///<summary>
///
///
///</summary>
public class nco : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "nco";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///Region. Inspired by the fifth part of the value of the 'ADR' property as defined in RFC 2426, sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#region"/>
    ///</summary>
    public static readonly Property region = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#region"));    

    ///<summary>
    ///A role played by a contact. Contacts that denote people, can have many roles (e.g. see the hasAffiliation property and Affiliation class). Contacts that denote Organizations or other Agents usually have one role.  Each role can introduce additional contact media.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Role"/>
    ///</summary>
    public static readonly Class Role = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Role"));    

    ///<summary>
    ///A telephone number with voice communication capabilities. Class inspired by the TYPE=voice parameter of the TEL property defined in RFC 2426 sec. 3.3.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#VoicePhoneNumber"/>
    ///</summary>
    public static readonly Class VoicePhoneNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#VoicePhoneNumber"));    

    ///<summary>
    ///A telephone number.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PhoneNumber"/>
    ///</summary>
    public static readonly Class PhoneNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PhoneNumber"));    

    ///<summary>
    ///A Video telephone number. A class inspired by the TYPE=video parameter of the TEL property defined in RFC 2426 sec. 3.3.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#VideoTelephoneNumber"/>
    ///</summary>
    public static readonly Class VideoTelephoneNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#VideoTelephoneNumber"));    

    ///<summary>
    ///A part of an address specyfing the country. Inspired by the seventh part of the value of the 'ADR' property as defined in RFC 2426, sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#country"/>
    ///</summary>
    public static readonly Property country = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#country"));    

    ///<summary>
    ///An extended part of an address. This field might be used to express parts of an address that aren't include in the name of the Contact but also aren't part of the actual location. Usually the streed address and following fields are enough for a postal letter to arrive. Examples may include ('University of California Campus building 45', 'Sears Tower 34th floor' etc.) Inspired by the second part of the value of the 'ADR' property as defined in RFC 2426, sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#extendedAddress"/>
    ///</summary>
    public static readonly Property extendedAddress = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#extendedAddress"));    

    ///<summary>
    ///A superclass for all contact media - ways to contact an entity represented by a Contact instance. Some of the subclasses of this class (the various kinds of telephone numbers and postal addresses) have been inspired by the values of the TYPE parameter of ADR and TEL properties defined in RFC 2426 sec. 3.2.1. and 3.3.1 respectively. Each value is represented by an appropriate subclass with two major exceptions TYPE=home and TYPE=work. They are to be expressed by the roles these contact media are attached to i.e. contact media with TYPE=home parameter are to be attached to the default role (nco:Contact or nco:PersonContact), whereas media with TYPE=work parameter should be attached to nco:Affiliation or nco:OrganizationContact.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactMedium"/>
    ///</summary>
    public static readonly Class ContactMedium = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactMedium"));    

    ///<summary>
    ///An ISDN phone number. Inspired by the (TYPE=isdn) parameter of the TEL property as defined in RFC 2426 sec  3.3.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IsdnNumber"/>
    ///</summary>
    public static readonly Class IsdnNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IsdnNumber"));    

    ///<summary>
    ///An entity responsible for making contributions to the content of the InformationElement.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contributor"/>
    ///</summary>
    public static readonly Property contributor = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contributor"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#phoneNumber"/>
    ///</summary>
    public static readonly Property phoneNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#phoneNumber"));    

    ///<summary>
    ///Current status of the given IM account. When this property is set, the nco:imStatusType should also always be set. Applications should attempt to parse this property to determine the presence, only falling back to the nco:imStatusType property in the case that this property's value is unrecognised. Values for this property may include 'available', 'offline', 'busy' etc. The exact choice of them is unspecified, although it is recommended to follow the guidance of the Telepathy project when choosing a string identifier http://telepathy.freedesktop.org/spec/Connection_Interface_Simple_Presence.html#description
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imStatus"/>
    ///</summary>
    public static readonly Property imStatus = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imStatus"));    

    ///<summary>
    ///A property used to group contacts into contact groups. This 
    ///    property was NOT defined in the VCARD standard. See documentation for the 
    ///    'ContactList' class for details
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#containsContact"/>
    ///</summary>
    public static readonly Property containsContact = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#containsContact"));    

    ///<summary>
    ///Department. The organizational unit within the organization.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#department"/>
    ///</summary>
    public static readonly Property department = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#department"));    

    ///<summary>
    ///The given name for the object represented by this Contact. See documentation for 'nameFamily' property for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameGiven"/>
    ///</summary>
    public static readonly Property nameGiven = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameGiven"));    

    ///<summary>
    ///To specify the formatted text corresponding to the name of the object the Contact represents. An equivalent of the FN property as defined in RFC 2426 Sec. 3.1.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#fullname"/>
    ///</summary>
    public static readonly Property fullname = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#fullname"));    

    ///<summary>
    ///A group of Contacts. Could be used to express a group in an addressbook or on a contact list of an IM application. One contact can belong to many groups.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactGroup"/>
    ///</summary>
    public static readonly Class ContactGroup = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactGroup"));    

    ///<summary>
    ///The streed address. Inspired by the third part of the value of the 'ADR' property as defined in RFC 2426, sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#streetAddress"/>
    ///</summary>
    public static readonly Property streetAddress = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#streetAddress"));    

    ///<summary>
    ///A number for telephony communication with the object represented by this Contact. An equivalent of the 'TEL' property defined in RFC 2426 Sec. 3.3.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasPhoneNumber"/>
    ///</summary>
    public static readonly Property hasPhoneNumber = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasPhoneNumber"));    

    ///<summary>
    ///Photograph attached to a Contact. The DataObject referred to by this property is usually interpreted as an nfo:Image. Inspired by the PHOTO property defined in RFC 2426 sec. 3.1.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#photo"/>
    ///</summary>
    public static readonly Property photo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#photo"));    

    ///<summary>
    ///A fax number. Inspired by the (TYPE=fax) parameter of the TEL property as defined in RFC 2426 sec  3.3.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#FaxNumber"/>
    ///</summary>
    public static readonly Class FaxNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#FaxNumber"));    

    ///<summary>
    ///A comment about the contact medium. (Deprecated in favor of nie:comment or nao:description - based on the context)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contactMediumComment"/>
    ///</summary>
    public static readonly Property contactMediumComment = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contactMediumComment"));    

    ///<summary>
    ///The URL of the FOAF file.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#foafUrl"/>
    ///</summary>
    public static readonly Property foafUrl = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#foafUrl"));    

    ///<summary>
    ///A car phone number. Inspired by the (TYPE=car) parameter of the TEL property as defined in RFC 2426 sec  3.3.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#CarPhoneNumber"/>
    ///</summary>
    public static readonly Class CarPhoneNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#CarPhoneNumber"));    

    ///<summary>
    ///International Delivery Addresse. Class inspired by TYPE=intl parameter of the ADR property defined in RFC 2426 sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#InternationalDeliveryAddress"/>
    ///</summary>
    public static readonly Class InternationalDeliveryAddress = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#InternationalDeliveryAddress"));    

    ///<summary>
    ///Sound clip attached to a Contact. The DataObject referred to by this property is usually interpreted as an nfo:Audio. Inspired by the SOUND property defined in RFC 2425 sec. 3.6.6.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#sound"/>
    ///</summary>
    public static readonly Property sound = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#sound"));    

    ///<summary>
    ///A nickname attached to a particular IM Account.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imNickname"/>
    ///</summary>
    public static readonly Property imNickname = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imNickname"));    

    ///<summary>
    ///A Blog url.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#blogUrl"/>
    ///</summary>
    public static readonly Property blogUrl = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#blogUrl"));    

    ///<summary>
    ///Domestic Delivery Addresse. Class inspired by TYPE=dom parameter of the ADR property defined in RFC 2426 sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#DomesticDeliveryAddress"/>
    ///</summary>
    public static readonly Class DomesticDeliveryAddress = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#DomesticDeliveryAddress"));    

    ///<summary>
    ///A Female
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#female"/>
    ///</summary>
    public static readonly Resource female = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#female"));    

    ///<summary>
    ///The default Address for a Contact. An equivalent of the 'ADR' property as defined in RFC 2426 Sec. 3.2.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasPostalAddress"/>
    ///</summary>
    public static readonly Property hasPostalAddress = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasPostalAddress"));    

    ///<summary>
    ///Links a PersonContact with an Affiliation.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasAffiliation"/>
    ///</summary>
    public static readonly Property hasAffiliation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasAffiliation"));    

    ///<summary>
    ///A feature common in most IM systems. A message left by the user for all his/her contacts to see.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imStatusMessage"/>
    ///</summary>
    public static readonly Property imStatusMessage = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imStatusMessage"));    

    ///<summary>
    ///End datetime for the role, such as: the datetime of leaving a project or organization, datetime of ending employment, datetime of divorce. If absent or set to a date in the future, the role is currently active.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#end"/>
    ///</summary>
    public static readonly Property end = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#end"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imCapabilityText"/>
    ///</summary>
    public static readonly Resource imCapabilityText = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imCapabilityText"));    

    ///<summary>
    ///Indicates that an IMAccount has a certain capability.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasIMCapability"/>
    ///</summary>
    public static readonly Property hasIMCapability = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasIMCapability"));    

    ///<summary>
    ///Indicates that this IMAccount publishes its presence information to the other IMAccount.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#publishesPresenceTo"/>
    ///</summary>
    public static readonly Property publishesPresenceTo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#publishesPresenceTo"));    

    ///<summary>
    ///Indicates that this IMAccount has requested a subscription to the presence information of the other IMAccount.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#requestedPresenceSubscriptionTo"/>
    ///</summary>
    public static readonly Property requestedPresenceSubscriptionTo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#requestedPresenceSubscriptionTo"));    

    ///<summary>
    ///Indicates that this IMAccount has been blocked.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#isBlocked"/>
    ///</summary>
    public static readonly Property isBlocked = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#isBlocked"));    

    ///<summary>
    ///A postal address. A class aggregating the various parts of a value for the 'ADR' property as defined in RFC 2426 Sec. 3.2.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PostalAddress"/>
    ///</summary>
    public static readonly Class PostalAddress = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PostalAddress"));    

    ///<summary>
    ///A suffix for the name of the Object represented by the given object. See documentation for the 'nameFamily' for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameHonorificSuffix"/>
    ///</summary>
    public static readonly Property nameHonorificSuffix = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameHonorificSuffix"));    

    ///<summary>
    ///A Contact that denotes a Person. A person can have multiple Affiliations.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PersonContact"/>
    ///</summary>
    public static readonly Class PersonContact = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PersonContact"));    

    ///<summary>
    ///The family name of an Object represented by this Contact. These applies to people that have more than one given name. The 'first' one is considered 'the' given name (see nameGiven) property. All additional ones are considered 'additional' names. The name inherited from parents is the 'family name'. e.g. For Dr. John Phil Paul Stevenson Jr. M.D. A.C.P. we have contact with: honorificPrefix: 'Dr.', nameGiven: 'John', nameAdditional: 'Phil', nameAdditional: 'Paul', nameFamily: 'Stevenson', honorificSuffix: 'Jr.', honorificSuffix: 'M.D.', honorificSuffix: 'A.C.P.'. These properties form an equivalent of the compound 'N' property as defined in RFC 2426 Sec. 3.1.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameFamily"/>
    ///</summary>
    public static readonly Property nameFamily = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameFamily"));    

    ///<summary>
    ///A prefix for the name of the object represented by this Contact. See documentation for the 'nameFamily' property for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameHonorificPrefix"/>
    ///</summary>
    public static readonly Property nameHonorificPrefix = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameHonorificPrefix"));    

    ///<summary>
    ///Indicates that an Instant Messaging account owned by an entity represented by this contact.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasIMAccount"/>
    ///</summary>
    public static readonly Property hasIMAccount = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasIMAccount"));    

    ///<summary>
    ///Geographical location of the contact. Inspired by the 'GEO' property specified in RFC 2426 Sec. 3.4.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasLocation"/>
    ///</summary>
    public static readonly Property hasLocation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasLocation"));    

    ///<summary>
    ///A contact list, this class represents an addressbook or a contact list of an IM application. Contacts inside a contact list can belong to contact groups.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactList"/>
    ///</summary>
    public static readonly Class ContactList = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactList"));    

    ///<summary>
    ///Aggregates three properties defined in RFC2426. Originally all three were attached directly to a person. One person could have only one title and one role within one organization. This class is intended to lift this limitation.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Affiliation"/>
    ///</summary>
    public static readonly Class Affiliation = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Affiliation"));    

    ///<summary>
    ///Identifier of the IM account. Examples of such identifier might include ICQ UINs, Jabber IDs, Skype names etc.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imID"/>
    ///</summary>
    public static readonly Property imID = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imID"));    

    ///<summary>
    ///The geographical location of a postal address.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#addressLocation"/>
    ///</summary>
    public static readonly Property addressLocation = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#addressLocation"));    

    ///<summary>
    ///An object that represent an object represented by this Contact. Usually this property is used to link a Contact to an organization, to a contact to the representative of this organization the user directly interacts with. An equivalent for the 'AGENT' property defined in RFC 2426 Sec. 3.5.4
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#representative"/>
    ///</summary>
    public static readonly Property representative = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#representative"));    

    ///<summary>
    ///A Bulletin Board System (BBS) phone number. Inspired by the (TYPE=bbsl) parameter of the TEL property as defined in RFC 2426 sec  3.3.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#BbsNumber"/>
    ///</summary>
    public static readonly Class BbsNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#BbsNumber"));    

    ///<summary>
    ///A modem phone number. Inspired by the (TYPE=modem) parameter of the TEL property as defined in RFC 2426 sec  3.3.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ModemNumber"/>
    ///</summary>
    public static readonly Class ModemNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ModemNumber"));    

    ///<summary>
    ///Logo of a company. Inspired by the LOGO property defined in RFC 2426 sec. 3.5.3
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#logo"/>
    ///</summary>
    public static readonly Property logo = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#logo"));    

    ///<summary>
    ///A url of a website.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#websiteUrl"/>
    ///</summary>
    public static readonly Property websiteUrl = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#websiteUrl"));    

    ///<summary>
    ///A Male
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#male"/>
    ///</summary>
    public static readonly Resource male = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#male"));    

    ///<summary>
    ///Birth date of the object represented by this Contact. An equivalent of the 'BDAY' property as defined in RFC 2426 Sec. 3.1.5.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#birthDate"/>
    ///</summary>
    public static readonly Property birthDate = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#birthDate"));    

    ///<summary>
    ///Postal Code. Inspired by the sixth part of the value of the 'ADR' property as defined in RFC 2426, sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#postalcode"/>
    ///</summary>
    public static readonly Property postalcode = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#postalcode"));    

    ///<summary>
    ///Parcel Delivery Addresse. Class inspired by TYPE=parcel parameter of the ADR property defined in RFC 2426 sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ParcelDeliveryAddress"/>
    ///</summary>
    public static readonly Class ParcelDeliveryAddress = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ParcelDeliveryAddress"));    

    ///<summary>
    ///Deprecated in favour of nco:imCapabilityAudio.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#AudioIMAccount"/>
    ///</summary>
    public static readonly Class AudioIMAccount = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#AudioIMAccount"));    

    ///<summary>
    ///Links a Contact with a ContactGroup it belongs to.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#belongsToGroup"/>
    ///</summary>
    public static readonly Property belongsToGroup = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#belongsToGroup"));    

    ///<summary>
    ///The name of the contact group. This property was NOT defined 
    ///    in the VCARD standard. See documentation of the 'ContactGroup' class for 
    ///    details
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contactGroupName"/>
    ///</summary>
    public static readonly Property contactGroupName = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contactGroupName"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#emailAddress"/>
    ///</summary>
    public static readonly Property emailAddress = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#emailAddress"));    

    ///<summary>
    ///Locality or City. Inspired by the fourth part of the value of the 'ADR' property as defined in RFC 2426, sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#locality"/>
    ///</summary>
    public static readonly Property locality = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#locality"));    

    ///<summary>
    ///A hobby associated with a PersonContact. This property can be used to express hobbies and interests.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hobby"/>
    ///</summary>
    public static readonly Property hobby = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hobby"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imCapabilityAudio"/>
    ///</summary>
    public static readonly Resource imCapabilityAudio = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imCapabilityAudio"));    

    ///<summary>
    ///An encryption key attached to a contact. Inspired by the KEY property defined in RFC 2426 sec. 3.7.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#key"/>
    ///</summary>
    public static readonly Property key = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#key"));    

    ///<summary>
    ///A uniform resource locator associated with the given role of a Contact. Inspired by the 'URL' property defined in RFC 2426 Sec. 3.6.8.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#url"/>
    ///</summary>
    public static readonly Property url = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#url"));    

    ///<summary>
    ///A value that represents a globally unique  identifier corresponding to the individual or resource associated with the Contact. An equivalent of the 'UID' property defined in RFC 2426 Sec. 3.6.7
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contactUID"/>
    ///</summary>
    public static readonly Property contactUID = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#contactUID"));    

    ///<summary>
    ///An entity responsible for making the InformationElement available.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#publisher"/>
    ///</summary>
    public static readonly Property publisher = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#publisher"));    

    ///<summary>
    ///An account in an Instant Messaging system.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMAccount"/>
    ///</summary>
    public static readonly Class IMAccount = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMAccount"));    

    ///<summary>
    ///A superProperty for all properties linking a Contact to an instance of a contact medium.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasContactMedium"/>
    ///</summary>
    public static readonly Property hasContactMedium = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasContactMedium"));    

    ///<summary>
    ///Creator of an information element, an entity primarily responsible for the creation of the content of the data object.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#creator"/>
    ///</summary>
    public static readonly Property creator = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#creator"));    

    ///<summary>
    ///A nickname of the Object represented by this Contact. This is an equivalent of the 'NICKNAME' property as defined in RFC 2426 Sec. 3.1.3.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nickname"/>
    ///</summary>
    public static readonly Property nickname = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nickname"));    

    ///<summary>
    ///An entity occuring on a contact list (usually interpreted as an nco:Contact)
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactListDataObject"/>
    ///</summary>
    public static readonly Class ContactListDataObject = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#ContactListDataObject"));    

    ///<summary>
    ///A note about the object represented by this Contact. An equivalent for the 'NOTE' property defined in RFC 2426 Sec. 3.6.2
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#note"/>
    ///</summary>
    public static readonly Property note = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#note"));    

    ///<summary>
    ///Additional given name of an object represented by this contact. See documentation for 'nameFamily' property for details.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameAdditional"/>
    ///</summary>
    public static readonly Property nameAdditional = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#nameAdditional"));    

    ///<summary>
    ///Personal Communication Services Number. A class inspired by the TYPE=pcs parameter of the TEL property defined in RFC 2426 sec. 3.3.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PcsNumber"/>
    ///</summary>
    public static readonly Class PcsNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PcsNumber"));    

    ///<summary>
    ///A Contact that denotes on Organization.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#OrganizationContact"/>
    ///</summary>
    public static readonly Class OrganizationContact = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#OrganizationContact"));    

    ///<summary>
    ///A pager phone number. Inspired by the (TYPE=pager) parameter of the TEL property as defined in RFC 2426 sec  3.3.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PagerNumber"/>
    ///</summary>
    public static readonly Class PagerNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#PagerNumber"));    

    ///<summary>
    ///A number that can accept textual messages.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#MessagingNumber"/>
    ///</summary>
    public static readonly Class MessagingNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#MessagingNumber"));    

    ///<summary>
    ///Gender. Instances of this class may include male and female.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Gender"/>
    ///</summary>
    public static readonly Class Gender = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Gender"));    

    ///<summary>
    ///An email address. The recommended best practice is to use mailto: uris for instances of this class.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#EmailAddress"/>
    ///</summary>
    public static readonly Class EmailAddress = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#EmailAddress"));    

    ///<summary>
    ///Name of an organization or a unit within an organization the object represented by a Contact is associated with. An equivalent of the 'ORG' property defined in RFC 2426 Sec. 3.5.5
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#org"/>
    ///</summary>
    public static readonly Property org = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#org"));    

    ///<summary>
    ///The official title  the object represented by this contact in an organization. E.g. 'CEO', 'Director, Research and Development', 'Junior Software Developer/Analyst' etc. An equivalent of the 'TITLE' property defined in RFC 2426 Sec. 3.5.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#title"/>
    ///</summary>
    public static readonly Property title = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#title"));    

    ///<summary>
    ///Indicates if the given number accepts voice mail. (e.g. there is an answering machine). Inspired by TYPE=msg parameter of the TEL property defined in RFC 2426 sec. 3.3.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#voiceMail"/>
    ///</summary>
    public static readonly Property voiceMail = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#voiceMail"));    

    ///<summary>
    ///Deprecated in favour of nco:imCapabilityVideo.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#VideoIMAccount"/>
    ///</summary>
    public static readonly Class VideoIMAccount = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#VideoIMAccount"));    

    ///<summary>
    ///A cellular phone number. Inspired by the (TYPE=cell) parameter of the TEL property as defined in RFC 2426 sec  3.3.1. Usually a cellular phone can accept voice calls as well as textual messages (SMS), therefore this class has two superclasses.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#CellPhoneNumber"/>
    ///</summary>
    public static readonly Class CellPhoneNumber = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#CellPhoneNumber"));    

    ///<summary>
    ///Role an object represented by this contact represents in the organization. This might include 'Programmer', 'Manager', 'Sales Representative'. Be careful to avoid confusion with the title property. An equivalent of the 'ROLE' property as defined in RFC 2426. Sec. 3.5.2. Note the difference between nco:Role class and nco:role property.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#role"/>
    ///</summary>
    public static readonly Property role = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#role"));    

    ///<summary>
    ///Type of the IM account. This may be the name of the service that provides the IM functionality. Examples might include Jabber, ICQ, MSN etc
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imAccountType"/>
    ///</summary>
    public static readonly Property imAccountType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imAccountType"));    

    ///<summary>
    ///Post office box. This is the first part of the value of the 'ADR' property as defined in RFC 2426, sec. 3.2.1
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#pobox"/>
    ///</summary>
    public static readonly Property pobox = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#pobox"));    

    ///<summary>
    ///Gender of the given contact.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#gender"/>
    ///</summary>
    public static readonly Property gender = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#gender"));    

    ///<summary>
    ///Start datetime for the role, such as: the datetime of joining a project or organization, datetime of starting employment, datetime of marriage
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#start"/>
    ///</summary>
    public static readonly Property start = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#start"));    

    ///<summary>
    ///Capabilities of a cetain IMAccount.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMCapability"/>
    ///</summary>
    public static readonly Class IMCapability = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMCapability"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imCapabilityVideo"/>
    ///</summary>
    public static readonly Resource imCapabilityVideo = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imCapabilityVideo"));    

    ///<summary>
    ///Indicates the local IMAccount by which this IMAccount is accessed. This does not imply membership of a contact list.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#isAccessedBy"/>
    ///</summary>
    public static readonly Property isAccessedBy = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#isAccessedBy"));    

    ///<summary>
    ///A Contact. A piece of data that can provide means to identify or communicate with an entity.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Contact"/>
    ///</summary>
    public static readonly Class Contact = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#Contact"));    

    ///<summary>
    ///The status type of an IMAccount. Based on the Connection_Presence_Type enumeration of the Telepathy project: http://telepathy.freedesktop.org/spec/Connection_Interface_Simple_Presence.html#Enum:Connection_Presence_Type
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusType"/>
    ///</summary>
    public static readonly Class IMStatusType = new Class(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeExtendedAway"/>
    ///</summary>
    public static readonly Resource IMStatusTypeExtendedAway = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeExtendedAway"));    

    ///<summary>
    ///An address for electronic mail communication with the object specified by this contact. An equivalent of the 'EMAIL' property as defined in RFC 2426 Sec. 3.3.1.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasEmailAddress"/>
    ///</summary>
    public static readonly Property hasEmailAddress = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#hasEmailAddress"));    

    ///<summary>
    ///Current status type of the given IM account. When this property is set, the nco:imStatus property should also always be set. Applications should attempt to parse the nco:imStatus property to determine the presence, only falling back to this property in the case that the nco:imStatus property's value is unrecognised.
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imStatusType"/>
    ///</summary>
    public static readonly Property imStatusType = new Property(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#imStatusType"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeOffline"/>
    ///</summary>
    public static readonly Resource IMStatusTypeOffline = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeOffline"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeAvailable"/>
    ///</summary>
    public static readonly Resource IMStatusTypeAvailable = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeAvailable"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeAway"/>
    ///</summary>
    public static readonly Resource IMStatusTypeAway = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeAway"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeHidden"/>
    ///</summary>
    public static readonly Resource IMStatusTypeHidden = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeHidden"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeBusy"/>
    ///</summary>
    public static readonly Resource IMStatusTypeBusy = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeBusy"));    

    ///<summary>
    ///
    ///<see cref="http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeUnknown"/>
    ///</summary>
    public static readonly Resource IMStatusTypeUnknown = new Resource(new Uri("http://www.semanticdesktop.org/ontologies/2007/03/22/nco#IMStatusTypeUnknown"));
}

///<summary>
///Path Projection Ontology
///The Path Projection Ontology provides a vocabulary for describing how valid URLs can be computed using the properties of a resource.
///</summary>
public class ppo : Ontology
{
    public static readonly Uri Namespace = new Uri("http://semiodesk.com/ontologies/2012/ppo#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "ppo";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///(One part of a name that consists of the given literal value., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#LiteralPart"/>
    ///</summary>
    public static readonly Class LiteralPart = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#LiteralPart"));    

    ///<summary>
    ///(The class of all text transformations which are applied to texts at sentence level., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#SentenceTransform"/>
    ///</summary>
    public static readonly Class SentenceTransform = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#SentenceTransform"));    

    ///<summary>
    ///(The class of all text transformations which are applied to texts at word level., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#WordTransform"/>
    ///</summary>
    public static readonly Class WordTransform = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#WordTransform"));    

    ///<summary>
    ///(The class of all properties that identify URI syntax components., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#hasComponent"/>
    ///</summary>
    public static readonly Property hasComponent = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#hasComponent"));    

    ///<summary>
    ///(Constitutes a hierarchical relationship between two resources., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#hasChild"/>
    ///</summary>
    public static readonly Property hasChild = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#hasChild"));    

    ///<summary>
    ///(States that an element should be part of the generated name of a resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#hasNamePart"/>
    ///</summary>
    public static readonly Property hasNamePart = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#hasNamePart"));    

    ///<summary>
    ///(Returns a string in which all occurances of a certain character sequence is replaced by a given character sequence., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#ReplaceTransform"/>
    ///</summary>
    public static readonly Resource ReplaceTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#ReplaceTransform"));    

    ///<summary>
    ///(Returns a string in which all characters of the given input string have been transformed into lower case., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#LowerCaseTransform"/>
    ///</summary>
    public static readonly Resource LowerCaseTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#LowerCaseTransform"));    

    ///<summary>
    ///(Returns a string in which all characters of the given input string have been transformed into upper case., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#UpperCaseTransform"/>
    ///</summary>
    public static readonly Resource UpperCaseTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#UpperCaseTransform"));    

    ///<summary>
    ///(Parses a given DateTime object and returns a formatted string., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#DateTimeSerialization"/>
    ///</summary>
    public static readonly Resource DateTimeSerialization = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#DateTimeSerialization"));    

    ///<summary>
    ///(One component of a URI (RFC 3986)., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#UriComponent"/>
    ///</summary>
    public static readonly Class UriComponent = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#UriComponent"));    

    ///<summary>
    ///(One part of a name., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#NamePart"/>
    ///</summary>
    public static readonly Class NamePart = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#NamePart"));    

    ///<summary>
    ///(Expresses the importance of a resource in relation to another., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#priority"/>
    ///</summary>
    public static readonly Property priority = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#priority"));    

    ///<summary>
    ///(States that the given literal value should be inserted into the name of a resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#value"/>
    ///</summary>
    public static readonly Property value = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#value"));    

    ///<summary>
    ///(States that the literal value of the denoted property should be inserted into the name of a resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#valueOf"/>
    ///</summary>
    public static readonly Property valueOf = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#valueOf"));    

    ///<summary>
    ///(States that the projection of a resource should be inserted into the name of the denoted resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#projectionOf"/>
    ///</summary>
    public static readonly Property projectionOf = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#projectionOf"));    

    ///<summary>
    ///(Transforms the first character of a given string to upper case format., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#CapitalizeTransform"/>
    ///</summary>
    public static readonly Resource CapitalizeTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#CapitalizeTransform"));    

    ///<summary>
    ///(Removes the words from a list of 'stop words' which is dependend on the language tag of the provided input string., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#RemoveStopWordsTransform"/>
    ///</summary>
    public static readonly Resource RemoveStopWordsTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#RemoveStopWordsTransform"));    

    ///<summary>
    ///(The base URI of the URI generated by the denoted scheme., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#baseUri"/>
    ///</summary>
    public static readonly Property baseUri = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#baseUri"));    

    ///<summary>
    ///(A set of specifications which define how URIs (RFC 3986) should be generated from the properties of a given resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#UriSchema"/>
    ///</summary>
    public static readonly Class UriSchema = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#UriSchema"));    

    ///<summary>
    ///(An organizational unit, or container, used to organize folders and files into a hierarchical structure., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#Directory"/>
    ///</summary>
    public static readonly Class Directory = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#Directory"));    

    ///<summary>
    ///(A collection of data or information. , en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#File"/>
    ///</summary>
    public static readonly Class File = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#File"));    

    ///<summary>
    ///(One part of a name that is being generated from the property of the projected resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#PropertyPart"/>
    ///</summary>
    public static readonly Class PropertyPart = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#PropertyPart"));    

    ///<summary>
    ///(One part of a name that is being generated from a resource which is related to the projected resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#ResourcePart"/>
    ///</summary>
    public static readonly Class ResourcePart = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#ResourcePart"));    

    ///<summary>
    ///(The class of transformations which are applied to text., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#TextTransform"/>
    ///</summary>
    public static readonly Class TextTransform = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#TextTransform"));    

    ///<summary>
    ///(The class of all resources which are capable of producing a sequence of characters from a given data object., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#Serialization"/>
    ///</summary>
    public static readonly Class Serialization = new Class(new Uri("http://semiodesk.com/ontologies/2012/ppo#Serialization"));    

    ///<summary>
    ///(States that a URI schema defines a file system element to part of its generated URI string., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#hasPath"/>
    ///</summary>
    public static readonly Property hasPath = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#hasPath"));    

    ///<summary>
    ///(Indicates that a resource is not required., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#isOptional"/>
    ///</summary>
    public static readonly Property isOptional = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#isOptional"));    

    ///<summary>
    ///(States that a resource defines a mapping of a certain class., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#mappedType"/>
    ///</summary>
    public static readonly Property mappedType = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#mappedType"));    

    ///<summary>
    ///(Defines in which way a character string should be generated from the property value of a resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#serialization"/>
    ///</summary>
    public static readonly Property serialization = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#serialization"));    

    ///<summary>
    ///(States that a text transformation should be applied to the serialized property value of a resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#textTransform"/>
    ///</summary>
    public static readonly Property textTransform = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#textTransform"));    

    ///<summary>
    ///(Specifies the unique position of a thing in a set., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#order"/>
    ///</summary>
    public static readonly Property order = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#order"));    

    ///<summary>
    ///(States that the given string defines how a given data object should be serialized., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#formatString"/>
    ///</summary>
    public static readonly Property formatString = new Property(new Uri("http://semiodesk.com/ontologies/2012/ppo#formatString"));    

    ///<summary>
    ///(Removes any characters from the given input string which are not valid URL characters according to RFC 3986., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#RemoveSpecialCharactersTransform"/>
    ///</summary>
    public static readonly Resource RemoveSpecialCharactersTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#RemoveSpecialCharactersTransform"));    

    ///<summary>
    ///(Returns a string in which each word in the given input string has been capitalized and with all white spaces removed., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#CamelCaseTransform"/>
    ///</summary>
    public static readonly Resource CamelCaseTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#CamelCaseTransform"));    

    ///<summary>
    ///(Parses a given Boolean object and returns a formatted string. , en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#BooleanSerialization"/>
    ///</summary>
    public static readonly Resource BooleanSerialization = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#BooleanSerialization"));    

    ///<summary>
    ///(Removes any whitespace characters from the given input string., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#RemoveWhitespacesTransform"/>
    ///</summary>
    public static readonly Resource RemoveWhitespacesTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#RemoveWhitespacesTransform"));    

    ///<summary>
    ///(Removes any occurences of text which can be matched to a DateTime format. , en)
    ///<see cref="http://semiodesk.com/ontologies/2012/ppo#RemoveDateTimeTransform"/>
    ///</summary>
    public static readonly Resource RemoveDateTimeTransform = new Resource(new Uri("http://semiodesk.com/ontologies/2012/ppo#RemoveDateTimeTransform"));
}

///<summary>
///Semiodesk User Model Ontology
///The Semiodesk User Model Ontology provides a vocabulary for describing the data and personal information owned by a computer user.
///</summary>
public class sum : Ontology
{
    public static readonly Uri Namespace = new Uri("http://semiodesk.com/ontologies/2012/sum#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "sum";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#hasFavorite"/>
    ///</summary>
    public static readonly Property hasFavorite = new Property(new Uri("http://semiodesk.com/ontologies/2012/sum#hasFavorite"));    

    ///<summary>
    ///(States that someone is the owner of the denoted resource., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#owner"/>
    ///</summary>
    public static readonly Property owner = new Property(new Uri("http://semiodesk.com/ontologies/2012/sum#owner"));    

    ///<summary>
    ///
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#GoogleAccount"/>
    ///</summary>
    public static readonly Class GoogleAccount = new Class(new Uri("http://semiodesk.com/ontologies/2012/sum#GoogleAccount"));    

    ///<summary>
    ///
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#hasOAuthRefreshToken"/>
    ///</summary>
    public static readonly Property hasOAuthRefreshToken = new Property(new Uri("http://semiodesk.com/ontologies/2012/sum#hasOAuthRefreshToken"));    

    ///<summary>
    ///(States that a user is the owner of the denoted computer or online account., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#hasAccount"/>
    ///</summary>
    public static readonly Property hasAccount = new Property(new Uri("http://semiodesk.com/ontologies/2012/sum#hasAccount"));    

    ///<summary>
    ///
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#hasCollection"/>
    ///</summary>
    public static readonly Property hasCollection = new Property(new Uri("http://semiodesk.com/ontologies/2012/sum#hasCollection"));    

    ///<summary>
    ///(The person using a computer system., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#User"/>
    ///</summary>
    public static readonly Class User = new Class(new Uri("http://semiodesk.com/ontologies/2012/sum#User"));    

    ///<summary>
    ///
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#UserAccount"/>
    ///</summary>
    public static readonly Class UserAccount = new Class(new Uri("http://semiodesk.com/ontologies/2012/sum#UserAccount"));    

    ///<summary>
    ///(States that the denoted string is the default namespace for a user., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#hasNamespace"/>
    ///</summary>
    public static readonly Property hasNamespace = new Property(new Uri("http://semiodesk.com/ontologies/2012/sum#hasNamespace"));    

    ///<summary>
    ///
    ///<see cref="http://semiodesk.com/ontologies/2012/sum#favouriteColor"/>
    ///</summary>
    public static readonly Property favouriteColor = new Property(new Uri("http://semiodesk.com/ontologies/2012/sum#favouriteColor"));
}

///<summary>
///Semiodesk View Model Ontology
///The Semiodesk View Model Ontology provides a vocabulary for describing user interface elements and interaction logic.
///</summary>
public class svm : Ontology
{
    public static readonly Uri Namespace = new Uri("http://semiodesk.com/ontologies/2012/svm#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "svm";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///(A part of a user interface which provides possibilities for user interaction., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#Control"/>
    ///</summary>
    public static readonly Class Control = new Class(new Uri("http://semiodesk.com/ontologies/2012/svm#Control"));    

    ///<summary>
    ///(A hyperlink which has rectangular shape and can be arranged in a collection., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#Tile"/>
    ///</summary>
    public static readonly Class Tile = new Class(new Uri("http://semiodesk.com/ontologies/2012/svm#Tile"));    

    ///<summary>
    ///(A string character identifying an icon in the Icono icon font., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#fontIcon"/>
    ///</summary>
    public static readonly Property fontIcon = new Property(new Uri("http://semiodesk.com/ontologies/2012/svm#fontIcon"));    

    ///<summary>
    ///(A value indicating the width of a preview column in pixel., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#columnWidth"/>
    ///</summary>
    public static readonly Property columnWidth = new Property(new Uri("http://semiodesk.com/ontologies/2012/svm#columnWidth"));    

    ///<summary>
    ///(An interface which provides data visualizations and possibilities for user interaction., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#View"/>
    ///</summary>
    public static readonly Class View = new Class(new Uri("http://semiodesk.com/ontologies/2012/svm#View"));    

    ///<summary>
    ///(References to a fixed position in a set of columns., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#columnPosition"/>
    ///</summary>
    public static readonly Property columnPosition = new Property(new Uri("http://semiodesk.com/ontologies/2012/svm#columnPosition"));    

    ///<summary>
    ///(References to a fixed position in a set of tiles., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#tilePosition"/>
    ///</summary>
    public static readonly Property tilePosition = new Property(new Uri("http://semiodesk.com/ontologies/2012/svm#tilePosition"));    

    ///<summary>
    ///(Specifies the area a tile should occupy in multiples of one tile size., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#tileSize"/>
    ///</summary>
    public static readonly Property tileSize = new Property(new Uri("http://semiodesk.com/ontologies/2012/svm#tileSize"));    

    ///<summary>
    ///(An interface which provides preview functionality for a set of related resources., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#PreviewColumn"/>
    ///</summary>
    public static readonly Class PreviewColumn = new Class(new Uri("http://semiodesk.com/ontologies/2012/svm#PreviewColumn"));    

    ///<summary>
    ///(The RDF type for which this control is intended., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/svm#targetType"/>
    ///</summary>
    public static readonly Property targetType = new Property(new Uri("http://semiodesk.com/ontologies/2012/svm#targetType"));
}

///<summary>
///Semiodesk Framework Ontology
///The Semiodesk Ontology contains classes and properties necessary for the Semiodesk Framework
///</summary>
public class sfo : Ontology
{
    public static readonly Uri Namespace = new Uri("http://semiodesk.com/ontologies/2012/sfo#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "sfo";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///(A property denoting that the given type has a specific representation and can be grouped together with elements ., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#belongsToGroupRepresentation"/>
    ///</summary>
    public static readonly Property belongsToGroupRepresentation = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#belongsToGroupRepresentation"));    

    ///<summary>
    ///(A group which contains represenations of events., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#EventGroup"/>
    ///</summary>
    public static readonly Resource EventGroup = new Resource(new Uri("http://semiodesk.com/ontologies/2012/sfo#EventGroup"));    

    ///<summary>
    ///A DataObject which is stored in the semiodesk preview manager database.
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#PreviewDataObject"/>
    ///</summary>
    public static readonly Class PreviewDataObject = new Class(new Uri("http://semiodesk.com/ontologies/2012/sfo#PreviewDataObject"));    

    ///<summary>
    ///(A group which contains represenations of documents., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#DocumentsGroup"/>
    ///</summary>
    public static readonly Resource DocumentsGroup = new Resource(new Uri("http://semiodesk.com/ontologies/2012/sfo#DocumentsGroup"));    

    ///<summary>
    ///(The name of a representation group and can be used for graphical user interfaces., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#groupName"/>
    ///</summary>
    public static readonly Property groupName = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#groupName"));    

    ///<summary>
    ///All classes which are belong to one representation.
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#groupRepresentationFor"/>
    ///</summary>
    public static readonly Property groupRepresentationFor = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#groupRepresentationFor"));    

    ///<summary>
    ///(A group which contains represenations of tasks., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#TasksGroup"/>
    ///</summary>
    public static readonly Resource TasksGroup = new Resource(new Uri("http://semiodesk.com/ontologies/2012/sfo#TasksGroup"));    

    ///<summary>
    ///Google contact releationship as retrieved from google api.
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#hasGoogleContactsRelationships"/>
    ///</summary>
    public static readonly Property hasGoogleContactsRelationships = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#hasGoogleContactsRelationships"));    

    ///<summary>
    ///Google contact medium primary attribute.
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#isGooglePrimary"/>
    ///</summary>
    public static readonly Property isGooglePrimary = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#isGooglePrimary"));    

    ///<summary>
    ///This resource indicates that an item has a connection to a container. This can be used to determine if an element has been moved.
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#belongsToGoogleContainer"/>
    ///</summary>
    public static readonly Property belongsToGoogleContainer = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#belongsToGoogleContainer"));    

    ///<summary>
    ///(A Group which contains representations of media., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#MediaGroup"/>
    ///</summary>
    public static readonly Resource MediaGroup = new Resource(new Uri("http://semiodesk.com/ontologies/2012/sfo#MediaGroup"));    

    ///<summary>
    ///(Terms which capture the essence of resource. Should be included in all searches., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#subjectHeading"/>
    ///</summary>
    public static readonly Property subjectHeading = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#subjectHeading"));    

    ///<summary>
    ///(A Group which contains representations of notes., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#NotesGroup"/>
    ///</summary>
    public static readonly Resource NotesGroup = new Resource(new Uri("http://semiodesk.com/ontologies/2012/sfo#NotesGroup"));    

    ///<summary>
    ///(A group which contains represenations of file system elements., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#FilesGroup"/>
    ///</summary>
    public static readonly Resource FilesGroup = new Resource(new Uri("http://semiodesk.com/ontologies/2012/sfo#FilesGroup"));    

    ///<summary>
    ///Key of the item in the PreviewManager database.
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#key"/>
    ///</summary>
    public static readonly Property key = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#key"));    

    ///<summary>
    ///(Instances of this class are repesenations groups which mark class types to be grouped together in the Semiodesk Framework., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#GroupRepresentation"/>
    ///</summary>
    public static readonly Class GroupRepresentation = new Class(new Uri("http://semiodesk.com/ontologies/2012/sfo#GroupRepresentation"));    

    ///<summary>
    ///(The base property for all relation properties., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#isRelated"/>
    ///</summary>
    public static readonly Property isRelated = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#isRelated"));    

    ///<summary>
    ///(A Group which contains representations of people., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#PeopleGroup"/>
    ///</summary>
    public static readonly Resource PeopleGroup = new Resource(new Uri("http://semiodesk.com/ontologies/2012/sfo#PeopleGroup"));    

    ///<summary>
    ///(A date or time value., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#endTime"/>
    ///</summary>
    public static readonly Property endTime = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#endTime"));    

    ///<summary>
    ///</summary>
    public static readonly Property upcomingVisible = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#upcomingVisible"));    

    ///<summary>
    ///(A date or time value to be evalutated by the upcoming activity list., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#upcomingDateTime"/>
    ///</summary>
    public static readonly Property upcomingDateTime = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#upcomingDateTime"));    

    ///<summary>
    ///(Descriptive text about a resource. Should be evaluted for full text searches., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#descriptor"/>
    ///</summary>
    public static readonly Property descriptor = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#descriptor"));    

    ///<summary>
    ///(A date or time value., en)
    ///<see cref="http://semiodesk.com/ontologies/2012/sfo#startTime"/>
    ///</summary>
    public static readonly Property startTime = new Property(new Uri("http://semiodesk.com/ontologies/2012/sfo#startTime"));
}

///<summary>
///Semiodesk Calendar Ontology
///The Semiodesk Calendar Ontology contains classes and properties for organising calendars and events.
///</summary>
public class sco : Ontology
{
    public static readonly Uri Namespace = new Uri("http://semiodesk.com/ontologies/2013/sco#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "sco";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///A period of time where the calendar is blocked for events.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#FreeBusy"/>
    ///</summary>
    public static readonly Class FreeBusy = new Class(new Uri("http://semiodesk.com/ontologies/2013/sco#FreeBusy"));    

    ///<summary>
    ///A list or register of events (appointments or social events or court cases etc)
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#Calendar"/>
    ///</summary>
    public static readonly Class Calendar = new Class(new Uri("http://semiodesk.com/ontologies/2013/sco#Calendar"));    

    ///<summary>
    ///An element which is part of a calendar. Superclass of more distinct elements of a calendar.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#CalendarItem"/>
    ///</summary>
    public static readonly Class CalendarItem = new Class(new Uri("http://semiodesk.com/ontologies/2013/sco#CalendarItem"));    

    ///<summary>
    ///Something that happens at a given place and time.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#Event"/>
    ///</summary>
    public static readonly Class Event = new Class(new Uri("http://semiodesk.com/ontologies/2013/sco#Event"));    

    ///<summary>
    ///Relation which denotes that a calendar has a calendar item.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#hasCalendarItem"/>
    ///</summary>
    public static readonly Property hasCalendarItem = new Property(new Uri("http://semiodesk.com/ontologies/2013/sco#hasCalendarItem"));    

    ///<summary>
    ///Location of a Calendar item.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#Location"/>
    ///</summary>
    public static readonly Property Location = new Property(new Uri("http://semiodesk.com/ontologies/2013/sco#Location"));    

    ///<summary>
    ///A person who is present and participates in a meeting.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#Attendee"/>
    ///</summary>
    public static readonly Class Attendee = new Class(new Uri("http://semiodesk.com/ontologies/2013/sco#Attendee"));    

    ///<summary>
    ///Relation which denotes that a calendar item belongs to a calendar.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#belongsToCalendar"/>
    ///</summary>
    public static readonly Property belongsToCalendar = new Property(new Uri("http://semiodesk.com/ontologies/2013/sco#belongsToCalendar"));    

    ///<summary>
    ///Begin of a calendar item.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#StartTime"/>
    ///</summary>
    public static readonly Property StartTime = new Property(new Uri("http://semiodesk.com/ontologies/2013/sco#StartTime"));    

    ///<summary>
    ///End of a calendar item
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#EndTime"/>
    ///</summary>
    public static readonly Property EndTime = new Property(new Uri("http://semiodesk.com/ontologies/2013/sco#EndTime"));    

    ///<summary>
    ///Denotes if the calendar item is reserved for the whole day.
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#isWholeDay"/>
    ///</summary>
    public static readonly Property isWholeDay = new Property(new Uri("http://semiodesk.com/ontologies/2013/sco#isWholeDay"));    

    ///<summary>
    ///DataObject attached to a calendar item
    ///<see cref="http://semiodesk.com/ontologies/2013/sco#attachment"/>
    ///</summary>
    public static readonly Property attachment = new Property(new Uri("http://semiodesk.com/ontologies/2013/sco#attachment"));
}

///<summary>
///Semiodesk Web Connector Ontology
///The Semiodesk Web Connector Ontology contains classes and properties necessary for the Semiodesk Ubiquity Synchronization Service.
///</summary>
public class swco : Ontology
{
    public static readonly Uri Namespace = new Uri("http://semiodesk.com/ontologies/2013/swco#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "swco";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///(The rdf types this connector creates in its model. The connector has to deliver the given types, but it is not restricted to them, en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#suppliesTypes"/>
    ///</summary>
    public static readonly Property suppliesTypes = new Property(new Uri("http://semiodesk.com/ontologies/2013/swco#suppliesTypes"));    

    ///<summary>
    ///(The assembly of the connector., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#hasAssemblyName"/>
    ///</summary>
    public static readonly Property hasAssemblyName = new Property(new Uri("http://semiodesk.com/ontologies/2013/swco#hasAssemblyName"));    

    ///<summary>
    ///(The username used to access the model of the connector., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#username"/>
    ///</summary>
    public static readonly Property username = new Property(new Uri("http://semiodesk.com/ontologies/2013/swco#username"));    

    ///<summary>
    ///(A connector to a web service which is stored in a C# assembly., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#Connector"/>
    ///</summary>
    public static readonly Class Connector = new Class(new Uri("http://semiodesk.com/ontologies/2013/swco#Connector"));    

    ///<summary>
    ///(Determines if the connector should be used or if it is disabled., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#isEnabled"/>
    ///</summary>
    public static readonly Property isEnabled = new Property(new Uri("http://semiodesk.com/ontologies/2013/swco#isEnabled"));    

    ///<summary>
    ///(The password used to access the model of the connector., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#password"/>
    ///</summary>
    public static readonly Property password = new Property(new Uri("http://semiodesk.com/ontologies/2013/swco#password"));    

    ///<summary>
    ///(The separated model of the connector., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#Model"/>
    ///</summary>
    public static readonly Class Model = new Class(new Uri("http://semiodesk.com/ontologies/2013/swco#Model"));    

    ///<summary>
    ///(The model where the connector stores it's metadata., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#hasModel"/>
    ///</summary>
    public static readonly Property hasModel = new Property(new Uri("http://semiodesk.com/ontologies/2013/swco#hasModel"));    

    ///<summary>
    ///(The version of the connector., en)
    ///<see cref="http://semiodesk.com/ontologies/2013/swco#hasVersion"/>
    ///</summary>
    public static readonly Property hasVersion = new Property(new Uri("http://semiodesk.com/ontologies/2013/swco#hasVersion"));
}

///<summary>
///
///
///</summary>
public class wgs84 : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "wgs84";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///
    ///Recent changes to this namespace:
    ///$Log: wgs84_pos.rdf,v $
    ///Revision 1.22  2009/04/20 15:00:30  timbl
    ///Remove the time bits which have been deal with elsewhere eg in iCal.
    ///
    ///Revision 1.21  2009/04/20 12:52:47  timbl
    ///try again
    ///
    ///Revision 1.20  2009/04/20 12:42:11  timbl
    ///Add Event (edited ages ago and never checked in), and location (following discussion http://chatlogs.planetrdf.com/swig/2009-04-20#T12-36-09)
    ///
    ///Revision 1.19  2009/04/20 12:36:31  timbl
    ///Add Event (edited ages ago and never checked in), and location (following discussion http://chatlogs.planetrdf.com/swig/2009-04-20#T12-36-09)
    ///
    ///Revision 1.18  2006/02/01 22:01:04  danbri
    ///Clarified that lat and long are decimal degrees, and that alt is decimal metres about local reference ellipsoid
    ///
    ///Revision 1.17  2004/02/06 17:38:12  danbri
    ///Fixed a bad commit screwup
    ///
    ///Revision 1.15  2003/04/19 11:24:08  danbri
    ///Fixed the typo even more.
    ///
    ///Revision 1.14  2003/04/19 11:16:56  danbri
    ///fixed a typo
    ///
    ///Revision 1.13  2003/02/19 22:27:27  connolly
    ///relaxed domain constraints on lat/long/alt from Point to SpatialThing
    ///
    ///Revision 1.12  2003/01/12 01:41:41  danbri
    ///Trying local copy of XSLT doc.
    ///
    ///Revision 1.11  2003/01/12 01:20:18  danbri
    ///added a link to morten's xslt rdfs viewer.
    ///
    ///Revision 1.10  2003/01/11 18:56:49  danbri
    ///Removed datatype range from lat and long properties, since they would
    ///have required each occurance of the property to mention the datatype.
    ///
    ///Revision 1.9  2003/01/11 11:41:31  danbri
    ///Another typo; repaired rdfs:Property to rdf:Property x4
    ///
    ///Revision 1.8  2003/01/11 11:05:02  danbri
    ///Added an rdfs:range for each lat/long/alt property,
    ///http://www.w3.org/2001/XMLSchema#float
    ///
    ///Revision 1.7  2003/01/10 20:25:16  danbri
    ///Longer rdfs:comment for Point, trying to be Earth-centric and neutral about
    ///coordinate system(s) at the same time. Feedback welcomed.
    ///
    ///Revision 1.6  2003/01/10 20:18:30  danbri
    ///Added CVS log comments into the RDF/XML as an rdfs:comment property of the
    ///vocabulary. Note that this is not common practice (but seems both harmless
    ///and potentially useful).
    ///
    ///
    ///revision 1.5
    ///date: 2003/01/10 20:14:31;  author: danbri;  state: Exp;  lines: +16 -5
    ///Updated schema:
    ///Added a dc:date, added url for more info. Changed the rdfs:label of the
    ///namespace from gp to geo. Added a class Point, set as the rdfs:domain of
    ///each property. Added XML comment on the lat_long property suggesting that
    ///we might not need it (based on #rdfig commentary from implementors).
    ///
    ///revision 1.4
    ///date: 2003/01/10 20:01:07;  author: danbri;  state: Exp;  lines: +6 -5
    ///Fixed typo; several rdfs:about attributes are now rdf:about. Thanks to MortenF in
    ///#rdfig for catching this error.
    ///
    ///revision 1.3
    ///date: 2003/01/10 11:59:03;  author: danbri;  state: Exp;  lines: +4 -3
    ///fixed buglet in vocab, added more wgs links
    ///
    ///revision 1.2
    ///date: 2003/01/10 11:01:11;  author: danbri;  state: Exp;  lines: +4 -4
    ///Removed alt from the as-a-flat-string property, and switched from
    ///space separated to comma separated.
    ///
    ///revision 1.1
    ///date: 2003/01/10 10:53:23;  author: danbri;  state: Exp;
    ///basic geo vocab
    ///
    ///
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#"/>
    ///</summary>
    public static readonly Resource wgs84_pos = new Resource(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#"));    

    ///<summary>
    ///A point, typically described using a coordinate system relative to Earth, such as WGS84.
    ///  
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#Point"/>
    ///</summary>
    public static readonly Class Point = new Class(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#Point"));    

    ///<summary>
    ///The WGS84 latitude of a SpatialThing (decimal degrees).
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#lat"/>
    ///</summary>
    public static readonly Property lat = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#lat"));    

    ///<summary>
    ///The WGS84 altitude of a SpatialThing (decimal meters 
    ///above the local reference ellipsoid).
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#alt"/>
    ///</summary>
    public static readonly Property alt = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#alt"));    

    ///<summary>
    ///The relation between something and the point, 
    /// or other geometrical thing in space, where it is.  For example, the realtionship between
    /// a radio tower and a Point with a given lat and long.
    /// Or a relationship between a park and its outline as a closed arc of points, or a road and
    /// its location as a arc (a sequence of points).
    /// Clearly in practice there will be limit to the accuracy of any such statement, but one would expect
    /// an accuracy appropriate for the size of the object and uses such as mapping .
    /// 
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#location"/>
    ///</summary>
    public static readonly Property location = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#location"));    

    ///<summary>
    ///A comma-separated representation of a latitude, longitude coordinate.
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#lat_long"/>
    ///</summary>
    public static readonly Property lat_long = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#lat_long"));    

    ///<summary>
    ///The WGS84 longitude of a SpatialThing (decimal degrees).
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#long"/>
    ///</summary>
    public static readonly Property _long = new Property(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#long"));    

    ///<summary>
    ///Anything with spatial extent, i.e. size, shape, or position.
    /// e.g. people, places, bowling balls, as well as abstract areas like cubes.
    ///
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#SpatialThing"/>
    ///</summary>
    public static readonly Class SpatialThing = new Class(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#SpatialThing"));
}

public class vcard : Ontology
{
    public static readonly Uri Namespace = new Uri("http://www.w3.org/2001/vcard-rdf/3.0#");
    public static Uri GetNamespace() { return Namespace; }

    public static readonly string Prefix = "vcard";
    public static string GetPrefix() { return Prefix; }

    public static readonly Property N = new Property(new Uri("http://www.w3.org/2001/vcard-rdf/3.0#N"));

    public static readonly Property givenName = new Property(new Uri("http://www.w3.org/2001/vcard-rdf/3.0#givenName"));
}

}