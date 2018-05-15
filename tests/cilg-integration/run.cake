
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

List<string> executables = new List<string>{ "net35/test.exe", "net40/test.exe", "net461/test.exe", "netcore/test.dll", "netstandard-net461/test.exe", "netstandard-netcore/test.dll"};
List<string> injectables = new List<string>{ "net35/test.exe", "net40/test.exe", "net461/test.exe", "netcore/test.dll", "netstandard-net461/netstandard20.dll", "netstandard-netcore/netstandard20.dll"};
//, "netstandard-net461", "netstandard-netcore"

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

Task("cilg-Test").DoesForEach(executables, (path) =>
{
	var filePath = ExpandPath(path);
	if(!FileExists(filePath))
		throw new Exception("File "+filePath+" does not exist.");

	Information("Testing "+filePath);

	Information("\nRunning without injection:");
	Execute(filePath);

	Information("\nInjecting...");
	Inject(ExpandPath(injectables[counter]));

	Information("\nRunning with injection:");
	if( Execute(filePath) != 0 )
		throw new Exception("cilg integration test for file "+filePath+" failed.");

	Information("\n--------------------------------------------\n");
	counter += 1;
});


Task("Default")
	.IsDependentOn("cilg-Test");

RunTarget(target);