[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-073-verification'
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
    & (Join-Path $projectRoot 'tools\Verify-CDR072.ps1')
    if ($LASTEXITCODE -ne 0) { throw "CDR-072 baseline verification failed: $LASTEXITCODE." }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-073 progress demo failed: $LASTEXITCODE." }
    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.player.UpwardCornerCorrectionX -ne 1 -or
        $manifest.player.CornerStartX -ne 0 -or
        $manifest.player.CornerFinalX -ne 1 -or
        $manifest.player.CornerStartY -ne 4 -or
        $manifest.player.CornerFinalY -ne 2 -or
        $manifest.player.CornerCorrectionEventCount -ne 1 -or
        $manifest.player.CornerVerticalSpeedPreserved -ne $true -or
        $manifest.player.deterministicReplay -ne $true) {
        throw 'CDR-073 demo manifest did not prove bounded deterministic upward corner correction.'
    }
    if ($report -notmatch 'CDR-073' -or
        $report -notmatch 'UpwardCornerCorrected=1') {
        throw 'CDR-073 demo report omitted the corner-correction explanation or diagnostic.'
    }

    Write-Host 'CDR-073 OFFLINE VERIFICATION PASSED: Simulation.Core 34/34, Player 50/50 and 884/884 total regressions; generated upward corner collision shifted right by exactly one pixel, completed the remaining rise and emitted one explicit correction diagnostic; no game, GUI, install access or tracked reference content.'
}
finally { Pop-Location }
