#Requires -Version 5

Param(
    [Parameter(Mandatory=$False)]
    [string]$Configuration = "Release",
    [Parameter(Mandatory=$False)]
    [switch]$Package
)

$ErrorActionPreference = "Stop"

$packages = Join-Path $PSScriptRoot "packages"

$vswhere = Join-Path $packages "vswhere/tools/vswhere.exe"
$nuget = Join-Path $packages "nuget.commandline/tools/nuget.exe"
$candle = Join-Path $packages "wix/tools/candle.exe"
$light = Join-Path $packages "wix/tools/light.exe"

"Restoring packages"
& ./paket.cmd restore

$path = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ($path) {
    $msbuild = Join-Path $path 'MSBuild/15.0/Bin/MSBuild.exe'
}

if ($null -eq $msbuild -or !(Test-Path $msbuild)) {
    Write-Error "MSBuild could not be found."
}

"Building solution"
& $msbuild Snoop.sln /property:Configuration=$Configuration /v:m /nologo

#"Building Test-Harnesses"
#& $msbuild ".\TestHarnesses\Snoop TestHarnesses.sln" /property:Configuration=$Configuration /v:m /nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
}

if ($Package) {
    $buildOutput = Join-Path $PSScriptRoot "build/$Configuration"
    $intermediateOutput = Join-Path $PSScriptRoot "Intermediate"
    $version = (Get-Item (Join-Path $buildOutput "snoop.exe")).VersionInfo.FileVersion
    $outputDirectory = Join-Path $PSScriptRoot "build/publish"

    # Create chocolatey signal files for shim generation
    Get-ChildItem -Path $buildOutput/*.exe -Exclude "Snoop.exe" | ForEach-Object { New-Item "$_.ignore" -ErrorAction SilentlyContinue | Out-Null }
    New-Item (Join-Path $buildOutput "Snoop.exe.gui") -ErrorAction SilentlyContinue | Out-Null    

    "Creating chocolatey package for version $version"
    & $nuget pack "$(Join-Path $PSScriptRoot 'chocolatey\snoop.nuspec')" -Version $version -Properties Configuration=$Configuration -OutputDirectory "$outputDirectory" -NoPackageAnalysis

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Creating chocolatey package failed."
    }

    "Creating zip for version $version"
    $zipOutput = (Join-Path $outputDirectory "Snoop.$version.zip")
    Remove-Item $zipOutput -ErrorAction SilentlyContinue
    Compress-Archive -Path $buildOutput\Scripts, $buildOutput\*.dll, $buildOutput\*.pdb, $buildOutput\*.exe, $buildOutput\*.config -DestinationPath $zipOutput

    "Creating msi for version $version"
    $msiOutput = Join-Path $outputDirectory "Snoop.$version.msi"
    & $candle Snoop.wxs -ext WixUIExtension -o "$intermediateOutput/Snoop.wixobj" -dProductVersion="$version" -nologo

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Candle failed."
    }

    & $light -out "$msioutput" -b "$buildOutput" "$intermediateOutput/Snoop.wixobj" -ext WixUIExtension -dProductVersion=$version -pdbout "$intermediateOutput/Snoop.wixpdb" -nologo

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Light failed."
    }
}