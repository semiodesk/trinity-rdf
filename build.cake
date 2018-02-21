//#addin "nuget:?package=Cake.StyleCop&version=1.1.0"
//#addin "Cake.XdtTransform"
//#addin "Cake.SemVer"
#tool nuget:?package=NUnit.ConsoleRunner&version=3.7.0
#addin "Cake.DocFx"
#tool "docfx.msbuild"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

struct PathStruct{
    public DirectoryPath ProjectPath;
    public DirectoryPath OutputPath; 
    public FilePathCollection Solutions;
}

var Paths = new PathStruct(){ 
    ProjectPath = MakeAbsolute(Directory("./")),
    OutputPath = MakeAbsolute(Directory("./Build")),
    Solutions = GetFiles("./**/*.sln")
};

Task("Clean-Outputs")
	.Does(() => 
	{
		CleanDirectory(Paths.OutputPath);
	});

Task("Clean")
	.IsDependentOn("Clean-Outputs")
	.Does(() => 
	{
        foreach( var solutionFile in Paths.Solutions )
        {
            DotNetBuild(solutionFile, settings => settings
                .SetConfiguration(configuration)
                .WithTarget("Clean")
                .SetVerbosity(Verbosity.Minimal));
        }
	});


/* 
Task("StyleCop")
	.Does(() => 
	{
        StyleCopAnalyse(settings => settings
			.WithSolution(solutionFile)
			.WithSettings(File("./Settings.StyleCop"))
			.ToResultFile(buildDir + File("StyleCopViolations.xml")));
	});
*/

Task("Build")
	.IsDependentOn("Clean")
	//.IsDependentOn("StyleCop")	TODO: enable
    .Does(() =>
	{
        foreach( var solutionFile in Paths.Solutions )
        {
            Information(solutionFile);
            
            NuGetRestore(solutionFile);

            DotNetBuild(solutionFile, settings => settings
                .SetConfiguration(configuration)
                .WithTarget("Rebuild")
                .SetVerbosity(Verbosity.Minimal));
                 
        }
    });

Task("Test")
	.IsDependentOn("Build")
    .Does(() =>
	{
        var testAssemblies = GetFiles(Paths.OutputPath+"/**/Trinity.Test.dll");
		foreach(var asm in testAssemblies )
        try
        {
            Information(asm);
            //NUnit3(Paths.OutputPath+"/**/" + configuration + "/Trinity.Test.dll", new NUnit3Settings {
            //   
            //    });
        }
        catch(Exception exp) {}
    });

Task("Documentation")
    .IsDependentOn("Test")
    .Does(()=>{
    
        DocFxBuild("./doc/docfx_project/docfx.json");
    });

Task("Default")
	.IsDependentOn("Documentation");

RunTarget(target);