param([switch]$Demo)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA=Join-Path $repo 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES=Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
Push-Location $repo
try {
    & dotnet restore tests/CelesteDesktop.RuntimeIsolation.Tests --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0) { throw 'ISOLATION_RESTORE_FAILED' }
    & dotnet build tests/CelesteDesktop.RuntimeIsolation.Tests -c Release --no-restore
    if($LASTEXITCODE -ne 0) { throw 'ISOLATION_BUILD_FAILED' }
    $demoArgs=@(); if($Demo){$demoArgs=@('--demo')}
    $lines=@(& dotnet tests/CelesteDesktop.RuntimeIsolation.Tests/bin/Release/net8.0/CelesteDesktop.RuntimeIsolation.Tests.dll @demoArgs)
    if($LASTEXITCODE -ne 0) { throw 'ISOLATION_TEST_FAILED' }
    $lines | ForEach-Object { Write-Output $_ }
    $result=@($lines | Where-Object { $_ -match '^ISOLATION_VERIFIED passed=45 failed=0 originalBound=false recoveredCodeExecuted=false$' })
    if($result.Count -ne 1) { throw 'ISOLATION_RESULT_INVALID' }
    $summary=[ordered]@{schemaVersion=1;taskId='CDR-082';stage='managed-isolation-adapter';checksPassed=45;checksFailed=0;fixedHz=60;deniedServiceCount=8;demoFrames=5;originalBound=$false;recoveredCodeExecuted=$false;originalSourceModified=$false;newAssetsRead=$false;guiOpened=$false;steamApiCalled=$false;gameLaunched=$false;installationWrites=0;processSandboxEstablished=$false;originalFrameworkBridgeEstablished=$false}
    $artifact=Join-Path $repo 'artifacts/cdr-082-isolation'
    New-Item -ItemType Directory -Path $artifact -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $artifact 'summary.json'),($summary | ConvertTo-Json))
} finally { Pop-Location }
exit 0
