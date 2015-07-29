param($installPath, $toolsPath, $package, $project)
 
$foundAppConfig = $false;
$foundWebConfig = $false;
 
ForEach ($item in $project.ProjectItems)
{
	if( $item.Name.ToLower() -eq "app.config" )
    {
		$foundAppConfig = $true;
    }
		
	if( $item.Name.ToLower() -eq "web.config" )
    {
		$foundAppConfig = $true;
    }
       
}

if( $foundAppConfig -eq $false -and $foundWebConfig -eq $false )
{
	(get-content "$installPath\tools\App.t.config") | foreach-object {$_ -replace "\{0\}", $project.Properties.Item("RootNamespace").Value} | set-content "$installPath\tools\App.config";
    $project.ProjectItems.AddFromFileCopy("$installPath\tools\App.config");
	
	#$item = $project.ProjectItems | where-object {$_.Name -eq "App.config" };
    #$item.Properties.Item("BuildAction").Value = [int]2;
}