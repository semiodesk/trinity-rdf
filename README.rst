========
Overview
========

Semiodesk Trinity is an application development platform for Microsoft .NET and Mono.
It allows to easily build Linked Data and Semantic Web applications based on the `RDF metadata standard`_ issued by the W3C.
The API allows for developing first-class .NET applications with direct access to Linked Open Data repositories and knowledge graphs.

Our platform is built on top of the powerful and stable `dotNetRDF`_  library which has been in development since early 2009.
Since dotNetRDF is low-level and primarliy focused on directly manipulating triples, it does not integrate well with existing application frameworks and introduces a steep learning curve for new developers.
Therefore, our primary goal was to allow developers to use proven enterprise development patterns such as MVC or MVVM to build Linked Data applications that integrate well into existing application eco-systems.

The software is supported by `Semiodesk`_, for more information have a look at the `product page`_.
If you have any questions, suggestions or just want to tell us in which projects you are using the library, don't hesitate to `contact us`_.

Installation
============
The easiest way to start using the Trinity API is to add it to your project trough NuGet.

  PM> Install-Package Semiodesk.Trinity

Getting Started
===============
After the installation our `First Steps`_ guide should help you getting started.

Features
========

Semantic Object Mapping
-----------------------
Similar to OR-mapping techniques for relational databases, we provide a way to map RDFS / OWL terms to .NET objects. 
Using byte-code manipulation, we implement the code required for the mapping during program compilation. This results in 
higher performance and less runtime errors than solutions based on introspection.

Ontology Classes
----------------
The Trinity platform offers a way to work with ontologies directly in the IDE. We have developed a set of tools to help 
you integrate ontology concepts directly into your project.

A small code generator transforms RDFS and OWL ontologies into C# representations. With these the IDE, for example 
Visual Studios IntelliSense, can offer you autocompletion as well as documentation hints for the ontologies you are working with.
Additionally this process can be integrated into the build process, that way the code stays up to date with the ontologies.

A different tool deploys the ontologies to your triple store. If you are working on your own ontologies, you can integrate 
this process into your toolchain. 

LINQ
---------
Not familiar with SPARQL? No problem. The powerful LINQ to SPARQL translator in Trinity RDF lets you query your RDF knowledge 
graph using LINQ.

Data Virtualization
-------------------
Knowledge graphs can become large datasets quite easily. In order to maintain application performance and user interface 
responsiveness, Trinity RDF makes paged queries to your datasets easy. Simply use the Take and Skip operators in your LINQ queries 
and you're done. Of course, SPARQL has built-in support for paging if you are creating more advanced queries.

Various Backends
----------------
The Semiodesk Trinity API has built-in support for three store types. 
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
* `Newtonsoft JSON`_
* `Remotion LINQ`_

The libraries are included in the release package. If you install via NuGet the depencies should be resolved for you.

Support
=======
If you need help or want priority support, contact us under `hello@semiodesk.com`_.


.. GENERAL LINKS
.. _`triplestores`: http://en.wikipedia.org/wiki/Triplestore
.. _`MIT license`: http://en.wikipedia.org/wiki/MIT_License
.. _`Semiodesk`: https://www.semiodesk.com
.. _`product page`: https://trinity-rdf.net
.. _`contact us`: mailto:hello@semiodesk.com
.. _`hello@semiodesk.com`: mailto:hello@semiodesk.com
.. _`Unity3D`: https://unity3d.com/
.. _`dotNetRDF`: http://dotnetrdf.org/
.. _`OpenLink.Data.Virtuoso`: https://github.com/openlink/virtuoso-opensource
.. _`First Steps`: https://trinity-rdf.net/doc/tutorials/firstSteps.html
.. _`RDF metadata standard`: https://w3.org/rdf
.. _`Newtonsoft JSON`: https://www.newtonsoft.com/json
.. _`Remotion LINQ`: https://github.com/re-motion/Relinq