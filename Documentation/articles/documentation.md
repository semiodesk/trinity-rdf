# Documentation #

We wanted to create a RDF API for C# and Mono that does not require the developer to work with the triple store by adding and removing triples manually. On the lowest level we offer a Resource-Property-Value interface. On the highest level, the developer can work with mapped classes. This makes the API perfect for developing user interfaces, as it supports data binding and modern application paradigms such as [MVC ](https://en.wikipedia.org/wiki/Model–view–controller) / [MVVM](https://en.wikipedia.org/wiki/Model_View_ViewModel).

## Setup ##
One of the core requirements for [Semiodesk Trinity](http://www.semiodesk.com/products/trinity) was that ontology terms need to be easily accessible from native .NET languages. If you are using the Semiodesk.Trinity.Modelling NuGet package you are ready to go. In a pre-compilation step, our framework generates a C# representation of the ontology in a the file *obj/ontologies.g.cs*. It creates classes for each given ontology. These classes contain basic information about the ontology:
```
#!csharp
string prefix = owl.Prefix;
Uri ns = owl.Namespace;

```
and all RDF classes and properties. 
```
#!csharp
Class onto = owl.Ontology; // type contains the class with URI <http://www.w3.org/2002/07/owl#Ontology>
string ontoString = OWL.Ontology; // ontoString contains the string constant "http://www.w3.org/2002/07/owl#Ontology"
```
These can then be used to make queries, add values to resources or create class mappings.


The convention is that lower case ontology classes (e.g. rdf) contains the Class and Property instances, upper case classes (e.g. RDF) contains the string representations neccessary for the attributes.

The generation of the ontolgies can be configured in the App.config file. The following snippet shows an example how this looks like.

```
#!xml
<configuration>
  ...
  <configSections>
    <section name="TrinitySettings" type="Semiodesk.Trinity.Configuration.TrinitySettings, Semiodesk.Trinity" />
  </configSections>

  <!-- Generate the ontology classes in the 'CliExample'-namespace -->
  <TrinitySettings namespace="CliExample">
    <OntologySettings>
      <!--Generate the class 'CliExample.rdf' from the contents of the file Ontologies\rdf.rdf -->
      <Ontology Uri="http://www.w3.org/1999/02/22-rdf-syntax-ns#" Prefix="rdf">
        <FileSource Location="Ontologies\rdf.rdf" />
      </Ontology>
    </OntologySettings>
  </TrinitySettings>
  ...
</configuration>

```
Using the *namespace* attribute of the TrinitySettings element you can control the CLR namespace in which the ontologies should be generated.

The *Uri* is the namespace of the ontology. the *Prefix* is a shortcode for the ontology. This is used as the generated classes name. The *FileSource* element defines the location of the file relative to the configuration.

||
|---|
| **Note:** If you are using NuGet, please be aware that if you change the framework (for example from .Net 4.5 to .Net 3.5 you need to retarget the packages. In most cases the easiest way to do that is by reinstalling them. |

## Store Connection ##
A Rdf store, Triple store or simply just store represents the physical location of the data.
This can either be in a database, a remote endpoint or just a temporary store in the computers memory.

Establish connection to a store with 

```
#!csharp

IStore store = StoreFactory.CreateStore("CONNECTIONSTRING");
```

If you don't want to keep the connection store in code, you can define a connection string in the app.config 
```
#!xml
<configuration>
  ...
  <connectionStrings>
    <add name="virt0" providerName="Semiodesk.Trinity" connectionString="provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba;rule=urn:semiodesk/ruleset" />
  </connectionStrings>
  ...
</configuration>

```
Then you can create a store with the followling call
```
#!csharp

IStore store = StoreFactory.CreateStoreFromConfiguration("virt0");
```

The connection string has one fixed key, the *provider*. All following keys depend on the selected provider.
Currently three providers are supported, OpenLink Virtuoso, SparqlEndpoints and a memory store based on dotnetrdf. It is also possible to write and load a custom store module.

### Loading Configuration to Store ###
The ontologies specified in the configuration need to be loaded into the store to do things like inferencing. The method to do this is called *Store.LoadOntologySettings()*. Optionally it can be given the path of a configuration file. By default it will use the app.config file of the current assembly. 
As second parameter you can define the base directory for the ontologies.

||
|---|
| **IMPORTANT:** Do not forget to set all ontologies to "Copy always" so they will be found at runtime. |

Example:
```
#!csharp

store.LoadOntologySettings();

// Or

store.LoadOntologySettings(Path.Combine(Environment.CurrentDirectory, "myConfig.cfg"));

// Or

store.LoadOntologySettings(Path.Combine(Environment.CurrentDirectory, "myConfig.cfg"), "C:\\ontologyDir");
```

It is in the responsibility of the developer to decide whether the ontologies have changed and need to be redeployed. During development it usually is no issue to do that at the start of the software. 


### OpenLink Virtuoso ###

This store is an excellent choice as backend if you want to host your own Semantic-enabled application. You can download the open source version of it [here](http://virtuoso.openlinksw.com/dataspace/doc/dav/wiki/Main/). 
The following example creates a connection to an OpenLink Virtuoso:
```
#!csharp

IStore store = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");
```
Possible options are:

**host** : Hostename of the server where the Virtuoso instance is being run

**port** : Port of the Virtuoso instance. This can be looked up in the server configuration

**uid** and **pw** : Credentals to access the server

**rule** : The default ruleset for inferencing.

Rulesets can be defined in the app.config like this:

```
#!xml
<configuration>
  ...
  <TrinitySettings>
    <VirtuosoStoreSettings>
      <RuleSets>
        <RuleSet Uri="urn:semiodesk/ruleset">
          <Graphs>
            <Graph Uri="http://www.w3.org/1999/02/22-rdf-syntax-ns#" />
          </Graphs>
        </RuleSet>
      </RuleSets>
    </VirtuosoStoreSettings>
  </TrinitySettings>
  ...
</configuration>

```

### Sparql Endpoints ###

Sparql Endpoints offer a platform independent way to access linked data. If you just need reliable read-only access, this would be the way to go. 

```
#!csharp

IStore store = StoreFactory.CreateStore("provider=sparqlendpoint;endpoint=http://live.dbpedia.org/sparql");
```

With the only option **endpoint** you can define the endpoint you want to connect to. Please keep in mind that Sparql Endpoints do not support model management or data updates.

### Memory Store ###

This store is great for small projects which don't need much in the way of inferencing. If you just want to load a serialized collection of triples or an ontology, this offers the most flexiblity. Just remember that you have to save the content manually before shutting down the application or everything is lost.
```
#!csharp

IStore store = StoreFactory.CreateStore("provider=dotnetrdf");
```

### Custom Store ###

If you want to use the Trinity with a currently unsupported store, you can write a custom store provider and register it with Trinity.
With the following function you can try to load the custom module.
```
#!csharp

StoreFactory.LoadProvider("CustomStoreProvider.dll")
```

The *CustomStoreProvider.dll* needs to contain a class derived from StoreProvider and an IStore implementation to work. Then you can create a connection to the store by calling the CreateStore function with the provider name you set.
```
#!csharp

IStore store = StoreFactory.CreateStore("provider=YourProviderName");
```

## Model Management ##

A model can be used to group contextual data together. They create a barrier between data that can be used to separate information and controll the access to the data. For example, it makes sense to create a model for each registered user on your system, if they are not allowed to share data. These barriers can be softend tough, as it is possible to query multiple models at once.

The following code snippet outlines the basic methods for managing the models in a store:
```
#!csharp
IStore store = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba"); 

Uri modelUri = new Uri("http://localhost:8890/Models/ExampleModel");

// Check if a model with the given URI already exists.
if (store.ContainsModel(modelUri))
{
   // Remove the model from the store.
   store.RemoveModel(modelUri);
}

// Create an empty model in the store.
store.CreateModel(modelUri);

// Now we can do work with the model.
IModel model = store.GetModel(modelUri);

// Load the contents from a file into the model.
store.Read(modelUri, new Uri("file://example.n3"), RdfSerializationFormat.N3);

// Write the contents of a model into a file.
FileStream stream = new FileStream("file://example.rdf", FileMode.Create);

store.Write(stream, modelUri, RdfSerializationFormat.RdfXml);
```

### Model Groups ###

A model group allows to make queries over multiple models at once. This is great way to blend different information sources together. Due to technical restrictions it is not possible to modify the result resources tough. They are marked as read-only and a commit will result in an error.
The following code piece demonstrates how model groups work.

```
#!csharp

// create model group of two models
IModelGroup modelGroup = store.CreateModelGroup(new Uri("ex:Test1"), new Uri("ex:Test2"));

// we can use a model group like a regular model
bool contains = modelGroup.ContainsResource(new Uri("ex:Test/testResource"));

// we can make queris on them
ResourceQuery q = new ResourceQuery(nco.Contact);
var res = modelGroup.GetResources(q);

// we cannot change resources directly
IResource resource= modelGroup.GetResource(resourceUri);

// we need to get a writable represenation from the 
IModel test1 = store.GetModel(new Uri("ex:Test1"));
if( test1.ContainsResource(resource.Uri) )
  resource = test1.GetResource(resource.Uri);

```
Because of the nature of the models, it is possible that resources exist in both models at once, both with different bits of information. This is why the developer has to decide which resource he want to change and thus, which part of the information he wants to modify. 

##Resource Management##

Creating new generic resources is done with the *CreateResource* function on the model handle.
```
#!csharp

IResource john = model.CreateResource(new Uri("ex:testModel/john"));
```

### Adding Properties ###
To add a property we use the AddProperty method. Look at the Chapter **Ontology Handling** to see how to use properties from an ontology.
```
#!csharp

// Without generated ontologies
john.AddProperty(new Property(new Uri("ex:myProperty"), "My value");

// With generated ontologies
john.AddProperty(rdf.type, nco.Contact); 
john.AddProperty(nco.fullname, "John Doe"); 
```
### Iterating over Properties ###

To iterate over all properties, we can call the *ListProperties* method. To access the values, we have then have to call the *ListValues* method. There is also a *GetValue* method, which will only return the first value or null.
```
#!csharp


foreach( Property p in john.ListProperties())
{
  foreach( var value in john.ListValues(p) )
  {
    Console.WriteLine("{0} {1} {2}", john, p, value);
  }
}
```

Alternatively you can call *ListValues* and iterate over all Triples.
```
#!csharp


foreach( Tuple<Property, object> tuple in john.ListValues())
{
  Console.WriteLine("{0} {1} {2}", john, tuple.Item1, tuple.Item2);
}
```

To test if a property exists in a resource, you can call *HasProperty* either just with a property or with a property and value to test for a combination.

### Removing Properties ###

To remove a property, simply call RemoveProperty with the property and the value you want to remove.
```
#!csharp

// Without generated ontologies
john.RemoveProperty(new Property(new Uri("ex:myProperty")), "My value");

// With generated ontologies
john.RemoveProperty(rdf.type, nco.Contact); 
john.RemoveProperty(nco.fullname, "John Doe"); 
```

### Save Changes ###

To persist changes in the model, they need to be comitted. Every modification in the resource is temporary until the *Commit* method is called.
```
#!csharp

john.Commit();
```

If the resource has been created by calling it's constructor and not using the *IModel.CreateResource* method, it can be added retroactivly by calling *IModel.AddResource*. The resulting copy of the resource supports the *Commit* method.
```
#!csharp

john2 = model.AddResource(john);
```


## Semantic Object Mapping ##

Semiodesk Trinity offers two ways of doing class mapping. The more readable and easier one is decorating. It needs to run a post-compiler step. If you cannot do that, you can also use mapping objects. In the following we describe both ways.

**Note:** 
Valid types for mapping are all base value types, DateTime, Classes derived from Resource as well as collections of these types implementing the IList interface.

### Decorating ###

```
#!csharp

[RdfClass(FOAF.Person)]
public class Person : Agent
{
  #region Constructors
  public Person(Uri uri) : base(uri) { }
  #endregion

  #region Mapping
  [RdfProperty(FOAF.firstName)]
  public string FirstName{ get; set; }

  [RdfProperty(FOAF.lastName)]
  public string LastName { get; set; }
  #endregion
}
```
The class needs to be decorated with the RDF class it is mapping.
It is important that the constructor with a Uri parameter is implemented.
For the actual mapping of properties, you just need to decorate them with the RDF property they should be mapped to. The getter and setter need to be empty. 

For the decorating you need to use the upper case prefix of the ontologies (e.g. FOAF instead of foaf) because C# only accepts static strings for decorating.

### Mapping objects ###

In environments that cannot do post-build processing, it can be desirable to use the native mapping mechanism. The following example demonstrates how this works. 

```
#!csharp
public class Person : Agent
{
    #region Constructors
    // This constructor is neccessary
    public Person(Uri uri) : base(uri) { }
    #endregion

    #region Mapping
    // This function defines which RDF class or classes should be mapped
    public override IEnumerable<Class> GetTypes()
    {
        return new Class[] { foaf.Person };
    }

    // every mapped property needs a PropertyMapping object to store the value, it needs the name of the property as well as the RDF property it mapps as parameter
    protected PropertyMapping<string> firstNameProperty = new PropertyMapping<string>("FirstName", foaf.firstName);
    // The getter and setter of the property need to access the PropertyMapping object for the real value
    public string FirstName
    {
        get { return GetValue(firstNameProperty); }
        set { SetValue(firstNameProperty, value); }
    }

    protected PropertyMapping<string> lastNameProperty = new PropertyMapping<string>("LastName", foaf.lastName);
    public string LastName
    {
        get { return GetValue(lastNameProperty); }
        set { SetValue(lastNameProperty, value); }
    }

    #endregion
}
```


## Resource Query ##

Inline queries are error prone, as native data types need to be serialized manually by the developer each time. With the resource query this work is done by the API. 

||
|---|
| **Note:** Though C# already has a concept for queries embedded in code, LINQ, we decided against it. The reason for this is because not all SPARQL expressions can be mapped to LINQ expressions. We are still evaluating the possiblities we have. |

SPARQL queries are basically patterns that are matched against the graph. 

```
#!SPARQL
PREFIX nco: <http://www.semanticdesktop.org/ontologies/2007/03/22/nco#> 
PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
DESCRIBE ?contact 
WHERE 
{ 
  ?contact rdf:type nco:Contact .
  ?contact nco:belongsToGroup ?group .
  ?group nco:contactGroupName „Family“ .
  ?contact nco:birthDate ?birthDate .
  FILTER (?birthDate  < "1990-01-01"^^xsd:date )
}

```

This query tries to find a contact, born before 1. January 1990 and belongs to the group "Family".

The following example shows this query in the Resource Query format.

```
#!csharp
ResourceQuery contact = new ResourceQuery(nco.PersonContact);
contact.Where(nco.birthDate).LessThan(new DateTime(1990, 1, 1));

ResourceQuery group = new ResourceQuery(nco.ContactGroup);
group.Where(nco.contactGroupName, "Family");

contact.Where(nco.belongsToGroup, group);

IResourceQueryResult result = model.ExecuteQuery(contact);
foreach (Resource r in result.GetResources())
{
  Console.WriteLine(r.Uri);
}


```

## Paged Data Access ##

Loading a large amount of resources takes some time. In most cases it is not necessary to access them all at once but only one at a time. For these cases the data can be loaded in chunks. The following example shows how it is done.


```
#!csharp
ResourceQuery q = new ResourceQuery(foaf.Person); // This also works for SparqlQueries
var x = model.ExecuteQuery(q);
int count = x.Count();
int pageSize = 600;
int pageCount = (count + pageSize - 1) / pageSize;

for (int j = 0; j < pageCount; j++)
{
    var result = x.GetResources<Person>(j * pageSize, pageSize);
    foreach (Resource r in result.GetResources())
    {
       Console.WriteLine(r.Uri);
    }
}

```



## Data Virtualization ##

...
