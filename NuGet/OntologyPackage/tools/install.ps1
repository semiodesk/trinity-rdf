param($installPath, $toolsPath, $package, $project)

$currentCmds = $project.Properties.Item("PreBuildEvent").Value

$cdCmd = 'CD "$(SolutionDir)packages\Semiodesk.Trinity.Ontologies*\tools"'
if (!$currentCmds.Contains($cdCmd))
{
	
	$lines = [regex]::Split($currentCmds,"\r\n")
	$cmds = ""

	foreach($line in $lines)
	{
		if( $line.Length -gt 0 -and !$line.Contains($cdCmd))
		{
			$cmds += $line + "`r`n" 
		}
	}
      
	$cmds += $cdCmd + "`r`n"
      
	$currentCmds = $cmds
}

$buildCmd = 'OntologyGenerator.exe'
$buildCmdTemplate = 'OntologyGenerator.exe -c $(ProjectDir)GeneratorConfig.xml -g $(ProjectDir)Ontologies.cs'

$deployCmd = 'OntologyDeployment.exe'
$deployCmdTemplate = 'OntologyDeployment.exe -c $(ProjectDir)DeploymentConfig.xml -o ""'

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

$currentCmds = $project.Properties.Item("PostBuildEvent").Value

$cdCmd = 'CD "$(SolutionDir)packages\Semiodesk.Trinity.Ontologies*\tools"'
if (!$currentCmds.Contains($cdCmd))
{
	
	$lines = [regex]::Split($currentCmds,"\r\n")
	$cmds = ""

	foreach($line in $lines)
	{
		if( $line.Length -gt 0 -and !$line.Contains($cdCmd))
		{
			$cmds += $line + "`r`n" 
		}
	}
      
	$cmds += $cdCmd + "`r`n"
      
	$currentCmds = $cmds
}


$generateCmd = 'cilg.exe'
$generateCmdTemplate = 'cilg.exe -i $(TargetPath) -o $(TargetPath)'

if (!$currentCmds.Contains($generateCmd))
{
	
	$lines = [regex]::Split($currentCmds,"\r\n")
	$cmds = ""

	foreach($line in $lines)
	{
		if( $line.Length -gt 0 -and !$line.Contains($generateCmd))
		{
			$cmds += $line + "`r`n" 
		}
	}
      
	$cmds += $generateCmdTemplate + "`r`n"
      
	$currentCmds = $cmds
}