# Simple Unity Example #

There are a number of use cases where semantic web technology makes a lot of sense in a gaming engine. In this first example we do not want bother ourselves to much with persistence. We look at how, in general, the Semiodesk Trinity platform can be used in combination with the Unity3d framework. 
A few restrictions apply:

- Modelling/Object Mapping should be done in a separate project
- .Net 3.5 only

Requirements for this example:

- Basic knowledge about Unity3D
- Knowledge about C# and threading (BackgroundWorker)

## What does this Example do ##
This is a small "game" which lists all books of a series on the press of a button. In this example it's the Lord of the Rings. It follows the links of the subsequent works. We start out with a hard coded link to the book "[The Fellowship of the Ring](http://dbpedia.org/page/The_Fellowship_of_the_Ring)". The property "[dbo:subsequentWork](http://dbpedia.org/ontology/subsequentWork)" links all three books of the trilogy together.

**What are the problem of this example?**

Dbpedia data does not always follow ontologies. We use for this example the Lord of the Rings trilogy. If we were to exchange that for Harry Potter books, it doesn't work because the links are not there. Well, thats generated data for you.

## Downloading the finished Project ##
I know, a lot of people want to skip ahead and see what the result looks like. You can download the result from the following link:

[http://static.semiodesk.com/semiodesk.trinity/examples/0.9/SimpleDbpedia.zip](http://static.semiodesk.com/semiodesk.trinity/examples/0.9/SimpleDbpedia.zip)

Because of the way NuGet works, there is one step that needs to be completed for the example to work.  Open the Nuget Package Manager console ("Tools" -> "NuGet Package Manager" -> "Package Manager Console") and install Semiodesk.Trinity.Modelling.

```
#!

PM> Install-Package Semiodesk.Trinity.Modelling -source http://nuget.semiodesk.com/api/v2/
```
Make a release build before you open the project in Unity.
You can find the scene under *Assets/Scenes/Simple.scene*.

## Getting started ##

First you need to set up a new Unity3d Project. Next to the Assets folder you can put a new folder for the ontology mapping project. I usually call it External. Here we create a new C# Library project (create it either in Visual Studio or XamarinStudio/Monodevelop). To make the results of this projects available our Unity projects, we need to set the build output (preferably of the release build) to *\..\\..\\Assets\\Plugins\\*
The Plugins directory is a [special folder](http://docs.unity3d.com/Manual/SpecialFolders.html) of unity which treats the contained DLLs differently.

![FolderStructure.png](https://bitbucket.org/repo/pnBbge/images/1059885734-FolderStructure.png)

## Adding Semiodesk Trinity ##

Now we can add the Semiodesk.Trinity.Modelling package to the project by executing the following command in the Package Manager Console (*Tools -> NuGet Package Manager -> Package Manager Console*).

||
|---|
| **IMPORTANT:** We currently have some issues with our dependencies. The current version of OpenLink.Data.Virtuoso.dll is not comaptible with Mono. This is why we decided to switch back to the old version. As this change is not in the release branch at the moment, it is neccessary to use the development branch.  |

```
#!
PM> Install-Package Semiodesk.Trinity.Modelling -source http://nuget.semiodesk.com/api/v2/
```

Then you need to manually remove the references to virtado3, dotNetRDF.Data.Virtuoso and OpenLink.Data.Virtuoso as these are not compatible with Unity3D.


## Adding the dbpedia ontology ##

I have picked up the dbpdia ontology from this [link](http://downloads.dbpedia.org/2015-04/dbpedia_2015-04.nt.bz2). The next step is to extract it to the ontologies directory.
Then we need the [foaf ontology](http://xmlns.com/foaf/spec/index.rdf) which I stored as foaf.rdf in the ontologies directory. Then we can add the following part to the *OntologySettings* section in the App.config

```
#!xml

      <Ontology Uri="http://dbpedia.org/ontology/" Prefix="dbo">
        <FileSource Location="Ontologies\dbpedia_2015-04.nt"/>
      </Ontology>
      <Ontology Uri="http://xmlns.com/foaf/0.1/" Prefix="foaf">
        <FileSource Location="Ontologies\foaf.rdf" />
      </Ontology>
```

## Create object mapping ##
Now we add the C# classes for the mappings we want.
For example we want information about written works:

```
#!csharp

    [RdfClass(DBO.WrittenWork)]
    public class WrittenWork : Resource
    {
        #region Constructor
        public WrittenWork(Uri uri)
            : base(uri)
        {}

        public WrittenWork(Resource other)
            : base(other)
        {}

        public WrittenWork(string uriString)
            : base(uriString)
        {}
        #endregion

        #region Properties

        [RdfProperty(FOAF.name)]
        public string Name { get; set; }

        [RdfProperty(DBO.previousWork)]
        public List<WrittenWork> PreviousWork { get; set; }

        [RdfProperty(DBO.subsequentWork)]
        public List<WrittenWork> SubsequentWork { get; set; }

        [RdfProperty(DBO.author)]
        public Person Author { get; set; }


        #endregion
    }
```

To get information about the author, we need a mapping for foaf:Person

```
#!csharp
    [RdfClass(FOAF.Person)]
    public class Person : Resource
    {
        public Person(Uri uri) : base(uri) 
        {}

        [RdfProperty(FOAF.name)]
        public string Name { get; set; }

        [RdfProperty(FOAF.surname)]
        public string Surname { get; set; }

        [RdfProperty(FOAF.givenname)]
        public string GivenName { get; set; }
    }

```

For convenience we also create a DataStore class which connects to the store on creation.


```
#!csharp
 public class DataStore
    {
        #region Members
        Uri _endpoint = new Uri("http://live.dbpedia.org/sparql");
        IStore _store;

        public IStore Store { get { return _store; } }
        #endregion

        #region Constructor
        public DataStore()
        {
            SemiodeskDiscovery.Discover();
            _store = StoreFactory.CreateStore("provider=sparqlendpoint;endpoint=http://live.dbpedia.org/sparql");
        }
        #endregion
    }

```

## Integration into Unity3d ##
To get the Trinity running in Unity3D you first need to set the *Api Compatibility Level* to **.NET 2.0 Subset**. For this, open the Player settings in Unity3D (Edit -> Project Settings -> Player) and change the appropriate entry.
When the DataModel project is now being built in release mode, Unity should automatically pick it up and make it available for scripting.

## Adding a simple interaction ##
For this example we just need a Canvas with a Button and a Text control. Add them to your scene (GameObject -> UI -> Canvas / Button / Text). Now we add the querying mechanism by adding a script to the canvas. For this, just select the canvas and in the Inspector click *Add Component* -> *New Script* and make sure Csharp is selected. We call this script *LoadData* and add the following code to it.

```
#!csharp

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DataModel;
using Semiodesk.Trinity;
using System;
using System.ComponentModel;
using System.Collections.Generic;

public class LoadData : MonoBehaviour 
{
	public Text Target;

	DataStore _store;
	IModel _model;
	BackgroundWorker _worker;
	Queue<Action> _actions = new Queue<Action>();

	WrittenWork _currentBook;
	Uri _targetBook;


	// Use this for initialization
	void Start () 
	{
		_targetBook = new Uri ("http://dbpedia.org/resource/The_Fellowship_of_the_Ring");
		_store = new DataStore ();
		_model = _store.Model;

		_worker = new BackgroundWorker ();
		_worker.DoWork += (object sender, DoWorkEventArgs e) => ExecuteLoad();
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		lock (_actions) 
		{
			if( _actions.Count > 0 )
			{
				_actions.Dequeue().Invoke();
			}
		}
	}

	public void StartLoading()
	{
		_worker.RunWorkerAsync ();
	}

	void ExecuteLoad()
	{
		if (_currentBook == null) 
		{
			_currentBook = _model.GetResource<WrittenWork> (_targetBook, null);
		}
		else 
		{
			var works = _currentBook.SubsequentWork;
			if( works.Count > 0 )
				_currentBook = works[0];
		}
		if (_currentBook != null) 
		{
			lock(_actions)
			{
				_actions.Enqueue (new Action (() => Target.text = _currentBook.Name));
			}
		}
	}

	void OnDestroy()
	{
		_worker.Dispose ();
	}

}

```

Now we just need to set the Button to call the appropriate function. Select it and set the *On Click()* handler like in the following image.

![onclick.png](https://bitbucket.org/repo/pnBbge/images/397689992-onclick.png)

||
|---|
| **Note:** Even tough the setting of *Target.text* does not seem like a call to Unity method, it could be property that calls methods which are not allowed to be called from a thread. To be safe, relay as much as possible back to the UI thread.   |

## The result ##

You now have a small game that gives you the title of all three parts of The Lord of the Rings if you press the button repeatedly.

![result.png](https://bitbucket.org/repo/pnBbge/images/932505194-result.png)

Yay for Semantic Web!
