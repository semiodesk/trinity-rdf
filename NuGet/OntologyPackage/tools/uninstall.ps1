param($installPath, $toolsPath, $package, $project)

$cmds = $project.Properties.Item("PreBuildEvent").Value

$buildCmd = 'OntologyGenerator.exe'
$deployCmd = 'OntologyDeployment.exe'


if ($cmds.Contains($buildCmd) -or $cmds.Contains($deployCmd) )
{
    $lines = [regex]::Split($cmds,"\r\n")
    $newCmds = ""
   
   foreach($line in $lines)
   {
        if($line.Length -gt 0 -and !$line.Contains($buildCmd) -and !$line.Contains($deployCmd))
        {
            $newCmds += $line + "`r`n"  # Creating a cleaned list of commands
        }
   }

  $project.Properties.Item("PreBuildEvent").Value = $newCmds
}
