[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

###########################################################################
# CONFIGURATION
###########################################################################

$BuildProjectFile = "$PSScriptRoot\.build\_build.csproj"
$TempDirectory = "$PSScriptRoot\.tmp"

$DotNetGlobalFile = "$PSScriptRoot\global.json"
$DotNetInstallUrl = "https://dot.net/v1/dotnet-install.ps1"
$DotNetChannel = "Current"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1

###########################################################################
# EXECUTION
###########################################################################

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If global.json exists, load expected version
if (Test-Path $DotNetGlobalFile) {
    $DotNetGlobal = $(Get-Content $DotNetGlobalFile | Out-String | ConvertFrom-Json)
    if ($DotNetGlobal.PSObject.Properties["sdk"] -and $DotNetGlobal.sdk.PSObject.Properties["version"]) {
        $DotNetVersion = $DotNetGlobal.sdk.version
        Write-Verbose "Using SDK-Version $DotNetVersion from global.json"
    }
}

# If dotnet is installed locally, and expected version is not set or installation matches the expected version
if ($null -ne (Get-Command "dotnet" -ErrorAction SilentlyContinue) -and `
     (!(Test-Path variable:DotNetVersion) -or $(& dotnet --version) -ge $DotNetVersion)) {
    $env:DOTNET_EXE = (Get-Command "dotnet").Path
    Write-Verbose "Found dotnet with version $(& dotnet --version) at $($env:DOTNET_EXE)"
}
else {
    Write-Verbose "Could not find dotnet command. Starting download..."

    $DotNetDirectory = "$TempDirectory\dotnet-win"
    $env:DOTNET_EXE = "$DotNetDirectory\dotnet.exe"

    # Download install script
    $DotNetInstallFile = "$TempDirectory\dotnet-install.ps1"
    New-Item -ItemType Directory -Path $TempDirectory -Force | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallUrl, $DotNetInstallFile)

    # Install by channel or version
    if (!(Test-Path variable:DotNetVersion)) {
        ExecSafe { & $DotNetInstallFile -InstallDir $DotNetDirectory -Channel $DotNetChannel -NoPath }
    } else {
        ExecSafe { & $DotNetInstallFile -InstallDir $DotNetDirectory -Version $DotNetVersion -NoPath }
    }
}

Write-Output "Microsoft (R) .NET Core SDK version $(& $env:DOTNET_EXE --version)"
Write-Output "Preparing and running build..."
ExecSafe { & $env:DOTNET_EXE build $BuildProjectFile -c release /nodeReuse:false /nologo /consoleLoggerParameters:"ErrorsOnly;NoSummary" }
Write-Output "Starting build..."
ExecSafe { & $env:DOTNET_EXE run --project $BuildProjectFile --no-build -c release -- $BuildArguments -nologo }