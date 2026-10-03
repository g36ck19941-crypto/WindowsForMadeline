[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationArtifacts = Join-Path $projectRoot 'artifacts/cdr-081-verification'
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
    foreach ($project in @(Get-ChildItem src,tests,samples -Recurse -Filter '*.csproj')) {
        if ($project.FullName -match $retiredPattern -or
            (Get-Content $project.FullName -Raw) -match $retiredPattern) { throw 'Retired project reference found.' }
    }
    $analyzer = Get-Content tools/CelesteDesktop.AssemblyInventory/Program.cs -Raw
    if ($analyzer -match 'Assembly\.Load|AssemblyLoadContext|Process\.Start|GetMethodBody') {
        throw 'Analyzer contains forbidden execution/IL APIs.'
    }
    $total = 0
    $suites = @(Get-ChildItem tests -Recurse -Filter '*.csproj')
    if ($suites.Count -ne 13) { throw 'Expected 13 test suites.' }
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
    # Windows junction guards use only generated repository fixtures. Never touch the real installation.
    $fixture = Join-Path $verificationArtifacts ('junction-' + [guid]::NewGuid().ToString('N'))
    $install = Join-Path $fixture 'install'
    $target = Join-Path $fixture 'target'
    $alias = Join-Path $fixture 'alias'
    New-Item -ItemType Directory -Path $install,$target -Force | Out-Null
    Copy-Item tools/CelesteDesktop.AssemblyInventory/bin/Release/net8.0/CelesteDesktop.AssemblyInventory.dll (Join-Path $install 'Celeste.dll')
    New-Item -ItemType Junction -Path $alias -Target $install | Out-Null
    $origLink = Join-Path $install 'orig'
    New-Item -ItemType Junction -Path $origLink -Target $target | Out-Null
    try {
        foreach ($selected in @($alias,$install)) {
            # Windows PowerShell 5 treats native stderr as error records, including intentional negative probes.
            $ErrorActionPreference = 'Continue'
            try {
                $output = @(& dotnet tools/CelesteDesktop.AssemblyInventory/bin/Release/net8.0/CelesteDesktop.AssemblyInventory.dll --root $selected --output (Join-Path $fixture 'report.json') 2>&1)
                $probeExit = $LASTEXITCODE
            } finally { $ErrorActionPreference = 'Stop' }
            if ($probeExit -ne 2 -or ($output -join '') -notmatch 'REPARSE_POINT') { throw 'Junction guard failed.' }
            if (($output -join '') -match [regex]::Escape($selected)) { throw 'Diagnostic leaked private root.' }
        }
    } finally {
        # Delete the junction entries, never recursively delete their targets.
        [IO.Directory]::Delete($alias)
        [IO.Directory]::Delete($origLink)
    }
    $demoOutput = Join-Path $verificationArtifacts 'progress'
    & dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-build --no-restore -- --output $demoOutput
    if ($LASTEXITCODE -ne 0) { throw 'Progress report failed.' }
    $manifest = Get-Content (Join-Path $demoOutput 'manifest.json') -Raw | ConvertFrom-Json
    if ($manifest.demoId -ne 'CDR-081' -or $manifest.runtimeIntegrated -ne $false -or
        $manifest.persistedCommercialBytes -ne 0 -or $manifest.gameLaunched -ne $false) { throw 'Report boundary failed.' }
    $syntheticReport = Join-Path $fixture 'synthetic.json'
    & dotnet tools/CelesteDesktop.AssemblyInventory/bin/Release/net8.0/CelesteDesktop.AssemblyInventory.dll --root $install --output $syntheticReport
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic report failed.' }
    $fakeDemo = Join-Path $fixture 'demo'
    & dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-build --no-restore -- --output $fakeDemo --identity-report $syntheticReport
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic identity demo failed.' }
    $fakeManifest = Get-Content (Join-Path $fakeDemo 'manifest.json') -Raw | ConvertFrom-Json
    if ($fakeManifest.identityReportAvailable -ne $true -or $fakeManifest.runtimeIntegrated -ne $false) { throw 'Synthetic report boundary failed.' }
    $missingDemo = Join-Path $fixture 'missing-demo'
    & dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-build --no-restore -- --output $missingDemo --identity-report (Join-Path $fixture 'missing.json')
    if ($LASTEXITCODE -ne 0) { throw 'Missing identity demo failed.' }
    $missingManifest = Get-Content (Join-Path $missingDemo 'manifest.json') -Raw | ConvertFrom-Json
    if ($missingManifest.identityReportAvailable -ne $false) { throw 'Missing report was called inspected.' }
    $unsafeReport = Get-Content $syntheticReport -Raw | ConvertFrom-Json
    $unsafeReport.assemblyExecuted = $true
    $unsafeReportPath = Join-Path $fixture 'unsafe.json'
    $unsafeReport | ConvertTo-Json -Depth 20 | Set-Content $unsafeReportPath -Encoding UTF8
    $ErrorActionPreference = 'Continue'
    try {
        $rejection = @(& dotnet run --project samples/CelesteDesktop.ProgressDemo -c Release --no-build --no-restore -- --output (Join-Path $fixture 'unsafe-demo') --identity-report $unsafeReportPath 2>&1)
        $probeExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = 'Stop' }
    if ($probeExit -eq 0 -or ($rejection -join '') -notmatch 'safety contract failed') { throw 'Unsafe report was accepted.' }
    Write-Output "CDR081_VERIFIED suites=$($suites.Count) passed=$total junctionChecks=2 reportChecks=3"
} finally { Pop-Location }
exit 0
