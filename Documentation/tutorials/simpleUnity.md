# Unity 3D Game #
[Download Project](http://static.semiodesk.com/semiodesk.trinity/examples/0.9/SimpleDbpedia.zip)


We look at how, in general, the Trinity RDF platform can be used in combination with the [Unity gaming engine](https://unity.com).

#### Required Knowledge

* Basic knowledge about Unity 3D
* Knowledge about C# and threading (BackgroundWorker)

#### Technical Restrictions

* Object Mapping should be done in a separate project
* .NET 3.5 only (because of Unity)

## Goals ##
This is a small 'game' which lists all books of a series on the press of a button. In 
this example it's the 'Lord of the Rings'. It follows the links of the subsequent works. 
We start out with a hard coded link to the book '[The Fellowship of the Ring](http://dbpedia.org/page/The_Fellowship_of_the_Ring)'. 
The property [dbo:subsequentWork](http://dbpedia.org/ontology/subsequentWork) links all 
three books of the trilogy together.

## Challenges ##
DBpedia data does not always follow ontologies. We use for this example the Lord of the 
Rings trilogy. If we were to exchange that for Harry Potter books, it doesn't work because 
the links are not there.

## Setup ##
Open the Nuget Package Manager console (Tools -> NuGet Package Manager -> Package Manager Console) 
and type the following:

```
PM> Install-Package Trinity.RDF
```

Make a release build before you open the project in Unity.
You can find the scene under *Assets/Scenes/Simple.scene*.

## Getting Started ##
First you need to set up a new Unity 3D Project. Next to the Assets folder you can put a new folder for 
the ontology mapping project. I usually call it External. Here we create a new C# Library project 
(create it either in Visual Studio or XamarinStudio/Monodevelop). To make the results of this projects 
available our Unity projects, we need to set the build output (preferably of the release build) to *\..\\..\\Assets\\Plugins\\*
The Plugins directory is a [special folder](http://docs.unity3d.com/Manual/SpecialFolders.html) of Unity which treats the contained DLLs differently.

![FolderStructure.png](https://bitbucket.org/repo/pnBbge/images/1059885734-FolderStructure.png)

## DBpedia Ontology ##
I have picked up the DBpdia ontology [here](http://downloads.dbpedia.org/2015-04/dbpedia_2015-04.nt.bz2). The 
next step is to extract it to the ontologies directory.

Then we need the [foaf ontology](http://xmlns.com/foaf/spec/index.rdf) which I stored as foaf.rdf in the 
ontologies directory. Then we can add the following part to the *OntologySettings* section in the App.config

```
      <Ontology Uri="http://dbpedia.org/ontology/" Prefix="dbo">
        <FileSource Location="Ontologies\dbpedia_2015-04.nt"/>
      </Ontology>
      <Ontology Uri="http://xmlns.com/foaf/0.1/" Prefix="foaf">
        <FileSource Location="Ontologies\foaf.rdf" />
      </Ontology>
```

## Object Mapping ##
Now we add the C# classes for the object mappings we want. For example we want information about written works:

```
    [RdfClass(DBO.WrittenWork)]
    public class WrittenWork : Resource
    {
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

        #region Constructors

        public WrittenWork(Uri uri) : base(uri) {}

        public WrittenWork(Resource other) : base(other) {}

        public WrittenWork(string uriString) : base(uriString) {}

        #endregion
    }
```

To get information about the author, we need a mapping for foaf:Person

```
    [RdfClass(FOAF.Person)]
    public class Person : Resource
    {
		#region Properties

	    [RdfProperty(FOAF.name)]
        public string Name { get; set; }

        [RdfProperty(FOAF.surname)]
        public string Surname { get; set; }

        [RdfProperty(FOAF.givenname)]
        public string GivenName { get; set; }

		#endregion

	    #region Constructors

        public Person(Uri uri) : base(uri) {}

		#endregion
    }

```

For convenience we also create a 'DataStore' class which connects to the store on creation.


```
#!csharp
 public class DataStore
    {
        #region Members

        private readonly Uri _endpoint = new Uri("http://live.dbpedia.org/sparql");

        private IStore _store;

        public IStore Store { get { return _store; } }

        #endregion

        #region Constructors

        public DataStore()
        {
            SemiodeskDiscovery.Discover();

            _store = StoreFactory.CreateSparqlEndpointStore(_endpoint);
        }

        #endregion
    }

```

## Unity 3D Integration ##
To get Trinity RDF running in Unity 3D, you first need to set the *Api Compatibility Level* to **.NET 2.0 Subset**. 
For this, open the Player settings in Unity3D (Edit -> Project Settings -> Player) and change the appropriate entry.
When the DataModel project is now being built in Release mode, Unity should automatically pick it up and make it 
available for scripting.

## Adding Simple Interaction ##
For this example we just need a Canvas with a Button and a Text control. Add them to your scene 
(GameObject -> UI -> Canvas / Button / Text). Now we add the querying mechanism by adding a script 
to the canvas. For this, just select the canvas and in the Inspector click *Add Component* -> *New Script* 
and make sure Csharp is selected. We call this script *LoadData* and add the following code to it.

```
using UnityEngine;
using UnityEngine.UI;
using DataModel;
using Semiodesk.Trinity;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

public class LoadData : MonoBehaviour 
{
	#region Members

	private DataStore _store;

	private IModel _model;

	private BackgroundWorker _worker;

	private readonly Queue<Action> _actions = new Queue<Action>();

	private WrittenWork _currentBook;

	private Uri _targetBook;

	public Text Target;

	#endregion

	#region Methods

	// Use this for initialization.
	void Start() 
	{
		_targetBook = new Uri("http://dbpedia.org/resource/The_Fellowship_of_the_Ring");
		_store = new DataStore();
		_model = _store.Model;

		_worker = new BackgroundWorker();
		_worker.DoWork += (object sender, DoWorkEventArgs e) => ExecuteLoad();
	}
	
	// Update is called once per frame.
	void Update() 
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
		_worker.RunWorkerAsync();
	}

	void ExecuteLoad()
	{
		if (_currentBook == null) 
		{
			_currentBook = _model.GetResource<WrittenWork>(_targetBook, null);
		}
		else
		{
			var works = _currentBook.SubsequentWork;

			if( works.Count > 0 )
			{
				_currentBook = works[0];
			}
		}

		if (_currentBook != null) 
		{
			lock(_actions)
			{
				_actions.Enqueue(new Action (() => Target.text = _currentBook.Name));
			}
		}
	}

	void OnDestroy()
	{
		_worker.Dispose();
	}

	#endregion
}

```

Now we just need to set the Button to call the appropriate function. Select it and set the 
*On Click()* handler like in the following image.

![onclick.png](https://bitbucket.org/repo/pnBbge/images/397689992-onclick.png)

**Note:** Even tough the setting of *Target.text* does not seem like a call to Unity method, 
it could be property that calls methods which are not allowed to be called from a thread. 
To be safe, relay as much as possible back to the UI thread.

## The Result ##
You now have a small game that gives you the title of all three parts of The Lord of the Rings 
if you press the button repeatedly.

![result.png](https://bitbucket.org/repo/pnBbge/images/932505194-result.png)

Hurray! :-)