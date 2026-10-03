$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'artifacts/cdr-081-verification/dotnet-home'
$env:APPDATA = Join-Path $projectRoot 'artifacts/cdr-081-verification/appdata'
$env:NUGET_PACKAGES = Join-Path $projectRoot 'artifacts/cdr-081-verification/nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Verify-RuntimeIsolation.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Reviewed managed isolation suite failed.' }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Invoke-OriginalCompileProbe.ps1') -SelfTest
if ($LASTEXITCODE -ne 0) { throw 'CDR082 synthetic probe tests failed.' }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Verify-CDR081.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Retained-tool regression gate failed.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    & dotnet restore tools/CelesteDesktop.CompileClosure --configfile NuGet.Offline.Config
    if ($LASTEXITCODE -ne 0) { throw 'Closure tool offline restore failed.' }
    & dotnet build tools/CelesteDesktop.CompileClosure -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Closure tool build failed.' }
    & dotnet tools/CelesteDesktop.CompileClosure/bin/Release/net10.0/CelesteDesktop.CompileClosure.dll --self-test
    if ($LASTEXITCODE -ne 0) { throw 'Generated closure/emit checks failed.' }
    $fixture = Join-Path $projectRoot ('artifacts/cdr-082-verification/report-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $fixture -Force | Out-Null
    $inputReport = Join-Path $fixture 'summary.json'
    $demoDll = 'samples/CelesteDesktop.ProgressDemo/bin/Release/net8.0/CelesteDesktop.ProgressDemo.dll'
    $report = [ordered]@{schemaVersion=1;taskId='CDR-082';candidate='orig/Celeste.exe';recoveredSourceFiles=15;selectedCoreFiles=15;recoveredCodeExecuted=$false;gameLaunched=$false;guiOpened=$false;installationWrites=0;dependencyDownloads=0;commercialMaterialLocalOnly=$true;runtimeIntegrated=$false;compileEstablished=$false;compileAttempted=$true;compileExitCode=1}
    $report | ConvertTo-Json | Set-Content -LiteralPath $inputReport -Encoding UTF8
    $demoOutput = Join-Path $fixture 'demo'
    & dotnet $demoDll --output $demoOutput --compile-report $inputReport
    if ($LASTEXITCODE -ne 0) { throw 'Generated blocked compile report failed.' }
    $manifest = Get-Content (Join-Path $demoOutput 'manifest.json') -Raw | ConvertFrom-Json
    if (-not $manifest.compileReportAvailable -or $manifest.runtimeIntegrated) { throw 'Compile summary availability failed.' }
    $html = Get-Content (Join-Path $demoOutput 'index.html') -Raw -Encoding UTF8
    if ($manifest.compileStatus -ne 'compile-blocked' -or $html -notmatch 'data-compile-status="compile-blocked"' -or $html -notmatch 'CDR-082') { throw 'Blocked compilation hidden.' }
    & dotnet $demoDll --output (Join-Path $fixture 'missing-demo') --compile-report (Join-Path $fixture 'missing.json')
    if ($LASTEXITCODE -ne 0) { throw 'Missing compile report failed.' }
    $manifest = Get-Content (Join-Path $fixture 'missing-demo/manifest.json') -Raw | ConvertFrom-Json
    if ($manifest.compileReportAvailable) { throw 'Missing compile report called available.' }
    foreach ($kind in @('executed','false-pass')) {
        $report.recoveredCodeExecuted = ($kind -eq 'executed')
        $report.compileEstablished = ($kind -eq 'false-pass')
        $report | ConvertTo-Json | Set-Content -LiteralPath $inputReport -Encoding UTF8
        $ErrorActionPreference = 'Continue'
        try { $lines = @(& dotnet $demoDll --output (Join-Path $fixture $kind) --compile-report $inputReport 2>&1); $code = $LASTEXITCODE }
        finally { $ErrorActionPreference = 'Stop' }
        if ($code -eq 0 -or ($lines -join '') -notmatch 'PROGRESS_REPORT_FAILED') { throw 'Unsafe/inconsistent compile report accepted.' }
    }
    $isolationInput=Join-Path $fixture 'isolation.json'
    $isolationReport=Get-Content (Join-Path $projectRoot 'artifacts/cdr-082-isolation/summary.json') -Raw | ConvertFrom-Json
    $isolationReport | ConvertTo-Json | Set-Content -LiteralPath $isolationInput -Encoding UTF8
    & dotnet $demoDll --output (Join-Path $fixture 'isolation-valid') --isolation-report $isolationInput
    if($LASTEXITCODE -ne 0) { throw 'Isolation summary valid case failed.' }
    $isolationHtml=Get-Content (Join-Path $fixture 'isolation-valid/index.html') -Raw -Encoding UTF8
    if($isolationHtml -notmatch 'data-isolation-status="managed-only-original-unbound"') { throw 'Isolation limits hidden.' }
    & dotnet $demoDll --output (Join-Path $fixture 'isolation-missing') --isolation-report (Join-Path $fixture 'absent-isolation.json')
    if($LASTEXITCODE -ne 0) { throw 'Missing isolation summary failed.' }
    $isolationHtml=Get-Content (Join-Path $fixture 'isolation-missing/index.html') -Raw -Encoding UTF8
    if($isolationHtml -notmatch 'data-isolation-status="not-inspected"') { throw 'Missing isolation summary called verified.' }
    foreach($kind in @('original-bound','wrong-count')) {
        $isolationReport.originalBound=($kind -eq 'original-bound')
        $isolationReport.checksPassed=if($kind -eq 'wrong-count'){0}else{45}
        $isolationReport | ConvertTo-Json | Set-Content -LiteralPath $isolationInput -Encoding UTF8
        $ErrorActionPreference='Continue'
        try { $lines=@(& dotnet $demoDll --output (Join-Path $fixture $kind) --isolation-report $isolationInput 2>&1); $code=$LASTEXITCODE }
        finally { $ErrorActionPreference='Stop' }
        if($code -eq 0 -or ($lines -join '') -notmatch 'PROGRESS_REPORT_FAILED') { throw 'False isolation proof accepted.' }
    }
} finally { Pop-Location }
Write-Output 'CDR082_VERIFIED syntheticProbeChecks=26 retainedCases=317 closureChecks=13 compileReportChecks=4 isolationChecks=45 isolationReportChecks=4 windowProbesExecuted=0 originalRuntimePassNotClaimed=true'
exit 0
