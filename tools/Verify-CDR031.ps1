[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-031-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-031 restore failed: $LASTEXITCODE." }

    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-031 build failed: $LASTEXITCODE." }

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
        'tests/CelesteDesktop.Rendering.Tests/CelesteDesktop.Rendering.Tests.csproj',
        'tests/CelesteDesktop.Desktop.Tests/CelesteDesktop.Desktop.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-031 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    # This opt-in call is authorized for CDR-031 and emits aggregate counts/DPI only.
    & dotnet run --project tests/CelesteDesktop.Desktop.Tests/CelesteDesktop.Desktop.Tests.csproj --configuration Release --no-build --no-restore -- --real-readonly
    if ($LASTEXITCODE -ne 0) { throw "CDR-031 authorized read-only desktop snapshot failed: $LASTEXITCODE." }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-031 progress demo failed: $LASTEXITCODE." }

    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-031' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.desktop.source -ne 'program-generated' -or
        $manifest.desktop.snapshotCount -ne 2 -or
        $manifest.desktop.finalVisibleSurfaceCount -ne 2 -or
        $manifest.desktop.movedSurfaceCount -ne 1 -or
        $manifest.desktop.titlesRead -ne 0 -or
        $manifest.desktop.contentRead -ne 0 -or
        $manifest.desktop.screenshotsRead -ne 0 -or
        $manifest.desktop.inputRead -ne 0 -or
        $manifest.presentation.presentCalls -ne 3 -or
        $manifest.presentation.pixelsChangedCount -ne 2 -or
        $manifest.presentation.humanVisibilityConfirmed -ne $false) {
        throw 'CDR-031 demo manifest did not match the anonymous desktop contract.'
    }
    if ($report -notmatch 'CDR-031' -or $report -notmatch '匿名桌面几何' -or $report -notmatch 'surface-000001' -or $report -notmatch 'human_visible=false') {
        throw 'CDR-031 demo report omitted required progress or limitation evidence.'
    }

    Write-Host 'CDR-031 READ-ONLY VERIFICATION PASSED: anonymous geometry, DPI, visibility and velocity only; no titles, content, screenshots, input or visible GUI.'
}
finally {
    Pop-Location
}
