
using System;
using System.Collections.Generic;
using System.Text;
using Semiodesk.Trinity;

namespace netstandard20
{
	
///<summary>
///Friend of a Friend (FOAF) vocabulary
///
///</summary>
public class foaf : Ontology
{
    public static readonly Uri Namespace = new Uri("http://xmlns.com/foaf/0.1/");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "foaf";
    public static string GetPrefix() { return Prefix; }     

    ///<summary>
    ///
    ///<see cref="http://xmlns.com/foaf/0.1/"/>
    ///</summary>
    public static readonly Resource _0_1 = new Resource(new Uri("http://xmlns.com/foaf/0.1/"));    

    ///<summary>
    ///
    ///<see cref="http://xmlns.com/wot/0.1/assurance"/>
    ///</summary>
    public static readonly Property assurance = new Property(new Uri("http://xmlns.com/wot/0.1/assurance"));    

    ///<summary>
    ///
    ///<see cref="http://xmlns.com/wot/0.1/src_assurance"/>
    ///</summary>
    public static readonly Property src_assurance = new Property(new Uri("http://xmlns.com/wot/0.1/src_assurance"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2003/06/sw-vocab-status/ns#term_status"/>
    ///</summary>
    public static readonly Property term_status = new Property(new Uri("http://www.w3.org/2003/06/sw-vocab-status/ns#term_status"));    

    ///<summary>
    ///
    ///<see cref="http://purl.org/dc/elements/1.1/description"/>
    ///</summary>
    public static readonly Property description = new Property(new Uri("http://purl.org/dc/elements/1.1/description"));    

    ///<summary>
    ///
    ///<see cref="http://purl.org/dc/elements/1.1/title"/>
    ///</summary>
    public static readonly Property title = new Property(new Uri("http://purl.org/dc/elements/1.1/title"));    

    ///<summary>
    ///
    ///<see cref="http://purl.org/dc/elements/1.1/date"/>
    ///</summary>
    public static readonly Property date = new Property(new Uri("http://purl.org/dc/elements/1.1/date"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2000/01/rdf-schema#Class"/>
    ///</summary>
    public static readonly Class Class = new Class(new Uri("http://www.w3.org/2000/01/rdf-schema#Class"));    

    ///<summary>
    ///A foaf:LabelProperty is any RDF property with texual values that serve as labels.
    ///<see cref="http://xmlns.com/foaf/0.1/LabelProperty"/>
    ///</summary>
    public static readonly Class LabelProperty = new Class(new Uri("http://xmlns.com/foaf/0.1/LabelProperty"));    

    ///<summary>
    ///A person.
    ///<see cref="http://xmlns.com/foaf/0.1/Person"/>
    ///</summary>
    public static readonly Class Person = new Class(new Uri("http://xmlns.com/foaf/0.1/Person"));    

    ///<summary>
    ///An agent (eg. person, group, software or physical artifact).
    ///<see cref="http://xmlns.com/foaf/0.1/Agent"/>
    ///</summary>
    public static readonly Class Agent = new Class(new Uri("http://xmlns.com/foaf/0.1/Agent"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#SpatialThing"/>
    ///</summary>
    public static readonly Class SpatialThing = new Class(new Uri("http://www.w3.org/2003/01/geo/wgs84_pos#SpatialThing"));    

    ///<summary>
    ///A document.
    ///<see cref="http://xmlns.com/foaf/0.1/Document"/>
    ///</summary>
    public static readonly Class Document = new Class(new Uri("http://xmlns.com/foaf/0.1/Document"));    

    ///<summary>
    ///An organization.
    ///<see cref="http://xmlns.com/foaf/0.1/Organization"/>
    ///</summary>
    public static readonly Class Organization = new Class(new Uri("http://xmlns.com/foaf/0.1/Organization"));    

    ///<summary>
    ///A class of Agents.
    ///<see cref="http://xmlns.com/foaf/0.1/Group"/>
    ///</summary>
    public static readonly Class Group = new Class(new Uri("http://xmlns.com/foaf/0.1/Group"));    

    ///<summary>
    ///A project (a collective endeavour of some kind).
    ///<see cref="http://xmlns.com/foaf/0.1/Project"/>
    ///</summary>
    public static readonly Class Project = new Class(new Uri("http://xmlns.com/foaf/0.1/Project"));    

    ///<summary>
    ///An image.
    ///<see cref="http://xmlns.com/foaf/0.1/Image"/>
    ///</summary>
    public static readonly Class Image = new Class(new Uri("http://xmlns.com/foaf/0.1/Image"));    

    ///<summary>
    ///A personal profile RDF document.
    ///<see cref="http://xmlns.com/foaf/0.1/PersonalProfileDocument"/>
    ///</summary>
    public static readonly Class PersonalProfileDocument = new Class(new Uri("http://xmlns.com/foaf/0.1/PersonalProfileDocument"));    

    ///<summary>
    ///An online account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineAccount"/>
    ///</summary>
    public static readonly Class OnlineAccount = new Class(new Uri("http://xmlns.com/foaf/0.1/OnlineAccount"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2002/07/owl#Thing"/>
    ///</summary>
    public static readonly Resource Thing = new Resource(new Uri("http://www.w3.org/2002/07/owl#Thing"));    

    ///<summary>
    ///An online gaming account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineGamingAccount"/>
    ///</summary>
    public static readonly Class OnlineGamingAccount = new Class(new Uri("http://xmlns.com/foaf/0.1/OnlineGamingAccount"));    

    ///<summary>
    ///An online e-commerce account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineEcommerceAccount"/>
    ///</summary>
    public static readonly Class OnlineEcommerceAccount = new Class(new Uri("http://xmlns.com/foaf/0.1/OnlineEcommerceAccount"));    

    ///<summary>
    ///An online chat account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineChatAccount"/>
    ///</summary>
    public static readonly Class OnlineChatAccount = new Class(new Uri("http://xmlns.com/foaf/0.1/OnlineChatAccount"));    

    ///<summary>
    ///A  personal mailbox, ie. an Internet mailbox associated with exactly one owner, the first owner of this mailbox. This is a 'static inverse functional property', in that  there is (across time and change) at most one individual that ever has any particular value for foaf:mbox.
    ///<see cref="http://xmlns.com/foaf/0.1/mbox"/>
    ///</summary>
    public static readonly Property mbox = new Property(new Uri("http://xmlns.com/foaf/0.1/mbox"));    

    ///<summary>
    ///The sha1sum of the URI of an Internet mailbox associated with exactly one owner, the  first owner of the mailbox.
    ///<see cref="http://xmlns.com/foaf/0.1/mbox_sha1sum"/>
    ///</summary>
    public static readonly Property mbox_sha1sum = new Property(new Uri("http://xmlns.com/foaf/0.1/mbox_sha1sum"));    

    ///<summary>
    ///The gender of this Agent (typically but not necessarily 'male' or 'female').
    ///<see cref="http://xmlns.com/foaf/0.1/gender"/>
    ///</summary>
    public static readonly Property gender = new Property(new Uri("http://xmlns.com/foaf/0.1/gender"));    

    ///<summary>
    ///A textual geekcode for this person, see http://www.geekcode.com/geek.html
    ///<see cref="http://xmlns.com/foaf/0.1/geekcode"/>
    ///</summary>
    public static readonly Property geekcode = new Property(new Uri("http://xmlns.com/foaf/0.1/geekcode"));    

    ///<summary>
    ///A checksum for the DNA of some thing. Joke.
    ///<see cref="http://xmlns.com/foaf/0.1/dnaChecksum"/>
    ///</summary>
    public static readonly Property dnaChecksum = new Property(new Uri("http://xmlns.com/foaf/0.1/dnaChecksum"));    

    ///<summary>
    ///A sha1sum hash, in hex.
    ///<see cref="http://xmlns.com/foaf/0.1/sha1"/>
    ///</summary>
    public static readonly Property sha1 = new Property(new Uri("http://xmlns.com/foaf/0.1/sha1"));    

    ///<summary>
    ///A location that something is based near, for some broadly human notion of near.
    ///<see cref="http://xmlns.com/foaf/0.1/based_near"/>
    ///</summary>
    public static readonly Property based_near = new Property(new Uri("http://xmlns.com/foaf/0.1/based_near"));    

    ///<summary>
    ///Title (Mr, Mrs, Ms, Dr. etc)
    ///<see cref="http://xmlns.com/foaf/0.1/title"/>
    ///</summary>
    public static readonly Property title_0 = new Property(new Uri("http://xmlns.com/foaf/0.1/title"));    

    ///<summary>
    ///A short informal nickname characterising an agent (includes login identifiers, IRC and other chat nicknames).
    ///<see cref="http://xmlns.com/foaf/0.1/nick"/>
    ///</summary>
    public static readonly Property nick = new Property(new Uri("http://xmlns.com/foaf/0.1/nick"));    

    ///<summary>
    ///A jabber ID for something.
    ///<see cref="http://xmlns.com/foaf/0.1/jabberID"/>
    ///</summary>
    public static readonly Property jabberID = new Property(new Uri("http://xmlns.com/foaf/0.1/jabberID"));    

    ///<summary>
    ///An AIM chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/aimChatID"/>
    ///</summary>
    public static readonly Property aimChatID = new Property(new Uri("http://xmlns.com/foaf/0.1/aimChatID"));    

    ///<summary>
    ///A Skype ID
    ///<see cref="http://xmlns.com/foaf/0.1/skypeID"/>
    ///</summary>
    public static readonly Property skypeID = new Property(new Uri("http://xmlns.com/foaf/0.1/skypeID"));    

    ///<summary>
    ///An ICQ chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/icqChatID"/>
    ///</summary>
    public static readonly Property icqChatID = new Property(new Uri("http://xmlns.com/foaf/0.1/icqChatID"));    

    ///<summary>
    ///A Yahoo chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/yahooChatID"/>
    ///</summary>
    public static readonly Property yahooChatID = new Property(new Uri("http://xmlns.com/foaf/0.1/yahooChatID"));    

    ///<summary>
    ///An MSN chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/msnChatID"/>
    ///</summary>
    public static readonly Property msnChatID = new Property(new Uri("http://xmlns.com/foaf/0.1/msnChatID"));    

    ///<summary>
    ///A name for some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/name"/>
    ///</summary>
    public static readonly Property name = new Property(new Uri("http://xmlns.com/foaf/0.1/name"));    

    ///<summary>
    ///The first name of a person.
    ///<see cref="http://xmlns.com/foaf/0.1/firstName"/>
    ///</summary>
    public static readonly Property firstName = new Property(new Uri("http://xmlns.com/foaf/0.1/firstName"));    

    ///<summary>
    ///The last name of a person.
    ///<see cref="http://xmlns.com/foaf/0.1/lastName"/>
    ///</summary>
    public static readonly Property lastName = new Property(new Uri("http://xmlns.com/foaf/0.1/lastName"));    

    ///<summary>
    ///The given name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/givenName"/>
    ///</summary>
    public static readonly Property givenName = new Property(new Uri("http://xmlns.com/foaf/0.1/givenName"));    

    ///<summary>
    ///The given name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/givenname"/>
    ///</summary>
    public static readonly Property givenname = new Property(new Uri("http://xmlns.com/foaf/0.1/givenname"));    

    ///<summary>
    ///The surname of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/surname"/>
    ///</summary>
    public static readonly Property surname = new Property(new Uri("http://xmlns.com/foaf/0.1/surname"));    

    ///<summary>
    ///The family name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/family_name"/>
    ///</summary>
    public static readonly Property family_name = new Property(new Uri("http://xmlns.com/foaf/0.1/family_name"));    

    ///<summary>
    ///The family name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/familyName"/>
    ///</summary>
    public static readonly Property familyName = new Property(new Uri("http://xmlns.com/foaf/0.1/familyName"));    

    ///<summary>
    ///A phone,  specified using fully qualified tel: URI scheme (refs: http://www.w3.org/Addressing/schemes.html#tel).
    ///<see cref="http://xmlns.com/foaf/0.1/phone"/>
    ///</summary>
    public static readonly Property phone = new Property(new Uri("http://xmlns.com/foaf/0.1/phone"));    

    ///<summary>
    ///A homepage for some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/homepage"/>
    ///</summary>
    public static readonly Property homepage = new Property(new Uri("http://xmlns.com/foaf/0.1/homepage"));    

    ///<summary>
    ///A weblog of some thing (whether person, group, company etc.).
    ///<see cref="http://xmlns.com/foaf/0.1/weblog"/>
    ///</summary>
    public static readonly Property weblog = new Property(new Uri("http://xmlns.com/foaf/0.1/weblog"));    

    ///<summary>
    ///An OpenID for an Agent.
    ///<see cref="http://xmlns.com/foaf/0.1/openid"/>
    ///</summary>
    public static readonly Property openid = new Property(new Uri("http://xmlns.com/foaf/0.1/openid"));    

    ///<summary>
    ///A tipjar document for this agent, describing means for payment and reward.
    ///<see cref="http://xmlns.com/foaf/0.1/tipjar"/>
    ///</summary>
    public static readonly Property tipjar = new Property(new Uri("http://xmlns.com/foaf/0.1/tipjar"));    

    ///<summary>
    ///A .plan comment, in the tradition of finger and '.plan' files.
    ///<see cref="http://xmlns.com/foaf/0.1/plan"/>
    ///</summary>
    public static readonly Property plan = new Property(new Uri("http://xmlns.com/foaf/0.1/plan"));    

    ///<summary>
    ///Something that was made by this agent.
    ///<see cref="http://xmlns.com/foaf/0.1/made"/>
    ///</summary>
    public static readonly Property made = new Property(new Uri("http://xmlns.com/foaf/0.1/made"));    

    ///<summary>
    ///An agent that  made this thing.
    ///<see cref="http://xmlns.com/foaf/0.1/maker"/>
    ///</summary>
    public static readonly Property maker = new Property(new Uri("http://xmlns.com/foaf/0.1/maker"));    

    ///<summary>
    ///An image that can be used to represent some thing (ie. those depictions which are particularly representative of something, eg. one's photo on a homepage).
    ///<see cref="http://xmlns.com/foaf/0.1/img"/>
    ///</summary>
    public static readonly Property img = new Property(new Uri("http://xmlns.com/foaf/0.1/img"));    

    ///<summary>
    ///A depiction of some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/depiction"/>
    ///</summary>
    public static readonly Property depiction = new Property(new Uri("http://xmlns.com/foaf/0.1/depiction"));    

    ///<summary>
    ///A thing depicted in this representation.
    ///<see cref="http://xmlns.com/foaf/0.1/depicts"/>
    ///</summary>
    public static readonly Property depicts = new Property(new Uri("http://xmlns.com/foaf/0.1/depicts"));    

    ///<summary>
    ///A derived thumbnail image.
    ///<see cref="http://xmlns.com/foaf/0.1/thumbnail"/>
    ///</summary>
    public static readonly Property thumbnail = new Property(new Uri("http://xmlns.com/foaf/0.1/thumbnail"));    

    ///<summary>
    ///A Myers Briggs (MBTI) personality classification.
    ///<see cref="http://xmlns.com/foaf/0.1/myersBriggs"/>
    ///</summary>
    public static readonly Property myersBriggs = new Property(new Uri("http://xmlns.com/foaf/0.1/myersBriggs"));    

    ///<summary>
    ///A workplace homepage of some person; the homepage of an organization they work for.
    ///<see cref="http://xmlns.com/foaf/0.1/workplaceHomepage"/>
    ///</summary>
    public static readonly Property workplaceHomepage = new Property(new Uri("http://xmlns.com/foaf/0.1/workplaceHomepage"));    

    ///<summary>
    ///A work info homepage of some person; a page about their work for some organization.
    ///<see cref="http://xmlns.com/foaf/0.1/workInfoHomepage"/>
    ///</summary>
    public static readonly Property workInfoHomepage = new Property(new Uri("http://xmlns.com/foaf/0.1/workInfoHomepage"));    

    ///<summary>
    ///A homepage of a school attended by the person.
    ///<see cref="http://xmlns.com/foaf/0.1/schoolHomepage"/>
    ///</summary>
    public static readonly Property schoolHomepage = new Property(new Uri("http://xmlns.com/foaf/0.1/schoolHomepage"));    

    ///<summary>
    ///A person known by this person (indicating some level of reciprocated interaction between the parties).
    ///<see cref="http://xmlns.com/foaf/0.1/knows"/>
    ///</summary>
    public static readonly Property knows = new Property(new Uri("http://xmlns.com/foaf/0.1/knows"));    

    ///<summary>
    ///A page about a topic of interest to this person.
    ///<see cref="http://xmlns.com/foaf/0.1/interest"/>
    ///</summary>
    public static readonly Property interest = new Property(new Uri("http://xmlns.com/foaf/0.1/interest"));    

    ///<summary>
    ///A thing of interest to this person.
    ///<see cref="http://xmlns.com/foaf/0.1/topic_interest"/>
    ///</summary>
    public static readonly Property topic_interest = new Property(new Uri("http://xmlns.com/foaf/0.1/topic_interest"));    

    ///<summary>
    ///A link to the publications of this person.
    ///<see cref="http://xmlns.com/foaf/0.1/publications"/>
    ///</summary>
    public static readonly Property publications = new Property(new Uri("http://xmlns.com/foaf/0.1/publications"));    

    ///<summary>
    ///A current project this person works on.
    ///<see cref="http://xmlns.com/foaf/0.1/currentProject"/>
    ///</summary>
    public static readonly Property currentProject = new Property(new Uri("http://xmlns.com/foaf/0.1/currentProject"));    

    ///<summary>
    ///A project this person has previously worked on.
    ///<see cref="http://xmlns.com/foaf/0.1/pastProject"/>
    ///</summary>
    public static readonly Property pastProject = new Property(new Uri("http://xmlns.com/foaf/0.1/pastProject"));    

    ///<summary>
    ///An organization funding a project or person.
    ///<see cref="http://xmlns.com/foaf/0.1/fundedBy"/>
    ///</summary>
    public static readonly Property fundedBy = new Property(new Uri("http://xmlns.com/foaf/0.1/fundedBy"));    

    ///<summary>
    ///A logo representing some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/logo"/>
    ///</summary>
    public static readonly Property logo = new Property(new Uri("http://xmlns.com/foaf/0.1/logo"));    

    ///<summary>
    ///A topic of some page or document.
    ///<see cref="http://xmlns.com/foaf/0.1/topic"/>
    ///</summary>
    public static readonly Property topic = new Property(new Uri("http://xmlns.com/foaf/0.1/topic"));    

    ///<summary>
    ///The primary topic of some page or document.
    ///<see cref="http://xmlns.com/foaf/0.1/primaryTopic"/>
    ///</summary>
    public static readonly Property primaryTopic = new Property(new Uri("http://xmlns.com/foaf/0.1/primaryTopic"));    

    ///<summary>
    ///The underlying or 'focal' entity associated with some SKOS-described concept.
    ///<see cref="http://xmlns.com/foaf/0.1/focus"/>
    ///</summary>
    public static readonly Property focus = new Property(new Uri("http://xmlns.com/foaf/0.1/focus"));    

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2004/02/skos/core#Concept"/>
    ///</summary>
    public static readonly Resource Concept = new Resource(new Uri("http://www.w3.org/2004/02/skos/core#Concept"));    

    ///<summary>
    ///A document that this thing is the primary topic of.
    ///<see cref="http://xmlns.com/foaf/0.1/isPrimaryTopicOf"/>
    ///</summary>
    public static readonly Property isPrimaryTopicOf = new Property(new Uri("http://xmlns.com/foaf/0.1/isPrimaryTopicOf"));    

    ///<summary>
    ///A page or document about this thing.
    ///<see cref="http://xmlns.com/foaf/0.1/page"/>
    ///</summary>
    public static readonly Property page = new Property(new Uri("http://xmlns.com/foaf/0.1/page"));    

    ///<summary>
    ///A theme.
    ///<see cref="http://xmlns.com/foaf/0.1/theme"/>
    ///</summary>
    public static readonly Property theme = new Property(new Uri("http://xmlns.com/foaf/0.1/theme"));    

    ///<summary>
    ///Indicates an account held by this agent.
    ///<see cref="http://xmlns.com/foaf/0.1/account"/>
    ///</summary>
    public static readonly Property account = new Property(new Uri("http://xmlns.com/foaf/0.1/account"));    

    ///<summary>
    ///Indicates an account held by this agent.
    ///<see cref="http://xmlns.com/foaf/0.1/holdsAccount"/>
    ///</summary>
    public static readonly Property holdsAccount = new Property(new Uri("http://xmlns.com/foaf/0.1/holdsAccount"));    

    ///<summary>
    ///Indicates a homepage of the service provide for this online account.
    ///<see cref="http://xmlns.com/foaf/0.1/accountServiceHomepage"/>
    ///</summary>
    public static readonly Property accountServiceHomepage = new Property(new Uri("http://xmlns.com/foaf/0.1/accountServiceHomepage"));    

    ///<summary>
    ///Indicates the name (identifier) associated with this online account.
    ///<see cref="http://xmlns.com/foaf/0.1/accountName"/>
    ///</summary>
    public static readonly Property accountName = new Property(new Uri("http://xmlns.com/foaf/0.1/accountName"));    

    ///<summary>
    ///Indicates a member of a Group
    ///<see cref="http://xmlns.com/foaf/0.1/member"/>
    ///</summary>
    public static readonly Property member = new Property(new Uri("http://xmlns.com/foaf/0.1/member"));    

    ///<summary>
    ///Indicates the class of individuals that are a member of a Group
    ///<see cref="http://xmlns.com/foaf/0.1/membershipClass"/>
    ///</summary>
    public static readonly Property membershipClass = new Property(new Uri("http://xmlns.com/foaf/0.1/membershipClass"));    

    ///<summary>
    ///The birthday of this Agent, represented in mm-dd string form, eg. '12-31'.
    ///<see cref="http://xmlns.com/foaf/0.1/birthday"/>
    ///</summary>
    public static readonly Property birthday = new Property(new Uri("http://xmlns.com/foaf/0.1/birthday"));    

    ///<summary>
    ///The age in years of some agent.
    ///<see cref="http://xmlns.com/foaf/0.1/age"/>
    ///</summary>
    public static readonly Property age = new Property(new Uri("http://xmlns.com/foaf/0.1/age"));    

    ///<summary>
    ///A string expressing what the user is happy for the general public (normally) to know about their current activity.
    ///<see cref="http://xmlns.com/foaf/0.1/status"/>
    ///</summary>
    public static readonly Property status = new Property(new Uri("http://xmlns.com/foaf/0.1/status"));
}
///<summary>
///Friend of a Friend (FOAF) vocabulary
///
///</summary>
public static class FOAF
{
    public static readonly Uri Namespace = new Uri("http://xmlns.com/foaf/0.1/");
    public static Uri GetNamespace() { return Namespace; }
    
    public static readonly string Prefix = "FOAF";
    public static string GetPrefix() { return Prefix; } 

    ///<summary>
    ///
    ///<see cref="http://xmlns.com/foaf/0.1/"/>
    ///</summary>
    public const string _0_1 = "http://xmlns.com/foaf/0.1/";

    ///<summary>
    ///
    ///<see cref="http://xmlns.com/wot/0.1/assurance"/>
    ///</summary>
    public const string assurance = "http://xmlns.com/wot/0.1/assurance";

    ///<summary>
    ///
    ///<see cref="http://xmlns.com/wot/0.1/src_assurance"/>
    ///</summary>
    public const string src_assurance = "http://xmlns.com/wot/0.1/src_assurance";

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2003/06/sw-vocab-status/ns#term_status"/>
    ///</summary>
    public const string term_status = "http://www.w3.org/2003/06/sw-vocab-status/ns#term_status";

    ///<summary>
    ///
    ///<see cref="http://purl.org/dc/elements/1.1/description"/>
    ///</summary>
    public const string description = "http://purl.org/dc/elements/1.1/description";

    ///<summary>
    ///
    ///<see cref="http://purl.org/dc/elements/1.1/title"/>
    ///</summary>
    public const string title = "http://purl.org/dc/elements/1.1/title";

    ///<summary>
    ///
    ///<see cref="http://purl.org/dc/elements/1.1/date"/>
    ///</summary>
    public const string date = "http://purl.org/dc/elements/1.1/date";

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2000/01/rdf-schema#Class"/>
    ///</summary>
    public const string Class = "http://www.w3.org/2000/01/rdf-schema#Class";

    ///<summary>
    ///A foaf:LabelProperty is any RDF property with texual values that serve as labels.
    ///<see cref="http://xmlns.com/foaf/0.1/LabelProperty"/>
    ///</summary>
    public const string LabelProperty = "http://xmlns.com/foaf/0.1/LabelProperty";

    ///<summary>
    ///A person.
    ///<see cref="http://xmlns.com/foaf/0.1/Person"/>
    ///</summary>
    public const string Person = "http://xmlns.com/foaf/0.1/Person";

    ///<summary>
    ///An agent (eg. person, group, software or physical artifact).
    ///<see cref="http://xmlns.com/foaf/0.1/Agent"/>
    ///</summary>
    public const string Agent = "http://xmlns.com/foaf/0.1/Agent";

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2003/01/geo/wgs84_pos#SpatialThing"/>
    ///</summary>
    public const string SpatialThing = "http://www.w3.org/2003/01/geo/wgs84_pos#SpatialThing";

    ///<summary>
    ///A document.
    ///<see cref="http://xmlns.com/foaf/0.1/Document"/>
    ///</summary>
    public const string Document = "http://xmlns.com/foaf/0.1/Document";

    ///<summary>
    ///An organization.
    ///<see cref="http://xmlns.com/foaf/0.1/Organization"/>
    ///</summary>
    public const string Organization = "http://xmlns.com/foaf/0.1/Organization";

    ///<summary>
    ///A class of Agents.
    ///<see cref="http://xmlns.com/foaf/0.1/Group"/>
    ///</summary>
    public const string Group = "http://xmlns.com/foaf/0.1/Group";

    ///<summary>
    ///A project (a collective endeavour of some kind).
    ///<see cref="http://xmlns.com/foaf/0.1/Project"/>
    ///</summary>
    public const string Project = "http://xmlns.com/foaf/0.1/Project";

    ///<summary>
    ///An image.
    ///<see cref="http://xmlns.com/foaf/0.1/Image"/>
    ///</summary>
    public const string Image = "http://xmlns.com/foaf/0.1/Image";

    ///<summary>
    ///A personal profile RDF document.
    ///<see cref="http://xmlns.com/foaf/0.1/PersonalProfileDocument"/>
    ///</summary>
    public const string PersonalProfileDocument = "http://xmlns.com/foaf/0.1/PersonalProfileDocument";

    ///<summary>
    ///An online account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineAccount"/>
    ///</summary>
    public const string OnlineAccount = "http://xmlns.com/foaf/0.1/OnlineAccount";

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2002/07/owl#Thing"/>
    ///</summary>
    public const string Thing = "http://www.w3.org/2002/07/owl#Thing";

    ///<summary>
    ///An online gaming account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineGamingAccount"/>
    ///</summary>
    public const string OnlineGamingAccount = "http://xmlns.com/foaf/0.1/OnlineGamingAccount";

    ///<summary>
    ///An online e-commerce account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineEcommerceAccount"/>
    ///</summary>
    public const string OnlineEcommerceAccount = "http://xmlns.com/foaf/0.1/OnlineEcommerceAccount";

    ///<summary>
    ///An online chat account.
    ///<see cref="http://xmlns.com/foaf/0.1/OnlineChatAccount"/>
    ///</summary>
    public const string OnlineChatAccount = "http://xmlns.com/foaf/0.1/OnlineChatAccount";

    ///<summary>
    ///A  personal mailbox, ie. an Internet mailbox associated with exactly one owner, the first owner of this mailbox. This is a 'static inverse functional property', in that  there is (across time and change) at most one individual that ever has any particular value for foaf:mbox.
    ///<see cref="http://xmlns.com/foaf/0.1/mbox"/>
    ///</summary>
    public const string mbox = "http://xmlns.com/foaf/0.1/mbox";

    ///<summary>
    ///The sha1sum of the URI of an Internet mailbox associated with exactly one owner, the  first owner of the mailbox.
    ///<see cref="http://xmlns.com/foaf/0.1/mbox_sha1sum"/>
    ///</summary>
    public const string mbox_sha1sum = "http://xmlns.com/foaf/0.1/mbox_sha1sum";

    ///<summary>
    ///The gender of this Agent (typically but not necessarily 'male' or 'female').
    ///<see cref="http://xmlns.com/foaf/0.1/gender"/>
    ///</summary>
    public const string gender = "http://xmlns.com/foaf/0.1/gender";

    ///<summary>
    ///A textual geekcode for this person, see http://www.geekcode.com/geek.html
    ///<see cref="http://xmlns.com/foaf/0.1/geekcode"/>
    ///</summary>
    public const string geekcode = "http://xmlns.com/foaf/0.1/geekcode";

    ///<summary>
    ///A checksum for the DNA of some thing. Joke.
    ///<see cref="http://xmlns.com/foaf/0.1/dnaChecksum"/>
    ///</summary>
    public const string dnaChecksum = "http://xmlns.com/foaf/0.1/dnaChecksum";

    ///<summary>
    ///A sha1sum hash, in hex.
    ///<see cref="http://xmlns.com/foaf/0.1/sha1"/>
    ///</summary>
    public const string sha1 = "http://xmlns.com/foaf/0.1/sha1";

    ///<summary>
    ///A location that something is based near, for some broadly human notion of near.
    ///<see cref="http://xmlns.com/foaf/0.1/based_near"/>
    ///</summary>
    public const string based_near = "http://xmlns.com/foaf/0.1/based_near";

    ///<summary>
    ///Title (Mr, Mrs, Ms, Dr. etc)
    ///<see cref="http://xmlns.com/foaf/0.1/title"/>
    ///</summary>
    public const string title_0 = "http://xmlns.com/foaf/0.1/title";

    ///<summary>
    ///A short informal nickname characterising an agent (includes login identifiers, IRC and other chat nicknames).
    ///<see cref="http://xmlns.com/foaf/0.1/nick"/>
    ///</summary>
    public const string nick = "http://xmlns.com/foaf/0.1/nick";

    ///<summary>
    ///A jabber ID for something.
    ///<see cref="http://xmlns.com/foaf/0.1/jabberID"/>
    ///</summary>
    public const string jabberID = "http://xmlns.com/foaf/0.1/jabberID";

    ///<summary>
    ///An AIM chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/aimChatID"/>
    ///</summary>
    public const string aimChatID = "http://xmlns.com/foaf/0.1/aimChatID";

    ///<summary>
    ///A Skype ID
    ///<see cref="http://xmlns.com/foaf/0.1/skypeID"/>
    ///</summary>
    public const string skypeID = "http://xmlns.com/foaf/0.1/skypeID";

    ///<summary>
    ///An ICQ chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/icqChatID"/>
    ///</summary>
    public const string icqChatID = "http://xmlns.com/foaf/0.1/icqChatID";

    ///<summary>
    ///A Yahoo chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/yahooChatID"/>
    ///</summary>
    public const string yahooChatID = "http://xmlns.com/foaf/0.1/yahooChatID";

    ///<summary>
    ///An MSN chat ID
    ///<see cref="http://xmlns.com/foaf/0.1/msnChatID"/>
    ///</summary>
    public const string msnChatID = "http://xmlns.com/foaf/0.1/msnChatID";

    ///<summary>
    ///A name for some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/name"/>
    ///</summary>
    public const string name = "http://xmlns.com/foaf/0.1/name";

    ///<summary>
    ///The first name of a person.
    ///<see cref="http://xmlns.com/foaf/0.1/firstName"/>
    ///</summary>
    public const string firstName = "http://xmlns.com/foaf/0.1/firstName";

    ///<summary>
    ///The last name of a person.
    ///<see cref="http://xmlns.com/foaf/0.1/lastName"/>
    ///</summary>
    public const string lastName = "http://xmlns.com/foaf/0.1/lastName";

    ///<summary>
    ///The given name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/givenName"/>
    ///</summary>
    public const string givenName = "http://xmlns.com/foaf/0.1/givenName";

    ///<summary>
    ///The given name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/givenname"/>
    ///</summary>
    public const string givenname = "http://xmlns.com/foaf/0.1/givenname";

    ///<summary>
    ///The surname of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/surname"/>
    ///</summary>
    public const string surname = "http://xmlns.com/foaf/0.1/surname";

    ///<summary>
    ///The family name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/family_name"/>
    ///</summary>
    public const string family_name = "http://xmlns.com/foaf/0.1/family_name";

    ///<summary>
    ///The family name of some person.
    ///<see cref="http://xmlns.com/foaf/0.1/familyName"/>
    ///</summary>
    public const string familyName = "http://xmlns.com/foaf/0.1/familyName";

    ///<summary>
    ///A phone,  specified using fully qualified tel: URI scheme (refs: http://www.w3.org/Addressing/schemes.html#tel).
    ///<see cref="http://xmlns.com/foaf/0.1/phone"/>
    ///</summary>
    public const string phone = "http://xmlns.com/foaf/0.1/phone";

    ///<summary>
    ///A homepage for some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/homepage"/>
    ///</summary>
    public const string homepage = "http://xmlns.com/foaf/0.1/homepage";

    ///<summary>
    ///A weblog of some thing (whether person, group, company etc.).
    ///<see cref="http://xmlns.com/foaf/0.1/weblog"/>
    ///</summary>
    public const string weblog = "http://xmlns.com/foaf/0.1/weblog";

    ///<summary>
    ///An OpenID for an Agent.
    ///<see cref="http://xmlns.com/foaf/0.1/openid"/>
    ///</summary>
    public const string openid = "http://xmlns.com/foaf/0.1/openid";

    ///<summary>
    ///A tipjar document for this agent, describing means for payment and reward.
    ///<see cref="http://xmlns.com/foaf/0.1/tipjar"/>
    ///</summary>
    public const string tipjar = "http://xmlns.com/foaf/0.1/tipjar";

    ///<summary>
    ///A .plan comment, in the tradition of finger and '.plan' files.
    ///<see cref="http://xmlns.com/foaf/0.1/plan"/>
    ///</summary>
    public const string plan = "http://xmlns.com/foaf/0.1/plan";

    ///<summary>
    ///Something that was made by this agent.
    ///<see cref="http://xmlns.com/foaf/0.1/made"/>
    ///</summary>
    public const string made = "http://xmlns.com/foaf/0.1/made";

    ///<summary>
    ///An agent that  made this thing.
    ///<see cref="http://xmlns.com/foaf/0.1/maker"/>
    ///</summary>
    public const string maker = "http://xmlns.com/foaf/0.1/maker";

    ///<summary>
    ///An image that can be used to represent some thing (ie. those depictions which are particularly representative of something, eg. one's photo on a homepage).
    ///<see cref="http://xmlns.com/foaf/0.1/img"/>
    ///</summary>
    public const string img = "http://xmlns.com/foaf/0.1/img";

    ///<summary>
    ///A depiction of some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/depiction"/>
    ///</summary>
    public const string depiction = "http://xmlns.com/foaf/0.1/depiction";

    ///<summary>
    ///A thing depicted in this representation.
    ///<see cref="http://xmlns.com/foaf/0.1/depicts"/>
    ///</summary>
    public const string depicts = "http://xmlns.com/foaf/0.1/depicts";

    ///<summary>
    ///A derived thumbnail image.
    ///<see cref="http://xmlns.com/foaf/0.1/thumbnail"/>
    ///</summary>
    public const string thumbnail = "http://xmlns.com/foaf/0.1/thumbnail";

    ///<summary>
    ///A Myers Briggs (MBTI) personality classification.
    ///<see cref="http://xmlns.com/foaf/0.1/myersBriggs"/>
    ///</summary>
    public const string myersBriggs = "http://xmlns.com/foaf/0.1/myersBriggs";

    ///<summary>
    ///A workplace homepage of some person; the homepage of an organization they work for.
    ///<see cref="http://xmlns.com/foaf/0.1/workplaceHomepage"/>
    ///</summary>
    public const string workplaceHomepage = "http://xmlns.com/foaf/0.1/workplaceHomepage";

    ///<summary>
    ///A work info homepage of some person; a page about their work for some organization.
    ///<see cref="http://xmlns.com/foaf/0.1/workInfoHomepage"/>
    ///</summary>
    public const string workInfoHomepage = "http://xmlns.com/foaf/0.1/workInfoHomepage";

    ///<summary>
    ///A homepage of a school attended by the person.
    ///<see cref="http://xmlns.com/foaf/0.1/schoolHomepage"/>
    ///</summary>
    public const string schoolHomepage = "http://xmlns.com/foaf/0.1/schoolHomepage";

    ///<summary>
    ///A person known by this person (indicating some level of reciprocated interaction between the parties).
    ///<see cref="http://xmlns.com/foaf/0.1/knows"/>
    ///</summary>
    public const string knows = "http://xmlns.com/foaf/0.1/knows";

    ///<summary>
    ///A page about a topic of interest to this person.
    ///<see cref="http://xmlns.com/foaf/0.1/interest"/>
    ///</summary>
    public const string interest = "http://xmlns.com/foaf/0.1/interest";

    ///<summary>
    ///A thing of interest to this person.
    ///<see cref="http://xmlns.com/foaf/0.1/topic_interest"/>
    ///</summary>
    public const string topic_interest = "http://xmlns.com/foaf/0.1/topic_interest";

    ///<summary>
    ///A link to the publications of this person.
    ///<see cref="http://xmlns.com/foaf/0.1/publications"/>
    ///</summary>
    public const string publications = "http://xmlns.com/foaf/0.1/publications";

    ///<summary>
    ///A current project this person works on.
    ///<see cref="http://xmlns.com/foaf/0.1/currentProject"/>
    ///</summary>
    public const string currentProject = "http://xmlns.com/foaf/0.1/currentProject";

    ///<summary>
    ///A project this person has previously worked on.
    ///<see cref="http://xmlns.com/foaf/0.1/pastProject"/>
    ///</summary>
    public const string pastProject = "http://xmlns.com/foaf/0.1/pastProject";

    ///<summary>
    ///An organization funding a project or person.
    ///<see cref="http://xmlns.com/foaf/0.1/fundedBy"/>
    ///</summary>
    public const string fundedBy = "http://xmlns.com/foaf/0.1/fundedBy";

    ///<summary>
    ///A logo representing some thing.
    ///<see cref="http://xmlns.com/foaf/0.1/logo"/>
    ///</summary>
    public const string logo = "http://xmlns.com/foaf/0.1/logo";

    ///<summary>
    ///A topic of some page or document.
    ///<see cref="http://xmlns.com/foaf/0.1/topic"/>
    ///</summary>
    public const string topic = "http://xmlns.com/foaf/0.1/topic";

    ///<summary>
    ///The primary topic of some page or document.
    ///<see cref="http://xmlns.com/foaf/0.1/primaryTopic"/>
    ///</summary>
    public const string primaryTopic = "http://xmlns.com/foaf/0.1/primaryTopic";

    ///<summary>
    ///The underlying or 'focal' entity associated with some SKOS-described concept.
    ///<see cref="http://xmlns.com/foaf/0.1/focus"/>
    ///</summary>
    public const string focus = "http://xmlns.com/foaf/0.1/focus";

    ///<summary>
    ///
    ///<see cref="http://www.w3.org/2004/02/skos/core#Concept"/>
    ///</summary>
    public const string Concept = "http://www.w3.org/2004/02/skos/core#Concept";

    ///<summary>
    ///A document that this thing is the primary topic of.
    ///<see cref="http://xmlns.com/foaf/0.1/isPrimaryTopicOf"/>
    ///</summary>
    public const string isPrimaryTopicOf = "http://xmlns.com/foaf/0.1/isPrimaryTopicOf";

    ///<summary>
    ///A page or document about this thing.
    ///<see cref="http://xmlns.com/foaf/0.1/page"/>
    ///</summary>
    public const string page = "http://xmlns.com/foaf/0.1/page";

    ///<summary>
    ///A theme.
    ///<see cref="http://xmlns.com/foaf/0.1/theme"/>
    ///</summary>
    public const string theme = "http://xmlns.com/foaf/0.1/theme";

    ///<summary>
    ///Indicates an account held by this agent.
    ///<see cref="http://xmlns.com/foaf/0.1/account"/>
    ///</summary>
    public const string account = "http://xmlns.com/foaf/0.1/account";

    ///<summary>
    ///Indicates an account held by this agent.
    ///<see cref="http://xmlns.com/foaf/0.1/holdsAccount"/>
    ///</summary>
    public const string holdsAccount = "http://xmlns.com/foaf/0.1/holdsAccount";

    ///<summary>
    ///Indicates a homepage of the service provide for this online account.
    ///<see cref="http://xmlns.com/foaf/0.1/accountServiceHomepage"/>
    ///</summary>
    public const string accountServiceHomepage = "http://xmlns.com/foaf/0.1/accountServiceHomepage";

    ///<summary>
    ///Indicates the name (identifier) associated with this online account.
    ///<see cref="http://xmlns.com/foaf/0.1/accountName"/>
    ///</summary>
    public const string accountName = "http://xmlns.com/foaf/0.1/accountName";

    ///<summary>
    ///Indicates a member of a Group
    ///<see cref="http://xmlns.com/foaf/0.1/member"/>
    ///</summary>
    public const string member = "http://xmlns.com/foaf/0.1/member";

    ///<summary>
    ///Indicates the class of individuals that are a member of a Group
    ///<see cref="http://xmlns.com/foaf/0.1/membershipClass"/>
    ///</summary>
    public const string membershipClass = "http://xmlns.com/foaf/0.1/membershipClass";

    ///<summary>
    ///The birthday of this Agent, represented in mm-dd string form, eg. '12-31'.
    ///<see cref="http://xmlns.com/foaf/0.1/birthday"/>
    ///</summary>
    public const string birthday = "http://xmlns.com/foaf/0.1/birthday";

    ///<summary>
    ///The age in years of some agent.
    ///<see cref="http://xmlns.com/foaf/0.1/age"/>
    ///</summary>
    public const string age = "http://xmlns.com/foaf/0.1/age";

    ///<summary>
    ///A string expressing what the user is happy for the general public (normally) to know about their current activity.
    ///<see cref="http://xmlns.com/foaf/0.1/status"/>
    ///</summary>
    public const string status = "http://xmlns.com/foaf/0.1/status";
}

}