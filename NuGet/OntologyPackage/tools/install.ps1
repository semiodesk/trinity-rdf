param($installPath, $toolsPath, $package, $project)

$currentCmds = $project.Properties.Item("PreBuildEvent").Value

$buildCmd = 'OntologyGenerator.exe -c $(ProjectDir)GeneratorConfig.xml -g $(ProjectDir)Ontologies.cs'

if (!$currentCmds.Contains($buildCmd))
{
	
	$lines = [regex]::Split($currentCmds,"\r\n")
	$cmds = ""

	foreach($line in $lines)
	{
		if( $line.Length -gt 0 -and !$line.Contains('OntologyGenerator.exe'))
		{
			$cmds += $line + "`r`n" 
		}
	}
      
	$cmds += $buildCmd + "`r`n"
      
	$project.Properties.Item("PreBuildEvent").Value = $cmds
}

