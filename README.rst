========
Overview
========

Semiodesk.Trinity is an application development platform for Microsoft .NET and Mono.
It allows to easily build Linked Data and Semantic Web applications based on the RDF metadata standard issued by the W3C.
The API allows for developing first-class .NET applications with direct access to Linked Open Data repositories and knowledge bases such as DBPedia, Freebase, Geonames, BBC Business News, BBC Sports and many more.

Our platform is built on top of the powerful and stable `dotNetRDF`_  library which has been in development since early 2009.
Since dotNetRDF is low-level and primarliy focused on directly manipulating triples, it does not integrate well with existing application frameworks and introduces a steep learning curve for new developers.
Therefore, our primary goal was to allow developers to use proven enterprise development patterns such as MVC or MVVM to build Linked Data applications that integrate well into existing application eco-systems.

The software is supported by `Semiodesk`_.
If you have any questions, suggestions or just want to tell us in which projects you are using the library, don't hesitate to `contact us`_.


Features
========

Semantic Object Mapping
-----------------------
Similar to the OR-mapping technique for relational databases, we provide a way to map RDFS/OWL classes to C# classes.
This makes creating new resources and and adding properties a breeze. 

Additional to the convenience it brings, the mapping also lets you utilize the type inheritance of RDF in your C# classes.
Suppose you have PersonContact and CompanyContact resources in your model, both are derived from the Contact class.
With Trinity you can query for the base type and you will get all derived types in your results as well. Use the power of inheritance in your data modelling! 

Ontology Classes
----------------
The Trinity platform offers a way to work with ontologies directly in the IDE. We have developed a set of tools to help you integrate the modelling made in ontologies directly into your project.

A small code generator transforms RDFS and OWL ontologies into C# representations.
With these the IDE, for example Visual Studios IntelliSense, can offer you autocompletion as well as documentation hints for the ontologies you are working with.
Additionally this process can be integrated into the build process, that way the code stays up to date with the ontologies.

A different tool deploys the ontologies to your triple store. If you are working on your own ontologies, you can integrate this process into your toolchain. 

Query API
---------
Writing inline queries can easily result in errors. Native data types need to be serialized manually by the developer each time. With Trinitys Native Language Query system this work is done by the API.

Data Virtualization
-------------------
For user interfaces to stay responsive, it is necessary to load the data asynchronously.
We employ a data virtualization strategy to mitigate that problem for the application developer.
With this the data can be visualized step-by-step as it comes from the data store.

Because RDF models are graphs, resources are connected either directly or indirectly to a potentially large number of other resources.
With a lazy loading mechanism, the related resources are only loaded when accessed. 

Various Backends
----------------
The Trinity API has built-in support for three store types. 
We offer an in-memory store to get started quick and without much overhead. 
For more sophisticated applications we support the use of OpenLinks Virtuoso database. 
To access data from the Linked Open Data cloud, Trinity is able to connect to SPARQL endpoints. 
Additional stores can be used trough external modules. 


License
=======
The library and tools in this repository are all released under the terms of the `MIT license`_. 
This means you can use it for every project you like, even commercial ones, as long as you keep the copyright header intact. 
The source code, documentation and issue tracking can be found at our bitbucket page. 
If you like what we are doing and want to support us, please consider donating.

Dependencies
============
The Semiodesk.Trinity API has dependencies to 

* `dotNetRDF`_ 
* `OpenLink.Data.Virtuoso`_

The libraries are included in the release package. If you install via NuGet the depencies should be resolved for you.

Installation
============
The easiest way to start using the Trinity API is to add it to your project trough NuGet.

  PM> Install-Package Semiodesk.Trinity

Getting Started
===============
After the installation our `First Steps`_ guide should help you getting started.


Support
=======


References
==========




.. GENERAL LINKS

.. _`triplestores`: http://en.wikipedia.org/wiki/Triplestore
.. _`MIT license`: http://en.wikipedia.org/wiki/MIT_License
.. _`Semiodesk`: http://www.semiodesk.com
.. _`contact us`: mailto:hello@semiodesk.com
.. _`Unity3D`: https://unity3d.com/
.. _`dotNetRDF`: http://dotnetrdf.org/
.. _`OpenLink.Data.Virtuoso`: https://github.com/openlink/virtuoso-opensource
.. _`First Steps`: https://bitbucket.org/semiodesk/semiodesk.trinity/wiki/FirstSteps