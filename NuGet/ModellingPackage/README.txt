
Semiodesk Trinity Modelling Package
====================================

This document will give you help you getting started using ontologies with the Semiodesk Trinity API.
You can also get further information in the Wiki (https://bitbucket.org/semiodesk/trinity/wiki/Home).

What does this package do?
--------------------------

This package enables the project it has been added to, to manage ontologies and include them in your development process.
It added several things to do that.

1. Ontologies folder with the three most basic ontologies, the OWL, RDF and RDFS
2. For your convenience we have added the OntologyDiscovery.cs file. It contains a static function that needs to be called at 
   the beginning of your program to register the ontologies as well as the mapped classes with the Semiodesk Trinity API.
3. If no App.config or Web.config exists, an App.config file will be created.
4. The App.config or Web.config will be configured for the included ontologies.
5. To deploy the ontologies after the build, you need to supply a connection string. 
   Example:
   <connectionStrings>
    <add name="virt0" providerName="Semiodesk.Trinity" connectionString="provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba"/>
  </connectionStrings>
   

What are the next steps?
------------------------

Decide which backend you want to use. Most use-cases will require the Openlink Virtuoso as triple store. Have a look at the guide
if you don't know how to install it. (https://bitbucket.org/semiodesk/trinity/wiki/SetupVirtuoso)

Don't forget to add the connection string to the config file.

When you have your environment running, find the ontologies that best fit your usecase and add them you your project. The Linked Open Vocabularies page (http://lov.okfn.org)
is a good place to start. With a set of ontologies that define the foundation of your modelling process, you can then start to
describe the specific domain you are working in with your own ontology.



Is there a tutorial to help me getting started?
-----------------------------------------------

Yes! We have prepared some step-by-step guides in the Wiki (https://bitbucket.org/semiodesk/trinity/wiki/FirstSteps).

