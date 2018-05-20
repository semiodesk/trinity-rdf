
#tool nuget:https://www.nuget.org/api/v2?package=NUnit.ConsoleRunner


var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var blueprintDirectory = new DirectoryPath("blueprint");
var workDirectory = new DirectoryPath("work");

var nugetSource = new DirectoryPath("../../Build/Release");
var nugetPackage = "Semiodesk.Trinity";

IEnumerable<FilePath> projects;

FilePath ExpandPath(FilePath path)
{
	return new FilePath("./build/"+configuration.ToLower()+"/"+path);
}

int Execute(FilePath file)
{
	int exitCode = -1;
	IEnumerable<string> stdOut;

	if( file.GetExtension() == ".exe")
	{
		exitCode = StartProcess(file, new ProcessSettings {
			Arguments = "",
			RedirectStandardOutput = true
		}, out stdOut);
	}else if( file.GetExtension() == ".dll")
	{
		exitCode = StartProcess("dotnet", new ProcessSettings {
			Arguments = file.ToString(),
			RedirectStandardOutput = true
		}, out stdOut);
	}else
	{
		throw new Exception("Unexcpected file extension");
	}

	Information("Exit code: {0}", exitCode);
	return exitCode;
}

void Inject(FilePath path)
{
	string argument = "-i "+path.FullPath+" -o "+path.FullPath;
	var exitCode = StartProcess("../../Build/Net40/"+configuration+"/cilg.exe", argument);
}

int counter = 0;

Task("prepare-work-directory").Does(() => {
	Information("Prepare work directory "+workDirectory.FullPath);

	if(DirectoryExists(workDirectory.FullPath))
		CleanDirectories(workDirectory.FullPath);

	CopyDirectory(blueprintDirectory.FullPath, workDirectory.FullPath);
	projects = GetFiles(workDirectory+"/**/*.csproj");

	//Information(projects);
});

Task("add-nuget").IsDependentOn("prepare-work-directory").Does(() => {
	foreach( var project in projects )
	{
		Information("Adding NuGet to "+project+"\n--------------------------------------------\n");
		NuGetInstall(nugetPackage, new NuGetInstallSettings {
			Source = new [] {MakeAbsolute(nugetSource).FullPath},
			FallbackSource= new [] { "https://api.nuget.org/v3/index.json"},
			Verbosity = Cake.Common.Tools.NuGet.NuGetVerbosity.Detailed
		});
	}
});

Task("nuget-Test")

.IsDependentOn("add-nuget")
.Does(() =>
{
	


	Information("\n--------------------------------------------\n");

});


Task("Default")
	.IsDependentOn("add-nuget");

RunTarget(target);