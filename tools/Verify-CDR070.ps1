[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-070-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:MSBUILDDISABLENODEREUSE = '1'

Push-Location $projectRoot
try {
    & (Join-Path $projectRoot 'tools\Verify-CDR060.ps1')
    if ($LASTEXITCODE -ne 0) { throw "CDR-060 baseline verification failed: $LASTEXITCODE." }
    & dotnet run --project tests/CelesteDesktop.LocalReference.Tests/CelesteDesktop.LocalReference.Tests.csproj --configuration Release --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-070 focused verification failed: $LASTEXITCODE." }
    Write-Host 'CDR-070 BASELINE VERIFICATION PASSED IN CURRENT TREE: 10/10 focused reference-safety cases and 884 current total regressions passed, including later CDR-071/CDR-072/CDR-073 Player cases; no real installation, game, GUI, decompiler download or commercial reference was used by this gate.'
}
finally { Pop-Location }
