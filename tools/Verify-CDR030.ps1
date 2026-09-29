[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-030-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-030 restore failed: $LASTEXITCODE." }

    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-030 build failed: $LASTEXITCODE." }

    $testProjects = @(
        'tests/CelesteDesktop.Install.Tests/CelesteDesktop.Install.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Tests/CelesteDesktop.AssetWorker.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Meta.Tests/CelesteDesktop.AssetWorker.Meta.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Data.Tests/CelesteDesktop.AssetWorker.Data.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.SpriteXml.Tests/CelesteDesktop.AssetWorker.SpriteXml.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Catalog.Tests/CelesteDesktop.AssetWorker.Catalog.Tests.csproj',
        'tests/CelesteDesktop.RealInstallConformance.Tests/CelesteDesktop.RealInstallConformance.Tests.csproj',
        'tests/CelesteDesktop.Simulation.Core.Tests/CelesteDesktop.Simulation.Core.Tests.csproj',
        'tests/CelesteDesktop.Player.Tests/CelesteDesktop.Player.Tests.csproj',
        'tests/CelesteDesktop.Player.Traversal.Tests/CelesteDesktop.Player.Traversal.Tests.csproj',
        'tests/CelesteDesktop.Rendering.Tests/CelesteDesktop.Rendering.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-030 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run `
        --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-030 progress demo failed: $LASTEXITCODE." }

    $manifestPath = Join-Path $demoOutput 'manifest.json'
    $reportPath = Join-Path $demoOutput 'index.html'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-030' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.independentValidation.taskId -ne 'CDR-016' -or
        $manifest.independentValidation.status -ne 'passed-separately' -or
        $manifest.independentValidation.executedByThisDemo -ne $false -or
        $manifest.unassignedTaskIds.Count -ne 3 -or
        $manifest.unassignedTaskIds[0] -ne 'CDR-017' -or
        $manifest.unassignedTaskIds[2] -ne 'CDR-019' -or
        $manifest.catalog.entityCount -ne 2 -or
        $manifest.catalog.frameCount -ne 2 -or
        $manifest.catalog.decodedPageCount -ne 1 -or
        $manifest.simulation.tickRate -ne 60 -or
        $manifest.simulation.tickCount -ne 13 -or
        $manifest.simulation.deterministicReplay -ne $true -or
        $manifest.player.tickCount -ne 24 -or
        $manifest.player.deterministicReplay -ne $true -or
        $manifest.traversal.tickCount -ne 24 -or
        $manifest.traversal.deterministicReplay -ne $true -or
        $manifest.presentation.pixelWidth -ne 24 -or
        $manifest.presentation.pixelHeight -ne 18 -or
        $manifest.presentation.presentCalls -ne 3 -or
        $manifest.presentation.pixelsChangedCount -ne 2 -or
        $manifest.presentation.humanVisibilityConfirmed -ne $false -or
        $manifest.presentation.events.Count -ne 12 -or
        ($manifest.presentation.events | Where-Object EventId -eq 'PRESENTER_PRESENTED').Count -ne 3 -or
        ($manifest.presentation.events | Where-Object EventId -eq 'PRESENTED_PIXELS_CHANGED').Count -ne 2) {
        throw 'CDR-030 demo manifest did not match the generated presentation contract.'
    }
    if ($report -notmatch 'CDR-030' -or
        $report -notmatch 'diagnostic_placeholder=true' -or
        $report -notmatch 'CDR-016' -or
        $report -notmatch 'CDR-017' -or
        $report -notmatch 'CDR-019' -or
        $report -notmatch 'PRESENTER_PRESENTED' -or
        $report -notmatch 'HUMAN_VISIBILITY_CONFIRMED') {
        throw 'CDR-030 demo report omitted required presentation or limitation evidence.'
    }

    Write-Host 'CDR-030 HIDDEN VERIFICATION PASSED: generated pixels reached DirectComposition Commit/WaitForCommitCompletion; no visible GUI, real desktop observation, installation access, or live input.'
}
finally {
    Pop-Location
}
