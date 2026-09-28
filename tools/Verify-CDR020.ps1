[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-020-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-020 restore failed: $LASTEXITCODE." }

    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-020 build failed: $LASTEXITCODE." }

    $testProjects = @(
        'tests/CelesteDesktop.Install.Tests/CelesteDesktop.Install.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Tests/CelesteDesktop.AssetWorker.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Meta.Tests/CelesteDesktop.AssetWorker.Meta.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Data.Tests/CelesteDesktop.AssetWorker.Data.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.SpriteXml.Tests/CelesteDesktop.AssetWorker.SpriteXml.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Catalog.Tests/CelesteDesktop.AssetWorker.Catalog.Tests.csproj',
        'tests/CelesteDesktop.RealInstallConformance.Tests/CelesteDesktop.RealInstallConformance.Tests.csproj',
        'tests/CelesteDesktop.Simulation.Core.Tests/CelesteDesktop.Simulation.Core.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-020 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run `
        --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-020 progress demo failed: $LASTEXITCODE." }

    $manifestPath = Join-Path $demoOutput 'manifest.json'
    $reportPath = Join-Path $demoOutput 'index.html'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-020' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.catalog.entityCount -ne 2 -or
        $manifest.catalog.frameCount -ne 2 -or
        $manifest.catalog.decodedPageCount -ne 1 -or
        $manifest.simulation.tickRate -ne 60 -or
        $manifest.simulation.tickCount -ne 13 -or
        $manifest.simulation.finalActorX -ne 8 -or
        $manifest.simulation.finalPlatformX -ne 3 -or
        $manifest.simulation.carryEventCount -ne 3 -or
        $manifest.simulation.blockedEventCount -ne 1 -or
        $manifest.simulation.deterministicReplay -ne $true -or
        $manifest.simulation.rows.Count -ne 13) {
        throw 'CDR-020 demo manifest did not match the generated simulation contract.'
    }
    if ($report -notmatch 'CDR-020' -or
        $report -notmatch 'diagnostic_placeholder=true' -or
        $report -notmatch 'ActorBlocked' -or
        $report -notmatch '完全一致') {
        throw 'CDR-020 demo report omitted required simulation evidence.'
    }

    Write-Host 'CDR-020 OFFLINE VERIFICATION PASSED: no real installation, game, GUI, or desktop access.'
}
finally {
    Pop-Location
}
