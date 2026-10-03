[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-075-verification'
Push-Location $projectRoot
try {
    & (Join-Path $projectRoot 'tools\Verify-CDR074.ps1')
    if ($LASTEXITCODE -ne 0) { throw "CDR-074 baseline failed: $LASTEXITCODE." }
    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-075 demo failed: $LASTEXITCODE." }
    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    $duck = $manifest.ducking
    if ($manifest.demoId -ne 'CDR-075' -or $manifest.persistedCommercialBytes -ne 0 -or
        $duck.TaskId -ne 'CDR-075' -or $duck.Rows.Count -ne 9 -or
        $duck.DuckHeight -ne 6 -or $duck.StandingHeight -ne 11 -or
        $duck.FeetPreserved -ne $true -or $duck.NeverOverlapped -ne $true -or
        $duck.RestoredStanding -ne $true -or $duck.DeterministicReplay -ne $true -or
        $duck.StartedCount -ne 1 -or $duck.BlockedCount -ne 2 -or $duck.CompletedCount -ne 1) {
        throw 'CDR-075 manifest did not prove feet-anchored ducking and safe rise.'
    }
    foreach ($row in $duck.Rows) {
        if ($row.Bottom -ne 11) { throw 'CDR-075 foot anchor changed.' }
    }
    if ($duck.Rows[6].BlockingSolidId -ne 'duck-low-ceiling' -or
        $duck.Rows[7].BlockingSolidId -ne 'duck-low-ceiling' -or
        $duck.Rows[6].Height -ne 6 -or $duck.Rows[7].Height -ne 6 -or
        $duck.Rows[8].Ducking -ne $false -or $duck.Rows[8].Height -ne 11 -or
        $report -notmatch 'id="duck-clearance-demo"' -or
        $report -notmatch 'UnduckBlocked' -or $report -notmatch 'UnduckCompleted') {
        throw 'CDR-075 per-tick clearance evidence or report is incomplete.'
    }
    Write-Host 'CDR-075 OFFLINE VERIFICATION PASSED: Core 48/48, Player 72/72, total 920/920; nine-tick generated duck/blocked-rise/clear-rise replay, heights 6/11, feet y=11, events 1/2/1; no overlap, game, visible GUI, live input, install/reference access or commercial bytes.'
}
finally { Pop-Location }
