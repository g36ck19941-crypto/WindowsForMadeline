[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-032-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-032 restore failed: $LASTEXITCODE." }
    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-032 build failed: $LASTEXITCODE." }

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
        'tests/CelesteDesktop.Animation.Tests/CelesteDesktop.Animation.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-032 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj --configuration Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-032 progress demo failed: $LASTEXITCODE." }

    $manifest = Get-Content -LiteralPath (Join-Path $demoOutput 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.demoId -ne 'CDR-032' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.frame.pixelByteCount -ne 192 -or
        $manifest.catalog.entityCount -ne 2 -or
        $manifest.catalog.frameCount -ne 3 -or
        $manifest.desktop.source -ne 'program-generated' -or
        $manifest.animationPresentation.source -ne 'program-generated-validated-catalog' -or
        $manifest.animationPresentation.tickCount -ne 8 -or
        $manifest.animationPresentation.presentedCount -ne 8 -or
        $manifest.animationPresentation.frameChangedCount -ne 3 -or
        $manifest.animationPresentation.deterministicReplay -ne $true -or
        $manifest.animationPresentation.humanVisibilityConfirmed -ne $false -or
        ($manifest.animationPresentation.events | Where-Object EventId -eq 'ANIMATION_FRAME_RESOLVED').Count -ne 8 -or
        ($manifest.animationPresentation.events | Where-Object EventId -eq 'ANIMATION_FRAME_COMPOSED').Count -ne 8) {
        throw 'CDR-032 demo manifest did not match the offline animation presentation contract.'
    }
    # Keep source-level evidence markers ASCII-only so Windows PowerShell 5.1 can
    # execute this UTF-8 script without mis-decoding a non-ASCII string literal.
    if ($report -notmatch 'CDR-032' -or $report -notmatch '<th>Tick</th>' -or $report -notmatch 'demo/player/idle01' -or $report -notmatch 'human_visible=false' -or $report -notmatch 'id="plain-language-proof"' -or $report -notmatch 'id="plain-language-limits"') {
        throw 'CDR-032 demo report omitted required animation or limitation evidence.'
    }

    Write-Host 'CDR-032 OFFLINE VERIFICATION PASSED: validated catalog frames resolved and presented by fixed tick; no visible GUI, live input, installation write or commercial-byte persistence.'
}
finally { Pop-Location }
