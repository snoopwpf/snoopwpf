$desktopPath = $([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::DesktopDirectory))
$shortcutFilePath = Join-Path $desktopPath "Snoop.lnk"

if (Test-Path($shortcutFilePath)) {
	Remove-Item $shortcutFilePath -ErrorAction SilentlyContinue
}