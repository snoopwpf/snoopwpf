dir -recurse -filter *.ps1 (Split-Path $MyInvocation.MyCommand.Path) | % { . $_.FullName }
Export-ModuleMember *