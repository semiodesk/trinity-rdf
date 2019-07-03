# Web Application #
This example demonstrate how to use Semiodesk Trinity with ASP.NET MVC 5 and SignalR. The resulting application is able to generate random people and connections between them. The web page visualizes the dependencies in a graph with [D3js](https://d3js.org/).

![result.png](https://bitbucket.org/repo/pnBbge/images/1927643689-result.png)

You can download the finished example from  
[http://static.semiodesk.com/semiodesk.trinity/examples/0.9/WebApp.zip](http://static.semiodesk.com/semiodesk.trinity/examples/0.9/WebApp.zip)

Because of the way NuGet works, you have to add the Semiodesk Trinity packages manually.  Open the Nuget Package Manager console ("Tools" -> "NuGet Package Manager" -> "Package Manager Console") and install Semiodesk.Trinity.Modelling to the DataModel Project and Semiodesk.Trinity.Core to the EbApp.

```
#!

PM> Install-Package Semiodesk.Trinity.Modelling -ProjectName DataModel

PM> Install-Package Semiodesk.Trinity.Core -ProjectName WebApp
```

Also, you might need to configure the Virtuoso connection string in the Web.config file appropriately.

## Architecture and Object Model Overview ##
To separate the data model from the application I have created a project that only contains the ontologies and the mapped classes.
To keep things simple, I used the [foaf](http://xmlns.com/foaf/spec/) ontology again. The mapping is nearly the same as the [first steps example](https://bitbucket.org/semiodesk/trinity/wiki/FirstSteps).

As we want to serialize our objects to Json we need to change it a bit. Serializers often have limitation when it comes to possible dependency cycles. We need to handle the Knows relationship differently than before. To prevent loops we hide the Knows property from the serializer with *JsonIgnore* and create a new property that only exposes the URIs of the related objects. That way we can still access the objects, but only when we actively decide to do so.


```
#!csharp
[RdfProperty(FOAF.knows), JsonIgnore]
public List<Person> Knows { get; set; }

public IEnumerable<Uri> knows
{
    get { return from x in Knows select x.Uri; }
}
```

To make the data available, I have implemented the [repository pattern](http://martinfowler.com/eaaCatalog/repository.html) under *Models/ResourceRepository.cs*. This is used to create an additional abstraction layer between the database and it's peculiarities and the application logic.

## Displaying the Data ##

The actual data is not rendered directly to html, but queried by the Javascript code that is deployed using ASP.NET. Creating a data backend is very easy using the aforementioned repository pattern and [SignalR](http://signalr.net/). This has the advantage that the data can be loaded asynchronously, while the web page is already displayed.

The following diagram shows the full process how the data is acquired.
![process.png](https://bitbucket.org/repo/pnBbge/images/698958943-process.png)

The result is then transformed to create a visualisation with D3.

## Checklist ##
Things to consider when you build a new project with ASP.NET and Trinity:

* Add *SemiodeskDiscovery.Discover();* to Global.asax.cs -- Application_Start()
* Get *ResourceRepository.cs* and *StoreFactory.cs* from this example, they are generic and make your life easier.
* While creating the mapping classes, think about serialization issues.
* If you have trouble with SignalR, use 
```
#!javascript

$.connection.hub.logging = true;
```



