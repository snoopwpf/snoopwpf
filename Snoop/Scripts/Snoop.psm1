$path = Split-Path $MyInvocation.MyCommand.Path

ipmo -force (Join-Path $path "PSProvider\PSProviderFramework.dll")
dir -recurse -filter *.ps1 $path | % { . $_.FullName }

Export-ModuleMember *