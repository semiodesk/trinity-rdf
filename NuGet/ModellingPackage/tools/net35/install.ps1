param($installPath, $toolsPath, $package, $project)
 
$appConfig = $project.ProjectItems | where-object { $_.Name.ToLower() -eq "app.config" };
$webConfig = $project.ProjectItems | where-object { $_.Name.ToLower() -eq "web.config" };

# If there is no app.config or no web.config, generate one from a template.
if(!$appConfig -and !$webConfig)
{
	(get-content "$installPath\tools\App.t.config") | foreach-object {$_ -replace "\{0\}", $project.Properties.Item("RootNamespace").Value} | set-content "$installPath\tools\App.config";

    $project.ProjectItems.AddFromFileCopy("$installPath\tools\App.config");
}

# NOTE: Since the file is to be modified by the ontology generator, deploying the file via the NuGet package content is not an option.
$ontologiesDir = $project.ProjectItems | where-object { $_.Name.ToLower() -eq "ontologies" };

if(!$ontologiesDir)
{
    write-host "Creating Ontologies folder.."

    $ontologiesDir = $project.ProjectItems.AddFolder("Ontologies");
}

if($ontologiesDir)
{
    write-host "Adding Ontologies.g.cs.."

    # We add the ontologies file to the project for cross-platform compatibility with Xamarin Studio.
    $item = $ontologiesDir.ProjectItems.AddFromFileCopy("$installPath\Ontologies.g.cs");

    write-host "Setting build action for Ontologies.g.cs.."

    # Set the build action to 'Compile', see: https://msdn.microsoft.com/en-us/library/aa983962(VS.71).aspx
    $item.Properties.Item("BuildAction").Value = [int]1;
}