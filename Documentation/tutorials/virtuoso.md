# OpenLink Virtuoso
Many features of the Trinity RDF are geared towards the [OpenLink Virtuoso](https://github.com/openlink/virtuoso-opensource) 
database. If you create a quick installation to get you started then just follow these instructions.

## Windows ##
### 1. Obtaining Virtuoso
You can download a pre-build OpenLink Virtuoso version [here](http://virtuoso.openlinksw.com/dataspace/doc/dav/wiki/Main/VOSDownload).

### 2. Unzip Package
This is where your Virtuoso installation will reside, so pick a sensible directory. Inside 
this directory, you will have the following folders:

| Folder        | Description   |
| --- | --- |
| <code>bin</code> | Contains appliation binaries. |
| <code>database</code> | Contains an example configuration. |  
| <code>doc</code> | Contains documentation. |
| <code>hosting</code> | Additional modules. |
| <code>lib</code> | The libraries to access the database. |
| <code>vad</code> | VAD packages BPEL, Conductor, tutorials, documentation. |

### 3. Configuring
To get started quickly, you can just use the example configuration in the <code>database</code> folder.  

### 4. Starting Virtuoso 
Here we have two options, the quickstart which creates a Virtuoso instance that stops once the console 
window is closed and installing Virtuoso as a Windows Service.  

#### a. Console
Open a console in your Virtuoso directory and start it with the following command:  

```
bin\virtuoso-t.exe -f -c database/virtuoso.ini
```  

Closing the console or pressing <code>CTRL+C</code> will stop the database server.

#### b. Windows Service
This process takes a few more steps, but the Server will automatically be started when the computer is restarted.  

First you need a console with administration rights. The easiest way to get one is to open the start menu, type 
in <code>cmd.exe</code> and press <code>CTRL+SHIFT+ENTER</code>. You then need to navigate into your Virtuoso installation directory.

To **install** the service, enter
```
bin\virtuoso-t.exe +service screate -I 'My Virtuoso Server' -c database/virtuoso.ini
``` 

To **start** the service, enter
```
bin\virtuoso-t.exe +service start -I 'My Virtuoso Server'
``` 

If you want to **delete** it from your system, you can use
```
bin\virtuoso-t.exe +service delete -I 'My Virtuoso Server'
```

### 5. Testing
Navigate to [http://localhost:8890/conductor/](http://localhost:8890/conductor/) and try 
to login with the credentials <code>dba/dba</code>. If everything works you should now have a running 
Virtuoso server.

### 6. Using Virtuoso
When you use the default configuration, you can use the following configuration string:

```
provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba
```

To create the store, use the following snippet:

```
IStore store = StoreFactory.CreateStore("provider=virtuoso;host=localhost;port=1111;uid=dba;pw=dba");
```
