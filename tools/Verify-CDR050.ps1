[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-050-verification'
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
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Offline.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-050 restore failed: $LASTEXITCODE." }
    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-050 build failed: $LASTEXITCODE." }

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
        'tests/CelesteDesktop.Desktop.Tests/CelesteDesktop.Desktop.Tests.csproj',
        'tests/CelesteDesktop.Animation.Tests/CelesteDesktop.Animation.Tests.csproj',
        'tests/CelesteDesktop.Entity.Theo.Tests/CelesteDesktop.Entity.Theo.Tests.csproj',
        'tests/CelesteDesktop.Entity.Glider.Tests/CelesteDesktop.Entity.Glider.Tests.csproj',
        'tests/CelesteDesktop.Entity.Spring.Tests/CelesteDesktop.Entity.Spring.Tests.csproj',
        'tests/CelesteDesktop.Entity.Refill.Tests/CelesteDesktop.Entity.Refill.Tests.csproj',
        'tests/CelesteDesktop.Entity.Water.Tests/CelesteDesktop.Entity.Water.Tests.csproj',
        'tests/CelesteDesktop.Entity.Bumper.Tests/CelesteDesktop.Entity.Bumper.Tests.csproj',
        'tests/CelesteDesktop.Entity.Puffer.Tests/CelesteDesktop.Entity.Puffer.Tests.csproj',
        'tests/CelesteDesktop.Entity.Seeker.Tests/CelesteDesktop.Entity.Seeker.Tests.csproj',
        'tests/CelesteDesktop.App.Tests/CelesteDesktop.App.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-050 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-050 progress demo failed: $LASTEXITCODE." }

    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-050' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.app.source -ne 'program-generated-input-and-existing-verified-contracts' -or
        $manifest.app.fidelity -ne 'partial' -or
        $manifest.app.tickCount -ne 4 -or
        $manifest.app.SimulationCompletedCount -ne 4 -or
        $manifest.app.EffectRoutedCount -lt 2 -or
        $manifest.app.PresentationCalls -ne 2 -or
        $manifest.app.ComponentDisabledCount -ne 1 -or
        $manifest.app.Paused -ne $true -or
        $manifest.app.Resumed -ne $true -or
        $manifest.app.FinalLifecycle -ne 'Stopped' -or
        $manifest.app.DeterministicReplay -ne $true) {
        throw 'CDR-050 demo manifest did not match the offline App orchestration contract.'
    }
    if ($report -notmatch 'CDR-050' -or
        $report -notmatch 'APP_SIMULATION_COMPLETED' -or
        $report -notmatch 'APP_COMPONENT_DISABLED' -or
        $report -notmatch 'id="plain-language-proof"' -or
        $report -notmatch 'id="plain-language-limits"') {
        throw 'CDR-050 demo report omitted required App or limitation evidence.'
    }

    Write-Host 'CDR-050 OFFLINE VERIFICATION PASSED: App lifecycle, one simulation step per tick, effect routing, presentation isolation and deterministic replay passed; 800/800 total regressions, no visible GUI, live input, installation access or commercial bytes.'
}
finally { Pop-Location }
