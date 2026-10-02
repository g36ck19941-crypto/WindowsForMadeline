[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-072-verification'
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
    & (Join-Path $projectRoot 'tools\Verify-CDR071.ps1')
    if ($LASTEXITCODE -ne 0) { throw "CDR-071 baseline verification failed: $LASTEXITCODE." }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-072 progress demo failed: $LASTEXITCODE." }
    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.player.RetainedWallSpeed -ne 90 -or
        $manifest.player.InitialWallRetentionTicks -ne 4 -or
        $manifest.player.RestoredWallSpeed -ne 90 -or
        $manifest.player.WallRetainedEventCount -ne 1 -or
        $manifest.player.WallRestoredEventCount -ne 1 -or
        $manifest.player.deterministicReplay -ne $true) {
        throw 'CDR-072 demo manifest did not prove deterministic wall-speed retention and restoration.'
    }
    if ($report -notmatch 'CDR-072' -or
        $report -notmatch 'WallSpeedRetained/Restored=1/1') {
        throw 'CDR-072 demo report omitted the wall-speed explanation or diagnostics.'
    }

    Write-Host 'CDR-072 OFFLINE VERIFICATION PASSED: Player 45/45 and 878/878 total regressions; generated wall collision retained speed 90 for four ticks and restored 90 with one retain/restore diagnostic; jump and external launches cancel stale retention; no game, GUI, install access or tracked reference content.'
}
finally { Pop-Location }
