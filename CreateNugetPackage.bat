Net40\.nuget\nuget.exe restore Net40\semiodesk.trinity.sln 
MSBuild.exe /p:Configuration=Release /t:Rebuild "/p:Platform=Any CPU" Net40/Semiodesk.Trinity.sln

Net40\.nuget\nuget.exe restore Net35\semiodesk.trinity-net35.sln 
MSBuild.exe /p:Configuration=Release /t:Rebuild "/p:Platform=Any CPU" Net35/Semiodesk.Trinity-Net35.sln

mkdir NuGet\CorePackage\lib\Net35\
copy /y Build\Net35\Release\Semiodesk.Trinity.dll NuGet\CorePackage\lib\Net35\Semiodesk.Trinity.dll
copy /y Build\Net35\Release\Semiodesk.Trinity.pdb NuGet\CorePackage\lib\Net35\Semiodesk.Trinity.pdb
copy /y Build\Net35\Release\Semiodesk.dotNetRDF.Data.Virtuoso.dll NuGet\CorePackage\lib\Net35\dotNetRDF.Data.Virtuoso.dll
copy /y Build\Net35\Release\dotNetRDF.dll NuGet\CorePackage\lib\Net35\dotNetRDF.dll
copy /y Build\Net35\Release\HtmlAgilityPack.dll NuGet\CorePackage\lib\Net35\HtmlAgilityPack.dll
copy /y Build\Net35\Release\OpenLink.Data.Virtuoso.dll NuGet\CorePackage\lib\Net35\OpenLink.Data.Virtuoso.dll
copy /y Build\Net35\Release\VDS.Common.dll NuGet\CorePackage\lib\Net35\VDS.Common.dll

mkdir NuGet\CorePackage\lib\Net40\
copy /y Build\Net40\Release\Semiodesk.Trinity.dll NuGet\CorePackage\lib\Net40\Semiodesk.Trinity.dll
copy /y Build\Net40\Release\Semiodesk.Trinity.pdb NuGet\CorePackage\lib\Net40\Semiodesk.Trinity.pdb
copy /y Build\Net40\Release\dotNetRDF.Data.Virtuoso.dll NuGet\CorePackage\lib\Net35\dotNetRDF.Data.Virtuoso.dll
copy /y Build\Net40\Release\dotNetRDF.dll NuGet\CorePackage\lib\Net40\dotNetRDF.dll
copy /y Build\Net40\Release\HtmlAgilityPack.dll NuGet\CorePackage\lib\Net40\HtmlAgilityPack.dll
copy /y Build\Net40\Release\OpenLink.Data.Virtuoso.dll NuGet\CorePackage\lib\Net40\OpenLink.Data.Virtuoso.dll
copy /y Build\Net40\Release\VDS.Common.dll NuGet\CorePackage\lib\Net40\VDS.Common.dll

xcopy /y Build\Net40\Release\*.exe NuGet\ModellingPackage\tools\net40
xcopy /y Build\Net40\Release\*.dll NuGet\ModellingPackage\tools\net40

xcopy /y Build\Net35\Release\*.exe NuGet\ModellingPackage\tools\net35
xcopy /y Build\Net35\Release\*.dll NuGet\ModellingPackage\tools\net35


Net40\.nuget\nuget.exe pack NuGet\ModellingPackage\Modelling.nuspec
Net40\.nuget\nuget.exe pack NuGet\CorePackage\Core.nuspec