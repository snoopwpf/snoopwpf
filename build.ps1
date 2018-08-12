#Requires -Version 5

Param(
    [Parameter(Mandatory=$False)]
    [string]$Configuration = "Release",
    [Parameter(Mandatory=$False)]
    [switch]$Package
)

$ErrorActionPreference = "Stop"

if ($null -eq (Get-Command vswhere)) {
    Write-Error "vswhere could not be found. To build this project you need vswhere. You can install vswhere using 'choco install vswhere -y'."
}

$path = vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ($path) {
    $msbuild = Join-Path $path 'MSBuild/15.0/Bin/MSBuild.exe'
}

if ($null -eq $msbuild -or !(Test-Path $msbuild)) {
    Write-Error "MSBuild could not be found."
}

# Build solution
& $msbuild Snoop.sln /property:Configuration=$Configuration /v:m /nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
}

if ($Package) {
    if ($null -eq (Get-Command nuget)) {
        Write-Error "nuget could not be found. To package this project you need nuget. You can install nuget using 'choco install nuget -y'."
    }

    $buildOutput = Join-Path $PSScriptRoot "build/$Configuration"
    $version = (Get-Item (Join-Path $buildOutput "snoop.exe")).VersionInfo.FileVersion
    $outputDirectory = Join-Path $PSScriptRoot "build/publish"

    # Create chocolatey signal files for shim generation
    Get-ChildItem -Path $buildOutput/*.exe -Exclude "Snoop.exe" | ForEach-Object { New-Item "$_.ignore" -ErrorAction SilentlyContinue | Out-Null }
    New-Item (Join-Path $buildOutput "Snoop.exe.gui") -ErrorAction SilentlyContinue | Out-Null    

    "Creating chocolatey package for version $version"
    &nuget pack "$(Join-Path $PSScriptRoot 'chocolatey\snoop.nuspec')" -Version $version -Properties Configuration=$Configuration -OutputDirectory "$outputDirectory" -NoPackageAnalysis

    "Creating zip for version $version"
    $zipOutput = (Join-Path $outputDirectory "Snoop.$version.zip")
    Remove-Item $zipOutput -ErrorAction SilentlyContinue
    Compress-Archive -Path $buildOutput\Scripts, $buildOutput\*.dll, $buildOutput\*.pdb, $buildOutput\*.exe, $buildOutput\*.config -DestinationPath $zipOutput
}