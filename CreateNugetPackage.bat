Net40\.nuget\nuget.exe restore Net40\semiodesk.trinity.sln 
MSBuild.exe /p:Configuration=Release /t:Rebuild "/p:Platform=Any CPU" Net40/Semiodesk.Trinity.sln

Net40\.nuget\nuget.exe restore Net35\semiodesk.trinity-net35.sln 
MSBuild.exe /p:Configuration=Release /t:Rebuild "/p:Platform=Any CPU" Net35/Semiodesk.Trinity-Net35.sln

mkdir NuGet\CorePackage\lib\Net35\
copy /y Build\Net35\Release\Semiodesk.Trinity.dll NuGet\CorePackage\lib\Net35\Semiodesk.Trinity.dll
copy /y Build\Net35\Release\Semiodesk.Trinity.pdb NuGet\CorePackage\lib\Net35\Semiodesk.Trinity.pdb

mkdir NuGet\CorePackage\lib\Net40\
copy /y Build\Net40\Release\Semiodesk.Trinity.dll NuGet\CorePackage\lib\Net40\Semiodesk.Trinity.dll
copy /y Build\Net40\Release\Semiodesk.Trinity.pdb NuGet\CorePackage\lib\Net40\Semiodesk.Trinity.pdb

xcopy /y Build\Net40\Release\*.exe NuGet\ModellingPackage\tools\net40
xcopy /y Build\Net40\Release\*.dll NuGet\ModellingPackage\tools\net40

xcopy /y Build\Net35\Release\*.exe NuGet\ModellingPackage\tools\net35
xcopy /y Build\Net35\Release\*.dll NuGet\ModellingPackage\tools\net35


Net40\.nuget\nuget.exe pack NuGet\ModellingPackage\Modelling.nuspec
Net40\.nuget\nuget.exe pack NuGet\CorePackage\Core.nuspec