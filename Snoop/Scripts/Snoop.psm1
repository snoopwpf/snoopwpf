$path = Split-Path $MyInvocation.MyCommand.Path
dir -recurse -filter *.ps1 $path | % { . $_.FullName }

Export-ModuleMember *