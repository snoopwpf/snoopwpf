try {
  # Create a desktop link to snoop executable
  $parentfolder = Split-Path -parent $MyInvocation.MyCommand.Definition
  $target = Join-Path $parentfolder 'snoop.exe'
  Install-ChocolateyDesktopLink $target

  # the following is all part of error handling
  Write-ChocolateySuccess 'snoop'
} catch {
  Write-ChocolateyFailure 'snoop' "$($_.Exception.Message)"
  throw
}