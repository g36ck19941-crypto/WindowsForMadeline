$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $repo 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA = Join-Path $repo 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES = Join-Path $repo 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$dll = Join-Path $repo 'samples/CelesteDesktop.ProgressDemo/bin/Release/net8.0/CelesteDesktop.ProgressDemo.dll'
$fixture = Join-Path $repo ('artifacts/cdr-082-restricted-process/report-checks-' + [guid]::NewGuid().ToString('N'))
for($cursor=$fixture; $cursor; $cursor=[IO.Path]::GetDirectoryName($cursor)) {
    if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'PROGRESS_FIXTURE_LINK'}
}
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
Push-Location $repo
try {
    & dotnet restore samples/CelesteDesktop.ProgressDemo --configfile NuGet.Offline.Config
    if($LASTEXITCODE -ne 0){throw 'PROGRESS_OFFLINE_RESTORE_FAILED'}
    & dotnet build samples/CelesteDesktop.ProgressDemo -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'PROGRESS_BUILD_FAILED'}
    $actual = Join-Path $repo 'artifacts/cdr-082-restricted-process/summary.json'
    & dotnet $dll --output (Join-Path $fixture 'actual') --restricted-report $actual
    if($LASTEXITCODE -ne 0){throw 'CURRENT_PARTIAL_REPORT_FAILED'}
    $html = [IO.File]::ReadAllText((Join-Path $fixture 'actual/index.html'))
    if($html -notmatch 'data-restricted-status="partial-managed-startup-blocked"'){throw 'MANAGED_FAILURE_HIDDEN'}
    & dotnet $dll --output (Join-Path $fixture 'missing') --restricted-report (Join-Path $fixture 'absent.json')
    if($LASTEXITCODE -ne 0){throw 'ABSENT_REPORT_FAILED'}
    if([IO.File]::ReadAllText((Join-Path $fixture 'missing/index.html')) -notmatch 'data-restricted-status="not-inspected"'){throw 'ABSENT_REPORT_CALLED_PASS'}
    foreach($test in @('unsafe-original','inconsistent-status','oversized','outside-artifacts')) {
        $report = Get-Content -LiteralPath $actual -Raw | ConvertFrom-Json
        if($test -eq 'unsafe-original'){$report.originalLoaded=$true}
        if($test -eq 'inconsistent-status'){$report.managedProbeExitCode=0}
        $content = $report | ConvertTo-Json -Depth 8
        if($test -eq 'oversized'){$content += (' ' * 20000)}
        $reportPath = Join-Path $fixture ($test+'.json')
        [IO.File]::WriteAllText($reportPath,$content)
        if($test -eq 'outside-artifacts'){$reportPath=Join-Path $repo 'docs/GOAL.md'}
        $savedPreference=$ErrorActionPreference
        try { $ErrorActionPreference='Continue'; $rejection=@(& dotnet $dll --output (Join-Path $fixture $test) --restricted-report $reportPath 2>&1); $exitCode=$LASTEXITCODE }
        finally { $ErrorActionPreference=$savedPreference }
        if($exitCode -ne 2){throw ('RESTRICTED_REPORT_NOT_REJECTED_'+$test)}
    }
    Write-Output 'RESTRICTED_PROGRESS_CHECKS passed=6 failed=0 guiOpened=false'
} finally {Pop-Location}
