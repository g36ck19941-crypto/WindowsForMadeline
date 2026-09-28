[CmdletBinding()]
param(
    [string]$InstallRoot,
    [string]$EvidenceOutput,
    [switch]$OfflineOnly
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts\cdr-016-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'

if (-not $OfflineOnly -and [string]::IsNullOrWhiteSpace($InstallRoot)) {
    throw 'CDR-016 real conformance requires an explicit -InstallRoot.'
}
if ($OfflineOnly -and -not [string]::IsNullOrWhiteSpace($InstallRoot)) {
    throw 'Do not combine -OfflineOnly with -InstallRoot.'
}

if ([string]::IsNullOrWhiteSpace($EvidenceOutput)) {
    $EvidenceOutput = Join-Path $verificationArtifacts 'real-install-report.json'
}
$EvidenceOutput = [System.IO.Path]::GetFullPath($EvidenceOutput)

Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile (Join-Path $projectRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "CDR-016 restore failed: $LASTEXITCODE." }

    & dotnet build CelesteDesktopRuntime.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "CDR-016 build failed: $LASTEXITCODE." }

    $testProjects = @(
        'tests/CelesteDesktop.Install.Tests/CelesteDesktop.Install.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Tests/CelesteDesktop.AssetWorker.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Meta.Tests/CelesteDesktop.AssetWorker.Meta.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Data.Tests/CelesteDesktop.AssetWorker.Data.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.SpriteXml.Tests/CelesteDesktop.AssetWorker.SpriteXml.Tests.csproj',
        'tests/CelesteDesktop.AssetWorker.Catalog.Tests/CelesteDesktop.AssetWorker.Catalog.Tests.csproj',
        'tests/CelesteDesktop.RealInstallConformance.Tests/CelesteDesktop.RealInstallConformance.Tests.csproj'
    )
    foreach ($testProject in $testProjects) {
        & dotnet run --project $testProject --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "CDR-016 regression failed for ${testProject}: $LASTEXITCODE." }
    }

    if ($OfflineOnly) {
        Write-Host 'CDR-016 OFFLINE VERIFICATION PASSED: real installation was not accessed.'
        return
    }

    $canonicalInstallRoot = [System.IO.Path]::GetFullPath($InstallRoot)
    $canonicalEvidenceOutput = [System.IO.Path]::GetFullPath($EvidenceOutput)
    $installPrefix = [System.IO.Path]::TrimEndingDirectorySeparator($canonicalInstallRoot) + [System.IO.Path]::DirectorySeparatorChar
    if ($canonicalEvidenceOutput.StartsWith($installPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Evidence output must remain outside the selected installation.'
    }

    & dotnet run `
        --project tools/CelesteDesktop.RealInstallConformance/CelesteDesktop.RealInstallConformance.csproj `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --root $canonicalInstallRoot `
        --output $canonicalEvidenceOutput
    if ($LASTEXITCODE -ne 0) { throw "CDR-016 real conformance failed: $LASTEXITCODE." }

    $reportText = Get-Content -LiteralPath $canonicalEvidenceOutput -Raw -Encoding UTF8
    if ($reportText.Contains($canonicalInstallRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'CDR-016 report leaked the selected installation root.'
    }
    $report = $reportText | ConvertFrom-Json
    if ($report.TaskId -ne 'CDR-016' -or
        $report.Passed -ne $true -or
        $report.ReadOnly -ne $true -or
        $report.GameLaunched -ne $false -or
        $report.GuiLaunched -ne $false -or
        $report.InstallationWritesObserved -ne $false -or
        $report.PersistedCommercialBytes -ne 0 -or
        $report.SelectedRootPersisted -ne $false -or
        $report.SourceFileCount -ne 3 -or
        $report.Sources.Count -ne 3 -or
        $report.AtlasPageCount -ne 1 -or
        $report.AtlasEntryCount -ne 6824 -or
        $report.SpriteDefinitionCount -ne 5 -or
        $report.AnimationCount -ne 93 -or
        $report.ResolvedFrameCount -ne 706 -or
        $report.DecodedPageCount -ne 1 -or
        $report.DecoderFingerprintStable -ne $true -or
        $report.CatalogFingerprintStable -ne $true -or
        $report.SourceFingerprintSha256 -notmatch '^[0-9a-f]{64}$' -or
        $report.DecoderFingerprintSha256 -notmatch '^[0-9a-f]{64}$' -or
        $report.CatalogFingerprintSha256 -notmatch '^[0-9a-f]{64}$') {
        throw 'CDR-016 report did not match the authorized selected-install contract.'
    }

    Write-Host 'CDR-016 REAL CONFORMANCE PASSED: read-only, deterministic, zero commercial bytes persisted.'
}
finally {
    Pop-Location
}
