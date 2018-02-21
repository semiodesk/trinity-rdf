# First Steps #
The tutorial on this page should give you a first impression what you can do with the Semiodesk Trinity API. If you follow the steps you will have a working application that already uses a lot of features that semantic web technologies can offer. 

You can download the finished example from  
[http://static.semiodesk.com/semiodesk.trinity/examples/0.9/CliExample-0.9.102.zip](http://static.semiodesk.com/semiodesk.trinity/examples/0.9/CliExample-0.9.102.zip)

We have prepared this example to work under Windows with the .Net platform. Different examples for Linux and Mac are not done yet but they will be published here once they are completed.

This example is a simple console application that just creates and queries some data. In following examples we will show you how to do more complicated stuff.

## Create a project ##
To get started we will open Visual Studio and create a new console project, lets call it "CliExample".

Now we can start to add the dependencies using NuGet. We add the Semiodesk.Trinity.Modelling to the project.
This package contains the tools that are neccessary to create the object mapping. It also has a dependency to the Semiodesk.Trinity.Core package which contains the actual library.

The resulting project structure should look like this  

![ProjectStructureCli.png](https://bitbucket.org/repo/pnBbge/images/2121744128-ProjectStructureCli.png)

The package added a folder called Ontologies, which contains the three most basic ontologies, rdf, rdfs and owl.
The App.config file has been extended to contain the configuration for Semiodesk.Trinity.

Now we can start to build the domain model for our application.

## Add an ontology ##

First we need to add the foundation of our modelling to the project, the ontologies. If you have been following this tutorial and asking yourself what an ontology is, don't worry. It's basically just a collection of classes and their properties  formalised in a standardised way. In this example you don't have to write one yourself, we can just take an existing one.  
If you want to read up on the topic here is a link to the [Wikipedia article](https://en.wikipedia.org/wiki/Ontology_%28information_science%29).

In this example we're going to use the 'Friend of a Friend' ontology, or just foaf in short.
We download a XML/RDF serialised version from  
[http://xmlns.com/foaf/spec/index.rdf](http://xmlns.com/foaf/spec/index.rdf)  
and copy it into the 'Ontologies' folder in the ObjectModel project. Also to avoid confusion, we rename it to 'foaf.rdf'. Then we just need to add the file to the project in Visual Studio.

![Ontologies.png](https://bitbucket.org/repo/pnBbge/images/1285680337-Ontologies.png)

Now we need to change the configuration to make the new ontology known to Semiodesk.Trinity.
To make this happen, we add the following passage to the App.config under <OntologySettings>.

```
#!xml
  <!--http://xmlns.com/foaf/0.1/-->
  <Ontology Uri="http://xmlns.com/foaf/0.1/" Prefix="foaf">
    <FileSource Location="Ontologies\foaf.rdf"/>
  </Ontology>

```

With this, we tell the framework where the ontology lies, what base uri it has and the prefix it should use for the C# class.

As we want to use inferencing, we need to tell Virtuoso to use these ontologies, so we have to modify the RuleSet in the same file.
Just replace the existing VirtuosoSpecific part with the following one and you are set.
```
#!xml
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

When you build your project now it creates the c# represenations of the ontologies in the background.

## Create mappings ##

Now we want to create our domain model. We are building a small manager for people in groups.
We are using the foaf ontology as a base, so we just have to create the classes according to the  [specification](http://xmlns.com/foaf/spec/). 

||
|---|
| **Note:** We are currently working on a tool to automatically create a mapping from an ontology. These steps will be significantly easier in the future. |

As base class for our Group and Person we want to use an Agent class. So we create a new class called Agent. 

```
#!csharp
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

The class needs to be derived from Resource. The mapping can simply be done by decorating the class and the properties with the rdf classes and properties from the foaf ontology. 

||
|---|
| **Note:** There is a distinction to be made between the generated foaf class and the upper case FOAF class. The upper case version supplies the string representaion of the ontology elements and should only be used for the decorating. The lower case variant supplies Class and Property objects. |


Lets now create the other two classes, Person and Group. Person is derived from Agent and has a property that models the relationship between a person and other people.

```
#!csharp
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

And the Group, which is also derived from Agent and contains a property modelling its membership property.

```
#!csharp
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

## Connection to the backend ##

We want to use a Virtuoso backend for this example. It is fairly simple to set up, so just follow the instructions [here](https://bitbucket.org/semiodesk/trinity/wiki/SetupVirtuoso).
The quickstart method should be sufficient for this case.

To tell Semiodesk.Trinity how to connect to the running Virtuoso instance, we need to add the following connection string to the App.config file. 
```
#!xml
<connectionStrings>
    <add name="virt0" providerName="Semiodesk.Trinity" connectionString="provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba;rule=urn:semiodesk/ruleset"/>
</connectionStrings>
```
When you build your application now, the given ontologies will be deployed to the Virtuoso instance given in the connection string.

To verify this, you can look at the Virtuoso Conductor in your browser (default is [http://localhost:8890](http://localhost:8890), login is dba/dba) you should be able see the new ontology graphs when you navigate to "Linked Data" -> "Graphs".
The following screenshot shows how it should look like.

![Graphs.png](https://bitbucket.org/repo/pnBbge/images/1616352935-Graphs.png)


## Building the application ##

Now we can start writing the application. First we need to do some initialization and then we can open a connection to the store. This can be done by using the name of the connection string.
With the *LoadOntologySettings()* method we tell the store to import all ontologies from the current app.config file. In the case of the Virtuoso the ruleset is also created.

```
#!c#

SemiodeskDiscovery.Discover();

IStore store = StoreFactory.CreateStoreFromConfiguration("virt0");
store.LoadOntologySettings(); 

```

Then we either create or open a model. If the model exists, we clear it, so we don't add the same information again. A model in RDF contains triples and is identified by a Uri. It can be used to group information of one domain together. 

```
#!csharp
Uri modelUri = new Uri("http://semiodesk.com/example/cli");

IModel model;

if (store.ContainsModel(modelUri))
{
  model = store.GetModel(modelUri);
  model.Clear();
}
else
{
  model = store.CreateModel(modelUri);
}
```

Then we can start to add our mapped objects to the model.
First we let the model create a new resource of type "Person". The empty parameter in the *CreateResource()* method means that we want the model to create a URI for the resource. After adding values to the resource we need to commit it to the model by calling the *Commit()* method.

```
#!csharp
Person john = model.CreateResource<Person>();
john.EMailAccounts.Add(new Resource("mailto:john.doe@example.com"));
john.Name = "John Doe";
john.Commit();

Group myGroup = model.CreateResource<Group>();
myGroup.Name = "My Group";
myGroup.Member.Add(john);
myGroup.Commit();

```

When we want to get every Agent, meaning all Group and all Person objects, we can call ''model.GetResources<Agent>(true)''. The type restricts the query to all Agent objects. With the true parameter we tell the model to infer the types from the ontologies. Because foaf:Person and foaf:Group are subclasses of foaf:Agent the query also returns the mapped objects for these classes.

```
#!csharp
foreach (Agent a in model.GetResources<Agent>(true))
{
   Console.WriteLine(a.Name);
}
```

