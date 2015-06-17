param($installPath, $toolsPath, $package, $project)

$currentCmds = $project.Properties.Item("PreBuildEvent").Value

$buildCmd = 'OntologyGenerator.exe'
$buildCmdTemplate = 'OntologyGenerator.exe -c $(ProjectDir)GeneratorConfig.xml -g $(ProjectDir)Ontologies.cs'

$deployCmd = 'OntologyDeployment.exe'
$deployCmdTemplate = 'OntologyDeployment.exe -c $(ProjectDir)DeploymentConfig.xml -o "provider=dotnetrdf"'

if (!$currentCmds.Contains($buildCmd))
{
	
	$lines = [regex]::Split($currentCmds,"\r\n")
	$cmds = ""

	foreach($line in $lines)
	{
		if( $line.Length -gt 0 -and !$line.Contains($buildCmd))
		{
			$cmds += $line + "`r`n" 
		}
	}
      
	$cmds += $buildCmdTemplate + "`r`n"
      
	$currentCmds = $cmds
}


if (!$currentCmds.Contains($deployCmd))
{
	$lines = [regex]::Split($currentCmds,"\r\n")
	$cmds = ""

	foreach($line in $lines)
	{
		if( $line.Length -gt 0 -and !$line.Contains($deployCmd))
		{
			$cmds += $line + "`r`n" 
		}
	}
      
	$cmds += $deployCmdTemplate + "`r`n"
      
	$project.Properties.Item("PreBuildEvent").Value = $cmds
}