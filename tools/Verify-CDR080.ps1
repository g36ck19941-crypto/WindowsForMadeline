[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts/cdr-080-verification'
$env:DOTNET_CLI_HOME = Join-Path $verificationArtifacts 'dotnet-home'
$env:APPDATA = Join-Path $verificationArtifacts 'appdata'
$env:NUGET_PACKAGES = Join-Path $verificationArtifacts 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
Push-Location $projectRoot
try {
    & dotnet restore CelesteDesktopRuntime.sln --configfile NuGet.Offline.Config
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    & dotnet build CelesteDesktopRuntime.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $retiredPattern = 'CelesteDesktop\.(Player|Simulation\.Core|Entity\.|App)'
    $projects = @(Get-ChildItem src,tests,samples -Recurse -Filter '*.csproj')
    foreach ($project in $projects) {
        if ($project.FullName -match $retiredPattern -or
            (Get-Content $project.FullName -Raw) -match $retiredPattern) { throw 'Retired project reference found.' }
    }
    $total = 0
    $suites = @(Get-ChildItem tests -Recurse -Filter '*.csproj')
    if ($suites.Count -ne 12) { throw 'Expected 12 retained test suites.' }
    foreach ($suite in $suites) {
        $lines = @(& dotnet run --project $suite.FullName -c Release --no-build --no-restore)
        if ($LASTEXITCODE -ne 0) { throw "Test failed: $($suite.Name)" }
        $lines | Write-Output
        $result = @($lines | Select-String 'RESULT total=(\d+) passed=(\d+) failed=(\d+)')
        if ($result.Count -ne 1) { throw 'Missing test result.' }
        $match = $result[0].Matches[0]
        if ($match.Groups[1].Value -ne $match.Groups[2].Value -or $match.Groups[3].Value -ne '0') {
            throw 'Incomplete test pass.'
        }
        $total += [int]$match.Groups[1].Value
    }
    & dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-build --no-restore -- --output artifacts/current-progress-demo
    if ($LASTEXITCODE -ne 0) { throw 'Report failed.' }
    $manifest = Get-Content artifacts/current-progress-demo/manifest.json -Raw | ConvertFrom-Json
    if ($manifest.demoId -ne 'CDR-080' -or $manifest.runtimeIntegrated -ne $false -or
        $manifest.persistedCommercialBytes -ne 0 -or $manifest.gameLaunched -ne $false) { throw 'Report boundary failed.' }
    Write-Output "CDR080_VERIFIED suites=$($suites.Count) passed=$total"
} finally { Pop-Location }
