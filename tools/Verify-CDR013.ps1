[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-013-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) {
        throw "CDR-013 restore failed with exit code $LASTEXITCODE."
    }

    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "CDR-013 build failed with exit code $LASTEXITCODE."
    }

    & dotnet run `
        --project tests/CelesteDesktop.Install.Tests/CelesteDesktop.Install.Tests.csproj `
        --configuration Release `
        --no-build `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "CDR-010 regression tests failed with exit code $LASTEXITCODE."
    }

    & dotnet run `
        --project tests/CelesteDesktop.AssetWorker.Tests/CelesteDesktop.AssetWorker.Tests.csproj `
        --configuration Release `
        --no-build `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "CDR-011 regression tests failed with exit code $LASTEXITCODE."
    }

    & dotnet run `
        --project tests/CelesteDesktop.AssetWorker.Meta.Tests/CelesteDesktop.AssetWorker.Meta.Tests.csproj `
        --configuration Release `
        --no-build `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "CDR-012 metadata tests failed with exit code $LASTEXITCODE."
    }

    & dotnet run `
        --project tests/CelesteDesktop.AssetWorker.Data.Tests/CelesteDesktop.AssetWorker.Data.Tests.csproj `
        --configuration Release `
        --no-build `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "CDR-013 data tests failed with exit code $LASTEXITCODE."
    }

    $demoOutput = Join-Path $verificationArtifacts ('progress-demo-' + [guid]::NewGuid().ToString('N'))

    & dotnet run `
        --project samples/CelesteDesktop.ProgressDemo/CelesteDesktop.ProgressDemo.csproj `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --output $demoOutput
    if ($LASTEXITCODE -ne 0) {
        throw "CDR-013 progress demo failed with exit code $LASTEXITCODE."
    }

    $demoManifestPath = Join-Path $demoOutput 'manifest.json'
    $demoReportPath = Join-Path $demoOutput 'index.html'
    if (-not (Test-Path -LiteralPath $demoManifestPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $demoReportPath -PathType Leaf)) {
        throw 'CDR-013 progress demo did not produce both review artifacts.'
    }

    $demoManifest = Get-Content -LiteralPath $demoManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($demoManifest.demoId -ne 'CDR-013' -or
        $demoManifest.diagnosticPlaceholder -ne $true -or
        $demoManifest.source -ne 'program-generated' -or
        $demoManifest.persistedCommercialBytes -ne 0 -or
        $demoManifest.frame.width -ne 8 -or
        $demoManifest.frame.height -ne 6 -or
        $demoManifest.entries.Count -ne 2) {
        throw 'CDR-013 progress demo manifest did not match the synthetic contract.'
    }

    $demoReport = Get-Content -LiteralPath $demoReportPath -Raw -Encoding UTF8
    if ($demoReport -notmatch 'diagnostic_placeholder=true' -or
        $demoReport -notmatch $demoManifest.frame.contentSha256) {
        throw 'CDR-013 progress demo report omitted required safety evidence.'
    }

    $developerLaunchers = Get-ChildItem -LiteralPath $projectRoot -File -Filter '*.cmd'
    $demoLauncher = $developerLaunchers | Where-Object {
        (Get-Content -LiteralPath $_.FullName -Raw) -match 'CelesteDesktop.ProgressDemo'
    }
    $verificationLauncher = $developerLaunchers | Where-Object {
        (Get-Content -LiteralPath $_.FullName -Raw) -match 'Verify-CDR013.ps1'
    }
    if (@($demoLauncher).Count -ne 1 -or
        @($verificationLauncher).Count -ne 1) {
        throw 'CDR-013 developer-facing command launchers are missing.'
    }
}
finally {
    Pop-Location
}
