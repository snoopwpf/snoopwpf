# Create a desktop link to snoop executable
$parentfolder = Split-Path -parent $MyInvocation.MyCommand.Definition
$target = Join-Path $parentfolder 'snoop.exe'

$desktopPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Desktop)
$shortcutFilePath = Join-Path $desktopPath "Snoop.lnk"
Install-ChocolateyShortcut -ShortcutFilePath $shortcutFilePath -TargetPath $target