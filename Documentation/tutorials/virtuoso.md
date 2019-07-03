# Set up a Virtuoso instance

Many features of the Semiodesk.Trinity API are geared towards the [Virtuoso Database](https://github.com/openlink/virtuoso-opensource). If you create a quick installation to get you started then just follow the instructions. The process is a bit different depending on the platform you are on.

## Windows ##

**1. Get the latest binary distribution**  
    You can find the pre-build package on this [page](http://virtuoso.openlinksw.com/dataspace/doc/dav/wiki/Main/VOSDownload).

**2. Unzip it to your desired location**  
    This is where your Virtuoso installation will reside, so pick a sensible directory. Inside this directory, you will have the following folders:  
    *bin* contains the binaries  
    *database* contains an example configuration  
    *doc* contains the documentation  
    *hosting* additional modules  
    *lib* the libraries to access the database  
    *vad* VAD packages BPEL, Conductor, tutorials, documentation   


**3.  Set up the configuration**  
    To get started quick, you can just use the example configuration in the *database* folder.  

**4.  Starting Virtuoso**  
    Here we have two options, the quickstart which creates a Virtuoso instance that stops once the console window is closed and installing Virtuoso as a Windows Service.  

*a. Quickstart*  
Open a console in your Virtuoso directory and start it with the following command:  

```
bin\virtuoso-t.exe -f -c database/virtuoso.ini
```  

Closing the console or pressing ctrl-c will stop the database server.

*b. Installing Virtuoso as a Service*  
This process takes a few more steps, but the Server will automatically be started when the computer is restarted.  
First you need a console with administration rights. The easiest way to get one is to open the start menu, type in "cmd.exe" and press ctrl-shift-enter. You then need to navigate into your virtuoso installation directory.  
To install the Service, enter
```
bin\virtuoso-t.exe +service screate -I "My Virtuoso Server" -c database/virtuoso.ini
``` 

To start the Service, enter
```
bin\virtuoso-t.exe +service start -I "My Virtuoso Server"
``` 

If you want to remove it from your system, you can use
```
bin\virtuoso-t.exe +service delete -I "My Virtuoso Server"
```

**5. Testing if everything works**

Navigate to [http://localhost:8890/conductor/](http://localhost:8890/conductor/) and try to login with dba/dba. If everything works you should now have a running Virtuoso server.

**6.  Using Virtuoso with the Semiodesk.Trinity API** 

When you use the default configuration, you can use the following configuration string 

*"provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba"* 

with the Semiodesk.Trinity.
To create the store, use the following snippet:
```
#!c#
IStore store = Stores.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");

```



    