
Semiodesk.Trinity.Ontologies Package
====================================

This document will give you help you getting started using ontologies with the Semiodesk.Trinity API.
You can also get further information in the Wiki (https://bitbucket.org/semiodesk/semiodesk.trinity/wiki/Home).

What does this package do?
--------------------------

This package enables the project it has been added to, to manage ontologies and include them in your development process.
It added several things to do that.

1. Ontologies folder with the three most basic ontologies, the OWL, RDF and RDFS
2. Ontologies.cs is the file which gets generated from these ontologies every build. This contains a very basic C# representation
   of the ontologies.
3. GeneratorConfig.xml contains the settings for the generation process. If you add new ontologies you have to add them to the
   configuration as well. Here you can also change the namespace of generated file if you like.
4. The prebuild command "OntologyGenerator.exe -c $(ProjectDir)GeneratorConfig.xml -g $(ProjectDir)Ontologies.cs" which triggers 
   the generation process. If you want to rename the configuration or the generated file, you have to change the command as well.
5. For your convenience we have added the OntologyDiscovery.cs file. It contains a static function that needs to be called at 
   the beginning of your program to register the ontologies as well as the mapped classes with the Semiodesk.Trinity API.
6. To be able to do inferencing, the ontologies must be deployed to the triple store, the prebuild command
   "OntologyDeployment.exe -c $(ProjectDir)DeploymentConfig.xml -o ''" does that for you. 
   ATTENTION: You have to configure the -o parameter with your actual connection string. Please note that, this currently makes
   only sense if you are using Virtuoso as backend.
7. The file DeploymentConfig.xml needs to be modified so that all your ontologies get deployed. You also need to set your 
   inferencing rules in there.

What are the next steps?
------------------------

Decide which backend you want to use. Most use-cases will require the Openlink Virtuoso as triple store. Have a look at the guide
if you don't know how to install it. (https://bitbucket.org/semiodesk/semiodesk.trinity/wiki/SetupVirtuoso)

Don't forget to add the connection string to the prebuild command.

When you have your environment running, find the ontologies that best fit your usecase and add them you your project. The Linked Open Vocabularies page (http://lov.okfn.org)
is a good place to start. With a set of ontologies that define the foundation of your modelling process, you can then start to
describe the specific domain you are working in with your own ontology.



Is there a tutorial to help me getting started?
-----------------------------------------------

Yes! We have prepared some step-by-step guides in the Wiki (https://bitbucket.org/semiodesk/semiodesk.trinity/wiki/FirstSteps).

