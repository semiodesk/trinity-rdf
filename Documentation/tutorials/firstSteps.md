# First Steps #
[Download Project](http://static.semiodesk.com/semiodesk.trinity/examples/0.9/CliExample-0.9.102.zip)

The tutorial on this page should give you a first impression what you can do with Trinity RDF. 
If you follow the steps you will have a working application that already uses many features of the
Semantic Web technology stack. 

This example is a simple console application that creates and queries some data. In following examples 
we will show you how to do more complicated things.

## Create Project ##
To get started we will open Visual Studio and create a new console project, lets call it 'CliExample'.

Now we can start to add the dependencies using NuGet. We add the <code>Semiodesk.Trinity</code> to the project. 
This package contains the tools that are neccessary to create the object mapping. The resulting project 
structure should look like this:

![ProjectStructureCli.png](https://bitbucket.org/repo/pnBbge/images/2121744128-ProjectStructureCli.png)

The package creates a folder named 'Ontologies', which contains the three most basic ontologies, 
<code>rdf</code>, <code>rdfs</code> and <code>owl</code>.

The <code>App.config</code> file has been extended to contain the Trinity RDF configuration. Now we can 
start to build the domain model for our application.

## Manage Ontologies ##
First we need to add the foundation of our modelling to the project, the ontologies. If you have been 
following this tutorial and asking yourself what an ontology is, don't worry. It's basically just a collection 
of classes and their properties in a standardised format. In this example you don't have to write one 
yourself, we can just take an existing one.

If you want some background information on ontologies, read this [Wikipedia article](https://en.wikipedia.org/wiki/Ontology_%28information_science%29).

In this example we're going to use the 'Friend of a Friend' ontology, or just <code>foaf</code> in short. 
We download a <code>XML/RDF</code> serialised version [here](http://xmlns.com/foaf/spec/index.rdf) and copy it into the 
'Ontologies' folder in the ObjectModel project.

Also to avoid confusion, we rename it to <code>foaf.rdf</code>. Then we just need to add the file to the project in Visual Studio.

![Ontologies.png](https://bitbucket.org/repo/pnBbge/images/1285680337-Ontologies.png)

Now we need to change the configuration to make the new ontology known to Trinity RDF.
To make this happen, we add the following passage to the <code>App.config</code> as a child of 
the <code>OntologySettings</code> element:

```
<Ontology Uri="http://xmlns.com/foaf/0.1/" Prefix="foaf">
<FileSource Location="Ontologies\foaf.rdf"/>
</Ontology>
```

With this, we tell the framework where the ontology resides, which base URI it has and the namespace prefix 
we want to use. A namespace prefix is a shorthand for the bulky URI.

As we want to leverage inferencing, we need to tell [OpenLink Virtuoso](virtuoso.md) to use these ontologies, 
so we have to modify the <code>RuleSet</code> in the same file. Just replace the existing <code>VirtuosoSpecific</code>
element with the following one and you are done:

```
<VirtuosoStoreSettings>
    <RuleSets>
		<RuleSet Uri="urn:semiodesk/ruleset">
			<Graphs>
				<Graph Uri="http://www.w3.org/1999/02/22-rdf-syntax-ns#" />
				<Graph Uri="http://www.w3.org/2000/01/rdf-schema#" />
				<Graph Uri="http://www.w3.org/2002/07/owl#" />
				<Graph Uri="http://xmlns.com/foaf/0.1/" />
			</Graphs>
		</RuleSet>
    </RuleSets>
</VirtuosoStoreSettings>
```

When you build your project now, C# represenations of the ontologies will be created in the background.

## Create Mappings ##
Now we want to create our domain model. We are building a small manager for people in groups. We are 
using the <code>foaf</code> ontology as a base, so we just have to create the classes according to the  
[specification](http://xmlns.com/foaf/spec/). 

**Note:** We are currently working on a tool to automatically create a mapping from an ontology. These 
steps will be significantly easier in the future.

As base class for our <code>Group</code> and <code>Person</code> we want to use an <code>Agent</code> 
class. So we create a new class called Agent. 

```
using System;
using System.Collections.Generic;
using Semiodesk.Trinity;

namespace CliExample
{
    [RdfClass(FOAF.Agent)]
    public class Agent : Resource
    {
        public Agent(Uri uri) : base(uri) 
        { 
            EMailAccounts = new List<Resource>();
        }

        [RdfProperty(FOAF.name)]
        public string Name { get; set; }

        [RdfProperty(FOAF.mbox)]
        public List<Resource> EMailAccounts { get; set; }
    }
}
```

The class needs to be derived from <code>Resource</code>. The mapping can simply be done by 
decorating the class and the properties with the RDF classes and properties from the <code>foaf</code> ontology. 

**Note:** There is a distinction to be made between the generated 'foaf' class and the upper case 'FOAF' class. 
The upper case version provides the string representaion of the ontology elements and can be used in decorators
and attributes. The lower case variant provides <code>Class</code> and </code>Property</code> instances.

Now, let's create the other two classes, <code>Group</code> and <code>Person</code>. <code>Person</code> is derived 
from <code>Agent</code> and has a property that models the relationship between a person and other people.

```
[RdfClass(FOAF.Person)]
public class Person : Agent
{
    public Person(Uri uri) : base(uri) 
    {
        Knows = new List<Person>();
    }

    [RdfProperty(FOAF.knows)]
    public List<Person> Knows { get; set; }
}
```

And the <code>Group</code>, which is also derived from <code>Agent</code> and contains a property modelling its membership property.

```
[RdfClass(FOAF.Group)]
public class Group : Agent
{
    public Group(Uri uri) : base(uri) 
    {
        Member = new List<Agent>();
    }

    [RdfProperty(FOAF.member)]
    public List<Agent> Member { get; set; }
}
```

## Database Connection ##
We want to use a Virtuoso backend for this example. It is fairly simple to set up, so just follow the 
instructions [here](virtuoso.md). Starting the database from the console should be sufficient for this case.

To tell Trinity RDF how to connect to the running Virtuoso instance, we need to add the following 
connection string to the <code>App.config</code> file. 

```
<connectionStrings>
    <add name="virt0"
	     providerName="Semiodesk.Trinity"
		 connectionString="provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba;rule=urn:semiodesk/ruleset"/>
</connectionStrings>
```

When you build your application now, the given ontologies will be deployed to the Virtuoso instance 
given in the connection string.

To verify this, you can look at the Virtuoso Conductor in your browser. The default URL is [http://localhost:8890](http://localhost:8890), 
and admin login is <code>dba/dba</code>. You should be able see the new ontology graphs when you navigate 
to "Linked Data" -> "Graphs". The following screenshot shows how it should look like.

![Graphs.png](https://bitbucket.org/repo/pnBbge/images/1616352935-Graphs.png)

## Building the Application ##
Now we can start writing the application. First we need to do some initialization and then we can open 
a connection to the store. This can be done by using the name of the connection string. With the 
<code>LoadOntologySettings()</code>-method we tell the store to import all ontologies from the current 
<code>App.config</code> file. In the case of the Virtuoso the ruleset is also created.

```
SemiodeskDiscovery.Discover();

IStore store = StoreFactory.CreateStoreFromConfiguration("virt0");
store.LoadOntologySettings(); 
```

Then we either create or open a model. If the model exists, we clear it, so we don't add the same information 
again. A model in RDF contains triples and is identified by a URI. It can be used to group information of one 
domain together. 

```
IModel model = store.GetModel(new Uri("http://semiodesk.com/example/cli");

if(!model.IsEmpty)
{
  model.Clear();
}
```

Then we can start to add our mapped objects to the model. First we let the model create a new 
resource of type <code>Person</code>. The empty parameter in the <code>CreateResource()</code> method means 
that we want the model to create a URI for the resource. After adding values to the resource we need 
to commit it to the model by calling the <code>Commit()</code> method.

```
Person john = model.CreateResource<Person>();
john.EMailAccounts.Add(new Resource("mailto:john.doe@example.com"));
john.Name = "John Doe";
john.Commit();

Group group = model.CreateResource<Group>();
group.Name = "My Group";
group.Member.Add(john);
group.Commit();
```

When we want to get every <code>Agent</code>, meaning all <code>Group</code> and all <code>Person</code> objects, 
we can call <code>model.GetResources<Agent>(true)</code>. The type restricts the query to all <code>Agent</code> objects. 

With the <code>true</code> parameter we tell the model to infer the types from the ontologies. Because <code>foaf:Person</code>
and <code>foaf:Group</code> are subclasses of <code>foaf:Agent</code> the query also returns the mapped objects for these classes.

```
foreach (Agent agent in model.GetResources<Agent>(true))
{
   Console.WriteLine(agent.Name);
}
```
