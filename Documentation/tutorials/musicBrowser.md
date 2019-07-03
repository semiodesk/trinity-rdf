# Music Browser #

![MusicBrowser.png](https://bitbucket.org/repo/pnBbge/images/2022201284-MusicBrowser.png)

This examples shows you how to access the LinkedBrainz dataset with the Trinity API. 
It uses WinForms as UI toolkit and utilises Data binding.

**Linkedbrainz seems to be offline. Before you try this example check linkedbrainz.org!!**

You can download the finished example from  
[http://static.semiodesk.com/semiodesk.trinity/examples/0.9/MusicBrowser.zip](http://static.semiodesk.com/semiodesk.trinity/examples/0.9/MusicBrowser.zip)

Because of the way NuGet works, there is one step that needs to be completed for the example to work.  Open the Nuget Package Manager console ("Tools" -> "NuGet Package Manager" -> "Package Manager Console") and install Semiodesk.Trinity.Modelling.

```
#!

PM> Install-Package Semiodesk.Trinity.Modelling 
```

## Ontologies and Object Model##

As data model we've used the [Music Ontology](http://purl.org/ontology/mo/), the [FOAF](http://xmlns.com/foaf/0.1/) and the [DCES](http://purl.org/dc/elements/1.1/).

For the object model we have created representations of the artist as well as his created works.
![ClassDiagram.png](https://bitbucket.org/repo/pnBbge/images/225525627-ClassDiagram.png)

||
|---|
| **Note:** As you can see, there is no connection between MusicArtist and Release. In this example we have solved the latency problem by wrapping this request in a separate query. |

## Accessing Sparql Endpoints ##
To query the endpoint we need to create a store with the appropriate parameters.

```
#!csharp
IStore _store = StoreFactory.CreateStore("provider=sparqlendpoint;endpoint=http://linkedbrainz.org/sparql");
IModel _model = _store.GetModel(new Uri("http://linkedbrainz.org/sparql"));

```
We use the Sparql Endpoint provider and configure the endpoint adress at *http://linkedbrainz.org/sparql*.

## Data binding ##
Though a bit rudimentary, data binding is possible in Winforms. We have created two ListBoxes which are displaying the Title property of our resources.
This can be done by setting the *[DisplayMember](https://msdn.microsoft.com/library/system.windows.forms.listcontrol.displaymember%28v=vs.110%29.aspx)* Property of the ListBox.

As datasource we create a VirtualizingResourceCollection and set it to the ListBoxes DataSource.

```
#!csharp
ResourceQuery artistEntity = new ResourceQuery(artist); // Artist is the concrete artist resource.
ResourceQuery madeEntity = new ResourceQuery(mo.Release);
artistEntity.Where(foaf.made, madeEntity);
albumListBox.DataSource = new VirtualizingResourceCollection<Release>(Model, madeEntity);

```

## Loading Data in Parallel ##
We have used the async/await pattern to keep the UI responsive during querying. As threading is not an issue when using a SPARQL endpoint there is no need to worry about connection pooling.