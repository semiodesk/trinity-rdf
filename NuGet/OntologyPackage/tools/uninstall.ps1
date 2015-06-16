param($installPath, $toolsPath, $package, $project)

$cmds = $project.Properties.Item("PreBuildEvent").Value

if ($cmds.Contains('OntologyGenerator.exe'))
{
    $lines = [regex]::Split($cmds,"\r\n")
    $newCmds = ""
   
   foreach($line in $lines)
   {
        if($line.Length -gt 0 -and !$line.Contains('OntologyGenerator.exe'))
        {
            $newCmds += $line + "`r`n"  # Creating a cleaned list of commands
        }
   }

  $project.Properties.Item("PreBuildEvent").Value = $newCmds
}
