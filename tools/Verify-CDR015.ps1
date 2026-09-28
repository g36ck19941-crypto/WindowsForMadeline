[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-015-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-015 restore failed: $LASTEXITCODE." }

    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-015 build failed: $LASTEXITCODE." }

    $testProjects = @(
        'tests/CelesteDesktop.Install.Tests/CelesteDesktop.Install.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Tests/CelesteDesktop.AssetWorker.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Meta.Tests/CelesteDesktop.AssetWorker.Meta.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Data.Tests/CelesteDesktop.AssetWorker.Data.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.SpriteXml.Tests/CelesteDesktop.AssetWorker.SpriteXml.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Catalog.Tests/CelesteDesktop.AssetWorker.Catalog.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-015 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))
    & dotnet run `
        --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-015 progress demo failed: $LASTEXITCODE." }

    $demoManifestPath = Join-Path $demoOutput 'manifest.json'
    $demoReportPath = Join-Path $demoOutput 'index.html'
    if (-not (Test-Path -LiteralPath $demoManifestPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $demoReportPath -PathType Leaf)) {
        throw 'CDR-015 progress demo did not produce both review artifacts.'
    }

    $manifest = Get-Content -LiteralPath $demoManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.demoId -ne 'CDR-015' -or
        $manifest.diagnosticPlaceholder -ne $true -or
        $manifest.source -ne 'program-generated' -or
        $manifest.persistedCommercialBytes -ne 0 -or
        $manifest.frame.width -ne 8 -or
        $manifest.frame.height -ne 6 -or
        $manifest.entries.Count -ne 2 -or
        $manifest.sprites.Count -ne 2 -or
        $manifest.catalog.entityCount -ne 2 -or
        $manifest.catalog.animationCount -ne 2 -or
        $manifest.catalog.frameCount -ne 2 -or
        $manifest.catalog.decodedPageCount -ne 1 -or
        $manifest.catalog.openedPageCount -ne 1 -or
        $manifest.catalog.catalogSha256 -notmatch '^[0-9a-f]{64}$') {
        throw 'CDR-015 progress demo manifest did not match the synthetic contract.'
    }

    $demoReport = Get-Content -LiteralPath $demoReportPath -Raw -Encoding UTF8
    if ($demoReport -notmatch 'diagnostic_placeholder=true' -or
        $demoReport -notmatch $manifest.catalog.catalogSha256 -or
        $demoReport -notmatch 'CDR-015') {
        throw 'CDR-015 progress demo report omitted required evidence.'
    }

    $developerLaunchers = Get-ChildItem -LiteralPath $projectRoot -File -Filter '*.cmd'
    $demoLauncher = $developerLaunchers | Where-Object {
        (Get-Content -LiteralPath $_.FullName -Raw) -match 'CelesteDesktop.ProgressDemo'
    }
    $verificationLauncher = $developerLaunchers | Where-Object {
        (Get-Content -LiteralPath $_.FullName -Raw) -match 'Verify-CDR015.ps1'
    }
    if (@($demoLauncher).Count -ne 1 -or @($verificationLauncher).Count -ne 1) {
        throw 'CDR-015 developer-facing command launchers are missing.'
    }
}
finally {
    Pop-Location
}
