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
}
finally {
    Pop-Location
}
