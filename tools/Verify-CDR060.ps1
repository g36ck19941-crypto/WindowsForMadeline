[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-060-verification'
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
    if ($LASTEXITCODE -ne 0) { throw "CDR-060 restore failed: $LASTEXITCODE." }
    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-060 build failed: $LASTEXITCODE." }

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
        'tests/CelesteDesktop.App.Tests/CelesteDesktop.App.Tests.csproj',
        'tests/CelesteDesktop.App.Host.Tests/CelesteDesktop.App.Host.Tests.csproj',
        'tests/CelesteDesktop.Contracts.Extensions.Tests/CelesteDesktop.Contracts.Extensions.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-060 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-060 progress demo failed: $LASTEXITCODE." }

    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.demoId -notmatch '^CDR-[0-9]{3}$' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.extensionContracts.taskId -ne 'CDR-060' -or
        $manifest.extensionContracts.source -ne 'synthetic-providers-only' -or
        $manifest.extensionContracts.fidelity -ne 'contract-only' -or
        $manifest.extensionContracts.AssetProviderId -ne 'demo.assets' -or
        $manifest.extensionContracts.AssetSourceKind -ne 'Synthetic' -or
        $manifest.extensionContracts.AssetResolutionStatus -ne 'Found' -or
        $manifest.extensionContracts.AssetBytes -ne 4 -or
        $manifest.extensionContracts.AssetBudgetRejected -ne $true -or
        $manifest.extensionContracts.WorldProviderId -ne 'demo.worlds' -or
        $manifest.extensionContracts.WorldCount -ne 1 -or
        $manifest.extensionContracts.RoomCount -ne 1 -or
        $manifest.extensionContracts.SolidCount -ne 2 -or
        $manifest.extensionContracts.SpawnCount -ne 1 -or
        $manifest.extensionContracts.EntityCount -ne 3 -or
        $manifest.extensionContracts.EntityKinds -ne 'TheoCrystal,Spring,Glider' -or
        $manifest.extensionContracts.WorldBudgetRejected -ne $true -or
        $manifest.extensionContracts.DeterministicReplay -ne $true) {
        throw 'CDR-060 demo manifest did not match the data-only provider contract.'
    }
    if ($report -notmatch 'CDR-060' -or
        $report -notmatch 'id="extension-provider-contracts"' -or
        $report -notmatch 'id="asset-budget-result"' -or
        $report -notmatch 'id="world-budget-result"' -or
        $report -notmatch 'id="plain-language-proof"' -or
        $report -notmatch 'id="plain-language-limits"') {
        throw 'CDR-060 demo report omitted required provider or limitation evidence.'
    }

    Write-Host 'CDR-060 BASELINE VERIFICATION PASSED IN CURRENT TREE: provider contracts remain valid; 910 current non-reference regressions passed, including later CDR-071/CDR-072/CDR-073/CDR-074 Player cases; no real map, Mod directory, executable code, installation/local-reference access, visible GUI or commercial bytes.'
}
finally { Pop-Location }
