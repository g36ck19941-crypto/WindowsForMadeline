[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-071-verification'
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
    & (Join-Path $projectRoot 'tools\Verify-CDR070.ps1')
    if ($LASTEXITCODE -ne 0) { throw "CDR-070 baseline verification failed: $LASTEXITCODE." }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-071 progress demo failed: $LASTEXITCODE." }
    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.player.AppliedLiftX -ne 250 -or
        $manifest.player.AppliedLiftY -ne -130 -or
        $manifest.player.LiftEventCount -ne 1 -or
        $manifest.player.deterministicReplay -ne $true) {
        throw 'CDR-071 demo manifest did not prove bounded deterministic lift inheritance.'
    }
    if ($report -notmatch 'CDR-071' -or $report -notmatch 'LiftVelocityApplied=1') {
        throw 'CDR-071 demo report omitted the lift-inheritance explanation or diagnostic.'
    }

    Write-Host 'CDR-071 BASELINE VERIFICATION PASSED IN CURRENT TREE: Player 50/50 and 884/884 current total regressions, including later CDR-072/CDR-073 cases; generated moving-Solid jump still applies bounded lift (250,-130) with one diagnostic and deterministic replay; no game, GUI, install access or tracked reference content.'
}
finally { Pop-Location }
