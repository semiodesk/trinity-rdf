//#addin "nuget:?package=Cake.StyleCop&version=1.1.0"
//#addin "Cake.XdtTransform"
//#addin "Cake.SemVer"
#tool nuget:?package=NUnit.ConsoleRunner&version=3.7.0


var configuration = Argument("configuration", "Release");

struct PathStruct{
    public DirectoryPath ProjectPath;
    public DirectoryPath OutputPath; 

    public IEnumerable<FilePath> Solutions;

    public DirectoryPath IntegrationTestPath;
    public IEnumerable<FilePath> TestSolutions;
    public IEnumerable<FilePath> IntegrationTests;
}

PathStruct Paths = new PathStruct{
    ProjectPath = MakeAbsolute(Directory("./")),
    OutputPath = MakeAbsolute(Directory("./Build")),
    IntegrationTestPath = MakeAbsolute(Directory("./tests")),
    Solutions = new []{new FilePath("Semiodesk.Trinity.sln")},
    TestSolutions =  new []{new FilePath("./tests/cilg-integration/cilg-integration.sln")},
    IntegrationTests = new []{new FilePath("./tests/cilg-integration/run.cake")}//, new FilePath("./tests/nuget-integration/run.cake")}

};

void Clean(FilePath solution)
{
    Section("Clean and remove obj");
        MSBuild(solution, settings => settings
                        .SetConfiguration(configuration)
                        .WithTarget("Clean")
                        .SetVerbosity(Verbosity.Quiet));

        var parsedSolution = ParseSolution(solution);
        foreach( var project in parsedSolution.Projects )
        {
            var dir = GetDirectories(project.Path.GetDirectory()+"/obj");
            CleanDirectories(dir);
        }
}

void Build(FilePath solution)
{
    Section("BEGIN: "+solution);

    Clean(solution);

    NuGetRestore(solution);

    Section("Build");
    MSBuild(solution, settings => settings
            .SetConfiguration(configuration)
            .WithTarget("Rebuild")
            .SetVerbosity(Verbosity.Quiet));

    Section("DONE: "+solution);
}

void Section(string info)
{
    Information("\n****************************************\n* "+info+"\n****************************************\n");
}



Setup(context =>
{
    foreach( var x in Paths.Solutions)
        Information(x);
});

Task("Clean-Outputs")
	.Does(() => 
	{
		CleanDirectory(Paths.OutputPath);
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
    .IsDependentOn("Clean-Outputs")
	//.IsDependentOn("StyleCop")	TODO: enable
    .DoesForEach(Paths.Solutions, (solution, ctx) => {
       Build(solution);
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() => {
        
        MSBuild("./Trinity/Trinity.csproj", settings => settings
                .SetConfiguration(configuration)
                .WithTarget("pack")
                .SetVerbosity(Verbosity.Quiet));
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
        catch(Exception) {}
    });

Task("Documentation")
    .IsDependentOn("Test")
    .Does(()=>{
    
        //DocFxBuild("./doc/docfx_project/docfx.json");
    });

Task("Integration-Test-Build")
    .IsDependentOn("Build")
    .DoesForEach( Paths.TestSolutions, (solution) => {

        Build(solution);
        
    });

Task("Integration-Test")
    .IsDependentOn("Pack")
    .IsDependentOn("Integration-Test-Build")
    .DoesForEach(Paths.IntegrationTests, (test) => {
        
        CakeExecuteScript(test, new CakeSettings {
            Arguments = new Dictionary<string, string>{
                { "configuration", configuration }
              
            }});
    });


Task("Default")
	.IsDependentOn("Integration-Test");

RunTarget("Default");

