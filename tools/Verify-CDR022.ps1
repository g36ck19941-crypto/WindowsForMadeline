[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-022-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-022 restore failed: $LASTEXITCODE." }

    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-022 build failed: $LASTEXITCODE." }

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
        'tests/CelesteDesktop.Player.Traversal.Tests/CelesteDesktop.Player.Traversal.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-022 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run `
        --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-022 progress demo failed: $LASTEXITCODE." }

    $manifestPath = Join-Path $demoOutput 'manifest.json'
    $reportPath = Join-Path $demoOutput 'index.html'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-022' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.catalog.entityCount -ne 2 -or
        $manifest.catalog.frameCount -ne 2 -or
        $manifest.catalog.decodedPageCount -ne 1 -or
        $manifest.simulation.tickRate -ne 60 -or
        $manifest.simulation.tickCount -ne 13 -or
        $manifest.simulation.carryEventCount -ne 3 -or
        $manifest.simulation.blockedEventCount -ne 1 -or
        $manifest.simulation.deterministicReplay -ne $true -or
        $manifest.player.tickCount -ne 24 -or
        $manifest.player.maxRunReached -ne $true -or
        $manifest.player.jumpEventCount -ne 1 -or
        $manifest.player.minimumY -ne -16 -or
        $manifest.player.deterministicReplay -ne $true -or
        $manifest.traversal.tickCount -ne 24 -or
        $manifest.traversal.dashStartedCount -ne 1 -or
        $manifest.traversal.wallSlideStartedCount -ne 1 -or
        $manifest.traversal.wallJumpedCount -ne 1 -or
        $manifest.traversal.climbStartedCount -ne 1 -or
        $manifest.traversal.minimumStamina -ge 110 -or
        $manifest.traversal.deterministicReplay -ne $true -or
        $manifest.traversal.rows.Count -ne 24) {
        throw 'CDR-022 demo manifest did not match the generated traversal contract.'
    }
    if ($report -notmatch 'CDR-022' -or
        $report -notmatch 'diagnostic_placeholder=true' -or
        $report -notmatch 'DashStarted' -or
        $report -notmatch 'WallJumped' -or
        $report -notmatch 'ClimbStarted' -or
        $report -notmatch 'partial') {
        throw 'CDR-022 demo report omitted required traversal or limitation evidence.'
    }

    Write-Host 'CDR-022 OFFLINE VERIFICATION PASSED: no real installation, game, GUI, desktop, or live-input access.'
}
finally {
    Pop-Location
}
