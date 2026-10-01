[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-047-verification'
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
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-047 restore failed: $LASTEXITCODE." }
    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-047 build failed: $LASTEXITCODE." }

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
        'tests/CelesteDesktop.Entity.Seeker.Tests/CelesteDesktop.Entity.Seeker.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-047 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-047 progress demo failed: $LASTEXITCODE." }

    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-047' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.puffer.DeterministicReplay -ne $true -or
        $manifest.seeker.source -ne 'program-generated-geometry-and-input' -or
        $manifest.seeker.fidelity -ne 'partial' -or
        $manifest.seeker.tickCount -ne 30 -or
        $manifest.seeker.PatrolCount -ne 7 -or
        $manifest.seeker.AlertCount -ne 3 -or
        $manifest.seeker.ChaseStartCount -ne 3 -or
        $manifest.seeker.ChasedCount -ne 3 -or
        $manifest.seeker.WindupCount -ne 2 -or
        $manifest.seeker.DashStartCount -ne 2 -or
        $manifest.seeker.DashedCount -ne 1 -or
        $manifest.seeker.TargetHitCount -ne 1 -or
        $manifest.seeker.WallHitCount -ne 1 -or
        $manifest.seeker.StunnedCount -ne 2 -or
        $manifest.seeker.RecoveredCount -ne 2 -or
        $manifest.seeker.TargetLostCount -ne 1 -or
        $manifest.seeker.DeterministicReplay -ne $true) {
        throw 'CDR-047 demo manifest did not match the deterministic Seeker contract.'
    }
    if ($report -notmatch 'CDR-047' -or $report -notmatch 'SEEKER_ALERTED' -or
        $report -notmatch 'SEEKER_CHASE_STARTED' -or $report -notmatch 'SEEKER_DASH_STARTED' -or
        $report -notmatch 'SEEKER_TARGET_HIT' -or $report -notmatch 'SEEKER_WALL_HIT' -or
        $report -notmatch 'SEEKER_RECOVERED' -or
        $report -notmatch 'id="plain-language-proof"' -or
        $report -notmatch 'id="plain-language-limits"') {
        throw 'CDR-047 demo report omitted required Seeker or limitation evidence.'
    }

    Write-Host 'CDR-047 OFFLINE VERIFICATION PASSED: generated Seeker patrol, alert, chase, windup, dash, target hit, wall stun, recovery, target loss and isolation passed; 750/750 total regressions, no visible GUI, live input, installation access or commercial bytes.'
}
finally { Pop-Location }
