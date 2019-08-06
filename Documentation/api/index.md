# Trinity RDF 1.0
We wanted to create a high-level RDF API for C# and Mono that makes working with RDF graph 
databases as simple as possible. With Trinity RDF developers can work with mapped objects 
and LINQ without seeing any RDF at all.

### ORM Benefits
* Shallow learning curve for developers.
* Best possible compatibility when integrating with existing platforms and tools.
* Supports enterprise application development patterns such as [MVC](https://en.wikipedia.org/wiki/Model–view–controller) / [MVVM](https://en.wikipedia.org/wiki/Model_View_ViewModel).
* Reduces potential for errors through object type constraints.

## Setup
One of the core requirements for [Trinity RDF](https://trinity-rdf.net) was that ontology terms 
need to be easily accessible from native .NET languages. If you are using the <code>Trinity.RDF</code>-NuGet 
package you are ready to go.

### Ontology Constants
In a pre-compilation step, our framework generates a C# representation of your ontologies in a file named 
<code>*obj/ontologies.g.cs*</code>. 
It creates classes for each ontology configured in <code>App.config</code>. These classes contain basic information 
about the ontology:

```
// Default ontology namespace prefix (i.e. "schema").
string prefix = schema.Prefix;

// Ontology namespace URI (i.e. "http://schema.org").
Uri ns = schema.Namespace;
```

and all the RDF classes and properties defined in the ontology:

```
// The 'Person' class with the URI "http://schema.org/Person" and all properties defined in the vocabulary.
Class personClass = schema.Person;

// String constant with the URI "http://schema.org/Person"
string personUriString = SCHEMA.Person;
```

These constants can then be used to create queries, add values to resources or create mappings.

The convention is that lower case ontology classes (e.g. rdf) contains the Class and Property instances, 
upper case classes (e.g. RDF) contains the string representations neccessary for the attributes.

The generation of the ontolgies can be configured in the <code>App.config</code> file:

```
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

Using the <code>namespace</code> attribute of the <code>TrinitySettings</code> element you can control 
the CLR namespace in which the ontologies should be generated.

The <code>Uri</code> is the namespace of the ontology. The <code>Prefix</code> is a short identifier for the ontology. 
It is used as the generated classes name. The <code>FileSource</code> element defines the location of the file 
relative to the configuration.

**Note:** If you are using NuGet, please be aware that if you change the framework (for example from .Net 4.5 to 
.Net 3.5 you need to retarget the packages. In most cases the easiest way to do that is by reinstalling them.

## Store Connection
A RDF store, Triple Store or simply just 'store' represents the physical location of the data. This can either 
be in a database, a remote SPARQL endpoint or just a temporary store in memory.

Establish connection to a store with 

```
IStore store = StoreFactory.CreateStore("CONNECTIONSTRING");
```

If you don't want to keep the connection store in code, you can define a connection string in the <code>App.config</code>

```
<configuration>
  ...
  <connectionStrings>
    <add name="virt0"
		 providerName="Semiodesk.Trinity"
		 connectionString="provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba;rule=urn:semiodesk/ruleset" />
  </connectionStrings>
  ...
</configuration>
```

Then you can create a store with the followling call

```
IStore store = StoreFactory.CreateStoreFromConfiguration("virt0");
```

The connection string has one fixed key, the so called 'provider'. All following keys depend on the selected provider.
Currently three providers are supported, OpenLink Virtuoso, SparqlEndpoints and a memory store based on [dotNetRdf](http://www.dotnetrdf.org/). It 
is also possible to write and load a custom store module.

### Loading Configurations
The ontologies specified in the configuration need to be loaded into the store to do things like inferencing. The 
method to do this is called <code>Store.LoadOntologySettings()</code>. Optionally it can be given the path of a 
configuration file. By default it will use the <code>App.config</code> file of the current assembly. As second 
parameter you can define the base directory for the ontologies.

**IMPORTANT:** Do not forget to set all ontologies to "Copy always" so they will be found at runtime.

```
store.LoadOntologySettings();

// Or
store.LoadOntologySettings(Path.Combine(Environment.CurrentDirectory, "myConfig.cfg"));

// Or
store.LoadOntologySettings(Path.Combine(Environment.CurrentDirectory, "myConfig.cfg"), "C:\\ontologyDir");
```

It is in the responsibility of the developer to decide whether the ontologies have changed and need to be 
redeployed. During development it usually is no issue to do that at the start of the software. 

### OpenLink Virtuoso
This store is an excellent choice as backend if you want to host your own Semantic-enabled application. 
You can download the open source version of it [here](http://virtuoso.openlinksw.com/dataspace/doc/dav/wiki/Main/).

The following example creates a connection to an OpenLink Virtuoso:

```
IStore store = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");
```

Possible options are:

| Parameter | Description |
| --- | --- |
| <code>host</code> | Hostename of the running Virtuoso instance. |
| <code>port</code> | Port of the Virtuoso instance. This can be looked up in the server configuration. |
| <code>uid</code> | Username |
| <code>pw</code> | Password |
| <code>rule</code> | The default ruleset for inferencing. |

Rulesets can be defined in the <code>App.config</code> like this:

```
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

### Sparql Endpoints
SPARQL endpoints offer a platform independent way to access linked data sets.

```
IStore store = StoreFactory.CreateSparqlEndpointStore(new Uri("http://live.dbpedia.org/sparql"));
```

Please keep in mind that SPARQL endpoints usually do not support model management or data updates.

### Memory Store
This store is ideal as a playground or for temporarily manipulating data. If you just 
want to load a serialized collection of triples or an ontology, this offers the most flexiblity. Please
remember that you have to save the content manually before shutting down the application or everything is lost.

```
IStore store = StoreFactory.CreateMemoryStore();
```

### Custom Store
If you want to use the Trinity RDF with an unsupported store, you can write a custom store 
provider and register it. With the following function you can try to load the custom module:

```
StoreFactory.LoadProvider("CustomStoreProvider.dll")
```

The <code>CustomStoreProvider.dll</code> needs to contain a class derived from StoreProvider and 
an IStore implementation to work. Then you can create a connection to the store by calling the 
<code>CreateStore</code> function with the provider name you set.

```
IStore store = StoreFactory.CreateStore("provider=YourProviderName");
```

## Model Management
A model can be used to group contextual data together. They create a barrier between data that 
can be used to separate information and controll the access to the data. For example, it makes 
sense to create a model for each registered user on your system, if they are not allowed to share 
data. These barriers can be softend tough, as it is possible to query multiple models at once.

The following code snippet outlines the basic methods for managing the models in a store:

```
IStore store = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba"); 

// We retrieve an existing or a new model.
IModel model = store.GetModel(new Uri("http://localhost:8890/models/example"));

if(!model.IsEmpty)
{
	model.Clear();
}

// Load the contents from a file into the model.
store.Read(modelUri, new Uri("file://example.n3"), RdfSerializationFormat.N3);

// Write the contents of a model into a file.
FileStream stream = new FileStream("file://example.rdf", FileMode.Create);

store.Write(stream, modelUri, RdfSerializationFormat.RdfXml);
```

### Model Groups
A model group allows to make queries over multiple models at once. This is great 
way to blend different information sources together. Due to technical restrictions it is 
not possible to modify the result resources tough. They are marked as read-only and a 
commit will result in an error.

The following code piece demonstrates how model groups work:

```
// Create model group of two models
IModelGroup modelGroup = store.CreateModelGroup(
	new Uri("http://localhost/models/test1"),
	new Uri("http://localhost/models/test2")
);

// We can use a model group like a regular model.
bool contains = modelGroup.ContainsResource(new Uri("ex:something"));

// We can make queris on them.
foreach(Contact c in modelGroup.AsQueryable<Contact>())
{
	Console.WriteLine(c.Name);
}

// Note: we cannot change resources directly! To do this we need to get 
// a writable represenation from the model it is stored in..
IModel test1 = store.GetModel(new Uri("http://localhost:8890/models/test1"));

if(test1.ContainsResource(new Uri("ex:thing")))
{
  IResource thing = test1.GetResource(new Uri("ex:thing"));
  thing.Name = "Anything";
  thing.Commit();
}
```

It is possible that a resource exist in multiple models at once, all of them with 
different bits of information. Therefore we have to decide which resource 
he want to change and thus, which part of the information we want to modify. 

## Resource Management
Creating new generic resources is done with the <code>CreateResource</code> function on the model:

```
IResource john = model.CreateResource(new Uri("ex:john"));
```

### Checking Properties
To test if a property exists for a resource, you can call <code>HasProperty</code> either just 
with a property or with a property and value combination:

```
// With generated ontologies
john.HasProperty(schema.name); 
john.HasProperty(schema.name, "John Doe"); 
```

### Iterating Properties
To iterate over all properties, we can call the <code>ListProperties</code> method. To access the values, 
we have then have to call the <code>ListValues</code> method. There is also a <code>GetValue</code> method, 
which will only return the first value or null.

```
foreach(Property property in john.ListProperties())
{
  foreach(var value in john.ListValues(property))
  {
    Console.WriteLine($"{john} {property} {value}");
  }
}
```

Alternatively you can call <code>ListValues</code> and iterate over all the triples:
```
foreach(Tuple<Property, object> propertyValue in john.ListValues())
{
  Console.WriteLine($"{john} {propertyValue.Item1} {propertyValue.Item2}");
}
```

### Adding Properties
To add a property we use the <code>AddProperty</code> method. Look at the Chapter **Ontology Handling** 
to see how to use properties from an ontology.

```
// Without using generated ontologies.
john.AddProperty(new Property(new Uri("http://schema.org/name"), "John Doe");

// Using generated ontologies.
john.AddProperty(rdf.type, schema.Person); 
john.AddProperty(schema.name, "John Doe"); 
```

### Removing Properties 
To remove a property, simply call <code>RemoveProperty</code> with the property and the value you want to remove.

```
// Without using generated ontologies.
john.RemoveProperty(new Property(new Uri("http://schema.org/name")), "John Doe");

// Using generated ontologies.
john.RemoveProperty(schema.name, "John Doe"); 
```

### Saving Changes
To persist changes in the model, they need to be comitted. Every modification in the resource is temporary 
until the <code>Commit</code> method is called:

```
john.Commit();
```

If the resource has been created by calling it's constructor and not using the <code>IModel.CreateResource</code> method, 
it can be added retroactivly by calling <code>IModel.AddResource</code>. The resulting copy of the resource supports the 
<code>Commit</code> method:

```
IResource john2 = new Resource("ex:john2");
john2 = model.AddResource(john);
john2.Commit();
```

## Object Mapping (ORM)
Trinity RDF offers two ways for defining object mappings. The recommended way is by decorating classes and 
properties using attributes. The mapping is then implemented in a post-compiler step by our byte-code manipulator (cilg.exe).

If for some reasons you cannot do that, you can also implement the mapping manually. In the following we describe both ways.

**Note:** 
Valid types for mapping are all value types, <code>DateTime</code> structs and classes derived from Resource. Additionally, all
collections of these types which implement the <code>IList</code> interface.

### Using Decorators
```
// The class needs to be decorated with the RDF class it is being mapped to.
[RdfClass(SCHEMA.Person)]
public class Person : Agent
{
  [RdfProperty(SCHEMA.givenName)]
  public string FirstName{ get; set; }

  [RdfProperty(SCHEMA.familyName)]
  public string LastName { get; set; }

  // It is important that the constructor with a Uri parameter is implemented.
  public Person(Uri uri) : base(uri) {}
}
```

For the actual mapping of properties, you just need to decorate them with the RDF property you want them to be mapped. The 
getter and setter need to be empty. 

For decoration you need to use the upper case prefix of the ontologies (e.g. <code>SCHEMA</code> instead of <code>schema</code>) 
because C# only accepts string constants in this context.

### Manual Mapping
In environments where you cannot do post-build processing, it can be desirable to use the native mapping mechanism. 
The following example demonstrates how this works:

```
public class Person : Agent
{
    // This method defines the RDF classes the type is mapped to.
    public override IEnumerable<Class> GetTypes()
    {
        yield return schema.Person;
    }

    // Every mapped property needs a PropertyMapping object to store the value. 
	// It also needs the name of the property as well as the mapped RDF property as a parameter.
    private PropertyMapping<string> _firstNameProperty = new PropertyMapping<string>("FirstName", schema.givenName);

    // The getters and setters access the backing field.
    public string FirstName
    {
        get { return GetValue(_firstNameProperty); }
        set { SetValue(_firstNameProperty, value); }
    }

    private PropertyMapping<string> _lastNameProperty = new PropertyMapping<string>("LastName", schema.familyName);

    public string LastName
    {
        get { return GetValue(_lastNameProperty); }
        set { SetValue(_lastNameProperty, value); }
    }

	// It is important that the constructor with a Uri parameter is implemented.
    public Person(Uri uri) : base(uri) {}
}
```

## LINQ
Trinity RDF has built-in support for the [Language Integrated Query (LINQ)](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/) feature of the .NET platform. This works
by translating native LINQ queries into SPARQL queries at runtime.

## Executing Queries
The <code>AsQueryable<T></code> method of the <code>Model</code> class is the entry point for issuing LINQ queries. 
The generic type argument is the type you want to start your query with. This defaults to <code>Resource</code>. The 
method accepts one boolean parameter to enable or disable inferencing with your query:

```
using Semiodesk.Trinity;
using System.Linq;

public class Program
{
	public static void Main()
	{
		IStore store = StoreFactory.CreateMemoryStore();
		IModel model = store.GetModel(new Uri("http://localhost/models/test1"));

		// Execute query without inferencing.
		foreach(Person person in model.AsQueryable<Person>())
		{
			Console.WriteLine(person.Name);
		}

		// Execute query with inferencing.
		foreach(Agent agent in model.AsQueryable<Agent>(true))
		{
			// Includes all the person instances listed above..
			Console.WriteLine(agent.Id);
		}
	}
}
```

## Paged Data Access
Loading a large amount of resources takes some time. In most cases it is not necessary 
to access them all at once but only one at a time. For these cases the data can be loaded 
in chunks. The following example shows how it is done:


```
using Semiodesk.Trinity;
using System.Linq;

public class Program
{
	public static void Main()
	{
		IStore store = StoreFactory.CreateMemoryStore();
		IModel model = store.GetModel(new Uri("http://localhost/models/test1"));

		// Load 100 items per page
		var persons = model.AsQueryable<Person>();

		// Load 10 persons per page.
		var pageSize = 10;

		// Skip one page and load the next 10 persons.
		foreach (Person p in persons.Skip(1 * pageSize).Take(pageSize))
		{
		  Console.WriteLine(p.Name);
		}
	}
}
```