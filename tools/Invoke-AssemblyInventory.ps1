[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$InstallRoot)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$runArtifacts = Join-Path $projectRoot 'artifacts/cdr-081-real'
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA = Join-Path $projectRoot 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES = Join-Path $projectRoot 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
Push-Location $projectRoot
try {
    & dotnet restore tools/CelesteDesktop.AssemblyInventory --configfile NuGet.Offline.Config
    if ($LASTEXITCODE -ne 0) { throw 'Offline restore failed.' }
    & dotnet run --project tools/CelesteDesktop.AssemblyInventory -c Release --no-restore -- --root $InstallRoot --output (Join-Path $runArtifacts 'inventory.json')
    if ($LASTEXITCODE -ne 0) { throw 'Read-only metadata inspection failed. See phase/code details above.' }
    & dotnet restore samples/CelesteDesktop.ProgressDemo --configfile NuGet.Offline.Config
    if ($LASTEXITCODE -ne 0) { throw 'Report restore failed.' }
    & dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-restore -- --output artifacts/current-progress-demo
    if ($LASTEXITCODE -ne 0) { throw 'Summary generation failed.' }
} finally { Pop-Location }
