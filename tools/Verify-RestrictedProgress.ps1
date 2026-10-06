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
    if($html -notmatch 'data-startup-diagnostics="recorded"'){throw 'STARTUP_DIAGNOSTICS_HIDDEN'}
    if($html -notmatch 'data-latest-update="method-design-v1"'){throw 'LATEST_UPDATE_HIDDEN'}
    & dotnet $dll --output (Join-Path $fixture 'missing') --restricted-report (Join-Path $fixture 'absent.json')
    if($LASTEXITCODE -ne 0){throw 'ABSENT_REPORT_FAILED'}
    if([IO.File]::ReadAllText((Join-Path $fixture 'missing/index.html')) -notmatch 'data-restricted-status="not-inspected"'){throw 'ABSENT_REPORT_CALLED_PASS'}
    foreach($test in @('unsafe-original','inconsistent-status','invalid-phase','image-budget','contradictory-image','timeline-budget','source-confusion','unsafe-image-name','oversized','outside-artifacts')) {
        $report = Get-Content -LiteralPath $actual -Raw | ConvertFrom-Json
        if($test -eq 'unsafe-original'){$report.originalLoaded=$true}
        if($test -eq 'inconsistent-status'){$report.managedProbeExitCode=0}
        if($test -eq 'invalid-phase'){$report.lastOwnPhase='original-ready'}
        if($test -eq 'image-budget'){$report.loadedImageCount=129}
        if($test -eq 'contradictory-image'){$report.loadedImageCount=0;$report.observedCoreClrImage=$true}
        if($test -eq 'timeline-budget'){$report.startupEventCount=257}
        if($test -eq 'source-confusion'){$report.monitorFailureSource='owned-child-debug-event'}
        if($test -eq 'unsafe-image-name'){$report.lastObservedLoadedImage='C:\private\image.dll'}
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
    $frameworkActual=Join-Path $repo 'artifacts/cdr-082-restricted-process/framework-summary.json'
    & dotnet $dll --output (Join-Path $fixture 'framework-actual') --framework-probe-report $frameworkActual
    if($LASTEXITCODE -ne 0){throw 'FRAMEWORK_PARTIAL_REPORT_FAILED'}
    if([IO.File]::ReadAllText((Join-Path $fixture 'framework-actual/index.html')) -notmatch 'data-framework-probe-status="partial-own-net472-blocked"'){throw 'FRAMEWORK_FAILURE_HIDDEN'}
    & dotnet $dll --output (Join-Path $fixture 'framework-missing') --framework-probe-report (Join-Path $fixture 'framework-absent.json')
    if($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $fixture 'framework-missing/index.html')) -notmatch 'data-framework-probe-status="not-inspected"'){throw 'FRAMEWORK_MISSING_CALLED_PASS'}
    foreach($test in @('unsafe','false-pass','too-many','wrong-target','oversized','outside')){
        $report=Get-Content -LiteralPath $frameworkActual -Raw|ConvertFrom-Json
        if($test -eq 'unsafe'){$report.originalLoaded=$true}
        if($test -eq 'false-pass'){$report.probeExitCode=0}
        if($test -eq 'too-many'){$report.passed=7}
        if($test -eq 'wrong-target'){$report.framework='net8'}
        $content=$report|ConvertTo-Json -Depth 8
        if($test -eq 'oversized'){$content+=(' '*20000)}
        $path=Join-Path $fixture ('framework-'+$test+'.json');[IO.File]::WriteAllText($path,$content)
        if($test -eq 'outside'){$path=Join-Path $repo 'docs/GOAL.md'}
        $savedPreference=$ErrorActionPreference
        try{$ErrorActionPreference='Continue';$rejection=@(& dotnet $dll --output (Join-Path $fixture ('framework-'+$test)) --framework-probe-report $path 2>&1);$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
        if($code -ne 2){throw ('FRAMEWORK_REPORT_NOT_REJECTED_'+$test)}
    }
    Write-Output 'RESTRICTED_PROGRESS_CHECKS passed=20 failed=0 guiOpened=false'
} finally {Pop-Location}
