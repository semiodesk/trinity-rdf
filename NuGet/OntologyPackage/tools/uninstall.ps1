param($installPath, $toolsPath, $package, $project)

$cmds = $project.Properties.Item("PreBuildEvent").Value

$cdCmd = 'CD "$(SolutionDir)packages\Semiodesk.Trinity.Ontologies*\tools"'
$buildCmd = 'OntologyGenerator.exe'
$deployCmd = 'OntologyDeployment.exe'


if ($cmds.Contains($buildCmd) -or $cmds.Contains($deployCmd) -or $cmds.Contains($cdCmd) )
{
    $lines = [regex]::Split($cmds,"\r\n")
    $newCmds = ""
   
   foreach($line in $lines)
   {
        if($line.Length -gt 0 -and !$line.Contains($buildCmd) -and !$line.Contains($deployCmd) -and !$line.Contains($cdCmd))
        {
            $newCmds += $line + "`r`n"  # Creating a cleaned list of commands
        }
   }

  $project.Properties.Item("PreBuildEvent").Value = $newCmds
}

$cmds = $project.Properties.Item("PostBuildEvent").Value

$generateCmd = 'cilg.exe'

$cdCmd = 'CD "$(SolutionDir)packages\Semiodesk.Trinity.Ontologies*\tools"'

if ($cmds.Contains($generateCmd) -or $cmds.Contains($cdCmd) )
{
    $lines = [regex]::Split($cmds,"\r\n")
    $newCmds = ""
   
   foreach($line in $lines)
   {
        if($line.Length -gt 0 -and !$line.Contains($generateCmd) -and !$line.Contains($cdCmd))
        {
            $newCmds += $line + "`r`n"  # Creating a cleaned list of commands
        }
   }

  $project.Properties.Item("PostBuildEvent").Value = $newCmds
}