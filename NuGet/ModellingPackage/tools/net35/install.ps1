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
	$project.ProjectItems.AddFromFileCopy("$installPath\tools\App.config");
	#$item = $project.ProjectItems | where-object {$_.Name -eq "App.config" };
    #$item.Properties.Item("BuildAction").Value = [int]2;
}