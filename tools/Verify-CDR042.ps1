[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-042-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-042 restore failed: $LASTEXITCODE." }
    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-042 build failed: $LASTEXITCODE." }

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
        'tests/CelesteDesktop.Entity.Spring.Tests/CelesteDesktop.Entity.Spring.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-042 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-042 progress demo failed: $LASTEXITCODE." }

    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-042' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.glider.DeterministicReplay -ne $true -or
        $manifest.spring.source -ne 'program-generated-geometry-and-input' -or
        $manifest.spring.fidelity -ne 'partial' -or
        $manifest.spring.tickCount -ne 30 -or
        $manifest.spring.ActivationCount -ne 3 -or
        $manifest.spring.LaunchCount -ne 3 -or
        $manifest.spring.ReadyCount -ne 3 -or
        $manifest.spring.PlayerApplicationCount -ne 1 -or
        $manifest.spring.TheoApplicationCount -ne 1 -or
        $manifest.spring.GliderApplicationCount -ne 1 -or
        $manifest.spring.DeterministicReplay -ne $true) {
        throw 'CDR-042 demo manifest did not match the deterministic Spring contract.'
    }
    if ($report -notmatch 'CDR-042' -or $report -notmatch 'SPRING_ACTIVATED' -or
        $report -notmatch 'SPRING_LAUNCH_ISSUED' -or $report -notmatch 'SPRING_COOLDOWN_STARTED' -or
        $report -notmatch 'SPRING_READY' -or $report -notmatch 'id="plain-language-proof"' -or
        $report -notmatch 'id="plain-language-limits"') {
        throw 'CDR-042 demo report omitted required Spring or limitation evidence.'
    }

    Write-Host 'CDR-042 OFFLINE VERIFICATION PASSED: generated Spring activation, target routing, Player/Theo/Glider application, lifecycle and isolation passed; no visible GUI, live input, installation access or commercial bytes.'
}
finally { Pop-Location }
