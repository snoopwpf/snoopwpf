Param(
    [Parameter(Mandatory=$False)]
    [string]$Configuration = "Release",
    [Parameter(Mandatory=$False)]
    [switch]$Package
)

$ErrorActionPreference = "Stop"

if ($null -eq (Get-Command vswhere)) {
    Write-Error "vswhere could not be found. To build this project you need vswhere."
}

$path = vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ($path) {
    $msbuild = Join-Path $path 'MSBuild/15.0/Bin/MSBuild.exe'
}

if ($null -eq $msbuild -or !(Test-Path $msbuild)) {
    Write-Error "msbuild could not be found"
}

& $msbuild Snoop.sln /property:Configuration=$Configuration /v:m /nologo

if ($Package) {
    if ($null -eq (Get-Command nuget)) {
        Write-Error "nuget could not be found. To package this project you need nuget."
    }

    $buildOutput = Join-Path $PSScriptRoot "build/$Configuration"
    Get-ChildItem -Path $buildOutput/*.exe -Exclude "Snoop.exe" | ForEach-Object { New-Item "$_.ignore" -ErrorAction SilentlyContinue | Out-Null }
    New-Item (Join-Path $buildOutput "Snoop.exe.gui") -ErrorAction SilentlyContinue | Out-Null

    $nuspec = Join-Path $PSScriptRoot chocolatey\snoop.nuspec
    $version = (Get-Item (Join-Path $buildOutput "snoop.exe")).VersionInfo.FileVersion
    $outputDirectory = Join-Path $PSScriptRoot "build/chocolatey"

    "Creating chocolatey package for version $version"
    &nuget pack "$nuspec" -Version $version -Properties Configuration=$Configuration -OutputDirectory "$outputDirectory" -NoPackageAnalysis
}