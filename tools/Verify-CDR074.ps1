[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-074-verification'
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
    & (Join-Path $projectRoot 'tools\Verify-CDR073.ps1')
    if ($LASTEXITCODE -ne 0) { throw "CDR-073 baseline verification failed: $LASTEXITCODE." }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-074 progress demo failed: $LASTEXITCODE." }
    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-074' -or
        $manifest.player.OneWayPassedUpward -ne $true -or
        $manifest.player.OneWayDropStartY -ne 0 -or
        $manifest.player.OneWayLandingY -ne 19 -or
        $manifest.player.OneWayLandedPlatformId -ne 'one-way-lower' -or
        $manifest.player.OneWayDropStartedCount -ne 1 -or
        $manifest.player.OneWayDropCompletedCount -ne 1 -or
        $manifest.player.OneWayLandingCount -ne 1 -or
        $manifest.player.OneWayDropRearmed -ne $true -or
        $manifest.player.deterministicReplay -ne $true) {
        throw 'CDR-074 demo manifest did not prove deterministic one-way-platform passage, landing, drop-through and rearm.'
    }
    if ($report -notmatch 'CDR-074' -or
        $report -notmatch 'one-way-lower') {
        throw 'CDR-074 demo report omitted the plain-language one-way-platform evidence.'
    }

    Write-Host 'CDR-074 OFFLINE VERIFICATION PASSED: Simulation.Core 42/42, Player 59/59 and 901/901 total regressions; generated Player passed upward through one-way geometry, dropped from y=0, landed on the lower platform at y=19, emitted one start/completion/landing diagnostic each, rearmed and replayed identically; ordinary Solids remain blocking; no game, GUI, live input, install/local-reference access or commercial bytes.'
}
finally { Pop-Location }
